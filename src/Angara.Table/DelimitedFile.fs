namespace Angara.Data.DelimitedFile

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
    static member public Default : WriteSettings = { Delimiter = Delimiter.Comma; AllowNullStrings = false; SaveHeader = true }

    
[<NoEquality; NoComparison>]
type ReadSettings = 
    {
        Delimiter : Delimiter
        InferNullStrings : bool
        HasHeader: bool
        ColumnsCount : int option
        ColumnTypes : (int * string -> System.Type option) option 
    }
    static member public Default : ReadSettings = { Delimiter = Delimiter.Comma; HasHeader = true; InferNullStrings = false; ColumnsCount = None; ColumnTypes = None }