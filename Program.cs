using System.IO;
using System.Extensions;
using Inoculator.Core;

string fieldIl = File.ReadAllText("./Test.il");
var tokens = fieldIl.Split(new char[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
int i = 0;
Result<Assembly, Exception> result = AssemblyParser.Parse(ref i, tokens);

if(result is Success<Assembly, Exception> success) {
    Console.WriteLine(success.Value);
} else {
    Console.WriteLine(result);
}