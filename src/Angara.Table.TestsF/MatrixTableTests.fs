module MatrixTableTests

open FsUnit
open NUnit.Framework
open FsCheck
open Angara.Data.TestsF.Common
open Angara.Data
open System.IO
open System.Collections.Immutable

[<Property(Verbose=false); Category("CI")>]
let ``Creates a matrix table from rows matrix``(matrix:int[,]) =
    let n,m = matrix.GetLength(0), matrix.GetLength(1)
    let immMatrix = 
        Seq.init n (fun irow -> ImmutableArray.Create<int>(matrix.[irow,*]))
        |> ImmutableArray.CreateRange
    let names = Seq.init m (fun i -> sprintf "column %d" i) |> ImmutableArray.CreateRange
    let t = Table.OfMatrix(names, immMatrix)

    Assert.AreEqual(m, t.Count, "columns count")
    Assert.AreEqual(names, t |> Seq.map(fun c -> c.Name), "column names")
    for icol in 0 .. t.Count-1 do
        Assert.AreEqual(matrix.[*,icol] |> Array.toSeq, t.[icol].Rows.AsInt, sprintf "Column %d" icol)

    Assert.AreEqual((if t.Count = 0 then 0 else n), t.RowsCount, "rows count")
    for irow in 0 .. t.RowsCount-1 do
        Assert.AreEqual(matrix.[irow,*] |> Array.toSeq, t.Matrix.[irow], sprintf "Row %d" irow)


[<Test; Category("CI")>]
let ``Adds a column and a row to a matrix table`` () = 
    let matrix = 
        ImmutableArray.Create<ImmutableArray<int>>
            [| ImmutableArray.Create<int> [|11;12;13|]
               ImmutableArray.Create<int> [|21;22;23|] |]

    let t = Table.OfMatrix (ImmutableArray.CreateRange ["a"; "b"; "c"], matrix)
    let t2 = t.AddColumn("d", ImmutableArray.Create<int>([|14;24|]))
              .AddRow(ImmutableArray.Create<int>([|31;32;33;34|]))

    Assert.AreEqual(4, t2.Count, "columns count")
    Assert.AreEqual(["a";"b";"c";"d"], t2 |> Seq.map(fun c -> c.Name), "column names")
    Assert.AreEqual([|11;12;13;14|] |> Array.toSeq, t2.Matrix.[0], "row 0")
    Assert.AreEqual([|21;22;23;24|] |> Array.toSeq, t2.Matrix.[1], "row 1")
    Assert.AreEqual([|31;32;33;34|] |> Array.toSeq, t2.Matrix.[2], "row 2")

    Assert.AreEqual([|11;21;31|] |> Array.toSeq, t2.[0].Rows.AsInt, "column 0")
    Assert.AreEqual([|12;22;32|] |> Array.toSeq, t2.[1].Rows.AsInt, "column 1")
    Assert.AreEqual([|13;23;33|] |> Array.toSeq, t2.[2].Rows.AsInt, "column 2")
    Assert.AreEqual([|14;24;34|] |> Array.toSeq, t2.[3].Rows.AsInt, "column 3")

