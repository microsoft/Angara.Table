namespace Angara.Data

open System
open System.Collections.Generic
open System.Collections.Immutable
open Angara.Statistics

open Util


type NumericColumnSummary = { 
    Min: float
    Lb95: float
    Lb68: float
    Median: float
    Ub68: float
    Ub95: float
    Max: float
    Mean: float
    Variance: float
    TotalCount: int
    Count: int
}

type ComparableColumnSummary<'a when 'a : comparison> = {
    Min: 'a
    Max: 'a
    TotalCount: int
    Count: int
}

type BooleanColumnSummary = {
    TrueCount: int
    FalseCount: int
}

type ColumnSummary =
    | NumericColumnSummary  of NumericColumnSummary
    | StringColumnSummary   of ComparableColumnSummary<string>
    | DateColumnSummary     of ComparableColumnSummary<DateTime>
    | BooleanColumnSummary  of BooleanColumnSummary

type Field =
    | IntField      of int
    | RealField     of float
    | StringField   of string
    | DateField     of DateTime
    | BooleanField  of Boolean
    member x.AsInt     = match x with IntField v     -> v | _ -> invalidCast "The field is not integer"
    member x.AsReal    = match x with RealField v    -> v | _ -> invalidCast "The field is not real"
    member x.AsString  = match x with StringField v  -> v | _ -> invalidCast "The field is not a string"
    member x.AsDate    = match x with DateField v    -> v | _ -> invalidCast "The field is not a date"
    member x.AsBoolean = match x with BooleanField v -> v | _ -> invalidCast "The field is not boolean"


[<NoComparison>]
type ColumnRows =
    | IntColumn     of Lazy<ImmutableArray<int>>
    | RealColumn    of Lazy<ImmutableArray<float>>
    | StringColumn  of Lazy<ImmutableArray<string>>
    | DateColumn    of Lazy<ImmutableArray<DateTime>>
    | BooleanColumn of Lazy<ImmutableArray<Boolean>>
    member x.AsInt     = match x with IntColumn v     -> v.Value | _ -> invalidCast "The column is not integer"
    member x.AsReal    = match x with RealColumn v    -> v.Value | _ -> invalidCast "The column is not real"
    member x.AsString  = match x with StringColumn v  -> v.Value | _ -> invalidCast "The column is not a string"
    member x.AsDate    = match x with DateColumn v    -> v.Value | _ -> invalidCast "The column is not a date"
    member x.AsBoolean = match x with BooleanColumn v -> v.Value | _ -> invalidCast "The column is not boolean"
    member x.Item rowIndex =
        match x with
        | IntColumn v     -> v.Value.[rowIndex] |> IntField
        | RealColumn v    -> v.Value.[rowIndex] |> RealField
        | StringColumn v  -> v.Value.[rowIndex] |> StringField
        | DateColumn v    -> v.Value.[rowIndex] |> DateField
        | BooleanColumn v -> v.Value.[rowIndex] |> BooleanField
    override this.ToString() =
        let notEvaluated = "Array is not evaluated yet"
        match this with
        | IntColumn v     -> if v.IsValueCreated then sprintf "Ints %A" v.Value else notEvaluated
        | RealColumn v    -> if v.IsValueCreated then sprintf "Reals %A" v.Value else notEvaluated
        | StringColumn v  -> if v.IsValueCreated then sprintf "Strings %A" v.Value else notEvaluated
        | DateColumn v    -> if v.IsValueCreated then sprintf "Dates %A" v.Value else notEvaluated
        | BooleanColumn v -> if v.IsValueCreated then sprintf "Booleans %A" v.Value else notEvaluated
    


