using Inoculator.Attributes;
using Inoculator.Builder;

public class LogEntrencyAttribute : InterceptorAttribute
{
    public override void OnEntry(Metadata method)
    {
        Console.WriteLine($"Started Method {method.Name}");
    }

    public override void OnExit(Metadata method)
    {
        Console.WriteLine($"Finished Method {method.Name}");
    }
}