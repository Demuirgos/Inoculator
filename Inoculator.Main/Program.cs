#define TEST
using System;
using System.Extensions;
using System.IO;
using System.Text;
using Inoculator.Core;
using Inoculator.Builder;
using Inoculator.Extensions;
using System.Diagnostics;

var targetPath 
#if TEST
    = @"D:\Projects\Innoculator\Inoculated.Test\bin\Debug\net7.0\Inoculated.Test.dll";
#else
    = args[0];
#endif

var reader = Reader.Create<Reader>(targetPath) switch {
    Success<Reader, Exception> success => success.Value,
    Error<Reader, Exception> failure => throw failure.Message,
};

reader.Run().Bind(assembly => {
    var result = Weaver.Modify(assembly);
    if(result is Success<RootDecl.Declaration.Collection, Exception> success) {
        File.WriteAllText(@".\Result.il", success.Value.ToString());
    } else if(result is Error<RootDecl.Declaration.Collection, Exception> failure) {
        return Error<int, Exception>.From(failure.Message) as Result<int, Exception>;
    }
    return Success<int, Exception>.From(0) as Result<int, Exception>;
});