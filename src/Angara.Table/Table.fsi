namespace Angara.Data

open System
open System.Collections
open System.Collections.Generic

/// IRArray - The data in a column to be accessed in read only fashion.
/// IReadOnlyList gives you a count of the total number of elements,
/// general and typed iterators,
/// and direct access to each item.
/// In addition this interface allows for efficient copying of the entire
/// data or a subset to a new array.
[<ReflectedDefinition>]
[<Interface>]
type IRArray<'a> =
    inherit IReadOnlyList<'a>

    /// Copy a subset of this array to a new array
    abstract member Sub : startIndex:int -> count:int -> 'a[]

    /// Copy the array to a new array
    abstract member ToArray : unit -> 'a[]

/// IRArrayAdapter - Efficiently pretend one IRArray<'a> is another IRArray<'b> 
/// by casting each element only when accessed, using cast function
[<ReflectedDefinition>]
type IRArrayAdapter<'a, 'b> =
    interface IRArray<'b>

    /// Create a new IRArrayAdapter from an IRArray and cast function
    new : ass:IRArray<'a> * cast:('a->'b) -> IRArrayAdapter<'a, 'b>

/// RArray - An implementation of IRArray backed by an array.
[<ReflectedDefinition>]
type RArray<'a> =
    interface IRArray<'a>

    /// Create a new RArray from an array.
    /// Note that this doesn't copy the given array and thus if the array changes, the RArray also changes,
    /// but it is not possible to change the array through this class
    new : ass:'a[] -> RArray<'a>

    /// Create a new RArray from a sequence.
    /// This performs Seq.toArray so does make a copy and so is less efficient.
    new : ass:seq<'a> -> RArray<'a>

/// LazyRArray - A read only wrapper around a typed lazy array.
[<ReflectedDefinition>]
type LazyRArray<'a> =
    interface IRArray<'a>

    /// Create a new LazyRArray from a Lazy array
    new : ass:Lazy<'a[]> -> LazyRArray<'a>



/// RealColumnSummary - Basic statistics for columns containing numeric data
[<ReflectedDefinition>]
type RealColumnSummary = {
    Min: float
    /// Lower bound of 95-th percentile.
    Lb95: float
    /// Lower bound of 68-th percentile.
    Lb68: float
    Median: float
    /// Upper bound of 68-th percentile.
    Ub68: float
    /// Upper bound of 95-th percentile.
    Ub95: float
    Max: float
    Mean: float
    Variance: float
    /// Total number of elements in the column.
    TotalRows: int
    /// Number of elements in the column except for NaNs.
    DataRows: int
}

/// ComparableColumnSummary - Simple statistics for columns containing non-numeric data
[<ReflectedDefinition>]
type ComparableColumnSummary<'a when 'a : comparison> = {
    /// A minimum value of the column.
    Min: 'a
    /// A maximum value of the column.
    Max: 'a
    /// Total number of elements in the column.
    TotalRows: int
    /// Number of elements in the column except for missing values,
    /// which is null or empty string.
    DataRows: int
}

/// ComparableColumnSummary - Simple statistics for columns containing non-numeric data
[<ReflectedDefinition>]
type BooleanColumnSummary = {
    /// Number of rows with value "true"
    TrueRows: int
    /// Number of rows with value "false"
    FalseRows: int
}

/// Discriminated Union type holding statistics for each column type
[<ReflectedDefinition>]
type ColumnSummary =
    /// Statistics for int and real columns
    | NumericColumnSummary of RealColumnSummary
    /// Statistics for string columns
    | StringColumnSummary of ComparableColumnSummary<string>
    /// Statistics for DateTime columns
    | DateColumnSummary of ComparableColumnSummary<DateTime>
    /// Statistics for Boolean columns
    | BooleanColumnSummary of BooleanColumnSummary



