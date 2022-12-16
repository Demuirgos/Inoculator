using System.Linq;
using Inoculator.Parser.Models;

namespace Inoculator.Parser.Core;

public class ArgumentParser {
    public static void ParseArgument(ref int index, ref string[] code, out Argument argument)
    {
        argument = new Argument();
        if(code[index] == "()")
        {
            argument.Type = "void";
            index++;
        } else {
            if(code[index] == "valuetype" || code[index] == "class" ) {
                argument.Indicator = code[index];
                index++;
            } 
            argument.Type = code[index++];
            argument.Name = code[index++].Replace(",", string.Empty);
        }
    }
    public static Result<Argument, System.Exception> Parse(ref int i, string[] tokens)
    {
        int start = i;
        ParseArgument(ref i, ref tokens, out Argument argument);
        if(argument == null) {
            return Error<Argument, System.Exception>.From(new System.Exception("Argument not found"));
        }
        argument.Code = tokens[start..i].Join(" ");
        return Success<Argument, System.Exception>.From(argument);
    }
}