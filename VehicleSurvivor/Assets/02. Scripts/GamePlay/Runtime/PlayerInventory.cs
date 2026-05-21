using System.Collections.Generic;

public sealed class PlayerInventory
{
    private const int DefaultMaxWeaponSlots = 6;
    private const int DefaultMaxPassiveSlots = 6;

    private readonly PlayerRuntime owner;

    private readonly List<WeaponInstance> weapons = new();
    private readonly List<PassiveInstance> passives = new();
    private readonly List<ItemInstance> items = new();

    public IReadOnlyList<WeaponInstance> Weapons => weapons;
    public IReadOnlyList<PassiveInstance> Passives => passives;
    public IReadOnlyList<ItemInstance> Items => items;
    public int MaxWeaponSlots { get; }
    public int MaxPassiveSlots { get; }

    public PlayerInventory(PlayerRuntime owner, int maxWeaponSlots = DefaultMaxWeaponSlots, int maxPassiveSlots = DefaultMaxPassiveSlots)
    {
        this.owner = owner;
        MaxWeaponSlots = maxWeaponSlots;
        MaxPassiveSlots = maxPassiveSlots;
    }

    public bool CanAddWeapon()
    {
        return weapons.Count < MaxWeaponSlots;
    }

    public bool CanAddPassive()
    {
        return passives.Count < MaxPassiveSlots;
    }

    public WeaponInstance GetWeapon(WeaponDefinitionSO definition)
    {
        if (definition == null)
            return null;

        return weapons.Find(existing => existing.Definition.WeaponId == definition.WeaponId);
    }

    public PassiveInstance GetPassive(PassiveDefinitionSO definition)
    {
        if (definition == null)
            return null;

        return passives.Find(existing => existing.Definition.PassiveId == definition.PassiveId);
    }

    public void AddOrLevelUpPassive(PassiveDefinitionSO definition)
    {
        if (definition == null)
        {
            return;
        }

        PassiveInstance passive = GetPassive(definition);

        if (passive != null)
        {
            passive.TryLevelUp();
            return;
        }

        if (!CanAddPassive())
        {
            return;
        }

        passives.Add(new PassiveInstance(definition, owner));
    }

    public void AddOrLevelUpItem(ItemDefinitionSO definition)
    {
        if (definition == null)
        {
            return;
        }

        ItemInstance item = items.Find(existing => existing.Definition.ItemId == definition.ItemId);

        if (item != null)
        {
            item.AddStack();
            return;
        }

        items.Add(new ItemInstance(definition));
    }

    public WeaponInstance AddOrLevelUpWeapon(WeaponDefinitionSO definition)
    {
        if (definition == null)
        {
            return null;
        }

        WeaponInstance weapon = GetWeapon(definition);

        if (weapon != null)
        {
            weapon.TryLevelUp();
            return weapon;
        }

        if (!CanAddWeapon())
        {
            return null;
        }

        weapon = new WeaponInstance(definition);
        weapons.Add(weapon);
        return weapon;
    }
}
