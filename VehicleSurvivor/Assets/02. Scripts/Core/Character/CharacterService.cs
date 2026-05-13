using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class CharacterService : ICharacterService
{
    private readonly Dictionary<string, CharacterDefinitionSO> _characterLookup = new Dictionary<string, CharacterDefinitionSO>();

    private CharacterDefinitionTableSO _characterTable;
    private CharacterDefinitionSO _defaultCharacter;
    private CharacterDefinitionSO _selectedCharacter;

    public bool IsInitialized { get; private set; }

    public async UniTask<bool> InitializeAsync(IResourceService resourceService)
    {
        if (IsInitialized)
            return true;

        if (resourceService == null)
            throw new ArgumentNullException(nameof(resourceService));

        CharacterDefinitionTableSO loadedTable = await resourceService.LoadAssetAsync<CharacterDefinitionTableSO>(ResourceKeys.Data.CharacterDefinitionTable);
        if (loadedTable == null)
        {
            Debug.LogError($"[CharacterService] '{ResourceKeys.Data.CharacterDefinitionTable}' 키로 CharacterDefinitionTableSO를 로드하지 못했습니다.");
            ResetState();
            return false;
        }

        _characterTable = loadedTable;
        BuildLookup();
        _defaultCharacter = ResolveDefaultCharacter(_characterTable.DefaultCharacterId);
        _selectedCharacter = _defaultCharacter;
        IsInitialized = true;

        return true;
    }

    public IReadOnlyList<CharacterDefinitionSO> GetAllCharacters()
    {
        if (_characterTable == null)
            return Array.Empty<CharacterDefinitionSO>();

        return _characterTable.Characters;
    }

    public CharacterDefinitionSO GetDefaultCharacter()
    {
        return _defaultCharacter;
    }

    public CharacterDefinitionSO GetSelectedCharacter()
    {
        return _selectedCharacter != null ? _selectedCharacter : _defaultCharacter;
    }

    public bool TryGetCharacter(string characterId, out CharacterDefinitionSO characterDefinition)
    {
        if (string.IsNullOrWhiteSpace(characterId))
        {
            characterDefinition = null;
            return false;
        }

        return _characterLookup.TryGetValue(characterId.Trim(), out characterDefinition);
    }

    public bool SetSelectedCharacter(string characterId)
    {
        if (!TryGetCharacter(characterId, out CharacterDefinitionSO characterDefinition))
            return false;

        _selectedCharacter = characterDefinition;
        return true;
    }

    public void SetSelectedCharacter(CharacterDefinitionSO characterDefinition)
    {
        if (characterDefinition == null)
        {
            ResetSelectedCharacter();
            return;
        }

        if (!TryGetCharacter(characterDefinition.CharacterId, out CharacterDefinitionSO registeredCharacter))
        {
            Debug.LogWarning($"[CharacterService] 등록되지 않은 캐릭터 '{characterDefinition.name}'은(는) 선택할 수 없습니다.");
            return;
        }

        _selectedCharacter = registeredCharacter;
    }

    public void ResetSelectedCharacter()
    {
        _selectedCharacter = _defaultCharacter;
    }

    private void BuildLookup()
    {
        _characterLookup.Clear();

        if (_characterTable == null || _characterTable.Characters == null)
        {
            Debug.LogWarning("[CharacterService] CharacterDefinitionTableSO가 없어 캐릭터 목록을 비운 상태로 시작합니다.");
            return;
        }

        for (int i = 0; i < _characterTable.Characters.Count; i++)
        {
            CharacterDefinitionSO character = _characterTable.Characters[i];
            if (character == null)
                continue;

            if (string.IsNullOrWhiteSpace(character.CharacterId))
            {
                Debug.LogWarning($"[CharacterService] '{character.name}'의 CharacterId가 비어 있어 등록하지 않습니다.");
                continue;
            }

            string characterId = character.CharacterId.Trim();
            if (_characterLookup.ContainsKey(characterId))
            {
                Debug.LogWarning($"[CharacterService] 중복 CharacterId '{characterId}'가 감지되어 뒤의 항목을 무시합니다.");
                continue;
            }

            _characterLookup.Add(characterId, character);
        }
    }

    private void ResetState()
    {
        IsInitialized = false;
        _characterTable = null;
        _defaultCharacter = null;
        _selectedCharacter = null;
        _characterLookup.Clear();
    }

    private CharacterDefinitionSO ResolveDefaultCharacter(string defaultCharacterId)
    {
        if (TryGetCharacter(defaultCharacterId, out CharacterDefinitionSO defaultCharacter))
            return defaultCharacter;

        if (!string.IsNullOrWhiteSpace(defaultCharacterId))
            Debug.LogWarning($"[CharacterService] 기본 캐릭터 ID '{defaultCharacterId}'를 찾지 못해 첫 번째 캐릭터를 사용합니다.");

        if (_characterTable != null && _characterTable.Characters != null)
        {
            for (int i = 0; i < _characterTable.Characters.Count; i++)
            {
                if (_characterTable.Characters[i] != null)
                    return _characterTable.Characters[i];
            }
        }

        Debug.LogWarning("[CharacterService] 기본 캐릭터가 없어 선택 상태 없이 시작합니다.");
        return null;
    }
}
