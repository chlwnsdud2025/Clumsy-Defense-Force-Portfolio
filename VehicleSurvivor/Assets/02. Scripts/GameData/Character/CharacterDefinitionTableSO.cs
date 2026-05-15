using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterDefinitionTableSO", menuName = "Scriptable Objects/Gameplay/Character Definition Table SO")]
public class CharacterDefinitionTableSO : ScriptableObject
{
    [SerializeField] private string _defaultCharacterId = "character.default";
    [SerializeField] private List<CharacterDefinitionSO> _characters = new List<CharacterDefinitionSO>();

    public string DefaultCharacterId => _defaultCharacterId;
    public IReadOnlyList<CharacterDefinitionSO> Characters => _characters;

    private void OnValidate()
    {
        _defaultCharacterId = string.IsNullOrWhiteSpace(_defaultCharacterId) ? string.Empty : _defaultCharacterId.Trim();
    }
}
