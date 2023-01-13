using System.Reflection;
using System.Text.Json;

public class Class : Printable<Class> {
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
    public string[] TypeParameters { get; set; }
    public Attribute[] ParameterAttributes { get; set; } 
    public Attribute[] ConstraintsAttributes { get; set; } 
    public Method[] Methods { get; set; }
    public Field[] Fields { get; set; }
    public Event[] Events { get; set; }
    public Property[] Properties { get; set; }
    public Class[] TypeDefs { get; set; }
    public Attribute[] Attributes { get; set; }
    public string[] Modifiers { get; set; }
    public bool? IsGeneric => TypeParameters?.Length > 0;
    public ClassType? Type => BaseClass switch 
    {
        "[System.Runtime]System.MulticastDelegate" => ClassType.Delegate,
        "[System.Runtime]System.Enum" => ClassType.Enum,
        "[System.Runtime]System.ValueType" => ClassType.Struct,
        _ => Modifiers is not null 
                ? Modifiers.Contains("interface") ? ClassType.Interface : ClassType.Class
                : null
    };
    public string Code { get; set; }
}