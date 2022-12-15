using System;
using System.Collections.Generic;
using System.Linq;
using System.Extensions;
using System.Diagnostics;
using Inoculator.Extensions;

namespace Inoculator.Core;

public class Writer : IntermediateIOBase {
    readonly new static string processName = "ilasm";
    public override Result<string, Exception> Run()
    {
        ArgumentNullException.ThrowIfNull(process);
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            return Error<string, Exception>.From(new Exception($"ilasm failed with exit code: {process.ExitCode}"));
        }
        return Success<string, Exception>.From(string.Empty);
    }

    protected override ProcessStartInfo MakeProcess(string ilasmPath, string targetFile) {
        string currentDirectory = Environment.CurrentDirectory;
        var ilasmPsi = new ProcessStartInfo();
        ilasmPsi.UseShellExecute = false;
        ilasmPsi.WorkingDirectory = currentDirectory;
        ilasmPsi.CreateNoWindow = true;
        ilasmPsi.FileName = ilasmPath;
        string asmDllFileName = $"{Path.GetFileNameWithoutExtension(targetFile)}.dll";
        ilasmPsi.Arguments =
            $"-nologo -dll -optimize -output={asmDllFileName} {targetFile}";
        return ilasmPsi;
    }
}