/// Column - An abstract definition of a column, use its static members to make operations over it.
[<ReflectedDefinition>]
[<NoComparison>]
[<Sealed>]
type Column =
    /// Return a new column
    /// 'a may be one of:
    ///     Column, IRArray<'b>, 'b[], seq<'b>, Lazy<'b[]>, System.Array with element type 'b
    /// where 'b may be one of:
    ///     int, float, string, DateTime, bool
    static member New<'a> : data:'a -> Column
    
    static member ValidTypes : Type[] with get

    /// Return the column content's type
    static member Type : column:Column -> Type
    
    /// Return a copy of a range of the column contents if column is correct type
    /// 'a may be one of:
    ///     Column, IRArray<'b>, 'b[], Array
    /// where 'b may be one of:
    ///     int, float, string, DateTime, bool
    static member TrySub<'a> : startIndex:int -> count:int -> column:Column -> 'a option

    /// Return a copy of a range of the column contents if column is correct type
    /// 'a may be one of:
    ///     Column, IRArray<'b>, 'b[], Array
    /// where 'b may be one of:
    ///     int, float, string, DateTime, bool
    static member Sub<'a> : startIndex:int -> count:int -> column:Column -> 'a

    /// Return the column contents if column is correct type
    /// 'a may be one of:
    ///     IRArray<'b>, 'b[], Array
    /// where 'b may be one of:
    ///     int, float, string, DateTime, bool
    static member TryToArray<'a> : column:Column -> 'a option

    /// Return the column contents if column is correct type
    /// 'a may be one of:
    ///    IRArray<'b>, 'b[], Array
    /// where 'b may be one of:
    ///     int, float, string, DateTime, bool
    static member ToArray<'a> : column:Column -> 'a

    /// Return the column content's count
    static member Count : column:Column -> int

    /// Return the minimum count across all columns
    static member MinimumCount : columns:seq<Column> -> int

    /// Return the column content's typed enumerator
    static member GetEnumerator<'a> : column:Column -> IEnumerator<'a>

    /// Return the column content's enumerator
    static member GetEnumerator : column:Column -> IEnumerator

    /// Return an item from the column contents, if in range and the correct type
    static member TryItem<'a> : index:int -> column:Column -> 'a option

    /// Return an item from the column contents, if in range and the correct type
    static member Item<'a> : index:int -> column:Column -> 'a
    
    /// Apply a map function to a set of items from a set of columns contents
    /// The generic map function is only partially defined
    /// If there are:
    ///     1 column, map should be map:('a->'c), where 'a is the type of the column, so 'b = 'c
    ///     2 columns, map('a->'d->'c), where 'a and 'd are the types of the columns, so 'b = 'd->'c
    ///     3 columns, map('a->'d->'e->'c), where 'a, 'd and 'e are the types of the columns, so 'b = 'd->'e->'c
    ///     n...
    /// If one input column is shorter than the other then the remaining elements of the longer column are ignored.
    static member Map<'a,'b,'c> : map:('a->'b) -> columns:seq<Column> -> 'c[]

    /// Apply a map function to a set of items from a set of columns contents
    /// The generic map function is only partially defined
    /// If there are:
    ///     1 column, map should be map:(int->'a->'c), where 'a is the type of the column, so 'b = 'c
    ///     2 columns, map:(int->'a->'d->'c), where 'a and 'd are the types of the columns, so 'b = 'd->'c
    ///     3 columns, map:(int->'a->'d->'e->'c), where 'a, 'd and 'e are the types of the columns, so 'b = 'd->'e->'c
    ///     n...
    static member Mapi<'a,'b,'c> : map:(int->'a->'b) -> columns:seq<Column> -> 'c[]

    /// Return a PDF of the column contents if the column contents are numeric
    static member TryPdf : pointCount:int -> column:Column -> (float[] * float[]) option

    /// Return a PDF of the column contents if the column contents are numeric
    static member Pdf : pointCount:int -> column:Column -> (float[] * float[])

    /// Return a float view of an int column
    static member IntToRealArray : ir:IRArray<int> -> IRArray<float>

    /// Return a float view of a column if the column content's type is numeric
    static member TryToRealArray : column:Column -> IRArray<float> option

    /// Return a float view of a column if the column content's type is numeric
    static member ToRealArray : column:Column -> IRArray<float>

    /// Combine a binary mask with a column to create a new column with the same type
    /// As in Seq.zip mask and column contents need not have the same length
    static member Select : mask:seq<bool> -> column:Column -> Column

    /// Return some simple statistical properties of an IRArray of ints
    static member Summary : i:IRArray<int> -> RealColumnSummary

    /// Return some simple statistical properties of an IRArray of floats
    static member Summary : a:IRArray<float> -> RealColumnSummary

    /// Return some simple statistical properties of an IRArray of strings
    static member Summary : a:seq<string> -> ComparableColumnSummary<string>

    /// Return some simple statistical properties of an IRArray of DateTimes
    static member Summary : a:seq<DateTime> -> ComparableColumnSummary<DateTime>
    
    /// Return some simple statistical properties of an IRArray of Booleans
    static member Summary : a:seq<Boolean> -> BooleanColumnSummary

    /// Return some simple statistical properties of a column
    static member Summary : column:Column -> ColumnSummary



