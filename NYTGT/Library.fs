namespace NYTGT

open System
open FSharp.Data
open Thoth.Json.Net

/// Extensions to <code>Thoth.Json.Net.Decode</code>.
module Decode =
    let exactlyOne (decoder: Decoder<'t>) : Decoder<'t> =
        Decode.list decoder
        |> Decode.andThen (function
            | [ item ] -> Decode.succeed item
            | items -> Decode.fail $"Expected exactly one list entry, but got {List.length items} entries")

/// Extensions to <code>Microsoft.FSharp.Core.Result</code>.
module Result =
    let assertOk =
        function
        | Ok x -> x
        | Error e -> failwithf "Assertion failed: %A" e

module Helpers =
    open System.Text
    let version i = $"v%d{i}"

    [<Literal>]
    let DateFormat = "yyyy-MM-dd"

    let formatDate (date: DateTime) = date.ToString(DateFormat)

    let urlForDate game v (date: DateTime) =
        $"https://www.nytimes.com/svc/%s{game}/%s{version v}/%s{formatDate date}.json"

    let encoding = Encoding.GetEncoding "ISO-8859-1"

    let getRequest url =
        Http.RequestString url
        |> fun r -> encoding.GetBytes r
        |> Encoding.UTF8.GetString

/// Common information that is published with each NYT game
type PublicationInformation = {
    Id: int
    PrintDate: DateTime
    EditedBy: string option
    ConstructedBy: string option
}

// Shared interfaces

type IGame<'t> =
    abstract parse: string -> Result<'t, string>

type ICurrentGame<'t> =
    inherit IGame<'t>
    abstract getCurrentRaw: unit -> string
    abstract getCurrentGame: unit -> Result<'t, string>

type IHistoryGame<'t> =
    inherit IGame<'t>
    abstract getRaw: DateTime -> string
    abstract getGame: DateTime -> Result<'t, string>

[<AutoOpen>]
module Shared =
    let getCurrentGame (game: ICurrentGame<'t>) = game.getCurrentGame ()
    let getGame date (game: IHistoryGame<'t>) = game.getGame date

// API implementations for each game

type StrandsGame = {
    Info: PublicationInformation
    Clue: string
    Spangram: string
    ThemeWords: string list
    Board: string array
}

type Strands() =
    let decoder: Decoder<StrandsGame> =
        Decode.object (fun get -> {
            Info = {
                Id = get.Required.Field "id" Decode.int
                PrintDate = get.Required.Field "printDate" Decode.datetimeLocal
                EditedBy = get.Optional.Field "editor" Decode.string
                ConstructedBy = get.Required.Field "constructors" Decode.string |> Some
            }
            Clue = get.Required.Field "clue" Decode.string
            Spangram = get.Required.Field "spangram" Decode.string
            ThemeWords = get.Required.Field "themeWords" (Decode.list Decode.string)
            Board = get.Required.Field "startingBoard" (Decode.array Decode.string)
        })

    interface IGame<StrandsGame> with
        member this.parse(arg: string) : Result<StrandsGame, string> = Decode.fromString decoder arg

    interface IHistoryGame<StrandsGame> with
        member this.getRaw date =
            Helpers.urlForDate "strands" 2u date |> Helpers.getRequest

        member this.getGame date =
            (this :> IHistoryGame<StrandsGame>).getRaw date
            |> (this :> IGame<StrandsGame>).parse

    interface ICurrentGame<StrandsGame> with
        member this.getCurrentRaw() : string =
            (this :> IHistoryGame<StrandsGame>).getRaw DateTime.Now

        member this.getCurrentGame() : Result<StrandsGame, string> =
            (this :> ICurrentGame<StrandsGame>).getCurrentRaw ()
            |> (this :> IGame<StrandsGame>).parse

type Category = { Title: string; Cards: string list }

type ConnectionsGame = {
    Info: PublicationInformation
    Categories: Category list
}

type Connections() =
    let decodeCategory: Decoder<Category> =
        Decode.object (fun get -> {
            Title = get.Required.Field "title" Decode.string
            Cards =
                get.Required.Field
                    "cards"
                    (Decode.list (
                        Decode.oneOf [
                            Decode.object (fun get -> get.Required.Field "content" Decode.string)
                            Decode.object (fun get -> get.Required.Field "image_alt_text" Decode.string)
                        ]
                    ))
        })

    let decoder: Decoder<ConnectionsGame> =
        Decode.object (fun get -> {
            Info = {
                Id = get.Required.Field "id" Decode.int
                PrintDate = get.Required.Field "print_date" Decode.datetimeLocal
                EditedBy = get.Optional.Field "editor" Decode.string
                ConstructedBy = None
            }
            Categories = get.Required.Field "categories" (Decode.list decodeCategory)
        })

    interface IGame<ConnectionsGame> with
        member this.parse(arg: string) : Result<ConnectionsGame, string> = Decode.fromString decoder arg

    interface IHistoryGame<ConnectionsGame> with
        member this.getRaw date =
            Helpers.urlForDate "connections" 2u date |> Helpers.getRequest

        member this.getGame date =
            (this :> IHistoryGame<ConnectionsGame>).getRaw date
            |> (this :> IGame<ConnectionsGame>).parse

    interface ICurrentGame<ConnectionsGame> with
        member this.getCurrentRaw() : string =
            (this :> IHistoryGame<ConnectionsGame>).getRaw DateTime.Now

        member this.getCurrentGame() : Result<ConnectionsGame, string> =
            (this :> ICurrentGame<ConnectionsGame>).getCurrentRaw ()
            |> (this :> IGame<ConnectionsGame>).parse

type ConnectionsSportsGame = {
    Info: PublicationInformation
    Categories: Category list
}

type ConnectionsSports() =
    let decodeCategory: Decoder<Category> =
        Decode.object (fun get -> {
            Title = get.Required.Field "title" Decode.string
            Cards =
                get.Required.Field
                    "cards"
                    (Decode.list (Decode.object (fun get -> get.Required.Field "content" Decode.string)))
        })

    let decoder: Decoder<ConnectionsSportsGame> =
        Decode.object (fun get -> {
            Info = {
                Id = get.Required.At [ "data"; "getPuzzleById"; "id" ] Decode.int
                PrintDate = get.Required.At [ "data"; "getPuzzleById"; "printDate" ] Decode.datetimeLocal
                EditedBy = get.Optional.At [ "data"; "getPuzzleById"; "editor" ] Decode.string
                ConstructedBy = None
            }
            Categories = get.Required.At [ "data"; "getPuzzleById"; "categories" ] (Decode.list decodeCategory)
        })

    interface IGame<ConnectionsSportsGame> with
        member this.parse(arg: string) : Result<ConnectionsSportsGame, string> = Decode.fromString decoder arg

    interface IHistoryGame<ConnectionsSportsGame> with
        member this.getRaw date =
            let gqlQuery =
                """query GetPuzzleById($puzzleId: String!) {
                                    getPuzzleById(puzzleId: $puzzleId) {
                                        categories {
                                            title
                                            cards {
                                                content
                                                position
                                                img
                                            }
                                        }
                                    printDate
                                    id
                                    editor
                                }
                            }"""

            Http.RequestString(
                "https://api.theathletic.com/graphql",
                headers = [ HttpRequestHeaders.ContentType "application/json; charset=utf-8" ],
                httpMethod = HttpMethod.Post,
                body =
                    HttpRequestBody.TextRequest(
                        sprintf
                            """{
                                    "query":
                                        "%s",
                                    "variables": {
                                        "puzzleId": "%s"
                                    }
                                }"""
                            (gqlQuery.ReplaceLineEndings(@"\n"))
                            (Helpers.formatDate date)
                    )
            )

        member this.getGame date =
            (this :> IHistoryGame<ConnectionsSportsGame>).getRaw date
            |> (this :> IGame<ConnectionsSportsGame>).parse

    interface ICurrentGame<ConnectionsSportsGame> with
        member this.getCurrentRaw() : string =
            (this :> IHistoryGame<ConnectionsSportsGame>).getRaw DateTime.Now

        member this.getCurrentGame() : Result<ConnectionsSportsGame, string> =
            (this :> ICurrentGame<ConnectionsSportsGame>).getCurrentRaw ()
            |> (this :> IGame<ConnectionsSportsGame>).parse

type LetterBoxedGame = {
    Info: PublicationInformation
    Sides: string list
    Solution: string list
    Par: int
}

type LetterBoxed() =
    let decoder: Decoder<LetterBoxedGame> =
        Decode.object (fun get -> {
            Info = {
                Id = get.Required.Field "id" Decode.int
                PrintDate = get.Required.Field "printDate" Decode.datetimeLocal
                EditedBy = get.Optional.Field "editor" Decode.string
                ConstructedBy = None
            }
            Sides = get.Required.Field "sides" (Decode.list Decode.string)
            Solution = get.Required.Field "ourSolution" (Decode.list Decode.string)
            Par = get.Required.Field "par" Decode.int
        })

    interface IGame<LetterBoxedGame> with
        member this.parse(arg: string) : Result<LetterBoxedGame, string> = Decode.fromString decoder arg

    interface ICurrentGame<LetterBoxedGame> with
        member this.getCurrentRaw() : string =
            let scriptPrefix = "window.gameData = "

            HtmlDocument.Load "https://www.nytimes.com/puzzles/letter-boxed"
            |> fun n -> n.CssSelect "script[type=text/javascript]"
            |> List.filter (fun n -> n.DirectInnerText().StartsWith scriptPrefix)
            |> List.exactlyOne
            |> fun n -> n.DirectInnerText()[String.length scriptPrefix ..]

        member this.getCurrentGame() : Result<LetterBoxedGame, string> =
            (this :> ICurrentGame<LetterBoxedGame>).getCurrentRaw ()
            |> (this :> IGame<LetterBoxedGame>).parse

type SpellingBeeGame = {
    Info: PublicationInformation
    CenterLetter: char
    OuterLetters: char list
    Answers: string list
}

type SpellingBee() =
    let decoder: Decoder<SpellingBeeGame> =
        Decode.object (fun get -> {
            Info = {
                Id = get.Required.At [ "today"; "id" ] Decode.int
                PrintDate = get.Required.At [ "today"; "printDate" ] Decode.datetimeLocal
                EditedBy = get.Optional.At [ "today"; "editor" ] Decode.string
                ConstructedBy = None
            }
            CenterLetter = get.Required.At [ "today"; "centerLetter" ] Decode.char
            OuterLetters = get.Required.At [ "today"; "outerLetters" ] (Decode.list Decode.char)
            Answers = get.Required.At [ "today"; "answers" ] (Decode.list Decode.string)
        })

    interface IGame<SpellingBeeGame> with
        member this.parse(arg: string) : Result<SpellingBeeGame, string> = Decode.fromString decoder arg

    interface ICurrentGame<SpellingBeeGame> with
        member this.getCurrentRaw() : string =
            let scriptPrefix = "window.gameData = "

            HtmlDocument.Load "https://www.nytimes.com/puzzles/spelling-bee"
            |> fun n -> n.CssSelect "script[type=text/javascript]"
            |> List.filter (fun n -> n.DirectInnerText().StartsWith scriptPrefix)
            |> List.exactlyOne
            |> fun n -> n.DirectInnerText()[String.length scriptPrefix ..]

        member this.getCurrentGame() : Result<SpellingBeeGame, string> =
            (this :> ICurrentGame<SpellingBeeGame>).getCurrentRaw ()
            |> (this :> IGame<SpellingBeeGame>).parse

type WordleGame = {
    Info: PublicationInformation
    Solution: string
}

type Wordle() =
    let decoder: Decoder<WordleGame> =
        Decode.object (fun get -> {
            Info = {
                Id = get.Required.Field "id" Decode.int
                PrintDate = get.Required.Field "print_date" Decode.datetimeLocal
                EditedBy = get.Optional.Field "editor" Decode.string
                ConstructedBy = None
            }
            Solution = get.Required.Field "solution" Decode.string |> _.ToUpper()
        })

    interface IGame<WordleGame> with
        member this.parse(arg: string) : Result<WordleGame, string> = Decode.fromString decoder arg

    interface IHistoryGame<WordleGame> with
        member this.getRaw date =
            Helpers.urlForDate "wordle" 2u date |> Helpers.getRequest

        member this.getGame date =
            (this :> IHistoryGame<WordleGame>).getRaw date
            |> (this :> IGame<WordleGame>).parse

    interface ICurrentGame<WordleGame> with
        member this.getCurrentRaw() : string =
            (this :> IHistoryGame<WordleGame>).getRaw DateTime.Now

        member this.getCurrentGame() : Result<WordleGame, string> =
            (this :> ICurrentGame<WordleGame>).getCurrentRaw ()
            |> (this :> IGame<WordleGame>).parse

type CrosswordDirection =
    | Across
    | Down

type CrosswordClue = {
    Direction: CrosswordDirection
    Label: int
    Hint: string
}

module CrosswordShared =
    let decodeDirection: Decoder<CrosswordDirection> =
        Decode.string
        |> Decode.andThen (function
            | "Across" -> Decode.succeed Across
            | "Down" -> Decode.succeed Down
            | invalid -> Decode.fail (sprintf " `%s` is an invalid clue direction" invalid))

    let decodeClue: Decoder<CrosswordClue> =
        Decode.object (fun get -> {
            Direction = get.Required.Field "direction" decodeDirection
            Label = get.Required.Field "label" Decode.int
            Hint =
                get.Required.Field
                    "text"
                    (Decode.exactlyOne (Decode.object (fun get -> get.Required.Field "plain" Decode.string)))
        })

    let decodeCell: Decoder<string option> =
        Decode.object (fun get -> get.Optional.Field "answer" Decode.string)

type TheMiniGame = {
    Info: PublicationInformation
    Solution: string option array array
    Clues: CrosswordClue list
    Height: int
    Width: int
}


type TheMini() =
    let decoder: Decoder<TheMiniGame> =
        Decode.object (fun get ->
            let boardHeight =
                get.Required.Field
                    "body"
                    (Decode.exactlyOne (
                        Decode.object (fun get -> get.Required.At [ "dimensions"; "height" ] Decode.int)
                    ))

            let boardWidth =
                get.Required.Field
                    "body"
                    (Decode.exactlyOne (
                        Decode.object (fun get -> get.Required.At [ "dimensions"; "width" ] Decode.int)
                    ))

            {
                Info = {
                    Id = get.Required.Field "id" Decode.int
                    PrintDate = get.Required.Field "publicationDate" Decode.datetimeLocal
                    EditedBy = get.Optional.Field "editor" Decode.string
                    ConstructedBy =
                        get.Required.Field "constructors" (Decode.list Decode.string)
                        |> List.exactlyOne
                        |> Some
                }
                Solution =
                    get.Required.Field
                        "body"
                        (Decode.exactlyOne (
                            Decode.object (fun get ->
                                get.Required.Field "cells" (Decode.array CrosswordShared.decodeCell))
                         )
                         |> Decode.map (Array.chunkBySize boardWidth))
                Clues =
                    get.Required.Field
                        "body"
                        (Decode.exactlyOne (
                            Decode.object (fun get ->
                                get.Required.Field "clues" (Decode.list CrosswordShared.decodeClue))
                        ))
                Height = boardHeight
                Width = boardWidth
            })

    interface IGame<TheMiniGame> with
        member this.parse(arg: string) : Result<TheMiniGame, string> = Decode.fromString decoder arg

    interface ICurrentGame<TheMiniGame> with
        member this.getCurrentRaw() : string =
            "https://www.nytimes.com/svc/crosswords/v6/puzzle/mini.json"
            |> Helpers.getRequest

        member this.getCurrentGame() : Result<TheMiniGame, string> =
            (this :> ICurrentGame<TheMiniGame>).getCurrentRaw ()
            |> (this :> IGame<TheMiniGame>).parse

type TheCrosswordGame = {
    Info: PublicationInformation
    Solution: string option array array
    Clues: CrosswordClue list
    Height: int
    Width: int
}

type TheCrossword() =
    let decoder: Decoder<TheCrosswordGame> =
        Decode.object (fun get ->
            let boardHeight =
                get.Required.Field
                    "body"
                    (Decode.exactlyOne (
                        Decode.object (fun get -> get.Required.At [ "dimensions"; "height" ] Decode.int)
                    ))

            let boardWidth =
                get.Required.Field
                    "body"
                    (Decode.exactlyOne (
                        Decode.object (fun get -> get.Required.At [ "dimensions"; "width" ] Decode.int)
                    ))

            {
                Info = {
                    Id = get.Required.Field "id" Decode.int
                    PrintDate = get.Required.Field "publicationDate" Decode.datetimeLocal
                    EditedBy = get.Optional.Field "editor" Decode.string
                    ConstructedBy =
                        get.Required.Field "constructors" (Decode.list Decode.string)
                        |> List.exactlyOne
                        |> Some
                }
                Solution =
                    get.Required.Field
                        "body"
                        (Decode.exactlyOne (
                            Decode.object (fun get ->
                                get.Required.Field "cells" (Decode.array CrosswordShared.decodeCell))
                         )
                         |> Decode.map (Array.chunkBySize boardWidth))
                Clues =
                    get.Required.Field
                        "body"
                        (Decode.exactlyOne (
                            Decode.object (fun get ->
                                get.Required.Field "clues" (Decode.list CrosswordShared.decodeClue))
                        ))
                Height = boardHeight
                Width = boardWidth
            })

    interface IGame<TheCrosswordGame> with
        member this.parse(arg: string) : Result<TheCrosswordGame, string> = Decode.fromString decoder arg

    interface ICurrentGame<TheCrosswordGame> with
        member this.getCurrentRaw() : string =
            Http.RequestString(
                "https://www.nytimes.com/svc/crosswords/v6/puzzle/daily.json",
                headers = [ "x-games-auth-bypass", "true" ]
            )
            |> String.filter (Char.IsAscii)

        member this.getCurrentGame() : Result<TheCrosswordGame, string> =
            (this :> ICurrentGame<TheCrosswordGame>).getCurrentRaw ()
            |> (this :> IGame<TheCrosswordGame>).parse

[<RequireQualifiedAccess>]
type SudukoDifficulty =
    | Easy
    | Medium
    | Hard

    static member parse s =
        match s with
        | "easy" -> Easy
        | "medium" -> Medium
        | "hard" -> Hard
        | e -> failwithf "Unknown difficulty: '%s'" e

type SudukoGame = {
    Info: PublicationInformation
    Puzzle: (int option) array array
    Solution: int array array
}

type Suduko() =

    let decoder: Decoder<SudukoGame> =
        Decode.object (fun get -> {
            Info = {
                Id = get.Required.Field "puzzle_id" Decode.int
                PrintDate = get.Required.Field "published" Decode.datetimeLocal
                EditedBy = None
                ConstructedBy = None
            }
            Puzzle =
                get.Required.At
                    [ "puzzle_data"; "puzzle" ]
                    (Decode.array (Decode.int |> Decode.map (fun i -> if i = 0 then None else Some i))
                     |> Decode.map (Array.chunkBySize 9))
            Solution =
                get.Required.At
                    [ "puzzle_data"; "solution" ]
                    (Decode.array Decode.int |> Decode.map (Array.chunkBySize 9))
        })

    let decodeData: Decoder<Map<SudukoDifficulty, SudukoGame>> =
        CustomDecoders.keyValueOptions (CustomDecoders.ignoreFail decoder)
        |> Decode.andThen (fun kvs -> kvs |> List.map (fun (k, v) -> (SudukoDifficulty.parse k), v) |> Decode.succeed)
        |> Decode.map Map

    interface IGame<Map<SudukoDifficulty, SudukoGame>> with
        member this.parse(arg: string) : Result<Map<SudukoDifficulty, SudukoGame>, string> =
            Decode.fromString decodeData arg

    interface ICurrentGame<Map<SudukoDifficulty, SudukoGame>> with
        member this.getCurrentRaw() : string =
            let scriptPrefix = "window.gameData = "

            HtmlDocument.Load "https://www.nytimes.com/puzzles/sudoku"
            |> fun n -> n.CssSelect "script[type=text/javascript]"
            |> List.filter (fun n -> n.DirectInnerText().StartsWith scriptPrefix)
            |> List.exactlyOne
            |> fun n -> n.DirectInnerText()[String.length scriptPrefix ..]

        member this.getCurrentGame() : Result<Map<SudukoDifficulty, SudukoGame>, string> =
            (this :> ICurrentGame<Map<SudukoDifficulty, SudukoGame>>).getCurrentRaw ()
            |> (this :> IGame<Map<SudukoDifficulty, SudukoGame>>).parse
