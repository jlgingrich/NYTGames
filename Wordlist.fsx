#r "nuget: FSharp.Data"
open FSharp.Data
open System.IO
open System.Text.RegularExpressions

[<Literal>] 
let private WORD_REG = @"""(\w{5})"""
let Word = Regex(WORD_REG)

let private nodeText (node : HtmlNode) = node.DirectInnerText()

let fetchList path =
    let words =
        HtmlDocument
            .Load("https://wordlearchive.com")
            .CssSelect "body > script"
        |> List.filter (fun node ->
            let s = nodeText node
            s.TrimStart().StartsWith "var Wd"
        )
        |> List.exactlyOne
        |> nodeText
        |> Word.Matches
        |> Seq.map _.Groups[1].Value.ToUpper()
        |> Seq.distinct
        |> Seq.sort

    File.WriteAllLines(path, words)
    words

let readList path =
    File.ReadAllLines path |> Array.toSeq

let getList path =
    if File.Exists path then
        printfn "Reading wordlist from local cache"
        readList path
    else
        printfn "Fetching wordlist from online source"
        fetchList path
