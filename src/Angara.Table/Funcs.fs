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
            None) |> Seq.toArray
    args

/// Creates a delegate from a curried function.
let toDelegate (f:'a->'b) =
    let args = fsFuncParameters f
    let pars = args |> Array.map(fun a -> Expression.Parameter a) 
    let calls = pars |> Seq.fold(fun (expr:Expression) p -> upcast Expression.Call(expr, "Invoke", [||], [|p :> Expression|])) (Expression.Constant f :> Expression)
    let lambda = Expression.Lambda(calls, pars)
    lambda.Compile()

/// Creates a delegate from a curried function, but each parameter of the delegate is a 1d-array with a single element to be passed to the original function.
let toDelegate_UnwrapArrays (f:'a->'b) =
    let args = fsFuncParameters f
    let pars = args |> Array.map(fun a -> 
        let arrType = System.Array.CreateInstance(a, 0).GetType()
        Expression.Parameter arrType) 
    let calls = pars |> Seq.fold(fun (expr:Expression) p -> 
        let arg = Expression.ArrayIndex(p :> Expression, Expression.Constant 0) :> Expression
        upcast Expression.Call(expr, "Invoke", [||], [| arg |])) (Expression.Constant f :> Expression)
    let lambda = Expression.Lambda(calls, pars)
    lambda.Compile()

