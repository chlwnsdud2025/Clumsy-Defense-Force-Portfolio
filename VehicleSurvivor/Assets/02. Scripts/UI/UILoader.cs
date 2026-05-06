using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary>
/// UI 프리팹의 Addressables 로드·인스턴스화를 담당합니다.
/// </summary>
public sealed class UILoader
{
    private IResourceService _resourceService;

    public bool TryResolveResourceService()
    {
        if (_resourceService != null)
            return true;

        if (ServiceLocator.TryGet(out IResourceService resourceService))
        {
            _resourceService = resourceService;
            return true;
        }

        Debug.LogError("[UILoader] IResourceService가 등록되지 않았습니다. ServiceBootstrapper 초기화 순서를 확인하세요.");
        return false;
    }

    public bool TryRelease(AssetReference assetReference)
    {
        if (assetReference == null)
            return false;

        if (!TryResolveResourceService())
            return false;

        _resourceService.Release(assetReference);
        return true;
    }

    public async UniTask<T> LoadAndInstantiateAsync<T>(AssetReferenceGameObject assetRef, Transform layer) where T : UIBase
    {
        try
        {
            if (!TryResolveResourceService())
                return null;

            Debug.Log($"[UILoader] ResourceService를 통해 UI Asset을 로드합니다: GUID={assetRef.AssetGUID}");
            GameObject asset = await _resourceService.LoadAssetAsync<GameObject>(assetRef);
            if (asset == null)
            {
                Debug.LogError($"[UILoader] ResourceService가 UI Asset을 반환하지 않았습니다: GUID={assetRef.AssetGUID}");
                return null;
            }

            T prefab = asset.GetComponent<T>();
            if (prefab != null)
            {
                T instance = UnityEngine.Object.Instantiate(prefab, layer);
                Debug.Log($"[UILoader] ResourceService Asset으로 인스턴스 생성 완료: {typeof(T).Name}");
                return instance;
            }

            Debug.LogError($"[UILoader] Asset 로드는 성공했지만 GameObject에서 {typeof(T).Name} 컴포넌트를 찾을 수 없습니다.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[UILoader] ResourceService를 통한 UI 로드 중 오류가 발생했습니다. GUID={assetRef.AssetGUID}, 오류={ex.Message}");

#if UNITY_EDITOR
            Debug.LogWarning($"[UILoader] editor fallback을 시도합니다: GUID={assetRef.AssetGUID}");
            try
            {
                var editorAsset = assetRef.editorAsset as GameObject;
                if (editorAsset != null)
                {
                    Debug.Log($"[UILoader] Editor Asset을 찾았습니다: {editorAsset.name}");
                    T prefab = editorAsset.GetComponent<T>();
                    if (prefab != null)
                    {
                        T instance = UnityEngine.Object.Instantiate(prefab, layer);
                        Debug.Log($"[UILoader] Editor Asset에서 인스턴스 생성 완료: {typeof(T).Name}");
                        return instance;
                    }

                    Debug.LogError($"[UILoader] Editor Asset에 {typeof(T).Name} 컴포넌트가 없습니다.");
                }
                else
                {
                    Debug.LogError($"[UILoader] GUID={assetRef.AssetGUID}에 해당하는 Editor Asset이 null입니다.");
                }
            }
            catch (Exception editorEx)
            {
                Debug.LogError($"[UILoader] editor fallback에 실패했습니다: {editorEx.Message}");
            }
#endif
        }

        return null;
    }
}
