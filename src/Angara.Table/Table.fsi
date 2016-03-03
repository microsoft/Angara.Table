namespace Angara.Data

open System
open System.Collections.Generic
open System.Collections.Immutable


// The term “field” is usually used interchangeably with ”column,” but database purists prefer to use the word “field” to denote a particular value or single item of a column.
// https://www.techopedia.com/definition/8/database-column
type Field =
    | IntField      of int
    | RealField     of float
    | StringField   of string
    | DateField     of DateTime
    | BooleanField  of Boolean
    /// If this instance is IntField, returns the integer value; otherwise, throws `InvalidCastException`.
    member AsInt     : int
    member AsReal    : float
    member AsString  : string
    member AsDate    : DateTime
    member AsBoolean : Boolean

/// Represents column rows as an immutable array of one of the supported types which is computed on demand.
[<NoComparison>]
type ColumnRows =
    | IntColumn     of Lazy<ImmutableArray<int>>
    | RealColumn    of Lazy<ImmutableArray<float>>
    | StringColumn  of Lazy<ImmutableArray<string>>
    | DateColumn    of Lazy<ImmutableArray<DateTime>>
    | BooleanColumn of Lazy<ImmutableArray<Boolean>>
    /// If this instance is IntColumn, returns the immutable integer array; otherwise, throws `InvalidCastException`.
    /// If the column array has not been evalutated before, this function performs the execution of the Lazy instance.
    member AsInt     : ImmutableArray<int>
    member AsReal    : ImmutableArray<float>
    member AsString  : ImmutableArray<string>
    member AsDate    : ImmutableArray<DateTime>
    member AsBoolean : ImmutableArray<Boolean>
    /// Returns a column field at the specified row index.
    /// If the column array has not been evalutated before, this function performs the execution of the Lazy instance.
    member Item      : rowIndex:int -> Field

/// Represents a table column which is a pair of column name and an immutable array of one of the supported types.
[<NoComparison>]
type Column =
    { Name : string
      Rows : ColumnRows }
    static member OfArray<'a> : name:string * rows:'a[] -> Column
    static member OfArray<'a> : name:string * rows:ImmutableArray<'a> -> Column

/// Basic statistics for columns containing numeric data
type NumericColumnSummary = {
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
    TotalCount: int
    /// Number of elements in the column except for NaNs.
    Count: int
}

/// Simple statistics for columns containing non-numeric data
type ComparableColumnSummary<'a when 'a : comparison> = {
    /// A minimum value of the column.
    Min: 'a
    /// A maximum value of the column.
    Max: 'a
    /// Total number of elements in the column.
    TotalCount: int
    /// Number of elements in the column except for missing values,
    /// which is null or empty string.
    Count: int
}

/// Simple statistics for columns containing boolean values.
type BooleanColumnSummary = {
    /// Number of rows with value "true"
    TrueCount: int
    /// Number of rows with value "false"
    FalseCount: int
}

/// Discriminated Union type holding statistics for each column type
type ColumnSummary =
    /// Statistics for int and real columns
    | NumericColumnSummary of NumericColumnSummary
    /// Statistics for string columns
    | StringColumnSummary of ComparableColumnSummary<string>
    /// Statistics for DateTime columns
    | DateColumnSummary of ComparableColumnSummary<DateTime>
    /// Statistics for Boolean columns
    | BooleanColumnSummary of BooleanColumnSummary


