module StatisticsTests

open Angara.Data
open NUnit.Framework

[<Test; Category("CI")>]
let ``Correlation of columns in a table``() =
    let t = Table.OfColumns([   Column.CreateReal("x", [| for i in 0..99 -> float(i) |])
                                Column.CreateInt("y", [| for i in 0..99 -> 2*i+1 |])
                                Column.CreateString("z", [| for i in 0..99 -> i.ToString() |]) ])
    let nms, corr = TableStatistics.Correlation t
    Assert.AreEqual([|"x";"y"|], nms, "names")
    Assert.AreEqual([| [| 1.0 |] |], corr, "correlations")

