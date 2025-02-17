namespace NYTGT

open Thoth.Json.Net

module CustomDecoders =
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
