using System;
using System.Collections.Generic;
using System.Linq;
using System.Extensions;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Inoculator.Extensions;

public abstract class IntermediateIOBase<TOutput> {
    readonly public static string DefaultPath = 
          RuntimeInformation.IsOSPlatform(OSPlatform.Windows)   ? "runtimes\\win-x64\\native\\" 
        : RuntimeInformation.IsOSPlatform(OSPlatform.Linux)     ? "runtimes\\linux-x64\\native\\" 
        : RuntimeInformation.IsOSPlatform(OSPlatform.OSX)       ? "runtimes\\osx-arm64\\native\\"
        : throw new PlatformNotSupportedException();

    readonly static string coreRoot = Environment.GetEnvironmentVariable("CORE_ROOT") ?? DefaultPath;
    internal virtual string ProcessName {get; set;} = string.Empty;
    protected Process process = null; 
    public virtual Result<TOutput, Exception> Run() {
        ArgumentNullException.ThrowIfNull(process);
        process.Start();
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            string inner_error = process.StandardError.ReadToEnd();
            return Error<TOutput, Exception>.From(new Exception($"ildasm failed with exit code: {process.ExitCode}\n{inner_error}"));
        }
        process.Dispose();
        return Success<TOutput, Exception>.From(default);
    }

    public static Result<T, Exception> Create<T>(string ilFilePath) 
        where T : IntermediateIOBase<TOutput>
    {
        var reader = (T)Activator.CreateInstance(typeof(T));

        var PathResult = RetrievePath(reader.ProcessName);
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

    private static Result<string, ArgumentException> RetrievePath(string processName) {
        //  get path of current process
        var current = Process.GetCurrentProcess();
        var currentPath = current.MainModule.FileName;
        var currentDirectory = Path.GetDirectoryName(currentPath);

        var ProcessPath = String.Empty;
        if (coreRoot != DefaultPath && string.IsNullOrWhiteSpace(coreRoot))
        {
            return Error<string, ArgumentException>.From(new ArgumentException("Environment variable is not set: 'CORE_ROOT'"));
        }

        var fileLocation = Path.Combine(currentDirectory, coreRoot);
        if (!Directory.Exists(fileLocation))
        {
            return Error<string, ArgumentException>.From(new ArgumentException($"Did not find CORE_ROOT directory: {fileLocation}"));
        }

        var nativeExeExtensions = new string[] { string.Empty, ".exe" };
        foreach (string nativeExeExtension in nativeExeExtensions)
        {
            ProcessPath = Path.Combine(fileLocation, $"{processName}{nativeExeExtension}");
            if (File.Exists(ProcessPath))
            {
                break;
            }
        }

        if (ProcessPath is null)
        {
            return Error<string, ArgumentException>.From(new ArgumentException($"Did not find ilasm or ildasm in CORE_ROOT directory: {fileLocation}"));
        }
        return Success<string, ArgumentException>.From(ProcessPath);
    }

}