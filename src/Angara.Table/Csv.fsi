namespace Angara.Data

open System
open System.IO

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
      ColumnTypes : (int * string -> Type option) option }
    /// Expects comma as delimiter, has header, doesn't infer null strings and doesn't predefine column count or types.
    static member CommaDelimited : ReadSettings


/// Type of column elements.
type internal ColumnType = 
    | Integer 
    | Double
    | Boolean 
    | DateTime 
    | String 

/// Describes a table column.
type internal ColumnSchema = 
    { /// Name of a column.
        Name : string
        /// Type of a column.
        Type : ColumnType }


/// Implements writer and reader for a text representation of a list of named arrays of certain types.
/// The implementation mostly follows RFC 4180, but:
/// 1) First line always considered as a header.
/// 2) In addition to comma separator, it supports tab, semicolon and space.
[<AbstractClass; Sealed>]
type internal DelimitedFile =
    /// <summary>Writes a sequence of named arrays to a stream in a delimited text format (e.g. CSV).</summary>
    static member Write : settings:WriteSettings -> stream: Stream -> table:(string * Array) seq -> unit

    /// <summary>Reads a table from a delimited text format.</summary>
    static member Read : settings: ReadSettings -> stream: Stream -> (ColumnSchema * Array) [] 

module internal Helpers =
    /// Splits string lines read from the given StreamReader by delimeter; supports escaping using double quotes and quoted elements with newlines.
    /// Returns None, if input stream is ended;
    /// Some of split items, otherwise.
    ///
    /// The rules are:
    /// - null is represented as an empty string []
    /// - empty string is represented as double quotes [""]    
    /// - a string that contains either quote, newline or a delimiter, its quotes are replaced with double quotes and surrounded with quotes;
    /// - otherwise, used as it.
    /// - escaped string always starts from the beginning but can continue after end quote which is end of escaped string, 
    ///   but the rest part cannot be escaped again (see Excel), e.g. 
    ///     ["hello ""ABC""" from "DEF"] is read to [hello "ABC" from "DEF"]
    ///
    /// Example of special cases for strings ( '[...]' indicate a cell text, surrounded with a delimiter or new line or end of file ):
    ///
    /// (value)                 (written as)                
    /// --------------------------------------------------------
    /// null                    []  (empty string)
    /// (empty string)          [""] (double quotes)
    /// " (single quote)        [""""] 
    /// .\n.. (escaped string)  [".\n.."]
    /// "... (string starting with quote)
    ///                         ["""..."] 
    /// ".\n.. (escaped string starting with quote)
    ///                         [""".\n.."]
    val splitRow : char -> StreamReader -> string[] option

    /// Makes the string proper to write in a CSV file. See rules in comments for splitRow.
    val escapeString : string -> string -> bool -> string

    /// Produces a non-empty string to be used as a name of a column with the given index.
    /// The produced names are similar to Excel column names; e.g.
    /// index 28 gives the name "AC".
    val indexToName: int -> string
