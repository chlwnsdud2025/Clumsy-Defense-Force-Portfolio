using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerVisual : MonoBehaviour
{
    [Tooltip("선택된 캐릭터 비주얼 프리팹이 붙을 부모 트랜스폼")]
    [SerializeField] private Transform visualRoot;

    [Tooltip("캐릭터 비주얼에 사용되는 Animator")]
    [SerializeField] private Animator animator;

    [Tooltip("피격 시 플래시 효과")]
    [SerializeField] private HitFlashEffect hitFlash;

    private GameObject spawnedVisual;
    private CancellationTokenSource applyCharacterCts;

    private void Awake()
    {
        if (animator == null && visualRoot != null)
            animator = visualRoot.GetComponentInParent<Animator>();

        if (hitFlash == null)
            hitFlash = gameObject.GetOrAddComponent<HitFlashEffect>();
    }

    private void OnDestroy()
    {
        CancelPendingApplyCharacter();
        spawnedVisual = null;
    }

    public async UniTask InitializeFromCharacterAsync(CharacterDefinitionSO character)
    {
        await ApplyVisualPrefabFromCharacter(character);
    }

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
                    Debug.LogWarning($"[{nameof(PlayerVisual)}] VisualRoot is not assigned, so '{character.DisplayName}' visual cannot be attached.", this);
                return;
            }

            if (character == null || character.VisualPrefab == null)
            {
                if (character != null)
                    Debug.LogWarning($"[{nameof(PlayerVisual)}] '{character.DisplayName}' has no VisualPrefab.", this);
                return;
            }

            ClearVisual(visualRoot);
            await UniTask.NextFrame(cancellationToken: cancellationToken);

            spawnedVisual = Instantiate(character.VisualPrefab, visualRoot);
            spawnedVisual.transform.localPosition = Vector3.zero;
            spawnedVisual.transform.localRotation = Quaternion.identity;
            spawnedVisual.transform.localScale = Vector3.one;

            await UniTask.NextFrame(cancellationToken: cancellationToken);

            if (animator != null)
            {
                animator.Rebind();
                animator.Update(0f);
            }

            if (hitFlash != null)
                hitFlash.RefreshRenderersFrom(spawnedVisual.transform);
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

    private void CancelPendingApplyCharacter()
    {
        if (applyCharacterCts == null)
            return;

        applyCharacterCts.Cancel();
        applyCharacterCts.Dispose();
        applyCharacterCts = null;
    }
}
