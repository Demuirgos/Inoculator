using System.IO;
using System.Extensions;
using Inoculator.Core;

string fieldIl = File.ReadAllText("./Test.il");
var tokens = fieldIl.Split(
        new char[] { ' ', '\n', '\r' }, 
        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
    );

AssemblyParser.Parse(tokens)
    .Bind(assembly => {
        Console.WriteLine(assembly);
        return Success<Assembly, Exception>.From(assembly);
    });