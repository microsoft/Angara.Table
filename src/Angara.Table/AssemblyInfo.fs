namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("Angara.Table")>]
[<assembly: AssemblyProductAttribute("Angara.Table")>]
[<assembly: AssemblyDescriptionAttribute("A .NET library to work with table data type.")>]
[<assembly: AssemblyVersionAttribute("0.2.0")>]
[<assembly: AssemblyFileVersionAttribute("0.2.0")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "0.2.0"
    let [<Literal>] InformationalVersion = "0.2.0"
