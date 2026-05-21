using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerVisual : MonoBehaviour
{
    [Tooltip("Parent transform for the selected character visual prefab.")]
    [SerializeField] private Transform visualRoot;

    [Tooltip("Animator used by the character visual.")]
    [SerializeField] private Animator animator;

    private GameObject spawnedVisual;
    private CancellationTokenSource applyCharacterCts;

    private void Awake()
    {
        ResolveAnimator();
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

            ResolveAnimator();
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

    private void ResolveAnimator()
    {
        if (animator == null && visualRoot != null)
            animator = visualRoot.GetComponentInParent<Animator>();
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
