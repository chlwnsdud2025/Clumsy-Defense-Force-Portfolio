using UnityEngine;

public sealed class ItemSpecialEffectManager : MonoSceneSingleton<ItemSpecialEffectManager>
{
    private const string EditorTreasureChestPrefabPath = "Assets/03. Prefabs/Map/TresureBox.prefab";

    [Header("Gold Star Stamp")]
    [SerializeField] private GameObject goldStarStampVfxPrefab;
    [SerializeField] private Vector3 goldStarVfxOffset = new(0f, 2f, 0f);
    [SerializeField] private float fallbackGoldStarScale = 0.35f;

    [Header("Pajama Invitation")]
    [SerializeField] private GameObject treasureChestPrefab;
    [SerializeField] private float treasureSpawnRadius = 6f;
    [SerializeField] private float treasureSpawnMinDistance = 2f;
    [SerializeField] private float groundRayHeight = 20f;
    [SerializeField] private float groundRayDistance = 60f;
    [SerializeField] private LayerMask groundLayer = ~0;
    [SerializeField] private Transform spawnedChestParent;

    private PlayerRuntime playerRuntime;
    private Transform playerTransform;
    private Material fallbackGoldStarMaterial;

    public static ItemSpecialEffectManager GetOrCreate()
    {
        if (TryGetInstance(out ItemSpecialEffectManager manager))
            return manager;

        GameObject managerObject = new GameObject(nameof(ItemSpecialEffectManager));
        return managerObject.AddComponent<ItemSpecialEffectManager>();
    }

    public void InitializeForRun(RunSession runSession, Player player)
    {
        ReleaseRun();

        playerRuntime = runSession?.Player;
        playerTransform = player != null ? player.FollowTransform : null;

        if (playerRuntime != null)
            playerRuntime.ItemEffects.TreasureChestSpawnRequested += HandleTreasureChestSpawnRequested;
    }

    public void ReleaseRun()
    {
        if (playerRuntime != null)
            playerRuntime.ItemEffects.TreasureChestSpawnRequested -= HandleTreasureChestSpawnRequested;

        playerRuntime = null;
        playerTransform = null;
    }

    public GameObject CreateGoldStarVisual(Transform target)
    {
        if (target == null)
            return null;

        GameObject visual = goldStarStampVfxPrefab != null
            ? Instantiate(goldStarStampVfxPrefab, target)
            : CreateFallbackGoldStarVisual(target);

        visual.transform.localPosition = goldStarVfxOffset;
        visual.transform.localRotation = Quaternion.identity;
        return visual;
    }

    public void PlayGoldStarStampSfx()
    {
        // Sound hook only. Connect SoundPlayback here when the SFX asset/id is ready.
    }

    private void HandleTreasureChestSpawnRequested(PlayerRuntime owner, int chestCount)
    {
        if (owner != playerRuntime)
            return;

        GameObject chestPrefab = ResolveTreasureChestPrefab();
        if (chestPrefab == null)
        {
            Debug.LogWarning("[ItemSpecialEffectManager] Treasure chest prefab is missing.");
            return;
        }

        for (int i = 0; i < chestCount; i++)
        {
            Vector3 spawnPosition = GetTreasureChestSpawnPosition(i, chestCount);
            Instantiate(chestPrefab, spawnPosition, chestPrefab.transform.rotation, spawnedChestParent);
        }
    }

    private Vector3 GetTreasureChestSpawnPosition(int index, int totalCount)
    {
        Vector3 origin = playerTransform != null ? playerTransform.position : transform.position;

        float angle = totalCount > 0 ? 360f * index / totalCount : Random.Range(0f, 360f);
        float radians = angle * Mathf.Deg2Rad;
        Vector3 direction = new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians));
        Vector3 candidate = origin + direction * treasureSpawnRadius;

        if (Vector3.Distance(origin, candidate) < treasureSpawnMinDistance)
            candidate = origin + direction * treasureSpawnMinDistance;

        Vector3 rayStart = candidate + Vector3.up * groundRayHeight;
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, groundRayDistance, groundLayer))
            return hit.point;

        return candidate;
    }

    private GameObject ResolveTreasureChestPrefab()
    {
        if (treasureChestPrefab != null)
            return treasureChestPrefab;

#if UNITY_EDITOR
        treasureChestPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(EditorTreasureChestPrefabPath);
#endif
        return treasureChestPrefab;
    }

    private GameObject CreateFallbackGoldStarVisual(Transform target)
    {
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.name = "GoldStarStampVisual";
        visual.transform.SetParent(target, false);
        visual.transform.localScale = Vector3.one * fallbackGoldStarScale;

        if (visual.TryGetComponent(out Collider collider))
            Destroy(collider);

        if (fallbackGoldStarMaterial == null)
        {
            Shader shader = Shader.Find("Standard");
            if (shader != null)
            {
                fallbackGoldStarMaterial = new Material(shader);
                fallbackGoldStarMaterial.color = new Color(1f, 0.82f, 0.05f);
            }
        }

        if (fallbackGoldStarMaterial != null && visual.TryGetComponent(out Renderer renderer))
            renderer.sharedMaterial = fallbackGoldStarMaterial;

        return visual;
    }
}
