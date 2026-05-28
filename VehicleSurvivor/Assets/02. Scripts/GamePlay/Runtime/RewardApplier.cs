using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class RewardApplier
{
    private readonly WeaponEvolutionService weaponEvolutionService = new();

    public bool Apply(
        PlayerRuntime player,
        RewardOption reward,
        WeaponDatabaseSO weaponDatabase,
        PlayerWeaponSpawner playerWeaponSpawner,
        Transform owner)
    {
        if (player == null || reward == null)
            return false;

        switch (reward.Type)
        {
            case ERewardType.Weapon:
                return ApplyWeapon(player, reward.Weapon, weaponDatabase, playerWeaponSpawner, owner);
            case ERewardType.WeaponEvolution:
                return ApplyWeaponEvolution(player, reward.SourceWeaponId, weaponDatabase, playerWeaponSpawner, owner);
            case ERewardType.Badge:
                return ApplyBadge(player, reward.Badge, weaponDatabase, playerWeaponSpawner, owner);
            case ERewardType.Item:
                return ApplyItem(player, reward.Item);
            case ERewardType.Heal:
                player.Health.Heal(reward.HealAmount);
                return true;
            default:
                return false;
        }
    }

    public bool ApplyStartingWeapon(
        PlayerRuntime player,
        WeaponDefinitionSO definition,
        PlayerWeaponSpawner playerWeaponSpawner,
        Transform owner) =>
        ApplyWeapon(player, definition, null, playerWeaponSpawner, owner);

    public bool TryEvolveAllEligibleWeapons(
        PlayerRuntime player,
        WeaponDatabaseSO weaponDatabase,
        PlayerWeaponSpawner playerWeaponSpawner,
        Transform owner)
    {
        return weaponEvolutionService.TryEvolveAllEligibleWeapons(
            player,
            weaponDatabase,
            playerWeaponSpawner,
            owner);
    }

    private bool ApplyWeapon(
        PlayerRuntime player,
        WeaponDefinitionSO definition,
        WeaponDatabaseSO weaponDatabase,
        PlayerWeaponSpawner playerWeaponSpawner,
        Transform owner)
    {
        if (definition == null)
            return false;

        bool alreadySpawned = playerWeaponSpawner != null && playerWeaponSpawner.HasSpawnedWeapon(definition.WeaponId);
        WeaponInstance weapon = player.Inventory.AddOrLevelUpWeapon(definition);
        if (weapon == null)
            return false;

        if (!alreadySpawned && playerWeaponSpawner != null)
            playerWeaponSpawner.TrySpawnWeapon(weapon, owner);

        return true;
    }

    private bool ApplyBadge(
        PlayerRuntime player,
        BadgeDefinitionSO definition,
        WeaponDatabaseSO weaponDatabase,
        PlayerWeaponSpawner playerWeaponSpawner,
        Transform owner)
    {
        if (definition == null)
            return false;

        player.Inventory.AddOrLevelUpBadge(definition);
        return true;
    }

    private bool ApplyWeaponEvolution(
        PlayerRuntime player,
        string sourceWeaponId,
        WeaponDatabaseSO weaponDatabase,
        PlayerWeaponSpawner playerWeaponSpawner,
        Transform owner)
    {
        return weaponEvolutionService.TryEvolveWeapon(
            player,
            sourceWeaponId,
            weaponDatabase,
            playerWeaponSpawner,
            owner);
    }
    private static bool ApplyItem(PlayerRuntime player, ItemDefinitionSO definition)
    {
        if(definition == null)
        {
            return false;
        }
        if (!player.Inventory.OnlyAddItem(definition))
        {
            return false;
        }

        ItemSpecialEffectService.ApplyOnItemCollected(player, definition);
        return true;
    }
}

public static class ItemSpecialEffectService
{
    private const int PajamaInvitationTreasureChestCount = 3;
    private const float GoldStarStampChance = 0.25f;
    private const float GoldStarStampDuration = 5f;
    private const float GoldStarDamageTakenMultiplier = 1.2f;
    private const float GoldStarMoveSpeedMultiplier = 0.7f;

    public static void ApplyOnItemCollected(PlayerRuntime player, ItemDefinitionSO item)
    {
        if (player == null || item == null || !item.HasSpecialEffect)
            return;

        switch (item.SpecialEffectType)
        {
            case EItemSpecialEffectType.PajamaInvitation:
                player.ItemEffects.RequestTreasureChestSpawn(PajamaInvitationTreasureChestCount);
                if (!player.ItemEffects.HasTreasureChestSpawnListener)
                    Debug.LogWarning("[ItemSpecialEffectService] Pajama invitation has no treasure chest spawn listener yet.");
                break;

            case EItemSpecialEffectType.GoldStarStamp:
                player.ItemEffects.EnableGoldStarStamp();
                break;
        }
    }

