using System;
using UnityEditor;
using UnityEngine;


namespace MarchingSquares.Util
{
    public class SerializeFieldReadOnlyAttribute : Attribute
    {

    }

#if UNITY_EDITOR

    [CustomPropertyDrawer(typeof(SerializeFieldReadOnlyAttribute))]
    public sealed class ReadOnlyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }

#endif
}