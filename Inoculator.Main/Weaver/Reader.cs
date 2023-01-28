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
using System.Text.RegularExpressions;
using Dove.Core;

namespace Inoculator.Core;

public partial class Reader : IntermediateIOBase<IL_Unit> {
    internal string TargetFile {get; set;}
    internal override string ProcessName => "ildasm";
    public static string TempFilePath = Path.Combine(Environment.CurrentDirectory, "part1.tmp");
    public static Result<T, Exception> Parse<T>(string code) where T : IDeclaration<T> {
        code = CommentRegex().Replace(code, string.Empty);
        if(Parser.TryParse(code, out T assembly, out string error)) {
            return Success<T, Exception>.From(assembly);
        } else {
            return Error<T, Exception>.From(new Exception($"Failed to parse IL with error : \n{error}"));
        }
    }
    public override Result<IL_Unit, Exception> Run()
    {
        base.Run();
        try {
            string disasmIl = File.ReadAllText(TempFilePath);
            File.Delete(TempFilePath);
            return Parse<IL_Unit>(disasmIl);
        } catch (Exception exception) {
            return Error<IL_Unit, Exception>.From(exception);
        }
    }

    protected override ProcessStartInfo MakeProcess(string? ildasmPath, string targetFile) {
        File.Create(TempFilePath).Dispose();
        this.TargetFile = targetFile;
        var ildasmPsi = new ProcessStartInfo
        {
            UseShellExecute = false,
            FileName = ildasmPath,
            Arguments =  $"{targetFile} /OUT={TempFilePath}"
        };
        ildasmPsi.RedirectStandardError = true;
        return ildasmPsi;
    }

    [GeneratedRegex("//.*")]
    private static partial Regex CommentRegex();
}