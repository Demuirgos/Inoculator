using System;
using System.Collections.Generic;
using System.Linq;
using System.Extensions;
using System.Diagnostics;
using Inoculator.Extensions;
using IL_Unit = RootDecl.Declaration.Collection;
using System.IO;

namespace Inoculator.Core;

public class Writer : IntermediateIOBase<string> {
    internal string TargetFile {get; set;}
    internal override string ProcessName => "ilasm";
    public static string TempFilePath = Path.Combine(Environment.CurrentDirectory, "part2.il");

    public override Result<string, Exception> Run()
    {
        ArgumentNullException.ThrowIfNull(process);
        File.Delete(TargetFile);
        process.Start();
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            string inner_error = process.StandardError.ReadToEnd();
            return Error<string, Exception>.From(new Exception($"ilasm failed with exit code: {process.ExitCode}\n{inner_error}"));
        }
        return Success<string, Exception>.From(string.Empty);
    }

    protected override ProcessStartInfo MakeProcess(string? ilasmPath, string targetFile) {
        string currentDirectory = Environment.CurrentDirectory;
        this.TargetFile = targetFile;
        var ilasmPsi = new ProcessStartInfo
        {
            UseShellExecute = false,
            WorkingDirectory = currentDirectory,
            CreateNoWindow = true,
            FileName = ilasmPath ?? ProcessName,
            Arguments = $"/DLL /NOLOGO /QUIET /OUTPUT={targetFile} {TempFilePath}",
            RedirectStandardError = true
        };
        return ilasmPsi;
    }
}