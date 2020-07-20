# Configuration in ASP .NET Core using Saturn

## What is configuration?

When writing applications we often have a bunch of settings which are used in our code, but the exact value of these settings depends on the environment in which the application is running.

These might include sensitive data such as passwords, connection strings and API keys.

We may also have different settings for things such as logging between development and production environments, for example, and wish to switch these dynamically.

ASP .NET Core has a [neat way of dealing with these concerns](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-3.1) by providing us with a dictionary of settings under the IConfiguration interface, which can be resolved from the service container or retrieved using dependency injection.


## How do we define configurations?

Configurations can be provided a [number of ways](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-3.1#configuration-providers). 

They can be defined simple JSON files which commonly live at the root of your web project (usually **Server** in a SAFE Stack solution).

These contain the key-value pairs that you wish to load at runtime. Here's an example:

```json
{
    "MySetting": "Hello World"
}
```

## What about secrets?

When we have sensitive data which we don't want to check into source control, ASP .NET Core provides us with a secrets store which lives outside of the project directory on the user's local machine and can be used for development purposes.

It is [**not** encrypted or otherwise secured](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-3.1&tabs=windows#secret-manager) however, so it unsuitable for use in production. For that, we use a service such as [Azure Key Vault](https://azure.microsoft.com/en-gb/services/key-vault/) to provide the configuration (more on this further down).

To initialise a local secrets file, we have two options.

### 1. From the console

Navigate to your web project directory and run ```dotnet user-secrets init```.

The console should show ```Set UserSecretsId to '{some guid}' for MSBuild project '{project path}'```.

If you look in the .fsproj file you should see an element like this ```<UserSecretsId>{some guid}</UserSecretsId>```.

You now have a secrets file stored on your local machine. You should not refer to it's location directly or rely on its internal format. All interaction with the secrets file is done through the dotnet tooling.

You can add secrets to the file using ```dotnet user-secrets set "Key" "Value"``` and list all secrets using ```dotnet user-secrets list```

For a complete list of the commands available see [How the Secret Manager tool works](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-3.1&tabs=windows#environment-variables).

### 2.  From Visual Studio

If you are using Visual Studio, you can initialise secrets by right clicking on the project and selecting **Manage User Secrets**. 

This will also open the secrets.json file in the IDE, where you can edit directly.

Note however that the secrets file doesn't use the same nested json object format that other configuration files use. It is 'flattened' using **colons** to indicate the hierarchy, so
```json
{
    "MySetting": {
        "SubSetting": "Hello" 
    }
}
```
becomes
```json
{
    "MySetting":"SubSetting":"Hello"
}
```

## How are configurations loaded?

When an ASP.NET Core application starts up it configures a [.NET Core Generic Host](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/generic-host?view=aspnetcore-3.1) using a [HostBuilder](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting.hostbuilder?view=dotnet-plat-ext-3.1) which encapsulates its resources, such as dependency injection, logging, configuration and hosted services such as a http server.

Most app templates begin with a default HostBuilder provided by the framework, and then customise as necessary.

This default HostBuilder does quite a few things, it is worth [reading up on all of it](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/generic-host?view=aspnetcore-3.1#set-up-a-host).

The _order_ in which configurations are loaded is significant, because they are being combined into one dictionary. If there are duplicate keys then the _last_ value loaded will be the one used. In the case of the default host, the load order is:

1. appsettings.json
1. appsettings.{Environment}.json
1. secrets.json (if in **Development** environment specifically)
1. Environment variables
1. Command line arguments

As you can see, the default host loads our configuration files provided their file names are in the ```appsettings.json``` or ```appsettings.{Environment}.json``` format and they exist at the root of our project directory, with the more specific settings paths taking precedence over the more general ones.

## How can we add settings to the IConfiguration at runtime?

The default host builder setup explained above is included out-of-the-box for us in most ASP .NET Core templates, [including by Saturn](https://github.com/SaturnFramework/Saturn/blob/55474aecd701b3e6416bac72c18cc8f8b13e8db7/src/Saturn/Application.fs#L110).

If we want to customise the host after this point, there are hooks provided by the framework to achieve this.

Using Saturn, we find the ```application``` computation expression at the root of our Server and add a ```host_config``` step. This allows us to modify an instance of ```IHostBuilder```.

```fsharp
let configureHost (hostBuilder : IHostBuilder) =
    hostBuilder
        .ConfigureAppConfiguration(
            fun (context : HostBuilderContext) (config : IConfigurationBuilder) ->
                // Add things to the configuration here, either manually or using a ConfigurationProvider 
            ) |> ignore
    hostBuilder

let app =
    application {
        host_config configureHost // add this step
    }

run app
```

> When creating the sample project for this blog post, I found that my secrets.json file wasn't being loaded even though I was in a Development environment. I suspect the reason for this is that the host requires a reference to the assembly which contains the ```<UserSecretsId>``` element in its project file, and in this case the executing assembly is Saturn instead of our project. I worked around the issue by manually adding secrets to the configuration using ```host_config``` as explained above, adding the lines
>```fsharp
>if context.HostingEnvironment.IsDevelopment() then 
>    config.AddUserSecrets<AnyType>() |> ignore
>```
> where ```AnyType``` is literally any type in the project, it is just a handle to the assembly to allow the UserSecretsId to be located.


## How do we use Azure KeyVault to store our secrets?

If we want to use a secure, cloud based store for our secrets, then [Azure Key Vault](https://docs.microsoft.com/en-gb/azure/key-vault/) is a great choice. 

### How do we add a secret to KeyVault?

Assuming we have set up an instance of a key vault online and [authorised  our application](https://docs.microsoft.com/en-us/azure/key-vault/general/managed-identity#grant-your-app-access-to-key-vault) to access it, then we may add secrets to it in a similar way to our secrets.json file, however this time we use **double dashes** to flatten the object hierarchy, so
```json
{
    "MySetting": {
        "SubSetting": "Hello" 
    }
}
```
becomes ```MySetting--SubSetting``` as the **name** of a secret, with ```Hello``` as the **value**.


### How do we access KeyVault secrets in our app?


When we want to access secrets from key vault in our application, we have a couple of choices.

1. Key Vault References

    If we are hosted on Azure we can use [Key Vault References](https://docs.microsoft.com/en-us/azure/app-service/app-service-key-vault-references) to define slot-level environment variables, which in turn reference values stored in our Key Vault.

    This is acheived by setting a specially formatted string as the Environment Variable value, which links it to the appropriate Key Vault entry:
    
    ```@Microsoft.KeyVault(VaultName={keyvault name};SecretName={secret name};SecretVersion={version guid})```
    
    We can choose to deploy these Application Settings with [Farmer](https://compositionalit.github.io/farmer/) or traditional ARM templates, or alternatively manually configure them in the Azure portal.


2. Azure Key Vault Configuration Provider

    The [Azure Key Vault Configuration Provider](https://docs.microsoft.com/en-us/aspnet/core/security/key-vault-configuration?view=aspnetcore-3.1) allows us to load secrets from a Key Vault into our application settings, directly in our code.

    Providing we are hosting on Azure and our Key Vault has granted Get permission to our app, the SDK will take care of access token management [for us](https://docs.microsoft.com/en-us/aspnet/core/security/key-vault-configuration?view=aspnetcore-3.1#use-managed-identities-for-azure-resources). Otherwise we need to do a bit more [setup beforehand](https://docs.microsoft.com/en-us/aspnet/core/security/key-vault-configuration?view=aspnetcore-3.1#use-application-id-and-x509-certificate-for-non-azure-hosted-apps).

    To get started, we install [Microsoft.Extensions.Configuration.AzureKeyVault](https://www.nuget.org/packages/Microsoft.Extensions.Configuration.AzureKeyVault/) from NuGet.

    Next we open the following namespaces in our Server module:
    ```fsharp
    open Microsoft.Azure.KeyVault
    open Microsoft.Azure.Services.AppAuthentication
    open Microsoft.Extensions.Configuration.AzureKeyVault
    ```

    Finally we configure a KeyVaultClient and pass it to the IConfigurationBuilder during Host setup, just as we did earlier with local secrets.

    We draw the name of our KeyVault dynamically from the existing IConfiguration which was passed from the default host. This allows us to use different key vaults for each environment if we wish.

    At the end of this process, our configureHost function could look something like this:

    ```fsharp
    let configureHost (hostBuilder : IHostBuilder) =
        hostBuilder.ConfigureAppConfiguration(fun ctx cfg ->

            if ctx.HostingEnvironment.IsDevelopment()
            then cfg.AddUserSecrets<AnyType>() |> ignore

            if (ctx.HostingEnvironment.IsStaging() || ctx.HostingEnvironment.IsProduction())
            then
                let builtConfig = cfg.Build()
                let tokenCallback authority resource scope =
                    AzureServiceTokenProvider().KeyVaultTokenCallback.Invoke(authority, resource, scope) 
                let keyVaultClient = new KeyVaultClient(KeyVaultClient.AuthenticationCallback(tokenCallback))
                cfg.AddAzureKeyVault(
                    sprintf "https://%s.vault.azure.net/" builtConfig.["KeyVaultName"],
                    keyVaultClient,
                    DefaultKeyVaultSecretManager()) |> ignore

        ) |> ignore
        hostBuilder

    ```
    where ```appsettings.json``` looks like
    ```json
    {
        "KeyVaultName": "MyProductionKeyVault"
    }
    ```
    and ```appsettings.Staging.json``` looks like
    ```json
    {
        "KeyVaultName": "MyStagingKeyVault"
    }
    ```


### Conclusion

Hopefully you can see that although there are a lot of options and moving parts to consider, it is fairly quick and easy to switch configurations and keep your secrets safe using ASP .NET Core, Azure and Saturn.

A sample project demonstrating most of the features discussed in this article can be found on [our Github](www.github.com).