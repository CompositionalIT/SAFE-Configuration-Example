namespace Shared

type Setting =
    { Key : string
      Value : string }

module Route =
    let builder typeName methodName =
        sprintf "/api/%s/%s" typeName methodName

type IConfigurationApi =
    { getSetting : string -> Async<Setting> }
