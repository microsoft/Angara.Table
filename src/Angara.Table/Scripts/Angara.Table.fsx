(*** hide ***)
#I __SOURCE_DIRECTORY__
#load "load-project-debug.fsx"
open Angara.Data
open System
open System.Collections.Generic
(**
# Angara.Data.Table (F#)

A **table** is a collection of named columns, where each **column** is one-dimensional array and lengths of all columns are equal.

Having the index less than columns length fixed, 
we define **row** as a one-dimensional array with length equal to number of the columns 
whose elements are elements of the columns having the chosen index, with the order of columns respected.
The chosen index is called a **row index**. Thus number of table rows equals to length of table columns.

Columns **names** are arbitrary strings which can be duplicated within a table.
*)

(** In the library, a column is represented as a discriminitated union `Angara.Data.Column`: *)

type Column =
    | IntColumn     of IRArray<int>
    | RealColumn    of IRArray<float>
    | StringColumn  of IRArray<string>
    | DateColumn    of IRArray<DateTime>
    | BooleanColumn of IRArray<Boolean>

(** 
Such representation enables typed operations on a column though it limits the set of valid column types.

A column instance is immutable due to use of the generic interface `IRArray<'a>` which represents a read-only array of elements
of type `'a`.
*)

type IRArray<'a> =
    inherit IReadOnlyList<'a>
    /// Copy a subset of this array to a new array
    abstract member Sub : startIndex:int * count:int -> 'a[]
    /// Copy the underlying array to a new array
    abstract member ToArray : unit -> 'a[]

(**
`IReadOnlyList<'a>` gives you a count of the total number of elements, general and typed iterators,
and direct access to each item.
In addition this interface allows for efficient copying of the entire data or a subset to a new array.
*)

(**
`Angara.Data.Table` type is immutable and can be constructed from a sequence of name and column pairs.
It exposes the `Names` and `Columns` as read-only lists of same length.
The `RowsCount` property returns the total number of rows in the table.
*)
   
type Table =
    /// Construct a table from a sequence of tuples of column names and columns.
    /// All columns must have same length.
    /// Names are arbitrary strings that can be duplicated.
    new : nameColumns:seq<string * Column> -> Table

    /// Return readonly list of column names
    member Names : IReadOnlyList<string> with get
    /// Return readonly list of columns
    member Columns : IReadOnlyList<Column> with get
    /// Return rows count 
    member RowsCount : int with get

(**
Example:
*)

let table = 
    Table(
        ["x",      RealColumn (RArray.ofArray [| for i in 0..99 -> float(i) / 10.0  |])
         "sin(x)", RealColumn (RArray.ofArray [| for i in 0..99 -> sin (float(i) / 10.0) |]) ])


(**
# Functional Operations on Tables
[Angara.Data.Table](angara-data-table.html) exploits functional approach and allows to use succinct code to perform complex operations on tables.

*)

