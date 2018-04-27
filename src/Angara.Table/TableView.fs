namespace Angara.Data

type TableViewerTab = 
    | TabSummary = 0
    | TabData = 1
    | TabCorrelation = 2

type PageSize =
    | Size10 = 10
    | Size25 = 25
    | Size50 = 50
    | Size100 = 100

type TableViewSettings = 
    { DefaultTab : TableViewerTab
    ; DefaultPageSize : PageSize
    ; HideNaNs : bool
    ; CustomFormatters : Map<string, string>}
    /// Create TableViewSettings.
    /// customFormatters if a Map<string, string> where keys are names of corresponding table columns
    /// and values are bodies of javascript functions x -> string performing the desired formatting
    /// e.g. "return x.toFixed(2);" Note, that the name of the formal parameter passed into this function is always "x"
    static member Create (defaultTab, defaultPageSize, hideNaNs, customFormatters) : TableViewSettings =
        let customFormattersValue = match customFormatters with | Some v -> v | None -> Map.empty
        { DefaultTab = defaultTab; DefaultPageSize = defaultPageSize; HideNaNs = hideNaNs; CustomFormatters = customFormattersValue }
        
    /// Create TableViewSettings.
    /// customFormatters if an IDictionary<string, string> where keys are names of corresponding table columns
    /// and values are bodies of javascript functions x -> string performing the desired formatting
    /// e.g. "return x.toFixed(2);" Note, that the name of the formal parameter passed into this function is always "x"
    static member Create (defaultTab, defaultPageSize, hideNaNs, customFormatters : System.Collections.Generic.IDictionary<string, string>) : TableViewSettings =
        let customFormattersValue =
            if customFormatters = null then Map.empty
            else customFormatters |> Seq.map (fun kvp -> kvp.Key, kvp.Value) |> Map.ofSeq
        { DefaultTab = defaultTab; DefaultPageSize = defaultPageSize; HideNaNs = hideNaNs; CustomFormatters = customFormattersValue }

type TableView =
    { Table : Table
    ; ViewSettings : TableViewSettings }