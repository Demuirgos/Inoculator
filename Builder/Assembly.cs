using System.Reflection;

public class Assembly : Printable<Assembly> {
    public string Name { get; set; }
    public Class[] Classes { get; set; }
    public Attribute[] Attributes { get; set; }
    public string Code { get; set; }

}