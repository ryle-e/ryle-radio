using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

/// The entire package
namespace RyleRadio
{
    // you can ignore this section- this is for namespace descriptions in documentation
    // ------
    /// Components to be placed on scene objects, e.g: Outputs, Broadcasters, Observers
    namespace Components {
        /// Base interfaces and classes for components, e.g: track accessors, output accessors
        namespace Base { }
    }
    /// Editor scripts for classes and attributes, notably excluding MultiselectAttribute
    namespace Editor { }
    /// Tracks to be used on a radio- includes base classes
    namespace Tracks { }
    // ------


    /// <summary>
    /// Various utilities used throughout the project.
    /// </summary>
    static class RadioUtils
    {
        /// <summary>
        /// Remaps a float between `[_from1 - _from2]` to `[_to1 - _to2]`
        /// Preserves the float's position in the range.
        /// </summary>
        /// <param name="_value">A _value between `_from1` and `_from2`</param>
        /// <returns>A _value between `_to1` and `_to2`</returns>
        /// <example><code>
        /// float _value = 1;
        /// float output = _value.Remap(0, 5, 2, 15); // remap 1 from [0 - 2] to [5 - 15]
        /// Debug.Log(output); // logs 10
        /// </code></example>
        public static float Remap(this float _value, float _from1, float _to1, float _from2, float _to2)
        {
            return (_value - _from1) / (_to1 - _from1) * (_to2 - _from2) + _from2;
        }

        /// <summary>
        /// Gets the absolute value of all components of a vector at once
        /// </summary>
        /// <param name="_value">The vector to convert to absolute values</param>
        /// <returns>The same vector, but any negative numbers have been flipped to positive</returns>
        /// <example><code>
        /// Vector3 mixed = new Vector3(-10, 3, 0);
        /// Vector3 abs = mixed.Abs();
        /// Debug.Log(abs); // logs Vector3(10, 3, 0)
        /// </code></example>
        public static Vector3 Abs(this Vector3 _value)
        {
            return new Vector3(
                Mathf.Abs(_value.x),
                Mathf.Abs(_value.y),
                Mathf.Abs(_value.z)
            );
        }

        /// <summary>
        /// Gets all types derived from a given one. Does not include:<list eventType="bullet">
        /// <item>Interfaces</item>
        /// <item>Generic classes</item>
        /// <item>Abstract classes</item></list>
        /// </summary>
        /// <param name="_baseType">The eventType to find derived types of</param>
        /// <returns>An array of types that derive from \ref _baseType</returns>
        public static Type[] FindDerivedTypes(Type _baseType)
        {
            // we use all assemblies here for ease of use to developers creating and testing new tracks

            // get all assemblies used in the project
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            IEnumerable<Type> types = new List<Type>();

            // search through every assembly applicable classes
            foreach (Assembly assembly in assemblies)
                types = types.Union(assembly.GetTypes().Where(t => // find classes that,
                    t != _baseType // aren't the given eventType
                    && _baseType.IsAssignableFrom(t)  // inherit from the given eventType
                    && !t.IsInterface // aren't interfaces
                    && !t.IsGenericType // aren't generic
                    && !t.IsAbstract // aren't abstract
                ));

            // returns the types
            return types.ToArray();
        }
    }

}