namespace NYTGames

open System
open FSharp.Data
open Thoth.Json.Net

/// Extensions to <code>Microsoft.FSharp.Core.Result</code>.
module Result =
    let assertOk =
        function
        | Ok x -> x
        | Error e -> failwithf "Assertion failed: %A" e

/// Convenience methods used in multiple game implementations
module Helpers =
    open System.Text
    let version i = $"v%d{i}"

    [<Literal>]
    let DateFormat = "yyyy-MM-dd"

    let formatDate (date: DateTime) = date.ToString(DateFormat)

    let urlForDate game v date =
        $"https://www.nytimes.com/svc/%s{game}/%s{version v}/%s{formatDate date}.json"

    let encoding = Encoding.GetEncoding "ISO-8859-1"

    let getRequest url =
        async {
            let! response = Http.AsyncRequestString url
            return encoding.GetBytes response |> Encoding.UTF8.GetString
        }

module Async =
    let map next a =
        async {
            let! res = a
            return next res
        }

module Game =

    module Strands =
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

        let parse arg = Decode.fromString decoder arg

        let getRaw date =
            Helpers.urlForDate "strands" 2u date |> Helpers.getRequest

        let getGame date = getRaw date |> Async.map parse

        let getCurrentRaw () = getRaw DateTime.Now

        let getCurrentGame () = getCurrentRaw () |> Async.map parse

    module Connections =

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

        let parse arg = Decode.fromString decoder arg

        let getRaw date =
            Helpers.urlForDate "connections" 2u date |> Helpers.getRequest

        let getGame date = getRaw date |> Async.map parse

        let getCurrentRaw () = getRaw DateTime.Now

        let getCurrentGame () = getCurrentRaw () |> Async.map parse

    module ConnectionsSports =
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

        let parse arg = Decode.fromString decoder arg

        let getRaw date =
            let gqlQuery =
                """
                query GetPuzzleById($puzzleId: String!) {
                    getPuzzleById(puzzleId: $puzzleId) {
                    categories {
                        title
                        cards {
                        content
                        position
                        img
                        }
                    }
                    printDate: print_date
                    id
                    hint_url
                    editor
                    difficulty
                    }
                }"""

            Http.AsyncRequestString(
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

        let getGame date = getRaw date |> Async.map parse

        let getCurrentRaw () = getRaw DateTime.Now

        let getCurrentGame () = getCurrentRaw () |> Async.map parse

    module LetterBoxed =
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

        let parse arg = Decode.fromString decoder arg

        let getCurrentRaw () =
            let scriptPrefix = "window.gameData = "

            async {
                let! res = HtmlDocument.AsyncLoad "https://www.nytimes.com/puzzles/letter-boxed"

                return
                    res.CssSelect "script[type=text/javascript]"
                    |> List.filter (fun n -> n.DirectInnerText().StartsWith scriptPrefix)
                    |> List.exactlyOne
                    |> fun n -> n.DirectInnerText()[String.length scriptPrefix ..]
            }

        let getCurrentGame () = getCurrentRaw () |> Async.map parse

    module SpellingBee =
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

        let parse arg = Decode.fromString decoder arg

        let getCurrentRaw () =
            let scriptPrefix = "window.gameData = "

            async {
                let! res = HtmlDocument.AsyncLoad "https://www.nytimes.com/puzzles/spelling-bee"

                return
                    res.CssSelect "script[type=text/javascript]"
                    |> List.filter (fun n -> n.DirectInnerText().StartsWith scriptPrefix)
                    |> List.exactlyOne
                    |> fun n -> n.DirectInnerText()[String.length scriptPrefix ..]
            }

        let getCurrentGame () = getCurrentRaw () |> Async.map parse


    module Wordle =
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

        let parse arg = Decode.fromString decoder arg

        let getRaw date =
            Helpers.urlForDate "wordle" 2u date |> Helpers.getRequest

        let getGame date = getRaw date |> Async.map parse

        let getCurrentRaw () = getRaw DateTime.Now

        let getCurrentGame () = getCurrentRaw () |> Async.map parse

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

    module TheMini =
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

        let parse arg = Decode.fromString decoder arg

        let getCurrentRaw () =
            "https://www.nytimes.com/svc/crosswords/v6/puzzle/mini.json"
            |> Helpers.getRequest

        let getCurrentGame () = getCurrentRaw () |> Async.map parse

    module Pips =
        let decoder: Decoder<PipsGame> =
            Decode.object (fun get -> {
                Info = {
                    Id = get.Required.Field "id" Decode.int
                    PrintDate = System.DateTime.MinValue // Overwritten by shared data
                    EditedBy = None // Overwritten by shared data
                    ConstructedBy = get.Required.Field "constructors" Decode.string |> Some
                }
                Dominoes = get.Required.Field "dominoes" (Decode.list Decode.intPair)
                Regions = get.Required.Field "regions" (Decode.list PipsRegion.decoder)
            })

        let decodeData: Decoder<_> =
            Decode.keyValueOptions (Decode.ignoreFail decoder)
            |> Decode.andThen (fun kvs -> kvs |> List.map (fun (k, v) -> Difficulty.parse k, v) |> Decode.succeed)
            |> Decode.map Map

        let decoderSharedInfo: Decoder<PublicationInformation> =
            Decode.object (fun get -> {
                Id = 0
                PrintDate = get.Required.Field "printDate" Decode.datetimeLocal
                EditedBy = get.Required.Field "editor" Decode.string |> Some
                ConstructedBy = None
            })

        let parse arg =
            let info = Decode.fromString decodeData arg
            let sharedInfo = Decode.fromString decoderSharedInfo arg |> Result.assertOk

            info
            |> Result.map (
                Map.map (fun _ value -> {
                    value with
                        Info.EditedBy = sharedInfo.EditedBy
                        Info.PrintDate = sharedInfo.PrintDate
                })
            )

        let getRaw date =
            Helpers.urlForDate "pips" 1u date |> Helpers.getRequest

        let getGame date = getRaw date |> Async.map parse

        let getCurrentRaw () = getRaw DateTime.Now

        let getCurrentGame () = getCurrentRaw () |> Async.map parse

    module TheCrossword =
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

        let parse arg = Decode.fromString decoder arg

        let getCurrentRaw () =
            async {
                let! res =
                    Http.AsyncRequestString(
                        "https://www.nytimes.com/svc/crosswords/v6/puzzle/daily.json",
                        headers = [ "x-games-auth-bypass", "true" ]
                    )

                return String.filter Char.IsAscii res
            }

        let getCurrentGame () = getCurrentRaw () |> Async.map parse

    module Suduko =

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

        let decodeData: Decoder<Map<Difficulty, SudukoGame>> =
            Decode.keyValueOptions (Decode.ignoreFail decoder)
            |> Decode.andThen (fun kvs -> kvs |> List.map (fun (k, v) -> Difficulty.parse k, v) |> Decode.succeed)
            |> Decode.map Map

        let parse arg = Decode.fromString decodeData arg

        let getCurrentRaw () =
            let scriptPrefix = "window.gameData = "

            async {
                let! res = HtmlDocument.AsyncLoad("https://www.nytimes.com/puzzles/sudoku")

                return
                    res.CssSelect "script[type=text/javascript]"
                    |> List.filter (fun n -> n.DirectInnerText().StartsWith scriptPrefix)
                    |> List.exactlyOne
                    |> fun n -> n.DirectInnerText()[String.length scriptPrefix ..]
            }

        let getCurrentGame () = getCurrentRaw () |> Async.map parse
