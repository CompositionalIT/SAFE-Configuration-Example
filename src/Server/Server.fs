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
open Microsoft.Azure.KeyVault
open Microsoft.Azure.Services.AppAuthentication
open Microsoft.Extensions.Configuration.AzureKeyVault

open Shared

let getSetting (ctx : HttpContext) key = async {
    let config = ctx.GetService<IConfiguration>()
    let value = config.[key]
    if String.IsNullOrWhiteSpace(value)
    then return { Key = key; Value = "Not found" }
    else return { Key = key; Value = config.[key] }
}

let configurationApi (ctx : HttpContext) =
    { getSetting = getSetting ctx}

let webApp next ctx = task {
    let handler =
        Remoting.createApi()
        |> Remoting.withRouteBuilder Route.builder
        |> Remoting.fromValue (configurationApi ctx)
        |> Remoting.buildHttpHandler
    return! handler next ctx
}

type UserSecretsTarget = UserSecretsTarget of unit
let configureHost (hostBuilder : IHostBuilder) =
    hostBuilder.ConfigureAppConfiguration(fun ctx cfg ->

        if ctx.HostingEnvironment.IsDevelopment() then
            cfg.AddUserSecrets<UserSecretsTarget>() |> ignore

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
