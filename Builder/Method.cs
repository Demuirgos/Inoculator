using System.Text.Json;

public class Method : Printable<Method> {
    public Attribute[] Attributes { get; set; } 
    public Attribute[] ParameterAttributes { get; set; } 
    public string[] TypeParameters { get; set; }
    public Argument[] Parameters { get; set; }
    public Argument ReturnValue { get; set; }
    public String Name { get; set; }
    public String Type { get; set; }
    public String[] Modifiers { get; set; }
    public Dictionary<string, String> Body { get; set; }
    public string[] Lines { get; set; }
    public Argument[] LocalsInits { get; set; }
    public bool? IsStatic => Modifiers?.Contains("static");
    public bool? IsGeneric => TypeParameters?.Length > 0;
    public bool IsConstructor => Name == ".ctor";
    public int MaxStack { get; set; }
    public string Code { get; set; }
}