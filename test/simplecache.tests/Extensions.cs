using System.Reflection;

namespace simplecache.tests;

public static class Extensions
{
    public static T GetPrivateProperty<T>(this object obj, string name) {
        var bindingFlags =  BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        var typ = obj.GetType();
        var field = typ.GetProperty(name, bindingFlags);
        return (T)field?.GetValue(obj);
    }
}