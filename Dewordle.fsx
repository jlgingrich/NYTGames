#load "Wordlist.fsx"
open Wordlist

let WORDLIST = Wordlist.getList "wordlist.txt"

[<RequireQualifiedAccess>]
module CharStatus =
    [<Literal>]
    let NotInWord = '_'

    [<Literal>]
    let WrongPosition = '?'

    [<Literal>]
    let RightPosition = '!'

type WordleState =
    { KnownPositions: Map<uint, char>
      KnownExclusions: Map<uint, Set<char>>
      MinCounts: Map<char, uint>
      MaxCounts: Map<char, uint> }

module WordleState =
    let empty =
        { KnownPositions = Map.ofList []
          KnownExclusions = Map.ofList []
          MinCounts = Map.ofList []
          MaxCounts = Map.ofList [] }

// TESTING

let feedbackIs pred seq =
    Seq.filter (fun (_, _, f) -> pred f) seq

let mapMerge (m1: Map<'k, 'v>) (m2: Map<'k, 'v>) : Map<'k, 'v> = Map.foldBack Map.add m1 m2

let learn (word: string, feedback: string) (state: WordleState) : WordleState =
    // Index, word char, feedback char
    let digest = Seq.zip3 [ 0u .. 5u ] word feedback

    let knownPositions =
        digest
        |> feedbackIs ((=) CharStatus.RightPosition)
        |> Seq.map (fun (i, c, _) -> [ i, c ])
        |> Seq.concat
        |> Map

    let knownExclusions =
        digest
        |> feedbackIs ((=) CharStatus.WrongPosition)
        |> Seq.map (fun (i, c, _) -> [ i, set [ c ] ])
        |> Seq.concat
        |> Map

    let counts = digest |> Seq.countBy (fun (i, c, f) -> c) |> Map

    let maskedCounts =
        digest
        |> feedbackIs ((<>) CharStatus.NotInWord)
        |> Seq.countBy (fun (i, c, f) -> c)
        |> Map

    let minCounts =
        word
        |> Seq.distinct
        |> Seq.map (fun c ->
            let totalCount = counts[c] // Never fails
            let maskedCount = defaultArg (maskedCounts.TryFind c) 0

            if totalCount > maskedCount then
                c, uint maskedCount
            else
                c, uint totalCount)
        |> Map

    let maxCounts =
        word
        |> Seq.distinct
        |> Seq.map (fun c ->
            let totalCount = counts[c] // Never fails
            let maskedCount = defaultArg (maskedCounts.TryFind c) 0

            if totalCount > maskedCount then
                seq [ c, uint maskedCount ]
            else
                seq [])
        |> Seq.concat
        |> Map

    { state with
        KnownPositions = mapMerge state.KnownPositions knownPositions
        KnownExclusions = mapMerge state.KnownExclusions knownExclusions
        MinCounts = mapMerge state.MinCounts minCounts
        MaxCounts = mapMerge state.MaxCounts maxCounts }

let matchState state (word: string) =
    let passesKnownPositions =
        state.KnownPositions |> Map.forall (fun k v -> word[int k] = v)

    if not passesKnownPositions then
        false
    else
        let passesKnownExclusions =
            state.KnownExclusions
            |> Map.forall (fun k v -> Set.contains word[int k] v |> not)

        if not passesKnownExclusions then
            false
        else
            let passesMinCounts =
                word
                |> Seq.countBy id // Get char frequencies
                |> Seq.forall (fun (c, count) ->
                    let countMin = (defaultArg (state.MinCounts.TryFind c) 0u)
                    uint count >= countMin)

            if not passesMinCounts then
                false
            else
                word
                |> Seq.countBy id // Get char frequencies
                |> Seq.forall (fun (c, count) ->
                    let countMax = (defaultArg (state.MaxCounts.TryFind c) (uint System.Int32.MaxValue))
                    uint count <= countMax)

let search state =
    WORDLIST
    |> Seq.filter (matchState state)
    |> Seq.sortByDescending (fun chars -> Seq.distinct chars |> Seq.length)
    |> Seq.toList

let wordleSearch input = 
    Seq.foldBack learn input WordleState.empty
    |> search
