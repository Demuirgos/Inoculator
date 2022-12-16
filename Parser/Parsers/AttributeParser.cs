
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inoculator.Parser.Models;
using Attribute = Inoculator.Parser.Models.Attribute;

namespace Inoculator.Parser.Core;

public class AttributeParser {
    public static void ParseAttribute(ref int index, ref string[] code, out Attribute attribute)
    {
        attribute = new Attribute();
        if(code[index] != ".custom") {
            return;
        }
        index += 1;
        //  Note: This is a very naive implementation of this method.
        bool IsNextBlockStarting(string keywork) => keywork[0] == '.' ;

        void ParseModifiers(ref int index, ref string[] code, ref Attribute attribute)
        {
            List<string> modifiers = new List<string>();
            while(code[index] is not "void") {
                modifiers.Add(code[index]);
                index += 1;
            }
            attribute.Modifiers = modifiers.ToArray();
            index++;
        }

        void IgnoreTokens(ref int index, ref string[] code, ref Attribute _)
        {
            while(code[index] != "(") {
                index += 1;
            }
            index++;
            return;
        }

        void ParseByteArr(ref int index, ref string[] code, ref Attribute attribute)
        {
            // ignore = (
            var bytes = new List<string>();
            
            while(code[index] != ")") {
                bytes.Add(code[index]);
                index += 1;
            }
            attribute.Bytes = bytes.ToArray();
            index += 1;
        }

        void ParseNameAndArgs(ref int index, ref string[] code, ref Attribute attribute)
        {
            string name = code[index][..code[index].IndexOf("::.ctor")];
            attribute.Name = name;
            StringBuilder sb = new StringBuilder();
            while(code[index]!="=") {
                sb.Append(code[index]);
                index += 1;
            }
            attribute.Constructor = sb.ToString();
            index++;
        }

        ParseModifiers(ref index, ref code, ref attribute);
        ParseNameAndArgs(ref index, ref code, ref attribute);
        IgnoreTokens(ref index, ref code, ref attribute);
        ParseByteArr(ref index, ref code, ref attribute);
    }
    public static Result<Attribute, System.Exception> Parse(ref int i, string[] tokens)
    {
        int start = i;
        ParseAttribute(ref i, ref tokens, out Attribute attribute);
        if(attribute == null) {
            return Error<Attribute, System.Exception>.From(new System.Exception("Attribute not found"));
        }
        attribute.Code = tokens[start..i].Join(" ");
        return Success<Attribute, System.Exception>.From(attribute);
    }
}