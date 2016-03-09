#I "../ext/nuget/FSharp.Formatting.CommandTool.2.14.0/tools"
#r "../ext/nuget/FSharp.Formatting.CommandTool.2.14.0/tools/FSharp.CodeFormat.dll"
#r "../ext/nuget/FSharp.Formatting.CommandTool.2.14.0/tools/FSharp.Literate.dll"
#r "../ext/nuget/FSharp.Formatting.CommandTool.2.14.0/tools/FSharp.Markdown.dll"
#r "../ext/nuget/FSharp.Formatting.CommandTool.2.14.0/tools/FSharp.MetadataFormat.dll"

open FSharp.Literate
open FSharp.MetadataFormat
open System.IO

let templates = [ Path.GetFullPath "../scripts/templates"; Path.GetFullPath "../scripts/templates/reference" ]
let template_path = Path.GetFullPath "../ext/nuget/FSharp.Formatting.CommandTool.2.14.0/literate/templates/template-file.html"
let tips_path = Path.GetFullPath "../ext/nuget/FSharp.Formatting.CommandTool.2.14.0/styles/tips.js"
let style_path = Path.GetFullPath "../ext/nuget/FSharp.Formatting.CommandTool.2.14.0/styles/style.css"
let output_dir = Path.GetFullPath "../docs"
let output path = Path.Combine(output_dir, Path.GetFileName path)
let content path = Path.Combine(output_dir, (Path.Combine("content", Path.GetFileName path)))
let src path = Path.Combine(Path.GetFullPath "../src", path)
Directory.SetCurrentDirectory(__SOURCE_DIRECTORY__)

// prepare output directory
if Directory.Exists(output "") then Directory.Delete(output "", true)
Directory.CreateDirectory(output "")
Directory.CreateDirectory(content "")
for f in [tips_path; style_path] do
    printfn "%s" (content f)
    File.WriteAllText(content f, File.ReadAllText f)

// Generates HTML from FSharp script files using F# Literate Programming.
// process script files
let fsi = FsiEvaluator()
for f in 
    [ src "Angara.Table/Scripts/Angara.Table.fsx" ] 
    do
    if File.Exists f then
        printfn "Processing %s" (Path.GetFullPath(f))
        Literate.ProcessScriptFile(f, template_path, Path.ChangeExtension(output f, "html"), fsiEvaluator=fsi)
    else printfn "No such file: %s" f

// Generates F# library documentation from inline comments.
for assembly in [  src "Angara.Table/bin/Debug/Angara.Table.dll"
                   ]
    do
    if File.Exists assembly then
        printfn "Processing %s" (Path.GetFullPath(assembly))
        MetadataFormat.Generate 
            ( assembly,
              output "",
              templates,
              sourceRepo = "https://github.com/Microsoft/Angara.Table/tree/master",
              sourceFolder = Path.GetFullPath(assembly),
              markDownComments = true,
              parameters = 
                [ "project-name", "Angara.Table"
                ; "project-author", "Microsoft Research" 
                ; "root", "." 
                ; "project-nuget", "https://www.nuget.org/packages?q=Angara"
                ; "project-github", "https://github.com/Microsoft/Angara.Table"])
    else printfn "No such file: %s" assembly  
