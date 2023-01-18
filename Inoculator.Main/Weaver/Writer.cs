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
    internal override string ProcessName => "ilasm";
    public static string TempFilePath = Path.Combine(Environment.CurrentDirectory, "part2.tmp");

    public override Result<string, Exception> Run()
    {
        ArgumentNullException.ThrowIfNull(process);
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
        var ilasmPsi = new ProcessStartInfo();
        ilasmPsi.UseShellExecute = false;
        ilasmPsi.WorkingDirectory = currentDirectory;
        ilasmPsi.CreateNoWindow = true;
        ilasmPsi.FileName = ilasmPath ?? ProcessName;
        string asmDllFileName = $"{Path.GetFileNameWithoutExtension(targetFile)}.dll";
        ilasmPsi.Arguments =
            $"/DLL /NOLOGO /QUIET /OPTIMIZE /OUTPUT={asmDllFileName} {TempFilePath}";
        ilasmPsi.RedirectStandardError = true;
        return ilasmPsi;
    }
}