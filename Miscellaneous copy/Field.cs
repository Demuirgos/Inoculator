using System.Text.Json;

public class Field : Printable<Field>{
    public String[] Attributes { get; set; } 
    public Argument ReturnValue { get; set; }
    public String Name { get; set; }
    public String Type { get; set; }
    public String[] Modifiers { get; set; }
    public string[] Lines { get; set; }
    public bool? IsStatic => Modifiers?.Contains("static");
    public bool? IsGenerated => Attributes?.Contains("[System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute");
}