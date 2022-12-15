using System;
using System.Collections.Generic;
using System.Linq;
using System.Extensions;
using System.Diagnostics;

namespace Inoculator.Core;

public class Weaver {
    public static Result<string, Exception> Assemble(string code, string path)
        => Writer.Create<Writer>(path) switch {
            Success<Writer, Exception> writer 
                => writer.Value.Run(),
            Error<Writer, Exception> error 
                => Error<string, Exception>.From(error.Message)
        }; 

    public static Result<string, Exception> Modify(string code) {

        return new Success<string, Exception>(String.Empty);
    }

    public static Result<string, Exception> Disassemble(string path)
        => Reader.Create<Reader>(path) switch {
            Success<Reader, Exception> reader =>
                reader.Value.Run() switch {
                    Success<string, Exception> holder
                        => Modify(holder.Value),
                    Error<string, Exception> error 
                        => Error<string, Exception>.From(error.Message),
                    _ => throw new Exception("Unreachable code")
                },
            Error<Reader, Exception> error => Error<string, Exception>.From(error.Message)
        };
}