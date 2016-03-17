(*** hide ***)
#I __SOURCE_DIRECTORY__
#load "load-project-debug.fsx"
open Angara.Data
open System
open System.Collections.Generic
open System.Collections.Immutable
(**
# Angara.Data.Table (F#)

A table is an immutable collection of named columns. 
Column values are represented as lazy one-dimensional immutable array of one of the supported types. 
Heights of all columns in a table are equal.
Columns names are arbitrary strings;
duplicate names are allowed but may cause ambiguity in some API functions.
*)

(**
## Column

A column is represented as an immutable type `Angara.Data.Column` which
keeps column name, height and values. Column name cannot be a null string. *)

(*** include:typedef-Column ***)

(**
Column values are represented as an instance of discriminated union `Angara.Data.ColumnValues`:
*)

(*** include:typedef-ColumnValues ***)

(** The [ImmutableArray<'a>](https://msdn.microsoft.com/en-us/library/dn638264(v=vs.111).aspx)
structure represents an array that cannot be changed once it is created. Use of 
[Lazy<'a>](https://msdn.microsoft.com/en-us/library/dd233247.aspx) enables evaluation of
the column array on demand. 

There are several static functions to build a column from name and values:

- `Column.OfArray` creates a column from an array of one of the valid column types. If a mutable array is given, 
it is copied to guarantee immutability of the column; otherwise, if an immutable array is given, it is used without copying.*)
let cx = Column.OfArray ("x", [| for i in 0..99 -> float(i) / 10.0  |])
(**
- `Column.OfLazyArray` creates a column from a lazy immutable array of one of the valid types. This function requires a user to provide
a length of the given lazy array. Evalutation of the array will be performed when the column rows are first time accessed.*)
let cx' = Column.OfLazyArray ("x", lazy(ImmutableArray.CreateRange(seq{ for i in 0..99 -> float(i) / 10.0 })), 100)
(**
- `Colum.OfColumnValues` creates a column from an instance of `ColumnValues` discriminated union. This is a type safe function since
validity of the array type is checked on compilation; also this function allows to create a new column from values of another column.*)
let cx'' = Column.OfColumnValues ("x", RealColumn(lazy(ImmutableArray.CreateRange(seq{ for i in 0..99 -> float(i) / 10.0 }))), 100)

(**
### Getting Column Values

Generic tools usually do not expect a column to have a certain type, but must handle all possible types.
In this case, use `match` by value of the `Column.Rows` property to get column values.
The following example prints values of the column: *)

(*** define-output:print-rows ***)
match cx.Rows with
| RealColumn v -> printf "floats: %A" v.Value
| IntColumn v -> printf "ints: %A" v.Value
| StringColumn v -> printf "strings: %A" v.Value
| DateColumn v -> printf "dates: %A" v.Value
| BooleanColumn v -> printf "bools: %A" v.Value
(*** include-output:print-rows ***)

(**
When a column is expected to be of a certain type, use one of the functions `ColumnValues.AsReal`,
`ColumnValues.AsInt`, `ColumnValues.AsString`, `ColumnValues.AsDate`, `ColumnValues.AsBoolean`
which evaluate the column array (if it is not evaluated yet) and return the `ImmutableArray<'a>` instance, 
assuming that the column type corresponds the function;
otherwise, if the column type is incorrect, the function fails. 
*)

let x : ImmutableArray<float> = cx.Rows.AsReal

(*** include-value:x ***)

(** Also, the `ColumnValues` allows getting an individual data value by an index; again, 
there is a generic approach based on `match` and a succinct approach when a certain type is expected.

The following example returns a median of the ordered column `cx` when type is unknown:
*)

(*** define-output:print-rows-item ***)
match cx.Rows.[cx.Height / 2] with
| RealValue v -> printf "float: %f" v
| IntValue v -> printf "int: %d" v
| StringValue v -> printf "string: %s" v
| DateValue v -> printf "date: %A" v
| BooleanValue v -> printf "bool: %A" v
(*** include-output:print-rows-item ***)

(** The next example assumes that the column is real:*)
(*** define-output:print-rows-item2 ***)
printf "float: %f" (cx.Rows.[cx.Height / 2].AsReal)
(*** include-output:print-rows-item2 ***)


(**


## Table


The type `Angara.Data.Table` represents an immutable table. *)

(*** include:typedef-Table ***)

(**

### Table as Collection of Columns

The `Table.Empty` property returns an empty table, i.e. a table that has no columns.

A table can be created from a finite sequence of columns:
*)

let table = 
    Table.OfColumns
        [ Column.OfArray ("x", [| for i in 0..99 -> float(i) / 10.0  |])
          Column.OfArray ("sin(x)", [| for i in 0..99 -> sin (float(i) / 10.0) |]) ]

(**
To add a column to a table, you can use the static function `Table.Add` which creates 
a new table that has all columns of the original table appended with the given column.
Duplicate names are allowed. 
Normally, all columns of a table must have same height which is the table row count; 
if the new table column has different height, `Table.Add` fails.

In the following example the resulting `table` is identical to the `table` of the previous example:
*)

let table =
    Table.Empty 
    |> Table.Add (Column.OfArray ("x", [| for i in 0..99 -> float(i) / 10.0  |]))
    |> Table.Add (Column.OfArray ("sin(x)", [| for i in 0..99 -> sin (float(i) / 10.0) |]))

(** To remove columns from a table by names, you can use `Table.Remove`: *)

let table2 = table |> Table.Remove ["sin(x)"]

(** Alternatively, you can filter a table as a sequence of columns and create a new table instance. *)

(**

The `Table` implements the `IEnumerable<Column>` interface and exposes members
`Count` and `Item` that allow to get a count of the total number of columns in the table
and get a column by its index or name.

The following example prints a schema of the table without evalutation of the columns values:
*)
(*** define-output:table-as-seq ***)
table
|> Seq.iteri (fun colIdx col ->
    printfn "%d: %s of type %s" colIdx col.Name
                (match col.Rows with
                | RealColumn _    -> "float"
                | IntColumn _     -> "int"
                | StringColumn _  -> "string"
                | DateColumn _    -> "DateTime"
                | BooleanColumn _ -> "bool"))

(*** include-output:table-as-seq ***)  

(** 
The indexed properties `Table.Item` and `Table.TryItem` return a column by its index or name. If a column is not found,
an exception is thrown; if there are two or more columns with the given name,
the first column having the name is returned.

The example gets a name of a table column with index 1: *)

let col_name = table.[1].Name

(*** include-value:col_name ***)

(**
Next, we compute an average of the column named "sin(x)", assuming that it is real:
*)
let sin_avg = table.["sin(x)"].Rows.AsReal |> Seq.average

(*** include-value:sin_avg ***)

(**


### Table as Collection of Rows 



There are several ways how rows can be represented to construct a table. First is to use `Table.ofRecords` which builds a table
from a sequence of record type instances, when one instance is one row and record field is a column: *)

type SinX = { x: float; ``sin(x)``: float }

let tableSinX : Table<SinX> = 
    Table.OfRows [| for i in 0..99 -> { x = float(i) / 10.0; ``sin(x)`` = sin (float(i) / 10.0) } |]

(*** include-value:tableSinX ***)

(** 
The function `Table.OfRows<'r>` returns an instance of type `Table<'r>` inherited from `Table`, 
such that each public property of a given type `'r` 
becomes the table column with the name and type identical to the property;
each table row corresponds to an element of the input sequence with the order respected.
If the type `'r` is an F# record, the order of columns is identical to the record properties order.
If there is a public property having a type that is not valid for a table column, the function fails with an exception.
and each row is represented as an intance of the type `'r`, so that public properties of the type correspond to columns of the table.
*)

(** The type `Table<'r>` allows efficiently appending a table with new rows:
*)

let tableSinX' = tableSinX.AddRows [| for i in 100..199 -> { x = float(i) / 10.0; ``sin(x)`` = sin (float(i) / 10.0) } |]

(**
Second way is to use `Table.ofTuples2`, `Table.ofTuples3` etc which builds a table from a sequence of tuples,
when one tuple instance is one row and tuple elements are columns; columns names are given separately: *)
//  
//let tuples : (float*float*float)[] = [| (*...*) |]
//
//let tableWheat = Table.ofTuples3 ("lat", "lon", "wheat") tuples  
  
(** Third way is to use `Table.OfRows: columnNames:string seq -> rows:System.Array seq -> Table` which creates a table from 
a sequence of `System.Array` instances and a sequence of column names. *)

(**

A number of rows in the table is available through the property `Table.RowsCount`:
*)

(*** define-output:rowscount ***)
printf "Rows count: %d" table.RowsCount
(*** include-output:rowscount ***)

(**

There are three ways to perform row-wise data access:

* If table schema is known and can be represented as a record, you can use the generic function `Table.ToRows<'r>` which returns `'r seq`,
one instance of `'r` for each row.
* Get column values then do explicit slicing:
*)
let rows : (float*float) seq = 
    let x = table.["x"].Rows.AsReal
    let sinx = table.["sin(x)"].Rows.AsReal
    seq{ for i in 0..table.RowsCount-1 -> x.[i], sinx.[i] }

(*** include-value:rows ***)

(**
* Use helper function `Table.Map` which invokes the given function for each of the table rows and provides values of certain columns as arguments;
the result is a sequence of values returned by the function calls: 
*)
let rows' : (float*float) seq =
    table |> Table.Map ["x";"sin(x)"] (fun (x:float) (sinx:float) -> x, sinx)

(*** include-value:rows' ***)

(**

### Table as Matrix

Matrix table is represented as `Angara.Data.MatrixTable<'v>` inherited from `Angara.Data.Table`, where
type `'v` is a matrix value type, i.e. all columns of the table have same type which must be a valid column type.

To create a matrix table, use `Table.OfMatrix` and provide column names and the matrix as an array of rows:

*)

let matrix = 
    ImmutableArray.Create<ImmutableArray<int>>
        [| ImmutableArray.Create<int> [|11;12;13|]
           ImmutableArray.Create<int> [|21;22;23|] |]

let tableMatrix = Table.OfMatrix (ImmutableArray.CreateRange ["a"; "b"], matrix)

(**
Matrix table allows adding columns and rows using `AddRows`, `AddRow` and `AddColumns`, `AddColumn` functions:
*)

tableMatrix
    .AddColumn("c", ImmutableArray.Create<int> [|14;24;34|])
    .AddRow(ImmutableArray.Create<int> [|31;32;33;34|])

(**

## Save and Load

The `Table` type exposes static functions `Save` and `Load` to save and load a table in the delimited text format
in accordance with [RFC 4180](https://tools.ietf.org/html/rfc4180) but with extended set of delimiters: comma, tab, semicolon and space.

The `Table.Save` function saves a table to a file or using given `TextWriter`: *)

Table.Save(table, "table.csv")

(** The `table.csv` contains the following text: 

    x,sin(x)
    0,0
    0.1,0.099833416646828155
    0.2,0.19866933079506122
    0.3,0.29552020666133955
    ...
*)

(**
To load a table from a delimited text file, such as CSV file, or using given `TextReader`, you can call 
`Table.Load` function:
*)

let table = Table.Load("table.csv")

(** 
`Table.Load` performs columns types inference from text, but numeric values are always read as `float` 
and never as `int` to avoid ambiguity. If you need an integer column, you can provide custom settings to the 
`Load` function with specific `ColumnTypes` function.
*)

(** Typed load: *)

//let table = Table.Load<SinX> "table.csv"



(**Also there are overloaded functions `Load` and `Save` that allow to provide custom settings,
such as specific delimiter, header, support of null strings, and prefefined columns count and types.
*)

(**


## Table Operations



[Angara.Data.Table](angara-data-table.html) exposes a set of functions that should simplify a code
operating with tables, though payoff is that the type checking is performed in runtime.
*)

(**

### Duplicate Names Disambiguation 

All functions described below identify a column by its name. Thus duplicate names cause ambiguity which is implicitly resolved
by choosing the first column having the given name. Still you can explicitly resolve the ambiguity using one of the following approaches:

1. If only one of the columns is needed, then you can build a new table that 
has all columns excluding unnecessary. 
2. If multiple columns with same name are necessary, build a new table that has same columns but with unique names.

Both approaches do not cause any column data evaluation or copying.

For example, if `table` has several columns named `"x"` and you need only one with index 0,
create a table that contains the only needed column `"x"`:
*)

let table2 =
    Table( 
        table
        |> Seq.mapi (fun i c -> i, c)
        |> Seq.choose (fun (i, c) -> 
            match c.Name with
            | "x" when i <> 0 -> None
            | _ -> Some c))

(** Next example renames columns named `"x"` by appending the column index to the name: *)

let table3 =
    Table( 
        table
        |> Seq.mapi (fun i c -> 
            match c.Name with
            | "x" -> Column.OfColumnValues (sprintf "x (%d)" i, c.Rows, c.Height)
            | _ -> c))

(**
### Mapping Rows

#### Table.Map, Table.Mapi

The function `Table.Map` builds a sequence whose elements are the results of applying the given function to each of the rows of certain table columns.
`Table.Mapi` also provides an integer index passed to the function which indicates the index of row being transformed.

The signature is: `Map<'a,'b,'c> : columnNames:seq<string> -> map:('a->'b) -> table:Table -> 'c seq`

The generic function `map:'a->'b` is only partially defined. If `columnNames` contains:

* 0 columns, then `map:unit->'c`, so `'a = unit`, `'b = 'c`
* 1 column, then `map:'a->'c`, where `'a` is the type of the column, so `'b = 'c`
* 2 columns, then `map:'a->'d->'c`, where `'a` and `'d` are the types of the columns, so `'b = 'd->'c`
* 3 columns, then `map:'a->'d->'e->'c`, where `'a`, `'d` and `'e` are the types of the columns, so `'b = 'd->'e->'c`
* n...

The following example produces a sequence of multiplied values of columns `"x"` and `"sin(x)"` for each of the table rows:
*)

let xsinx : float seq = 
    table
    |> Table.Map ["x"; "sin(x)"] (fun (x:float) (sinx:float) -> x*sinx)    

(*** include-value:xsinx ***)


(**

#### Table.MapToColumn, Table.MapiToColumn

The function `Table.MapToColumn` builds a new table that contains all columns of the given table and
a new column or a replacement of an original table column (if there is an existing column with same name as the target name in the original table); 
elements of the column are the results of applying the given function to each of the rows of the given table columns. 
`Table.MapiToColumn` also provides an integer index passed to the function which indicates the index of row being transformed.

The signature is: `MapToColumn : columnNames:seq<string> -> newColumnName:string -> map:('a->'b) -> table:Table -> Table`

The generic function `map:'a->'b` is only partially defined. If `columnNames` contains:

* 0 columns, then `map:unit->'b`, so the new column type is `'b` and `'a = unit`
* 1 column, then `map:'a->'b`, where `'a` is the type of the source column, and `'b` is the new column type
* 2 columns, then `map:'a->'d->'c`, where `'a` and `'d` are the types of the source columns, so `'b = 'd->'c`, and `'c` is the new column type
* 3 columns, then `map:'a->'d->'e->'c`, where `'a`, `'d` and `'e` are the types of the source columns, so `'b = 'd->'e->'c`, and `'c` is the new column type
* n...

Ultimate result type of the `map` function must be valid column type: either `int`, `float`, `string`, `bool` or `System.DateTime`.

The following examples adds new table column named `"log(x)"` which contains logarithm of the column `"x"` value for each of the table rows:
*)

let tableLog = 
    table
    |> Table.MapToColumn ["x"] "log(x)" log

(*** include-value: tableLog ***)

(**
### Filtering Rows _to do_

The filtering functions return a new table containing all rows from a table where a predicate is true, 
while the predicate takes a set of columns.

`Table.Filter`
`Table.Filteri`

*)

(** 
The following example creates a table that contains only the rows of the `table` where value of 
the column `"x"` is between 0 and 1:
*)

let table_filter_x = table |> Table.Filter ["x"] (fun x -> x >= 0.0 && x <= 1.0) 

(*** include-value: table_filter_x ***)

(** 
To get a subset of table rows, use the function `Table.Filteri`:
*)

let table_10rows = table |> Table.Filteri [] (fun i -> i < 10)

(**
### Transforming and Appending Tables _to do_

`Table.Append`
`Table.Transform`
`Table.AppendTransform`

### Grouping Rows _to do_

`Table.GroupBy`

### Ordering Rows _to do_

`Table.OrderBy`

### Statistics _to do_

`Table.Summary`
`Table.TrySummary`
`Table.Correlation`
`Table.TryCorrelation`
`Table.Pdf`
`Table.TryPdf`

*)

(**
# Samples _to do_

## Titanic survivor analysis

The following example computes the survival rates for the different passenger classes.
The original data is taken from [https://www.kaggle.com/c/titanic](https://www.kaggle.com/c/titanic).
*)

(** Having the table functions: *)
//
//let GroupBy (colName : string) (projection : 'a -> 'b) (table : Table) : ('b * Table) seq =
//    Table.ToArray<'a[]> colName table 
//    |> Array.groupBy projection 
//    |> Seq.map(fun (key: 'b, _) ->
//        key, table |> Table.Filter [colName] (fun (v:'a) -> projection v = key))
//
//let OrderBy<'a,'b when 'b : comparison> (colName: string) (projection : 'a -> 'b) (table : Table) : Table =
//    let order = 
//        Table.ToArray<'a[]> colName table
//        |> Array.mapi (fun i v -> (i, projection v)) 
//        |> Array.sortBy snd |> Array.map fst
//    let cols =
//        table.Columns |> Seq.mapi(fun i c -> 
//            table.Names.[i],            
//            match Column.Type c with
//            | t when t = typeof<float> -> Column.New(lazy(let arr:float[] = Column.ToArray c in Array.init arr.Length (fun i -> arr.[order.[i]])))
//            | t when t = typeof<int> -> Column.New(lazy(let arr:int[] = Column.ToArray c in Array.init arr.Length (fun i -> arr.[order.[i]])))
//            | t when t = typeof<string> -> Column.New(lazy(let arr:string[] = Column.ToArray c in Array.init arr.Length (fun i -> arr.[order.[i]])))
//            | t when t = typeof<System.DateTime> -> Column.New(lazy(let arr:System.DateTime[] = Column.ToArray c in Array.init arr.Length (fun i -> arr.[order.[i]])))
//            | t when t = typeof<bool> -> Column.New(lazy(let arr:bool[] = Column.ToArray c  in Array.init arr.Length (fun i -> arr.[order.[i]])))
//            | _ -> failwith "Unexpected column type")
//    Table(cols)
//
//let OfTuples3<'a,'b,'c> (names: string*string*string) (rows : ('a*'b*'c) seq) : Table =
//    let na, nb, nc = names   
//    let ca, cb, cc = rows |> Seq.toArray |> Array.unzip3
//    Table([na; nb; nc], [Column.New ca; Column.New cb; Column.New cc])
//
//(** then - untyped solution: *)
//
//let survivors =         
//    Table.Load(@"data\titanic.csv",
//               { DelimitedFile.ReadSettings.Default with 
//                     ColumnTypes = Some(fun (_,name) -> match name with "Survived" | "Pclass"-> Some typeof<int> | _ -> None) })
//    |> GroupBy "Pclass" id 
//    |> Seq.map(fun (pclass:int, table) -> 
//        let stat = table |> Table.ToArray<int[]> "Survived" |> Array.countBy id |> Array.sortBy fst |> Array.map snd
//        pclass, stat.[0], stat.[1])
//    |> OfTuples3 ("Pclass", "Died", "Survived") 
//    |> Table.MapToColumn ["Died"; "Survived"] "Died" (fun (died:int) (survived:int) -> 100.0*(float died)/(float (died + survived)))
//    |> Table.MapToColumn ["Died"] "Survived" (fun (died:float) -> 100.0 - died)
//    |> OrderBy<int,int> "Pclass" id

(*** include-value: survivors ***)

(** Typed solution: *)

//type Passenger = { Pclass: int; Survived: int }
//type Survivors = { Pclass: int; Survived: float; Died: float }
//
//let survivors : Table<Survivors> =         
//    Table.Load<Passenger> @"data\titanic.csv"
//    |> GroupBy (fun (p:Passenger) -> p.Pclass) 
//    |> Seq.map(fun (pclass:int, table:Table<Passenger>) -> 
//        let stat = table?Survived |> Array.countBy id |> Array.sortBy fst |> Array.map snd
//        { Pclass = pclass; Survived = float(stat.[0]); Died = float(stat.[1]) })
//    |> OfRecords
//    |> Table.Map (fun (s:Survivors) -> 
//        { Pclass = pclass; 
//          Died = 100.0*s.Died/(s.Died + s.Survived)
//          Survived = 100.0*s.Survived/(s.Died + s.Survived))
//    |> OrderBy (fun (s:Survivors) -> s.Pclass)
//
//let pclass1 : Survivors = survivors.[0];

(*** include-value: survivors ***)

(*** define:typedef-Column ***)
type Column =
    /// Gets a name of the column.
    member Name : string with get
    /// Gets a number of rows in the column.
    member Height : int with get
    /// Returns column values.
    member Rows : ColumnValues with get

(*** define:typedef-ColumnValues ***)
type ColumnValues =
    | IntColumn     of Lazy<ImmutableArray<int>>
    | RealColumn    of Lazy<ImmutableArray<float>>
    | StringColumn  of Lazy<ImmutableArray<string>>
    | DateColumn    of Lazy<ImmutableArray<DateTime>>
    | BooleanColumn of Lazy<ImmutableArray<Boolean>>

(*** define:typedef-Table ***)
type Table = 
    new : columns : Column seq -> Table
    interface IEnumerable<Column> 
    /// Gets a count of the total number of columns in the table.
    member Count : int with get
    /// Gets a count of the total number of rows in the table.
    member RowsCount : int with get
    /// Gets a column by its index.
    member Item : index:int -> Column with get
    /// Gets a column by its name.
    /// If there are several columns with same name, returns the fist column having the name.
    member Item : name:string -> Column with get
    /// Tries to get a column by its index.
    member TryItem : index:int -> Column option with get
    /// Tries to get a column by its name.
    /// If there are several columns with same name, returns the fist column having the name.
    member TryItem : name:string -> Column option with get