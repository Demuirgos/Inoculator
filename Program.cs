using System;
using System.Extensions;
using System.IO;
using System.Text;
using Inoculator.Core;
using Inoculator.Builder;
using Inoculator.Extensions;
using System.Diagnostics;

var targetPath = args[0];

var reader = Reader.Create<Reader>(targetPath) switch {
    Success<Reader, Exception> success => success.Value,
    Error<Reader, Exception> failure => throw failure.Message,
};

Console.WriteLine(reader.Run());