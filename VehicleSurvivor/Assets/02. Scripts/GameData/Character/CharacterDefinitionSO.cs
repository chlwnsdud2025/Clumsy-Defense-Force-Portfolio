using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct CharacterCombatStats
{
    [Min(1f)] public float MaxHealth;
    [Min(0f)] public float Defense;
    [Min(0f)] public float MoveSpeedMultiplier;
    [Min(0f)] public float DamageMultiplier;
    [Min(0f)] public float PickupRange;
}

[System.Serializable]
public struct CharacterLoadoutData
{
    public string StartingWeaponId;
    public string UniqueTraitId;
    [TextArea] public string TraitDescription;
    public List<string> PassiveIds;
}

[System.Serializable]
public struct CharacterMetaData
{
    public bool IsDefaultUnlocked;
    [Min(0)] public int UnlockCost;
    public CharacterRarity Rarity;
    public List<string> Tags;
}

public enum CharacterRarity
{
    Common,
    Rare,
    Epic,
    Legendary,
}

[CreateAssetMenu(fileName = "CharacterDefinitionSO", menuName = "Scriptable Objects/Gameplay/Character Definition SO")]
public class CharacterDefinitionSO : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string _characterId = "character.default";
    [SerializeField] private string _displayName = "Default Survivor";
    [TextArea][SerializeField] private string _description = "Default playable character.";
    [SerializeField] private Sprite _icon;
    [SerializeField] private Sprite _portrait;
    [SerializeField] private GameObject _previewPrefab;
    [SerializeField] private GameObject _visualPrefab;

    [Header("Default Weapon")]
    [SerializeField] private string defaultWeaponId;
    public string DefaultWeaponId => defaultWeaponId;

    [Header("Combat")]
    [SerializeField]
    private CharacterCombatStats _combat = new CharacterCombatStats
    {
        MaxHealth = 100f,
        Defense = 0f,
        MoveSpeedMultiplier = 1f,
        DamageMultiplier = 1f,
        PickupRange = 1f,
    };

    [Header("Loadout")]
    [SerializeField]
    private CharacterLoadoutData _loadout = new CharacterLoadoutData
    {
        StartingWeaponId = string.Empty,
        UniqueTraitId = string.Empty,
        TraitDescription = string.Empty,
        PassiveIds = null,
    };

    [Header("Meta")]
    [SerializeField]
    private CharacterMetaData _meta = new CharacterMetaData
    {
        IsDefaultUnlocked = true,
        UnlockCost = 0,
        Rarity = CharacterRarity.Common,
        Tags = null,
    };

    public string CharacterId => _characterId;
    public string DisplayName => _displayName;
    public string Description => _description;
    public Sprite Icon => _icon;
    public Sprite Portrait => _portrait;
    public GameObject PreviewPrefab => _previewPrefab;
    public GameObject VisualPrefab => _visualPrefab;

    public CharacterCombatStats Combat => _combat;
    public CharacterLoadoutData Loadout => _loadout;
    public CharacterMetaData Meta => _meta;

    private void OnValidate()
    {
        _characterId = string.IsNullOrWhiteSpace(_characterId) ? "character.default" : _characterId.Trim();
        _displayName = string.IsNullOrWhiteSpace(_displayName) ? "Default Survivor" : _displayName.Trim();
        _description = _description?.Trim() ?? string.Empty;

        _combat.MaxHealth = Mathf.Max(1f, _combat.MaxHealth);
        _combat.Defense = Mathf.Max(0f, _combat.Defense);
        _combat.MoveSpeedMultiplier = Mathf.Max(0f, _combat.MoveSpeedMultiplier);
        _combat.DamageMultiplier = Mathf.Max(0f, _combat.DamageMultiplier);
        _combat.PickupRange = Mathf.Max(0f, _combat.PickupRange);

        _loadout.StartingWeaponId = _loadout.StartingWeaponId?.Trim() ?? string.Empty;
        _loadout.UniqueTraitId = _loadout.UniqueTraitId?.Trim() ?? string.Empty;
        _loadout.TraitDescription = _loadout.TraitDescription?.Trim() ?? string.Empty;
        _loadout.PassiveIds ??= new List<string>();

        for (int i = _loadout.PassiveIds.Count - 1; i >= 0; i--)
        {
            string passiveId = _loadout.PassiveIds[i]?.Trim();
            if (string.IsNullOrWhiteSpace(passiveId))
            {
                _loadout.PassiveIds.RemoveAt(i);
                continue;
            }

            _loadout.PassiveIds[i] = passiveId;
        }

        _meta.UnlockCost = Mathf.Max(0, _meta.UnlockCost);
        _meta.Tags ??= new List<string>();

        for (int i = _meta.Tags.Count - 1; i >= 0; i--)
        {
            string tag = _meta.Tags[i]?.Trim();
            if (string.IsNullOrWhiteSpace(tag))
            {
                _meta.Tags.RemoveAt(i);
                continue;
            }

            _meta.Tags[i] = tag;
        }
    }
}
