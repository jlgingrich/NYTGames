#r "NYTGT/bin/Debug/net8.0/NYTGT.dll"
#r "nuget: Thoth.Json.Net"
#r "nuget: FSharp.Data"

open NYTGT

open System
open System.Text

let s = StringBuilder ""

let toStringBuilder (t: string) = s.Append t |> _.AppendLine() |> ignore

let today = DateTime.Now

let dateStamp = today |> Helpers.formatDate
let dateName = today.ToString "MMMM d, yyyy"

$"# Word Games - %s{dateName}" |> toStringBuilder

try
    Wordle()
    |> getCurrentGame
    |> Result.assertOk
    |> fun game ->
        let editor = defaultArg game.Info.EditedBy "Nobody?"

        $"\n## Wordle\n\n**By:** %s{editor}\n\n**Solution:** `%s{game.Solution}`"
        |> toStringBuilder
with :? Net.WebException ->
    printfn "Unable to access Wordle"

try
    Connections()
    |> getCurrentGame
    |> Result.assertOk
    |> fun game ->
        let editor = defaultArg game.Info.EditedBy "Nobody?"

        $"\n## Connections\n\n**By:** %s{editor}\n\n**Categories:**\n"
        |> toStringBuilder

        game.Categories
        |> List.iteri (fun i c ->
            $"%d{i + 1}. **%s{c.Title}**" |> toStringBuilder
            c.Cards |> List.iter (fun card -> $"    - `%s{card}`" |> toStringBuilder))
with :? Net.WebException ->
    printfn "Unable to access Connections"

try
    ConnectionsSports()
    |> getCurrentGame
    |> Result.assertOk
    |> fun game ->
        let editor = defaultArg game.Info.EditedBy "Nobody?"

        $"\n## Connections: Sports Edition\n\n**By:** %s{editor}\n\n**Categories:**\n"
        |> toStringBuilder

        game.Categories
        |> List.iteri (fun i c ->
            $"%d{i + 1}. **%s{c.Title}**" |> toStringBuilder
            c.Cards |> List.iter (fun card -> $"    - `%s{card}`" |> toStringBuilder))
with :? Net.WebException ->
    printfn "Unable to access Connections: Sports Edition"

try
    Strands()
    |> getCurrentGame
    |> Result.assertOk
    |> fun game ->
        let editor = defaultArg game.Info.EditedBy "Nobody?"
        $"\n## Strands\n\n**By:** %s{editor}\n" |> toStringBuilder
        $"**Spangram:** `%s{game.Spangram}`\n\n**Theme words:**\n" |> toStringBuilder
        game.ThemeWords |> List.iter (fun word -> $"- `%s{word}`" |> toStringBuilder)
with :? Net.WebException ->
    printfn "Unable to access Spangram"

try
    LetterBoxed()
    |> getCurrentGame
    |> Result.assertOk
    |> fun game ->
        let solution =
            game.Solution |> List.map (fun s -> $"`%s{s}`") |> String.concat " - "

        let editor = defaultArg game.Info.EditedBy "Nobody?"

        $"\n## Letter Boxed\n\n**By:** %s{editor}\n\n**Solution:** %s{solution}"
        |> toStringBuilder
with :? Net.WebException ->
    printfn "Unable to access Letter Boxed"

try
    TheMini()
    |> getCurrentGame
    |> Result.assertOk
    |> fun game ->
        "\n## The Mini\n" |> toStringBuilder

        match game.Info.EditedBy with
        | Some s -> $"**Edited by:** %s{s}\n" |> toStringBuilder
        | None -> ()

        match game.Info.ConstructedBy with
        | Some s -> $"**Constructed by:** %s{s}\n" |> toStringBuilder
        | None -> ()

        $"**Solution:**\n" |> toStringBuilder

        game.Solution
        |> Array.map (fun row ->
            row
            |> Array.map (function
                | Some c -> c
                | None -> " ")
            |> String.concat " ")
        |> String.concat "\n"
        |> fun s -> toStringBuilder $"```text\n%s{s}\n```"
with :? Net.WebException ->
    printfn "Unable to access The Mini"

try
    TheCrossword()
    |> getCurrentGame
    |> Result.assertOk
    |> fun game ->
        "\n## The Crossword\n" |> toStringBuilder

        match game.Info.EditedBy with
        | Some s -> $"**Edited by:** %s{s}\n" |> toStringBuilder
        | None -> ()

        match game.Info.ConstructedBy with
        | Some s -> $"**Constructed by:** %s{s}\n" |> toStringBuilder
        | None -> ()

        $"**Solution:**\n" |> toStringBuilder

        game.Solution
        |> Array.map (fun row ->
            row
            |> Array.map (function
                | Some c -> c
                | None -> " ")
            |> String.concat " ")
        |> String.concat "\n"
        |> fun s -> toStringBuilder $"```text\n%s{s}\n```"
with :? Net.WebException ->
    printfn "Unable to access The Crossword"

try
    SpellingBee()
    |> getCurrentGame
    |> Result.assertOk
    |> fun game ->
        let editor = defaultArg game.Info.EditedBy "Nobody?"
        $"\n## Spelling Bee\n\n**By:** %s{editor}\n\n**Solution:**\n" |> toStringBuilder

        game.Answers
        |> List.map (fun word -> $"- `%s{word.ToUpper()}`")
        |> List.sortBy (fun w -> -w.Length)
        |> String.concat "\n"
        |> toStringBuilder
with :? Net.WebException ->
    printfn "Unable to access Spelling Bee"

try
    Suduko()
    |> getCurrentGame
    |> Result.assertOk
    |> fun game ->
        $"\n## Sudoku" |> toStringBuilder

        for KeyValue(difficulty, puzzle) in game do

            $"\n### {difficulty.ToString()}\n" |> toStringBuilder

            puzzle.Solution
            |> Array.map (fun row ->
                row
                |> Array.map string
                |> Array.chunkBySize 3
                |> Array.map (String.concat " ")
                |> String.concat "   ")
            |> Array.chunkBySize 3
            |> Array.map (String.concat "\n")
            |> String.concat "\n\n"
            |> fun s -> toStringBuilder $"```text\n%s{s}\n```"
with :? Net.WebException ->
    printfn "Unable to access Suduko"

File.write $"Reports/%s{dateStamp}.nytgames.md" (s.ToString())
