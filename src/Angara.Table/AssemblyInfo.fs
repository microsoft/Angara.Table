namespace Angara.AssemblyInfo

open System.Reflection
open System.Runtime.CompilerServices
open System.Runtime.InteropServices

module internal Const =
    [<Literal>]
    let Version = "0.1.0"

[<assembly: AssemblyTitle("Angara.Table")>]
[<assembly: AssemblyDescription("A library that contains types for table representation and operations to manipulate with tables as well as save and load them from text files such as CSV files.")>]
[<assembly: AssemblyConfiguration("")>]
[<assembly: AssemblyCompany("Microsoft Research")>]
[<assembly: AssemblyProduct("Angara")>]
[<assembly: AssemblyCopyright("Copyright © 2016 Microsoft Research")>]
[<assembly: AssemblyTrademark("")>]
[<assembly: AssemblyCulture("")>]

[<assembly: ComVisible(false)>]

[<assembly: Guid("c45fa56d-4094-4d91-bcd6-bbdea8b7f198")>]

[<assembly: AssemblyVersion(Const.Version + ".0")>]
[<assembly: AssemblyFileVersion(Const.Version + ".0")>]

[<assembly: InternalsVisibleTo("Angara.Table.TestsF")>]
[<assembly: InternalsVisibleTo("Angara.Table.TestsC")>]

do
    ()
