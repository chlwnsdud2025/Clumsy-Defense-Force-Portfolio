using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public sealed class PlayerOcclusionSilhouette : MonoBehaviour
{
    private const string DefaultSilhouetteMaterialResourcePath = "PlayerOcclusionSilhouette";
    private const string DefaultStencilMaterialResourcePath = "PlayerOcclusionStencil";

    [SerializeField] private Material silhouetteMaterial;
    [SerializeField] private Material stencilMaterial;
    [SerializeField] private Color silhouetteColor = new(0.25f, 0.9f, 1f, 0.55f);

    private readonly List<GameObject> clones = new();
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private MaterialPropertyBlock propertyBlock;

    public void RebuildFrom(Transform visualRoot)
    {
        Clear();

        Material silhouette = ResolveSilhouetteMaterial();
        Material stencil = ResolveStencilMaterial();
        if (visualRoot == null || silhouette == null || stencil == null)
            return;

        propertyBlock ??= new MaterialPropertyBlock();
        propertyBlock.SetColor(BaseColorId, silhouetteColor);

        foreach (SkinnedMeshRenderer source in visualRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (!IsValidSource(source))
                continue;

            CreateSkinnedClone(source, stencil, $"{source.name}_OcclusionStencil", null);
            CreateSkinnedClone(source, silhouette, $"{source.name}_Silhouette", propertyBlock);
        }

        foreach (MeshRenderer source in visualRoot.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (!IsValidSource(source))
                continue;

            if (!source.TryGetComponent(out MeshFilter meshFilter) || meshFilter.sharedMesh == null)
                continue;

            CreateMeshClone(source, meshFilter, stencil, $"{source.name}_OcclusionStencil", null);
            CreateMeshClone(source, meshFilter, silhouette, $"{source.name}_Silhouette", propertyBlock);
        }
    }

    public void Clear()
    {
        for (int i = clones.Count - 1; i >= 0; i--)
        {
            GameObject clone = clones[i];
            if (clone == null)
                continue;

            if (Application.isPlaying)
                Destroy(clone);
            else
                DestroyImmediate(clone);
        }

        clones.Clear();
    }

    private Material ResolveSilhouetteMaterial()
    {
        if (silhouetteMaterial != null)
            return silhouetteMaterial;

        silhouetteMaterial = Resources.Load<Material>(DefaultSilhouetteMaterialResourcePath);
        if (silhouetteMaterial == null)
            Debug.LogWarning($"[{nameof(PlayerOcclusionSilhouette)}] Resources/{DefaultSilhouetteMaterialResourcePath} material is missing.", this);

        return silhouetteMaterial;
    }

    private Material ResolveStencilMaterial()
    {
        if (stencilMaterial != null)
            return stencilMaterial;

        stencilMaterial = Resources.Load<Material>(DefaultStencilMaterialResourcePath);
        if (stencilMaterial == null)
            Debug.LogWarning($"[{nameof(PlayerOcclusionSilhouette)}] Resources/{DefaultStencilMaterialResourcePath} material is missing.", this);

        return stencilMaterial;
    }

    private static bool IsValidSource(Renderer source)
    {
        return source != null && source.enabled;
    }

    private void CreateSkinnedClone(SkinnedMeshRenderer source, Material material, string cloneName, MaterialPropertyBlock block)
    {
        GameObject cloneObject = CreateCloneObject(source.transform, cloneName);
        SkinnedMeshRenderer clone = cloneObject.AddComponent<SkinnedMeshRenderer>();

        clone.sharedMesh = source.sharedMesh;
        clone.rootBone = source.rootBone;
        clone.bones = source.bones;
        clone.localBounds = source.localBounds;
        clone.quality = source.quality;
        clone.updateWhenOffscreen = true;

        ConfigureRenderer(clone, source.sharedMaterials.Length, material, block);
    }

    private void CreateMeshClone(MeshRenderer source, MeshFilter sourceFilter, Material material, string cloneName, MaterialPropertyBlock block)
    {
        GameObject cloneObject = CreateCloneObject(source.transform, cloneName);
        MeshFilter cloneFilter = cloneObject.AddComponent<MeshFilter>();
        MeshRenderer clone = cloneObject.AddComponent<MeshRenderer>();

        cloneFilter.sharedMesh = sourceFilter.sharedMesh;
        ConfigureRenderer(clone, source.sharedMaterials.Length, material, block);
    }

    private GameObject CreateCloneObject(Transform source, string cloneName)
    {
        GameObject cloneObject = new(cloneName);
        cloneObject.transform.SetParent(source, false);
        cloneObject.transform.localPosition = Vector3.zero;
        cloneObject.transform.localRotation = Quaternion.identity;
        cloneObject.transform.localScale = Vector3.one;
        clones.Add(cloneObject);
        return cloneObject;
    }

    private void ConfigureRenderer(Renderer renderer, int sourceMaterialCount, Material material, MaterialPropertyBlock block)
    {
        int materialCount = Mathf.Max(1, sourceMaterialCount);
        Material[] materials = new Material[materialCount];

        for (int i = 0; i < materials.Length; i++)
            materials[i] = material;

        renderer.sharedMaterials = materials;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

        if (block != null)
            renderer.SetPropertyBlock(block);
    }

    private void OnDestroy()
    {
        Clear();
    }
}
