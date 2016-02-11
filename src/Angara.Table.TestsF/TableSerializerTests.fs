module TableSerializer.Tests

open NUnit.Framework
open FsUnit
open FsCheck
open Angara.Data
open Angara.Serialization

open Angara.Data.TestsF.Common

let buildReinstateLib() = 
    let lib = SerializerLibrary("Reinstate")
    Angara.Data.TableSerializers.Register([lib])
    SerializerCompositeResolver([ lib; CoreSerializerResolver.Instance ])

let buildHtmlLib() = 
    let lib = SerializerLibrary("Html")
    Angara.Data.TableSerializers.Register([lib])
    SerializerCompositeResolver([ lib; CoreSerializerResolver.Instance ])

[<Test; Category("CI")>]
let ``Serialization of a table to Json``() =
    let table = 
        Table.New "int" [| 1; 2; 3 |]
        |> Table.Add "float" [| 1.1; 1.2; 1.3 |]
        |> Table.Add "string" [| "a"; "b"; "c" |]
        |> Table.Add "bool" [| true; false; true |]
        |> Table.Add "date" [| System.DateTime(2020, 1, 1); System.DateTime(2020, 1, 2); System.DateTime(2020, 1, 3) |]

    let lib = buildHtmlLib()
    let infoSet = table |> ArtefactSerializer.Serialize lib  
    let json = Angara.Serialization.Json.Marshal(infoSet, None)   
    System.Diagnostics.Trace.WriteLine(json)   
    Assert.AreEqual(":Table", json.First.Path); // todo: validate against schema so the expected json is as expected by TableViewer.show().


[<Test; Category("CI")>]
let ``Table with one float column is serialized``() =
    let data = [| 3.1415; 2.87; -1.0 |]
    let column = Column.New<float[]>(data)
    let table = Table.Empty |> Table.Add "col1" column

    let lib = buildReinstateLib()
    let table2 = table |> ArtefactSerializer.Serialize lib |> ArtefactSerializer.Deserialize lib :?> Table

    table2.Names |> should equal [| "col1" |]
    table2.Types |> should equal [| typeof<float> |]

    let column2 = Table.Column "col1" table2
    Column.Type column2 |> should equal typeof<float>
    Column.Count column2 |> should equal 3
    Column.TryItem<float> 0 column2 |> should equal (Some(3.1415))
    Column.TryItem<float> 1 column2 |> should equal (Some(2.87))
    Column.TryItem<float> 2 column2 |> should equal (Some(-1.0))

[<Test; Category("CI")>]
let ``Serialization of empty table`` () =
    let table = Table.Empty

    let lib = buildReinstateLib()    
    let table2 = table |> ArtefactSerializer.Serialize lib |> ArtefactSerializer.Deserialize lib :?> Table

    Assert.AreEqual(0, table2.Names.Count)
    Assert.AreEqual(0, table2.Columns.Count)

[<Test; Category("CI")>]
let ``Serialization of a table with a cell containing string 'null'`` () =
    let table =  Table.Empty |> Table.Add ":'}" [| null; "" |]
    
    let lib = buildReinstateLib()    
    let table2 = table |> ArtefactSerializer.Serialize lib |> ArtefactSerializer.Deserialize lib :?> Table

    Assert.AreEqual(1, table2.Names.Count)
    Assert.AreEqual(1, table2.Columns.Count)
    Assert.AreEqual(":'}", table2.Names.[0])
    let deserializedArr = table2 |> Table.ToArray<string[]> ":'}";
    Assert.AreEqual([| null; "" |], deserializedArr)

[<Test; Category("CI")>]
let ``Serialization of table with empty column name`` () =
    let table = 
        Table.Empty
        |> Table.Add "" [|true;true;true;false|]
        |> Table.Add "VxD    " [|-2.0; -2.0; System.Double.NaN; -2.666667|]
        |> Table.Add "\9K*" [|true;false;false;false|]
    let lib = buildReinstateLib()
    let table2 = table |> ArtefactSerializer.Serialize lib |> ArtefactSerializer.Deserialize lib :?> Table
    Assert.IsTrue(Angara.Data.TestsF.Common.areEqualTablesForCsv table table2)


[<Property; Category("CI")>]
let ``A deserialized serialized table is identical to the original table`` (table: Table) =
    let lib = buildReinstateLib()
    let table2 = table |> ArtefactSerializer.Serialize lib |> ArtefactSerializer.Deserialize lib :?> Table
    Angara.Data.TestsF.Common.areEqualTablesForSerialization table table2

[<Test; Category("CI"); ExpectedException>]
let ``Adding a column with different length than existing`` () =
    let _ = 
        Table.Empty 
        |> Table.Add "a" [| 1 |]
        |> Table.Add "b" [| 1; 2 |]
    ()
    
[<Test; Category("CI"); ExpectedException>]
let ``Creating a table from two columns with different lengths`` () =
    let _ = new Table(
                    [ "a", Column.New [| 1 |]
                    ; "b", Column.New [| 1; 2|]])
    ()