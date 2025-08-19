namespace NYTGames

open System
open Thoth.Json.Net

type PublicationInformation = {
    Id: int
    PrintDate: DateTime
    EditedBy: string option
    ConstructedBy: string option
}

type Category = { Title: string; Cards: string list }

type StrandsGame = {
    Info: PublicationInformation
    Clue: string
    Spangram: string
    ThemeWords: string list
    Board: string array
}

type ConnectionsGame = {
    Info: PublicationInformation
    Categories: Category list
}

type ConnectionsSportsGame = {
    Info: PublicationInformation
    Categories: Category list
}

type LetterBoxedGame = {
    Info: PublicationInformation
    Sides: string list
    Solution: string list
    Par: int
}

type SpellingBeeGame = {
    Info: PublicationInformation
    CenterLetter: char
    OuterLetters: char list
    Answers: string list
}

type WordleGame = {
    Info: PublicationInformation
    Solution: string
}

type CrosswordDirection =
    | Across
    | Down

type CrosswordClue = {
    Direction: CrosswordDirection
    Label: int
    Hint: string
}

type TheMiniGame = {
    Info: PublicationInformation
    Solution: string option array array
    Clues: CrosswordClue list
    Height: int
    Width: int
}

type TheCrosswordGame = {
    Info: PublicationInformation
    Solution: string option array array
    Clues: CrosswordClue list
    Height: int
    Width: int
}

[<RequireQualifiedAccess>]
type Difficulty =
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

type PipsRegionType =
    | Equals
    | Unequal
    | Sum of Target: int
    | Less of Target: int
    | Greater of Target: int
    | Empty

type PipsRegion = {
    ``type``: PipsRegionType
    Indices: array<int * int>
} with

    static member decoder: Decoder<PipsRegion> =
        Decode.object (fun get ->
            let target = get.Optional.Field "target" Decode.int
            let typeField = get.Required.Field "type" Decode.string

            let typeObject =
                match typeField, target with
                | "equals", None -> Equals
                | "unequal", None -> Unequal
                | "sum", Some t -> Sum t
                | "less", Some t -> Less t
                | "greater", Some t -> Greater t
                | "empty", None -> Empty
                | er, t -> failwithf "Unable to parse region and target: '%s', '%A'" er t

            {
                ``type`` = typeObject
                Indices = get.Required.Field "indices" (Decode.array (Decode.tuple2 Decode.int Decode.int))
            })

type PipsGame = {
    Info: PublicationInformation
    Dominoes: List<int * int>
    Regions: List<PipsRegion>
}
