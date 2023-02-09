using System.Extensions;
using Inoculator.Core;
using Microsoft.VisualBasic;

public class Pipeline
{
    private Reader reader;
    private Writer writer;
    private Verifier verifier;
    string TargetPath;
    public Pipeline(string targetPath)
    {
        TargetPath = targetPath;
        reader = Reader.Create<Reader>(targetPath) switch {
            Success<Reader, Exception> success => success.Value,
            Error<Reader, Exception> failure => throw failure.Message,
        };

        writer = Writer.Create<Writer>(targetPath) switch {
            Success<Writer, Exception> success => success.Value,
            Error<Writer, Exception> failure => throw failure.Message,
        };

        verifier = Verifier.Create<Verifier>(targetPath) switch {
            Success<Verifier, Exception> success => success.Value,
            Error<Verifier, Exception> failure => throw failure.Message,
        };
    }

    public Result<string, Exception> Run()
    {
        return reader.Run().Bind(assembly => {
            var result = Weaver.Modify(TargetPath, assembly);
            return result.Bind(Ilcode => {
                string result = Ilcode.ToString();
                File.WriteAllText(Writer.TempFilePath, result);
                return writer.Run();//.Bind(_ => verifier.Run());
            });
        });
    }
}