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
            SfxDetailFields,
            ApplySfxDefaults);

        EditorGUILayout.Space(8f);

        DrawEntrySection(
            "bgmEntries",
            typeof(EBgmId),
            "BGM Entries",
            "clip",
            BgmDetailFields,
            ApplyBgmDefaults);

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
        string[] detailFieldNames,
        Action<SerializedProperty> applyDefaults)
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
        DrawListToolbar(listProp, enumType, applyDefaults);
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

        for (int displayIndex = 0; displayIndex < sortedIndices.Count; displayIndex++)
        {
            int arrayIndex = sortedIndices[displayIndex];
            SerializedProperty entry = listProp.GetArrayElementAtIndex(arrayIndex);
            if (DrawEntryRow(listProp, arrayIndex, entry, enumType, clipFieldName, detailFieldNames))
                break;

            EditorGUILayout.Space(2f);
        }
    }

    private void DrawListToolbar(
        SerializedProperty listProp,
        Type enumType,
        Action<SerializedProperty> applyDefaults)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        EditorGUI.BeginDisabledGroup(!HasUnusedEnumId(listProp, enumType));
        if (GUILayout.Button("+", GUILayout.Width(28f)))
        {
            Undo.RecordObject(target, "Add Sound Entry");
            AddEntry(listProp, enumType, applyDefaults);
        }
        EditorGUI.EndDisabledGroup();

        EditorGUI.BeginDisabledGroup(listProp.arraySize == 0);
        if (GUILayout.Button("-", GUILayout.Width(28f)))
        {
            Undo.RecordObject(target, "Remove Sound Entry");
            listProp.DeleteArrayElementAtIndex(listProp.arraySize - 1);
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(4f);
    }

    private static void AddEntry(
        SerializedProperty listProp,
        Type enumType,
        Action<SerializedProperty> applyDefaults)
    {
        listProp.InsertArrayElementAtIndex(listProp.arraySize);
        SerializedProperty added = listProp.GetArrayElementAtIndex(listProp.arraySize - 1);
        applyDefaults(added);

        if (TryGetFirstUnusedEnumId(listProp, enumType, out int enumIndex))
            added.FindPropertyRelative("id").enumValueIndex = enumIndex;
    }

    private static bool HasUnusedEnumId(SerializedProperty listProp, Type enumType)
    {
        return TryGetFirstUnusedEnumId(listProp, enumType, out _);
    }

    private static bool TryGetFirstUnusedEnumId(SerializedProperty listProp, Type enumType, out int enumIndex)
    {
        HashSet<int> used = new HashSet<int>();
        for (int i = 0; i < listProp.arraySize; i++)
        {
            used.Add(listProp.GetArrayElementAtIndex(i).FindPropertyRelative("id").enumValueIndex);
        }

        Array values = Enum.GetValues(enumType);
        for (int i = 0; i < values.Length; i++)
        {
            int value = (int)values.GetValue(i);
            if (value == 0 || used.Contains(value))
                continue;

            enumIndex = value;
            return true;
        }

        enumIndex = 0;
        return false;
    }

    private bool DrawEntryRow(
        SerializedProperty listProp,
        int arrayIndex,
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

        if (GUILayout.Button("-", GUILayout.Width(22f)))
        {
            Undo.RecordObject(target, "Remove Sound Entry");
            listProp.DeleteArrayElementAtIndex(arrayIndex);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            return true;
        }

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
        return false;
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
