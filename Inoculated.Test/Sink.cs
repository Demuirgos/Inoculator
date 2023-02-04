using Inoculator.Attributes;
using Inoculator.Builder;
using System.Diagnostics;
using System.Reflection;

public class MetadataSink : InterceptorAttribute
{
    public static int CallCount = 0;
}