#load "Dewordle.fsx"
open Dewordle

wordleSearch [
    "BLOCK", "_____"
    "ADMIN", "_____"
    "ERUPT", "?___?"
] |> printfn "%A"
