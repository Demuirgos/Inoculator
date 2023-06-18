using System;
using System.Collections.Generic;
using System.Linq;
using System.Extensions;
using System.Diagnostics;
using Inoculator.Extensions;
using IL_Unit = RootDecl.Declaration.Collection;
using System.IO;
using System.Reflection;
using System.Runtime.Versioning;

namespace Inoculator.Core;
//    <Exec WorkingDirectory="$(MSbuildProjectDirectory)\$(BaseOutputPath)$(Configuration)\$(TargetFramework)" Command='ilverify  >
public class Verifier : IntermediateIOBase<string> {
    internal string TargetFile {get; set;}
    internal override string ProcessName => "ilverify";
    public override Result<string, Exception> Run()
    {
        ArgumentNullException.ThrowIfNull(process);
        InstallTool();
        process.Start();
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            string inner_error = process.StandardError.ReadToEnd();
            return Error<string, Exception>.From(new Exception($"ilverifyPsi failed with exit code: {process.ExitCode}\n{inner_error}"));
        }
        return Success<string, Exception>.From(string.Empty);
    }
    
    protected void InstallTool() {
        var psi = new ProcessStartInfo
        {
            UseShellExecute = false,
            WorkingDirectory = Environment.CurrentDirectory,
            CreateNoWindow = true,
            FileName = "dotnet",
            Arguments = $"tool install --tool-path {IntermediateIOBase<Verifier>.DefaultPath}  dotnet-ilverify",
            RedirectStandardError = true
        };
        var process = new Process();
        process.StartInfo = psi;
        process.Start();
        process.WaitForExit();

    }

    protected override ProcessStartInfo MakeProcess(string? ilverifyPath, string targetFile) {
        string currentDirectory = Environment.CurrentDirectory;
        var version = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
        var files = Directory.GetFiles(currentDirectory, "*.dll")
            .Where(x => !x.EndsWith(targetFile))
            .Select(x => $"\"{x}\"")
            .Aggregate((x, y) => $"{x} {y}");
            
        this.TargetFile = targetFile;

        // get dotnet.exe path
        var dotnetPath = () => {
            var psi = new ProcessStartInfo
            {
                UseShellExecute = false,
                WorkingDirectory = currentDirectory,
                CreateNoWindow = true,
                FileName = "dotnet",
                Arguments = "--list-runtimes",
                RedirectStandardOutput = true
            };

            // get dotnet verion being used
            string? version = 
                Assembly.GetEntryAssembly()
                ?.GetCustomAttribute<TargetFrameworkAttribute>()
                ?.FrameworkDisplayName
                ?.Split(' ')
                .Last();

            var process = new Process();
            process.StartInfo = psi;
            process.Start();
            process.WaitForExit();
            var path =  
                String.Join(' ', process.StandardOutput
                        .ReadToEnd().Split('\n')
                        .First(x => x.StartsWith("Microsoft.NETCore.App"))
                        .Split(' ')[2..])[1..^2];

            var subfolder = Directory.GetDirectories(path)
                .Where(x => x.Contains(version))
                .Last().Trim();
            
            return subfolder;
        };

        var ilverifyPsi = new ProcessStartInfo
        {
            UseShellExecute = false,
            WorkingDirectory = currentDirectory,
            CreateNoWindow = true,
            FileName = ilverifyPath ?? ProcessName,
            Arguments = $"{targetFile} -r \"{dotnetPath()}\\*.dll\" {files}",
            RedirectStandardError = true
        };
        return ilverifyPsi;
    }
}