module MatrixTableTests

open FsUnit
open NUnit.Framework
open FsCheck
open Angara.Data.TestsF.Common
open Angara.Data
open System.IO
open System.Collections.Immutable

[<Property(Verbose=false); Category("CI")>]
let ``Creates a matrix table from rows matrix and access its rows, columns and cells``(matrix:int[,]) =
    let n = matrix.GetLength(0)
    let m = if n = 0 then 0 else matrix.GetLength(1)
    let t = Table.OfMatrix (Seq.init n (fun irow -> matrix.[irow,*]))

    Assert.AreEqual(m, t.Count, "columns count")
    Assert.AreEqual(Seq.init m Table.DefaultColumnName, t |> Seq.map(fun c -> c.Name), "column names")
    for icol in 0 .. t.Count-1 do
        Assert.AreEqual(matrix.[*,icol] |> Array.toSeq, t.Columns.[icol], sprintf "Column %d" icol)
        Assert.AreEqual(matrix.[*,icol] |> Array.toSeq, t.[icol].Rows.AsInt, sprintf "Column %d" icol)

    Assert.AreEqual((if t.Count = 0 then 0 else n), t.RowsCount, "rows count")
    for irow in 0 .. t.RowsCount-1 do
        Assert.AreEqual(matrix.[irow,*] |> Array.toSeq, t.Rows.[irow], sprintf "Row %d" irow)
        for icol in 0 .. t.Count-1 do
            Assert.AreEqual(matrix.[irow,icol], t.[irow, icol], sprintf "Row %d col %d" irow icol)


[<Test; Category("CI")>]
let ``Adds a column and a row to a matrix table`` () = 
    let t = Table.OfMatrix ([| [|11;12;13|]; [|21;22;23|] |], ImmutableArray.CreateRange ["a"; "b"; "c"])
    let t' = Table.OfMatrix ([| [| 14 |]; [| 24 |] |], ImmutableArray.CreateRange ["d"])
    let t2 = (Table.AppendMatrix t t').AddRow(ImmutableArray.Create<int>([|31;32;33;34|]))

    Assert.AreEqual(4, t2.Count, "columns count")
    Assert.AreEqual(3, t2.RowsCount, "rows count")
    Assert.AreEqual(["a";"b";"c";"d"], t2 |> Seq.map(fun c -> c.Name), "column names")
    Assert.AreEqual([|11;12;13;14|] |> Array.toSeq, t2.Rows.[0], "row 0")
    Assert.AreEqual([|21;22;23;24|] |> Array.toSeq, t2.Rows.[1], "row 1")
    Assert.AreEqual([|31;32;33;34|] |> Array.toSeq, t2.Rows.[2], "row 2")

    Assert.AreEqual([|11;21;31|] |> Array.toSeq, t2.Columns.[0], "column 0")
    Assert.AreEqual([|12;22;32|] |> Array.toSeq, t2.Columns.[1], "column 1")
    Assert.AreEqual([|13;23;33|] |> Array.toSeq, t2.Columns.[2], "column 2")
    Assert.AreEqual([|14;24;34|] |> Array.toSeq, t2.Columns.[3], "column 3")