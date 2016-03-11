namespace Angara.Data

open System
open System.Collections.Generic
open System.Collections.Immutable
open Angara.Statistics

open Util

type DataValue =
    | IntValue      of int
    | RealValue     of float
    | StringValue   of string
    | DateValue     of DateTime
    | BooleanValue  of Boolean
    member x.AsInt     = match x with IntValue v     -> v | _ -> invalidCast "The value is not integer"
    member x.AsReal    = match x with RealValue v    -> v | _ -> invalidCast "The value is not real"
    member x.AsString  = match x with StringValue v  -> v | _ -> invalidCast "The value is not a string"
    member x.AsDate    = match x with DateValue v    -> v | _ -> invalidCast "The value is not a date"
    member x.AsBoolean = match x with BooleanValue v -> v | _ -> invalidCast "The value is not boolean"


[<NoComparison>]
type ColumnValues =
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
        | IntColumn v     -> v.Value.[rowIndex] |> IntValue
        | RealColumn v    -> v.Value.[rowIndex] |> RealValue
        | StringColumn v  -> v.Value.[rowIndex] |> StringValue
        | DateColumn v    -> v.Value.[rowIndex] |> DateValue
        | BooleanColumn v -> v.Value.[rowIndex] |> BooleanValue

    member internal x.ToUntypedList() : System.Collections.IList = 
        match x with
        | IntColumn v     -> upcast v.Value
        | RealColumn v    -> upcast v.Value
        | StringColumn v  -> upcast v.Value
        | DateColumn v    -> upcast v.Value
        | BooleanColumn v -> upcast v.Value 

    member internal x.ToImmutableArray<'a>() : ImmutableArray<'a> = 
        match x with
        | IntColumn v     -> v.Value |> coerce
        | RealColumn v    -> v.Value |> coerce
        | StringColumn v  -> v.Value |> coerce
        | DateColumn v    -> v.Value |> coerce
        | BooleanColumn v -> v.Value |> coerce

    override x.ToString() =
        let notEvaluated = "Array is not evaluated yet"
        match x with
        | IntColumn v     -> if v.IsValueCreated then sprintf "int %A" v.Value else notEvaluated
        | RealColumn v    -> if v.IsValueCreated then sprintf "float %A" v.Value else notEvaluated
        | StringColumn v  -> if v.IsValueCreated then sprintf "string %A" v.Value else notEvaluated
        | DateColumn v    -> if v.IsValueCreated then sprintf "date %A" v.Value else notEvaluated
        | BooleanColumn v -> if v.IsValueCreated then sprintf "bool %A" v.Value else notEvaluated
    
    static member internal Select (mask:bool[]) (column:ColumnValues) : ColumnValues =
        match column with
        | IntColumn v     -> lazy(select mask v.Value) |> IntColumn 
        | RealColumn v    -> lazy(select mask v.Value) |> RealColumn 
        | StringColumn v  -> lazy(select mask v.Value) |> StringColumn 
        | DateColumn v    -> lazy(select mask v.Value) |> DateColumn 
        | BooleanColumn v -> lazy(select mask v.Value) |> BooleanColumn 

