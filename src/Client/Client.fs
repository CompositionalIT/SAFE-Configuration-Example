module Client

open Elmish
open Elmish.React
open Fable.Remoting.Client
open System
open Shared

type Model =
    { Settings: Setting list
      Input: string }

type Msg =
    | SetInput of string
    | FindSetting
    | SettingFound of Setting

let configurationApi =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.buildProxy<IConfigurationApi>

let init(): Model * Cmd<Msg> =
    let model =
        { Settings = []
          Input = "" }
    model, Cmd.none

let update (msg: Msg) (model: Model): Model * Cmd<Msg> =
    match msg with
    | SetInput value ->
        { model with Input = value }, Cmd.none
    | FindSetting ->
        let key = model.Input
        let cmd = Cmd.OfAsync.perform configurationApi.getSetting key SettingFound
        { model with Input = "" }, cmd
    | SettingFound setting ->
        { model with Settings = model.Settings @ [ setting ] }, Cmd.none

open Fable.React
open Fable.React.Props
open Fulma

let navBrand =
    Navbar.Brand.div [ ] [
        Navbar.Item.a [
            Navbar.Item.Props [ Href "https://safe-stack.github.io/" ]
            Navbar.Item.IsActive true
        ] [
            img [ Src "/favicon.png"
                  Alt "Logo" ]
        ]
    ]

let containerBox (model : Model) (dispatch : Msg -> unit) =
    Box.box' [ ]
        [ Content.content [ ]
            [ Content.Ol.ol [ ]
                [ for setting in model.Settings ->
                    li [ ] [ str (sprintf "%s : %s" setting.Key setting.Value) ] ] ]
          Field.div [ Field.IsGrouped ]
            [ Control.p [ Control.IsExpanded ]
                [ Input.text
                    [ Input.Value model.Input
                      Input.Placeholder "Setting Key"
                      Input.OnChange (fun x -> SetInput x.Value |> dispatch) ] ]
              Control.p [ ] [
                Button.a [
                    Button.Color IsPrimary
                    Button.Disabled (String.IsNullOrWhiteSpace model.Input)
                    Button.OnClick (fun _ -> dispatch FindSetting) ]
                  [ str "Search" ] ] ] ]

let view (model : Model) (dispatch : Msg -> unit) =
    Hero.hero [
        Hero.Color IsPrimary
        Hero.IsFullHeight
        Hero.Props [
            Style [
                Background """linear-gradient(rgba(0, 0, 0, 0.5), rgba(0, 0, 0, 0.5)), url("https://unsplash.it/1200/900?random") no-repeat center center fixed"""
                BackgroundSize "cover"
            ]
        ]
    ] [
        Hero.head [ ] [
            Navbar.navbar [ ] [
                Container.container [ ] [ navBrand ]
            ]
        ]

        Hero.body [ ] [
            Container.container [ ] [
                Column.column [
                    Column.Width (Screen.All, Column.Is6)
                    Column.Offset (Screen.All, Column.Is3)
                ] [
                    Heading.p [ Heading.Modifiers [ Modifier.TextAlignment (Screen.All, TextAlignment.Centered) ] ] [ str "Configuration Explorer" ]
                    containerBox model dispatch
                ]
            ]
        ]
    ]

#if DEBUG
open Elmish.Debug
open Elmish.HMR
#endif

Program.mkProgram init update view
#if DEBUG
|> Program.withConsoleTrace
#endif
|> Program.withReactBatched "elmish-app"
#if DEBUG
|> Program.withDebugger
#endif
|> Program.run
