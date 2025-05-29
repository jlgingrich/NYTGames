namespace NYTGames

open System

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
