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

type RecLatLon = { Lat : float; Lon : float }
type ValidTypesRec = { TypeString:string; TypeInt:int; TypeFloat: float; TypeBool: bool; TypeDate: System.DateTime }
type InvalidTypesRec = { TypeDecimal:System.Decimal }
type [<Class>] PubPropClass() = 
    let mutable p : string = ""
    member x.Prop with get() : string = p and set(v) = p <- v

[<Property; Category("CI")>]
let ``Combination of functions ToRows and OfRows returns a record array identical to original`` (p:ValidTypesRec[]) =
    let table = Table.OfRows p
    let rows = table.ToRows<ValidTypesRec>() |> Seq.toArray
    Assert.AreEqual(p, rows)
    Assert.AreEqual(p, table.Rows |> Seq.toArray)
    Assert.AreEqual(table.RowsCount, table.Rows.Length)


[<Test; Category("CI")>]
let ``Table.ToRows for a record``() =
    let t = Table.OfColumns ([  Column.CreateString("TypeString", [|"Abcdef"|])
                                Column.CreateInt("TypeInt", [|45|])
                                Column.CreateReal("TypeFloat", [|185.0|])
                                Column.CreateBoolean("TypeBool", [|true|])
                                Column.CreateDate("TypeDate", [|System.DateTime(1950, 02, 03)|])])
    let persons = t.ToRows<ValidTypesRec>() |> Seq.toArray
    persons |> Seq.iter (fun p -> System.Diagnostics.Trace.WriteLine(sprintf "%A" p))
    Assert.AreEqual(1, persons.Length)
    Assert.AreEqual({TypeString = "Abcdef"; TypeInt = 45; TypeFloat = 185.0; TypeBool = true; TypeDate = System.DateTime(1950, 02, 03)}, persons.[0])

[<Test; Category("CI")>]
let ``Table.ToRows for a non-record``() =
    let t = Table.OfColumns ([  Column.CreateString("Prop", [|"A";"b"|])
                                Column.CreateInt("Prop2", [|45; 56|])]) // should be ignored since not presented in the target type
    let p = t.ToRows<PubPropClass>() |> Seq.toArray
    p |> Seq.iter (fun p -> System.Diagnostics.Trace.WriteLine(sprintf "%A" p))
    Assert.AreEqual(2, p.Length)
    Assert.AreEqual("A", p.[0].Prop, "item 0")
    Assert.AreEqual("b", p.[1].Prop, "item 1")

[<Test; Category("CI")>]
let ``Table.OfRows for a non-record``() =
    let rows = [PubPropClass(); PubPropClass()] 
    rows.[0].Prop <- "A"
    rows.[1].Prop <- "B"
    let t = Table.OfRows rows
    Assert.AreEqual(1, t.Count, "columns count")
    Assert.AreEqual(2, t.RowsCount, "rows count")
    Assert.AreEqual("A", t.[0].Rows.[0].AsString, "0,0")
    Assert.AreEqual("B", t.["Prop"].Rows.[1].AsString, "0,1")
    Assert.AreEqual("A", t.Rows.[0].Prop)
    Assert.AreEqual("B", t.Rows.[1].Prop)

[<Test; Category("CI"); ExpectedException(typeof<System.Collections.Generic.KeyNotFoundException>)>]
let ``Table.ToRows fails when table has no column for a property``() =
    let t = Table.OfColumns ([Column.CreateInt("Prop2", [|45; 56|])]) 
    t.ToRows<PubPropClass>() |> Seq.toArray |> ignore

[<Test; Category("CI"); ExpectedException(typeof<System.ArgumentException>)>]
let ``Table.ToRows fails when table has different type of column than the property``() =
    let t =
        Table.OfColumns ([Column.CreateInt("TypeString", [|123|]); Column.CreateInt("TypeInt", [|45|]); Column.CreateReal("TypeFloat", [|185.0|]);
                Column.CreateBoolean("TypeBool", [|true|]); Column.CreateDate("TypeDate", [|System.DateTime(1950, 02, 03)|])])
    t.ToRows<ValidTypesRec>() |> Seq.toArray |> ignore

