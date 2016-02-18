module internal Funcs

open System.Linq.Expressions
open Microsoft.FSharp.Reflection


let fsFuncParameters (f:'a->'b) = 
    let ftype = f.GetType()
    let args = ftype |> Seq.unfold(fun t -> 
        match FSharpType.IsFunction t with
        | true ->
            let arg, res = FSharpType.GetFunctionElements t
            Some(arg, res)
        | false -> 
            None) 
    args |> Seq.toArray

let fsFuncRes (f:'a->'b) =
    let ftype = f.GetType()
    let args = ftype |> Seq.unfold(fun t -> 
        match FSharpType.IsFunction t with
        | true ->
            let _,res = FSharpType.GetFunctionElements t
            Some(res, res)
        | false -> 
            None) 
    args

/// Creates a delegate from a curried function.
let toDelegate (f:'a->'b) =
    let args = fsFuncParameters f 
    let pars = args |> Array.map(fun a -> Expression.Parameter a) 
    let calls = pars |> Array.fold(fun (expr:Expression) p -> upcast Expression.Call(expr, "Invoke", [||], [|p :> Expression|])) (Expression.Constant f :> Expression)
    let lambda = Expression.Lambda(calls, pars)
    lambda.Compile()

/// Creates a delegate from a curried function, but each parameter of the delegate is a 1d-array with a single element to be passed to the original function.
let toDelegate_UnwrapArrays (f:'a->'b) =
    let args = fsFuncParameters f
    let pars = args |> Array.map(fun a -> 
        let arrType = System.Array.CreateInstance(a, 0).GetType()
        Expression.Parameter arrType) 
    let calls = pars |> Array.fold(fun (expr:Expression) p -> 
        let arg = Expression.ArrayIndex(p :> Expression, Expression.Constant 0) :> Expression
        upcast Expression.Call(expr, "Invoke", [||], [| arg |])) (Expression.Constant f :> Expression)
    let lambda = Expression.Lambda(calls, pars)
    lambda.Compile()

/// Returns a System.Type of the result of a curried function of given order.
/// If n is 1, returns typeof<'b>; 
/// If n is 2, 'a->'b is 'a->'c->'d and return typeof<'d>; 
/// and so on for n in 3, 4, ... .
/// Fails for n < 1.
let getNthResultType (n:int) (f:'a->'b) : System.Type =
    match n with
    | 1 -> typeof<'b>
    | n when n > 1 -> fsFuncRes f |> Seq.take n |> Seq.last
    | _ -> failwith "Argument n is less than 1"