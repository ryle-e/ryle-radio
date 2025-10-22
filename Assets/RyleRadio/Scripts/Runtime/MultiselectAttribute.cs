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

    /// <summary>
    /// A custom attribute that allows ints to display as a multiselect dropdown for a given collection, like a custom LayerMask. Due to int limitations you can only have up to 32 options
    /// </summary>
    public class MultiselectAttribute : PropertyAttribute
    {
        /// <summary>
        /// Name of the variable this attribute uses for the options list
        /// </summary>
        public string OptionsName { get; private set; }

        /// <summary>
        /// A filler array with numbers 0 - 31, used when converting from a flag int to a list subset
        /// </summary>
        public static int[] ZeroTo31 => new int[32] 
        {
            0,  
            1,  2,  3,  4,  5,  6,  7,  8,
            9,  10, 11, 12, 13, 14, 15, 16,
            17, 18, 19, 20, 21, 22, 23, 24,
            25, 26, 27, 28, 29, 30, 31
        };

        /// <summary>
        /// A filler array with numbers 1 - 32. Not yet used, but theoretically helpful for indexing from the end of a list (list[^i] instead of list[i])
        /// </summary>
        public static int[] OneTo32 => new int[32] 
        {
            1,  2,  3,  4,  5,  6,  7,  8,
            9,  10, 11, 12, 13, 14, 15, 16,
            17, 18, 19, 20, 21, 22, 23, 24,
            25, 26, 27, 28, 29, 30, 31,
            32
        };

        /// <summary>
        /// Initialises the attribute
        /// </summary>
        public MultiselectAttribute(string _optionsName)
        {
            OptionsName = _optionsName;
        }


       /// <summary>
        /// Converts an int with the Multiselect attribute to a subset of a given list according to the int flags- converts the flag int to usable data
        /// </summary>
        /// <typeparam name="T">The eventType content of the list we're converting the flags to</typeparam>
        /// <param name="_flags">The int with the Multiselect attribute- a flag int</param>
        /// <param name="_options">The list we're getting a subset of according to the flags</param>
        /// <returns>A subset of `_options` that matches the `_flags` int</returns>
        /// <example><code>
        /// string[] options = new string[4] { "awesome", "attribute", "thanks", "ryle-e" };
        /// 
        /// [Multiselect("options")]
        /// private int flags1; // in the inspector, we set it to ["awesome", "thanks"]- the first and third options in the inspector. this int then becomes 0x0101
        /// 
        /// public void Convert()
        /// {
        ///     int flags2 = 0x1010; // equivalent to selecting the second and fourth options in the inspector
        ///     
        ///     List<string> converted1 = Multiselect.To<string>(flags1, options); // sets to ["awesome", "thanks"]
        ///     List<string> converted2 = Multiselect.To<string>(flags2, options); // sets to ["attribute", "ryle-e"]
        /// }
        /// </code></example>
        public static T[] To<T>(int _flags, T[] _options)
        {
            // if the flag int is invalid, alert the user and return nothing
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

        /// <summary>
        /// Shorthand for `MultiselectAttribute.To<int>(_flags, _options)`. Useful for converting a multiselect to indexes in a list
        /// </summary>
        /// <param name="_flags">The int with the Multiselect attribute</param>
        /// <returns></returns>
        public static int[] ToInt(int _flags)
        {
            return To<int>(_flags, ZeroTo31);
        }
    }


#if UNITY_EDITOR
    /// <summary>
    /// Draws the MultiselectAttribute in the inspector
    /// </summary>
    [CustomPropertyDrawer(typeof(MultiselectAttribute))]
    public class MultiselectDrawer : PropertyDrawer
    {
#if !SKIP_IN_DOXYGEN
        // the inspector seems to think this is larger than it actually is, so we give it a small adjustment in height
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return -2;
        }
#endif

        /// <summary>
        /// Displays the multiselect dropdown
        /// </summary>
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
                // specifying a generic eventType- and since we only have the variable id, we can't do that

                // if anyone knows of an alternate way to do this please do share :)

                if (options is IList list) // if the options are stored in a list, get them
                {
                    if (list.Count > 0)
                    {
                        optionNames = new string[list.Count];

                        // convert the options to strings for displaying
                        for (int i = 0; i < list.Count; i++)
                            optionNames[i] = list[i].ToString();

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

#if !SKIP_IN_DOXYGEN
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
#endif
    }
#endif

}