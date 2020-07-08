module Server

open Fable.Remoting.Server
open Fable.Remoting.Giraffe
open Saturn
open Giraffe
open Microsoft.AspNetCore.Http
open FSharp.Control.Tasks.V2
open Microsoft.Extensions.Configuration
open System

open Shared
open System.Reflection
open Microsoft.Extensions.Configuration.UserSecrets

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

let webApp next ctx =
    task {
        let handler =
            Remoting.createApi()
            |> Remoting.withRouteBuilder Route.builder
            |> Remoting.fromValue (secretsApi ctx)
            |> Remoting.buildHttpHandler
        return! handler next ctx
    }

let app =
    application {
        url "http://0.0.0.0:8085"
        use_router webApp
        memory_cache
        use_static "public"
        use_gzip
    }

run app
