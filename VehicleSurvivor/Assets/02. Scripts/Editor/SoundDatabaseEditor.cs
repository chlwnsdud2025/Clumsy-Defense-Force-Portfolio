#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SoundDatabaseSO))]
public class SoundDatabaseEditor : Editor
{
    private static readonly string[] SfxDetailFields = { "volume", "pitchMin", "pitchMax", "cooldownSeconds", "priority" };
    private static readonly string[] BgmDetailFields = { "volume" };

    private readonly Dictionary<string, bool> _entryFoldouts = new();
    private readonly Dictionary<string, bool> _sectionFoldouts = new();

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawEntrySection(
            "sfxEntries",
            typeof(ESoundId),
            "SFX Entries",
            "variations",
            SfxDetailFields);

        EditorGUILayout.Space(8f);

        DrawEntrySection(
            "bgmEntries",
            typeof(EBgmId),
            "BGM Entries",
            "clip",
            BgmDetailFields);

        EditorGUILayout.Space(8f);

        if (GUILayout.Button("Populate Missing Sound IDs"))
        {
            SoundDatabaseSO database = (SoundDatabaseSO)target;
            Undo.RecordObject(database, "Populate Sound IDs");

            SerializedProperty sfxProp = serializedObject.FindProperty("sfxEntries");
            SerializedProperty bgmProp = serializedObject.FindProperty("bgmEntries");

            PopulateEnumEntries(sfxProp, typeof(ESoundId), "id", ApplySfxDefaults);
            PopulateEnumEntries(bgmProp, typeof(EBgmId), "id", ApplyBgmDefaults);

            EditorUtility.SetDirty(database);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawEntrySection(
        string listPropertyName,
        Type enumType,
        string sectionLabel,
        string clipFieldName,
        string[] detailFieldNames)
    {
        SerializedProperty listProp = serializedObject.FindProperty(listPropertyName);
        string sectionKey = $"{target.GetInstanceID()}_{listPropertyName}";

        if (!_sectionFoldouts.ContainsKey(sectionKey))
            _sectionFoldouts[sectionKey] = true;

        _sectionFoldouts[sectionKey] = EditorGUILayout.Foldout(
            _sectionFoldouts[sectionKey],
            $"{sectionLabel} ({listProp.arraySize})",
            true);

        if (!_sectionFoldouts[sectionKey])
            return;

        EditorGUI.indentLevel++;
        DrawEntryList(listProp, enumType, clipFieldName, detailFieldNames);
        EditorGUI.indentLevel--;
    }

    private void DrawEntryList(
        SerializedProperty listProp,
        Type enumType,
        string clipFieldName,
        string[] detailFieldNames)
    {
        List<int> sortedIndices = new List<int>(listProp.arraySize);
        for (int i = 0; i < listProp.arraySize; i++)
            sortedIndices.Add(i);

        sortedIndices.Sort((a, b) =>
        {
            SerializedProperty idA = listProp.GetArrayElementAtIndex(a).FindPropertyRelative("id");
            SerializedProperty idB = listProp.GetArrayElementAtIndex(b).FindPropertyRelative("id");
            return idA.enumValueIndex.CompareTo(idB.enumValueIndex);
        });

        foreach (int index in sortedIndices)
        {
            SerializedProperty entry = listProp.GetArrayElementAtIndex(index);
            DrawEntryRow(entry, enumType, clipFieldName, detailFieldNames);
            EditorGUILayout.Space(2f);
        }
    }

    private void DrawEntryRow(
        SerializedProperty entry,
        Type enumType,
        string clipFieldName,
        string[] detailFieldNames)
    {
        SerializedProperty idProp = entry.FindPropertyRelative("id");
        SerializedProperty clipProp = entry.FindPropertyRelative(clipFieldName);

        string enumName = Enum.GetName(enumType, idProp.enumValueIndex) ?? "Unknown";
        bool hasClip = HasAssignedClip(clipProp, clipFieldName);

        string foldoutKey = entry.propertyPath;
        if (!_entryFoldouts.ContainsKey(foldoutKey))
            _entryFoldouts[foldoutKey] = false;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();

        Rect foldoutRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight, GUILayout.Width(14f));
        _entryFoldouts[foldoutKey] = EditorGUI.Foldout(foldoutRect, _entryFoldouts[foldoutKey], GUIContent.none, true);

        Color originalColor = GUI.contentColor;
        if (!hasClip)
            GUI.contentColor = new Color(1f, 0.75f, 0.4f);

        EditorGUILayout.LabelField(enumName, EditorStyles.boldLabel, GUILayout.MinWidth(160f), GUILayout.MaxWidth(220f));

        GUI.contentColor = originalColor;

        EditorGUILayout.PropertyField(clipProp, GUIContent.none);

        EditorGUILayout.EndHorizontal();

        if (_entryFoldouts[foldoutKey])
        {
            EditorGUI.indentLevel += 2;
            for (int i = 0; i < detailFieldNames.Length; i++)
            {
                SerializedProperty detailProp = entry.FindPropertyRelative(detailFieldNames[i]);
                EditorGUILayout.PropertyField(detailProp);
            }

            EditorGUI.indentLevel -= 2;
        }

        EditorGUILayout.EndVertical();
    }

    private static bool HasAssignedClip(SerializedProperty clipProp, string clipFieldName)
    {
        if (clipFieldName == "clip")
            return clipProp.objectReferenceValue != null;

        if (!clipProp.isArray || clipProp.arraySize == 0)
            return false;

        for (int i = 0; i < clipProp.arraySize; i++)
        {
            if (clipProp.GetArrayElementAtIndex(i).objectReferenceValue != null)
                return true;
        }

        return false;
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
