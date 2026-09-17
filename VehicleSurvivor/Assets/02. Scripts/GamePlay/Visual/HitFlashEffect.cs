using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class HitFlashEffect : MonoBehaviour
{
    [Header("Hit Flash Settings")]
    public Renderer[] modelRenderers;
    public float flashDuration = 0.1f;
    public float flashCooldown = 0.1f;
    public string colorPropertyName = "_BaseColor";
    public Color flashColor = Color.white;
    public float flashIntensity = 5f;
    [Range(0f, 1f)] public float flashAlpha = 0.5f;

    private Color[] originalColors;
    private Color[] precalculatedFlashColors;
    private int colorPropertyID;
    private MaterialPropertyBlock propBlock;
    private float flashTimer;
    private float cooldownUntil;
    private bool isInitialized;
    private bool scheduled;
    private static readonly List<HitFlashEffect> activeFlashes = new List<HitFlashEffect>();
    private static HitFlashUpdater updater;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetScheduler()
    {
        activeFlashes.Clear();
        updater = null;
    }

    private void Awake()
    {
        RebuildCache();
    }

    public void RefreshRenderersFrom(Transform root)
    {
        modelRenderers = root != null
            ? root.GetComponentsInChildren<Renderer>(false)
            : System.Array.Empty<Renderer>();

        RebuildCache();
    }

    public void RebuildCache()
    {
        InitializePropertyBlock();
        CacheColors();
    }

    public void ResetFlash()
    {
        flashTimer = 0f;
        cooldownUntil = 0f;
        if (scheduled) { activeFlashes.Remove(this); scheduled = false; }

        if (isInitialized)
            RestoreOriginalColors();
    }

    public void TriggerHitFlash()
    {
        if (!isInitialized || !isActiveAndEnabled)
            return;

        if (Time.time < cooldownUntil)
            return;

        flashTimer = flashDuration;
        cooldownUntil = Time.time + flashDuration + flashCooldown;
        if (!scheduled)
        {
            scheduled = true;
            activeFlashes.Add(this);
        }
        if (updater == null)
        {
            GameObject runner = new GameObject("HitFlashUpdater");
            DontDestroyOnLoad(runner);
            updater = runner.AddComponent<HitFlashUpdater>();
        }
    }

    private void OnDisable() => ResetFlash();

    internal static void UpdateActiveFlashes(float deltaTime)
    {
        for (int i = activeFlashes.Count - 1; i >= 0; i--)
        {
            HitFlashEffect effect = activeFlashes[i];
            if (effect == null) { activeFlashes.RemoveAt(i); continue; }
            effect.flashTimer -= deltaTime;
            if (effect.flashTimer > 0f) effect.ApplyFlashColor(effect.precalculatedFlashColors);
            else
            {
                effect.RestoreOriginalColors();
                effect.scheduled = false;
                activeFlashes.RemoveAt(i);
            }
        }
    }

    private void InitializePropertyBlock()
    {
        if (propBlock == null)
            propBlock = new MaterialPropertyBlock();

        colorPropertyID = Shader.PropertyToID(colorPropertyName);
    }

    private void CacheColors()
    {
        isInitialized = false;

        if (modelRenderers == null || modelRenderers.Length == 0)
            return;

        originalColors = new Color[modelRenderers.Length];
        precalculatedFlashColors = new Color[modelRenderers.Length];

        for (int i = 0; i < modelRenderers.Length; i++)
        {
            Renderer renderer = modelRenderers[i];
            if (renderer == null || renderer.sharedMaterial == null)
                continue;

            if (renderer.sharedMaterial.HasProperty(colorPropertyID))
                originalColors[i] = renderer.sharedMaterial.GetColor(colorPropertyID);
            else
                originalColors[i] = Color.white;

            precalculatedFlashColors[i] = Color.Lerp(originalColors[i], flashColor * flashIntensity, flashAlpha);
        }

        isInitialized = true;
    }

    private void ApplyFlashColor(Color[] colorsToApply)
    {
        if (modelRenderers == null || colorsToApply == null)
            return;

        for (int i = 0; i < modelRenderers.Length; i++)
        {
            Renderer renderer = modelRenderers[i];
            if (renderer == null)
                continue;

            renderer.GetPropertyBlock(propBlock);
            propBlock.SetColor(colorPropertyID, colorsToApply[i]);
            renderer.SetPropertyBlock(propBlock);
        }
    }

    private void RestoreOriginalColors()
    {
        if (modelRenderers == null || originalColors == null)
            return;

        for (int i = 0; i < modelRenderers.Length; i++)
        {
            Renderer renderer = modelRenderers[i];
            if (renderer == null)
                continue;

            renderer.GetPropertyBlock(propBlock);
            propBlock.SetColor(colorPropertyID, originalColors[i]);
            renderer.SetPropertyBlock(propBlock);
        }
    }
}
