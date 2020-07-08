namespace Shared

type Secret =
    { Key : string
      Value : string }

module Route =
    let builder typeName methodName =
        sprintf "/api/%s/%s" typeName methodName

type ISecretsApi =
    { getSecret : string -> Async<Secret> }
