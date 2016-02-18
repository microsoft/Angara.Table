namespace Angara.Data.DelimitedFile

open System  
open System.Collections.Generic
open System.Text
open System.IO
/// Type of column elements.
type internal ColumnType = 
    | Integer 
    | Double
    | Boolean 
    | DateTime 
    | String 

type internal InferredColumnSchema =
    { Name: string
    ; Type: ColumnType option
    ; IsUserDefined: bool }

/// Describes a table column.
type internal ColumnSchema = 
    { /// Name of a column.
        Name : string
        /// Type of a column.
        Type : ColumnType }

module internal Helpers =
    open System.Globalization
    
    let internal DefaultCulture = CultureInfo.InvariantCulture;

    let internal IsDateTime s (formatProvider:IFormatProvider) : bool =
        DateTime.TryParse(s, formatProvider, DateTimeStyles.None) |> fst

    let internal IsDouble s (formatProvider:IFormatProvider) : bool =
        Double.TryParse(s, NumberStyles.Float, formatProvider) |> fst

    let internal IsBool s (formatProvider:IFormatProvider) : bool =
        bool.TryParse(s) |> fst

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
    let splitRow (delimiter: char) (reader: StreamReader) : string[] option = 
        let missingValue : string = null
        match reader.ReadLine() with
        | null -> None
        | "" -> Some [| |]
        | s ->
            let items = List<string>(32)
            let current = StringBuilder()
            let mutable s = s
            let mutable isEscaping = false
            let mutable wordStart = true
            let mutable doContinue = true
            while doContinue do // single data row can consist of multiple file lines
                let n = s.Length
                let mutable i = 0
                while i < n do
                    let c = s.[i]
                    match wordStart with
                    | true ->
                        current.Clear() |> ignore
                        match c with
                        | c when c = delimiter -> // column is parsed
                            items.Add missingValue // empty item
                        | '"' -> // first char is "                            
                            isEscaping <- true
                            wordStart <- false  
                        | _ -> 
                            current.Append c |> ignore
                            wordStart <- false  
                    | false -> // not a word start char
                        match isEscaping with
                        | true ->
                            match c with
                            | '"' -> // end of escaped string or escaped quote
                                if i < n - 1 && s.[i + 1] = '"' then // "" == ", escaped quotes
                                    i <- i + 1
                                    current.Append '"' |> ignore
                                else // end of escaped string 
                                    isEscaping <- false;
                                    i <- i + 1
                                    while i < n && s.[i] <> delimiter do // copying the remaining of the cell
                                        current.Append s.[i] |> ignore
                                        i <- i + 1

                                    // Another column parsed:
                                    items.Add(current.ToString())

                                    if i < n then
                                        wordStart <- true
                                    else
                                        current.Clear() |> ignore
                            | _ as c -> // inside the string
                                current.Append c |> ignore
                        | false -> // not quoted
                            match c with
                            | c when c = delimiter -> // Another column parsed:
                                items.Add(current.ToString())
                                wordStart <- true                            
                            | _ -> 
                                current.Append c |> ignore
                    i <- i+1
                if isEscaping then 
                    current.Append Environment.NewLine |> ignore
                    s <- reader.ReadLine()
                doContinue <- isEscaping && s <> null
            
            if isEscaping then failwith "Format is incorrect: no matching end quotes"
            if wordStart then // Another column parsed:
                items.Add(missingValue)
            elif current.Length <> 0 then // Another column parsed:
                items.Add(current.ToString())
            Some(items.ToArray())

    /// Reads header from the file specified by the reader.
    let internal readHeader (delimiter:char) (reader : StreamReader) : InferredColumnSchema[]  = 
        match reader |> splitRow delimiter with
        | None -> Array.empty
        | Some items -> 
            items |> Array.map(fun colName -> 
                let name = if colName = null then "" else colName
                { Name = name; Type = None; IsUserDefined = false })

    /// Returns the same items but appended with null strings in order to have same length as number of columns.
    let internal ensureLength (columnsCount:int) (lineItems: string[]) : string[] =
        match lineItems.Length with
        | n when n < columnsCount -> 
            let completeItems = Array.zeroCreate columnsCount
            lineItems.CopyTo(completeItems, 0)
            completeItems
        | n when n = columnsCount -> lineItems
        | _ ->  failwith "Number of data columns is greater than number of header items" // - to do: extend original list with more columns?

    /// Process string items of a source line and updates `schema` 's column types taking into account the given items.
    let internal inferColumnTypes (schema: InferredColumnSchema[]) (items: string[]) : unit =       
        for j = 0 to schema.Length-1 do
            let col = schema.[j]
            let item = items.[j]
            if item <> null && not(col.IsUserDefined) then
                let colType = // Some, if type has changed.
                    match col.Type with 
                    | None -> // type is unknown
                        Some(
                            if IsDouble item DefaultCulture then ColumnType.Double
                            elif IsDateTime item DefaultCulture then ColumnType.DateTime
                            elif IsBool item DefaultCulture then ColumnType.Boolean
                            else ColumnType.String)
                    | Some(ColumnType.Double) when not(IsDouble item DefaultCulture) -> Some ColumnType.String
                    | Some(ColumnType.Boolean) when not(IsBool item DefaultCulture) -> Some ColumnType.String
                    | Some(ColumnType.DateTime) when not(IsDateTime item DefaultCulture) -> Some ColumnType.String
                    | _ -> None
                match colType with
                | Some _ -> schema.[j] <- { col with Type = colType }
                | None -> ()
        
    /// Makes the string proper to write in a CSV file. See rules in comments for splitRow.
    let escapeString (s:string) (delimiter:string) (allowNull:bool) : string =        
        match s with
        | null -> if allowNull then "" else failwith "An array of strings contains null but settings disable null values for strings"
        | s when s.Length = 0 -> "\"\"" // [""]
        | s when s.Contains "\"" -> System.String.Concat("\"", s.Replace("\"", "\"\""), "\"") // ["..."] and ["] -> [""]
        | s when (s.Contains delimiter || s.Contains "\n" || s.Contains "\r") -> System.String.Concat("\"", s, "\"") // ["..."]
        | _ -> s

    let internal delimiterToChar delimiter = 
        match delimiter with
        | Delimiter.Comma -> ','
        | Delimiter.Tab -> '\t'
        | Delimiter.Semicolon -> ';'
        | Delimiter.Space -> ' '
        | _ -> failwith "Unsupported delimiter"

    let internal typeToColumnType (t:System.Type) =
        match t with
        | t when t = typeof<System.Double>  -> ColumnType.Double
        | t when t = typeof<System.Boolean> -> ColumnType.Boolean
        | t when t = typeof<System.String>  -> ColumnType.String
        | t when t = typeof<System.DateTime>-> ColumnType.DateTime
        | t when t = typeof<System.Int32>   -> ColumnType.Integer
        | _ -> failwith "Unexpected column type"

    let internal chars = ['A'..'Z']

    /// Produces a non-empty string to be used as a name of a column with the given index.
    /// The produced names are similar to Excel column names; e.g.
    /// index 28 gives the name "AC".
    let internal indexToName (index: int) =
        if index < 0 then invalidArg "index" "index is negative"
        let k = chars.Length // radix
        let mutable name = List.empty
        let mutable m = index
        while m >= 0 do
            name <- chars.[m % k] :: name
            m <- m / k - 1 // so that 26 is 'AA' as in Excel, but not 'BA'
        new String(name |> List.toArray)