[<Test; Category("CI"); ExpectedException(typeof<System.ArgumentException>)>]
let ``Table.ToRows fails when target property has invalid type``() =
    let t = Table.OfColumns ([Column.CreateInt("TypeDecimal", [|45; 56|])]) 
    t.ToRows<InvalidTypesRec>() |> Seq.toArray |> ignore

[<Test; Category("CI"); ExpectedException(typeof<System.ArgumentException>)>]
let ``Table.OfRows fails when property has invalid type``() =
    let rows = [| { TypeDecimal = System.Decimal(100) } |] 
    Table.OfRows rows |> ignore

[<Test; Category("CI")>]
let ``MapToColumn replaces existing column - one column to another existing column``() =
    let t = Table.OfColumns ([Column.CreateInt("a", [|0;1|]); Column.CreateInt("b", [|0;1|])])
    let t2 = Table.MapToColumn ["b"] "a" (fun a -> a + 1) t
    Assert.AreEqual(2, t2.Count, "number of columns")
    Assert.AreEqual(["b"; "a"], t2 |> colNames, "names of columns") // new 'a' added to the end
    Assert.AreEqual([|1;2|], t2.["a"].Rows.AsInt |> toArr, "array of t2.a")

[<Test; Category("CI")>]
let ``MapToColumn replaces existing column - one column to itself``() =
    let t = Table.OfColumns ([Column.CreateInt("a", [|0;1|]); Column.CreateInt("b", [|0;1|])])
    let t2 = Table.MapToColumn ["a"] "a" (fun a -> a + 1) t
    Assert.AreEqual(2, t2.Count, "number of columns")
    Assert.AreEqual(["b"; "a"], t2 |> colNames, "names of columns") // new 'a' added to the end
    Assert.AreEqual([|1;2|], t2.["a"].Rows.AsInt |> toArr, "array of t2.a")

[<Test; Category("CI")>]
let ``MapToColumn replaces existing column - 2 columns``() =
    let t = Table.OfColumns ([Column.CreateInt("a", [|0;1|]); Column.CreateInt("b", [|0;1|])])
    let t2 = Table.MapToColumn ["a"; "b"] "a" (fun a b -> a + b + 1) t
    Assert.AreEqual(2, t2.Count, "number of columns")
    Assert.AreEqual(["b"; "a"], t2 |> colNames, "names of columns") // new 'a' added to the end
    Assert.AreEqual([|1;3|], t2.["a"].Rows.AsInt |> toArr, "array of t2.a")

[<Test; Category("CI")>]
let TableF_MapiToColumn_ManyArgs() =
    let table:Table = 
        Table.Empty 
        |> Table.Add(Column.CreateInt("a", [|1|])) 
        |> Table.Add(Column.CreateInt("b", [|2|]))  
        |> Table.Add(Column.CreateInt("c", [|3|]))  
        |> Table.Add(Column.CreateInt("d", [|4|]))  
        |> Table.Add(Column.CreateInt("e", [|5|]))  
        |> Table.Add(Column.CreateInt("f", [|6|]))  
        |> Table.Add(Column.CreateInt("g", [|7|])) 
        |> Table.Add(Column.CreateInt("h", [|8|])) 
        |> Table.Add(Column.CreateInt("i", [|9|]))  
    let table2 = table |> Table.MapiToColumn ["a"; "b"; "c"; "d"; "e"; "f"; "g"; "h"; "i"] "$" (fun idx a b c d e f g h i -> idx)
    let col : int[] = table2.["$"].Rows.AsInt |> toArr
    Assert.AreEqual([|0|], col, "Array of the column '$' produced by MapiToColumn")

[<Test; Category("CI")>]
let TableF_MapiToColumn_OneArg() =
    let table:Table = Table.OfColumns ([Column.CreateInt("a", [|1|])])
    let table2 = table |> Table.MapiToColumn ["a"] "$" (fun idx (a:int) -> idx)
    let col : int[] = table2.["$"].Rows.AsInt |> toArr
    Assert.AreEqual([|0|], col, "Array of the column '$' produced by MapiToColumn")


