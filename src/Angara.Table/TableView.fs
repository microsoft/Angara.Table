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
    ; HideNaNs : bool }

type TableView =
    { Table : Table
    ; ViewSettings : TableViewSettings }