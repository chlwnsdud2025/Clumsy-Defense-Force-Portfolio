using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerData", menuName = "Scriptable Objects/PlayerData")]
public class PlayerData : ScriptableObject
{
    //테스트용
    
    [field: SerializeField] public GameObject[] PlayerIcons { get; private set; }
    [field: SerializeField] public GameItemData[] SaveData { get; private set; }
    [field: SerializeField] public List<StringValuePair<string, int>> KeyValues { get; private set; }
}

[Serializable]
public struct StringValuePair<A, B>
{
    public A key;
    public B value;
}

[Serializable]
public struct GameItemData
{
    public string name;
    public string description;
    public string icon;
    public List<string> abilities;
    public List<StringValuePair<string, int>> keyValues;
    public Dictionary<string, int> stats; // Dictionary는 Unity Inspector에서 직접 편집할 수 없으므로, List<StringValuePair>로 대체하여 저장
}