[<Test; Category("CI")>]
let TableF_MapiToColumn_ZeroArg() =
    let table:Table = Table.OfColumns ([Column.CreateInt("a", [|1|])])
    let table2 = table |> Table.MapiToColumn [] "$" (fun idx -> idx)
    let col : int[] = table2.["$"].Rows.AsInt |> toArr
    Assert.AreEqual([|0|], col, "Array of the column '$' produced by MapiToColumn")

[<Test; Category("CI")>]
let TableF_MapToColumn_ManyArgs() =
    let table:Table = 
        Table.Empty 
        |> Table.Add(Column.CreateInt("a", [|1|])) 
        |> Table.Add(Column.CreateInt("b", [|2|]))  
        |> Table.Add(Column.CreateInt("c", [|3|]))  
        |> Table.Add(Column.CreateInt("d", [|4|]))  
        |> Table.Add(Column.CreateInt("e", [|5|]))  
        |> Table.Add(Column.CreateInt("f", [|6|]))  
        |> Table.Add(Column.CreateInt("g", [|7|])) 
        |> Table.Add(Column.CreateInt("h", [|8|])) 
        |> Table.Add(Column.CreateInt("i", [|9|]))  
    let table2 = table |> Table.MapToColumn ["a"; "b"; "c"; "d"; "e"; "f"; "g"; "h"; "i"] "$" (fun a b c d e f g h i -> true)
    let col : bool[] = table2.["$"].Rows.AsBoolean |> toArr
    Assert.AreEqual([|true|], col, "Array of the column '$' produced by MapToColumn")

[<Test; Category("CI")>]
let TableF_MapToColumn_OneArg() =
    let table:Table = Table.OfColumns ([Column.CreateInt("a", [|1|])])
    let table2 = table |> Table.MapToColumn ["a"] "$" (fun a -> a > 0)
    let col : bool[] = table2.["$"].Rows.AsBoolean |> toArr
    Assert.AreEqual([|true|], col, "Array of the column '$' produced by MapToColumn")


[<Test; Category("CI")>]
let TableF_MapToColumn_ZeroArg() =
    let table:Table = Table.OfColumns ([Column.CreateInt("a", [|1|])])
    let table2 = table |> Table.MapToColumn [] "$" (fun () -> true)
    let col : bool[] = table2.["$"].Rows.AsBoolean |> toArr
    Assert.AreEqual([|true|], col, "Array of the column '$' produced by MapToColumn")

[<Test; Category("CI")>]
let TableF_EmptyTable () =
    let table0:Table = Table.Empty
    table0 |> Seq.toList |> should equal []
    table0.Count |> should equal 0


[<Test; Category("CI")>]
let TableF_TestAddOneColumn() =
    let data:int[] = [| 1; 2; 4 |]
    let column0:Column = Column.CreateInt("col1", data)
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
    let table = Table.OfColumns ([Column.CreateInt("x", [|0..10|])])
    let res : int = Table.Transform ["x"] (fun (x:ImmutableArray<int>) -> x |> Seq.sum) table
    res |> should equal 55

[<Test; Category("CI")>]
let TableF_Transform_2() =
    let table = Table.OfColumns ([  Column.CreateInt("x", [|0..10|])
                                    Column.CreateReal("y", [| for i in 0..10 do yield float(i) |]) ])
    let res : float = Table.Transform ["x"; "y"] (fun (x:ImmutableArray<int>) (y:ImmutableArray<float>) -> float(Seq.sum x) + (Seq.sum y)) table
    res |> should equal 110.0

[<Test; Category("CI")>]
let TableF_AppendTransform_1() =
    let table = Table.OfColumns ([Column.CreateInt("x", [|0..10|])])
    let res = table |> Table.AppendTransform ["x"] (fun (x:ImmutableArray<int>) -> Table.OfColumns ([Column.CreateReal ("y", Seq.map float x |> Seq.toArray)]))
    res |> colNames |> should equal ["x"; "y"]
    [|for i in 0..10 do yield float i|] |> should equal (res.["y"].Rows.AsReal |> toArr)
    
