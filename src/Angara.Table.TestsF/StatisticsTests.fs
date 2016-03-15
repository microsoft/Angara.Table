module StatisticsTests

open Angara.Data
open NUnit.Framework

[<Test; Category("CI")>]
let ``Correlation of columns in a table``() =
    let t = Table([ Column.OfArray("x", [| for i in 0..99 -> float(i) |])
                    Column.OfArray("y", [| for i in 0..99 -> 2*i+1 |])
                    Column.OfArray("z", [| for i in 0..99 -> i.ToString() |]) ])
    let nms, corr = TableStatistics.Correlation t
    Assert.AreEqual([|"x";"y"|], nms, "names")
    Assert.AreEqual([| [| 1.0 |] |], corr, "correlations")

