module Angara.Data.TestsF.Common

open Angara.Data
open NUnit.Framework
open System

type PropertyAttribute = FsCheck.NUnit.PropertyAttribute

let random : Random = new Random()

let randomString (random:Random) (length:int) : string =
    let chars = "ABCDEFGHIJKLMNOPQRSTUVWUXYZ0123456789"

    let rndChars =
        seq { 0 .. (length-1) }
        |> Seq.map (fun _ -> chars.[random.Next(chars.Length)])
        |> Seq.toArray

    string(rndChars)

let randomDate (random:Random) : DateTime =
    let start = new DateTime(0L)
    let range = (DateTime.Today - start).Days

    start.AddDays(float(random.Next(range)))

let randomDoubleArray (random:Random) (length:int) : double[] =
    seq { 0 .. (length-1) }
    |> Seq.map (fun _ -> random.NextDouble())
    |> Seq.toArray

let randomIntArray (random:Random) (length:int) : int[] =
    seq { 0 .. (length-1) }
    |> Seq.map (fun _ -> random.Next())
    |> Seq.toArray

let randomStringArray (random:Random) (length1:int) (length2:int) : string[] =
    seq { 0 .. (length1-1) }
    |> Seq.map (fun _ -> (randomString random length2).ToLower())
    |> Seq.toArray

let randomDateArray (random:Random) (length1:int) : DateTime[] =
    seq { 0 .. (length1-1) }
    |>  Seq.map (fun _ -> randomDate random)
    |> Seq.toArray

let testOptionArraySome<'T, 'U> (expected:'T[]) (actual:'U option) : unit =
    Assert.IsTrue(actual.IsSome)
    Assert.AreEqual(expected, actual.Value)


/// Considers 
/// - two NaN equal and 
/// - "string null" equals "string empty"
/// - DateTime is compared without milliseconds
let areEqualArraysForCsv (a1:System.Array) (a2:System.Array) =
    let t1 = a1.GetType().GetElementType()
    let t2 = a2.GetType().GetElementType()
    
    t1 = t2 &&
    a1.Length = a2.Length &&     
    match t1 with
    | t when t = typeof<double> -> Array.forall2 (fun v1 v2 -> (Double.IsNaN(v1) && Double.IsNaN(v2)) || v1 = v2) (a1 :?> double[]) (a2 :?> double[])
    | t when t = typeof<string> -> Array.forall2 (fun v1 v2 -> (String.IsNullOrEmpty(v1) && String.IsNullOrEmpty(v2)) || v1 = v2) (a1 :?> string[]) (a2 :?> string[])
    | t when t = typeof<bool> -> Array.forall2 (fun v1 v2 -> v1 = v2) (a1 :?> bool[]) (a2 :?> bool[])
    | t when t = typeof<DateTime> -> Array.forall2 (fun (v1:System.DateTime) (v2:System.DateTime) -> v1.Subtract(TimeSpan.FromMilliseconds(float(v1.Millisecond))) = v2.Subtract(TimeSpan.FromMilliseconds(float(v2.Millisecond)))) (a1 :?> DateTime[]) (a2 :?> DateTime[])
    | _ -> failwithf "Unexpected type of column: %A" t1

let areEqualColumnsForCsv (c1:Column) (c2:Column) =
    let a1 = Column.ToArray<System.Array>(c1)
    let a2 = Column.ToArray<System.Array>(c2)
    Column.Type c1 = Column.Type c2 &&
    areEqualArraysForCsv a1 a2
    

/// Considers in data arrays:
/// - two NaN equal and 
/// - "string null" equals "string empty"
/// - DateTime is compared without milliseconds
let areEqualTablesForCsv (table:Table) (table2:Table) =
    table.Names.Count = table2.Names.Count &&
    areEqualArraysForCsv (table.Names|>Seq.toArray) (table2.Names|>Seq.toArray) &&
    table.Columns.Count = table2.Columns.Count &&
    Seq.forall2 (fun c1 c2 -> areEqualColumnsForCsv c1 c2) table.Columns table2.Columns

/// Considers in data arrays:
/// - two NaN equal 
/// - DateTime is compared without milliseconds
let areEqualArraysForSerialization (a1:System.Array) (a2:System.Array) =
    let t1 = a1.GetType().GetElementType()
    let t2 = a2.GetType().GetElementType()
    
    t1 = t2 &&
    a1.Length = a2.Length &&     
    match t1 with
    | t when t = typeof<double> -> Array.forall2 (fun v1 v2 -> (Double.IsNaN(v1) && Double.IsNaN(v2)) || v1 = v2) (a1 :?> double[]) (a2 :?> double[])
    | t when t = typeof<int> -> Array.forall2 (fun v1 v2 -> v1 = v2) (a1 :?> int[]) (a2 :?> int[])
    | t when t = typeof<string> -> Array.forall2 (fun v1 v2 -> v1 = v2) (a1 :?> string[]) (a2 :?> string[])
    | t when t = typeof<bool> -> Array.forall2 (fun v1 v2 -> v1 = v2) (a1 :?> bool[]) (a2 :?> bool[])
    | t when t = typeof<DateTime> -> Array.forall2 (fun (v1:System.DateTime) (v2:System.DateTime) -> v1.Subtract(TimeSpan.FromMilliseconds(float(v1.Millisecond))) = v2.Subtract(TimeSpan.FromMilliseconds(float(v2.Millisecond)))) (a1 :?> DateTime[]) (a2 :?> DateTime[])
    | _ -> failwithf "Unexpected type of column: %A" t1
    
/// Considers in data arrays:
/// - two NaN equal 
/// - DateTime is compared without milliseconds
let areEqualColumnsForSerialization (c1:Column) (c2:Column) =
    let a1 = Column.ToArray<System.Array>(c1)
    let a2 = Column.ToArray<System.Array>(c2)
    Column.Type c1 = Column.Type c2 &&
    areEqualArraysForSerialization a1 a2
    
/// Considers in data arrays:
/// - two NaN equal 
/// - DateTime is compared without milliseconds
let areEqualTablesForSerialization (table:Table) (table2:Table) =
    table.Names.Count = table2.Names.Count &&
    areEqualArraysForSerialization (table.Names|>Seq.toArray) (table2.Names|>Seq.toArray) &&
    table.Columns.Count = table2.Columns.Count &&
    Seq.forall2 (fun c1 c2 -> areEqualColumnsForSerialization c1 c2) table.Columns table2.Columns