[<Test; Category("CI")>]
let TableF_AppendTransform_2() =
    let table = Table.OfColumns ([ Column.CreateInt("x", [|0..10|])
                                   Column.CreateReal("y", [| for i in 0..10 do yield float(i) |]) ])
    let res = table |> Table.AppendTransform ["x";"y"] (fun (x:ImmutableArray<int>) (y:ImmutableArray<float>) -> Table.OfColumns ([Column.CreateReal ("z", Seq.zip x y |> Seq.map (fun (s, t) -> float(s) + t) |> Seq.toArray)]))
    res |> colNames |> Set.ofSeq |> should equal (Set.ofList ["x"; "y"; "z"])
    [|for i in 0..10 do yield 2.0*(float i)|] |> should equal (res.["z"].Rows.AsReal |> toArr)


[<Test; Category("CI")>]
let ``Table.Add keeps order of columns as FIFO``() =
    let table = Table.Empty |> Table.Add (Column.CreateInt("x", [|1|])) |> Table.Add (Column.CreateInt("y", [|2|]))
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
        if filterBy.Length > count then filterBy |> Seq.take count |> Seq.toArray
        elif filterBy.Length = count then filterBy
        else Array.init count (fun i -> if i < filterBy.Length then filterBy.[i] else 0)

    let table2 = table |> Table.Add (Column.CreateInt("_filterBy_", filterBy))
    let tableEven = table2 |> Table.Filter ["_filterBy_"] (fun d -> d % 2 = 0)

    let mask = filterBy |> Seq.mapi(fun i d -> (i,d)) |> Seq.filter(fun (_,d) -> d % 2 = 0) |> Seq.map fst |> Seq.toArray
    Seq.zip table (tableEven |> Table.Remove ["_filterBy_"])
    |> Seq.forall(fun (co, cf) ->
        (match co.Rows with
        | RealColumn v -> Column.CreateReal(co.Name, mask |> Array.map (fun i -> v.Value.[i]))
        | IntColumn v -> Column.CreateInt(co.Name, mask |> Array.map (fun i -> v.Value.[i]))
        | StringColumn v -> Column.CreateString(co.Name, mask |> Array.map (fun i -> v.Value.[i]))
        | BooleanColumn v -> Column.CreateBoolean(co.Name, mask |> Array.map (fun i -> v.Value.[i]))
        | DateColumn v -> Column.CreateDate(co.Name, mask |> Array.map (fun i -> v.Value.[i]))) |> areEqualColumnsForSerialization cf)


[<Test; Category("CI")>]
let ``Table.AddRows and Table.AddRow add rows to a typed table``() =
    let t = Table.OfRows([ {Lat=0.0;Lon=0.0}; {Lat=1.0;Lon=2.0} ])
    let t2 = t.AddRow({Lat=2.0;Lon=1.0}).AddRows([{Lat=3.0;Lon=4.0}])

    Assert.AreEqual(4, t2.RowsCount, "rows count")
    Assert.AreEqual(2, t2.Count, "columns count")
    Assert.AreEqual([|{Lat=0.0;Lon=0.0};{Lat=1.0;Lon=2.0};{Lat=2.0;Lon=1.0};{Lat=3.0;Lon=4.0}|], t2.Rows |> Seq.toArray, "rows")
    Assert.AreEqual([|0.0;1.0;2.0;3.0|], t2.["Lat"].Rows.AsReal |> Seq.toArray, "lat column by name")
    Assert.AreEqual([|0.0;1.0;2.0;3.0|], t2.[0].Rows.AsReal |> Seq.toArray, "lat column by index")
    Assert.AreEqual([|0.0;2.0;1.0;4.0|], t2.["Lon"].Rows.AsReal |> Seq.toArray, "lon column by name")
    Assert.AreEqual([|0.0;2.0;1.0;4.0|], t2.[1].Rows.AsReal |> Seq.toArray, "lon column by index")
