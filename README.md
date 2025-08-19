# NYTGames

An F# API for various NYT word games. Comes with a script that generates a Markdown report with the current day's solutions.

## Example

[Sample.fsx](Sample.fsx)

```fsharp
#r @"NYTGames\bin\Debug\net9.0\NYTGames.dll"

open NYTGames

open System

let printGame (res: Result<WordleGame, 'a>) =
    let game = res |> Result.assertOk

    printfn
        "Wordle %s\nBy %s\nSolution: '%s'\n"
        (game.Info.PrintDate.ToShortDateString())
        (game.Info.EditedBy |> Option.get)
        game.Solution

// Run on 12/31/2024
Game.Wordle.getCurrentGame () |> Async.RunSynchronously |> printGame

DateTime.Parse "12/30/2024"
|> Game.Wordle.getGame
|> Async.RunSynchronously
|> printGame
```

```text
Wordle 12/31/2024
By Tracy Bennett
Solution: 'lemur'

Wordle 12/30/2024
By Tracy Bennett
Solution: 'stare'
```

## How to use

Each supported game is represented as a module under `Game`.

All games provide these methods:

- `parse`: Transforms raw JSON into a strongly-typed F# object.
- `getCurrentRaw`: Gets a raw unparsed JSON string description of the current puzzle from NYT. Contains unneccessary data that are ignored when parsed.
- `getCurrentGame`: Gets a description of the puzzle for the current date, as determined by the New York Times website.

Only certain games provide these methods, depending on whether the API for that game provides access to archived games:

- `getRaw`: Gets a raw unparsed JSON string description of the specified puzzle from NYT. Contains unneccessary data that are ignored when parsed.
- `getGame`: Gets a description of the puzzle on a specific date.

## Supported Games

- **Connections**
- **Connections: Sports Edition**
- **Letter Boxed**
  - Only current
- **Pips**
- **Spelling Bee**
  - Only current
- **Strands**
- **Sudoko**
  - Only current
- **The Crossword**
  - Only current
- **The Mini**
  - Only current
- **Wordle**

## Unsupported Games

- **Tiles**
  - Not word-based.
  - Appears to generate puzzles on demand rather than fetch premade puzzles.

## Future plans

- Add Pips solution to report script
- Figure out how to access archived games for the following:
  - Letter Boxed
  - Spelling Bee
  - The Crossword
  - The Mini
  - Sudoku (if it has them)
