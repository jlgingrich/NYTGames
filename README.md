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

Each supported game is defined in a module that has these methods. Of these, you probably want `getCurrentGame` or, if the game supports it, `getGame`.

- `getRaw`: Gets a raw unparsed JSON string description of the puzzle from NYT. Usually contains unneccessary data that are ignored when parsed.
- `parse`: Parses the raw JSON string into a structured type that better represents the actual puzzle information and is easier to work with in F#.
- `getCurrentGame`: Gets a description of the puzzle for the system's current date. All games provide this.
- `getGame`: Gets a description of the puzzle on a specific date. Not all games currently provide this due to technical limitations.

## Supported Games

- **Connections**
- **Connections: Sports Edition**
- **Letter Boxed**
  - `getGame` not supported.
- **Spelling Bee**
  - `getGame` not supported.
- **Strands**
- **The Crossword**
  - `getGame` not supported.
- **The Mini**
  - `getGame` not supported.
- **Wordle**

## Unsupported Games

- **Sudoko**
  - Not word-based.
  - Appears to generate puzzles on demand rather than fetch premade puzzles.
- **Tiles**
  - Not word-based.
  - Appears to generate puzzles on demand rather than fetch premade puzzles.

## Future plans

- Figure out how to access archived games for the following:
  - Letter Boxed
  - Spelling Bee
  - The Crossword
  - The Mini
