using System;
using System.Collections.Generic;
using System.Linq;
using System.Extensions;
using System.Diagnostics;
using System.IO;

namespace Inoculator.Extensions;

public abstract class IntermediateIOBase<TOutput> {
    readonly static string coreRoot = Environment.GetEnvironmentVariable("CORE_ROOT");
    readonly static string processName = string.Empty;
    protected Process process = null; 

    public abstract Result<TOutput, Exception> Run();

    public static Result<T, Exception> Create<T>(string ilFilePath) 
        where T : IntermediateIOBase<TOutput>
    {
        var reader = (T)Activator.CreateInstance(typeof(T));

        var PathResult = RetrievePath();
        switch (PathResult)
        {
            case Success<string, ArgumentException> path :
                reader.process = new Process();
                reader.process.StartInfo = reader.MakeProcess(path.Value, ilFilePath);
                return Success<T, Exception>.From(reader);
            case Error<string, ArgumentException> exception : 
                return Error<T, Exception>.From(exception.Message);
            default : throw new Exception("Unreachable code");
        }
    }

    protected abstract ProcessStartInfo MakeProcess(string ilasmPath, string targetFile);

    private static Result<string, ArgumentException> RetrievePath() {
        var ProcessPath = String.Empty;
        if (string.IsNullOrWhiteSpace(coreRoot))
        {
            return Error<string, ArgumentException>.From(new ArgumentException("Environment variable is not set: 'CORE_ROOT'"));
        }
        if (!Directory.Exists(coreRoot))
        {
            return Error<string, ArgumentException>.From(new ArgumentException($"Did not find CORE_ROOT directory: {coreRoot}"));
        }

        var nativeExeExtensions = new string[] { string.Empty, ".exe" };
        foreach (string nativeExeExtension in nativeExeExtensions)
        {
            ProcessPath = Path.Combine(coreRoot, $"{processName}{nativeExeExtension}");
            if (File.Exists(ProcessPath))
            {
                break;
            }
        }

        if (ProcessPath is null)
        {
            return Error<string, ArgumentException>.From(new ArgumentException($"Did not find ilasm or ildasm in CORE_ROOT directory: {coreRoot}"));
        }
        return Success<string, ArgumentException>.From(ProcessPath);
    }

}