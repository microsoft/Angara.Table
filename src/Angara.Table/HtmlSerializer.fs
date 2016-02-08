namespace Angara.Data.Serialization

open System
open System.Collections.Generic
open Angara.Serialization
open Angara.Data

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
                | NumericColumnSummary(sum) -> InfoSet.ofPairs([("type", InfoSet.String("numeric"))
                                                                ("min", InfoSet.Double(sum.Min))
                                                                ("lb95", InfoSet.Double(sum.Lb95))
                                                                ("lb68", InfoSet.Double(sum.Lb68))
                                                                ("median", InfoSet.Double(sum.Median))
                                                                ("ub68", InfoSet.Double(sum.Ub68))
                                                                ("ub95", InfoSet.Double(sum.Ub95))
                                                                ("max", InfoSet.Double(sum.Max))
                                                                ("mean", InfoSet.Double(sum.Mean))
                                                                ("variance", InfoSet.Double(sum.Variance))
                                                                ("TotalRows", InfoSet.Int(sum.TotalRows))
                                                               ])
                | StringColumnSummary(sum) -> InfoSet.ofPairs([("type", InfoSet.String("string"))
                                                               ("min", InfoSet.String(sum.Min))
                                                               ("max", InfoSet.String(sum.Max))
                                                               ("TotalRows", InfoSet.Int(sum.TotalRows))
                                                               ("DataRows", InfoSet.Int(sum.DataRows))
                                                              ])
                | DateColumnSummary(sum) -> InfoSet.ofPairs([("type", InfoSet.String("date"))
                                                             ("min", InfoSet.DateTime(sum.Min))
                                                             ("max",  InfoSet.DateTime(sum.Max))
                                                             ("TotalRows", InfoSet.Int(sum.TotalRows))
                                                             ("DataRows", InfoSet.Int(sum.DataRows))
                                                            ])
                | BooleanColumnSummary(sum) -> InfoSet.ofPairs(["type", InfoSet.String("bool")
                                                                "false", InfoSet.Int(sum.FalseRows)
                                                                "true", InfoSet.Int(sum.TrueRows)])

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
                                 TotalRows = map.["TotalRows"].ToInt()
                                 DataRows = map.["TotalRows"].ToInt()
                               }
                               |> NumericColumnSummary

                | "string" -> { Min = map.["min"].ToStringValue()
                                Max = map.["max"].ToStringValue()
                                TotalRows = map.["TotalRows"].ToInt()
                                DataRows = map.["DataRows"].ToInt()
                              }
                              |> StringColumnSummary

                | "date" -> { Min = map.["min"].ToDateTime()
                              Max = map.["max"].ToDateTime()
                              TotalRows = map.["TotalRows"].ToInt()
                              DataRows = map.["DataRows"].ToInt()
                            }
                            |> DateColumnSummary

                | "bool" -> { TrueRows = map.["true"].ToInt()
                              FalseRows = map.["false"].ToInt() } |> BooleanColumnSummary

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

open HelpersForHtml

type TableHtmlSerializer() = 
    interface ISerializer<Table> with
        member x.TypeId = "Table"
        member x.Serialize resolver (t : Table) = 
            let corrSr = CorrelationSerializer() :> Angara.Serialization.ISerializer<string array * float array array>
            let summarySr = ColumnSummarySerializer() :> Angara.Serialization.ISerializer<ColumnSummary>
            let pdfSr = PdfSerializer() :> Angara.Serialization.ISerializer<float array * float array>
            let n = t.Columns.Count
            InfoSet.EmptyMap
                .AddInt("RowsCount", t.Count)
                .AddInfoSet("Correlation", match Table.TryCorrelation t with
                                           | Some(c) -> corrSr.Serialize resolver c
                                           | None -> InfoSet.Null)
                .AddSeq("Summary", [ for colName in t.Names -> let infoSet = summarySr.Serialize resolver (Table.Summary colName t)
                                                               infoSet.AddString("name", colName) ])
                .AddSeq("Pdf", [ for col in t.Columns -> match Column.TryPdf 512 col with
                                                         | Some(p) -> pdfSr.Serialize resolver p
                                                         | None -> InfoSet.Null])
                .AddInfoSet("Data", InfoSet.Seq(t.Columns |> Seq.map SerializeColumn))

        member x.Deserialize _ _ = failwith "Table deserialization is not supported"