    public static DamageInfo ApplyOnEnemyHit(Component target, DamageInfo damageInfo)
    {
        if (target == null)
            return damageInfo;

        GoldStarStampEffect stamp = target.GetComponent<GoldStarStampEffect>();
        PlayerRuntime sourcePlayer = damageInfo.SourcePlayer;

        if (sourcePlayer?.ItemEffects.HasGoldStarStamp == true &&
            Random.value < GoldStarStampChance)
        {
            stamp = target.gameObject.GetOrAddComponent<GoldStarStampEffect>();
            stamp.Apply(
                GoldStarStampDuration,
                GoldStarDamageTakenMultiplier,
                GoldStarMoveSpeedMultiplier);
        }

        if (stamp != null && stamp.IsActive)
            return damageInfo.WithAmount(damageInfo.Amount * stamp.DamageTakenMultiplier);

        return damageInfo;
    }
}

public sealed class GoldStarStampEffect : MonoBehaviour
{
    private Enemy enemy;
    private BossEnemy boss;
    private Coroutine expireRoutine;
    private GameObject visualInstance;
    private float cachedMoveSpeed;
    private bool hasCachedMoveSpeed;

    public bool IsActive { get; private set; }
    public float DamageTakenMultiplier { get; private set; } = 1f;

    public void Apply(float duration, float damageTakenMultiplier, float moveSpeedMultiplier)
    {
        CacheMoveSpeed();
        IsActive = true;
        DamageTakenMultiplier = Mathf.Max(1f, damageTakenMultiplier);
        SetMoveSpeed(cachedMoveSpeed * Mathf.Max(0f, moveSpeedMultiplier));
        ShowVisual();
        PlayStampSfx();

        if (expireRoutine != null)
            StopCoroutine(expireRoutine);

        expireRoutine = StartCoroutine(ExpireAfter(duration));
    }

    private void CacheMoveSpeed()
    {
        if (hasCachedMoveSpeed)
            return;

        enemy = GetComponent<Enemy>();
        boss = GetComponent<BossEnemy>();

        if (enemy != null)
            cachedMoveSpeed = enemy.moveSpeed;
        else if (boss != null)
            cachedMoveSpeed = boss.moveSpeed;
        else
            cachedMoveSpeed = 0f;

        hasCachedMoveSpeed = true;
    }

    private IEnumerator ExpireAfter(float duration)
    {
        yield return new WaitForSeconds(Mathf.Max(0f, duration));
        Clear();
    }

    private void Clear()
    {
        if (!IsActive)
            return;

        IsActive = false;
        DamageTakenMultiplier = 1f;
        ClearVisual();
        RestoreMoveSpeed();
        expireRoutine = null;
    }

    private void ShowVisual()
    {
        if (visualInstance != null)
            return;

        ItemSpecialEffectManager manager = ItemSpecialEffectManager.GetOrCreate();
        visualInstance = manager.CreateGoldStarVisual(transform);
    }

    private void ClearVisual()
    {
        if (visualInstance == null)
            return;

        Destroy(visualInstance);
        visualInstance = null;
    }

    private void PlayStampSfx()
    {
        ItemSpecialEffectManager manager = ItemSpecialEffectManager.GetOrCreate();
        manager.PlayGoldStarStampSfx();
    }

    private void RestoreMoveSpeed()
    {
        if (!hasCachedMoveSpeed)
            return;

        SetMoveSpeed(cachedMoveSpeed);
    }

    private void SetMoveSpeed(float value)
    {
        if (enemy != null)
            enemy.moveSpeed = value;
        else if (boss != null)
            boss.moveSpeed = value;
    }

    private void OnDisable()
    {
        ClearVisual();
        RestoreMoveSpeed();
        IsActive = false;
        DamageTakenMultiplier = 1f;
        expireRoutine = null;
    }
}

public sealed class WeaponEvolutionService
{
    public bool CanEvolveWeapon(PlayerRuntime player, WeaponInstance weapon)
    {
        if (player == null || weapon?.Definition == null)
            return false;

        WeaponDefinitionSO definition = weapon.Definition;
        if (string.IsNullOrWhiteSpace(definition.EvolutionWeaponId))
            return false;

        if (string.IsNullOrWhiteSpace(definition.RequiredBadgeId))
            return false;

        if (definition.MaxLevel <= 0)
            return false;

        if (!weapon.IsMaxLevel)
            return false;

        return HasMaxLevelRequiredBadge(player, definition.RequiredBadgeId);
    }

