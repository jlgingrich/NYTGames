#r "NYTGT/bin/Debug/net8.0/NYTGT.dll"
#r "nuget: Thoth.Json.Net"
#r "nuget: FSharp.Data"

open NYTGT

open System
open System.Text

let s = StringBuilder ""

let toStringBuilder (t: string) = s.Append(t) |> _.AppendLine() |> ignore

let today = DateTime.Now

let dateStamp = today |> Helpers.formatDate
let dateName = today.ToString("MMMM d, yyyy")

$"# Word Games - %s{dateName}" |> toStringBuilder

Wordle.getCurrentGame ()
|> Result.assertOk
|> fun game ->
    let editor = defaultArg game.Info.Editor "Nobody?"

    $"\n## Wordle\n\n**By:** %s{editor}\n\n**Solution:** `%s{game.Solution}`"
    |> toStringBuilder

Connections.getCurrentGame ()
|> Result.assertOk
|> fun game ->
    let editor = defaultArg game.Info.Editor "Nobody?"

    $"\n## Connections\n\n**By:** %s{editor}\n\n**Categories:**\n"
    |> toStringBuilder

    game.Categories
    |> List.iteri (fun i c ->
        $"%d{i + 1}. **%s{c.Title}**" |> toStringBuilder
        c.Cards |> List.iter (fun card -> $"    - `%s{card}`" |> toStringBuilder))

ConnectionsSportsEdition.getCurrentGame ()
|> Result.assertOk
|> fun game ->
    let editor = defaultArg game.Info.Editor "Nobody?"

    $"\n## Connections: Sports Edition\n\n**By:** %s{editor}\n\n**Categories:**\n"
    |> toStringBuilder

    game.Categories
    |> List.iteri (fun i c ->
        $"%d{i + 1}. **%s{c.Title}**" |> toStringBuilder
        c.Cards |> List.iter (fun card -> $"    - `%s{card}`" |> toStringBuilder))

Strands.getCurrentGame ()
|> Result.assertOk
|> fun game ->
    let editor = defaultArg game.Info.Editor "Nobody?"
    $"\n## Strands\n\n**By:** %s{editor}\n" |> toStringBuilder
    $"**Spangram:** `%s{game.Spangram}`\n\n**Theme words:**\n" |> toStringBuilder
    game.ThemeWords |> List.iter (fun word -> $"- `%s{word}`" |> toStringBuilder)

LetterBoxed.getCurrentGame ()
|> Result.assertOk
|> fun game ->
    let solution =
        game.Solution |> List.map (fun s -> $"`%s{s}`") |> String.concat " - "

    let editor = defaultArg game.Info.Editor "Nobody?"

    $"\n## Letter Boxed\n\n**By:** %s{editor}\n\n**Solution:** %s{solution}"
    |> toStringBuilder

Mini.getCurrentGame ()
|> Result.assertOk
|> fun game ->
    let editor = defaultArg game.Info.Editor "Nobody?"
    $"\n## The Mini\n\n**By:** %s{editor}\n" |> toStringBuilder

    if not (List.isEmpty game.Info.Constructors) then
        "**Constructed by:**\n" |> toStringBuilder

        game.Info.Constructors
        |> List.iter (fun word -> $"- %s{word}" |> toStringBuilder)

    $"\n**Solution:**\n" |> toStringBuilder

    game.Solution
    |> Array.map (fun row ->
        row
        |> Array.map (function
            | Some c -> Char.ToString c
            | None -> " ")
        |> String.concat " ")
    |> String.concat "\n"
    |> fun s -> toStringBuilder $"```text\n%s{s}\n```"

Crossword.getCurrentGame ()
|> Result.assertOk
|> fun game ->
    let editor = defaultArg game.Info.Editor "Nobody?"
    $"\n## The Crossword\n\n**By:** %s{editor}\n" |> toStringBuilder

    if not (List.isEmpty game.Info.Constructors) then
        "**Constructed by:**\n" |> toStringBuilder

        game.Info.Constructors
        |> List.iter (fun word -> $"- %s{word}" |> toStringBuilder)

    $"\n**Solution:**\n" |> toStringBuilder

    game.Solution
    |> Array.map (fun row ->
        row
        |> Array.map (function
            | Some c -> c
            | None -> " ")
        |> String.concat " ")
    |> String.concat "\n"
    |> fun s -> toStringBuilder $"```text\n%s{s}\n```"

SpellingBee.getCurrentGame ()
|> Result.assertOk
|> fun game ->
    let editor = defaultArg game.Info.Editor "Nobody?"
    $"\n## Spelling Bee\n\n**By:** %s{editor}\n\n**Solution:**\n" |> toStringBuilder

    game.Answers
    |> List.map (fun word -> $"- `%s{word.ToUpper()}`")
    |> List.sortBy (fun w -> -w.Length)
    |> String.concat "\n"
    |> toStringBuilder

File.write $"Reports/nytgames.%s{dateStamp}.md" (s.ToString())
