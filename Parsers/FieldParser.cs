
using System;
using System.Collections.Generic;
using System.Linq;
using System.Extensions;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.ComponentModel;

namespace Inoculator.Core;

public class FieldParser {
    public static void ParseField(ref int index, ref string[] code, out Field field)
    {
        field = new Field();

        void ParseModifiers(ref int index, ref string[] code, ref Field field)
        {
            List<String> modifiers = new List<String>();
            bool NextBlock(ref string[] code, ref int index, int offset) 
                => code[index + offset] == ".custom" || code[index + offset] == ".field" 
                || code[index + offset] == ".method" || code[index + offset] == ".property" 
                || code[index + offset] == ".event"  || code[index + offset] == "}";
            
            while(index + 2 < code.Length - 1   && code[index + 2] != "="
                && !NextBlock(ref code, ref index, 2)) 
            {
                modifiers.Add(code[index++]);
            }

            field.Modifiers = modifiers.ToArray();
            field.Type = code[index++];
            field.Name = code[index++];
            if(code[index] == "=") {
                while (!NextBlock(ref code, ref index, 0)) index++;
            }
        }

        void ParseAttributes (ref int index, ref string[] code, ref Field field)
        {
            List<String> attributes = new List<String>();
            while(code[index] == ".custom") {
                var attributeResult = AttributeParser.Parse(ref index, code);
                switch(attributeResult) {
                    case Success<String, Exception> success:
                        attributes.Add(success.Value);
                        break;
                    default : break;
                }
            }
            field.Attributes = attributes.ToArray();
        }

        if(code[index++] == ".field") {
            ParseModifiers(ref index, ref code, ref field);
            ParseAttributes(ref index, ref code, ref field);
        } else field = null;

    }
    public static Result<Field, Exception> Parse(ref int i, string[] tokens)
    {
        ParseField(ref i, ref tokens, out Field field);
        if(field is null) {
            return Error<Field, Exception>.From(new Exception("Failed to parse field"));
        }
        return Success<Field, Exception>.From(field);
    }
}