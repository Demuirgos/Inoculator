using System;
using System.IO;
using System.Text;
using Inoculator.Parser.Core;
using Inoculator.Parser.Models;

AssemblyParser
    .Parse(File.ReadAllText("./Test.il", Encoding.ASCII))
    .Bind(assembly => {
        File.WriteAllText("./Test.json", assembly.ToString());
        return Success<Assembly, Exception>.From(assembly);
    });
