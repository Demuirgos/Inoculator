
using System;
using System.Collections.Generic;
using System.Linq;
using System.Extensions;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.ComponentModel;
using System.ComponentModel.Design;

namespace Inoculator.Core;

public class AttributeParser {
    public static void ParseAttribute(ref int index, ref string[] code, out string? attribute)
    {
        attribute = null;
        if(code[index] != ".custom") {
            return;
        }
        index += 1;
        //  Note: This is a very naive implementation of this method.
        bool IsNextBlockStarting(string keywork) => keywork[0] == '.' ;

        while(index < code.Length - 1 && !IsNextBlockStarting(code[index])) {
            if(code[index].Contains("::.ctor")) {
                var token = code[index++]; 
                attribute = token.Substring(0, token.IndexOf("::.ctor"));
            } else {
                index += 1;
            }

            if(code[index] == ".custom") {
                break;                
            }
        }
    }
    public static Result<String, Exception> Parse(ref int i, string[] tokens)
    {
        ParseAttribute(ref i, ref tokens, out string? attribute);
        if(attribute == null) {
            return Error<String, Exception>.From(new Exception("Attribute not found"));
        }
        return Success<String, Exception>.From(attribute);
    }
}