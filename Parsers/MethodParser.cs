
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
            
            Console.Write($"Modifiers : ");
            foreach (var modifier in modifiers)
            {
                Console.Write($"{modifier}, ");
            }
            Console.WriteLine();

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
                while(code[index] != ")") {
                    
                    ArgumentTokens.Add(new Argument {
                        Type = code[index++],
                        Name = code[index++]
                    });
                    if(code[index] == ",") {
                        index += 1;
                    }
                }
            }
            Console.Write($"Signature : ");
            foreach (var param in ArgumentTokens)
            {
                Console.Write($"({param.Type} ");
            }
            Console.WriteLine(") => {0} as {1}", method.ReturnValue.Type, method.Name);

            method.Parameters = ArgumentTokens.ToArray();
            index += 1;
        }

        void IgnoreTokens(ref int index, ref string[] code, ref Method _)
        {
            while(code[index] != "{") {
                index += 1;
            }
            index++;
        }
        void ParseAttributes (ref int index, ref string[] code, ref Method method)
        {
            List<String> attributes = new List<String>();
            while(code[index] != ".maxstack") {
                if(code[index].EndsWith("Attribute::.ctor")) {
                    attributes.Add(code[index++].Replace("::.ctor", String.Empty));
                } else {
                    index += 1;
                }
            }
            method.Attributes = attributes.ToArray();
        }

        void ParseStackSize (ref int index, ref string[] code, ref Method method)
        {
            if(code[index++] == ".maxstack") {
                method.MaxStack = int.Parse(code[index++]);
            }
        }

        void  ParseLocalInits (ref int index, ref string[] code, ref Method method)
        {
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
        }

        if(code[index++] == ".method") {
            ParseModifiers(ref index, ref code, ref method);
            ParseSignature(ref index, ref code, ref method);
            IgnoreTokens(ref index, ref code, ref method);
            ParseAttributes(ref index, ref code, ref method);
            ParseStackSize(ref index, ref code, ref method);
            ParseLocalInits(ref index, ref code, ref method);
            ParseBody(ref index, ref code, ref method);
        }
    }
    public static Result<Method, Exception> Parse(string code)
    {
        string[] tokens = code.Split(new char[] {'\n', ' '}, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        // print tokens
        foreach (var token in tokens)
        {
            Console.WriteLine($"{token}, ");
        }
        int index = 0;
        ParseMethod(ref index, ref tokens, out Method method);
        return Success<Method, Exception>.From(method);
    }
}
/*
.method public hidebysig static 
        object test (
            string code,
            string path
        ) cil managed 
    {
        .custom instance void System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = (
            01 00 01 00 00
        )
        .custom instance void Inoculator.Core.InterceptorAttribute::.ctor() = (
            01 00 00 00
        )
        .param [0]
            .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = (
                01 00 02 00 00
            )
        // Method begins at RVA 0x20fc
        // Code size 11 (0xb)
        .maxstack 1
        .locals init (
            [0] object
        )

        IL_0000: nop
        IL_0001: newobj instance void [System.Runtime]System.Object::.ctor()
        IL_0006: stloc.0
        IL_0007: br.s IL_0009

        IL_0009: ldloc.0
        IL_000a: ret
    }*/
