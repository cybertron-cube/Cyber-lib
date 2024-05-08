using System.Diagnostics;

namespace Cybertron.CUpdater;

public static class ArgsHelper
{
    public static void AddToProcessStartInfo(ref ProcessStartInfo processStartInfo, UpdaterArgs updaterArgs)
    {
        var type = typeof(UpdaterArgs);
        foreach (var propertyInfo in type.GetProperties())
        {
            if (propertyInfo.GetValue(updaterArgs) is IEnumerable<object> enumerable)
            {
                foreach (var obj in enumerable)
                {
                    processStartInfo.ArgumentList.Add(obj.ToString()!);
                }
            }
            else
            {
                processStartInfo.ArgumentList.Add(propertyInfo.GetValue(updaterArgs)!.ToString()!);
            }
        }
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
}
