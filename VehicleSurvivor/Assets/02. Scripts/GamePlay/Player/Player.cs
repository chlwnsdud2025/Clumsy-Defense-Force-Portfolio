using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

[DisallowMultipleComponent]
public class Player : MonoBehaviour
{
    [Tooltip("캐릭터 비주얼 프리팹이 붙는 부모")]
    [SerializeField] private Transform visualRoot;
    [Tooltip("캐릭터 애니메이터")]
    [SerializeField] private Animator animator;

    [Header("Level")]
    public int level = 1;
    public int currentExp = 0;
    public int needExp = 10;

    public float currentHP;

    public bool isDead;

    private GameObject spawnedVisual;
    private CancellationTokenSource applyCharacterCts;

    public float MaxHp => 0;


    private void Awake()
    {
        if (animator == null && visualRoot != null)
            animator = visualRoot.GetComponentInParent<Animator>();
    }

    private void OnDestroy()
    {
        CancelPendingApplyCharacter();
        spawnedVisual = null;
    }

    public async UniTask InitializeFromCharacterAsync(CharacterDefinitionSO character)
    {
        ApplyStatsFromCharacter(character);
        await ApplyVisualPrefabFromCharacter(character);
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHP -= damage;
        currentHP = Mathf.Clamp(currentHP, 0f, MaxHp);

        if (currentHP <= 0f)
        {
            Die();
            return;
        }

        PublishHpIfPossible();
    }

    public void Heal(float amount)
    {
        if (isDead) return;

        currentHP += amount;
        currentHP = Mathf.Clamp(currentHP, 0f, MaxHp);
        PublishHpIfPossible();
    }

    public void AddExp(int expAmount)
    {
        if (isDead) return;

        currentExp += expAmount;

        while (currentExp >= needExp)
        {
            currentExp -= needExp;
            LevelUp();
        }
    }

    //public float GetFinalDamage(float weaponDamage)
    //{
    //    return weaponDamage * snapshot.DamageMultiplier;
    //}

    //public float GetFinalCooldown(float weaponCooldown)
    //{
    //    float mult = snapshot.AttackSpeedMultiplier;
    //    if (mult <= 0f)
    //        return weaponCooldown;

    //    return weaponCooldown / mult;
    //}

    //public int GetFinalProjectileCount(int weaponProjectileCount)
    //{
    //    return weaponProjectileCount + snapshot.BonusProjectileCount;
    //}

    private async UniTask ApplyVisualPrefabFromCharacter(CharacterDefinitionSO character)
    {
        CancelPendingApplyCharacter();
        applyCharacterCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
        CancellationToken cancellationToken = applyCharacterCts.Token;

        try
        {
            if (visualRoot == null)
            {
                if (character != null && character.VisualPrefab != null)
                    Debug.LogWarning($"[{nameof(Player)}] VisualRoot를 찾거나 할당하지 못해 '{character.DisplayName}' 비주얼을 붙일 수 없습니다.");
                return;
            }

            if (character == null || character.VisualPrefab == null)
            {
                if (character != null)
                    Debug.LogWarning($"[{nameof(Player)}] '{character.DisplayName}'의 VisualPrefab이 비어 있습니다. 스탯만 적용했습니다.");
                return;
            }

            ClearVisual(visualRoot);
            await UniTask.NextFrame(cancellationToken: cancellationToken);

            GameObject instance = Instantiate(character.VisualPrefab, visualRoot);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            await UniTask.NextFrame(cancellationToken: cancellationToken);

            // 애니메이션 재바인딩
            if (animator == null && visualRoot != null)
                animator = visualRoot.GetComponentInParent<Animator>();

            if (animator != null)
            {
                animator.Rebind();
                animator.Update(0f);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        finally
        {
            if (applyCharacterCts != null && applyCharacterCts.Token == cancellationToken)
            {
                applyCharacterCts.Dispose();
                applyCharacterCts = null;
            }
        }
    }

    private void ApplyStatsFromCharacter(CharacterDefinitionSO character)
    {
        //snapshot = character != null ? character.Stats : PlayerStatSheet.CreateBaselineDefaults();
        //currentHP = snapshot.MaxHealth;
        //isDead = false;
        //PublishHpIfPossible();
    }

    private void ClearVisual(Transform parent)
    {
        if (spawnedVisual != null)
        {
            Destroy(spawnedVisual);
            spawnedVisual = null;
        }

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            Destroy(child.gameObject);
        }
    }

    private void LevelUp()
    {
        level++;

        needExp += 5;

        Debug.Log($"레벨업! 현재 레벨: {level}");
    }

    private void Die()
    {
        isDead = true;
        Debug.Log("플레이어 사망");
        PublishHpIfPossible();
    }

    private void PublishHpIfPossible()
    {
        if (!ServiceLocator.TryGet(out IEventBus eventBus))
            return;

        int cur = Mathf.RoundToInt(currentHP);
        int max = Mathf.RoundToInt(MaxHp);
        eventBus.Publish(new HpChangedEvent(cur, max));
    }

    private void CancelPendingApplyCharacter()
    {
        if (applyCharacterCts == null)
            return;

        applyCharacterCts.Cancel();
        applyCharacterCts.Dispose();
        applyCharacterCts = null;
    }
}