(**
## Creating and Initializing Tables

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

(** Table can be constructed from a bunch of `System.Array` objects with corresponding column names; use `Table.FromArrays` function: *)

let table2 = Table.ofSeq [ "x", upcast [| 1; 2; 3 |]; "y", upcast [| 2; 4; 6 |]]

(** Supported column element types are listed in `Column.ValidTypes`: *)

(*** include-value: Column.ValidTypes ***)


(** Table.OfRows: columnNames:string seq -> rows:System.Array seq -> Table ` _to do_ *)


(** To remove columns from a table, use `Table.Remove`. *)


(**

To initialize a table from a delimited text file, such as CSV file, you can use 
`Table.Read` function:

*)

let tableWheat = Table.Read DelimitedFile.ReadSettings.Default @"data\wheat.csv"

(**
Now, `tableWheat.ToString()` returns the following string:
*)    
(*** include-value: tableWheat ***)

(**
It means that `tableWheat` has three columns with names `Lon`, `Lat`, `wheat`; number of rows is 691.
Also, for each column several first elements are printed.
*)

(** 

## Getting Arrays

There are two different views on a table: column-wise and row-wise. In the first case, you can get an array of a column using
`Table.ToArray` function.

The following examples gets an array of the column `wheat` and then computes its average value:

*)

let averageWheat = 
    tableWheat 
    |> Table.ToArray<float[]> "wheat"
    |> Array.average

(*** include-value: averageWheat ***)

(** 
To get a subset of an array, use `Table.Sub`. _Do we need it? It is possible to use Table.Filteri instead._
*)

(** _Do we need it? One can use the Table.Map() instead._
Also you might need rows of a table.
The following sample prints locations of points of the `tableWheat`: *)

let Rows2<'a,'b> (names:string*string) (t:Table) : ('a*'b) seq =
    let a,b = names
    let ca = t |> Table.ToArray<'a[]> a
    let cb = t |> Table.ToArray<'b[]> b
    seq{ for i in 0..ca.Length-1 -> ca.[i],cb.[i] }

let locationsWheat = 
    tableWheat 
    |> Rows2 ("Lat", "Lon") 
    |> Seq.map (fun (lat,lon) -> sprintf "%.2f, %.2f" lat lon)

(*** include-value: locationsWheat ***)

(**

## Transforming Tables

### Row-wise Operations

#### Mapping Operations

The function `Table.Map` builds a sequence whose elements are the results of applying the given function to each of the rows of certain table columns.
`Table.Mapi` also provides an integer index passed to the function which indicates the index of row being transformed.

The signature is: `Map<'a,'b,'c> : columnNames:seq<string> -> map:('a->'b) -> table:Table -> 'c seq`

The generic function `map:'a->'b` is only partially defined. If `columnNames` contains:

* 0 columns, map should be `map:unit->'c`, so `'a = unit`, `'b = 'c`
* 1 column, map should be `map:'a->'c`, where `'a` is the type of the column, so `'b = 'c`
* 2 columns, `map:'a->'d->'c`, where `'a` and `'d` are the types of the columns, so `'b = 'd->'c`
* 3 columns, `map:'a->'d->'e->'c`, where `'a`, `'d` and `'e` are the types of the columns, so `'b = 'd->'e->'c`
* n...

Using the `Table.Map` function, the example for `Rows2` function can be rewritten as follows:
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
#### Filtering Operations

`Table.Filter`
`Table.Filteri`

#### Grouping Operations

`Table.GroupBy` _to do_

### Table-wise Operations

`Table.Join`
`Table.Transform`
`Table.JoinTransform`

### Ordering Operations

`Table.OrderBy` _to do_

### Statistics Operations

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

let OfRows (columnNames : string seq) (rows : System.Array seq) : Table =
    let rows = rows |> Seq.toArray
    let cols =
        columnNames 
        |> Seq.mapi(fun iCol name ->
            name,
            match if rows.Length = 0 then typeof<float> else rows.[0].GetValue(iCol).GetType() with
            | t when t = typeof<float> -> Column.New(lazy(Array.init rows.Length (fun i -> rows.[i].GetValue(iCol) :?> float)))
            | t when t = typeof<int> -> Column.New(lazy(Array.init rows.Length (fun i -> rows.[i].GetValue(iCol) :?> int)))
            | t when t = typeof<string> -> Column.New(lazy(Array.init rows.Length (fun i -> rows.[i].GetValue(iCol) :?> string)))
            | t when t = typeof<System.DateTime> -> Column.New(lazy(Array.init rows.Length (fun i -> rows.[i].GetValue(iCol) :?> System.DateTime)))
            | t when t = typeof<bool> -> Column.New(lazy(Array.init rows.Length (fun i -> rows.[i].GetValue(iCol) :?> bool)))
            | _ -> failwith "Unexpected column type")
    Table(cols)

(** then *)

let survivors =         
    Table.Read { DelimitedFile.ReadSettings.Default with 
                     ColumnTypes = Some(fun (_,name) -> match name with "Survived" | "Pclass"-> Some typeof<int> | _ -> None) } 
               @"data\titanic.csv"
    |> GroupBy "Pclass" id 
    |> Seq.map(fun (pclass:int, table) -> 
        let stat = table |> Table.ToArray<int[]> "Survived" |> Array.countBy id |> Array.sortBy fst
        [| pclass; snd stat.[0]; snd stat.[1] |] :> System.Array)
    |> OfRows ["Pclass"; "Died"; "Survived"] 
    |> Table.MapToColumn ["Died"; "Survived"] "Died" (fun (died:int) (survived:int) -> 100.0*(float died)/(float (died + survived)))
    |> Table.MapToColumn ["Died"] "Survived" (fun (died:float) -> 100.0 - died)
    |> OrderBy<int,int> "Pclass" id

(*** include-value: survivors ***)