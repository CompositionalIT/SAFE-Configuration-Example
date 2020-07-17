module Server

open Fable.Remoting.Server
open Fable.Remoting.Giraffe
open Saturn
open Giraffe
open Microsoft.AspNetCore.Http
open FSharp.Control.Tasks.V2
open Microsoft.Extensions.Configuration
open System
open Microsoft.Extensions.Hosting

open Shared

type AnyType = AnyType

let getSecret (ctx : HttpContext) key = async {
    let config = ctx.GetService<IConfiguration>()
    let value = config.[key]
    if String.IsNullOrWhiteSpace(value)
    then return { Key = key; Value = "Not found" }
    else return { Key = key; Value = config.[key] }
}

let secretsApi (ctx : HttpContext) =
    { getSecret = getSecret ctx}

let webApp next ctx = task {
    let handler =
        Remoting.createApi()
        |> Remoting.withRouteBuilder Route.builder
        |> Remoting.fromValue (secretsApi ctx)
        |> Remoting.buildHttpHandler
    return! handler next ctx
}

let configureHost (hostBuilder : IHostBuilder) =
    hostBuilder.ConfigureAppConfiguration(fun ctx cfg ->

        // This shouldn't be necessary as Saturn already calls Host.CreateDefaultBuilder
        // which adds secrets if in Dev environment. I think maybe the issue is that
        // you have to pass a ref to a type from the assembly, which would be Saturn
        // instead of this project. By passing AnyType below we actually reg our secrets.
        if ctx.HostingEnvironment.IsDevelopment()
        then cfg.AddUserSecrets<AnyType>() |> ignore

        // This is where you enable KeyVault for production, requires installing some libs
        //if (ctx.HostingEnvironment.IsProduction())
        //then
        //    let builtConfig = cfg.Build()

        //    let azureServiceTokenProvider = new AzureServiceTokenProvider();
        //    let keyVaultClient =
        //        KeyVaultClient(
        //            KeyVaultClient.AuthenticationCallback(
        //                azureServiceTokenProvider.KeyVaultTokenCallback));

        //    cfg.AddAzureKeyVault(
        //        sprintf "https://%s.vault.azure.net/" builtConfig.["KeyVaultName"],
        //        keyVaultClient,
        //        DefaultKeyVaultSecretManager())
    ) |> ignore
    hostBuilder

let app =
    application {
        url "http://0.0.0.0:8085"
        host_config configureHost
        use_router webApp
        memory_cache
        use_static "public"
        use_gzip
    }

run app
