using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(QuadCoordinates))]
public class QuadCoordinatesDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        QuadCoordinates coordinates = new QuadCoordinates(
            property.FindPropertyRelative("x").intValue,
            property.FindPropertyRelative("z").intValue
        );

        position = EditorGUI.PrefixLabel(position, label);
        GUI.Label(position, coordinates.ToString());
    }
}
