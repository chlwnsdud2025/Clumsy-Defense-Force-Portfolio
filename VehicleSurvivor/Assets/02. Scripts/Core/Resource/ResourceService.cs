using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class ResourceService : IResourceService
{
    private readonly Dictionary<string, AsyncOperationHandle> _loadedKeyHandles = new();

    public UniTask<T> LoadAssetAsync<T>(AssetReference assetReference) where T : UnityEngine.Object
    {
        if (assetReference == null)
            throw new ArgumentNullException(nameof(assetReference));

        if (!assetReference.RuntimeKeyIsValid())
        {
            Debug.LogError("[ResourceService] Addressables 키가 올바르지 않습니다.");
            return UniTask.FromResult<T>(null);
        }

        string key = GetAddressablesKey(assetReference);
        if (string.IsNullOrWhiteSpace(key))
        {
            Debug.LogError("[ResourceService] RuntimeKey를 문자열로 변환할 수 없습니다.");
            return UniTask.FromResult<T>(null);
        }

        return LoadAssetAsync<T>(key);
    }

    public async UniTask<T> LoadAssetAsync<T>(string key) where T : UnityEngine.Object
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Addressables key가 비어 있습니다.", nameof(key));

        if (_loadedKeyHandles.TryGetValue(key, out AsyncOperationHandle cachedHandle) && cachedHandle.IsValid())
        {
            T cachedAsset = cachedHandle.Result as T;
            if (cachedAsset != null)
                return cachedAsset;

            if (cachedHandle.IsValid())
                Addressables.Release(cachedHandle);

            _loadedKeyHandles.Remove(key);
        }

        AsyncOperationHandle<T> loadHandle = Addressables.LoadAssetAsync<T>(key);
        T loadedAsset = await loadHandle.ToUniTask();
        if (loadedAsset == null)
        {
            Debug.LogError($"[ResourceService] 에셋 로드에 실패했습니다: key={key}");
            Addressables.Release(loadHandle);
            return null;
        }

        _loadedKeyHandles[key] = loadHandle;
        return loadedAsset;
    }

    public void Release(AssetReference assetReference)
    {
        if (assetReference == null)
            return;

        if (!assetReference.RuntimeKeyIsValid())
            return;

        string key = GetAddressablesKey(assetReference);
        if (string.IsNullOrWhiteSpace(key))
            return;

        Release(key);
    }

    public void Release(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        if (!_loadedKeyHandles.TryGetValue(key, out AsyncOperationHandle handle))
            return;

        if (handle.IsValid())
            Addressables.Release(handle);

        _loadedKeyHandles.Remove(key);
    }

    public void ReleaseAll()
    {
        foreach (AsyncOperationHandle handle in _loadedKeyHandles.Values)
        {
            if (handle.IsValid())
                Addressables.Release(handle);
        }

        _loadedKeyHandles.Clear();
    }

    private string GetAddressablesKey(AssetReference assetReference)
    {
        object runtimeKey = assetReference.RuntimeKey;
        if (runtimeKey is string s)
            return s;

        return runtimeKey?.ToString();
    }
}