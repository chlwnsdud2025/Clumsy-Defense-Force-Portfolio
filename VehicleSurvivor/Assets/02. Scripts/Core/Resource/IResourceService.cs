using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public interface IResourceService
{
    UniTask<T> LoadAssetAsync<T>(AssetReference assetReference) where T : Object;
    UniTask<T> LoadAssetAsync<T>(string key) where T : Object;
    void Release(AssetReference assetReference);
    void Release(string key);
    void ReleaseAll();
}