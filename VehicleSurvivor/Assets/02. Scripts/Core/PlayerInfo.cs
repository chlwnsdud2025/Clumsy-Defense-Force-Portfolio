using UnityEngine;

[CreateAssetMenu(fileName = "PlayerInfo", menuName = "Scriptable Objects/PlayerInfo")]
public class PlayerInfo : ScriptableObject
{
    [field: SerializeField] public string CharacterName { get; private set; }
    [field: SerializeField] public Sprite Icon { get; private set; }
    [field: SerializeField] public Sprite Portrait { get; private set; }

    [field: SerializeField] public int MaxHp { get; private set; }
    [field: SerializeField] public int MoveSpeed { get; private set; }
    [field: SerializeField] public string Description { get; private set; }

    [field: SerializeField] public GameObject PlayerPrefab { get; private set; }
}