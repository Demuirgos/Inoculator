using System.Reflection;

public class Assembly : Printable<Assembly> {
    public string Name { get; set; }
    public Class[] Classes { get; set; }
    public string[] Attributes { get; set; }
    public string[] Lines { get; set; }
}