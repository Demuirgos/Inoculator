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
var Pipeline = new Pipeline(EnvirementCurrentTargetDll);
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