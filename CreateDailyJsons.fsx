// Uses NYTGames to download the solutions to all of today's games as JSON files

#r "NYTGames/bin/Debug/net9.0/NYTGames.dll"
#r "nuget: Thoth.Json.Net"
#r "nuget: FSharp.Data"
#r "nuget: ObjectDumper.Net"

open NYTGames

open System
open System.IO
open Thoth.Json.Net
open Newtonsoft.Json

let dateStamp = DateTime.Now |> Helpers.formatDate

/// Formats a json string with an indent level of 4
/// <seealso cref="https://www.newtonsoft.com/json/help/html/T_Newtonsoft_Json_JsonTextWriter.htm" />
let formatJson json =
    use sr = new StringReader(json)
    use jr = new JsonTextReader(sr)
    use sw = new StringWriter()

    // This is where Newtonsoft.Json exposes the most formatting settings
    use jw = new JsonTextWriter(sw, Formatting = Formatting.Indented, Indentation = 4)

    jw.WriteToken(jr)
    sw.ToString()

let dumpResult filename object =
    async {
        let! res = object
        let json = res |> Result.assertOk |> Encode.Auto.toString |> formatJson

        return!
            File.WriteAllTextAsync($"Raw/%s{dateStamp}.%s{filename}.json", json)
            |> Async.AwaitTask
    }

// Exports
[
    Game.Wordle.getCurrentGame () |> dumpResult "wordle"
    Game.Connections.getCurrentGame () |> dumpResult "connections"
    Game.ConnectionsSports.getCurrentGame () |> dumpResult "connections-sports"
    Game.Strands.getCurrentGame () |> dumpResult "strands"
    Game.LetterBoxed.getCurrentGame () |> dumpResult "letter-boxed"
    Game.TheMini.getCurrentGame () |> dumpResult "the-mini"
    Game.TheCrossword.getCurrentGame () |> dumpResult "the-crossword"
    Game.SpellingBee.getCurrentGame () |> dumpResult "spelling-bee"
    Game.Suduko.getCurrentGame () |> dumpResult "suduko"
    Game.Pips.getCurrentGame () |> dumpResult "pips"

]
|> Async.Parallel
|> Async.RunSynchronously
|> ignore
