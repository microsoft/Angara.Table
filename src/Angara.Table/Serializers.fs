module Angara.Data.TableSerializers

open System
open Angara.Serialization
open Angara.Data.DelimitedFile

module internal HelpersForReinstate = 
    type TableBlob(table : Table) = 
        interface IBlob with
            
            member this.GetStream() = 
                let stream = new IO.MemoryStream()
                (this :> IBlob).WriteTo stream
                stream.Seek(0L, IO.SeekOrigin.Begin) |> ignore
                stream :> IO.Stream
            
            member this.WriteTo stream = 
                table |> Table.Write { WriteSettings.Default with AllowNullStrings = true } stream
    
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


module internal HelpersForHtml =
    let SerializeColumn (c: Column) =
        let colType = Column.Type c
        if colType = typeof<double> then InfoSet.DoubleArray(Column.ToArray<double[]> c)
        elif colType = typeof<DateTime> then InfoSet.DateTimeArray(Column.ToArray<DateTime[]> c)
        elif colType = typeof<int> then InfoSet.IntArray(Column.ToArray<int[]> c)
        elif colType = typeof<string> then InfoSet.StringArray(Column.ToArray<string[]> c)
        elif colType = typeof<bool> then InfoSet.BoolArray(Column.ToArray<bool[]> c)
        else failwith("Cannot serialize table column of type " + colType.FullName)

    let DeserializeColumn (infoSet: InfoSet) = 
        match infoSet with
        | DoubleArray(da) -> Column.New<double[]>(da |> Seq.toArray)
        | IntArray(ia) -> Column.New<int[]>(ia |> Seq.toArray)
        | StringArray(sa) -> Column.New<string[]>(sa |> Seq.toArray)
        | DateTimeArray(da) -> Column.New<DateTime[]>(da |> Seq.toArray)
        | BoolArray(ba) -> Column.New<bool[]>(ba |> Seq.toArray)
        | _ -> failwith "Cannot deserialize this InfoSet to Column"

    type ColumnSummarySerializer() =
        interface ISerializer<ColumnSummary> with
            member x.TypeId = "Table.ColumnSummary" 

            member x.Serialize _ summary = 
                match summary with
                | NumericColumnSummary(sum) -> InfoSet.ofPairs(["type", InfoSet.String("numeric")
                                                                "min", InfoSet.Double(sum.Min)
                                                                "lb95", InfoSet.Double(sum.Lb95)
                                                                "lb68", InfoSet.Double(sum.Lb68)
                                                                "median", InfoSet.Double(sum.Median)
                                                                "ub68", InfoSet.Double(sum.Ub68)
                                                                "ub95", InfoSet.Double(sum.Ub95)
                                                                "max", InfoSet.Double(sum.Max)
                                                                "mean", InfoSet.Double(sum.Mean)
                                                                "variance", InfoSet.Double(sum.Variance)
                                                                "totalCount", InfoSet.Int(sum.TotalCount)
                                                                "count", InfoSet.Int(sum.Count)
                                                               ])
                | StringColumnSummary(sum) -> InfoSet.ofPairs(["type", InfoSet.String("string")
                                                               "min", InfoSet.String(sum.Min)
                                                               "max", InfoSet.String(sum.Max)
                                                               "totalCount", InfoSet.Int(sum.TotalCount)
                                                               "count", InfoSet.Int(sum.Count)
                                                              ])
                | DateColumnSummary(sum) -> InfoSet.ofPairs(["type", InfoSet.String("date")
                                                             "min", InfoSet.DateTime(sum.Min)
                                                             "max",  InfoSet.DateTime(sum.Max)
                                                             "totalCount", InfoSet.Int(sum.TotalCount)
                                                             "count", InfoSet.Int(sum.Count)
                                                            ])
                | BooleanColumnSummary(sum) -> InfoSet.ofPairs(["type", InfoSet.String("bool")
                                                                "false", InfoSet.Int(sum.FalseCount)
                                                                "true", InfoSet.Int(sum.TrueCount)])

            member x.Deserialize _ si = 
                let map = si.ToMap()
                let sumType = map.["type"].ToStringValue()

                match sumType with 
                | "numeric" -> { RealColumnSummary.Min = map.["min"].ToDouble()
                                 Lb95 = map.["lb95"].ToDouble()
                                 Lb68 = map.["lb68"].ToDouble()
                                 Median = map.["median"].ToDouble()
                                 Ub68 = map.["ub68"].ToDouble()
                                 Ub95 = map.["ub95"].ToDouble()
                                 Max = map.["max"].ToDouble()
                                 Mean = map.["mean"].ToDouble()
                                 Variance = map.["variance"].ToDouble()
                                 TotalCount = map.["TotalRows"].ToInt()
                                 Count = map.["TotalRows"].ToInt()
                               }
                               |> NumericColumnSummary

                | "string" -> { Min = map.["min"].ToStringValue()
                                Max = map.["max"].ToStringValue()
                                TotalCount = map.["TotalRows"].ToInt()
                                Count = map.["DataRows"].ToInt()
                              }
                              |> StringColumnSummary

                | "date" -> { Min = map.["min"].ToDateTime()
                              Max = map.["max"].ToDateTime()
                              TotalCount = map.["TotalRows"].ToInt()
                              Count = map.["DataRows"].ToInt()
                            }
                            |> DateColumnSummary

                | "bool" -> { TrueCount = map.["true"].ToInt()
                              FalseCount = map.["false"].ToInt() } |> BooleanColumnSummary

                | _ -> failwith("type " + sumType + " is not supported")

    type PdfSerializer() = 
        interface ISerializer<float[] * float[]> with
            member x.TypeId = "Table.GetPdf"

            member x.Serialize _ pdf = 
                let x,f = pdf
                InfoSet.ofPairs([("x", InfoSet.DoubleArray(x)); ("f", InfoSet.DoubleArray(f))])

            member x.Deserialize _ si =
                let map = si.ToMap()
                map.["x"].ToDoubleArray(), map.["f"].ToDoubleArray()

    type CorrelationSerializer() = 
        interface ISerializer<string array * float array array> with
            member x.TypeId = "Table.GetCorrelation"

            member x.Serialize _ corr = 
                let c,r = corr
                InfoSet.ofPairs([("c", InfoSet.StringArray(c)); 
                                 ("r", InfoSet.Seq(r |> Seq.map (fun value -> InfoSet.DoubleArray(value))))])

            member x.Deserialize _ si = 
                let map = si.ToMap()
                let c = map.["c"].ToStringArray() |> Seq.toArray 
                let r = map.["r"].ToSeq() 
                        |> Seq.fold (fun resArray siArray -> match siArray with 
                                                             | InfoSet.DoubleArray(array) -> Array.append resArray ([array |> Array.ofSeq] |> List.toArray)
                                                             | _ -> failwith("array type is not supported")) Array.empty
                c,r

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
                        |> Implementation.Read
                             { ReadSettings.Default with 
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
        
open HelpersForHtml

type TableHtmlSerializer() = 
    interface ISerializer<Table> with
        member x.TypeId = "Table"
        member x.Serialize resolver (t : Table) = 
            let corrSr = CorrelationSerializer() :> Angara.Serialization.ISerializer<string array * float array array>
            let summarySr = ColumnSummarySerializer() :> Angara.Serialization.ISerializer<ColumnSummary>
            let pdfSr = PdfSerializer() :> Angara.Serialization.ISerializer<float array * float array>
            InfoSet.EmptyMap
                .AddInt("count", t.Count)
                .AddInfoSet("correlation", match Table.TryCorrelation t with
                                           | Some(c) -> corrSr.Serialize resolver c
                                           | None -> InfoSet.Null)
                .AddSeq("summary", [ for colName in t.Names -> let infoSet = summarySr.Serialize resolver (Table.Summary colName t)
                                                               infoSet.AddString("name", colName) ])
                .AddSeq("pdf", [ for col in t.Columns -> match Column.TryPdf 512 col with
                                                         | Some(p) -> pdfSr.Serialize resolver p
                                                         | None -> InfoSet.Null])
                .AddInfoSet("data", InfoSet.Seq(t.Columns |> Seq.map SerializeColumn))

        member x.Deserialize _ _ = failwith "Table deserialization is not supported"


// Registers proper serializers in given libraries.
let Register(libraries: SerializerLibrary seq) =
    for lib in libraries do
        match lib.Name with
        | "Reinstate" -> 
            lib.Register(TableReinstateSerializer()) 
        | "Html" ->
            lib.Register(TableHtmlSerializer())
        | _ -> () // nothing to register
