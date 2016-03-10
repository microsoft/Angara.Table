module Angara.Data.TestsF.Common

open Angara.Data
open NUnit.Framework
open System
open System.Collections.Immutable

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

/// - "string null" equals "string empty"
let areEqualStringsForCsv (a1:ImmutableArray<string>) (a2:ImmutableArray<string>) =
    a1.Length = a2.Length &&     
    Seq.forall2 (fun v1 v2 -> (String.IsNullOrEmpty(v1) && String.IsNullOrEmpty(v2)) || v1 = v2) a1 a2

/// - two NaN equal
let areEqualFloatsForCsv (a1:ImmutableArray<float>) (a2:ImmutableArray<float>) =
    a1.Length = a2.Length &&     
    Seq.forall2 (fun v1 v2 -> (Double.IsNaN(v1) && Double.IsNaN(v2)) || v1 = v2) a1 a2

let areEqualBooleansForCsv (a1:ImmutableArray<bool>) (a2:ImmutableArray<bool>) =
    a1.Length = a2.Length &&     
    Seq.forall2 (fun v1 v2 -> v1 = v2) a1 a2

/// - DateTime is compared without milliseconds
let areEqualDatesForCsv (a1:ImmutableArray<System.DateTime>) (a2:ImmutableArray<System.DateTime>) =
    a1.Length = a2.Length &&     
    Seq.forall2 (fun (v1:System.DateTime) (v2:System.DateTime) -> (v1.Subtract(TimeSpan.FromMilliseconds(float(v1.Millisecond))) = v2.Subtract(TimeSpan.FromMilliseconds(float(v2.Millisecond)))) || v1 = v2) a1 a2

let areEqualColumnsForCsv (c1:Column) (c2:Column) =
    c1.Name = c2.Name &&
    match c1.Rows, c2.Rows with
    | RealColumn v1, RealColumn v2 -> areEqualFloatsForCsv v1.Value v2.Value
    | StringColumn v1, StringColumn v2 -> areEqualStringsForCsv v1.Value v2.Value
    | DateColumn v1, DateColumn v2 -> areEqualDatesForCsv v1.Value v2.Value
    | BooleanColumn v1, BooleanColumn v2 -> areEqualBooleansForCsv v1.Value v2.Value
    | t1, t2 -> failwithf "Unexpected column types: %A, %A" t1 t2

/// Considers in data arrays:
/// - two NaN equal and 
/// - "string null" equals "string empty"
/// - DateTime is compared without milliseconds
let areEqualTablesForCsv (table:Table) (table2:Table) =
    table.Count = table2.Count &&
    Seq.forall2 (fun c1 c2 -> areEqualColumnsForCsv c1 c2) table table2
    
/// Considers in data arrays:
/// - two NaN equal 
/// - DateTime is compared without milliseconds
let areEqualColumnsForSerialization (c1:Column) (c2:Column) =
    c1.Name = c2.Name &&
    match c1.Rows, c2.Rows with
    | RealColumn v1, RealColumn v2 -> areEqualFloatsForCsv v1.Value v2.Value
    | StringColumn v1, StringColumn v2 -> v1.Value.Length = v2.Value.Length && Seq.forall2 (fun v1 v2 -> v1 = v2) v1.Value v2.Value
    | IntColumn v1, IntColumn v2 -> v1.Value.Length = v2.Value.Length && Seq.forall2 (fun v1 v2 -> v1 = v2) v1.Value v2.Value
    | DateColumn v1, DateColumn v2 -> areEqualDatesForCsv v1.Value v2.Value
    | BooleanColumn v1, BooleanColumn v2 -> v1.Value.Length = v2.Value.Length && Seq.forall2 (fun v1 v2 -> v1 = v2) v1.Value v2.Value
    | t1, t2 -> failwithf "Unexpected column types: %A, %A" t1 t2
    
/// Considers in data arrays:
/// - two NaN equal 
/// - DateTime is compared without milliseconds
let areEqualTablesForSerialization (table:Table) (table2:Table) =
    table.Count = table2.Count &&
    Seq.forall2 (fun c1 c2 -> areEqualColumnsForSerialization c1 c2) table table2