    public bool TryEvolveWeapon(
        PlayerRuntime player,
        string sourceWeaponId,
        WeaponDatabaseSO weaponDatabase,
        PlayerWeaponSpawner spawner,
        Transform owner)
    {
        if (player == null || string.IsNullOrWhiteSpace(sourceWeaponId))
            return false;

        string normalizedId = sourceWeaponId.Trim();

        foreach (WeaponInstance weapon in player.Inventory.Weapons)
        {
            if (weapon?.Definition != null && weapon.Definition.WeaponId == normalizedId)
                return TryEvolveWeapon(player, weapon, weaponDatabase, spawner, owner);
        }

        return false;
    }

    public bool TryEvolveWeapon(
        PlayerRuntime player,
        WeaponInstance weapon,
        WeaponDatabaseSO weaponDatabase,
        PlayerWeaponSpawner spawner,
        Transform owner)
    {
        if (!CanEvolveWeapon(player, weapon))
            return false;

        if (weaponDatabase == null)
        {
            Debug.LogWarning($"[WeaponEvolutionService] Cannot evolve {weapon.Definition.WeaponId}: WeaponDatabaseSO is missing.");
            return false;
        }

        string oldWeaponId = weapon.Definition.WeaponId;
        string evolutionWeaponId = weapon.Definition.EvolutionWeaponId;
        WeaponDefinitionSO evolvedDefinition = weaponDatabase.GetWeaponOrNull(evolutionWeaponId);

        if (evolvedDefinition == null)
        {
            Debug.LogWarning($"[WeaponEvolutionService] Cannot evolve {oldWeaponId}: evolution weapon not found ({evolutionWeaponId}).");
            return false;
        }

        if (spawner != null)
        {
            if (spawner.HasSpawnedWeapon(evolvedDefinition.WeaponId))
            {
                Debug.LogWarning($"[WeaponEvolutionService] Cannot evolve {oldWeaponId}: evolved weapon is already spawned ({evolvedDefinition.WeaponId}).");
                return false;
            }

            if (!spawner.CanSpawnWeapon(evolvedDefinition))
            {
                Debug.LogWarning($"[WeaponEvolutionService] Evolution weapon prefab is not spawnable: {evolvedDefinition.WeaponId}");
                return false;
            }
        }

        WeaponInstance evolvedWeapon = player.Inventory.ReplaceWeapon(weapon, evolvedDefinition);
        if (evolvedWeapon == null)
        {
            Debug.LogWarning($"[WeaponEvolutionService] Cannot evolve {oldWeaponId}: inventory replacement failed.");
            return false;
        }

        if (spawner == null)
        {
            Debug.Log($"[WeaponEvolutionService] Evolved weapon in inventory only: {oldWeaponId} -> {evolutionWeaponId}");
            return true;
        }

        if (!spawner.ReplaceSpawnedWeapon(oldWeaponId, evolvedWeapon, owner))
        {
            Debug.LogWarning($"[WeaponEvolutionService] Failed to replace spawned weapon: {oldWeaponId} -> {evolutionWeaponId}");
            return false;
        }

        Debug.Log($"[WeaponEvolutionService] Weapon evolved: {weapon.Definition.WeaponName} ({oldWeaponId}) -> {evolvedDefinition.WeaponName} ({evolutionWeaponId})");
        return true;
    }

    public bool TryEvolveAllEligibleWeapons(
        PlayerRuntime player,
        WeaponDatabaseSO weaponDatabase,
        PlayerWeaponSpawner spawner,
        Transform owner)
    {
        if (player == null)
            return false;

        List<WeaponInstance> weapons = new(player.Inventory.Weapons);
        bool evolvedAny = false;

        foreach (WeaponInstance weapon in weapons)
        {
            if (TryEvolveWeapon(player, weapon, weaponDatabase, spawner, owner))
                evolvedAny = true;
        }

        return evolvedAny;
    }

    private static bool HasMaxLevelRequiredBadge(PlayerRuntime player, string requiredBadgeId)
    {
        if (player == null || string.IsNullOrWhiteSpace(requiredBadgeId))
            return false;

        string normalizedId = requiredBadgeId.Trim();

        foreach (BadgeInstance badge in player.Inventory.Badges)
        {
            if (badge?.Definition != null &&
                badge.Definition.BadgeId == normalizedId &&
                badge.IsMaxLevel)
            {
                return true;
            }
        }

        return false;
    }
}
