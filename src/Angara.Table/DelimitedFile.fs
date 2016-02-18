namespace Angara.Data

open System  
open System.Collections.Generic
open System.Text
open System.IO

type Delimiter = 
    | Comma = 0
    | Tab = 1
    | Semicolon = 2
    | Space = 3

[<NoEquality; NoComparison>]
type WriteSettings = 
    {
        Delimiter : Delimiter
        AllowNullStrings : bool
        SaveHeader: bool
    }
    static member public CommaDelimited : WriteSettings = { Delimiter = Delimiter.Comma; AllowNullStrings = false; SaveHeader = true }

    
[<NoEquality; NoComparison>]
type ReadSettings = 
    {
        Delimiter : Delimiter
        InferNullStrings : bool
        HasHeader: bool
        ColumnsCount : int option
        ColumnTypes : (int * string -> Type option) option 
    }
    static member public CommaDelimited : ReadSettings = { Delimiter = Delimiter.Comma; HasHeader = true; InferNullStrings = false; ColumnsCount = None; ColumnTypes = None }
