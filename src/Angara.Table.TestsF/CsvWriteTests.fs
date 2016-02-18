module CsvWriteTests

open System.IO
open Angara.Data
open NUnit.Framework
open Angara.Data.TestsF.Common
open FsCheck

let CheckReadWrite rs ws (table:Table) =
    System.Diagnostics.Trace.WriteLine(table)
    use ms = new MemoryStream()
    table |> Table.Write ws ms 
    ms.Position <- 0L
    let table2 = Table.Read rs ms
    System.Diagnostics.Trace.WriteLine(table2)
    areEqualTablesForCsv table table2 |> Assert.IsTrue

[<Property; Category("CI")>]
let ``When reading the written table we get a table that equals original table`` (table: Table) =
    let precondition (t:Table) =
        t.Columns |> Seq.forall(fun c -> Column.Type c <> typeof<int>) && // no integer columns
        t.Columns |> Seq.forall(fun c -> 
            (Column.Type c = typeof<string> &&
             let strings = Column.ToArray<string[]> c in 
                strings |> Seq.forall(fun s -> let r = ref 0.0 in System.Double.TryParse(s, r)) ||
                strings |> Seq.forall(fun s -> let r = ref (System.DateTime.MinValue) in System.DateTime.TryParse(s, r)) ||
                strings |> Seq.forall(fun s -> let r = ref true in System.Boolean.TryParse(s, r)))
            |> not ) && // no string columns that look as numbers, dates or booleans and therefore will be read as typed.
        t.Columns |> Seq.forall(fun c -> Column.Count c > 0 || Column.Type c = typeof<string>) && // no empty non-string columns (always read as string, if empty)
        (t.Columns.Count <> 1 || not(System.String.IsNullOrEmpty (Table.Name (t.Columns.[0]) t))) // a single column has non-empty name (otherwise first line is empty as if table is empty)

    precondition table ==> lazy(table |> CheckReadWrite { DelimitedFile.ReadSettings.Default with InferNullStrings = true } { DelimitedFile.WriteSettings.Default with AllowNullStrings = true } )

[<Test; Category("CI")>]
let ``When reading the written table we get a table that equals original table - case 7`` () =
    Table.New "a" [ "10" ] |> CheckReadWrite { DelimitedFile.ReadSettings.Default with DelimitedFile.ColumnTypes = Some(fun _ -> Some(typeof<string>)) } DelimitedFile.WriteSettings.Default

[<Test; Category("CI")>]
let ``When reading the written table we get a table that equals original table - case 6`` () =
    Table.New (new System.String([| '\009'; '\029'; 'o'; '\001'; 'w'; '\025' |])) [ false; true; false; false ]
    |> Table.Add "" [ true; false; true; true ]
    |> CheckReadWrite DelimitedFile.ReadSettings.Default DelimitedFile.WriteSettings.Default

[<Test; Category("CI")>]
let ``When reading the written table we get a table that equals original table - case 5`` () =
    Table.New (new System.String([| '\020' |])) [ System.DateTime(69, 8, 6, 14, 04, 56, 132) ]
    |> Table.Add "" [true] 
    |> CheckReadWrite DelimitedFile.ReadSettings.Default DelimitedFile.WriteSettings.Default

[<Test; Category("CI")>]
let ``When reading the written table we get a table that equals original table - case 4`` () =
    Table.New (new System.String([| '\009' |])) [ 1.0 + 2.0/3.0; 1.0 ] |> CheckReadWrite DelimitedFile.ReadSettings.Default DelimitedFile.WriteSettings.Default

[<Test; Category("CI")>]
let ``When reading the written table we get a table that equals original table - case 3`` () =
    Table.New "Prh" [ 2.0; -2.0; 0.0; 2.0; -1.0 ]
    |> Table.Add "" [ true; true; false; true; false ] 
    |> CheckReadWrite DelimitedFile.ReadSettings.Default DelimitedFile.WriteSettings.Default

[<Test; Category("CI")>]
let ``When reading the written table we get a table that equals original table - case 1`` () =
    Table.New "Q" Array.empty<string> |> CheckReadWrite DelimitedFile.ReadSettings.Default DelimitedFile.WriteSettings.Default

[<Test; Category("CI")>]
let ``When reading the written table we get a table that equals original table - case 2`` () =
    Table.New "Q" Array.empty<float> |> CheckReadWrite { DelimitedFile.ReadSettings.Default with DelimitedFile.ColumnTypes = Some(fun _ -> Some(typeof<float>)) } DelimitedFile.WriteSettings.Default

[<Test; Category("CI")>]
let ``When reading the written table we get a table that equals original table - empty column name`` () =
    Table.New "" [|-2.0|] |> CheckReadWrite DelimitedFile.ReadSettings.Default DelimitedFile.WriteSettings.Default
