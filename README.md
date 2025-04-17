# NYTGameTools

An F# API for various NYT word games. Comes with a script that generates a Markdown report with the current day's solutions.

## Example

```fsharp
#load "NYTG.fsx"
open NYTG

open System

let printGame (res: Result<Wordle.Game,'a>) =
    let game = res |> Result.assertOk
    printfn
        "Wordle %s\nBy %s\nSolution: '%s'\n"
        (game.Info.PrintDate.ToShortDateString())
        game.Info.Editor
        game.Solution

// Run on 12/31/2024
Wordle.getCurrentGame ()
|> printGame

DateTime.Parse "12/30/2024"
|> Wordle.getGame
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

Each supported game is represented as a type that implements `IGame` and some combination of `ICurrentGame` and `IHistoryGame`.

`IGame` provides access to the `parse` method, which parses the raw JSON representation of the game into a structured F# data type. This is typically not reqired.

`ICurrentGame` provides access to the following methods:

- `getCurrentRaw`: Gets a raw unparsed JSON string description of the current puzzle from NYT. Contains unneccessary data that are ignored when parsed.
- `getCurrentGame`: Gets a description of the puzzle for the current date, as determined by the New York Times website. All games provide this.

`IHistoryGame` provides access to the following methods:

- `getRaw`: Gets a raw unparsed JSON string description of the specified puzzle from NYT. Contains unneccessary data that are ignored when parsed.
- `getGame`: Gets a description of the puzzle on a specific date. Not all games currently provide this due to technical limitations.

## Supported Games

- **Connections**
- **Connections: Sports Edition**
  - Currently broken; they switched from a JSON backend to a GraphQL backend. I will repair this when I get the chance.
- **Letter Boxed**
  - Only `ICurrentGame`
- **Spelling Bee**
  - Only `ICurrentGame`
- **Strands**
- **Sudoko**
  - Only `ICurrentGame`
- **The Crossword**
  - Only `ICurrentGame`
- **The Mini**
  - Only `ICurrentGame`
- **Wordle**

## Unsupported Games

- **Tiles**
  - Not word-based.
  - Appears to generate puzzles on demand rather than fetch premade puzzles.

## Future plans

- Add GraphQL capability for Connections: Sports Edition

- Figure out how to access archived games for the following:
  - Letter Boxed
  - Spelling Bee
  - The Crossword
  - The Mini
  - Sudoku (if it has them)
