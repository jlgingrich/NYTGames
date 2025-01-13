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

Each supported game is defined in a module that has these methods. Of these, you probably want `getGame` (if the game supports it) or `getCurrentGame`.

- `getRaw`: Gets a raw unparsed JSON description of the puzzle from NYT. Usually contains unneccessary data that are ignored when parsed.
- `parse`: Parses the raw JSON from NYT into a structured type that better represents the actual puzzle information and is easier to work with in F#.
- `getCurrentGame`: Gets a description of the puzzle for the system's current date.
- `getGame`: Gets a description of the puzzle on a specific date. Not all games support this currently due to technical limitations.

## Supported Games

- **Connections**
- **Connections: Sports Edition**
- **Letter Boxed**
  - Historical puzzles not supported.
- **The Mini**
  - Historical puzzles not supported.
- **Strands**
- **Wordle**
- **Spelling Bee**
  - Historical puzzles not supported.
- **The Crossword**
  - Historical puzzles not supported.

## Unsupported Games

- **Sudoko**
  - Doesn't have daily games and generates puzzles locally rather than fetching curated puzzles.
  - Use a :abacus: Sudoku solver :abacus:.
- **Tiles**
  - Not word based.
  - Might make a separate project around this though; HTML parsing involved and it would be an interesting optimization challenge.
