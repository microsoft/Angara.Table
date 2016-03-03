module internal Util 

let coerce<'a,'b> (o:'a) : 'b = o :> obj :?> 'b

let coerceSome<'a,'b> (o:'a) : 'b option = o :> obj :?> 'b |> Option.Some

let internal unpackOrFail<'a> (message:string) (opt:'a option) : 'a =
    match opt with
    | Some o -> o
    | None -> failwith message

let invalidCast message = raise (new System.InvalidCastException(message))

