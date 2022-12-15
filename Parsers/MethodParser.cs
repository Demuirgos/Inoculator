
using System;
using System.Collections.Generic;
using System.Linq;
using System.Extensions;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.ComponentModel;

namespace Inoculator.Core;

public class MethodParser {
    public static void ParseMethod(ref int index, ref string[] code, out Method method)
    {
        method = new Method();
        void ParseModifiers(ref int index, ref string[] code, ref Method method)
        {
            List<String> modifiers = new List<String>();
            while(code[index] is not "static" and not "instance") {
                modifiers.Add(code[index]);
                index += 1;
            }
            modifiers.Add(code[index++]);
            
            method.Modifiers = modifiers.ToArray();
        }
        
        void ParseSignature(ref int index, ref string[] code, ref Method method)
        {
            method.ReturnValue = new Argument {
                Type = code[index++]
            };
            method.Name = code[index++];
            var ArgumentTokens = new List<Argument>();
            if(code[index] == "()")
            {
                ArgumentTokens.Add(new Argument {
                    Type = "void",
                });
                index++;
            } else {
                index++;
                while(code[index] != ")") {
                    ArgumentTokens.Add(new Argument {
                        Type = code[index++],
                        Name = code[index++].Replace(",", String.Empty)
                    });
                }
            }

            method.Parameters = ArgumentTokens.ToArray();
            index += 1;
        }

        void IgnoreTokens(ref int index, ref string[] code, ref Method _)
        {
            while(code[index] != "{") {
                index += 1;
            }
            index++;
            return;
        }

        void ParseAttributes (ref int index, ref string[] code, ref Method method)
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
            method.Attributes = attributes.ToArray();
        }

        void ParseStackSize (ref int index, ref string[] code, ref Method method)
        {
            if(code[index] != ".maxstack") {
                return;
            }

            method.MaxStack = int.Parse(code[++index]);
        }

        void  ParseLocalInits (ref int index, ref string[] code, ref Method method)
        {
            if(code[index] != ".locals") {
                return;
            }

            List<Argument> locals = new List<Argument>();
            while(code[index] is ".locals" or "init" or "(") {
                index += 1;
            }
            while(true) {
                index++; 
                switch(code[index]) {
                    case "class" : 
                    case "valuetype" :
                    {
                        bool isNameless = code[index+1].EndsWith(",");
                        bool isLast = code[index+2] is ")"; 
                        locals.Add(new Argument {
                            Type = isNameless && !isLast ? code[index+1][..^1] : code[index+1],
                            Name = isLast || isLast ? String.Empty : code[index+2]
                        });
                        index += 2;
                        break;
                    }
                    default : {
                        bool isNameless = code[index].EndsWith(",");
                        bool isLast = code[index+1] is ")"; 
                        locals.Add(new Argument {
                            Type = isNameless && !isLast ? code[index][..^1] : code[index],
                            Name = isLast || isLast ? String.Empty : code[index + 1]
                        });
                        index += 1;
                        break;
                    }
                }
                
                if(code[index] == ")") {
                    index += 1;
                    break;
                }
                index += 1;
            }
            method.LocalsInits = locals.ToArray();
        }

        void ParseBody (ref int index, ref string[] code, ref Method method)
        {
            List<String> body = new List<String>();
            while(code[index] != "}") {
                body.Add(code[index++]);
            }
            method.Body = body.ToArray();
            index++;
        }

        if(code[index++] == ".method") {
            ParseModifiers(ref index, ref code, ref method);
            ParseSignature(ref index, ref code, ref method);
            IgnoreTokens(ref index, ref code, ref method);
            ParseAttributes(ref index, ref code, ref method);
            ParseStackSize(ref index, ref code, ref method);
            ParseLocalInits(ref index, ref code, ref method);
            ParseBody(ref index, ref code, ref method);
        } else method = null;
    }
    public static Result<Method, Exception> Parse(ref int i, string[] tokens)
    {
        ParseMethod(ref i, ref tokens, out Method method);
        if(method is null) {
            return Error<Method, Exception>.From(new Exception("Method is null"));
        }
        return Success<Method, Exception>.From(method);
    }
}