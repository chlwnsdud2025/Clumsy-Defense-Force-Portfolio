using System.Collections.Generic;

public sealed class PlayerInventory
{
    private readonly PlayerRuntime owner;

    private readonly List<WeaponInstance> weapons = new();
    private readonly List<PassiveInstance> passives = new();
    private readonly List<ItemInstance> items = new();

    public IReadOnlyList<WeaponInstance> Weapons => weapons;
    public IReadOnlyList<PassiveInstance> Passives => passives;
    public IReadOnlyList<ItemInstance> Items => items;

    public PlayerInventory(PlayerRuntime owner)
    {
        this.owner = owner;
    }

    public void AddOrLevelUpPassive(PassiveDefinitionSO definition)
    {
        if (definition == null)
        {
            return;
        }

        PassiveInstance passive = passives.Find(existing => existing.Definition.PassiveId == definition.PassiveId);

        if (passive != null)
        {
            passive.TryLevelUp();
            return;
        }

        passives.Add(new PassiveInstance(definition));
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

    public void AddOrLevelUpWeapon(WeaponDefinitionSO definition)
    {
        if (definition == null)
        {
            return;
        }

        WeaponInstance weapon = weapons.Find(existing => existing.Definition.WeaponId == definition.WeaponId);

        if (weapon != null)
        {
            weapon.TryLevelUp();
            return;
        }

        weapons.Add(new WeaponInstance(definition));
    }
}
