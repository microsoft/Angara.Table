namespace Angara.Data.Serialization

open System
open Angara.Data
open Angara.Serialization

module internal HelpersForReinstate = 
    type TableBlob(table : Table) = 
        interface IBlob with
            
            member this.GetStream() = 
                let stream = new IO.MemoryStream()
                (this :> IBlob).WriteTo stream
                stream.Seek(0L, IO.SeekOrigin.Begin) |> ignore
                stream :> IO.Stream
            
            member this.WriteTo stream = 
                table |> Table.Write { WriteSettings.CommaDelimited with AllowNullStrings = true } stream
    
    let serializeContent (table : Table) (blobName : string) : InfoSet = 
        let content = Seq.zip table.Names table.Types |> List.ofSeq
        InfoSet.ofPairs [ "schema", 
                          InfoSet.Seq
                              (content 
                               |> List.map 
                                      (fun (name, type') -> 
                                      InfoSet.ofPairs 
                                          [ "name", InfoSet.String name                                            
                                            "type", 
                                            InfoSet.String
                                                (if type' = typeof<int> then "Int"
                                                 elif type' = typeof<float> then "Real"
                                                 elif type' = typeof<string> then "String"
                                                 elif type' = typeof<DateTime> then "DateTime"
                                                 elif type' = typeof<bool> then "Boolean"
                                                 else failwith ("type " + (type'.FullName) + " is not supported in table")) ]))
                          "data", InfoSet.Blob(blobName, TableBlob table) ]


/// The serializer keeps table schema as typed InfoSet and 
/// table data formatted as CSV and represented as a InfoSet blob.
type TableReinstateSerializer() =
    static member DataFactory<'a>(data:Lazy<Array[]>, i:int) : Lazy<'a[]> =
            lazy(data.Value.[i] :?> 'a[])

    interface ISerializer<Table> with
        member x.TypeId: string = "Table"

        member x.Deserialize _ (si : InfoSet) = 
            let map = si.ToMap()
    
            let schema = map.["schema"].ToSeq()
            let columnNames = schema |> Seq.map (fun value -> value.ToMap().["name"].ToStringValue())
            let columnTypes = schema |> Seq.map (fun value -> value.ToMap().["type"].ToStringValue()) |> Seq.toArray

            let colType colInd =
                match columnTypes.[colInd] with
                | "Int" -> typeof<int>
                | "Real" -> typeof<float>
                | "String" -> typeof<string>
                | "DateTime" -> typeof<DateTime>
                | "Boolean" -> typeof<bool>
                | s -> failwithf "Type %s is not supported by the table serializer" s

            let data:Lazy<Array[]> =
                lazy(
                    let stream = (snd (map.["data"].ToBlob())).GetStream()
                    let cols = 
                        stream 
                        |> DelimitedFile.Read
                             { ReadSettings.CommaDelimited with 
                                InferNullStrings = true
                                ColumnTypes = Some(fun (colInd,_) -> Some(colType colInd))
                                ColumnsCount = Some columnTypes.Length } 
                        |> Array.map snd
                    cols)

            let columns:seq<Column> = 
                columnTypes
                |> Seq.mapi (fun i columnType ->
                    match columnType with
                    | "Int" -> Column.New(LazyRArray<int>(TableReinstateSerializer.DataFactory<int>(data, i)) :> IRArray<int>)
                    | "Real" -> Column.New(LazyRArray<float>(TableReinstateSerializer.DataFactory<float>(data, i)) :> IRArray<float>)
                    | "DateTime" -> Column.New(LazyRArray<DateTime>(TableReinstateSerializer.DataFactory<DateTime>(data, i)) :> IRArray<DateTime>)
                    | "String" -> Column.New(LazyRArray<string>(TableReinstateSerializer.DataFactory<string>(data, i))  :> IRArray<string>)
                    | "Boolean" -> Column.New(LazyRArray<Boolean>(TableReinstateSerializer.DataFactory<Boolean>(data, i))  :> IRArray<Boolean>)
                    | s -> failwithf "Type %s is not supported by the table serializer" s)

            new Table(columnNames, columns)
        
        member x.Serialize _ t  = HelpersForReinstate.serializeContent t "csv"
        
