using System.Diagnostics;
using System.Text;

namespace Cybertron.CUpdater;

public static class ArgsHelper
{
    public static void AddToProcessStartInfo(ProcessStartInfo processStartInfo, UpdaterArgs updaterArgs)
    {
        var type = typeof(UpdaterArgs);
        var properties = type.GetProperties();
        var sb = new StringBuilder(properties.Length);
        foreach (var propertyInfo in properties)
        {
            if (propertyInfo.GetValue(updaterArgs) is IEnumerable<object> enumerable)
            {
                foreach (var obj in enumerable)
                {
                    sb.Append(' ');
                    sb.Append(QuoteArgument(obj.ToString()));
                }
            }
            else
            {
                sb.Append(' ');
                sb.Append(QuoteArgument(propertyInfo.GetValue(updaterArgs)?.ToString()));
            }
        }

        processStartInfo.Arguments += sb.ToString();
    }
    
    public static UpdaterArgs ArrayToArgs(string[] array)
    {
        var type = typeof(UpdaterArgs);
        var ctorTypes = new List<Type>();
        var vals = new List<object>();
        
        // Last should be an enumerable
        var properties = type.GetProperties();
        var i = 0;
        foreach (var propertyInfo in properties[..^1])
        {
            ctorTypes.Add(propertyInfo.PropertyType);
            vals.Add(array[i]);
            i++;
        }
        
        ctorTypes.Add(properties[^1].PropertyType);
        var enumerable = array[i..].ToList();

        vals.Add(enumerable);
        
        var ctor = type.GetConstructor(ctorTypes.ToArray());
        var result = (UpdaterArgs)ctor!.Invoke(vals.ToArray());
        return result;
    }

    private static string QuoteArgument(string? argument)
    {
        // Maybe a powershell bug? but arguments to the ps script require triple quotes when there are spaces in the arg
        // The path to the ps script doesn't require triple quotes though
        // Unix shell script seems to work as it should without any unique fixes
        return OperatingSystem.IsWindows() ? $"\"\"\"{argument}\"\"\"" : $"\"{argument}\"";
    }
}
