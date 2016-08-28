# Mono MkBundle Assembly Scanner #

When using the built in MkBundle assembly scanner a number of problems can happen for any of the below advanced cases:

- App.config binding redirection means you need both the original assemblies and the binding redirect one when in reality you only need the redirected DLL.
- If there is both the same assembly in the GAC as you are using via a Nuget package Mkbundle reports a duplicate assembly name error. In these cases you prefer the Nuget version normally.

This tool implements its own custom assembly scan of your EXE file to get around the issues. For any EXE it will print to a file a list of all the assemblies in a recursive fashion
that the program uses. Note that it can't handle dynamic loading of assemblies - the references must be present at compile time. If you plan to add dynamic assemblies to your bundle
do so via the mkbundle path.

## How to use ##

    mkbundle --static --skip-scan --nodeps -o ./MkBundleScanner ./MkBundleAssemblyScanner.exe `cat output.log`

The above example:

- Lists {PathToYourExe} as the main executable to scan
- Output.log will contain the DLLs this executable uses - the tool uses this as its output path.
- The -d switch indicates a list of DLLs that may be required for scanning. Note that these will be used over any other DLLs that may be resolved by the tool (e.g one in the GAC)
-- In this case the scanner can resolve the Argu dependency so there's no need to specify it.

You will then have an output file that has a list of the current locations of the DLL's that program uses (e.g output.log) above.

An example of how to use that with the Mono mkbundle command to generate a Linux executable is as follows:

    mkbundle --skip-scan --nodeps -o ./MkBundleAssemblyScanner `cat output.log` 

This will create a binary called ./MkBundleAssemblyScanner with the DLL's embedded from output.log. The skip-scan and nodeps are required to turn off the mkbundle assembly scanner
and use the output.log file instead.