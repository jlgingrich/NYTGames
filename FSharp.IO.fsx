open System.IO

module File =
    /// Writes text to a filepath, creating any intermediate directories and overwriting an existing file
    let write (path: string) contents =
        let dir = Path.GetDirectoryName path

        if not (Directory.Exists dir) && dir <> "" then
            Directory.CreateDirectory dir |> ignore

        File.WriteAllText(path, contents)

    let read path () = File.ReadAllText path
    let exists path () = File.Exists path