[<NoComparison>]
type Column private (name:string, values: ColumnValues, height: int) =
    
    do if name = null then raise (ArgumentNullException("name", "Column name cannot be null"))

    member x.Name with get() = name
    member x.Rows with get() = values
    member x.Height with get() = height
    
    override this.ToString() = sprintf "%s[%d]: %O" this.Name height this.Rows

    static member OfColumnValues (name:string, values:ColumnValues, count: int) : Column =
        Column(name, values, count)

    static member OfArray (name:string, rows:'a[]) : Column =
        Column.OfArray(name, ImmutableArray.Create<'a>(rows))

    static member OfArray<'a> (name:string, rows:ImmutableArray<'a>) : Column =
        Column.OfLazyArray (name, Lazy.CreateFromValue(rows), rows.Length)

    static member OfArray (name:string, rows:System.Array) : Column =
        match rows with
        | null -> raise (new ArgumentNullException("rows"))
        | rows ->
            let columnValues =
                match rows.GetType().GetElementType() with
                | et when et = typeof<int>      -> lazy (ImmutableArray.Create<int>(rows :?> int[])) |> IntColumn
                | et when et = typeof<float>    -> lazy (ImmutableArray.Create<float>(rows :?> float[])) |> RealColumn
                | et when et = typeof<string>   -> lazy (ImmutableArray.Create<string>(rows :?> string[])) |> StringColumn 
                | et when et = typeof<DateTime> -> lazy (ImmutableArray.Create<DateTime>(rows :?> DateTime[])) |> DateColumn 
                | et when et = typeof<Boolean>  -> lazy (ImmutableArray.Create<Boolean>(rows :?> Boolean[])) |> BooleanColumn 
                | et -> failwithf "Unexpected array element type `%A`" et
            Column.OfColumnValues(name, columnValues, rows.Length)

    static member OfLazyArray (name:string, lazyRows:Lazy<ImmutableArray<'a>>, count: int) : Column =
        let values =
            match typeof<'a> with
            | t when t = typeof<int>      -> coerce lazyRows |> ColumnValues.IntColumn    
            | t when t = typeof<float>    -> coerce lazyRows |> ColumnValues.RealColumn   
            | t when t = typeof<string>   -> coerce lazyRows |> ColumnValues.StringColumn 
            | t when t = typeof<DateTime> -> coerce lazyRows |> ColumnValues.DateColumn   
            | t when t = typeof<bool>     -> coerce lazyRows |> ColumnValues.BooleanColumn  
            | t -> raise (new NotSupportedException(sprintf "Type '%A' is not a valid column type" t))
        Column(name, values, count)

type Table private (columns : Column list, height : int) =
    static let emptyTable = new Table(List.Empty)
    static let raiseDiffHeights() = invalidOp("Given columns are of different heights")
    static let assertAndGetHeight (columns:Column list) =
        match columns with
        | [] -> 0
        | c :: cols -> let n = c.Height in if not(cols |> List.forall (fun q -> q.Height = n)) then raiseDiffHeights() else n

    new(columns:Column seq) = 
        let columns_l = columns |> Seq.toList
        Table(columns_l, assertAndGetHeight columns_l)

    member internal x.Columns = columns

    member x.Count with get() : int = columns.Length
    member x.RowsCount with get() : int = height

    member x.Item with get(index:int) : Column = columns.[index]
    member x.Item 
        with get(name:string) : Column = 
            match columns |> List.tryFind(fun c -> c.Name = name) with
            | Some c -> c
            | None -> notFound (sprintf "Column '%s' not found" name)

    member x.TryItem
        with get(index:int) : Column option = 
            if index >= 0 && index < columns.Length then Some columns.[index] else None
    member x.TryItem
        with get(name:string) : Column option = 
            columns |> List.tryFind(fun c -> c.Name = name)

    interface IEnumerable<Column> with
        member x.GetEnumerator() : IEnumerator<Column> = (columns |> Seq.ofList).GetEnumerator()
        member x.GetEnumerator() : System.Collections.IEnumerator = ((columns |> Seq.ofList) :> System.Collections.IEnumerable).GetEnumerator()

    member x.ToRows<'r>() : 'r seq =
        failwith ""

    override x.ToString() = String.Join("\n", columns |> Seq.map (fun c -> c.ToString()))


    static member Empty: Table = emptyTable        
    
    static member Add (column: Column) (table:Table) : Table = 
        match table.Columns with
        | [] as cols -> Table([column])
        | cols -> if column.Height = table.RowsCount then Table(cols @ [column], table.RowsCount) else raiseDiffHeights()

    static member Remove (columnNames:seq<string>) (table:Table) : Table =
        match table.Columns |> List.filter(fun c -> not(columnNames |> Seq.contains c.Name)) with
        | [] -> Table.Empty
        | cols -> Table(cols, table.RowsCount)

    static member Filter (columnNames:seq<string>) (predicate:'a->'b) (table:Table) : Table =
        let mask = table |> Table.Map columnNames predicate |> Seq.toArray
        let mutable n = 0
        for i = 0 to mask.Length-1 do if mask.[i] then n <- n + 1
        let newColumns = table.Columns |> List.map (fun c -> Column.OfColumnValues (c.Name, ColumnValues.Select mask c.Rows, n))
        Table(newColumns, n)

    static member Filteri (columnNames:seq<string>) (predicate:int->'a) (table:Table) : Table =
        let mask = table |> Table.Mapi columnNames predicate |> Seq.toArray
        let mutable n = 0
        for i = 0 to mask.Length-1 do if mask.[i] then n <- n + 1
        let newColumns = table.Columns |> List.map (fun c -> Column.OfColumnValues (c.Name, ColumnValues.Select mask c.Rows, n))
        Table(newColumns, n)

    static member Append (table1:Table) (table2:Table) : Table =
        if table1.RowsCount = table2.RowsCount then Table(table1.Columns @ table2.Columns, table1.RowsCount) else raiseDiffHeights()

    static member AppendTransform(columnNames:seq<string>) (transform:ImmutableArray<'a>->'b) (table:Table) : Table =
        Table.Transform columnNames transform table
        |> Table.Append table

    static member Map<'a,'b,'c>(columnNames:seq<string>) (map:'a->'b) (table:Table) : 'c seq =        
        let columns = columnNames |> Seq.map (fun name -> table.[name]) |> Seq.toArray
        match columns.Length with
        | 0 -> // no columns; map must be (unit->'b) and 'c = 'b; length of the result equals table row count            
            match box map with
            | :? (unit -> 'c) as map0 -> let v = map0() in Seq.init table.RowsCount (fun _ -> v)
            | _ -> failwith "Unexpected type of the given function `map`"
        | 1 -> // 1 column; map must be ('a->'c)
            match box map with
            | (:? ('a->'c) as map1) -> 
                match columns.[0].Rows, box map1 with
                | IntColumn v, (:? (int->'c) as tmap) -> v.Value |> Seq.map tmap
                | RealColumn v, (:? (float->'c) as tmap) -> v.Value |> Seq.map tmap
                | StringColumn v, (:? (string->'c) as tmap) -> v.Value |> Seq.map tmap
                | DateColumn v, (:? (DateTime->'c) as tmap) -> v.Value |> Seq.map tmap
                | BooleanColumn v, (:? (Boolean->'c) as tmap) -> v.Value |> Seq.map tmap
                | _ -> failwith("Incorrect argument type of the given function `map`")
            | _ -> failwith("Incorrect `map` function")
        | _ -> // more than 1 column
            let colArrays = columns |> Array.map (fun c -> c.Rows.ToUntypedList())
            let deleg = Funcs.toDelegate map
            let row : obj[] = Array.zeroCreate columns.Length
            Seq.init table.RowsCount (fun rowIdx ->    
                for i = 0 to columns.Length-1 do row.[i] <- colArrays.[i].[rowIdx]
                deleg.DynamicInvoke(row) :?> 'c)

    static member Mapi<'a,'c>(columnNames:seq<string>) (map:(int->'a)) (table:Table) : 'c seq =
        let columns = columnNames |> Seq.map (fun name -> table.[name]) |> Seq.toArray
        match columns.Length with
        | 0 -> // map must be (unit->'b) and 'c = 'b; length of the result equals table row count            
            match box map with
            | :? (int -> 'c) as map0 -> Seq.init table.Count map0
            | _ -> failwith "Unexpected type of the given function `map`"
        | 1 -> // 1 column; map must be ('a->'c)
            match columns.[0].Rows, box map with
            | IntColumn v, (:? (int->int->'c) as tmap) -> v.Value |> Seq.mapi tmap
            | RealColumn v, (:? (int->float->'c) as tmap) -> v.Value |> Seq.mapi tmap
            | StringColumn v, (:? (int->string->'c) as tmap) -> v.Value |> Seq.mapi tmap
            | DateColumn v, (:? (int->DateTime->'c) as tmap) -> v.Value |> Seq.mapi tmap
            | BooleanColumn v, (:? (int->Boolean->'c) as tmap) -> v.Value |> Seq.mapi tmap
            | _ -> failwith("Incorrect `map` function")
        | _ -> // more than 1 column
            let colArrays = columns |> Array.map (fun c -> c.Rows.ToUntypedList())
            let deleg = Funcs.toDelegate map
            let row : obj[] = Array.zeroCreate (1 + columns.Length)
            Seq.init table.RowsCount (fun rowIdx ->    
                row.[0] <- box rowIdx
                for i = 1 to columns.Length do row.[i] <- colArrays.[i-1].[rowIdx]
                deleg.DynamicInvoke(row) :?> 'c)
        
    static member private MapToColumn_a<'a,'b,'c> (columnNames:seq<string>) (newColumnName:string) (map:('a->'b)) (table:Table) : Table =
        let columnArray = Table.Map<'a,'b,'c> columnNames map table |> ImmutableArray.CreateRange
        if table.Columns |> List.exists (fun c -> c.Name = newColumnName) then table |> Table.Remove [newColumnName] else table
        |> Table.Add (Column.OfArray(newColumnName, columnArray))

    static member private reflectedMapToColumn_a = typeof<Table>.GetMethod("MapToColumn_a", Reflection.BindingFlags.Static ||| Reflection.BindingFlags.NonPublic)

    static member MapToColumn(columnNames:seq<string>) (newColumnName:string) (map:('a->'b)) (table:Table) : Table =
        let names = columnNames |> Seq.toArray
        match names.Length with
        | 0 | 1 -> Table.MapToColumn_a<'a,'b,'b> names newColumnName map table
        | n -> 
            let res = Funcs.getNthResultType n map
            let mapTable = Table.reflectedMapToColumn_a.MakeGenericMethod( typeof<'a>, typeof<'b>, res )
            mapTable.Invoke(null, [|box names; box newColumnName; box map; box table|]) :?> Table
    
    static member private MapiToColumn_a<'a,'c> (columnNames:seq<string>) (newColumnName:string) (map:(int->'a)) (table:Table) : Table =
        let columnArray = Table.Mapi<'a,'c> columnNames map table |> ImmutableArray.CreateRange
        if table.Columns |> List.exists (fun c -> c.Name = newColumnName) then table |> Table.Remove [newColumnName] else table
        |> Table.Add (Column.OfArray(newColumnName, columnArray))

    static member private reflectedMapiToColumn_a = typeof<Table>.GetMethod("MapiToColumn_a", Reflection.BindingFlags.Static ||| Reflection.BindingFlags.NonPublic)

    static member MapiToColumn(columnNames:seq<string>) (newColumnName:string) (map:(int->'a)) (table:Table) : Table =
        let names = columnNames |> Seq.toArray
        match names.Length with
        | 0 -> Table.MapiToColumn_a<'a,'a> names newColumnName map table
        | n -> 
            let res = Funcs.getNthResultType (n+1) map
            let mapTable = Table.reflectedMapiToColumn_a.MakeGenericMethod( typeof<'a>, res )
            mapTable.Invoke(null, [|box names; box newColumnName; box map; box table|]) :?> Table

    static member Transform<'a,'b,'c> (columnNames:seq<string>) (transform:(ImmutableArray<'a>->'b)) (table:Table) : 'c =
        let cs = Seq.toArray columnNames
        if cs.Length = 1 then
            match box transform with
            | (:? (ImmutableArray<'a>->'c) as transform1) -> table.[cs.[0]].Rows.ToImmutableArray<'a>() |> transform1
            | _ -> failwith "The given transform function cannot be applied to the column"
        else 
            let deleg = Funcs.toDelegate transform
            let colArrays = cs |> Array.map(fun n -> table.[n].Rows.ToUntypedList() |> box)
            deleg.DynamicInvoke(colArrays) :?> 'c
            

    static member Load (reader:System.IO.TextReader, settings:Angara.Data.DelimitedFile.ReadSettings) : Table =
        let cols = 
            reader 
            |> Angara.Data.DelimitedFile.Implementation.Read settings
            |> Seq.map(fun (schema, data) ->                 
                match schema.Type with
                | Angara.Data.DelimitedFile.ColumnType.Double   -> Column.OfArray (schema.Name, data :?> ImmutableArray<float>)
                | Angara.Data.DelimitedFile.ColumnType.Integer  -> Column.OfArray (schema.Name, data :?> ImmutableArray<int>)
                | Angara.Data.DelimitedFile.ColumnType.Boolean  -> Column.OfArray (schema.Name, data :?> ImmutableArray<bool>)
                | Angara.Data.DelimitedFile.ColumnType.DateTime -> Column.OfArray (schema.Name, data :?> ImmutableArray<DateTime>)
                | Angara.Data.DelimitedFile.ColumnType.String   -> Column.OfArray (schema.Name, data :?> ImmutableArray<string>))
        new Table(cols |> Seq.toList)
    static member Load (reader:System.IO.TextReader) : Table = 
        Table.Load (reader, Angara.Data.DelimitedFile.ReadSettings.Default)
    static member Load (path: string, settings:Angara.Data.DelimitedFile.ReadSettings) : Table = 
        use reader = System.IO.File.OpenText path
        Table.Load (reader, settings)
    static member Load (path: string) : Table = 
        Table.Load (path, Angara.Data.DelimitedFile.ReadSettings.Default)

    static member Save (table:Table, writer:System.IO.TextWriter, settings:Angara.Data.DelimitedFile.WriteSettings) : unit =
        table
        |> Seq.map(fun column -> column.Name, column.Rows.ToUntypedList())
        |> Angara.Data.DelimitedFile.Implementation.Write settings writer
    static member Save (table:Table, writer:System.IO.TextWriter) : unit = 
        Table.Save (table, writer, Angara.Data.DelimitedFile.WriteSettings.Default)
    static member Save (table:Table, path: string, settings:Angara.Data.DelimitedFile.WriteSettings) : unit = 
        use writer = System.IO.File.CreateText path
        Table.Save (table, writer, settings)
    static member Save (table:Table, path: string) : unit =
        Table.Save (table, path, Angara.Data.DelimitedFile.WriteSettings.Default)