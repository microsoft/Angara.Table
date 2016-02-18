namespace Angara.Data

/// Determines a character that delimits columns of a table.
type Delimiter = 
    | Comma = 0
    | Tab = 1
    | Semicolon = 2
    | Space = 3

[<NoEquality; NoComparison>]
type WriteSettings = 
    { /// Determines which character will delimit columns.
      Delimiter : Delimiter
      /// If true, writes null strings as an empty string and an empty string as double quotes (""), 
      /// so that these cases could be distinguished; otherwise, if false, throws an exception if null is 
      /// in a string data array.
      AllowNullStrings : bool 
      /// If true, the first line will contain names corresponding to the columns of the table.
      /// Otherwise, if false, the first line is a data line.
      SaveHeader: bool }
    /// Uses comma as delimiter, saves a header and disallows null strings.
    static member CommaDelimited : WriteSettings

[<NoEquality; NoComparison>]
type ReadSettings = 
    { /// Determines which character delimits columns.
      Delimiter : Delimiter
      /// If true, double quotes ("") are considered as empty string and an empty string is considered as null; 
      /// otherwise, if false, both cases are considered as an empty string.
      InferNullStrings : bool
      /// If true, the first line is considered as a header of the table.
      /// This header will contain names corresponding to the fields in the file
      /// and should contain the same number of fields as the records in
      /// the rest of the file. Otherwise, if false, the first line is a data line and columns are named as A, B, C... using radix 26.
      HasHeader: bool
      /// An optional value that allows to provide an expected number of columns. If number of columns differs, the reading fails.
      ColumnsCount : int option
      /// An optional value that allows a user to specify element types for some of columns. This allows
      /// to read integer columns since automatic inference always uses Double type for numeric values.
      ColumnTypes : (int * string -> System.Type option) option }
    /// Expects comma as delimiter, has header, doesn't infer null strings and doesn't predefine column count or types.
    static member CommaDelimited : ReadSettings

