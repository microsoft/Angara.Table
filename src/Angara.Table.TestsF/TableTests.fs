module Angara.Data.TestsF.TableTests

open FsUnit
open NUnit.Framework
open FsCheck
open Angara.Data.TestsF.Common
open Angara.Data
open System.IO


[<Test; Category("CI")>]
let TableF_MapToColumn_ManyArgs() =
    let table:Table = 
        Table.New "a" [1] |>
        Table.Add "b" [2] |> 
        Table.Add "c" [3] |> 
        Table.Add "d" [4] |> 
        Table.Add "e" [5] |> 
        Table.Add "f" [6] |> 
        Table.Add "g" [7] |>
        Table.Add "h" [8] |>
        Table.Add "i" [9]  
    let table2 = table |> Table.MapToColumn ["a"; "b"; "c"; "d"; "e"; "f"; "g"; "h"; "i"] "$" (fun a b c d e f g h i -> true)
    let col : bool[] = table2 |> Table.ToArray "$"
    Assert.AreEqual([|true|], col, "Array of the column '$' produced by MapToColumn")

[<Test; Category("CI")>]
let TableF_MapToColumn_OneArg() =
    let table:Table = Table.New "a" [1] 
    let table2 = table |> Table.MapToColumn ["a"] "$" (fun a -> a > 0)
    let col : bool[] = table2 |> Table.ToArray "$"
    Assert.AreEqual([|true|], col, "Array of the column '$' produced by MapToColumn")


[<Test; Category("CI")>]
let TableF_MapToColumn_ZeroArg() =
    let table:Table = Table.New "a" [1] 
    let table2 = table |> Table.MapToColumn [] "$" (fun () -> true)
    let col : bool[] = table2 |> Table.ToArray "$"
    Assert.AreEqual([|true|], col, "Array of the column '$' produced by MapToColumn")

[<Test; Category("CI")>]
let TableF_EmptyTable() =
    let table0:Table = Table.Empty
    table0.Names |> should equal []
    table0.Columns |> should equal []
    table0.Count |> should equal 0
    table0.Types |> should equal []


[<Test; Category("CI")>]
let TableF_TestAddOneColumn() =

    let data:int[] = [| 1; 2; 4 |]

    let column0:Column = Column.New<int[]>(data)

    let table0:Table = Table.Empty

    let table1 =
        table0
        |> Table.Add "col1" column0

    table1.Names |> should equal [| "col1" |]
    table1.Columns |> should equal [| column0 |]
    table1.Types |> should equal [| typeof<int> |]
    table1.Count  |> should equal 3

    let column01 = Table.Column "col1" table1

    Column.Type column01 |> should equal typeof<int>
    Column.Count column01 |> should equal 3
    Column.TryItem<int> -1 column01 |> should equal None
    Column.TryItem<int> 0 column01 |> should equal (Some(1))
    Column.TryItem<int> 1 column01 |> should equal (Some(2))
    Column.TryItem<int> 2 column01 |> should equal (Some(4))
    Column.TryItem<int> 3 column01 |> should equal None
    (fun () -> Column.Item<int> -1 column01 |> ignore) |> should throw typeof<System.Exception>
    Column.Item<int> 0 column01 |> should equal 1
    Column.Item<int> 1 column01 |> should equal 2
    Column.Item<int> 2 column01 |> should equal 4
    (fun () -> Column.Item<int> 3 column01 |> ignore) |> should throw typeof<System.Exception>

    Table.Type "col1" table1 |> should equal typeof<int>
    Table.Column "col1" table1 |> Column.Count |> should equal 3
    Table.TryItem<int> "col1" -1 table1 |> should equal None
    Table.TryItem<int> "col1" 0 table1 |> should equal (Some(1))
    Table.TryItem<int> "col1" 1 table1 |> should equal (Some(2))
    Table.TryItem<int> "col1" 2 table1 |> should equal (Some(4))
    Table.TryItem<int> "col1" 3 table1 |> should equal None
    (fun () -> Table.Item<int> "col1" -1 table1 |> ignore) |> should throw typeof<System.Exception>
    Table.Item<int> "col1" 0 table1 |> should equal 1
    Table.Item<int> "col1" 1 table1 |> should equal 2
    Table.Item<int> "col1" 2 table1 |> should equal 4
    (fun () -> Table.Item<int> "col1" 3 table1 |> ignore) |> should throw typeof<System.Exception>

