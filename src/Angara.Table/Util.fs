module internal Util 

open System.Collections.Immutable

let inline coerce<'a,'b> (o:'a) : 'b = o :> obj :?> 'b

let inline coerceSome<'a,'b> (o:'a) : 'b option = o :> obj :?> 'b |> Option.Some

let inline unpackOrFail<'a> (message:string) (opt:'a option) : 'a =
    match opt with
    | Some o -> o
    | None -> failwith message

/// Returns an array which has all elements of `arr` that correspond to `true` of `mask`.
/// Requires that lengths of `mask` and `arr` are equal.
let select (mask:bool[]) (arr: ImmutableArray<'a>) : ImmutableArray<'a> =
    let m = mask.Length
    let mutable n = 0
    for i = 0 to m-1 do if mask.[i] then n <- n+1
    let bld = ImmutableArray.CreateBuilder(n);
    bld.Count <- n
    let mutable j = 0
    for i = 0 to m-1 do 
        if mask.[i] then bld.[j] <- arr.[i]; j <- j + 1
    bld.MoveToImmutable()

let inline invalidCast message = raise (new System.InvalidCastException(message))

let inline notFound message = raise (new System.Collections.Generic.KeyNotFoundException(message))

let getTypedRowProperties (typeR : System.Type) : (string*System.Type)[] =    
    typeR.GetProperties() // check: sealed & record
    |> Seq.choose(fun p -> 
        let cm_attrs = p.GetCustomAttributes(typeof<CompilationMappingAttribute>, false)
        if cm_attrs.Length >= 1 then
            Some((p.Name, p.PropertyType), (cm_attrs.[0] :?> CompilationMappingAttribute).SequenceNumber)
        else None)
    |> Seq.sortBy snd
    |> Seq.map fst
    |> Seq.toArray