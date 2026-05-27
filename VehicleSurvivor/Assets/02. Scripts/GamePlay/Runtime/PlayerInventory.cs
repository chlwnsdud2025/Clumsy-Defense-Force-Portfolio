using System;
using System.Collections.Generic;

public enum EInventoryCategory
{
    Weapon,
    Badge,
    Item
}

public sealed class PlayerInventory
{
    public const int DefaultMaxWeaponSlots = 6;
    public const int DefaultMaxBadgeSlots = 6;
    public const int DefaultMaxItemSlots = 20;

    private readonly PlayerRuntime owner;

    private readonly List<WeaponInstance> weapons = new();
    private readonly List<BadgeInstance> badges = new();
    private readonly List<ItemInstance> items = new();

    public IReadOnlyList<WeaponInstance> Weapons => weapons;
    public IReadOnlyList<BadgeInstance> Badges => badges;
    public IReadOnlyList<ItemInstance> Items => items;
    public int MaxWeaponSlots { get; }
    public int MaxBadgeSlots { get; }
    public int MaxItemSlots { get; }

    public event Action<EInventoryCategory, int> Changed;

    public PlayerInventory(PlayerRuntime owner, int maxWeaponSlots = DefaultMaxWeaponSlots, int maxBadgeSlots = DefaultMaxBadgeSlots, int maxItemSlots = DefaultMaxItemSlots)
    {
        this.owner = owner;
        MaxWeaponSlots = maxWeaponSlots;
        MaxBadgeSlots = maxBadgeSlots;
        MaxItemSlots = maxItemSlots;
    }

    public bool CanAddWeapon()
    {
        return weapons.Count < MaxWeaponSlots;
    }

    public bool CanAddBadge()
    {
        return badges.Count < MaxBadgeSlots;
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

    public BadgeInstance GetBadge(BadgeDefinitionSO definition)
    {
        if (definition == null)
            return null;

        return badges.Find(existing => existing.Definition.BadgeId == definition.BadgeId);
    }

    public bool HasBadge(string badgeId)
    {
        if (string.IsNullOrWhiteSpace(badgeId))
            return false;

        string normalizedId = badgeId.Trim();

        foreach (BadgeInstance badge in badges)
        {
            if (badge?.Definition != null && badge.Definition.BadgeId == normalizedId)
                return true;
        }

        return false;
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

    public WeaponInstance ReplaceWeapon(WeaponInstance oldWeapon, WeaponDefinitionSO newDefinition)
    {
        if (oldWeapon == null || newDefinition == null)
            return null;

        int index = weapons.IndexOf(oldWeapon);
        if (index < 0)
            return null;

        WeaponInstance newWeapon = new(newDefinition);
        weapons[index] = newWeapon;
        NotifyChanged(EInventoryCategory.Weapon, index);
        return newWeapon;
    }

    public void AddOrLevelUpBadge(BadgeDefinitionSO definition)
    {
        if (definition == null)
            return;

        BadgeInstance badge = GetBadge(definition);

        if (badge != null)
        {
            badge.TryLevelUp();
            NotifyChanged(EInventoryCategory.Badge, badges.IndexOf(badge));
            return;
        }

        if (!CanAddBadge())
            return;

        badges.Add(new BadgeInstance(definition, owner));
        NotifyChanged(EInventoryCategory.Badge, badges.Count - 1);
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