[<NoComparison>]
type Column =
    { Name : string
      Rows : ColumnRows }

    static member OfArray<'a> (name:string, rows:'a[]) : Column =
        let lazyRows = Lazy.CreateFromValue(ImmutableArray.Create<'a>(rows))
        let rows =
            match typeof<'a> with
            | t when t = typeof<int>      -> coerce lazyRows |> ColumnRows.IntColumn    
            | t when t = typeof<float>    -> coerce lazyRows |> ColumnRows.RealColumn   
            | t when t = typeof<string>   -> coerce lazyRows |> ColumnRows.StringColumn 
            | t when t = typeof<DateTime> -> coerce lazyRows |> ColumnRows.DateColumn   
            | t when t = typeof<bool>     -> coerce lazyRows |> ColumnRows.BooleanColumn  
            | t -> raise (new NotSupportedException(sprintf "Type '%A' is not a valid column type" t))
        { Name = name; Rows = rows }

    static member OfArray<'a> (name:string, rows:ImmutableArray<'a>) : Column =
        let lazyRows = Lazy.CreateFromValue(rows)
        let rows =
            match typeof<'a> with
            | t when t = typeof<int>      -> coerce lazyRows |> ColumnRows.IntColumn    
            | t when t = typeof<float>    -> coerce lazyRows |> ColumnRows.RealColumn   
            | t when t = typeof<string>   -> coerce lazyRows |> ColumnRows.StringColumn 
            | t when t = typeof<DateTime> -> coerce lazyRows |> ColumnRows.DateColumn   
            | t when t = typeof<bool>     -> coerce lazyRows |> ColumnRows.BooleanColumn  
            | t -> raise (new NotSupportedException(sprintf "Type '%A' is not a valid column type" t))
        { Name = name; Rows = rows }

    override this.ToString() =
        sprintf "%s: %O" this.Name this.Rows