[<ReflectedDefinition>]
/// Table - A readonly collection of named columns.
type Table =

    /// Default, empty constructor
    new : unit -> Table

    /// Construct a table from a sequence of column names and a sequence of columns
    /// Sequences must have the same length or an exception will be thrown
    /// All columns must have same number of rows.
    new : names:seq<string> * columns:seq<Column> -> Table

    /// Construct a table from a sequence of tuples of column names and columns
    /// All columns must have same number of rows.
    new : nameColumns:seq<string * Column> -> Table

    /// Return readonly list of column names
    member Names : IReadOnlyList<string> with get

    /// Return readonly list of columns
    member Columns : IReadOnlyList<Column> with get

    /// Return rows count 
    member Count : int with get

    /// Return readonly list of column types
    member Types : IReadOnlyList<Type> with get

    /// Create a new, empty table
    static member Empty : Table

    /// Creates an new table with a single column
    /// 'a may be one of:
    ///     Column, IRArray<'b>, 'b[], seq<'b>, Lazy<'b[]>
    static member New<'a> : columnName:string -> columnData:'a -> Table

    /// Creates a new table from named arrays.
    /// Lengths of the arrays must be equal.
    /// Type of array elements must be one of: 
    ///     int, float, string, DateTime, bool
    static member FromArrays: (string * Array) seq -> Table

    /// Gets name of a column.
    static member Name: column:Column -> table:Table -> string

    /// Gets name of a column.
    static member TryName: column:Column -> table:Table -> string option

    /// Return a new table with an additional column
    /// 'a may be one of:
    ///     Column, IRArray<'b>, 'b[], seq<'b>, Lazy<'b[]>
    /// where 'b may be one of:
    ///     int, float, string, DateTime, bool
    /// Length of the data must be equal to the Table.Count, if there is at least one column in the table.
    static member Add<'a> : name:string -> data:'a -> table:Table -> Table

    /// Return a new table that has all columns of the given table except those that have the given names.
    static member Remove : columnNames:seq<string> -> table:Table -> Table

    /// Try to find the index of a column in a table by name
    static member TryColumnIndex :columnName:string ->  table:Table -> int option

    /// Try to find the index of a column in a table by name
    static member ColumnIndex : columnName:string -> table:Table -> int

    /// Try to find a column in a table by name
    static member TryColumn : columnName:string -> table:Table -> Column option

    /// Try to find a column in a table by name
    static member Column : columnName:string -> table:Table -> Column

    /// Return the column content's type
    static member TryType : columnName:string -> table:Table -> Type option

    /// Return the column content's type
    static member Type : columnName:string -> table:Table -> Type

    /// Return a copy of a range of the column contents, if the correct type
    /// 'a may be one of:
    ///     Column, IRArray<'b>, 'b[], Array
    /// where 'b may be one of:
    ///     int, float, string, DateTime, bool
    static member TrySub<'a> : columnName:string -> startIndex:int -> count:int -> table:Table -> 'a option

    /// Return a copy of a range of the column contents, if the correct type
    /// 'a may be one of:
    ///     Column, IRArray<'b>, 'b[], Array
    /// where 'b may be one of:
    ///     int, float, string, DateTime, bool
    static member Sub<'a> : columnName:string -> startIndex:int -> count:int -> table:Table -> 'a

    /// Return a copy of the column contents, if the correct type
    /// 'a may be one of:
    ///     Column, IRArray<'b>, 'b[], Array
    /// where 'b may be one of:
    ///     int, float, string, DateTime, bool
    static member TryToArray<'a> : columnName:string -> table:Table -> 'a option

    /// Return a copy of the column contents, if the correct type
    /// 'a may be one of:
    ///     Column, IRArray<'b>, 'b[], Array
    /// where 'b may be one of:
    ///     int, float, string, DateTime, bool
    static member ToArray<'a> : columnName:string -> table:Table -> 'a

    /// Return the column content's enumerator
    static member GetEnumeratorT<'a> : columnName:string -> table:Table -> IEnumerator<'a>

    /// Return the column content's enumerator
    static member GetEnumerator : columnName:string -> table:Table -> IEnumerator

    /// Return an item from the column contents, if in range and the correct type
    static member TryItem<'a> : columnName:string -> index:int -> table:Table -> 'a option

    /// Return an item from the column contents, if in range and the correct type
    static member Item<'a> : columnName:string -> index:int -> table:Table -> 'a

    /// Return a new table containing all rows from a table where a predicate is true, where the predicate takes a set of columns
    /// The generic predicate function is only partially defined
    /// If there are:
    ///     1 column, predicate should be predicate:('a->bool), where 'a is the type of the column
    ///     2 columns, predicate:('b->'c->bool), where 'b and 'c are the types of the columns, so 'a = 'b->'c
    ///     3 columns, predicate:('b->'c->'d->bool), where 'b, 'c and 'd are the types of the columns, so 'a = 'b->'c->'d
    ///     n...
    static member Filter<'a> : columnNames:seq<string> -> predicate:('a->bool) -> table:Table -> Table

    /// Return a new table containing all rows from a table where a predicate is true, where the predicate takes a set of columns and row index
    /// The generic predicate function is only partially defined
    /// If there are:
    ///     1 column, predicate should be predicate:(int->'a->bool), where 'a is the type of the column
    ///     2 columns, predicate:(int->'b->'c->bool), where 'b and 'c are the types of the columns, so 'a = 'b->'c
    ///     3 columns, predicate:(int->'b->'c->'d->bool), where 'b, 'c and 'd are the types of the columns, so 'a = 'b->'c->'d
    ///     n...
    static member Filteri<'a> : columnNames:seq<string> -> predicate:(int->'a->bool) -> table:Table -> Table

    /// Returns a new table that contains columns of both given tables. Duplicate column names are allowed.
    static member Join : table1:Table -> table2:Table -> Table

    /// Returns a new table that contains all columns of the given table and columns that are the result of applying the given function
    /// to certain columns of the given table.
    /// The generic transform function is only partially defined
    /// If there are:
    ///     1 column, transform should be transform:('a->Table)
    ///     2 columns, transform:('b->'c->Table), so 'a = 'b->'c
    ///     3 columns, transform:('b->'c->'d->Table), so 'a = 'b->'c->'d
    ///     n...
    /// Each of these types may be one of Column, T[], IRArray<T> or Array, possibly different T's
    static member JoinTransform<'a,'b> : columnNames:seq<string> -> transform:('a->'b) -> table:Table -> Table

    /// Returns an array of objects. There will be one object for each row, the result of applying the given function
    /// to the given columns of each row of the table.
    /// The generic map function is only partially defined
    /// If there are:
    ///     1 column, map should be map:('a->'c), where 'a is the type of the column, so 'b = 'c
    ///     2 columns, map:('a->'d->'c), where 'a and 'd are the types of the columns, so 'b = 'd->'c
    ///     3 columns, map:('a->'d->'e->'c), where 'a, 'd and 'e are the types of the columns, so 'b = 'd->'e->'c
    ///     n...
    static member Map<'a,'b,'c> : columnNames:seq<string> -> map:('a->'b) -> table:Table -> 'c[]

    /// Returns an array of objects. There will be one object for each row, the result of applying the given function
    /// to the given columns of each row of the table.
    /// The generic map function is only partially defined
    /// If there are:
    ///     1 column, map should be map:(int->'a->'c), where 'a is the type of the column, so 'b = 'c
    ///     2 columns, map:(int->'a->'d->'c), where 'a and 'd are the types of the columns, so 'b = 'd->'c
    ///     3 columns, map:(int->'a->'d->'e->'c), where 'a, 'd and 'e are the types of the columns, so 'b = 'd->'e->'c
    ///     n...
    static member Mapi<'a,'b,'c> : columnNames:seq<string> -> map:(int->'a->'b) -> table:Table -> 'c[]

    /// Returns a new table that has all columns of the given table but with a new column or a replacement of an existing column;
    /// data of the column is the result of applying the rowise-function to the certain columns of the given table.
    /// The generic map function is only partially defined
    /// If there are:
    ///     1 column, map should be map:('a->'c), where 'a is the type of the column, so 'b = 'c
    ///     2 columns, map('a->'d->'c), where 'a and 'd are the types of the columns, so 'b = 'd->'c
    ///     3 columns, map('a->'d->'e->'c), where 'a, 'd and 'e are the types of the columns, so 'b = 'd->'e->'c
    ///     n...
    /// 'c may be one of Int, Float, String or DateTime
    static member MapToColumn<'a,'b,'c> : columnNames:seq<string> -> newColumnName:string -> map:('a->'b) -> table:Table -> Table

    /// Returns a new table that has all columns of the given table but with a new column or a replacement of an existing column;
    /// data of the column is the result of applying the rowise-function to the certain columns of the given table.
    /// The generic map function is only partially defined
    /// If there are:
    ///     1 column, map should be map:(int->'a->'c), where 'a is the type of the column, so 'b = 'c
    ///     2 columns, map:(int->'a->'d->'c), where 'a and 'd are the types of the columns, so 'b = 'd->'c
    ///     3 columns, map:(int->'a->'d->'e->'c), where 'a, 'd and 'e are the types of the columns, so 'b = 'd->'e->'c
    ///     n...
    /// 'c may be one of Int, Float, String or DateTime
    static member MapiToColumn<'a,'b,'c> : columnNames:seq<string> -> newColumnName:string -> map:(int->'a->'b) -> table:Table -> Table

    /// Return a PDF of the column contents if the column contents are numeric
    static member TryPdf : columnName:string -> pointCount:int -> table:Table -> (float[] * float[]) option

    /// Return a PDF of the column contents if the column contents are numeric
    static member Pdf : columnName:string -> pointCount:int -> table:Table -> (float[] * float[])

    /// Return a float view of a column if the column content's type is numeric
    static member TryToRealArray : columnName:string -> table:Table -> IRArray<float> option

    /// Return a float view of a column if the column content's type is numeric
    static member ToRealArray : columnName:string -> table:Table -> IRArray<float>

    /// Return some simple statistical properties of a column
    static member TrySummary : columnName:string -> table:Table -> ColumnSummary option

    /// Return some simple statistical properties of a column
    static member Summary : columnName:string -> table:Table -> ColumnSummary

    /// Apply transform function to a set of columns' contents as arrays
    /// The generic transform function is only partially defined
    /// If there are:
    ///     1 column, transform should be transform:('a->'c), so 'b = 'c
    ///     2 columns, transform('a->'d->'c), so 'b = 'd->'c
    ///     3 columns, transform('a->'d->'e->'c), so 'b = 'd->'e->'c
    ///     n...
    /// Each of these types may be one of Column, T[], IRArray<T> or Array, possibly different T's
    static member Transform<'a,'b,'c> : columnNames:seq<string> -> transform:('a->'b) -> table:Table -> 'c

    /// If at least two of the columns are real or int then Some(Column Names * Correlations)
    /// else None
    static member TryCorrelation : table:Table -> (string[] * float[][]) option

    /// If at least two of the columns are real or int then Column Names * Correlations
    /// else throw an exception
    static member Correlation : table:Table -> (string[] * float[][])

    /// Reads table from a delimited text stream.
    static member Read : settings:Angara.Data.ReadSettings -> stream:IO.Stream -> Table
    
    /// <summary>Writes a table to a stream as a delimited text.</summary>
    /// <remarks>
    /// <p>Requires that all columns have same length.</p>
    /// </remarks>
    static member Write : settings:Angara.Data.WriteSettings -> stream:IO.Stream -> table:Table -> unit
