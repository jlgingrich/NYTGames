#r "NYTGT/bin/Debug/net8.0/NYTGT.dll"
#r "nuget: Thoth.Json.Net"
#r "nuget: FSharp.Data"

open NYTGT
open Newtonsoft.Json

open System
open System.IO
open Microsoft.FSharp.Quotations

let today = DateTime.Now
let dateStamp = today |> Helpers.formatDate


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

let dumpJson filename raw =
    File.write $"Raw/%s{dateStamp}.%s{filename}.json" (formatJson raw)

// Exports

let inline dumpGame (game: ICurrentGame<'t>) =
    game.getCurrentRaw () |> dumpJson (game.GetType().Name)

[
    async { do dumpGame <| Wordle() }
    async { do dumpGame <| Connections() }
    async { do dumpGame <| ConnectionsSports() }
    async { do dumpGame <| Strands() }
    async { do dumpGame <| LetterBoxed() }
    async { do dumpGame <| TheMini() }
    async { do dumpGame <| TheCrossword() }
    async { do dumpGame <| SpellingBee() }
    async { do dumpGame <| Suduko() }
]
|> Async.Parallel
|> Async.RunSynchronously
