using System.Text.Json;

public class Field : Printable<Field>{
    public Attribute[] Attributes { get; set; } 
    public Argument ReturnValue { get; set; }
    public String Name { get; set; }
    public String Type { get; set; }
    public String[] Modifiers { get; set; }
    public string[] Lines { get; set; }
    public bool? IsStatic => Modifiers?.Contains("static");
    public bool? IsGenerated => Attributes?.Where(attr => attr.Name == "[System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute").Count() > 0;
    public string Code { get; set; }
}