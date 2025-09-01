using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

static class RadioUtils
{
    public static float Remap(this float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }

    public static Vector3 Abs(this Vector3 _value)
    {
        return new Vector3(
            Mathf.Abs(_value.x),
            Mathf.Abs(_value.y),
            Mathf.Abs(_value.z)
        );
    }

    // adjusted from https://iquilezles.org/articles/distfunctions/
    public static float SignedDistanceToBox(Vector3 _point, Bounds _box)
    {
        Vector3 adjustedPoint = _point - _box.center;
        Vector3 q = adjustedPoint.Abs() - _box.size;

        return q.magnitude + Mathf.Min(Mathf.Max(q.x, q.y, q.z), 0);
    }

    public static Type[] FindDerivedTypes(Type baseType)
    {
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        IEnumerable<Type> types = new List<Type>();
 
        foreach (Assembly assembly in assemblies)
            types = types.Union(assembly.GetTypes().Where(t => 
                t != baseType 
                && baseType.IsAssignableFrom(t) 
                && !t.IsInterface
                && !t.IsGenericType
                && !t.IsAbstract
            ));

        return types.ToArray();
    }
}