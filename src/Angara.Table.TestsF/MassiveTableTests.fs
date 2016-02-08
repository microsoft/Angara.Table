module MassiveTableTests

open FsCheck
open Angara.Data

module internal Helpers = 
    let colGen<'a> = gen {
        let! arr = Gen.sized(fun size -> Gen.arrayOfLength size Arb.generate<'a>)
        return Column.New arr
    }

    let colShrink<'a> col : Column seq = Arb.toShrink (Arb.Default.Array<'a>()) (col |> Column.ToArray) |> Seq.map Column.New

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
                    return! Gen.oneof [Helpers.colGen<int>; Helpers.colGen<float>; Helpers.colGen<string>; Helpers.colGen<System.DateTime>; Helpers.colGen<bool>]
                } 
            override x.Shrinker (col:Column) =
                match col |> Column.Type with
                | t when t = typeof<int> -> Helpers.colShrink<int> col
                | t when t = typeof<float> -> Helpers.colShrink<float> col
                | t when t = typeof<string> -> Helpers.colShrink<string> col
                | t when t = typeof<System.DateTime> -> Helpers.colShrink<System.DateTime> col
                | t when t = typeof<bool> -> Helpers.colShrink<bool> col
                | t -> failwithf "Unexpected column type %s" t.Name
                    
        }

    static member Table() =
        { new Arbitrary<Table>() with
            override x.Generator = 
                gen {
                    let namedColGen = gen {
                        let! name = Arb.generate<string>
                        let! col = Arb.generate<Column>
                        return name, col
                    }
                    let! cols = Gen.sized(fun size -> gen{
                        let! colN = Gen.choose(0, 1 + int(sqrt(float(size))))
                        return! Gen.listOfLength colN namedColGen
                    })
                    return new Table (cols)
                }

            override x.Shrinker (table:Table) =
                let shrinkNamedCol name col = 
                    let names = Arb.toShrink (Arb.Default.String()) name
                    let cols = Arb.toShrink (Generators.Column()) col
                    names |> Seq.map(fun n -> cols |> Seq.map(fun c -> Table.New n c)) |> Seq.concat

                let namedCols = table.Columns |> Seq.map(fun c -> Table.Name c table, c) |> Seq.toArray
                if namedCols.Length > 1 then
                    let t = table.Columns |> Seq.map(fun c -> new Table(namedCols |> Seq.filter(fun (_,c2) -> c2 <> c))) |> Seq.toArray
                    upcast t
                else if namedCols.Length = 1 then 
                    let name, col = namedCols.[0]
                    shrinkNamedCol name col
                else Seq.empty
                    
        }

[<NUnit.Framework.SetUpFixtureAttribute>]
type MassiveTestsSetup() =
    [<NUnit.Framework.SetUpAttribute>]
    member x.Initialize() =    
        Arb.register<Generators>() |> ignore   

