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

var Pipeline = new Pipeline(targetPath);
var result = Pipeline.Run();
if (result is Error<string, Exception> error)
{
    Console.WriteLine(error.Message);
    Environment.Exit(1);
}
else
{
    Console.WriteLine("Success!");
    Environment.Exit(0);
}