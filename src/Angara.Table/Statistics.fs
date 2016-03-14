module Angara.Data.TableStatistics

open System
open Angara.Statistics
open Util
open System.Collections.Immutable

type RealColumnSummary = {
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
    | NumericColumnSummary of RealColumnSummary
    | StringColumnSummary of ComparableColumnSummary<string>
    | DateColumnSummary of ComparableColumnSummary<DateTime>
    | BooleanColumnSummary of BooleanColumnSummary

let internal intToReal (a: ImmutableArray<int>) : ImmutableArray<float> =
    let bld = ImmutableArray.CreateBuilder<float>(a.Length)
    bld.Count <- a.Length
    for i in 0..a.Length-1 do
        bld.[i] <- float(a.[i])
    bld.MoveToImmutable()

let TryCorrelation (table:Table) : (string[] * float[][]) option =
    let realNames, realColumns =
        table
        |> Seq.choose (fun col ->
            match col.Rows with
            | RealColumn v -> (col.Name, v.Value |> immToArray) |> Some
            | IntColumn v -> (col.Name, v.Value |> intToReal |> immToArray) |> Some
            | _ -> None)
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

let Correlation (table:Table) : (string[] * float[][]) =
    table |> TryCorrelation |> Util.unpackOrFail "At least two columns must be real or int"


let SummaryR(a:ImmutableArray<float>) : RealColumnSummary =
    let summary = summary a
    let qsummary = qsummary a
    { Min = summary.min
      Lb95 = qsummary.lb95
      Lb68 = qsummary.lb68
      Median = qsummary.median
      Ub68 = qsummary.ub68
      Ub95 = qsummary.ub95
      Max = summary.max
      Mean = summary.mean
      Variance = summary.variance
      TotalCount = a.Length
      Count = summary.count }

let SummaryI(i:ImmutableArray<int>) : RealColumnSummary = i |> intToReal |> SummaryR

let SummaryS(a:seq<string>) : ComparableColumnSummary<string> =
    let min, max, total, count = 
        Seq.fold (fun (min, max, total, count) s ->
            match s with
            | "" | null -> min, max, total + 1, count
            | _ ->
                let min = if count = 0 || s < min then s else min
                let max = if count = 0 || s > max then s else max
                min, max, total + 1, count + 1
            ) ("", "", 0, 0) a
    { Min = min
      Max = max
      TotalCount = total
      Count = count }

let SummaryD(a:seq<DateTime>) : ComparableColumnSummary<DateTime> =
    let min, max, total = 
        Seq.fold (fun (min, max, total) s ->
            let min = if s < min then s else min
            let max = if s > max then s else max
            min, max, total + 1) (DateTime.MaxValue, DateTime.MinValue, 0) a
    { Min = min
      Max = max
      TotalCount = total
      Count = total }

let SummaryB(a:seq<Boolean>) : BooleanColumnSummary =
    let nT, nF = a |> Seq.fold (fun (nT, nF) s -> if s then nT+1,nF else nT,nF+1) (0, 0)
    { TrueCount = nT
      FalseCount = nF }

let Summary(column:Column) : ColumnSummary =
    match column.Rows with
    | IntColumn ir -> SummaryI ir.Value |> NumericColumnSummary
    | RealColumn ir -> SummaryR ir.Value |> NumericColumnSummary
    | StringColumn ir -> SummaryS ir.Value |> StringColumnSummary
    | DateColumn ir -> SummaryD ir.Value |> DateColumnSummary
    | BooleanColumn ir -> SummaryB ir.Value |> BooleanColumnSummary

let TryPdf (pointCount:int) (column:Column) : (float[] * float[]) option =
    match column.Height with
    | 0 -> None
    | _ -> match column.Rows with     
           | RealColumn v -> v.Value |> kde pointCount |> Some
           | IntColumn v -> v.Value |> intToReal |> kde pointCount |> Some
           | _ -> None 

let Pdf (pointCount:int) (column:Column) : (float[] * float[]) =
    match column.Height with
    | 0 -> invalidArg "column" "Column height is zero"
    | _ -> match column.Rows with     
           | RealColumn v -> v.Value |> kde pointCount
           | IntColumn v -> v.Value |> intToReal |> kde pointCount
           | _ -> invalidArg "column" "Column is neither real nor integer"