/// Represents a table wich is an immutable list of named columns.
[<Class>]
type Table = 
    interface IEnumerable<Column> 

    new : nameColumns : Column seq -> Table
    new : nameColumns : Column seq * rowsCount:int -> Table

    /// Gets a count of the total number of columns in the table.
    member Count : int with get
    /// Gets a column by its index.
    member Item : index:int -> Column with get
    /// Gets a column by its name.
    /// If there are several columns with same name, returns the fist column having the name.
    member Item : name:string -> Column with get

    /// Gets a count of the total number rows in the table.
    /// Evalutation of this propery for the first time can cause execution of the lazy column array,
    /// depending on whether the table has got the rows count in the constructor or not.
    member RowsCount : int with get

    /// Builds and returns rows of the table represented as a sequence of instances of certain type,
    /// so that one instance of `'r` corresponds to one row of the table with order respeced.
    /// Columns are mapped to public properties of `'r`.
    ///
    /// The method uses reflection to build instances of `'r` from the table columns:
    /// - If `'r` is F# record, then for each property of the type there must be a corresponding column of appropriate type
    /// - Otherwise, then for each public property of the type that has get and set accessors there must a corresponding column of appropriate type
    member ToRows<'r> : unit -> 'r seq

    /// Creates a new, empty table
    static member Empty : Table
    static member Add<'a> : column:Column -> table:Table -> Table
    static member Remove : columnNames:seq<string> -> table:Table -> Table

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

    /// Builds a new sequence whose elements are the results of applying the given function 'map'
    /// to each of the rows of the given table columns.
    /// 
    /// The generic map function is only partially defined.
    /// If there are:
    ///
    /// - 1 column, map should be `map:('a->'c)`, where `'a` is the type of the column, so `'b = 'c`
    /// - 2 columns, map('a->'d->'c), where 'a and 'd are the types of the columns, so 'b = 'd->'c
    /// - 3 columns, map('a->'d->'e->'c), where 'a, 'd and 'e are the types of the columns, so 'b = 'd->'e->'c
    /// - n...
    static member Map<'a,'b,'c> : columnNames:seq<string> -> map:('a->'b) -> table:Table -> 'c seq

    /// <summary>Builds a new sequence whose elements are the results of applying the given function 'map'
    /// to each of the rows of the given table columns.
    /// The integer index passed to the function indicates the index of row being transformed.</summary>
    /// <remarks><p>The generic map function is only partially defined.
    /// If there are:
    ///     0 columns, map is called for each row of the table and should be map:(int->'c), so 'a = 'c
    ///     1 column, map should be map:(int->'d->'c), where 'd is the type of the column, so 'a = 'd->'c
    ///     2 columns, map:(int->'d->'e->'c), where 'd and 'e are the types of the columns, so 'a = 'd->'e->'c
    ///     n...
    /// </p></remarks>
    static member Mapi<'a,'c> : columnNames:seq<string> -> map:(int->'a) -> table:Table -> 'c seq

    /// Builds a new table that contains all columns of the given table and a new column or a replacement of an original table column;
    /// elements of the column are the results of applying the given function to each of the rows of the given table columns.
    ///
    /// The generic map function is only partially defined.
    /// If there are:
    /// 
    /// - 1 column, map should be map:('a->'c), where 'a is the type of the column, so 'b = 'c
    /// - 2 columns, map('a->'d->'c), where 'a and 'd are the types of the columns, so 'b = 'd->'c
    /// - 3 columns, map('a->'d->'e->'c), where 'a, 'd and 'e are the types of the columns, so 'b = 'd->'e->'c
    /// - n...
    /// 
    /// Ultimate result type of the map function must be either Int, Float, String, Bool or DateTime.
    /// </remarks>
    static member MapToColumn : columnNames:seq<string> -> newColumnName:string -> map:('a->'b) -> table:Table -> Table

    /// <summary>Builds a new table that contains all columns of the given table and a new column or a replacement of an original table column;
    /// elements of the column are the results of applying the given function to each of the rows of the given table columns.
    /// The integer index passed to the function indicates the index of row being transformed.</summary>
    /// <remarks><p>The generic map function is only partially defined.
    /// If there are:
    ///     0 columns, map is called for each row of the table and should be map:(int->'c), so 'a = 'c
    ///     1 column, map should be map:(int->'d->'c), where 'd is the type of the column, so 'a = 'd->'c
    ///     2 columns, map:(int->'d->'e->'c), where 'd and 'e are the types of the columns, so 'a = 'd->'e->'c
    ///     n...
    /// </p>
    /// <p>Ultimate result type of the map function must be either Int, Float, String, Bool or DateTime.</p>
    /// </remarks>
    static member MapiToColumn : columnNames:seq<string> -> newColumnName:string -> map:(int->'a) -> table:Table -> Table

        /// <summary>Applies the given function to the arrays of given table columns.</summary>
    /// <remarks>
    /// <p>The generic curried transform function is only partially defined.
    /// If there are:
    ///     1 column, transform should be transform:('a->'c) where 'a is an array corresponding to the column type, so 'b = 'c
    ///     2 columns, transform('a->'d->'c) where 'a, 'd are arrays corresponding to the columns types, so 'b = 'd->'c
    ///     3 columns, transform('a->'d->'e->'c) where 'a, 'd, 'e are arrays corresponding to the columns types, so 'b = 'd->'e->'c
    ///     n...</p>
    /// <p>The transform function argument types may be one of: Column, T[], IRArray&lt;T> or Array.</p>
    /// </remarks>
    static member Transform<'a,'b,'c> : columnNames:seq<string> -> transform:('a->'b) -> table:Table -> 'c

    /// Builds a new table that contains columns of both given tables. Duplicate column names are allowed.
    static member Join : table1:Table -> table2:Table -> Table

    /// <summary>Builds a new table that contains columns of the given table appended with columns of a table produced by the
    /// given function applied to the arrays of given table columns.</summary>
    /// <remarks>
    /// <p>The generic curried transform function is only partially defined.
    /// If there are:
    ///     1 column, transform should be transform:('a->Table) where 'a is an array corresponding to the column type, so 'b = Table
    ///     2 columns, transform('a->'d->Table) where 'a, 'd are arrays corresponding to the columns types, so 'b = 'd->Table
    ///     3 columns, transform('a->'d->'e->Table) where 'a, 'd, 'e are arrays corresponding to the columns types, so 'b = 'd->'e->Table
    ///     n...</p>
    /// <p>The transform function argument types may be one of: Column, T[], IRArray&lt;T> or Array.</p>
    /// </remarks>
    static member JoinTransform<'a,'b> : columnNames:seq<string> -> transform:('a->'b) -> table:Table -> Table

        /// Return a PDF of the column contents if the column contents are numeric
    static member TryPdf : columnName:string -> pointCount:int -> table:Table -> (float[] * float[]) option

    /// Return a PDF of the column contents if the column contents are numeric
    static member Pdf : columnName:string -> pointCount:int -> table:Table -> (float[] * float[])

    /// Return some simple statistical properties of a column
    static member TrySummary : columnName:string -> table:Table -> ColumnSummary option

    /// Return some simple statistical properties of a column
    static member Summary : columnName:string -> table:Table -> ColumnSummary

    /// If at least two of the columns are real or int then Some(Column Names * Correlations)
    /// else None
    static member TryCorrelation : table:Table -> (string[] * float[][]) option

    /// If at least two of the columns are real or int then Column Names * Correlations
    /// else throw an exception
    static member Correlation : table:Table -> (string[] * float[][])

    /// Reads table from a delimited text file.
    static member Read : settings:Angara.Data.DelimitedFile.ReadSettings -> path:string -> Table

    /// Reads table from a delimited text stream.
    static member ReadStream : settings:Angara.Data.DelimitedFile.ReadSettings -> stream:IO.Stream -> Table
    
    /// Writes a table to a stream as a delimited text.
    static member Write : settings:Angara.Data.DelimitedFile.WriteSettings -> path:string -> table:Table -> unit

    /// Writes a table to a stream as a delimited text.
    static member WriteStream : settings:Angara.Data.DelimitedFile.WriteSettings -> stream:IO.Stream -> table:Table -> unit


[<Class>]
type Table<'r> = 
    inherit Table

    new : rows : 'r seq -> Table<'r>

    member Rows : ImmutableArray<'r>

