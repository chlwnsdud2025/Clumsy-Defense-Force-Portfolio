using Cysharp.Threading.Tasks;
using System.Collections.Generic;

public interface ICharacterService
{
    UniTask<bool> InitializeAsync(IResourceService resourceService);
    IReadOnlyList<CharacterDefinitionSO> GetAllCharacters();
    CharacterDefinitionSO GetDefaultCharacter();
    CharacterDefinitionSO GetSelectedCharacter();
    bool TryGetCharacter(string characterId, out CharacterDefinitionSO characterDefinition);
    bool SetSelectedCharacter(string characterId);
    void SetSelectedCharacter(CharacterDefinitionSO characterDefinition);
    void ResetSelectedCharacter();
}
