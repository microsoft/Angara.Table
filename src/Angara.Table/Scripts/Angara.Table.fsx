(*** hide ***)
#I __SOURCE_DIRECTORY__
#load "load-project-debug.fsx"
open Angara.Data
open System
open System.Collections.Generic
open System.Collections.Immutable
(**
# Angara.Data.Table (F#)

A **table** is an immutable collection of named columns. A **column** is one-dimensional immutable array of one of the supported 
types. Heights of all columns in a table are equal.
Columns **names** are arbitrary strings. A table column can be identified by both name and index.
Duplicate names are allowed but may cause ambiguity in some API functions.
*)

(** A column is represented as a discriminitated union `Angara.Data.Column`: 
*)

type Column =
    | IntColumn     of Lazy<ImmutableArray<int>>
    | RealColumn    of Lazy<ImmutableArray<float>>
    | StringColumn  of Lazy<ImmutableArray<string>>
    | DateColumn    of Lazy<ImmutableArray<DateTime>>
    | BooleanColumn of Lazy<ImmutableArray<Boolean>>

(** The [ImmutableArray<'a>](https://msdn.microsoft.com/en-us/library/dn638264(v=vs.111).aspx)
structure represents an array that cannot be changed once it is created. Use of 
[Lazy<'a>](https://msdn.microsoft.com/en-us/library/dd233247.aspx) enables evaluation of
the column array on demand.

The type `Angara.Data.Table` represents an immutable table:
*)
   
type Table =
    new : nameColumns : (string*Column) list -> Table
    new : nameColumns : (string*Column) list * rowsCount:int -> Table
    
    member ColumnsCount : int with get    
    member RowsCount : int with get
    
    member Column : index:int -> Column
    member Column : name:string -> Column
    
    member Columns : (string*Column) list with get

(**
`Table` can be constructed from a list of name and column pairs.
Then the rows count is determined on the first access to the `RowsCount` property.
If there is a column whose lazy array already has value, its length is taken; otherwise, the first column is evaluated.

If evaluation of a column takes significant time and the number of rows is known on table construction, 
you should use the second constructor to provide the `rowsCount` argument.

The example builds a table from a list of names and columns:
*)

let table = 
    Table(
        ["x",      RealColumn (lazy(ImmutableArray.Create [| for i in 0..99 -> float(i) / 10.0  |]))
         "sin(x)", RealColumn (lazy(ImmutableArray.Create [| for i in 0..99 -> sin (float(i) / 10.0) |])) ])

(**
The `Table.Columns` property allows listing columns names and types without forcing evaluation of lazy arrays; 
for example, the following code prints the table schema:
*)

table.Columns
|> List.iteri (fun colIdx (name, col) ->
    printf "%d: %s of type %s" colIdx name
        match col with
        | RealColumn _    -> "float"
        | IntColumn _     -> "int"
        | StringColumn _  -> "string"
        | DateColumn _    -> "DateTime"
        | BooleanColumn _ -> "bool")

(** 
The methods `Table.Column` return a column by its index or name. If a column is not found,
an exception is thrown; if there are two or more columns with the given name,
the first column having the name is returned.
*)

(**
The `Table` type exposes column-wise data access because it enables type safe code.
The following example computes an average of a column `wheat`: *)

let av = 
    match table.Column "wheat" with
    | RealColumn a -> Seq.average a.Value
    | _ -> failwith "Unexpected type of column" 

(**
There are three ways to perform row-wise access:

* If table schema is known and can be represented as a record, use the generic type `Table<'r>` which exposes property 
`Rows : 'r list`.
* Create type safe code by accessing columns elements at certain index:
*)
    let rowsLatLon : (float*float)[] = 
        match table.Column "lat", table.Column "lon" with
        | RealColumn lat, RealColumn lon -> [| i in 0..table.RowsCount-1 -> lat.Value.[i], lon.Value.[i] |]
        | _ -> failwith "Unexpected type"    
(**
* Use helper function `Table.Map` which enables succinct code but with runtime type check: 
*)
    let rowsLatLon : (float*float)[] =
        table
        |> Table.Map ["lat";"lon"] (fun lat lon -> lat, lon)

(**
If table schema is known at compile time, the generic table type `Type<'r>` can be used.
The type `'r` defines structure of a table and instance of `'r` represents a single table row.
The type `'r` must be sealed and all its public read-only instance properties are considered as table columns with 
corresponding names. The columns are ordered in an alphabetical order.

The `Table<'r>` instance can be built from a list of instances of `'r`:
*)

type Table<'r> inherit Table =
    new : 'r list -> Table<'r>
    member Rows : 'r list
    
(**
Arrays of columns are computed using reflection on demand. 
*)

(**
## Table Operations

[Angara.Data.Table](angara-data-table.html) exposes a number of functions that should simplify the code
operating with tables though payoff for some of them is that the type checking is performed in runtime.
*)

(**
All functions described below identify a column by its name. Thus duplicate names cause ambiguity which is implicitly resolved
by using the first column having the given name. Still you can explicitly resolve the ambiguity using following approaches:

1. If only one of the columns is needed, then you can build a new table that 
has all columns except those which are not needed. 
2. If several columns with same name are needed, build a new table that has same columns but give unique names 
to the columns with duplicate names.

Both approaches do not cause any column data evaluation or copying.

For example, if `table` has several columns named `wheat` and you need only one with index `wheatIdx`,
create a table that contains only one needed column `wheat`:
*)

let table2 =
    Table( 
        table.Columns
        |> Seq.mapi (fun i x -> i, x)
        |> Seq.choose (fun (i,(n,c)) -> 
            match n with
            | "wheat" when i <> wheatIdx -> None
            | _ -> Some(n,c)))

(** Next example renames columns named `wheat` by appending their index to the name: *)

let table3 =
    Table( 
        table.Columns
        |> Seq.mapi (fun i (n,c) -> 
            match n with
            | "wheat" -> sprintf "wheat (%d)" i, c
            | _ -> n, c))

(**
### Constructing from Columns

The `Table.Empty` property returns an empty table, i.e. a table that has no columns.
*)
open Angara.Data

let tableEmpty = Table.Empty

(**
To build a table from arrays (or other kinds of sequences) representing table columns, 
use `Table.Empty` and `Table.Add` functions. 
Normally, all columns of a table must have same number of elements; otherwise, `Table.Add` fails.

The following example creates
a table with two columns `"x"` and `"y"` with data given as arrays of floats:
*)

let table =
    Table.Empty 
    |> Table.Add "x" [| 1; 2; 3 |]
    |> Table.Add "y" [| 2; 4; 6 |]

(** To remove columns from a table, use `Table.Remove`. *)


(**
### Constructing from Rows 

There are several ways how rows can be represented to construct a table. First is to use `Table.ofRecords` which builds a table
from a sequence of record type instances, when one instance is one row and record field is a column: *)

type Wheat = { lat: float; lon: float; wheat: float }
let records : Wheat[] = [| (* ... *) |]

let tableWheat = Table.ofRecords records

(**
Second way is to use `Table.ofTuples2`, `Table.ofTuples3` etc which builds a table from a sequence of tuples,
when one tuple instance is one row and tuple elements are columns; columns names are given separately: *)
  
let tuples : (float*float*float)[] = [| (*...*) |]

let tableWheat = Table.ofTuples3 ("lat", "lon", "wheat") tuples  
  
(** Third way is to use `Table.OfRows: columnNames:string seq -> rows:System.Array seq -> Table` which creates a table from 
a sequence of `System.Array` instances and a sequence of column names. *)


(**

### Save and load

The `Table` exposes functions to load and save a table in the delimited text format
in accordance with [RFC 4180](https://tools.ietf.org/html/rfc4180) but with extended set of delimiters: comma, tab, semicolon and space.

To load a table from a delimited text file, such as CSV file, you can use 
`Table.Load` function:

*)

let tableWheat = Table.Load @"data\wheat.csv"

(** or typed: *)

let tableWheat = Table.Load<Wheat> @"data\wheat.csv"

(**
The `Table.Save` function saves a table to a file or stream: *)

Table.Save (tableWheat, "wheat.csv")

(**Also there are overloaded functions `Load` and `Save` that allow to provide custom settings: *)

type SaveSettings = 
    { /// Determines which character will delimit columns.
      Delimiter : Delimiter
      /// If true, writes null strings as an empty string and an empty string as double quotes (""), 
      /// so that these cases could be distinguished; otherwise, if false, throws an exception if null is 
      /// in a string data array.
      AllowNullStrings : bool 
      /// If true, the first line will contain names corresponding to the columns of the table.
      /// Otherwise, if false, the first line is a data line.
      SaveHeader: bool }
    /// Uses comma as delimiter, saves a header, and disallows null strings.
    static member Default : WriteSettings

type LoadSettings = 
    { /// Determines which character delimits columns.
      Delimiter : Delimiter
      /// If true, double quotes ("") are considered as empty string and an empty string is considered as null; 
      /// otherwise, if false, both cases are considered as an empty string.
      InferNullStrings : bool
      /// If true, the first line is considered as a header of the table.
      /// This header will contain names corresponding to the fields in the file
      /// and should contain the same number of fields as the records in
      /// the rest of the file. Otherwise, if false, the first line is a data line and columns are named as 
      /// A, B, C, ..., Z, AA, AB... .
      HasHeader: bool
      /// An optional value that allows to provide an expected number of columns. If number of columns differs, the reading fails.
      ColumnsCount : int option
      /// An optional value that allows a user to specify element types for some of columns. In particular this allows
      /// reading integer columns since automatic inference always uses Double type for numeric values.
      ColumnTypes : (int * string -> System.Type option) option }
    /// Expects comma as delimiter, has header, doesn't infer null strings, and doesn't predefine column count or types.
    static member Default : ReadSettings

(**
### Getting Data

There are two different views on a table: column-wise and row-wise. In the first case, you can get column elements using
`Table.ToArray` or `Table.ToSeq` functions. The former builds and returns a copy of a column array to 
guarantee immutability of the table; the latter doesn't create a copy and enumerates column elements.

The following examples computes an average of the column `wheat`:

*)

let averageWheat = 
    tableWheat 
    |> Table.ToSeq<float> "wheat"
    |> Seq.average

(*** include-value: averageWheat ***)

(** To get row-wise access to a table, use `Table.Map` or `Table.Mapi`.
The following sample gets a sequence of tuples containing latitides and longitudes of each table row: *)

let locationsWheat : (float*float) seq = 
    tableWheat 
    |> Table.Map ["Lat"; "Lon"] (fun lat lon -> lat,lon)

(*** include-value: locationsWheat ***)

(**
Typed `Table<'a>` exposes indexing property `Rows` which returns a row as a typed instance: *)

for i = 0..tableWheat.RowsCount-1 do
    let row = tableWheat.Rows.[i] 
    printf "%f, %f" row.Lat row.Lon

(**

### Mapping Rows

The function `Table.Map` builds a sequence whose elements are the results of applying the given function to each of the rows of certain table columns.
`Table.Mapi` also provides an integer index passed to the function which indicates the index of row being transformed.

The signature is: `Map<'a,'b,'c> : columnNames:seq<string> -> map:('a->'b) -> table:Table -> 'c seq`

The generic function `map:'a->'b` is only partially defined. If `columnNames` contains:

* 0 columns, map should be `map:unit->'c`, so `'a = unit`, `'b = 'c`
* 1 column, map should be `map:'a->'c`, where `'a` is the type of the column, so `'b = 'c`
* 2 columns, `map:'a->'d->'c`, where `'a` and `'d` are the types of the columns, so `'b = 'd->'c`
* 3 columns, `map:'a->'d->'e->'c`, where `'a`, `'d` and `'e` are the types of the columns, so `'b = 'd->'e->'c`
* n...

The following example prints locations for each row of the table:
*)

let locationsWheat2 : string seq = 
    tableWheat 
    |> Table.Map ["Lat"; "Lon"] (sprintf "%.2f, %.2f")    

(*** include-value: locationsWheat2 ***)

(**
The function `Table.MapToColumn` builds a new table that contains all columns of the given table and
a new column or a replacement of an original table column (if there is an existing column with same name as the target name in the original table); 
elements of the column are the results of applying the given function to each of the rows of the given table columns. 
`Table.MapiToColumn` also provides an integer index passed to the function which indicates the index of row being transformed.

The signature is: `MapToColumn : columnNames:seq<string> -> newColumnName:string -> map:('a->'b) -> table:Table -> Table`

The generic function `map:'a->'b` is only partially defined. If `columnNames` contains:

* 0 columns, map should be `map:unit->'b`, so the new column type is `'b` and `'a = unit`
* 1 column, map should be `map:'a->'b`, where `'a` is the type of the source column, and `'b` is the new column type
* 2 columns, `map:'a->'d->'c`, where `'a` and `'d` are the types of the source columns, so `'b = 'd->'c`, and `'c` is the new column type
* 3 columns, `map:'a->'d->'e->'c`, where `'a`, `'d` and `'e` are the types of the source columns, so `'b = 'd->'e->'c`, and `'c` is the new column type
* n...

Ultimate result type of the map function must be valid column type: either `int`, `float`, `string`, `bool` or `System.DateTime`.

The following examples adds new table column named "log(wheat)" which contains logarithm of wheat for each row:
*)

let tableLogWheat = 
    tableWheat 
    |> Table.MapToColumn ["wheat"] "log(wheat)" log

(*** include-value: tableLogWheat ***)

(**
### Filtering Rows

The filtering functions return a new table containing all rows from a table where a predicate is true, 
where the predicate takes a set of columns.

`Table.Filter`
`Table.Filteri`

*)

(** 
To get a subset of table rows, use the function `Table.Filteri':
*)

let tableWheat_10rows = tableWheat |> Table.Filteri [] (fun i -> i < 10)

(**
### Transforming and Joining Tables

`Table.Join`
`Table.Transform`
`Table.JoinTransform`

### Grouping Rows

`Table.GroupBy` _to do_

### Ordering Rows 

`Table.OrderBy` _to do_

### Statistics

`Table.Summary`
`Table.TrySummary`
`Table.Correlation`
`Table.TryCorrelation`
`Table.Pdf`
`Table.TryPdf`

*)

(**
# Samples

## Titanic survivor analysis

The following example computes the survival rates for the different passenger classes.
The original data is taken from [https://www.kaggle.com/c/titanic](https://www.kaggle.com/c/titanic).
*)

(** Having the table functions: *)

let GroupBy (colName : string) (projection : 'a -> 'b) (table : Table) : ('b * Table) seq =
    Table.ToArray<'a[]> colName table 
    |> Array.groupBy projection 
    |> Seq.map(fun (key: 'b, _) ->
        key, table |> Table.Filter [colName] (fun (v:'a) -> projection v = key))

let OrderBy<'a,'b when 'b : comparison> (colName: string) (projection : 'a -> 'b) (table : Table) : Table =
    let order = 
        Table.ToArray<'a[]> colName table
        |> Array.mapi (fun i v -> (i, projection v)) 
        |> Array.sortBy snd |> Array.map fst
    let cols =
        table.Columns |> Seq.mapi(fun i c -> 
            table.Names.[i],            
            match Column.Type c with
            | t when t = typeof<float> -> Column.New(lazy(let arr:float[] = Column.ToArray c in Array.init arr.Length (fun i -> arr.[order.[i]])))
            | t when t = typeof<int> -> Column.New(lazy(let arr:int[] = Column.ToArray c in Array.init arr.Length (fun i -> arr.[order.[i]])))
            | t when t = typeof<string> -> Column.New(lazy(let arr:string[] = Column.ToArray c in Array.init arr.Length (fun i -> arr.[order.[i]])))
            | t when t = typeof<System.DateTime> -> Column.New(lazy(let arr:System.DateTime[] = Column.ToArray c in Array.init arr.Length (fun i -> arr.[order.[i]])))
            | t when t = typeof<bool> -> Column.New(lazy(let arr:bool[] = Column.ToArray c  in Array.init arr.Length (fun i -> arr.[order.[i]])))
            | _ -> failwith "Unexpected column type")
    Table(cols)

let OfTuples3<'a,'b,'c> (names: string*string*string) (rows : ('a*'b*'c) seq) : Table =
    let na, nb, nc = names   
    let ca, cb, cc = rows |> Seq.toArray |> Array.unzip3
    Table([na; nb; nc], [Column.New ca; Column.New cb; Column.New cc])

(** then - untyped solution: *)

let survivors =         
    Table.Load(@"data\titanic.csv",
               { DelimitedFile.ReadSettings.Default with 
                     ColumnTypes = Some(fun (_,name) -> match name with "Survived" | "Pclass"-> Some typeof<int> | _ -> None) })
    |> GroupBy "Pclass" id 
    |> Seq.map(fun (pclass:int, table) -> 
        let stat = table |> Table.ToArray<int[]> "Survived" |> Array.countBy id |> Array.sortBy fst |> Array.map snd
        pclass, stat.[0], stat.[1])
    |> OfTuples3 ("Pclass", "Died", "Survived") 
    |> Table.MapToColumn ["Died"; "Survived"] "Died" (fun (died:int) (survived:int) -> 100.0*(float died)/(float (died + survived)))
    |> Table.MapToColumn ["Died"] "Survived" (fun (died:float) -> 100.0 - died)
    |> OrderBy<int,int> "Pclass" id

(*** include-value: survivors ***)

(** Typed solution: *)

type Passenger = { Pclass: int; Survived: int }
type Survivors = { Pclass: int; Survived: float; Died: float }

let survivors : Table<Survivors> =         
    Table.Load<Passenger> @"data\titanic.csv"
    |> GroupBy (fun (p:Passenger) -> p.Pclass) 
    |> Seq.map(fun (pclass:int, table:Table<Passenger>) -> 
        let stat = table?Survived |> Array.countBy id |> Array.sortBy fst |> Array.map snd
        { Pclass = pclass; Survived = float(stat.[0]); Died = float(stat.[1]) })
    |> OfRecords
    |> Table.Map (fun (s:Survivors) -> 
        { Pclass = pclass; 
          Died = 100.0*s.Died/(s.Died + s.Survived)
          Survived = 100.0*s.Survived/(s.Died + s.Survived))
    |> OrderBy (fun (s:Survivors) -> s.Pclass)

let pclass1 : Survivors = survivors.[0];

(*** include-value: survivors ***)