using UnityEngine;
using System.Linq;

namespace RyleRadio
{

#if UNITY_EDITOR
    using NaughtyAttributes.Editor;
    using UnityEditor;

    using System.Reflection;
    using System.Collections;
    using System;
#endif

    // allows an int to display as a multiselect field for a given collection
    // NOTE: due to int limitations there can only be a max of 32 options
    public class MultiselectAttribute : PropertyAttribute
    {
        // id of the options variable this is referencing
        public string OptionsName { get; private set; }

        public MultiselectAttribute(string _optionsName)
        {
            OptionsName = _optionsName;
        }


        // self-explanatory
        // useful for list indexes
        public static int[] ZeroTo31 => new int[32] {
        0,  
        1,  2,  3,  4,  5,  6,  7,  8,
        9,  10, 11, 12, 13, 14, 15, 16,
        17, 18, 19, 20, 21, 22, 23, 24,
        25, 26, 27, 28, 29, 30, 31
    };

        // useful for reverse list indexes(?? i haven't really used this one but thought i'd include it nonetheless)
        public static int[] OneTo32 => new int[32] {
        1,  2,  3,  4,  5,  6,  7,  8,
        9,  10, 11, 12, 13, 14, 15, 16,
        17, 18, 19, 20, 21, 22, 23, 24,
        25, 26, 27, 28, 29, 30, 31,
        32
    };

        // converts a multiselect int to a list of whatever type you like
        public static T[] To<T>(int _flags, T[] _options)
        {
            if (_flags < 0)
            {
                Debug.LogWarning("A value less than 0 is being used as the flag variable in a MultiselectAttribute.To<T>() call! The value is " + _flags);
                return new T[0];
            }

            int[] outIndexes = new int[32];

            // for each number 0 - 32,
            for (int i = 0; i < 32; i++)
            {
                // check if the int has the bit flag for this index
                if ((_flags & (1 << i)) != 0)
                    outIndexes[i] = i; // if it does, add the index to the output list
                else
                    outIndexes[i] = -1; // otherwise make the index invalid
            }

            // get the items out of the options list based on the valid indexes in the output list
            T[] o = outIndexes
                .Where(b => b >= 0)
                .Select(i => _options[i])
                .ToArray();

            // give back the output list as T
            return o;
        }

        // shorthand to convert it to ints or indexes
        public static int[] ToInt(int _flags)
        {
            return To<int>(_flags, ZeroTo31);
        }
    }


#if UNITY_EDITOR
    // draws the attribute
    [CustomPropertyDrawer(typeof(MultiselectAttribute))]
    public class MultiselectDrawer : PropertyDrawer
    {
        // the inspector seems to think this is larger than it actually is, so we give it a small adjustment in height
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return -2;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // make the selection a bit mask
            int mask = property.intValue;
            string dataName = ((MultiselectAttribute)attribute).OptionsName; // save the id of the variable in the attribute

            object options = GetValues(property, dataName); // get the options from that variable
            string[] optionNames;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(label);

            if (options != null) // if there are options, display them
            {
                // we have to use the individual collection types here as there's no way (that i know of) to access IEnumerable without
                // specifying a generic type- and since we only have the variable id, we can't do that

                // if anyone knows of an alternate way to do this please do share :)

                if (options is IList list) // if the options are stored in a list, get them
                {
                    if (list.Count > 0)
                    {
                        optionNames = new string[list.Count];

                        // convert the options to strings for displaying
                        for (int i = 0; i < list.Count; i++)
                            optionNames[i] = list[i].ToString();

                        optionNames = optionNames.Reverse().ToArray();

                        // draw the field
                        mask = EditorGUILayout.MaskField(mask, optionNames);
                    }
                    else
                    {
                        // options is empty- tell the user
                        EditorGUILayout.LabelField($"{dataName} has size 0!");
                    }
                }
                else if (options is Array array) // if the options are stored in an array, get them
                {
                    if (array.Length > 0)
                    {
                        optionNames = new string[array.Length];

                        // convert the options to strings for displaying
                        for (int i = 0; i < array.Length; i++)
                            optionNames[i] = array.GetValue(i).ToString();

                        optionNames = optionNames.Reverse().ToArray();

                        // draw the field
                        mask = EditorGUILayout.MaskField(mask, optionNames);
                    }
                    else
                    {
                        // options is empty- tell the user
                        EditorGUILayout.LabelField($"{dataName} has size 0!");
                    }
                }
                else
                {
                    // options is an unsupported collection- tell the user
                    EditorGUILayout.LabelField($"{dataName} is not a List or an Array!");
                }
            }
            else // the attribute has been given an invalid options variable id- tell the user
                EditorGUILayout.LabelField($"Invalid collection at {dataName}! Cannot display multiselect!");

            EditorGUILayout.EndHorizontal();

            // assign the mask clampedValue
            property.intValue = mask;
        }

        // taken from DropdownPropertyDrawer.cs in NaughtyAttributes.Editor
        // ======
        private object GetValues(SerializedProperty property, string valuesName)
        {
            object target = PropertyUtility.GetTargetObjectWithProperty(property);

            FieldInfo valuesFieldInfo = ReflectionUtility.GetField(target, valuesName);
            if (valuesFieldInfo != null)
            {
                return valuesFieldInfo.GetValue(target);
            }

            PropertyInfo valuesPropertyInfo = ReflectionUtility.GetProperty(target, valuesName);
            if (valuesPropertyInfo != null)
            {
                return valuesPropertyInfo.GetValue(target);
            }

            MethodInfo methodValuesInfo = ReflectionUtility.GetMethod(target, valuesName);
            if (methodValuesInfo != null &&
                methodValuesInfo.ReturnType != typeof(void) &&
                methodValuesInfo.GetParameters().Length == 0)
            {
                return methodValuesInfo.Invoke(target, null);
            }

            return null;
        }
        // ======
    }
#endif

}