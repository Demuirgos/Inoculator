using System;
using System.Collections.Generic;
using System.Linq;
using System.Extensions;
using System.Diagnostics;
using System.Reflection;

namespace Inoculator.Core;

public class Parser {
    
    public static Result<Assembly, Exception> Parse(string code, string path)
    {
        return Error<Assembly, Exception>.From(new Exception());
    }
}