type Table(names:seq<string>, columns:seq<Column>) =

    

    let namesRO : IReadOnlyList<string> =
        RArray<string>(names) :> IReadOnlyList<string>

    let columnsRO : IReadOnlyList<Column> =
        RArray<Column>(columns) :> IReadOnlyList<Column>

    let rowsCount = 
        let m = Seq.fold (fun (count:int) (col:Column) ->
                    let n = Column.Count col
                    if count <> -1 && count <> n then raise (ArgumentException("All columns of a table must have same number of rows"))
                    n) -1 columns
        if m = -1 then 0 else m

    let types : IReadOnlyList<Type> =
        let cns =
            columns
            |> Seq.map Column.Type

        RArray<Type>(cns) :> IReadOnlyList<Type>

    new() = Table([], [])

    new(nameColumns:seq<string * Column>) =
        let names, columns =
            nameColumns
            |> Array.ofSeq
            |> Array.unzip
        Table(names, columns)

    member this.Column(index:int) = columnsRO.[index]

    member this.Column(name:string) = columnsRO.[0] // todo
        

    member this.Names
        with get() : IReadOnlyList<string> =
            namesRO

    member this.Columns
        with get() : IReadOnlyList<Column> =
            columnsRO

    member this.Count
        with get() : int =
            rowsCount

    member this.Types
        with get() : IReadOnlyList<Type> =
            types

    override this.ToString() =
        let cols = 
            this.Columns 
            |> Seq.mapi(fun i c -> 
                let name = this.Names.[i]
                let count = Column.Count c
                sprintf "\"%s\"[%d]: %A" name count c)
        String.Join("\n", cols)


    static member Empty: Table =
        new Table()

    static member New<'a> (columnName:string) (columnData:'a) : Table =
        new Table([columnName, Column.New columnData])

    static member FromArrays (columns: (string * Array) seq) : Table =
        new Table(columns |> Seq.map(fun (n,a) -> n, Column.New<System.Array> a))

    static member Add<'a> (name:string) (data:'a) (table:Table) : Table =
        let column:Column = Column.New<'a> data
        if table.Columns.Count <> 0 && Column.Count column <> table.Count then raise (ArgumentException("The column has different number of rows than the table"))
        let names = List.append (table.Names |> Seq.toList) [name]
        let columns = List.append (table.Columns |> Seq.toList) [column]
        Table(names, columns)

    static member Remove (columnNames:seq<string>) (table:Table) : Table =
        let names = Set.ofSeq columnNames
        let newNames, newColumns =
            Seq.zip table.Names table.Columns
            |> Seq.filter(fun (name, _) -> not(names.Contains name))
            |> Seq.toArray
            |> Array.unzip

        Table(newNames, newColumns)

    static member TryName(column:Column) (table:Table) : string option =
        match  table.Columns |> Seq.tryFindIndex (fun c -> c.Equals(column)) with
        | Some i -> Some(table.Names.[i])
        | None -> None

    static member Name(column:Column) (table:Table) : string =
        match Table.TryName column table with
        | Some n -> n
        | None -> failwith "Column not found"

    static member TryColumnIndex(columnName:string) (table:Table) : int option =
        table.Names
        |> Seq.tryFindIndex (fun n -> Object.Equals(n, columnName))

    static member ColumnIndex(columnName:string) (table:Table) : int =
        match table.Names |> Seq.tryFindIndex (fun n -> Object.Equals(n, columnName)) with
        | Some i -> i
        | None -> failwith (sprintf "Column '%s' not found" columnName)

    static member TryColumn(columnName:string) (table:Table) : Column option =
        table
        |> Table.TryColumnIndex columnName
        |> Option.map (fun index -> table.Columns.[index])

    static member Column(columnName:string) (table:Table) : Column =
        let index = Table.ColumnIndex columnName table
        table.Columns.Item index

    static member TryType(columnName:string) (table:Table) : Type option =
        table
        |> Table.TryColumn columnName
        |> Option.map Column.Type

    static member Type(columnName:string) (table:Table) : Type =
        table
        |> Table.Column columnName
        |> Column.Type

    static member TrySub<'a>(columnName:string) (startIndex:int) (count:int) (table:Table) : 'a option =
        table
        |> Table.TryColumn columnName
        |> Option.bind (Column.TrySub<'a> startIndex count)

    static member Sub<'a>(columnName:string) (startIndex:int) (count:int) (table:Table) : 'a =
        table
        |> Table.Column columnName
        |> Column.Sub<'a> startIndex count

    static member TryToArray<'a>(columnName:string) (table:Table) : 'a option =
        table
        |> Table.TryColumn columnName
        |> Option.bind Column.TryToArray

    static member ToArray<'a>(columnName:string) (table:Table) : 'a =
        table
        |> Table.Column columnName
        |> Column.ToArray

    static member GetEnumeratorT<'a>(columnName:string) (table:Table) : IEnumerator<'a> =
        let column = Table.Column columnName table
        Column.GetEnumerator<'a> column

    static member GetEnumerator(columnName:string) (table:Table) : IEnumerator =
        table
        |> Table.Column columnName
        |> Column.GetEnumerator

    static member TryItem<'a>(columnName:string) (index:int) (table:Table) : 'a option =
        table
        |> Table.TryColumn columnName
        |> Option.bind (Column.TryItem<'a> index)

    static member Item<'a>(columnName:string) (index:int) (table:Table) : 'a =
        table
        |> Table.Column columnName
        |> Column.Item<'a> index

    static member Filter<'a> (columnNames:seq<string>) (predicate:('a->bool)) (table:Table) : Table =
        let mask =
            columnNames
            |> Seq.map (fun c -> Table.Column c table)
            |> Column.Map predicate

        let newColumns =
            table.Columns
            |> Seq.map (Column.Select mask)

        Table(table.Names, newColumns)

    static member Filteri<'a> (columnNames:seq<string>) (predicate:(int->'a->bool)) (table:Table) : Table =
        let mask =
            columnNames
            |> Seq.map (fun c -> Table.Column c table)
            |> Column.Mapi predicate

        let newColumns =
            table.Columns
            |> Seq.map (Column.Select mask)

        Table(table.Names, newColumns)

    static member Join(table1:Table) (table2:Table) : Table =
        let newNames:seq<string> = Seq.append table1.Names table2.Names
        let newColumns:seq<Column> = Seq.append table1.Columns table2.Columns

        Table(newNames, newColumns)

    static member JoinTransform<'a,'b>(columnNames:seq<string>) (transform:('a->'b)) (table:Table) : Table =
        Table.Transform<'a,'b,Table> columnNames transform table
        |> Table.Join table

    static member Map<'a,'b,'c>(columnNames:seq<string>) (map:('a->'b)) (table:Table) : 'c seq =
        if columnNames |> Seq.isEmpty then // map must be (unit->'b) and 'c = 'b; length of the result equals table row count            
            match box map with
            | :? (unit -> 'c) as map0 -> let v = map0() in Seq.init table.Count (fun _ -> v)
            | _ -> failwith "Unexpected type of map"
        else
            columnNames
            |> Seq.map (fun c -> Table.Column c table)
            |> Column.Map<'a,'b,'c> map
        
    static member private Mapa<'a,'b,'c>(columnNames:seq<string>) (map:('a->'b)) (table:Table) : 'c [] =
        Table.Map columnNames map table |> Seq.toArray

    static member private reflectedMapa = typeof<Table>.GetMethod("Mapa", Reflection.BindingFlags.Static ||| Reflection.BindingFlags.NonPublic)

    static member Mapi<'a,'c>(columnNames:seq<string>) (map:(int->'a)) (table:Table) : 'c seq =
        if columnNames |> Seq.isEmpty then // map must be (unit->'b) and 'c = 'b; length of the result equals table row count            
            match box map with
            | :? (int -> 'c) as map0 -> Seq.init table.Count map0
            | _ -> failwith "Unexpected type of map"
        else
            columnNames
            |> Seq.map (fun c -> Table.Column c table)
            |> Column.Mapi<'a,'c> map 

    static member private Mapia<'a,'c>(columnNames:seq<string>) (map:(int->'a)) (table:Table) : 'c [] =
        Table.Mapi columnNames map table |> Seq.toArray

    static member private reflectedMapia = typeof<Table>.GetMethod("Mapia", Reflection.BindingFlags.Static ||| Reflection.BindingFlags.NonPublic)

    static member MapToColumn(columnNames:seq<string>) (newColumnName:string) (map:('a->'b)) (table:Table) : Table =
        let names = columnNames |> Seq.toArray
        let data =
            match names.Length with
            | 0 | 1 -> Table.Map<'a,'b,'b> columnNames map table |> Seq.toArray :> System.Array
            | n -> 
                let res = Funcs.getNthResultType n map
                let mapTable = Table.reflectedMapa.MakeGenericMethod( typeof<'a>, typeof<'b>, res )
                mapTable.Invoke(null, [|box columnNames; box map; box table|]) :?> System.Array 
        if table.Names |> Seq.contains newColumnName then table |> Table.Remove [newColumnName] else table
        |> Table.Add newColumnName data

    static member MapiToColumn(columnNames:seq<string>) (newColumnName:string) (map:(int->'a)) (table:Table) : Table =
        let names = columnNames |> Seq.toArray
        let data =
            match names.Length with
            | 0 -> Table.Mapi<'a,'a> columnNames map table |> Seq.toArray :> System.Array
            | n -> 
                let res = Funcs.getNthResultType (n+1) map
                let mapTable = Table.reflectedMapia.MakeGenericMethod( typeof<'a>, res )
                mapTable.Invoke(null, [|box columnNames; box map; box table|]) :?> System.Array 
        if table.Names |> Seq.contains newColumnName then table |> Table.Remove [newColumnName] else table
        |> Table.Add newColumnName data

    static member TryPdf(columnName:string) (pointCount:int) (table:Table) : (float[] * float[]) option =
        table
        |> Table.TryColumn columnName
        |> Option.bind (Column.TryPdf pointCount)

    static member Pdf(columnName:string) (pointCount:int) (table:Table) : (float[] * float[]) =
        table
        |> Table.Column columnName
        |> Column.Pdf pointCount

    static member TryToRealArray(columnName:string) (table:Table) : IRArray<float> option =
        table
        |> Table.TryColumn columnName
        |> Option.bind Column.TryToRealArray

    static member ToRealArray(columnName:string) (table:Table) : IRArray<float> =
        table
        |> Table.Column columnName
        |> Column.ToRealArray

    static member TrySummary(columnName:string) (table:Table) : ColumnSummary option =
        table
        |> Table.TryColumn columnName
        |> Option.map Column.Summary

    static member Summary(columnName:string) (table:Table) : ColumnSummary =
        table
        |> Table.Column columnName
        |> Column.Summary

    static member Transform<'a,'b,'c> (columnNames:seq<string>) (transform:('a->'b)) (table:Table) : 'c =
        let cs = Seq.toArray columnNames
        if cs.Length = 1 then
            match box transform with
            | (:? ('a->'c) as transform1) ->  table |> Table.ToArray<'a> cs.[0] |> transform1
            | _ -> failwith "Transform function cannot be applied to the given column"
        else 
            let deleg = Funcs.toDelegate transform
            let colArrays = cs |> Array.map(fun c -> let col = Table.Column c table in Column.ToArray<System.Array> col) |> Array.map (fun a -> a :> obj)
            deleg.DynamicInvoke(colArrays) :?> 'c

    static member TryCorrelation(table:Table) : (string[] * float[][]) option =
        let realColumnChooser(name:string, column:Column) : (string * float[]) option =
            column
            |> Column.TryToRealArray
            |> Option.map (fun ir -> (name, ir.ToArray()))

        let realNames, realColumns:string[] * float[][] =
            Seq.zip table.Names table.Columns
            |> Seq.choose realColumnChooser
            |> Seq.toArray
            |> Array.unzip

        let n = realColumns.Length

        if n <= 1 then None
        else
            let corr:float[][] = Array.zeroCreate (n-1)

            for i = 0 to n-2 do
                let corri:float[] = Array.zeroCreate (n-i-1)
                corr.[i] <- corri
                let x1 = realColumns.[i]
                for j = 0 to n-i-2 do
                    let x2 = realColumns.[i+j+1]
                    corri.[j] <- correlation x1 x2

            Some(realNames, corr)

    static member Correlation(table:Table) : (string[] * float[][]) =
        table
        |> Table.TryCorrelation
        |> Util.unpackOrFail "At least two columns must be real or int"

    static member ReadStream (settings:Angara.Data.DelimitedFile.ReadSettings) (stream:IO.Stream) : Table =
        let cols = 
            stream 
            |> Angara.Data.DelimitedFile.Implementation.Read settings
            |> Array.map(fun (schema, data) -> 
                schema.Name,
                match schema.Type with
                | Angara.Data.DelimitedFile.ColumnType.Double -> Column.New (data :?> float[])
                | Angara.Data.DelimitedFile.ColumnType.Integer -> Column.New (data :?> int[])
                | Angara.Data.DelimitedFile.ColumnType.Boolean -> Column.New (data :?> bool[])
                | Angara.Data.DelimitedFile.ColumnType.DateTime -> Column.New (data :?> DateTime[])
                | Angara.Data.DelimitedFile.ColumnType.String -> Column.New (data :?> string[]))
        new Table(cols)

    static member WriteStream (settings:Angara.Data.DelimitedFile.WriteSettings) (stream:IO.Stream) (table:Table) : unit =
        table.Columns
        |> Seq.map(fun column ->
            Table.Name column table,
            Column.ToArray<System.Array> column)         
        |> Angara.Data.DelimitedFile.Implementation.Write settings stream            

    static member Read (settings:Angara.Data.DelimitedFile.ReadSettings) (path:string) : Table =
        use stream = new System.IO.FileStream(path, System.IO.FileMode.Open, System.IO.FileAccess.Read)
        Table.ReadStream settings stream

    static member Write (settings:Angara.Data.DelimitedFile.WriteSettings) (path:string) (table:Table) : unit =
        use stream = new System.IO.FileStream(path, System.IO.FileMode.Create, System.IO.FileAccess.Write)
        Table.WriteStream settings stream table