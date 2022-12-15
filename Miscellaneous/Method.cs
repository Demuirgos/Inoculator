using System.Text.Json;

public class Method : Printable<Method> {
    public String[] Attributes { get; set; } 
    public Argument[] Parameters { get; set; }
    public Argument ReturnValue { get; set; }
    public String Name { get; set; }
    public String Type { get; set; }
    public String[] Modifiers { get; set; }
    public String[] Body { get; set; }
    public string[] Lines { get; set; }
    public Argument[] LocalsInits { get; set; }
    public bool? IsStatic => Modifiers?.Contains("static");
    public bool IsConstructor => Name == ".ctor";
    public int MaxStack { get; set; }
    public Exception? Exception { get; set; }
}