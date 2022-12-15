using System.Reflection;
using System.Text.Json;

public class Class {
    public enum ClassType {
        Class,
        Struct,
        Interface,
        Enum,
        Delegate
    }
    public string Name { get; set; }
    public string BaseClass { get; set; }
    public string[] Interfaces { get; set; }
    public Method[] Methods { get; set; }
    public Field[] Fields { get; set; }
    public Event[] Events { get; set; }
    public Property[] Properties { get; set; }
    public Class[] TypeDefs { get; set; }
    public string[] Attributes { get; set; }
    public string[] Modifiers { get; set; }
    public ClassType Type => BaseClass switch 
    {
        "[System.Runtime]System.MulticastDelegate" => ClassType.Delegate,
        "[System.Runtime]System.Enum" => ClassType.Enum,
        "[System.Runtime]System.ValueType" => ClassType.Struct,
        _ => Modifiers.Contains("interface") ? ClassType.Interface : ClassType.Class
    };
    public string[] Body { get; set; }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions {
            WriteIndented = true
        });
    }
}