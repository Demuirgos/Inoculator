using System;
using System.Collections.Generic;
using System.Linq;
using System.Extensions;
using System.Diagnostics;
using Inoculator.Extensions;
using System.Net;
using RootDecl;
using IL_Unit = RootDecl.Declaration.Collection;
using System.IO;

namespace Inoculator.Core;

public class Reader : IntermediateIOBase<IL_Unit> {
    readonly new static string processName = "ildasm";
    private string disasmIlFileHolder = string.Empty;
    public static Result<T, Exception> Parse<T>(string code) where T : IDeclaration<T> {
        if(Parser.TryParse(code, out T assembly)) {
            return Success<T, Exception>.From(assembly);
        } else {
            return Error<T, Exception>.From(new Exception("Failed to parse IL"));
        }
    }
    public override Result<IL_Unit, Exception> Run()
    {
        ArgumentNullException.ThrowIfNull(process);
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            return Error<IL_Unit, Exception>.From(new Exception($"ilasm failed with exit code: {process.ExitCode}"));
        }

        try {
            string disasmIl = File.ReadAllText(disasmIlFileHolder);
            return Parse<IL_Unit>(disasmIl);
        } catch (Exception exception) {
            return Error<IL_Unit, Exception>.From(exception);
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