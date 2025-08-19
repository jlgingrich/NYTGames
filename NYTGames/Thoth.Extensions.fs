namespace Thoth.Json.Net

open Thoth.Json.Net

/// Extensions to <code>Thoth.Json.Net.Decode</code>.
module Decode =
    let ignoreFail (decoder: Decoder<'T>) : Decoder<'T option> =
        fun path token ->
            match decoder path token with
            | Ok x -> Ok(Some x)
            | Error _ -> Ok None

    let keyValueOptions (decoder: Decoder<'a option>) : Decoder<(string * 'a) list> =
        decoder
        |> Decode.keyValuePairs
        |> Decode.map (
            List.collect (fun (key, aOpt) ->
                match aOpt with
                | Some a -> [ key, a ]
                | None -> [])
        )

    let exactlyOne (decoder: Decoder<'t>) : Decoder<'t> =
        Decode.list decoder
        |> Decode.andThen (function
            | [ item ] -> Decode.succeed item
            | items -> Decode.fail $"Expected exactly one list entry, but got {List.length items} entries")

    let intPair: Decoder<int * int> = Decode.tuple2 Decode.int Decode.int
