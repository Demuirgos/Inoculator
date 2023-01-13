using System;
using System.Extensions;
using System.IO;
using System.Text;
using Inoculator.Core;

Reader
    .Parse<RootDecl.Declaration.Collection>(File.ReadAllText("./Test.il", Encoding.ASCII))
    .Bind(assembly => {
        File.WriteAllText("./Test.txt", assembly.ToString());
        return Success<RootDecl.Declaration.Collection, Exception>.From(assembly);
    });