[<Test; Category("CI")>]
let TableF_Transform_1() =
    let table = 
        Table.Empty 
        |> Table.Add "x" [0..10]
    let res = Table.Transform<_,_,int> ["x"] (fun (x:int[]) -> x |> Array.sum) table
    res |> should equal 55

[<Test; Category("CI")>]
let TableF_Transform_2() =
    let table = 
        Table.Empty 
        |> Table.Add "x" [0..10]
        |> Table.Add "y" [ for i in 0..10 do yield float(i) ]
    let res = Table.Transform<_,_,float> ["x"; "y"] (fun (x:int[]) (y:float[]) -> float(Array.sum x) + (Array.sum y)) table
    res |> should equal 110.0

[<Test; Category("CI")>]
let TableF_JoinTransform_1() =
    let table = 
        Table.Empty 
        |> Table.Add "x" [0..10]
    let res = table |> Table.JoinTransform ["x"] (fun (x:int[]) -> Table.New "y" (x |> Array.map float))
    res.Names |> should equal ["x"; "y"]
    [|for i in 0..10 do yield float i|] |> should equal (let col = Table.Column "y" res in Column.ToArray<float[]> col)
    
[<Test; Category("CI")>]
let TableF_JoinTransform_2() =
    let table = 
        Table.Empty 
        |> Table.Add "x" [0..10]
        |> Table.Add "y" [ for i in 0..10 do yield float(i) ]
    let res = table |> Table.JoinTransform ["x";"y"] (fun (x:int[]) (y:float[]) -> Table.New "z" (Array.zip x y |> Array.map (fun (s, t) -> float(s) + t)))
    res.Names |> Set.ofSeq |> should equal (Set.ofList ["x"; "y"; "z"])
    [|for i in 0..10 do yield 2.0*(float i)|] |> should equal (let col = Table.Column "z" res in Column.ToArray<float[]> col)


[<Test; Category("CI")>]
let ``Table.Add keeps order of columns as FIFO``() =
    let table = 
        Table.Empty 
        |> Table.Add "x" [1]
        |> Table.Add "y" [2]

    let x = table.Columns.[0]
    let y = table.Columns.[1]

    Assert.AreEqual("x", table |> Table.Name x)
    Assert.AreEqual("y", table |> Table.Name y)

    Assert.AreEqual(0, table |> Table.ColumnIndex "x")
    Assert.AreEqual(1, table |> Table.ColumnIndex "y")        



[<Property; Category("CI")>]
let ``Filters table rows by a single integer column`` (table: Table) (filterBy: int[]) =    
    let count = table.Count
    let filterBy = 
        if filterBy.Length > count then
            filterBy |> Seq.take count |> Seq.toArray
        elif filterBy.Length = count then
            filterBy
        else
            Array.init count (fun i -> if i < filterBy.Length then filterBy.[i] else 0)

    let table2 = table |> Table.Add "_filterBy_" filterBy
    
    let tableEven = table2 |> Table.Filter ["_filterBy_"] (fun d -> d % 2 = 0)

    let mask = filterBy |> Seq.mapi(fun i d -> (i,d)) |> Seq.filter(fun (i,d) -> d % 2 = 0) |> Seq.map fst |> Seq.toArray
    
    table.Columns 
    |> Seq.mapi(fun colInd col ->
        let original = Column.ToArray<System.Array> col
        let filtered = Column.ToArray<System.Array> tableEven.Columns.[colInd]
        mask
        |> Seq.mapi(fun iF iO -> System.Object.Equals(original.GetValue(iO), filtered.GetValue(iF)))
        |> Seq.forall id)
    |> Seq.forall id
        