open Helpers
open System.Globalization

/// Implements writer and reader for a text representation of a list of named arrays of certain types.
/// The implementation mostly follows RFC 4180, but:
/// 1) First line always considered as a header.
/// 2) In addition to comma separator, it supports tab, semicolon and space.
[<AbstractClass; Sealed>]
type internal Implementation =
    /// Writes a sequence of named arrays to a stream in a delimited text format (e.g. CSV).
    static member Write (settings:WriteSettings) (stream: Stream) (table:(string * Array) seq) : unit =
        let table = table |> Seq.toArray
        if table.Length > 0 then
            let output = new StreamWriter(stream, Text.Encoding.UTF8, 1024, true) // 1024 mentioned here: http://stackoverflow.com/questions/29412757/what-is-the-default-buffer-size-for-streamwriter
            let delimiter = settings.Delimiter |> Helpers.delimiterToChar
            let delimiterStr = delimiter.ToString()

            if settings.SaveHeader then
                let header = String.Join(delimiterStr, table |> Array.map (fun (name, _) -> Helpers.escapeString name delimiterStr settings.AllowNullStrings))
                output.WriteLine(header)

            let columns = table |> Array.map snd
            for i in 0..columns.Length-2 do
                if columns.[i].Length <> columns.[i + 1].Length then raise (ArgumentException "All arrays must have same length")

            let types = 
                columns 
                |> Array.map(fun column -> 
                    if column.Rank <> 1 then raise(ArgumentException "All arrays must be one-dimensional")
                    column.GetType().GetElementType() |> typeToColumnType)

            let lineCount = columns.[0].Length;
            let sb = new StringBuilder(1024)
            for i in 0..lineCount-1 do
                for j in 0..types.Length-1 do
                    match types.[j] with
                    | ColumnType.Double ->
                        // "R" or "r" : Round-trip :  Result: A string that can round-trip to an identical number.
                        sb.Append((columns.[j] :?> float[]).[i].ToString("R", DefaultCulture)) |> ignore
                    | ColumnType.Integer ->
                        sb.Append((columns.[j] :?> int[]).[i].ToString(DefaultCulture)) |> ignore
                    | ColumnType.Boolean  ->
                        sb.Append((columns.[j] :?> bool[]).[i].ToString(DefaultCulture)) |> ignore
                    | ColumnType.DateTime ->
                        sb.Append((columns.[j] :?> DateTime[]).[i].ToString(DefaultCulture)) |> ignore
                    | ColumnType.String ->
                        let item = (columns.[j] :?> string[]).[i]
                        sb.Append(escapeString item delimiterStr settings.AllowNullStrings) |> ignore

                    if j < types.Length - 1 then (sb.Append(delimiter) |> ignore)
                output.WriteLine(sb.ToString())
                sb.Clear() |> ignore
            output.Flush()

    /// Reads a table from a delimited text format.
    static member Read (settings: ReadSettings) (stream: Stream) : (ColumnSchema * Array) []  =
        let delimiter = settings.Delimiter |> Helpers.delimiterToChar
        let reader = new StreamReader(stream)
    
        // Prepare schema
        let headerSchema, firstRow = 
            match settings.HasHeader with
            | true -> 
                readHeader delimiter reader, splitRow delimiter reader
            | false -> 
                match splitRow delimiter reader with
                | None -> Array.empty, None // empty file
                | Some firstRow -> 
                    Array.init firstRow.Length (fun i -> { Name = indexToName i; Type = None; IsUserDefined = false }), 
                    Some firstRow

        let headerSchema = 
            match settings.ColumnsCount with
            | Some n ->
                if headerSchema.Length = 0 && n = 1 then 
                    [| { Name = ""; Type = None; IsUserDefined = false } |] // cannot be distinguished from text itself
                else 
                    if headerSchema.Length <> n then failwith "Number of columns is different than expected"
                    headerSchema
            | None -> headerSchema
        let schema =
            match settings.ColumnTypes with
            | Some getType ->
                headerSchema |> Array.mapi(fun i s -> 
                    match getType(i, s.Name) with
                    | Some colType -> { s with Type = Some (typeToColumnType colType); IsUserDefined = true }
                    | None -> s)
            | None -> headerSchema
        let colCount = schema.Length

        // Read string lines from the stream and modify column types in 'schema' depending on data items, if required
        let rows =
            Seq.append [firstRow] (Seq.initInfinite(fun _ -> reader |> splitRow delimiter))
            |> Seq.takeWhile Option.isSome
            |> Seq.map(fun _items ->
                let items = ensureLength colCount _items.Value
                do inferColumnTypes schema items // modifies schema
                items)
        let rows = rows |> Seq.toArray
        let rowsCount = rows.Length

        let finalSchema = schema |> Array.map(fun s -> { Name = s.Name; Type = match s.Type with Some(t) -> t | None -> ColumnType.String } )

        // Parse typed values from rows strings for the `finalSchema` column types
        let columns =
            [|
                for colIndex = 0 to finalSchema.Length-1 do
                    let colType = finalSchema.[colIndex].Type
                    yield 
                        match colType with
                        | ColumnType.String when settings.InferNullStrings -> 
                            [| for i = 0 to rowsCount-1 do 
                                yield rows.[i].[colIndex] |] :> Array
                        | ColumnType.String -> // when not(settings.InferNullStrings)
                            [| for i = 0 to rowsCount-1 do 
                                yield match rows.[i].[colIndex] with null -> String.Empty | s -> s |] :> Array
                        | ColumnType.Double ->
                            [| 
                                for i = 0 to rowsCount-1 do 
                                    let value = rows.[i].[colIndex]
                                    yield 
                                        match String.IsNullOrEmpty value with
                                        | false -> Double.Parse(value, NumberStyles.Float, DefaultCulture)
                                        | true -> Double.NaN
                            |] :> Array
                        | ColumnType.Integer ->
                            [| 
                                for i = 0 to rowsCount-1 do 
                                    let value = rows.[i].[colIndex]
                                    yield 
                                        match String.IsNullOrEmpty value with
                                        | false -> Int32.Parse(value, NumberStyles.Integer, DefaultCulture)
                                        | true -> raise (new FormatException(sprintf "Missing integer at row %d, column %d" (i + 2) (colIndex + 1)))
                            |] :> Array
                        | ColumnType.Boolean ->
                            [| 
                                for i = 0 to rowsCount-1 do 
                                    let value = rows.[i].[colIndex]
                                    yield 
                                        match String.IsNullOrEmpty value with
                                        | false -> Boolean.Parse value
                                        | true -> raise (new FormatException(sprintf "Missing boolean at row %d, column %d" (i + 2) (colIndex + 1)))
                            |] :> Array
                        | ColumnType.DateTime ->
                            [| 
                                for i = 0 to rowsCount-1 do 
                                    let value = rows.[i].[colIndex]
                                    yield 
                                        match String.IsNullOrEmpty value with
                                        | false -> DateTime.Parse (value, DefaultCulture)
                                        | true -> raise (new FormatException(sprintf "Missing date time at row %d, column %d" (i + 2) (colIndex + 1)))
                            |] :> Array                       
            |]

        Array.map2 (fun a b -> a,b) finalSchema columns 