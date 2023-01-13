using System;
using System.Collections.Generic;
using System.Linq;
using System.Extensions;
using System.Diagnostics;
using IL_Unit = RootDecl.Declaration.Collection;

namespace Inoculator.Core;

public class Weaver {
    public static Result<string, Exception> Assemble(string code, string path)
        => Writer.Create<Writer>(path) switch {
            Success<Writer, Exception> writer 
                => writer.Value.Run(),
            Error<Writer, Exception> error 
                => Error<string, Exception>.From(error.Message)
        }; 

    public static Result<IL_Unit, Exception> Modify(IL_Unit code) {

        return new Success<IL_Unit, Exception>(code);
    }

    public static Result<IL_Unit, Exception> Disassemble(string path)
        => Reader.Create<Reader>(path) switch {
            Success<Reader, Exception> reader =>
                reader.Value.Run() switch {
                    Success<IL_Unit, Exception> holder
                        => Modify(holder.Value),
                    Error<IL_Unit, Exception> error 
                        => Error<IL_Unit, Exception>.From(error.Message),
                    _ => throw new Exception("Unreachable code")
                },
            Error<Reader, Exception> error => Error<IL_Unit, Exception>.From(error.Message)
        };
}