module MassiveTableTests

open FsCheck
open Angara.Data
open System.Collections.Immutable

module internal Helpers = 
    let colGen<'a> (name:string) = gen {
        let! arr = Gen.sized(fun size -> Gen.arrayOfLength size Arb.generate<'a>)
        return Column.OfLazyArray(name, lazy(ImmutableArray.Create<'a>(arr)), arr.Length)
    }

    let colShrink<'a> (name:string) (values: ImmutableArray<'a>) : Column seq = 
        let arr = Array.zeroCreate values.Length 
        values.CopyTo(arr)
        Arb.toShrink (Arb.Default.Array<'a>()) (arr) |> Seq.map (fun arr2 -> Column.OfArray (name, arr2))

type Generators =
    /// Same as default string generator, but uses Environment.NewLine without individual \r, \n.
    static member String() =
        let generator = gen {
            let! s = Arb.Default.String() |> Arb.toGen
            return if s = null then null else s.Replace('\r','\n').Replace("\n", System.Environment.NewLine)
        }
        let shrinker (s:string) : string seq =
            match s with
            | null -> Seq.empty
            | _ ->  
                Arb.Default.String().Shrinker (s.Replace(System.Environment.NewLine, "\n"))
                |> Seq.map(fun s2 -> s2.Replace("\n", System.Environment.NewLine))
        Arb.fromGenShrink (generator, shrinker) 


    static member Column() =
        { new Arbitrary<Column>() with
            override x.Generator =                 
                gen {
                    let! name = (Arb.from<NonNull<string>> |> Arb.convert (fun ns -> ns.Get) NonNull).Generator
                    return! Gen.oneof [Helpers.colGen<int> name; Helpers.colGen<float> name; Helpers.colGen<string> name; Helpers.colGen<System.DateTime> name; Helpers.colGen<bool> name]
                } 
            override x.Shrinker (col:Column) =
                match col.Rows with
                | RealColumn v -> Helpers.colShrink col.Name v.Value
                | IntColumn v -> Helpers.colShrink col.Name v.Value
                | StringColumn v -> Helpers.colShrink col.Name v.Value
                | DateColumn v -> Helpers.colShrink col.Name v.Value
                | BooleanColumn v -> Helpers.colShrink col.Name v.Value
                    
        }

    static member Table() =
        { new Arbitrary<Table>() with
            override x.Generator = 
                gen {
                    let! cols = Gen.sized(fun size -> gen{
                        let! colN = Gen.choose(0, 1 + int(sqrt(float(size))))
                        return! Gen.listOfLength colN Arb.generate<Column>
                    })
                    return new Table (cols)
                }

            override x.Shrinker (table:Table) =
                let shrinkNamedCol (col:Column) = 
                    let names = Arb.toShrink (Arb.Default.String()) col.Name
                    let cols = Arb.toShrink (Generators.Column()) col
                    names |> Seq.map(fun n -> cols |> Seq.map(fun c -> Table([Column.OfColumnValues(n, c.Rows, c.Height)]))) |> Seq.concat

                if table.Count > 1 then
                    let t = table |> Seq.map(fun c -> new Table(table |> Seq.filter(fun c2 -> c2 <> c))) |> Seq.toArray
                    upcast t
                else if table.Count = 1 then 
                    shrinkNamedCol table.[0]
                else Seq.empty
                    
        }

[<NUnit.Framework.SetUpFixtureAttribute>]
type MassiveTestsSetup() =
    [<NUnit.Framework.SetUpAttribute>]
    member x.Initialize() =    
        Arb.register<Generators>() |> ignore   

