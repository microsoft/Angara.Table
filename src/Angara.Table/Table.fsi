namespace Angara.Data

open System
open System.Collections.Generic
open System.Collections.Immutable

/// Represents a single value of a table column.
type DataValue =
    | IntValue      of int
    | RealValue     of float
    | StringValue   of string
    | DateValue     of DateTime
    | BooleanValue  of Boolean
    /// If this instance is IntValue, returns the integer value; otherwise, throws `InvalidCastException`.
    member AsInt     : int
    member AsReal    : float
    member AsString  : string
    member AsDate    : DateTime
    member AsBoolean : Boolean

/// Represents data values of a table column as an immutable array of one of the supported types which is computed on demand.
[<NoComparison>]
type ColumnValues =
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
    member Item      : rowIndex:int -> DataValue

/// Represents a table column which is a pair of column name and an immutable array of one of the supported types.
type [<Class>] Column =
    member Name : string with get
    /// Returns column values.
    member Rows : ColumnValues with get
    /// Gets a count of the total number of values in the column.
    member Height : int with get 

    static member CreateInt : name:string * rows:int seq * ?count:int -> Column
    static member CreateReal : name:string * rows:float seq * ?count:int -> Column
    static member CreateString : name:string * rows:string seq * ?count:int -> Column
    static member CreateBoolean : name:string * rows:bool seq * ?count:int -> Column
    static member CreateDate : name:string * rows:DateTime seq * ?count:int -> Column

    static member CreateInt : name:string * rows:Lazy<ImmutableArray<int>> * count:int -> Column
    static member CreateReal : name:string * rows:Lazy<ImmutableArray<float>> * count:int -> Column
    static member CreateString : name:string * rows:Lazy<ImmutableArray<string>> * count:int -> Column
    static member CreateBoolean : name:string * rows:Lazy<ImmutableArray<bool>> * count:int -> Column
    static member CreateDate : name:string * rows:Lazy<ImmutableArray<DateTime>> * count:int -> Column
    
    static member Create : name:string * rows:ColumnValues * count:int -> Column
    static member CreateFromUntyped : name:string * rows:System.Array -> Column


