# SAFE Configuration in ASP .NET Core

This is an example project to accompany a [blog on our website](https://www.compositional-it.com/news-blog/configuration-secrets-and-keyvault-with-asp-net-core/).

It allows you to explore the configuration of a simple SAFE application by searching for settings using their keys.

## Install pre-requisites

You'll need to install the following pre-requisites in order to build SAFE applications

* The [.NET Core SDK](https://www.microsoft.com/net/download)
* The [Yarn](https://yarnpkg.com/lang/en/docs/install/) package manager (you can also use `npm` but the usage of `yarn` is encouraged).
* [Node LTS](https://nodejs.org/en/download/) installed for the front end components.

## Secrets / Key Vault

There are a couple of settings already configured in appsettings.json and appsettings.Development.json for demonstration purposes. You can see how changing your environment affects the final configuration provided to the app.

To see values from local secrets, you will need to initialise a secret store as explained in the [blog post](https://www.compositional-it.com/news-blog/configuration-secrets-and-keyvault-with-asp-net-core/). By default, these will be loaded if you are in a Development environment.

To access values from Key Vault you will need to set up a Key Vault instance in Azure, authorise an App Service to access that vault, and then deploy the SAFE app to that App Service. You will also need to set the KeyVaultName value in appsettings.json. By default, these will be loaded if you are in a Staging or Production environment.

## Running the application

Before you run the project **for the first time only** you should install its local tools with this command:

```bash
dotnet tool restore
```

To concurrently run the server and the client components in watch mode use the following command:

```bash
dotnet fake build -t run
```

Then open `http://localhost:8080` in your browser.


## SAFE Stack Documentation

You will find more documentation about the used F# components at the following places:

* [Saturn](https://saturnframework.org/docs/)
* [Fable](https://fable.io/docs/)
* [Elmish](https://elmish.github.io/elmish/)

If you want to know more about the full Azure Stack and all of it's components (including Azure) visit the official [SAFE documentation](https://safe-stack.github.io/docs/).
