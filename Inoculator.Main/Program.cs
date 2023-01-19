#define TEST
using System;
using System.Extensions;
using System.IO;
using System.Text;
using Inoculator.Core;
using Inoculator.Builder;
using Inoculator.Extensions;
using System.Diagnostics;
var EnvirementCurrentTargetDll = args[0];

var reader = Reader.Create<Reader>(EnvirementCurrentTargetDll) switch {
    Success<Reader, Exception> success => success.Value,
    Error<Reader, Exception> failure => throw failure.Message,
};

var writer = Writer.Create<Writer>(EnvirementCurrentTargetDll) switch {
    Success<Writer, Exception> success => success.Value,
    Error<Writer, Exception> failure => throw failure.Message,
};
var result = reader.Run().Bind(assembly => {
    var result = Weaver.Modify(assembly);
    return result.Bind(Ilcode => {
        string result = Ilcode.ToString();
        File.WriteAllText(Writer.TempFilePath, result);
        return writer.Run();
    });
}).PanicIfErr();