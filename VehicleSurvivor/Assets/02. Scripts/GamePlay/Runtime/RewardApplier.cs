using UnityEngine;

public sealed class RewardApplier
{
    public bool Apply(PlayerRuntime player, RewardOption reward, PlayerWeaponSpawner playerWeaponSpawner, Transform owner)
    {
        if (player == null || reward == null)
            return false;

        switch (reward.Type)
        {
            case ERewardType.Weapon:
                return ApplyWeapon(player, reward.Weapon, playerWeaponSpawner, owner);
            case ERewardType.Passive:
                return ApplyPassive(player, reward.Passive);
            case ERewardType.Heal:
                player.Health.Heal(reward.HealAmount);
                return true;
            default:
                return false;
        }
    }

    private static bool ApplyWeapon(
        PlayerRuntime player,
        WeaponDefinitionSO definition,
        PlayerWeaponSpawner playerWeaponSpawner,
        Transform owner)
    {
        if (definition == null)
            return false;

        bool alreadySpawned = playerWeaponSpawner != null && playerWeaponSpawner.HasSpawnedWeapon(definition.WeaponId);
        WeaponInstance weapon = player.Inventory.AddOrLevelUpWeapon(definition);

        if (!alreadySpawned && playerWeaponSpawner != null)
            playerWeaponSpawner.TrySpawnWeapon(weapon, owner);

        return weapon != null;
    }

    private static bool ApplyPassive(PlayerRuntime player, PassiveDefinitionSO definition)
    {
        if (definition == null)
            return false;

        player.Inventory.AddOrLevelUpPassive(definition);
        return true;
    }
}
