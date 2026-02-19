using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

[AttributeUsage(AttributeTargets.Method)]
public class StaticResetAttribute : Attribute
{
    public int Order;
    public StaticResetAttribute(int order = 0)
    {
        Order = order;
    }
}

public static class StaticResetSystem
{
    private static bool _cached;
    private static List<(MethodInfo method, int order)> _methods;

    // Срабатывает ВСЕГДА при старте Play Mode
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void AutoReset()
    {
        ResetAllStatics();
    }

    private static void CacheMethods()
    {
        _methods = new List<(MethodInfo, int)>();

        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.FullName.StartsWith("Assembly-CSharp"));

        foreach (var assembly in assemblies)
        {
            Type[] types;

            try { types = assembly.GetTypes(); }
            catch { continue; }

            foreach (var type in types)
            {
                var methods = type.GetMethods(
                    BindingFlags.Static |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);

                foreach (var method in methods)
                {
                    var attr = method.GetCustomAttribute<StaticResetAttribute>();
                    if (attr != null)
                    {
                        _methods.Add((method, attr.Order));
                    }
                }
            }
        }

        _cached = true;
    }

    public static void ResetAllStatics()
    {
        if (!_cached)
            CacheMethods();

        foreach (var entry in _methods.OrderBy(m => m.order))
        {
            try
            {
                entry.method.Invoke(null, null);
            }
            catch (Exception e)
            {
                Debug.LogError($"[StaticReset] Failed in {entry.method.DeclaringType}: {e}");
            }
        }

        Debug.Log($"[StaticReset] Auto reset {_methods.Count} systems.");
    }
}
