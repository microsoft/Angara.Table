module CsvWriteTests

open System.IO
open Angara.Data
open NUnit.Framework
open Angara.Data.TestsF.Common
open FsCheck


[<Property; Category("CI")>]
let ``When reading the written table we get a table that equals original table`` (table: Table) =
    let precondition (t:Table) =
        t.Columns |> Seq.forall(fun c -> Column.Type c <> typeof<int>) && // no integer columns
        t.Columns |> Seq.forall(fun c -> Column.Count c > 0 || Column.Type c = typeof<string>) && // no empty non-string columns (always read as string, if empty)
        (t.Columns.Count <> 1 || not(System.String.IsNullOrEmpty (Table.Name (t.Columns.[0]) t))) // a single column has non-empty name (otherwise first line is empty as if table is empty)

    let property (table:Table) = 
        use ms = new MemoryStream()
        table |> Table.Write { DelimitedFile.WriteSettings.Default with AllowNullStrings = true } ms
        ms.Position <- 0L
        let table2 = Table.Read { DelimitedFile.ReadSettings.Default with InferNullStrings = true } ms
        areEqualTablesForCsv table table2

    precondition table ==> lazy(property table)

[<Test; Category("CI")>]
let ``When reading the written table we get a table that equals original table - case 6`` () =
    let table = 
        Table.New (new System.String([| '\009'; '\029'; 'o'; '\001'; 'w'; '\025' |])) [ false; true; false; false ]
        |> Table.Add "" [ true; false; true; true ]

    System.Diagnostics.Trace.WriteLine(table)
    use ms = new MemoryStream()
    table |> Table.Write DelimitedFile.WriteSettings.Default ms 
    ms.Position <- 0L
    let table2 = Table.Read DelimitedFile.ReadSettings.Default ms
    System.Diagnostics.Trace.WriteLine(table2)
    areEqualTablesForCsv table table2 |> Assert.IsTrue

[<Test; Category("CI")>]
let ``When reading the written table we get a table that equals original table - case 5`` () =
    let table = 
        Table.New (new System.String([| '\020' |])) [ System.DateTime(69, 8, 6,14, 04, 56, 132) ]
        |> Table.Add "" [true] 

    System.Diagnostics.Trace.WriteLine(table)
    use ms = new MemoryStream()
    table |> Table.Write DelimitedFile.WriteSettings.Default ms 
    ms.Position <- 0L
    let table2 = Table.Read DelimitedFile.ReadSettings.Default ms
    System.Diagnostics.Trace.WriteLine(table2)
    areEqualTablesForCsv table table2 |> Assert.IsTrue

[<Test; Category("CI")>]
let ``When reading the written table we get a table that equals original table - case 4`` () =
    let table = 
        Table.New (new System.String([| '\009' |])) [ 1.0 + 2.0/3.0; 1.0 ] 
    System.Diagnostics.Trace.WriteLine(table)
    use ms = new MemoryStream()
    table |> Table.Write DelimitedFile.WriteSettings.Default ms 
    ms.Position <- 0L
    let table2 = Table.Read DelimitedFile.ReadSettings.Default ms
    System.Diagnostics.Trace.WriteLine(table2)
    areEqualTablesForCsv table table2 |> Assert.IsTrue

[<Test; Category("CI")>]
let ``When reading the written table we get a table that equals original table - case 3`` () =
    let table = 
        Table.New "Prh" [ 2.0; -2.0; 0.0; 2.0; -1.0 ] |>
        Table.Add "" [ true; true; false; true; false ]

    use ms = new MemoryStream()
    table |> Table.Write DelimitedFile.WriteSettings.Default ms 
    ms.Position <- 0L
    let table2 = Table.Read DelimitedFile.ReadSettings.Default ms
    areEqualTablesForCsv table table2 |> Assert.IsTrue

[<Test; Category("CI")>]
let ``When reading the written table we get a table that equals original table - case 1`` () =
    let table = Table.New "Q" Array.empty<string>
    use ms = new MemoryStream()
    table |> Table.Write DelimitedFile.WriteSettings.Default ms 
    ms.Position <- 0L
    let table2 = Table.Read DelimitedFile.ReadSettings.Default ms
    areEqualTablesForCsv table table2 |> Assert.IsTrue

[<Test; Category("CI")>]
let ``When reading the written table we get a table that equals original table - case 2`` () =
    let table = Table.New "Q" Array.empty<float>
    use ms = new MemoryStream()
    table |> Table.Write DelimitedFile.WriteSettings.Default ms 
    ms.Position <- 0L
    let table2 = Table.Read DelimitedFile.ReadSettings.Default ms
    Assert.AreEqual(1, table2.Columns.Count, "columns count")
    Assert.AreEqual(0, table2.Count, "rows count")
    Assert.AreEqual("Q", table2.Names.[0], "name")

[<Test; Category("CI")>]
let ``When reading the written table we get a table that equals original table - empty column name`` () =
    use ms = new MemoryStream()
    let table = Table.New "" [|-2.0|]
    table |> Table.Write DelimitedFile.WriteSettings.Default ms 
    ms.Position <- 0L
    let table2 = Table.Read DelimitedFile.ReadSettings.Default ms
    Assert.IsTrue(areEqualTablesForCsv table table2)
