using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

public interface IUIEntry<TEnum> where TEnum : Enum
{
    TEnum Type { get; }
    AssetReferenceGameObject PrefabReference { get; }
    EUILifetimePolicy LifetimePolicy { get; }
    bool ReleaseAssetOnClose { get; }
}

[CreateAssetMenu(fileName = "UIRegistry", menuName = "Scriptable Objects/UI/UIRegistry")]
public class UIRegistry : ScriptableObject
{
    [System.Serializable]
    public class SceneEntry : IUIEntry<ESceneUIType>
    {
        [SerializeField] private ESceneUIType _type;
        [SerializeField] private AssetReferenceGameObject _prefabReference;
        [SerializeField] private EUILifetimePolicy _lifetimePolicy = EUILifetimePolicy.Resident;
        [SerializeField] private bool _releaseAssetOnClose = false;

        ESceneUIType IUIEntry<ESceneUIType>.Type => _type;
        AssetReferenceGameObject IUIEntry<ESceneUIType>.PrefabReference => _prefabReference;
        EUILifetimePolicy IUIEntry<ESceneUIType>.LifetimePolicy => _lifetimePolicy;
        bool IUIEntry<ESceneUIType>.ReleaseAssetOnClose => _releaseAssetOnClose;
    }

    [System.Serializable]
    public class PopupEntry : IUIEntry<EPopupUIType>
    {
        [SerializeField] private EPopupUIType _type;
        [SerializeField] private AssetReferenceGameObject _prefabReference;
        [SerializeField] private EUILifetimePolicy _lifetimePolicy = EUILifetimePolicy.Cached;
        [SerializeField] private bool _releaseAssetOnClose = false;

        EPopupUIType IUIEntry<EPopupUIType>.Type => _type;
        AssetReferenceGameObject IUIEntry<EPopupUIType>.PrefabReference => _prefabReference;
        EUILifetimePolicy IUIEntry<EPopupUIType>.LifetimePolicy => _lifetimePolicy;
        bool IUIEntry<EPopupUIType>.ReleaseAssetOnClose => _releaseAssetOnClose;
    }

    public List<SceneEntry> Scenes;
    public List<PopupEntry> Popups;
}