/// Represents a table wich is an immutable list of named columns.
/// The type is thread safe.
type [<Class>] Table = 
    new : columns : Column seq -> Table
    interface IEnumerable<Column> 
    
    /// Gets a count of the total number of columns in the table.
    member Count : int with get
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

    /// Gets a count of the total number rows in the table.
    member RowsCount : int with get

    /// Builds and returns rows of the table represented as a sequence of instances of the type '`r',
    /// so that one instance of `'r` corresponds to one row of the table with the order respected.
    /// 
    /// Columns are mapped to public properties of `'r`. If there is a column in the table such that '`r' has no 
    /// public property with same name, the column is ignored. Next, if '`r' has a property such that
    /// there is no column with same name, the function fails with an exception.
    ///
    /// The method uses reflection to build instances of `'r` from the table columns:
    /// - If `'r` is F# record, then for each property of the type there must be a corresponding column of identical type.
    /// - Otherwise, then `'r` has default constructor and for each public writable property there must be a column of same name and type as the property.
    abstract ToRows<'r> : unit -> 'r seq

    /// Builds a table from a finite sequence of columns.
    /// All given columns must be of same height. 
    /// Duplicate column names are allowed.
    /// Order of columns in the table is same as in the input sequence.
    static member OfColumns : columns:Column seq -> Table

    /// Builds a table such that each public property of a given type `'r` 
    /// becomes the table column with the name and type identical to the property;
    /// each table row corresponds to an element of the input sequence with the order respected.
    /// If the type `'r` is an F# record, the order of columns is identical to the record properties order.
    /// If there is a public property having a type that is not valid for a table column, the function fails with an exception.
    static member OfRows<'r> : 'r seq -> Table<'r>
    /// Builds a table such that each public property of a given type `'r` 
    /// becomes the table column with the name and type identical to the property;
    /// each table row corresponds to an element of the input sequence with the order respected.
    /// If the type `'r` is an F# record, the order of columns is identical to the record properties order.
    /// If there is a public property having a type that is not valid for a table column, the function fails with an exception.
    static member OfRows<'r> : ImmutableArray<'r> -> Table<'r>

    static member OfMatrix<'v> : matrixRows:'v seq seq * ?columnNames:string seq -> MatrixTable<'v>
    static member OfMatrix<'v> : matrixRows:'v[] seq * ?columnNames:string seq -> MatrixTable<'v>
    static member OfMatrix<'v> : matrixRows:ImmutableArray<'v> seq * ?columnNames:string seq -> MatrixTable<'v>

    static member DefaultColumnName: columnIndex:int -> string

    /// Creates a new, empty table
    static member Empty : Table
    /// Creates a new table that has all columns of the original table appended with the given column.
    /// Duplicate names are allowed.
    static member Add : column:Column -> table:Table -> Table
    /// Creates a new table that has all columns of the original table excluding the columns having name
    /// contained in the given column names.
    static member Remove : columnNames:seq<string> -> table:Table -> Table

    /// Return a new table containing all rows from a table where a predicate is true, where the predicate takes a set of columns
    /// The generic predicate function is only partially defined
    /// If there are:
    ///     1 column, predicate should be predicate:('a->bool), where 'a is the type of the column, so 'b = bool
    ///     2 columns, predicate:('b>'c->bool), where 'b and 'c are the types of the columns, so 'b = 'c->bool
    ///     3 columns, predicate:('b->'c->'d->bool), where 'b, 'c and 'd are the types of the columns, so 'a = 'b->'c->'d
    ///     n...
    static member Filter : columnNames:seq<string> -> predicate:('a->'b) -> table:Table -> Table

    /// Return a new table containing all rows from a table where a predicate is true, where the predicate takes a set of columns and row index
    /// The generic predicate function is only partially defined
    /// If there are:
    ///     0 column, predicate should be: `int->bool`, so `'a = bool`
    ///     1 column, predicate should be: `int->'b->bool`, where 'b is the type of the column, so `'a = 'b -> bool`
    ///     2 columns, predicate should be: `int->'b->'c->bool`, where 'b and 'c are the types of the columns, so 'a = 'b->'c->bool
    ///     n...
    static member Filteri : columnNames:seq<string> -> predicate:(int->'a) -> table:Table -> Table

    /// Builds a new sequence whose elements are the results of applying the given function 'map'
    /// to each of the rows of the given table columns.
    /// 
    /// The generic map function is only partially defined.
    /// If there are:
    ///
    /// - 0 columns, map should be `map:(unit->'c)`, where `'a` is the type of the column, so `'a = unit` and `'b = 'c`
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
    /// </remarks>
    static member Transform<'a,'b,'c> : columnNames:seq<string> -> transform:(ImmutableArray<'a>->'b) -> table:Table -> 'c

    /// Builds a new table that contains columns of both given tables. Duplicate column names are allowed.
    static member Append : table1:Table -> table2:Table -> Table

    static member AppendMatrix : table1:MatrixTable<'v> -> table2:MatrixTable<'v> -> MatrixTable<'v>

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
    static member AppendTransform : columnNames:seq<string> -> transform:(ImmutableArray<'a>->'b) -> table:Table -> Table

    /// Loads a table from a delimited text file.
    static member Load : path:string -> Table
    /// Loads a table from a delimited text file.
    static member Load : path:string * settings:Angara.Data.DelimitedFile.ReadSettings -> Table
    /// Loads a table from a delimited text stream using given reader.
    static member Load : reader:System.IO.TextReader -> Table
    /// Loads a table from a delimited text stream using given reader.
    static member Load : reader:System.IO.TextReader * settings:Angara.Data.DelimitedFile.ReadSettings -> Table

    /// Saves the table to a delimited text file, overwriting an existing file, if it exists.
    static member Save : table:Table * path:string -> unit
    /// Saves the table to a delimited text file, overwriting an existing file, if it exists.
    static member Save : table:Table * path:string * settings:Angara.Data.DelimitedFile.WriteSettings -> unit
    /// Saves the table to a delimited text stream using given writer.
    static member Save : table:Table * writer:System.IO.TextWriter -> unit
    /// Saves the table to a delimited text stream using given writer.
    static member Save : table:Table * writer:System.IO.TextWriter * settings:Angara.Data.DelimitedFile.WriteSettings -> unit

and [<Class>] Table<'r> =
    inherit Table
    new : rows:ImmutableArray<'r> -> Table<'r>
    member Rows : ImmutableArray<'r>
    member AddRows : 'r seq -> Table<'r>
    member AddRow : 'r -> Table<'r>

and [<Class>] MatrixTable<'v> =
    inherit Table
    new : matrixRows:ImmutableArray<'v> seq * columnsNames:string seq option -> MatrixTable<'v>

    member Columns : ImmutableArray<ImmutableArray<'v>>
    member Rows : ImmutableArray<ImmutableArray<'v>> 
    member Item : row:int*col:int -> 'v with get 

    member AddRows : 'v seq seq -> MatrixTable<'v>
    member AddRows : 'v[] seq -> MatrixTable<'v>
    member AddRows : ImmutableArray<'v> seq -> MatrixTable<'v>
    member AddRow : 'v seq -> MatrixTable<'v>