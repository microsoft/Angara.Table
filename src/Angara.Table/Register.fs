module Angara.Data.Serialization.Serializers

open Angara.Serialization

// Registers proper serializers in given libraries.
let Register(libraries: SerializerLibrary seq) =
    for lib in libraries do
        match lib.Name with
        | "reinstate" -> 
            lib.Register(TableReinstateSerializer()) 
        | "html" ->
            lib.Register(TableHtmlSerializer())
        | _ -> () // nothing to register
