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
