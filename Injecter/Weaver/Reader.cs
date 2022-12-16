using System;
using System.Collections.Generic;
using System.Linq;
using System.Extensions;
using System.Diagnostics;
using Inoculator.Extensions;
using System.Net;

namespace Inoculator.Core;

public class Reader : IntermediateIOBase {
    readonly new static string processName = "ildasm";
    private string disasmIlFileHolder = string.Empty;
    public override Result<string, Exception> Run()
    {
        ArgumentNullException.ThrowIfNull(process);
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            return Error<string, Exception>.From(new Exception($"ilasm failed with exit code: {process.ExitCode}"));
        }

        try {
            string disasmIl = File.ReadAllText(disasmIlFileHolder);
            return Success<string, Exception>.From(disasmIl);
        } catch (Exception exception) {
            return Error<string, Exception>.From(exception);
        }
    }

    protected override ProcessStartInfo MakeProcess(string ildasmPath, string targetFile) {
        string currentDirectory = Environment.CurrentDirectory;
        var ildasmPsi = new ProcessStartInfo();
        ildasmPsi.UseShellExecute = false;
        ildasmPsi.WorkingDirectory = currentDirectory;
        ildasmPsi.CreateNoWindow = true;
        ildasmPsi.FileName = ildasmPath;
        var asmDllFileName = $"{Path.GetFileNameWithoutExtension(targetFile)}.dll";
        disasmIlFileHolder = $"{Path.GetFileNameWithoutExtension(targetFile)}_dis{Path.GetExtension(targetFile)}";
        ildasmPsi.Arguments = $"-out={disasmIlFileHolder} {asmDllFileName}";
        return ildasmPsi;
    }
}