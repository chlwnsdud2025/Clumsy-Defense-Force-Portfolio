using System;
using System.Collections.Generic;

public enum EInventoryCategory
{
    Weapon,
    Passive,
    Item
}

public sealed class PlayerInventory
{
    public const int DefaultMaxWeaponSlots = 6;
    public const int DefaultMaxPassiveSlots = 6;
    public const int DefaultMaxItemSlots = 20;

    private readonly PlayerRuntime owner;

    private readonly List<WeaponInstance> weapons = new();
    private readonly List<PassiveInstance> passives = new();
    private readonly List<ItemInstance> items = new();

    public IReadOnlyList<WeaponInstance> Weapons => weapons;
    public IReadOnlyList<PassiveInstance> Passives => passives;
    public IReadOnlyList<ItemInstance> Items => items;
    public int MaxWeaponSlots { get; }
    public int MaxPassiveSlots { get; }
    public int MaxItemSlots { get; }

    public event Action<EInventoryCategory, int> Changed;

    public PlayerInventory(PlayerRuntime owner, int maxWeaponSlots = DefaultMaxWeaponSlots, int maxPassiveSlots = DefaultMaxPassiveSlots, int maxItemSlots = DefaultMaxItemSlots)
    {
        this.owner = owner;
        MaxWeaponSlots = maxWeaponSlots;
        MaxPassiveSlots = maxPassiveSlots;
        MaxItemSlots = maxItemSlots;
    }

    public bool CanAddWeapon()
    {
        return weapons.Count < MaxWeaponSlots;
    }

    public bool CanAddPassive()
    {
        return passives.Count < MaxPassiveSlots;
    }

    public bool CanAddItme()
    {
        return items.Count < MaxItemSlots;
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

    public ItemInstance GetItem(ItemDefinitionSO definition)
    {
        if (definition == null)
            return null;

        return items.Find(existing => existing.Definition.ItemId == definition.ItemId);
    }

    public WeaponInstance AddOrLevelUpWeapon(WeaponDefinitionSO definition)
    {
        if (definition == null)
            return null;

        WeaponInstance weapon = GetWeapon(definition);

        if (weapon != null)
        {
            weapon.TryLevelUp();
            NotifyChanged(EInventoryCategory.Weapon, weapons.IndexOf(weapon));
            return weapon;
        }

        if (!CanAddWeapon())
            return null;

        weapon = new WeaponInstance(definition);
        weapons.Add(weapon);
        NotifyChanged(EInventoryCategory.Weapon, weapons.Count - 1);
        return weapon;
    }

    public void AddOrLevelUpPassive(PassiveDefinitionSO definition)
    {
        if (definition == null)
            return;

        PassiveInstance passive = GetPassive(definition);

        if (passive != null)
        {
            passive.TryLevelUp();
            NotifyChanged(EInventoryCategory.Passive, passives.IndexOf(passive));
            return;
        }

        if (!CanAddPassive())
            return;

        passives.Add(new PassiveInstance(definition, owner));
        NotifyChanged(EInventoryCategory.Passive, passives.Count - 1);
    }

    public void OnlyAddItem(ItemDefinitionSO definition)
    {
        if (definition == null)
            return;

        ItemInstance item = GetItem(definition);

        if (item != null)
        {
            item.AddStack();
            NotifyChanged(EInventoryCategory.Item, items.IndexOf(item));
            return;
        }

        items.Add(new ItemInstance(definition));
        NotifyChanged(EInventoryCategory.Item, items.Count - 1);
    }

    private void NotifyChanged(EInventoryCategory category, int slotIndex)
    {
        Changed?.Invoke(category, slotIndex);
    }
}
