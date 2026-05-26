#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SoundDatabaseSO))]
public class SoundDatabaseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (!GUILayout.Button("Populate Missing Sound IDs"))
            return;

        SoundDatabaseSO database = (SoundDatabaseSO)target;
        Undo.RecordObject(database, "Populate Sound IDs");

        SerializedObject serialized = new SerializedObject(database);
        SerializedProperty sfxProp = serialized.FindProperty("sfxEntries");
        SerializedProperty bgmProp = serialized.FindProperty("bgmEntries");

        PopulateEnumEntries(sfxProp, typeof(ESoundId), "id", ApplySfxDefaults);
        PopulateEnumEntries(bgmProp, typeof(EBgmId), "id", ApplyBgmDefaults);

        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(database);
    }

    private static void PopulateEnumEntries(
        SerializedProperty listProp,
        Type enumType,
        string idFieldName,
        Action<SerializedProperty> applyDefaults)
    {
        HashSet<int> existing = new HashSet<int>();
        for (int i = 0; i < listProp.arraySize; i++)
        {
            SerializedProperty element = listProp.GetArrayElementAtIndex(i);
            existing.Add(element.FindPropertyRelative(idFieldName).enumValueIndex);
        }

        Array values = Enum.GetValues(enumType);
        for (int i = 0; i < values.Length; i++)
        {
            int enumIndex = (int)values.GetValue(i);
            if (enumIndex == 0 || existing.Contains(enumIndex))
                continue;

            listProp.InsertArrayElementAtIndex(listProp.arraySize);
            SerializedProperty added = listProp.GetArrayElementAtIndex(listProp.arraySize - 1);
            added.FindPropertyRelative(idFieldName).enumValueIndex = enumIndex;
            applyDefaults(added);
        }
    }

    private static void ApplySfxDefaults(SerializedProperty entry)
    {
        entry.FindPropertyRelative("volume").floatValue = 1f;
        entry.FindPropertyRelative("pitchMin").floatValue = 0.95f;
        entry.FindPropertyRelative("pitchMax").floatValue = 1.05f;
        entry.FindPropertyRelative("cooldownSeconds").floatValue = 0f;
        entry.FindPropertyRelative("priority").enumValueIndex = (int)ESfxPriority.Normal;
        entry.FindPropertyRelative("variations").ClearArray();
    }

    private static void ApplyBgmDefaults(SerializedProperty entry)
    {
        entry.FindPropertyRelative("volume").floatValue = 1f;
        entry.FindPropertyRelative("clip").objectReferenceValue = null;
    }
}
#endif
