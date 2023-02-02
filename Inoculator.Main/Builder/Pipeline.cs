using System.Extensions;
using Inoculator.Core;
using Microsoft.VisualBasic;

public class Pipeline
{
    private Reader reader;
    private Writer writer;
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
    }

    public Result<string, Exception> Run()
    {
        return reader.Run().Bind(assembly => {
            var result = Weaver.Modify(TargetPath, assembly);
            return result switch {
                Success<RootDecl.Declaration.Collection, Exception> success => Continuation(success.Value),
                Error<RootDecl.Declaration.Collection, Exception> failure => Error<string, Exception>.From(failure.Message) as Result<string, Exception>,
            };
        });
    }

    private Result<string, Exception> Continuation(RootDecl.Declaration.Collection Ilcode)
    {
        string result = Ilcode.ToString();
        File.WriteAllText(Writer.TempFilePath, result);
        return writer.Run();
    }
}