namespace Angara.Data

open System
open System.Collections
open System.Collections.Generic
open Angara.Statistics

module Util =
    let coerce<'a,'b> (o:'a) : 'b = o :> obj :?> 'b

    let coerceSome<'a,'b> (o:'a) : 'b option = o :> obj :?> 'b |> Option.Some

    let internal unpackOrFail<'a> (message:string) (opt:'a option) : 'a =
        match opt with
        | Some o -> o
        | None -> failwith message

[<ReflectedDefinition>]
type IRArray<'a> =
    inherit IReadOnlyList<'a>

    abstract member Sub : startIndex:int -> count:int -> 'a[]

    abstract member ToArray : unit -> 'a[]

type EnumeratorAdaptor<'a, 'b>(e:IEnumerator, cast:('a->'b)) =
    interface IEnumerator with
        member this.Current
            with get() : obj =
                cast(e.Current :?> 'a) :> obj

        member this.MoveNext() : bool =
            e.MoveNext()

        member this.Reset() : unit =
            e.Reset()

type GenericEnumeratorAdaptor<'a, 'b>(e:IEnumerator<'a>, cast:('a->'b)) =
    interface IEnumerator<'b> with
        member this.Current
            with get() : 'b =
                cast(e.Current)

    interface IEnumerator with
        member this.Current
            with get() : obj =
                e.Current :> obj

        member this.MoveNext() : bool =
            e.MoveNext()

        member this.Reset() : unit =
            e.Reset()

    interface IDisposable with
        member this.Dispose() : unit =
            e.Dispose()

[<ReflectedDefinition>]
type IRArrayAdapter<'a, 'b>(a:IRArray<'a>, cast:('a->'b)) =
    interface IRArray<'b> with
        member this.Sub(startIndex:int) (count:int) : 'b[] =
            let bs = a.Sub startIndex count
            Array.map cast bs

        member this.ToArray() : 'b[] =
            let bs = Seq.map cast a
            Seq.toArray bs

    interface IReadOnlyCollection<'b> with
        member this.Count
            with get() : int =
                a.Count

    interface IEnumerable<'b> with
        member this.GetEnumerator() : IEnumerator<'b> =
            let e = a.GetEnumerator()
            new GenericEnumeratorAdaptor<'a, 'b>(e, cast) :> IEnumerator<'b>

    interface IEnumerable with
        member this.GetEnumerator() : IEnumerator =
            let e = a.GetEnumerator()
            EnumeratorAdaptor<'a, 'b>(e, cast) :> IEnumerator

    interface IReadOnlyList<'b> with
        member this.Item
            with get(i) : 'b =
                cast(a.[i])

[<ReflectedDefinition>]
type RArray<'a>(a: 'a[]) =
    new(ass:seq<'a>) =
        RArray<'a>(Seq.toArray ass)

    interface IReadOnlyCollection<'a> with
        member this.Count
            with get() =
                a.Length

    interface IEnumerable<'a> with
        member this.GetEnumerator() : IEnumerator<'a> =
            (a :> IEnumerable<'a>).GetEnumerator()

    interface IEnumerable with
        member this.GetEnumerator() : IEnumerator =
            a.GetEnumerator()

    interface IReadOnlyList<'a> with
        member this.Item
            with get(i) =
                a.[i]

    interface IRArray<'a> with
        member this.Sub(sourceIndex:int) (count:int) : 'a[] =
            let trg = Array.zeroCreate count
            Array.blit a sourceIndex trg 0 count
            trg

        member this.ToArray() : 'a[] =
            let n = a.Length
            let trg = Array.zeroCreate n
            Array.blit a 0 trg 0 n
            trg

[<ReflectedDefinition>]
type LazyRArray<'a>(a:Lazy<'a[]>) =
    interface IReadOnlyCollection<'a> with
        member this.Count
            with get() =
                a.Value.Length

    interface IEnumerable<'a> with
        member this.GetEnumerator() : IEnumerator<'a> =
            (a.Value :> IEnumerable<'a>).GetEnumerator()

    interface IEnumerable with
        member this.GetEnumerator() : IEnumerator =
            a.Value.GetEnumerator()

    interface IReadOnlyList<'a> with
        member this.Item
            with get(i) =
                a.Value.[i]

    interface IRArray<'a> with
        member this.Sub(sourceIndex:int) (count:int) : 'a[] =
            let trg = Array.zeroCreate count
            Array.blit a.Value sourceIndex trg 0 count
            trg

        member this.ToArray() : 'a[] =
            let n = a.Value.Length
            let trg = Array.zeroCreate n
            Array.blit a.Value 0 trg 0 n
            trg

[<ReflectedDefinition>]
type RealColumnSummary = { 
    Min: float
    Lb95: float
    Lb68: float
    Median: float
    Ub68: float
    Ub95: float
    Max: float
    Mean: float
    Variance: float
    TotalRows: int
    DataRows: int
}

[<ReflectedDefinition>]
type ComparableColumnSummary<'a when 'a : comparison> = {
    Min: 'a
    Max: 'a
    TotalRows: int
    DataRows: int
}

[<ReflectedDefinition>]
type BooleanColumnSummary = {
    TrueRows: int
    FalseRows: int
}

[<ReflectedDefinition>]
type ColumnSummary =
    | NumericColumnSummary of RealColumnSummary
    | StringColumnSummary of ComparableColumnSummary<string>
    | DateColumnSummary of ComparableColumnSummary<DateTime>
    | BooleanColumnSummary of BooleanColumnSummary


[<ReflectedDefinition>]
[<NoComparison>]
type Column =
    private
    | IntColumn of IRArray<int>
    | RealColumn of IRArray<float>
    | StringColumn of IRArray<string>
    | DateColumn of IRArray<DateTime>
    | BooleanColumn of IRArray<Boolean>
    override this.ToString() =
        match this with
        | IntColumn arr -> sprintf "%A" arr
        | RealColumn arr -> sprintf "%A" arr
        | StringColumn arr -> sprintf "%A" arr
        | DateColumn arr -> sprintf "%A" arr
        | BooleanColumn arr -> sprintf "%A" arr


    static member New<'a>(data:'a) : Column =
        match typeof<'a> with
        | t when t = typeof<Column> -> data |> Util.coerce<'a,Column>

        | t when t = typeof<IRArray<int>> -> IntColumn (data |> Util.coerce<'a,IRArray<int>>)
        | t when t = typeof<IRArray<float>> -> RealColumn (data |> Util.coerce<'a,IRArray<float>>)
        | t when t = typeof<IRArray<string>> -> StringColumn (data |> Util.coerce<'a,IRArray<string>>)
        | t when t = typeof<IRArray<DateTime>> -> DateColumn (data |> Util.coerce<'a,IRArray<DateTime>>)
        | t when t = typeof<IRArray<Boolean>> -> BooleanColumn (data |> Util.coerce<'a,IRArray<Boolean>>)

        | t when t = typeof<int[]> -> IntColumn (RArray<int>(data |> Util.coerce<'a,int[]>))
        | t when t = typeof<float[]> -> RealColumn (RArray<float>(data |> Util.coerce<'a,float[]>))
        | t when t = typeof<string[]> -> StringColumn (RArray<string>(data |> Util.coerce<'a,string[]>))
        | t when t = typeof<DateTime[]> -> DateColumn (RArray<DateTime>(data |> Util.coerce<'a,DateTime[]>))
        | t when t = typeof<Boolean[]> -> BooleanColumn (RArray<Boolean>(data |> Util.coerce<'a,Boolean[]>))

        | t when typeof<seq<int>>.IsAssignableFrom(t) -> IntColumn (RArray<int>(data |> Util.coerce<'a,seq<int>>))
        | t when typeof<seq<float>>.IsAssignableFrom(t) -> RealColumn (RArray<float>(data |> Util.coerce<'a,seq<float>>))
        | t when typeof<seq<string>>.IsAssignableFrom(t) -> StringColumn (RArray<string>(data |> Util.coerce<'a,seq<string>>))
        | t when typeof<seq<DateTime>>.IsAssignableFrom(t) -> DateColumn (RArray<DateTime>(data |> Util.coerce<'a,seq<DateTime>>))
        | t when typeof<seq<Boolean>>.IsAssignableFrom(t) -> BooleanColumn (RArray<Boolean>(data |> Util.coerce<'a,seq<Boolean>>))

        | t when t = typeof<Lazy<int[]>> -> IntColumn (LazyRArray<int>(data |> Util.coerce<'a,Lazy<int[]>>))
        | t when t = typeof<Lazy<float[]>> -> RealColumn (LazyRArray<float>(data |> Util.coerce<'a,Lazy<float[]>>))
        | t when t = typeof<Lazy<string[]>> -> StringColumn (LazyRArray<string>(data |> Util.coerce<'a,Lazy<string[]>>))
        | t when t = typeof<Lazy<DateTime[]>> -> DateColumn (LazyRArray<DateTime>(data |> Util.coerce<'a,Lazy<DateTime[]>>))
        | t when t = typeof<Lazy<Boolean[]>> -> BooleanColumn (LazyRArray<Boolean>(data |> Util.coerce<'a,Lazy<Boolean[]>>))
        
        | t when t = typeof<System.Array> ->
            let array = data |> Util.coerce<'a,System.Array>
            match array with
            | null -> failwith("Array is null")
            | _ ->
                match array.GetType().GetElementType() with
                | et when et = typeof<int> -> IntColumn (RArray<int>(data |> Util.coerce<'a,int[]>))
                | et when et = typeof<float> -> RealColumn (RArray<float>(data |> Util.coerce<'a,float[]>))
                | et when et = typeof<string> -> StringColumn (RArray<string>(data |> Util.coerce<'a,string[]>))
                | et when et = typeof<DateTime> -> DateColumn (RArray<DateTime>(data |> Util.coerce<'a,DateTime[]>))
                | et when et = typeof<Boolean> -> BooleanColumn (RArray<Boolean>(data |> Util.coerce<'a,Boolean[]>))
                | _ -> failwith("Unexpected type")

        | _ -> failwith("Unexpected type")


    static member ValidTypes
        with get() : Type[] =
            [| typeof<int>; typeof<float>; typeof<string>; typeof<DateTime>; typeof<bool> |]

    static member Type(column:Column) : Type =
        match column with
        | IntColumn _ -> typeof<int>
        | RealColumn _ -> typeof<float>
        | StringColumn _ -> typeof<string>
        | DateColumn _ -> typeof<DateTime>
        | BooleanColumn _ -> typeof<Boolean>

    static member TrySub<'a> (startIndex:int) (count:int) (column:Column) : 'a option =
        if startIndex >= 0 && count >= 0 && startIndex + count <= Column.Count column then
            if typeof<'a> = typeof<Column> then
                match column  with
                | IntColumn ir -> IntColumn(RArray<int>(ir.Sub startIndex count)) |> Util.coerceSome
                | RealColumn ir -> RealColumn(RArray<float>(ir.Sub startIndex count)) |> Util.coerceSome
                | StringColumn ir -> StringColumn(RArray<string>(ir.Sub startIndex count)) |> Util.coerceSome
                | DateColumn ir -> DateColumn(RArray<DateTime>(ir.Sub startIndex count)) |> Util.coerceSome
                | BooleanColumn ir -> BooleanColumn(RArray<Boolean>(ir.Sub startIndex count)) |> Util.coerceSome

            elif typedefof<'a> = typedefof<IRArray<_>> then
                match column with
                | IntColumn ir when typeof<'a> = typeof<IRArray<int>> -> RArray<int>(ir.Sub startIndex count) |> Util.coerceSome
                | RealColumn ir when typeof<'a> = typeof<IRArray<float>> -> RArray<float>(ir.Sub startIndex count) |> Util.coerceSome
                | StringColumn ir when typeof<'a> = typeof<IRArray<string>> -> RArray<string>(ir.Sub startIndex count) |> Util.coerceSome
                | DateColumn ir when typeof<'a> = typeof<IRArray<DateTime>> -> RArray<DateTime>(ir.Sub startIndex count) |> Util.coerceSome
                | BooleanColumn ir when typeof<'a> = typeof<IRArray<Boolean>> -> RArray<Boolean>(ir.Sub startIndex count) |> Util.coerceSome
                | _ -> None

            elif typeof<'a>.IsArray then
                match column with
                | IntColumn ir when typeof<'a> = typeof<int[]> -> ir.Sub startIndex count |> Util.coerceSome
                | RealColumn ir when typeof<'a> = typeof<float[]> -> ir.Sub startIndex count |> Util.coerceSome
                | StringColumn ir when typeof<'a> = typeof<string[]> -> ir.Sub startIndex count |> Util.coerceSome
                | DateColumn ir when typeof<'a> = typeof<DateTime[]> -> ir.Sub startIndex count |> Util.coerceSome
                | BooleanColumn ir when typeof<'a> = typeof<Boolean[]> -> ir.Sub startIndex count |> Util.coerceSome
                | _ -> None

            elif typeof<'a> = typeof<Array> then
                match column with
                | IntColumn ir -> ir.Sub startIndex count |> Util.coerceSome
                | RealColumn ir -> ir.Sub startIndex count |> Util.coerceSome
                | StringColumn ir -> ir.Sub startIndex count |> Util.coerceSome
                | DateColumn ir -> ir.Sub startIndex count |> Util.coerceSome
                | BooleanColumn ir -> ir.Sub startIndex count |> Util.coerceSome

            else None
        else None

    static member Sub<'a> (startIndex:int) (count:int) (column:Column) : 'a =
        Column.TrySub<'a> startIndex count column
        |> Util.unpackOrFail "Unexpected type"

    static member TryToArray<'a>(column:Column) : 'a option =
        if typeof<'a> = typeof<Column> then
            column |> Util.coerceSome

        elif typedefof<'a> = typedefof<IRArray<_>> then
            match column with
            | IntColumn ir when typeof<'a> = typeof<IRArray<int>> -> ir |> Util.coerceSome
            | RealColumn ir when typeof<'a> = typeof<IRArray<float>> -> ir |> Util.coerceSome
            | StringColumn ir when typeof<'a> = typeof<IRArray<string>> -> ir |> Util.coerceSome
            | DateColumn ir when typeof<'a> = typeof<IRArray<DateTime>> -> ir |> Util.coerceSome
            | BooleanColumn ir when typeof<'a> = typeof<IRArray<Boolean>> -> ir |> Util.coerceSome
            | _ -> None

        elif typeof<'a>.IsArray then
            match column with
            | IntColumn ir when typeof<'a> = typeof<int[]> -> ir.ToArray() |> Util.coerceSome
            | RealColumn ir when typeof<'a> = typeof<float[]> -> ir.ToArray() |> Util.coerceSome
            | StringColumn ir when typeof<'a> = typeof<string[]> -> ir.ToArray() |> Util.coerceSome
            | DateColumn ir when typeof<'a> = typeof<DateTime[]> -> ir.ToArray() |> Util.coerceSome
            | BooleanColumn ir when typeof<'a> = typeof<Boolean[]> -> ir.ToArray() |> Util.coerceSome
            | _ -> None

        elif typeof<'a> = typeof<Array> then
            match column with
            | IntColumn ir -> ir.ToArray() |> Util.coerceSome
            | RealColumn ir -> ir.ToArray() |> Util.coerceSome
            | StringColumn ir -> ir.ToArray() |> Util.coerceSome
            | DateColumn ir -> ir.ToArray() |> Util.coerceSome
            | BooleanColumn ir -> ir.ToArray() |> Util.coerceSome

        else None

    static member ToArray<'a>(column:Column) : 'a =
        Column.TryToArray<'a> column
        |> Util.unpackOrFail "Unexpected type"

    static member Count(column:Column) : int =
        match column with
        | IntColumn ir -> ir.Count
        | RealColumn ir -> ir.Count
        | StringColumn ir -> ir.Count
        | DateColumn ir -> ir.Count
        | BooleanColumn ir -> ir.Count

    static member MinimumCount(columns:seq<Column>) : int =
        columns
        |> Seq.map Column.Count
        |> Seq.min

    static member GetEnumerator<'a>(column:Column) : IEnumerator<'a> =
        match column, typeof<'a> with
        | IntColumn ir, t when t = typeof<int> -> ir.GetEnumerator() :?> IEnumerator<'a>
        | RealColumn ir, t when t = typeof<float> -> ir.GetEnumerator() :?> IEnumerator<'a>
        | StringColumn ir, t when t = typeof<string> -> ir.GetEnumerator() :?> IEnumerator<'a>
        | DateColumn ir, t when t = typeof<DateTime> -> ir.GetEnumerator() :?> IEnumerator<'a>
        | BooleanColumn ir, t when t = typeof<Boolean> -> ir.GetEnumerator() :?> IEnumerator<'a>
        | _ -> failwith("Unexpected type")

    static member GetEnumerator(column:Column) : IEnumerator =
        match column with
        | IntColumn ir -> (ir :> IEnumerable).GetEnumerator()
        | RealColumn ir -> (ir :> IEnumerable).GetEnumerator()
        | StringColumn ir -> (ir :> IEnumerable).GetEnumerator()
        | DateColumn ir -> (ir :> IEnumerable).GetEnumerator()
        | BooleanColumn ir -> (ir :> IEnumerable).GetEnumerator()

    static member TryItem<'a> (index:int) (column:Column) : 'a option =
        if index >= 0 && index < Column.Count column then
            if typeof<'a> = typeof<obj> then
                match column with
                | IntColumn ir -> ir.[index] |> Util.coerceSome
                | RealColumn ir -> ir.[index] |> Util.coerceSome
                | StringColumn ir -> ir.[index] |> Util.coerceSome
                | DateColumn ir -> ir.[index] |> Util.coerceSome
                | BooleanColumn ir -> ir.[index] |> Util.coerceSome
            else
                match column, typeof<'a> with
                | IntColumn ir, t when t = typeof<int> -> ir.[index] |> Util.coerceSome
                | RealColumn ir, t when t = typeof<float> -> ir.[index] |> Util.coerceSome
                | StringColumn ir, t when t = typeof<string> -> ir.[index] |> Util.coerceSome
                | DateColumn ir, t when t = typeof<DateTime> -> ir.[index] |> Util.coerceSome
                | BooleanColumn ir, t when t = typeof<Boolean> -> ir.[index] |> Util.coerceSome
                | _ -> None
        else None

    static member Item<'a> (index:int) (column:Column) : 'a =
        Column.TryItem<'a> index column
        |> Util.unpackOrFail "Unexpected type"


    static member Map2<'a,'b,'c> (map:('a->'b->'c)) (column1:Column) (column2:Column) : seq<'c> =
        match column1, column2, box map with
        | IntColumn ir1, IntColumn ir2, (:? (int->int->'c) as tmap) -> Seq.map2 tmap ir1 ir2
        | IntColumn ir1, RealColumn ir2, (:? (int->float->'c) as tmap) -> Seq.map2 tmap ir1 ir2
        | IntColumn ir1, StringColumn ir2, (:? (int->string->'c) as tmap) -> Seq.map2 tmap ir1 ir2
        | IntColumn ir1, DateColumn ir2, (:? (int->DateTime->'c) as tmap) -> Seq.map2 tmap ir1 ir2
        | RealColumn ir1, IntColumn ir2, (:? (float->int->'c) as tmap) -> Seq.map2 tmap ir1 ir2
        | RealColumn ir1, RealColumn ir2, (:? (float->float->'c) as tmap) -> Seq.map2 tmap ir1 ir2
        | RealColumn ir1, StringColumn ir2, (:? (float->string->'c) as tmap) -> Seq.map2 tmap ir1 ir2
        | RealColumn ir1, DateColumn ir2, (:? (float->DateTime->'c) as tmap) -> Seq.map2 tmap ir1 ir2
        | StringColumn ir1, IntColumn ir2, (:? (string->int->'c) as tmap) -> Seq.map2 tmap ir1 ir2
        | StringColumn ir1, RealColumn ir2, (:? (string->float->'c) as tmap) -> Seq.map2 tmap ir1 ir2
        | StringColumn ir1, StringColumn ir2, (:? (string->string->'c) as tmap) -> Seq.map2 tmap ir1 ir2
        | StringColumn ir1, DateColumn ir2, (:? (string->DateTime->'c) as tmap) -> Seq.map2 tmap ir1 ir2
        | DateColumn ir1, IntColumn ir2, (:? (DateTime->int->'c) as tmap) -> Seq.map2 tmap ir1 ir2
        | DateColumn ir1, RealColumn ir2, (:? (DateTime->float->'c) as tmap) -> Seq.map2 tmap ir1 ir2
        | DateColumn ir1, StringColumn ir2, (:? (DateTime->string->'c) as tmap) -> Seq.map2 tmap ir1 ir2
        | DateColumn ir1, DateColumn ir2, (:? (DateTime->DateTime->'c) as tmap) -> Seq.map2 tmap ir1 ir2
        | BooleanColumn ir1, IntColumn ir2, (:? (Boolean->int->'c) as tmap) -> Seq.map2 tmap ir1 ir2
        | BooleanColumn ir1, RealColumn ir2, (:? (Boolean->float->'c) as tmap) -> Seq.map2 tmap ir1 ir2
        | BooleanColumn ir1, StringColumn ir2, (:? (Boolean->string->'c) as tmap) -> Seq.map2 tmap ir1 ir2
        | BooleanColumn ir1, DateColumn ir2, (:? (Boolean->DateTime->'c) as tmap) -> Seq.map2 tmap ir1 ir2
        | _ -> failwith("Incorrect type")

    static member Map<'a,'b,'c> (map:('a->'b)) (columns:seq<Column>) : 'c[] =
        if Seq.isEmpty columns then Array.empty
        else
            let cs = Array.ofSeq columns
            if cs.Length = 1 then
                match box map with
                | (:? ('a->'c) as map1) -> 
                    match cs.[0], box map1 with
                    | IntColumn ir1, (:? (int->'c) as tmap) -> ir1.ToArray() |> Array.map tmap
                    | RealColumn ir1, (:? (float->'c) as tmap) -> ir1.ToArray() |> Array.map tmap
                    | StringColumn ir1, (:? (string->'c) as tmap) -> ir1.ToArray() |> Array.map tmap
                    | DateColumn ir1, (:? (DateTime->'c) as tmap) -> ir1.ToArray() |> Array.map tmap
                    | BooleanColumn ir1, (:? (Boolean->'c) as tmap) -> ir1.ToArray() |> Array.map tmap
                    | _ -> failwith("Incorrect type")
                | _ -> failwith("Incorrect map function")
            else
                let deleg = Funcs.toDelegate map
                let colArrays = cs |> Array.map (fun c -> Column.ToArray<System.Array> c)
                let len = colArrays |> Array.map (fun arr -> arr.Length) |> Array.min
                let res = Array.init len (fun i ->    
                    let row = colArrays |> Array.map (fun a -> a.GetValue(i))                  
                    deleg.DynamicInvoke(row) :?> 'c)
                res

    static member Mapi<'a,'b,'c> (map:(int->'a->'b)) (columns:seq<Column>) : 'c[] =
        if Seq.isEmpty columns then Array.empty
        else
            let cs = Array.ofSeq columns
            if cs.Length = 1 then
                match box map with
                | (:? (int->'a->'c) as map1) -> 
                    match cs.[0], box map1 with
                    | IntColumn ir1, (:? (int->int->'c) as tmap) -> ir1.ToArray() |> Array.mapi tmap
                    | RealColumn ir1, (:? (int->float->'c) as tmap) -> ir1.ToArray() |> Array.mapi tmap
                    | StringColumn ir1, (:? (int->string->'c) as tmap) -> ir1.ToArray() |> Array.mapi tmap
                    | DateColumn ir1, (:? (int->DateTime->'c) as tmap) -> ir1.ToArray() |> Array.mapi tmap
                    | BooleanColumn ir1, (:? (int->Boolean->'c) as tmap) -> ir1.ToArray() |> Array.mapi tmap
                    | _ -> failwith("Incorrect type")
                | _ -> failwith("Incorrect map function")
            else
                let deleg = Funcs.toDelegate map
                let colArrays = cs |> Array.map (fun c -> Column.ToArray<System.Array> c)
                let len = colArrays |> Array.map (fun arr -> arr.Length) |> Array.min
                let res = Array.init len (fun i ->                        
                    let row = Array.append [|i:>obj|] (colArrays |> Array.map (fun a -> a.GetValue(i)))
                    deleg.DynamicInvoke(row) :?> 'c)
                res

    static member TryPdf (pointCount:int) (column:Column) : (float[] * float[]) option =
        if Column.Count column = 0 then None
        else
            let rs = column |> Column.TryToRealArray   
            match rs with         
            | Some rs -> try Some(rs.ToArray() |> kde pointCount) with _ -> None
            | None -> None


    static member Pdf (pointCount:int) (column:Column) : (float[] * float[]) =
        if Column.Count column = 0 then failwith "Column is empty"
        else
            let rs = column |> Column.ToRealArray  
            rs.ToArray() |> kde pointCount

    static member IntToRealArray(ir:IRArray<int>) : IRArray<float> =
        IRArrayAdapter(ir, float) :> IRArray<float>

    static member TryToRealArray(column:Column) : IRArray<float> option =
        match column with
        | IntColumn ir -> Column.IntToRealArray(ir) |> Option.Some
        | RealColumn ir -> ir |> Option.Some
        | StringColumn _ -> None
        | DateColumn _ -> None
        | BooleanColumn _ -> None

    static member ToRealArray(column:Column) : IRArray<float> =
        column
        |> Column.TryToRealArray
        |> Util.unpackOrFail "Unexpected type"

    static member Select (mask:seq<bool>) (column:Column) : Column =
        match column with
        | IntColumn ir ->
            let sr:seq<int> =
                ir
                |> Seq.zip mask
                |> Seq.choose (fun (b,v) -> if b then Some v else None)
            IntColumn(RArray<int>(sr))

        | RealColumn ir ->
            let sr:seq<float> =
                ir
                |> Seq.zip mask
                |> Seq.choose (fun (b,v) -> if b then Some v else None)
            RealColumn(RArray<float>(sr))

        | StringColumn ir ->
            let sr:seq<string> =
                ir
                |> Seq.zip mask
                |> Seq.choose (fun (b,v) -> if b then Some v else None)
            StringColumn(RArray<string>(sr))

        | DateColumn ir ->
            let sr:seq<DateTime> =
                ir
                |> Seq.zip mask
                |> Seq.choose (fun (b,v) -> if b then Some v else None)
            DateColumn(RArray<DateTime>(sr))
            
        | BooleanColumn ir ->
            let sr:seq<Boolean> =
                ir
                |> Seq.zip mask
                |> Seq.choose (fun (b,v) -> if b then Some v else None)
            BooleanColumn(RArray<Boolean>(sr))

    static member Summary(i:IRArray<int>) : RealColumnSummary =
        i
        |> Column.IntToRealArray
        |> Column.Summary

    static member Summary(a:IRArray<float>) : RealColumnSummary =
        let summary = summary a
        let qsummary = qsummary a

        let ccs:RealColumnSummary = {
            Min = summary.min
            Lb95 = qsummary.lb95
            Lb68 = qsummary.lb68
            Median = qsummary.median
            Ub68 = qsummary.ub68
            Ub95 = qsummary.ub95
            Max = summary.max
            Mean = summary.mean
            Variance = summary.variance
            TotalRows = a.Count
            DataRows = summary.count
        }

        ccs

    static member Summary(a:seq<string>) : ComparableColumnSummary<string> =
        let min, max, total, count = 
            a
            |> Seq.fold (
                fun (min, max, total, count) s ->
                    match s with
                    | ""
                    | null -> (min, max, total + 1, count)
                    | _ ->
                        let min = if count = 0 || s < min then s else min
                        let max = if count = 0 || s > max then s else max
                        (min, max, total + 1, count + 1)
                    )
                ("", "", 0, 0)

        let ccs:ComparableColumnSummary<string> = {
            Min = min
            Max = max
            TotalRows = total
            DataRows = count
        }

        ccs

    static member Summary(a:seq<DateTime>) : ComparableColumnSummary<DateTime> =
        let min, max, total = 
            a
            |> Seq.fold (
                fun (min, max, total) s ->
                    let min = if s < min then s else min
                    let max = if s > max then s else max
                    (min, max, total + 1)
                )
                (DateTime.MaxValue, DateTime.MinValue, 0)

        let ccs:ComparableColumnSummary<DateTime> = {
            Min = min
            Max = max
            TotalRows = total
            DataRows = total
        }

        ccs

    static member Summary(a:seq<Boolean>) : BooleanColumnSummary =
        let nT, nF = 
            a |> Seq.fold (fun (nT, nF) s -> if s then nT+1,nF else nT,nF+1) (0, 0)

        let ccs:BooleanColumnSummary = {
             TrueRows = nT
             FalseRows = nF
        }
        ccs

    static member Summary(column:Column) : ColumnSummary =
        match column with
        | IntColumn ir -> Column.Summary ir |> NumericColumnSummary
        | RealColumn ir -> Column.Summary ir |> NumericColumnSummary
        | StringColumn ir -> Column.Summary ir |> StringColumnSummary
        | DateColumn ir -> Column.Summary ir |> DateColumnSummary
        | BooleanColumn ir -> Column.Summary ir |> BooleanColumnSummary

[<ReflectedDefinition>]
type Table(names:seq<string>, columns:seq<Column>) =

    let namesRO : IReadOnlyList<string> =
        RArray<string>(names) :> IReadOnlyList<string>

    let columnsRO : IReadOnlyList<Column> =
        RArray<Column>(columns) :> IReadOnlyList<Column>

    let rowsCount = 
        let m = Seq.fold (fun (count:int) (col:Column) ->
                    let n = Column.Count col
                    if count <> -1 && count <> n then raise (ArgumentException("All columns of a table must have same number of rows"))
                    n) -1 columns
        if m = -1 then 0 else m

    let types : IReadOnlyList<Type> =
        let cns =
            columns
            |> Seq.map Column.Type

        RArray<Type>(cns) :> IReadOnlyList<Type>

    new() = Table([], [])

    new(nameColumns:seq<string * Column>) =
        let names, columns =
            nameColumns
            |> Array.ofSeq
            |> Array.unzip
        Table(names, columns)

    member this.Names
        with get() : IReadOnlyList<string> =
            namesRO

    member this.Columns
        with get() : IReadOnlyList<Column> =
            columnsRO

    member this.Count
        with get() : int =
            rowsCount

    member this.Types
        with get() : IReadOnlyList<Type> =
            types

    override this.ToString() =
        let cols = 
            this.Columns 
            |> Seq.mapi(fun i c -> 
                let name = this.Names.[i]
                let count = Column.Count c
                sprintf "\"%s\"[%d]: %A" name count c)
        String.Join("\n", cols)


    static member Empty: Table =
        new Table()

    static member New<'a> (columnName:string) (columnData:'a) : Table =
        new Table([columnName, Column.New columnData])

    static member FromArrays (columns: (string * Array) seq) : Table =
        new Table(columns |> Seq.map(fun (n,a) -> n, Column.New<System.Array> a))

    static member Add<'a> (name:string) (data:'a) (table:Table) : Table =
        let column:Column = Column.New<'a> data
        if table.Columns.Count <> 0 && Column.Count column <> table.Count then raise (ArgumentException("The column has different number of rows than the table"))
        let names = List.append (table.Names |> Seq.toList) [name]
        let columns = List.append (table.Columns |> Seq.toList) [column]
        Table(names, columns)

    static member Remove (columnNames:seq<string>) (table:Table) : Table =
        let names = Set.ofSeq columnNames
        let newNames, newColumns =
            Seq.zip table.Names table.Columns
            |> Seq.filter(fun (name, _) -> not(names.Contains name))
            |> Seq.toArray
            |> Array.unzip

        Table(newNames, newColumns)

    static member TryName(column:Column) (table:Table) : string option =
        match  table.Columns |> Seq.tryFindIndex (fun c -> c.Equals(column)) with
        | Some i -> Some(table.Names.[i])
        | None -> None

    static member Name(column:Column) (table:Table) : string =
        match Table.TryName column table with
        | Some n -> n
        | None -> failwith "Column not found"

    static member TryColumnIndex(columnName:string) (table:Table) : int option =
        table.Names
        |> Seq.tryFindIndex (fun n -> Object.Equals(n, columnName))

    static member ColumnIndex(columnName:string) (table:Table) : int =
        match table.Names |> Seq.tryFindIndex (fun n -> Object.Equals(n, columnName)) with
        | Some i -> i
        | None -> failwith (sprintf "Column '%s' not found" columnName)

    static member TryColumn(columnName:string) (table:Table) : Column option =
        table
        |> Table.TryColumnIndex columnName
        |> Option.map (fun index -> table.Columns.[index])

    static member Column(columnName:string) (table:Table) : Column =
        let index = Table.ColumnIndex columnName table
        table.Columns.Item index

    static member TryType(columnName:string) (table:Table) : Type option =
        table
        |> Table.TryColumn columnName
        |> Option.map Column.Type

    static member Type(columnName:string) (table:Table) : Type =
        table
        |> Table.Column columnName
        |> Column.Type

    static member TrySub<'a>(columnName:string) (startIndex:int) (count:int) (table:Table) : 'a option =
        table
        |> Table.TryColumn columnName
        |> Option.bind (Column.TrySub<'a> startIndex count)

    static member Sub<'a>(columnName:string) (startIndex:int) (count:int) (table:Table) : 'a =
        table
        |> Table.Column columnName
        |> Column.Sub<'a> startIndex count

    static member TryToArray<'a>(columnName:string) (table:Table) : 'a option =
        table
        |> Table.TryColumn columnName
        |> Option.bind Column.TryToArray

    static member ToArray<'a>(columnName:string) (table:Table) : 'a =
        table
        |> Table.Column columnName
        |> Column.ToArray

    static member GetEnumeratorT<'a>(columnName:string) (table:Table) : IEnumerator<'a> =
        let column = Table.Column columnName table
        Column.GetEnumerator<'a> column

    static member GetEnumerator(columnName:string) (table:Table) : IEnumerator =
        table
        |> Table.Column columnName
        |> Column.GetEnumerator

    static member TryItem<'a>(columnName:string) (index:int) (table:Table) : 'a option =
        table
        |> Table.TryColumn columnName
        |> Option.bind (Column.TryItem<'a> index)

    static member Item<'a>(columnName:string) (index:int) (table:Table) : 'a =
        table
        |> Table.Column columnName
        |> Column.Item<'a> index

    static member Filter<'a> (columnNames:seq<string>) (predicate:('a->bool)) (table:Table) : Table =
        let mask =
            columnNames
            |> Seq.map (fun c -> Table.Column c table)
            |> Column.Map predicate

        let newColumns =
            table.Columns
            |> Seq.map (Column.Select mask)

        Table(table.Names, newColumns)

    static member Filteri<'a> (columnNames:seq<string>) (predicate:(int->'a->bool)) (table:Table) : Table =
        let mask =
            columnNames
            |> Seq.map (fun c -> Table.Column c table)
            |> Column.Mapi predicate

        let newColumns =
            table.Columns
            |> Seq.map (Column.Select mask)

        Table(table.Names, newColumns)

    static member Join(table1:Table) (table2:Table) : Table =
        let newNames:seq<string> = Seq.append table1.Names table2.Names
        let newColumns:seq<Column> = Seq.append table1.Columns table2.Columns

        Table(newNames, newColumns)

    static member JoinTransform<'a,'b>(columnNames:seq<string>) (transform:('a->'b)) (table:Table) : Table =
        Table.Transform<'a,'b,Table> columnNames transform table
        |> Table.Join table

    static member Map<'a,'b,'c>(columnNames:seq<string>) (map:('a->'b)) (table:Table) : 'c[] =
        columnNames
        |> Seq.map (fun c -> Table.Column c table)
        |> Column.Map<'a,'b,'c> map

    static member Mapi<'a,'b,'c>(columnNames:seq<string>) (map:(int->'a->'b)) (table:Table) : 'c[] =
        columnNames
        |> Seq.map (fun c -> Table.Column c table)
        |> Column.Mapi<'a,'b,'c> map 

    static member MapToColumn<'a,'b,'c>(columnNames:seq<string>) (newColumnName:string) (map:('a->'b)) (table:Table) : Table =
        let data = Table.Map<'a,'b,'c> columnNames map table
        if columnNames |> Seq.contains newColumnName then table |> Table.Remove [newColumnName] else table
        |> Table.Add newColumnName data

    static member MapiToColumn<'a,'b,'c>(columnNames:seq<string>) (newColumnName:string) (map:(int->'a->'b)) (table:Table) : Table =
        let data = Table.Mapi<'a,'b,'c> columnNames map table
        if Seq.contains newColumnName columnNames then table |> Table.Remove [newColumnName] else table
        |> Table.Add newColumnName data

    static member TryPdf(columnName:string) (pointCount:int) (table:Table) : (float[] * float[]) option =
        table
        |> Table.TryColumn columnName
        |> Option.bind (Column.TryPdf pointCount)

    static member Pdf(columnName:string) (pointCount:int) (table:Table) : (float[] * float[]) =
        table
        |> Table.Column columnName
        |> Column.Pdf pointCount

    static member TryToRealArray(columnName:string) (table:Table) : IRArray<float> option =
        table
        |> Table.TryColumn columnName
        |> Option.bind Column.TryToRealArray

    static member ToRealArray(columnName:string) (table:Table) : IRArray<float> =
        table
        |> Table.Column columnName
        |> Column.ToRealArray

    static member TrySummary(columnName:string) (table:Table) : ColumnSummary option =
        table
        |> Table.TryColumn columnName
        |> Option.map Column.Summary

    static member Summary(columnName:string) (table:Table) : ColumnSummary =
        table
        |> Table.Column columnName
        |> Column.Summary

    static member Transform<'a,'b,'c> (columnNames:seq<string>) (transform:('a->'b)) (table:Table) : 'c =
        let cs = Seq.toArray columnNames
        if cs.Length = 1 then
            match box transform with
            | (:? ('a->'c) as transform1) ->  table |> Table.ToArray<'a> cs.[0] |> transform1
            | _ -> failwith "Transform function cannot be applied to the given column"
        else 
            let deleg = Funcs.toDelegate transform
            let colArrays = cs |> Array.map(fun c -> let col = Table.Column c table in Column.ToArray<System.Array> col) |> Array.map (fun a -> a :> obj)
            deleg.DynamicInvoke(colArrays) :?> 'c

    static member TryCorrelation(table:Table) : (string[] * float[][]) option =
        let realColumnChooser(name:string, column:Column) : (string * float[]) option =
            column
            |> Column.TryToRealArray
            |> Option.map (fun ir -> (name, ir.ToArray()))

        let realNames, realColumns:string[] * float[][] =
            Seq.zip table.Names table.Columns
            |> Seq.choose realColumnChooser
            |> Seq.toArray
            |> Array.unzip

        let n = realColumns.Length

        if n <= 1 then None
        else
            let corr:float[][] = Array.zeroCreate (n-1)

            for i = 0 to n-2 do
                let corri:float[] = Array.zeroCreate (n-i-1)
                corr.[i] <- corri
                let x1 = realColumns.[i]
                for j = 0 to n-i-2 do
                    let x2 = realColumns.[i+j+1]
                    corri.[j] <- correlation x1 x2

            Some(realNames, corr)

    static member Correlation(table:Table) : (string[] * float[][]) =
        table
        |> Table.TryCorrelation
        |> Util.unpackOrFail "At least two columns must be real or int"

    static member Read (settings:Angara.Data.ReadSettings) (stream:IO.Stream) : Table =
        let cols = 
            stream 
            |> Angara.Data.DelimitedFile.Read settings
            |> Array.map(fun (schema, data) -> 
                schema.Name,
                match schema.Type with
                | Angara.Data.ColumnType.Double -> Column.New (data :?> float[])
                | Angara.Data.ColumnType.Integer -> Column.New (data :?> int[])
                | Angara.Data.ColumnType.Boolean -> Column.New (data :?> bool[])
                | Angara.Data.ColumnType.DateTime -> Column.New (data :?> DateTime[])
                | Angara.Data.ColumnType.String -> Column.New (data :?> string[]))
        new Table(cols)

    static member Write (settings:Angara.Data.WriteSettings) (stream:IO.Stream) (table:Table) : unit =
        table.Columns
        |> Seq.map(fun column ->
            Table.Name column table,
            Column.ToArray<System.Array> column)         
        |> Angara.Data.DelimitedFile.Write settings stream            
        
