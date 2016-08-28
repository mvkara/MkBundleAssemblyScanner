module MkBundleAssemblyScanner

open Argu
open System.Reflection
open System
open System.IO

/// This worked - odd for both System.Core and System.Security
//let assExample = Assembly.ReflectionOnlyLoad("System.Security")

/// Union used to represent the arguments for the tool
type CommandLineArguments = 
    | [<Mandatory; AltCommandLine("-m")>] MainExe of string
    | [<Mandatory; AltCommandLine("-o")>] OutputFile of string
    | [<AltCommandLine("-d")>] DependentDlls of string list
    interface IArgParserTemplate with
        member x.Usage = 
            match x with
            | MainExe _ -> "The main executable entry point to use"
            | DependentDlls _ -> "A list of dependent DLLs to bundle"
            | OutputFile _ -> "The file to output the list of DLL locations to for use with MkBundle command"

let commandLineParser = ArgumentParser.Create<CommandLineArguments>()

let loadAssembly providedAssemblyMap (assemblyName: AssemblyName) = 
    printfn "Attempting to load assembly %s" assemblyName.Name
    
    // Brute force scan either provided assemblies or current assembly context (includes GAC)
    match providedAssemblyMap |> Map.tryFind assemblyName.Name with
    | Some(x) -> 
        printfn "Assembly provided in arguments, loading provided assembly %s from %s" assemblyName.Name x
        Assembly.ReflectionOnlyLoadFrom(x)
    | None -> 
        printfn "Assembly not provided in arguments, loading assembly implictly %s" assemblyName.Name
        try
            Assembly.ReflectionOnlyLoad(assemblyName.Name)
        with 
        | ex -> 
            printfn "Assembly loading directly failed, trying full name %s" assemblyName.FullName
            Assembly.ReflectionOnlyLoad(assemblyName.FullName)

let rec getAssemblyDeps loadAssembly alreadyFetchedAssemblies (assemblyName: AssemblyName) = [
    match alreadyFetchedAssemblies |> Map.tryFind (assemblyName.Name) with
    | Some(ass) -> 
        printfn "Already found assembly %s, skipping" assemblyName.Name
        yield! []
    | None -> 
        let currentAss : Assembly = loadAssembly assemblyName
        printfn "Found assembly %s" assemblyName.Name
        yield currentAss
        let newMap = alreadyFetchedAssemblies |> Map.add assemblyName.Name currentAss
        for refAss in currentAss.GetReferencedAssemblies() do
            printfn "Recursively scanning for dependendies in %s" refAss.Name
            yield! getAssemblyDeps loadAssembly newMap refAss
    ]

open System.IO
let getAllDependenciesForExecutable allAssemblyNames (executable: string) = [ 
    
    let providedAssemblyMap = 
        allAssemblyNames
        |> Seq.append [executable]
        |> Seq.map (fun x -> (Path.GetFileNameWithoutExtension(x), x))
        |> Map.ofSeq

    let loadAssembly = loadAssembly providedAssemblyMap

    let assemblyName = AssemblyName.GetAssemblyName(executable)
    let currentAssembly: Assembly = loadAssembly assemblyName

    yield! 
        getAssemblyDeps loadAssembly Map.empty assemblyName
        |> List.map (fun x -> x.Location) 
    ]   

let outputDllListToFile (outputPath: string) (dllList: string list) = 
    let stringToWrite = String.Join(" ", dllList)
    File.WriteAllText(outputPath, stringToWrite)

[<EntryPoint>]
let main argv =
    try
        printfn "MonoBundleScanner starting..."

        let parsedResults = commandLineParser.Parse(argv)
        let mainAssembly = parsedResults.GetResult <@ CommandLineArguments.MainExe @>
        let outputFile = parsedResults.GetResult <@ CommandLineArguments.OutputFile @>
        let applicationDlls = parsedResults.GetResult <@ CommandLineArguments.DependentDlls @>
        
        let assemblies = 
            getAllDependenciesForExecutable applicationDlls mainAssembly
            |> List.distinct
        
        printfn "Assemblies scanned %A" assemblies
        outputDllListToFile outputFile assemblies

        0 // return an integer exit code
    with
    | ex ->
        printfn "Exception occured %s" ex.Message
        printfn "%s" ex.StackTrace
        1