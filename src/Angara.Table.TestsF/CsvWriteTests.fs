module CsvWriteTests

open System.IO
open Angara.Data
open NUnit.Framework
open Angara.Data.TestsF.Common
open FsCheck

let CheckReadWrite rs ws (table:Table) =
    System.Diagnostics.Trace.WriteLine(table)
    use ms = new MemoryStream()
    Table.Save(table, new StreamWriter(ms), ws)
    ms.Position <- 0L
    let table2 = Table.Load(new StreamReader(ms), rs)
    System.Diagnostics.Trace.WriteLine(table2)
    areEqualTablesForCsv table table2 |> Assert.IsTrue

[<Property; Category("CI")>]
let ``When reading the written table we get a table that equals original table`` (table: Table) =
    let precondition (t:Table) =
        t |> Seq.forall(fun c -> match c.Rows with IntColumn _ -> false | _ -> true) && // no integer columns
        t |> Seq.forall(fun c -> 
            ((match c.Rows with StringColumn _ -> true | _ -> false) &&
             let strings = c.Rows.AsString in 
                strings |> Seq.forall(fun s -> let r = ref 0.0 in System.Double.TryParse(s, r)) ||
                strings |> Seq.forall(fun s -> let r = ref (System.DateTime.MinValue) in System.DateTime.TryParse(s, r)) ||
                strings |> Seq.forall(fun s -> let r = ref true in System.Boolean.TryParse(s, r)))
            |> not ) && // no string columns that look as numbers, dates or booleans and therefore will be read as typed.
        t |> Seq.forall(fun c -> c.Height > 0 || (match c.Rows with StringColumn _ -> true | _ -> false)) && // no empty non-string columns (always read as string, if empty)
        (t.Count <> 1 || not(System.String.IsNullOrEmpty t.[0].Name)) // a single column has non-empty name (otherwise first line is empty as if table is empty)

    precondition table ==> lazy(table |> CheckReadWrite { DelimitedFile.ReadSettings.Default with InferNullStrings = true } { DelimitedFile.WriteSettings.Default with AllowNullStrings = true } )

[<Test; Category("CI")>]
let ``When reading the written table we get a table that equals original table - case 8`` () =
    Table.Empty 
    |> Table.Add (Column.OfArray("0&", [| true; true |])) 
    |> Table.Add (Column.OfArray("", [| null; "" |])) 
    |> CheckReadWrite { DelimitedFile.ReadSettings.Default with InferNullStrings = true } { DelimitedFile.WriteSettings.Default with AllowNullStrings = true }


[<Test; Category("CI")>]
let ``When reading the written table we get a table that equals original table - case 7`` () =
    Table.Empty |> Table.Add (Column.OfArray("a", [| "10" |])) |> CheckReadWrite { DelimitedFile.ReadSettings.Default with DelimitedFile.ColumnTypes = Some(fun _ -> Some(typeof<string>)) } DelimitedFile.WriteSettings.Default

[<Test; Category("CI")>]
let ``When reading the written table we get a table that equals original table - case 6`` () =
    Table.Empty 
    |> Table.Add (Column.OfArray(new System.String([| '\009'; '\029'; 'o'; '\001'; 'w'; '\025' |]), [| false; true; false; false |]))
    |> Table.Add (Column.OfArray("", [| true; false; true; true |]))
    |> CheckReadWrite DelimitedFile.ReadSettings.Default DelimitedFile.WriteSettings.Default

[<Test; Category("CI")>]
let ``When reading the written table we get a table that equals original table - case 5`` () =
    Table.Empty 
    |> Table.Add (Column.OfArray(new System.String([| '\020' |]), [| System.DateTime(69, 8, 6, 14, 04, 56, 132) |]))
    |> Table.Add(Column.OfArray("", [|true|]))
    |> CheckReadWrite DelimitedFile.ReadSettings.Default DelimitedFile.WriteSettings.Default

[<Test; Category("CI")>]
let ``When reading the written table we get a table that equals original table - case 4`` () =
    Table.Empty |> Table.Add (Column.OfArray(new System.String([| '\009' |]), [| 1.0 + 2.0/3.0; 1.0 |])) |> CheckReadWrite DelimitedFile.ReadSettings.Default DelimitedFile.WriteSettings.Default

[<Test; Category("CI")>]
let ``When reading the written table we get a table that equals original table - case 3`` () =
    Table.Empty 
    |> Table.Add (Column.OfArray("Prh", [| 2.0; -2.0; 0.0; 2.0; -1.0 |]))
    |> Table.Add(Column.OfArray("", [| true; true; false; true; false |] ))
    |> CheckReadWrite DelimitedFile.ReadSettings.Default DelimitedFile.WriteSettings.Default

[<Test; Category("CI")>]
let ``When reading the written table we get a table that equals original table - case 1`` () =
    Table.Empty |> Table.Add (Column.OfArray("Q", Array.empty<string>)) |> CheckReadWrite DelimitedFile.ReadSettings.Default DelimitedFile.WriteSettings.Default

[<Test; Category("CI")>]
let ``When reading the written table we get a table that equals original table - case 2`` () =
    Table.Empty |> Table.Add (Column.OfArray("Q", Array.empty<float>)) |> CheckReadWrite { DelimitedFile.ReadSettings.Default with DelimitedFile.ColumnTypes = Some(fun _ -> Some(typeof<float>)) } DelimitedFile.WriteSettings.Default

[<Test; Category("CI")>]
let ``When reading the written table we get a table that equals original table - empty column name`` () =
    Table.Empty |> Table.Add (Column.OfArray("", [|-2.0|])) |> CheckReadWrite DelimitedFile.ReadSettings.Default DelimitedFile.WriteSettings.Default
