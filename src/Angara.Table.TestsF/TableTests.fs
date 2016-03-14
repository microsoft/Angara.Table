module Angara.Data.TestsF.TableTests

open FsUnit
open NUnit.Framework
open FsCheck
open Angara.Data.TestsF.Common
open Angara.Data
open System.IO
open System.Collections.Immutable

let colNames (t:Table) = t |> Seq.map(fun c -> c.Name) |> Seq.toList
let toArr (imar:ImmutableArray<'a>) = 
    let arr = Array.zeroCreate imar.Length
    imar.CopyTo arr
    arr


type ValidTypesRec = { TypeString:string; TypeInt:int; TypeFloat: float; TypeBool: bool; TypeDate: System.DateTime }

[<Property; Category("CI")>]
let ``Combination of functions ToRows and FromRows returns a record array identical to original`` (p:ValidTypesRec[]) =
    let table = Table.OfRows p
    let rows = table.ToRows<ValidTypesRec>() |> Seq.toArray
    Assert.AreEqual(p, rows)


[<Test; Category("CI")>]
let ``Table.ToRows``() =
    let t =
        Table ([Column.OfArray("TypeString", [|"Abcdef"|])
                Column.OfArray("TypeInt", [|45|])
                Column.OfArray("TypeFloat", [|185.0|])
                Column.OfArray("TypeBool", [|true|])
                Column.OfArray("TypeDate", [|System.DateTime(1950, 02, 03)|])])
    let persons = t.ToRows<ValidTypesRec>() |> Seq.toArray
    persons |> Seq.iter (fun p -> System.Diagnostics.Trace.WriteLine(sprintf "%A" p))
    Assert.AreEqual(1, persons.Length)
    Assert.AreEqual({TypeString = "Abcdef"; TypeInt = 45; TypeFloat = 185.0; TypeBool = true; TypeDate = System.DateTime(1950, 02, 03)}, persons.[0])

[<Test; Category("CI")>]
let ``MapToColumn replaces existing column - one column to another existing column``() =
    let t = Table([Column.OfArray("a", [|0;1|]); Column.OfArray("b", [|0;1|])])
    let t2 = Table.MapToColumn ["b"] "a" (fun a -> a + 1) t
    Assert.AreEqual(2, t2.Count, "number of columns")
    Assert.AreEqual(["b"; "a"], t2 |> colNames, "names of columns") // new 'a' added to the end
    Assert.AreEqual([|1;2|], t2.["a"].Rows.AsInt |> toArr, "array of t2.a")

[<Test; Category("CI")>]
let ``MapToColumn replaces existing column - one column to itself``() =
    let t = Table([Column.OfArray("a", [|0;1|]); Column.OfArray("b", [|0;1|])])
    let t2 = Table.MapToColumn ["a"] "a" (fun a -> a + 1) t
    Assert.AreEqual(2, t2.Count, "number of columns")
    Assert.AreEqual(["b"; "a"], t2 |> colNames, "names of columns") // new 'a' added to the end
    Assert.AreEqual([|1;2|], t2.["a"].Rows.AsInt |> toArr, "array of t2.a")

[<Test; Category("CI")>]
let ``MapToColumn replaces existing column - 2 columns``() =
    let t = Table([Column.OfArray("a", [|0;1|]); Column.OfArray("b", [|0;1|])])
    let t2 = Table.MapToColumn ["a"; "b"] "a" (fun a b -> a + b + 1) t
    Assert.AreEqual(2, t2.Count, "number of columns")
    Assert.AreEqual(["b"; "a"], t2 |> colNames, "names of columns") // new 'a' added to the end
    Assert.AreEqual([|1;3|], t2.["a"].Rows.AsInt |> toArr, "array of t2.a")

[<Test; Category("CI")>]
let TableF_MapiToColumn_ManyArgs() =
    let table:Table = 
        Table.Empty 
        |> Table.Add(Column.OfArray("a", [|1|])) 
        |> Table.Add(Column.OfArray("b", [|2|]))  
        |> Table.Add(Column.OfArray("c", [|3|]))  
        |> Table.Add(Column.OfArray("d", [|4|]))  
        |> Table.Add(Column.OfArray("e", [|5|]))  
        |> Table.Add(Column.OfArray("f", [|6|]))  
        |> Table.Add(Column.OfArray("g", [|7|])) 
        |> Table.Add(Column.OfArray("h", [|8|])) 
        |> Table.Add(Column.OfArray("i", [|9|]))  
    let table2 = table |> Table.MapiToColumn ["a"; "b"; "c"; "d"; "e"; "f"; "g"; "h"; "i"] "$" (fun idx a b c d e f g h i -> idx)
    let col : int[] = table2.["$"].Rows.AsInt |> toArr
    Assert.AreEqual([|0|], col, "Array of the column '$' produced by MapiToColumn")

[<Test; Category("CI")>]
let TableF_MapiToColumn_OneArg() =
    let table:Table = Table([Column.OfArray("a", [|1|])])
    let table2 = table |> Table.MapiToColumn ["a"] "$" (fun idx (a:int) -> idx)
    let col : int[] = table2.["$"].Rows.AsInt |> toArr
    Assert.AreEqual([|0|], col, "Array of the column '$' produced by MapiToColumn")


[<Test; Category("CI")>]
let TableF_MapiToColumn_ZeroArg() =
    let table:Table = Table([Column.OfArray("a", [|1|])])
    let table2 = table |> Table.MapiToColumn [] "$" (fun idx -> idx)
    let col : int[] = table2.["$"].Rows.AsInt |> toArr
    Assert.AreEqual([|0|], col, "Array of the column '$' produced by MapiToColumn")

[<Test; Category("CI")>]
let TableF_MapToColumn_ManyArgs() =
    let table:Table = 
        Table.Empty 
        |> Table.Add(Column.OfArray("a", [|1|])) 
        |> Table.Add(Column.OfArray("b", [|2|]))  
        |> Table.Add(Column.OfArray("c", [|3|]))  
        |> Table.Add(Column.OfArray("d", [|4|]))  
        |> Table.Add(Column.OfArray("e", [|5|]))  
        |> Table.Add(Column.OfArray("f", [|6|]))  
        |> Table.Add(Column.OfArray("g", [|7|])) 
        |> Table.Add(Column.OfArray("h", [|8|])) 
        |> Table.Add(Column.OfArray("i", [|9|]))  
    let table2 = table |> Table.MapToColumn ["a"; "b"; "c"; "d"; "e"; "f"; "g"; "h"; "i"] "$" (fun a b c d e f g h i -> true)
    let col : bool[] = table2.["$"].Rows.AsBoolean |> toArr
    Assert.AreEqual([|true|], col, "Array of the column '$' produced by MapToColumn")

[<Test; Category("CI")>]
let TableF_MapToColumn_OneArg() =
    let table:Table = Table([Column.OfArray("a", [|1|])])
    let table2 = table |> Table.MapToColumn ["a"] "$" (fun a -> a > 0)
    let col : bool[] = table2.["$"].Rows.AsBoolean |> toArr
    Assert.AreEqual([|true|], col, "Array of the column '$' produced by MapToColumn")


[<Test; Category("CI")>]
let TableF_MapToColumn_ZeroArg() =
    let table:Table = Table([Column.OfArray("a", [|1|])])
    let table2 = table |> Table.MapToColumn [] "$" (fun () -> true)
    let col : bool[] = table2.["$"].Rows.AsBoolean |> toArr
    Assert.AreEqual([|true|], col, "Array of the column '$' produced by MapToColumn")

[<Test; Category("CI")>]
let TableF_EmptyTable() =
    let table0:Table = Table.Empty
    table0 |> Seq.toList |> should equal []
    table0.Count |> should equal 0


[<Test; Category("CI")>]
let TableF_TestAddOneColumn() =
    let data:int[] = [| 1; 2; 4 |]
    let column0:Column = Column.OfArray("col1", data)
    let table0:Table = Table.Empty

    let table1 = table0 |> Table.Add column0

    table1.[0].Name |> should equal "col1"
    table1.[0] |> should equal column0
    table1.["col1"] |> should equal column0 
    table1 |> Seq.exactlyOne |> should equal column0
    table1.RowsCount  |> should equal 3
    
    column0.Height |> should equal 3
    column0.Rows.[0].AsInt |> should equal 1
    column0.Rows.[1].AsInt |> should equal 2
    column0.Rows.[2].AsInt |> should equal 4
     |> should equal None
    (fun () -> column0.Rows.[4] |> ignore) |> should throw typeof<System.IndexOutOfRangeException>
    (fun () -> column0.Rows.[2].AsReal |> ignore) |> should throw typeof<System.InvalidCastException>

[<Test; Category("CI")>]
let TableF_Transform_1() =
    let table = Table([Column.OfArray("x", [|0..10|])])
    let res : int = Table.Transform ["x"] (fun (x:ImmutableArray<int>) -> x |> Seq.sum) table
    res |> should equal 55

[<Test; Category("CI")>]
let TableF_Transform_2() =
    let table = Table([ Column.OfArray("x", [|0..10|])
                        Column.OfArray("y", [| for i in 0..10 do yield float(i) |]) ])
    let res : float = Table.Transform ["x"; "y"] (fun (x:ImmutableArray<int>) (y:ImmutableArray<float>) -> float(Seq.sum x) + (Seq.sum y)) table
    res |> should equal 110.0

[<Test; Category("CI")>]
let TableF_AppendTransform_1() =
    let table = Table([Column.OfArray("x", [|0..10|])])
    let res = table |> Table.AppendTransform ["x"] (fun (x:ImmutableArray<int>) -> Table([Column.OfArray ("y", Seq.map float x |> Seq.toArray)]))
    res |> colNames |> should equal ["x"; "y"]
    [|for i in 0..10 do yield float i|] |> should equal (res.["y"].Rows.AsReal |> toArr)
    
[<Test; Category("CI")>]
let TableF_AppendTransform_2() =
    let table = Table([ Column.OfArray("x", [|0..10|])
                        Column.OfArray("y", [| for i in 0..10 do yield float(i) |]) ])
    let res = table |> Table.AppendTransform ["x";"y"] (fun (x:ImmutableArray<int>) (y:ImmutableArray<float>) -> Table([Column.OfArray ("z", Seq.zip x y |> Seq.map (fun (s, t) -> float(s) + t) |> Seq.toArray)]))
    res |> colNames |> Set.ofSeq |> should equal (Set.ofList ["x"; "y"; "z"])
    [|for i in 0..10 do yield 2.0*(float i)|] |> should equal (res.["z"].Rows.AsReal |> toArr)


[<Test; Category("CI")>]
let ``Table.Add keeps order of columns as FIFO``() =
    let table = Table.Empty |> Table.Add (Column.OfArray("x", [|1|])) |> Table.Add (Column.OfArray("y", [|2|]))
    let x = table.[0]
    let y = table.[1]
    Assert.AreEqual("x", x.Name)
    Assert.AreEqual("y", y.Name)
    Assert.AreEqual(x, table.["x"])
    Assert.AreEqual(y, table.["y"])

[<Property; Category("CI")>]
let ``Filters table rows by a single integer column`` (table: Table) (filterBy: int[]) =    
    let count = table.RowsCount
    let filterBy = 
        if filterBy.Length > count then
            filterBy |> Seq.take count |> Seq.toArray
        elif filterBy.Length = count then
            filterBy
        else
            Array.init count (fun i -> if i < filterBy.Length then filterBy.[i] else 0)

    let table2 = table |> Table.Add (Column.OfArray("_filterBy_", filterBy))
    let tableEven = table2 |> Table.Filter ["_filterBy_"] (fun d -> d % 2 = 0)

    let mask = filterBy |> Seq.mapi(fun i d -> (i,d)) |> Seq.filter(fun (_,d) -> d % 2 = 0) |> Seq.map fst |> Seq.toArray
    
    let res = 
        table 
        |> Seq.mapi(fun colInd col ->
            let original = col.Rows
            let filtered = tableEven.[colInd].Rows
            mask
            |> Seq.mapi(fun iF iO -> original.[iO] = filtered.[iF])
            |> Seq.forall id)
        |> Seq.forall id
    res
        
