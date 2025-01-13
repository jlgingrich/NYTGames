namespace NYTGT

open System
open FSharp.Data
open Thoth.Json.Net

/// Extensions to <code>Thoth.Json.Net.Decode</code>.
module Decode =
    let exactlyOne (decoder: Decoder<'value>) : Decoder<'value> =
        Decode.list decoder
        |> Decode.andThen (function
            | [ item ] -> Decode.succeed item
            | items -> Decode.fail $"Expected exactly one list entry, but got {List.length items} entries")

/// Simple way to dump a custom type into JSON
module Encode =
    let auto = Encode.Auto.toString

/// Extensions to <code>Microsoft.FSharp.Core.Result</code>.
module Result =
    let assertOk =
        function
        | Ok x -> x
        | Error e -> failwithf "Assertion failed: %A" e

module Helpers =
    let version i = $"v%d{i}"

    [<Literal>]
    let DateFormat = "yyyy-MM-dd"

    let formatDate (date: DateTime) = date.ToString(DateFormat)

    let urlForDate game v (date: DateTime) =
        $"https://www.nytimes.com/svc/%s{game}/%s{version v}/%s{formatDate date}.json"

    let getRequest url =
        Http.RequestString url |> String.filter (Char.IsAscii)

    let failNotImplemented () = failwith "Not implemented"

/// Common information that is published with each NYT game
type PublicationInformation =
    { Id: int
      PrintDate: DateTime
      Editor: string
      Constructors: string list }

// API implementations for each game

module Strands =
    type Game =
        { Info: PublicationInformation
          Clue: string
          Spangram: string
          ThemeWords: string list
          Board: string array }


    let getRaw date =
        Helpers.urlForDate "strands" 2u date |> Helpers.getRequest

    let private decoder: Decoder<Game> =
        Decode.object (fun get ->
            { Info =
                { Id = get.Required.Field "id" Decode.int
                  PrintDate = get.Required.Field "printDate" Decode.datetimeLocal
                  Editor = get.Required.Field "editor" Decode.string
                  Constructors = get.Required.Field "constructors" Decode.string |> List.singleton }
              Clue = get.Required.Field "clue" Decode.string
              Spangram = get.Required.Field "spangram" Decode.string
              ThemeWords = get.Required.Field "themeWords" (Decode.list Decode.string)
              Board = get.Required.Field "startingBoard" (Decode.array Decode.string) })


    let parse = Decode.fromString decoder

    let getGame date = getRaw date |> parse

    let getCurrentGame () = DateTime.Now |> getGame

module Connections =
    type Category = { Title: string; Cards: string list }

    type Game =
        { Info: PublicationInformation
          Categories: Category list }


    let getRaw date =
        Helpers.urlForDate "connections" 2u date |> Helpers.getRequest

    let private decodeCategory: Decoder<Category> =
        Decode.object (fun get ->
            { Title = get.Required.Field "title" Decode.string
              Cards =
                get.Required.Field
                    "cards"
                    (Decode.list (Decode.object (fun get -> get.Required.Field "content" Decode.string))) })

    let private decoder: Decoder<Game> =
        Decode.object (fun get ->
            { Info =
                { Id = get.Required.Field "id" Decode.int
                  PrintDate = get.Required.Field "print_date" Decode.datetimeLocal
                  Editor = get.Required.Field "editor" Decode.string
                  Constructors = List.empty }
              Categories = get.Required.Field "categories" (Decode.list decodeCategory) })

    let parse = Decode.fromString decoder

    let getGame date = getRaw date |> parse

    let getCurrentGame () = DateTime.Now |> getGame

module ConnectionsSportsEdition =
    type Category = { Title: string; Cards: string list }

    type Game =
        { Info: PublicationInformation
          Categories: Category list }

    let getRaw date =
        $"https://www.nytimes.com/games-assets/sports-connections/{Helpers.formatDate date}.json"
        |> Helpers.getRequest

    let private decodeCategory: Decoder<Category> =
        Decode.object (fun get ->
            { Title = get.Required.Field "title" Decode.string
              Cards =
                get.Required.Field
                    "cards"
                    (Decode.list (Decode.object (fun get -> get.Required.Field "content" Decode.string))) })

    let private decoder: Decoder<Game> =
        Decode.object (fun get ->
            { Info =
                { Id = get.Required.Field "id" Decode.int
                  PrintDate = get.Required.Field "printDate" Decode.datetimeLocal
                  Editor = get.Required.Field "editor" Decode.string
                  Constructors = List.empty }
              Categories = get.Required.Field "categories" (Decode.list decodeCategory) })

    let parse = Decode.fromString decoder

    let getGame date = getRaw date |> parse

    let getCurrentGame () = DateTime.Now |> getGame

module LetterBoxed =
    type Game =
        { Info: PublicationInformation
          Sides: string list
          Solution: string list
          Par: int }


    let getRaw () =
        let scriptPrefix = "window.gameData = "

        HtmlDocument.Load "https://www.nytimes.com/puzzles/letter-boxed"
        |> fun n -> n.CssSelect "script[type=text/javascript]"
        |> List.filter (fun n -> n.DirectInnerText().StartsWith scriptPrefix)
        |> List.exactlyOne
        |> fun n -> n.DirectInnerText()[String.length scriptPrefix ..]


    let private decoder: Decoder<Game> =
        Decode.object (fun get ->
            { Info =
                { Id = get.Required.Field "id" Decode.int
                  PrintDate = get.Required.Field "printDate" Decode.datetimeLocal
                  Editor = get.Required.Field "editor" Decode.string
                  Constructors = List.empty }
              Sides = get.Required.Field "sides" (Decode.list Decode.string)
              Solution = get.Required.Field "ourSolution" (Decode.list Decode.string)
              Par = get.Required.Field "par" Decode.int })

    let parse = Decode.fromString decoder

    let getGame date = Helpers.failNotImplemented ()

    let getCurrentGame () = getRaw () |> parse

module SpellingBee =
    type Game =
        { Info: PublicationInformation
          CenterLetter: char
          OuterLetters: char list
          Answers: string list }


    let getRaw () =
        let scriptPrefix = "window.gameData = "

        HtmlDocument.Load "https://www.nytimes.com/puzzles/spelling-bee"
        |> fun n -> n.CssSelect "script[type=text/javascript]"
        |> List.filter (fun n -> n.DirectInnerText().StartsWith scriptPrefix)
        |> List.exactlyOne
        |> fun n -> n.DirectInnerText()[String.length scriptPrefix ..]


    let private decoder: Decoder<Game> =
        Decode.object (fun get ->
            { Info =
                { Id = get.Required.At [ "today"; "id" ] Decode.int
                  PrintDate = get.Required.At [ "today"; "printDate" ] Decode.datetimeLocal
                  Editor = get.Required.At [ "today"; "editor" ] Decode.string
                  Constructors = List.empty }
              CenterLetter = get.Required.At [ "today"; "centerLetter" ] Decode.char
              OuterLetters = get.Required.At [ "today"; "outerLetters" ] (Decode.list Decode.char)
              Answers = get.Required.At [ "today"; "answers" ] (Decode.list Decode.string) })

    let parse = Decode.fromString decoder

    let getGame date = Helpers.failNotImplemented ()

    let getCurrentGame () = getRaw () |> parse

module Wordle =
    type Game =
        { Info: PublicationInformation
          Solution: string }


    let getRaw date =
        Helpers.urlForDate "wordle" 2u date |> Helpers.getRequest

    let private decoder: Decoder<Game> =
        Decode.object (fun get ->
            { Info =
                { Id = get.Required.Field "id" Decode.int
                  PrintDate = get.Required.Field "print_date" Decode.datetimeLocal
                  Editor = get.Required.Field "editor" Decode.string
                  Constructors = List.empty }
              Solution = get.Required.Field "solution" Decode.string |> _.ToUpper() })

    let parse = Decode.fromString decoder

    let getGame date = getRaw date |> parse

    let getCurrentGame () = DateTime.Now |> getGame

module Mini =
    type Direction =
        | Across
        | Down

    type Clue =
        { Direction: Direction
          Label: int
          Hint: string }

    type Game =
        { Info: PublicationInformation
          Solution: char option array array
          Clues: Clue list
          Height: int
          Width: int }

    let [<Literal>] BoardWidth = 5

    let getRaw () =
        "https://www.nytimes.com/svc/crosswords/v6/puzzle/mini.json"
        |> Helpers.getRequest

    let private decodeDirection: Decoder<Direction> =
        Decode.string
        |> Decode.andThen (function
            | "Across" -> Decode.succeed Across
            | "Down" -> Decode.succeed Down
            | invalid -> Decode.fail (sprintf " `%s` is an invalid clue direction" invalid))

    let private decodeClue: Decoder<Clue> =
        Decode.object (fun get ->
            { Direction = get.Required.Field "direction" decodeDirection
              Label = get.Required.Field "label" Decode.int
              Hint =
                get.Required.Field
                    "text"
                    (Decode.exactlyOne (Decode.object (fun get -> get.Required.Field "plain" Decode.string))) })

    let private decodeCell: Decoder<char option> =
        Decode.object (fun get -> get.Optional.Field "answer" Decode.string)
        |> Decode.andThen (Option.map Char.Parse >> Decode.succeed)

    let private decoder: Decoder<Game> =
        Decode.object (fun get ->
            { Info =
                { Id = get.Required.Field "id" Decode.int
                  PrintDate = get.Required.Field "publicationDate" Decode.datetimeLocal
                  Editor = get.Required.Field "editor" Decode.string
                  Constructors = get.Required.Field "constructors" (Decode.list Decode.string) }
              Solution =
                get.Required.Field
                    "body"
                    (Decode.exactlyOne (
                        Decode.object (fun get -> get.Required.Field "cells" (Decode.array decodeCell))
                     )
                     |> Decode.map (Array.chunkBySize BoardWidth))
              Clues =
                get.Required.Field
                    "body"
                    (Decode.exactlyOne (Decode.object (fun get -> get.Required.Field "clues" (Decode.list decodeClue))))
              Height =
                get.Required.Field
                    "body"
                    (Decode.exactlyOne (
                        Decode.object (fun get -> get.Required.At [ "dimensions"; "height" ] Decode.int)
                    ))
              Width =
                get.Required.Field
                    "body"
                    (Decode.exactlyOne (
                        Decode.object (fun get -> get.Required.At [ "dimensions"; "width" ] Decode.int)
                    )) })

    let parse = Decode.fromString decoder

    let getGame date = Helpers.failNotImplemented ()

    let getCurrentGame () = getRaw () |> parse


module Crossword =
    type Direction =
        | Across
        | Down

    type Clue =
        { Direction: Direction
          Label: int
          Hint: string }

    type Game =
        { Info: PublicationInformation
          Solution: char option array array
          Clues: Clue list
          Height: int
          Width: int }

    let [<Literal>] BoardWidth = 15

    let getRaw () =
        Http.RequestString("https://www.nytimes.com/svc/crosswords/v6/puzzle/daily.json", headers =[
            "x-games-auth-bypass", "true"
        ]) |> String.filter (Char.IsAscii)

    let private decodeDirection: Decoder<Direction> =
        Decode.string
        |> Decode.andThen (function
            | "Across" -> Decode.succeed Across
            | "Down" -> Decode.succeed Down
            | invalid -> Decode.fail (sprintf " `%s` is an invalid clue direction" invalid))

    let private decodeClue: Decoder<Clue> =
        Decode.object (fun get ->
            { Direction = get.Required.Field "direction" decodeDirection
              Label = get.Required.Field "label" Decode.int
              Hint =
                get.Required.Field
                    "text"
                    (Decode.exactlyOne (Decode.object (fun get -> get.Required.Field "plain" Decode.string))) })

    let private decodeCell: Decoder<char option> =
        Decode.object (fun get -> get.Optional.Field "answer" Decode.string)
        |> Decode.andThen (Option.map Char.Parse >> Decode.succeed)

    let private decoder: Decoder<Game> =
        Decode.object (fun get ->
            { Info =
                { Id = get.Required.Field "id" Decode.int
                  PrintDate = get.Required.Field "publicationDate" Decode.datetimeLocal
                  Editor = get.Required.Field "editor" Decode.string
                  Constructors = get.Required.Field "constructors" (Decode.list Decode.string) }
              Solution =
                get.Required.Field
                    "body"
                    (Decode.exactlyOne (
                        Decode.object (fun get -> get.Required.Field "cells" (Decode.array decodeCell))
                     )
                     |> Decode.map (Array.chunkBySize BoardWidth))
              Clues =
                get.Required.Field
                    "body"
                    (Decode.exactlyOne (Decode.object (fun get -> get.Required.Field "clues" (Decode.list decodeClue))))
              Height =
                get.Required.Field
                    "body"
                    (Decode.exactlyOne (
                        Decode.object (fun get -> get.Required.At [ "dimensions"; "height" ] Decode.int)
                    ))
              Width =
                get.Required.Field
                    "body"
                    (Decode.exactlyOne (
                        Decode.object (fun get -> get.Required.At [ "dimensions"; "width" ] Decode.int)
                    )) })

    let parse = Decode.fromString decoder

    let getGame date = Helpers.failNotImplemented ()

    let getCurrentGame () = getRaw () |> parse
