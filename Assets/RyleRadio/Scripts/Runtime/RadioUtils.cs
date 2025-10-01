using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace RyleRadio
{

    // random utilities used throughout the radio system
    static class RadioUtils
    {
        // remaps a value from between two to between two others
        public static float Remap(this float value, float from1, float to1, float from2, float to2)
        {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }

        // absolute value of a whole vector
        public static Vector3 Abs(this Vector3 _value)
        {
            return new Vector3(
                Mathf.Abs(_value.x),
                Mathf.Abs(_value.y),
                Mathf.Abs(_value.z)
            );
        }

        // gets all types derived from a given type
        public static Type[] FindDerivedTypes(Type baseType)
        {
            // we use all assemblies here for ease of use to developers creating and testing new tracks

            // get all assemblies used in the project
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            IEnumerable<Type> types = new List<Type>();

            // search through every assembly applicable classes
            foreach (Assembly assembly in assemblies)
                types = types.Union(assembly.GetTypes().Where(t => // find classes that,
                    t != baseType // aren't the given type
                    && baseType.IsAssignableFrom(t)  // inherit from the given type
                    && !t.IsInterface // aren't interfaces
                    && !t.IsGenericType // aren't generic
                    && !t.IsAbstract // aren't abstract
                ));

            // returns the types
            return types.ToArray();
        }
    }

}