using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class UIManager : MonoSingleton<UIManager>
{
    [Header("## 레지스트리")]
    [SerializeField] private UIRegistry _uiRegistry;

    [Header("## 씬 레이어")]
    [SerializeField] private Transform _sceneLayer;

    [Header("## 팝업 레이어")]
    [SerializeField] private Transform _popupLayer;

    [Header("## 로딩 레이어")]
    [SerializeField] private Transform _loadingLayer;

    [SerializeField] private UI_Loading _loadingUI;

    private readonly Dictionary<ESceneUIType, UIScene> _sceneCache = new Dictionary<ESceneUIType, UIScene>();
    private readonly Dictionary<EPopupUIType, UIPopup> _popupCache = new Dictionary<EPopupUIType, UIPopup>();

    private readonly Stack<UIPopup> _popupHistory = new Stack<UIPopup>();

    private UIScene _currentScene;
    private UIPopup _currentPopup;

    private bool _isInitialized = false;
    private bool _isInitializing = false;
    private bool _isLoadingInitialized = false;
    private float _initializationProgress = 0f;

    private int _processedItems;
    private int _totalItems;

    private readonly UILoader _uiLoader = new UILoader();

    public bool IsInitialized => _isInitialized;

    public async UniTask<bool> InitializeAsync(IProgress<float> progress = null)
    {
        if (_isInitialized)
        {
            Debug.LogWarning("[UIManager] UIManager는 이미 초기화되어 있습니다.");
            progress?.Report(1f);
            return true;
        }

        // 동시 호출 가드: 이미 초기화 중이면 완료까지 대기
        if (_isInitializing)
        {
            progress?.Report(_initializationProgress);
            await UniTask.WaitUntil(() => _isInitialized == true || _isInitializing == false);
            return _isInitialized;
        }

        // 필수 참조 가드: 씬에 배치된 UIManager에 레퍼런스를 설정하지 않으면 초기화 불가
        if (!HasRequiredReferences())
        {
            return false;
        }

        if (!_uiLoader.TryResolveResourceService())
        {
            return false;
        }

        Debug.Log("[UIManager] UniTask로 UI 초기화를 시작합니다.");
        _initializationProgress = 0f;
        _isInitializing = true;

        try
        {
            bool initializeSucceeded = true;
            _totalItems = CountPreloadTargets<ESceneUIType, UIRegistry.SceneEntry>(_uiRegistry.Scenes)
                + CountPreloadTargets<EPopupUIType, UIRegistry.PopupEntry>(_uiRegistry.Popups);
            _processedItems = 0;

            if (_uiRegistry.Scenes != null)
                initializeSucceeded &= await PreloadUIElementsAsync(_uiRegistry.Scenes, _sceneLayer, _sceneCache, progress);

            if (_uiRegistry.Popups != null)
                initializeSucceeded &= await PreloadUIElementsAsync(_uiRegistry.Popups, _popupLayer, _popupCache, progress);

            initializeSucceeded &= await InitializeLoadingAsync();

            if (!initializeSucceeded)
            {
                Debug.LogError("[UIManager] 일부 UI 초기화에 실패했습니다. 이전 로그를 확인하세요.");
                return false;
            }

            _isInitialized = true;
            _initializationProgress = 1f;
            progress?.Report(1f);

            Debug.Log("[UIManager] UI 초기화가 성공적으로 완료되었습니다.");
            return true;
        }
        catch (Exception ex)
        {
            _isInitialized = false;
            Debug.LogError($"[UIManager] UI 초기화 중 오류: {ex.Message}");
            return false;
        }
        finally
        {
            _isInitializing = false;
        }
    }

    #region Scene
    public UIScene ShowScene(ESceneUIType sceneType) => ShowCommon(_sceneCache, sceneType, ref _currentScene);
    public async UniTask<UIScene> ShowSceneAsync(ESceneUIType sceneType)
    {
        UIScene scene = await EnsureLoadedAsync(_uiRegistry?.Scenes, _sceneLayer, _sceneCache, sceneType);
        if (scene == null)
            return null;

        return ShowCommon(_sceneCache, sceneType, ref _currentScene);
    }

    public T ShowScene<T>(ESceneUIType sceneType) where T : UIScene
    {
        UIScene scene = ShowScene(sceneType);

        if (scene == null)
            return null;

        T typedScene = scene as T;
        if (typedScene == null)
        {
            Debug.LogError($"[UIManager] Scene Type {sceneType}은(는) {typeof(T).Name} 타입이 아닙니다.");
            return null;
        }

        return typedScene;
    }

    public async UniTask<T> ShowSceneAsync<T>(ESceneUIType sceneType) where T : UIScene
    {
        UIScene scene = await ShowSceneAsync(sceneType);

        if (scene == null)
            return null;

        T typedScene = scene as T;
        if (typedScene == null)
        {
            Debug.LogError($"[UIManager] Scene Type {sceneType}은(는) {typeof(T).Name} 타입이 아닙니다.");
            return null;
        }

        return typedScene;
    }

    public void CloseCurrentScene() => CloseCommon(ref _currentScene, _uiRegistry?.Scenes, _sceneCache);

    public T GetCurrentScene<T>() where T : UIScene => _currentScene as T;
    #endregion

    #region Popup
    public UIPopup ShowPopup(EPopupUIType popupType, bool remember = true)
    {
        if (!_popupCache.TryGetValue(popupType, out UIPopup popup))
        {
            Debug.LogError($"[UIManager] Popup Type {popupType}을(를) 찾을 수 없습니다.");
            return null;
        }

        if (_currentPopup != null && ReferenceEquals(_currentPopup, popup))
        {
            if (!_currentPopup.gameObject.activeSelf)
            {
                _currentPopup.Show();
                _currentPopup.transform.SetAsLastSibling();
            }
            return popup;
        }

        if (_currentPopup != null)
            _currentPopup.Hide();

        if (remember && popup.RememberInHistory)
            _popupHistory.Push(popup);

        _currentPopup = popup;
        _currentPopup.Show();
        _currentPopup.transform.SetAsLastSibling();
        return popup;
    }

    public async UniTask<UIPopup> ShowPopupAsync(EPopupUIType popupType, bool remember = true)
    {
        UIPopup popup = await EnsureLoadedAsync(_uiRegistry?.Popups, _popupLayer, _popupCache, popupType);
        if (popup == null)
            return null;

        return ShowPopup(popupType, remember);
    }

    public T ShowPopup<T>(EPopupUIType popupType, bool remember = true) where T : UIPopup
    {
        UIPopup popup = ShowPopup(popupType, remember);

        if (popup == null)
            return null;

        T typedPopup = popup as T;
        if (typedPopup == null)
        {
            Debug.LogError($"[UIManager] Popup Type {popupType}은(는) {typeof(T).Name} 타입이 아닙니다.");
            return null;
        }

        return typedPopup;
    }

    public async UniTask<T> ShowPopupAsync<T>(EPopupUIType popupType, bool remember = true) where T : UIPopup
    {
        UIPopup popup = await ShowPopupAsync(popupType, remember);

        if (popup == null)
            return null;

        T typedPopup = popup as T;
        if (typedPopup == null)
        {
            Debug.LogError($"[UIManager] Popup Type {popupType}은(는) {typeof(T).Name} 타입이 아닙니다.");
            return null;
        }

        return typedPopup;
    }

    public void CloseAllPopups() => ClosePopups(remain: 0);

    public void ClosePopups(int remain)
    {
        TrimInvalidPopupHistory();

        while (_popupHistory.Count > remain)
        {
            CloseCurrentPopup();

            if (_currentPopup == null)
                TrimInvalidPopupHistory();
        }
    }

    public void CloseCurrentPopup()
    {
        ClosePopup(_currentPopup);
    }

    public void ClosePopup(UIPopup popup)
    {
        if (popup == null)
            return;

        bool wasCurrent = ReferenceEquals(_currentPopup, popup);

        CloseManagedInstance(popup, _uiRegistry?.Popups, _popupCache);
        if (wasCurrent)
        {
            if (_popupHistory.Count > 0 && ReferenceEquals(_popupHistory.Peek(), popup))
                _popupHistory.Pop();

            _currentPopup = GetNextPopupFromHistory();
            if (_currentPopup != null)
            {
                _currentPopup.Show();
                _currentPopup.transform.SetAsLastSibling();
            }
        }
        else
        {
            RemovePopupFromHistory(popup);
        }
    }

    private void RemovePopupFromHistory(UIPopup popup)
    {
        if (popup == null || _popupHistory.Count == 0)
            return;

        var buffer = new Stack<UIPopup>(_popupHistory.Count);
        bool removed = false;

        while (_popupHistory.Count > 0)
        {
            var top = _popupHistory.Pop();
            if (!removed && ReferenceEquals(top, popup))
            {
                removed = true;
                continue;
            }

            buffer.Push(top);
        }

        while (buffer.Count > 0)
        {
            _popupHistory.Push(buffer.Pop());
        }
    }

    private UIPopup GetNextPopupFromHistory()
    {
        TrimInvalidPopupHistory();
        return _popupHistory.Count > 0 ? _popupHistory.Peek() : null;
    }

    private void TrimInvalidPopupHistory()
    {
        if (_popupHistory.Count == 0)
            return;

        var buffer = new Stack<UIPopup>(_popupHistory.Count);
        while (_popupHistory.Count > 0)
        {
            UIPopup popup = _popupHistory.Pop();
            if (popup != null)
                buffer.Push(popup);
        }

        while (buffer.Count > 0)
        {
            _popupHistory.Push(buffer.Pop());
        }
    }

    public T GetCurrentPopup<T>() where T : UIPopup => _currentPopup as T;
    #endregion

    #region Loading
    public UI_Loading ShowLoading()
    {
        if (_loadingUI == null)
        {
            Debug.LogError("[UIManager] Loading UI가 할당되지 않았습니다.");
            return null;
        }

        _loadingUI.Show();
        _loadingUI.transform.SetAsLastSibling();
        return _loadingUI;
    }

    public async UniTask<UI_Loading> ShowLoadingAsync()
    {
        UI_Loading loading = await EnsureLoadingAsync();
        if (loading == null)
            return null;

        return ShowLoading();
    }

    public void HideLoading()
    {
        if (_loadingUI == null)
            return;

        _loadingUI.Hide();
    }
    #endregion

    private TBase ShowCommon<TBase, TEnum>(Dictionary<TEnum, TBase> cache, TEnum type, ref TBase current)
       where TBase : UIBase
       where TEnum : Enum
    {
        if (!cache.TryGetValue(type, out var target))
        {
            Debug.LogError($"[UIManager] UI Type {type}을(를) 찾을 수 없습니다. 사용 가능한 Type: {string.Join(", ", cache.Keys)}");
            return null;
        }

        if (current != null)
        {
            if (ReferenceEquals(current, target))
                return target;

            CloseCachedOrTransientOnSwitch(ref current, cache);
        }

        current = target;
        current.Show();
        return target;
    }

    private void CloseCommon<TBase, TEnum, TEntry>(ref TBase current, List<TEntry> entries, Dictionary<TEnum, TBase> cache)
        where TBase : UIBase
        where TEnum : Enum
        where TEntry : class, IUIEntry<TEnum>
    {
        if (current == null)
            return;

        CloseManagedInstance(current, entries, cache);
        current = null;
    }

    private bool HasRequiredReferences()
    {
        bool isValid = true;

        if (_uiRegistry == null)
        {
            Debug.LogError("[UIManager] UIRegistry가 할당되지 않았습니다. 씬에 배치된 UIManager에 레퍼런스를 설정하세요.");
            isValid = false;
        }

        if ((_uiRegistry?.Scenes?.Count ?? 0) > 0 && _sceneLayer == null)
        {
            Debug.LogError("[UIManager] Scene Layer가 할당되지 않았습니다.");
            isValid = false;
        }

        if ((_uiRegistry?.Popups?.Count ?? 0) > 0 && _popupLayer == null)
        {
            Debug.LogError("[UIManager] Popup Layer가 할당되지 않았습니다.");
            isValid = false;
        }

        if (_loadingLayer == null)
        {
            Debug.LogError("[UIManager] Loading Layer가 할당되지 않았습니다.");
            isValid = false;
        }

        if (_loadingUI == null)
        {
            Debug.LogError("[UIManager] Loading UI가 할당되지 않았습니다.");
            isValid = false;
        }

        return isValid;
    }

    private void AdvanceInitializationProgress(IProgress<float> progress)
    {
        _processedItems++;
        _initializationProgress = _totalItems > 0 ? (float)_processedItems / _totalItems : 1f;
        progress?.Report(_initializationProgress);
    }

    private int CountPreloadTargets<TEnum, TEntry>(List<TEntry> entries)
         where TEnum : Enum
         where TEntry : class, IUIEntry<TEnum>
    {
        if (entries == null)
            return 0;

        int count = 0;
        foreach (var entry in entries)
        {
            if (entry != null && entry.LifetimePolicy == EUILifetimePolicy.Resident)
                count++;
        }

        return count;
    }

    private async UniTask<bool> InitializeLoadingAsync()
    {
        UI_Loading loading = await EnsureLoadingAsync();
        return loading != null;
    }

    private async UniTask<bool> PreloadUIElementsAsync<TBase, TEnum, TEntry>(List<TEntry> entries, Transform layer, Dictionary<TEnum, TBase> cache, IProgress<float> progress = null)
         where TBase : UIBase
         where TEnum : Enum
         where TEntry : class, IUIEntry<TEnum>
    {
        bool succeeded = true;
        HashSet<TEnum> seenTypes = new HashSet<TEnum>();

        foreach (var entry in entries)
        {
            bool shouldAdvanceProgress = entry != null && entry.LifetimePolicy == EUILifetimePolicy.Resident;

            try
            {
                if (entry == null)
                {
                    Debug.LogError("[UIManager] UI Registry Entry가 null입니다.");
                    succeeded = false;
                    continue;
                }

                if (!seenTypes.Add(entry.Type))
                {
                    Debug.LogError($"[UIManager] Registry에서 중복된 UI Type이 감지되었습니다: {entry.Type}");
                    succeeded = false;
                    continue;
                }

                if (cache.TryGetValue(entry.Type, out var cachedInstance) && cachedInstance != null)
                {
                    Debug.LogWarning($"[UIManager] 이미 캐시된 UI입니다. 중복 초기화를 건너뜁니다: {entry.Type}");
                    continue;
                }

                if (entry.PrefabReference == null)
                {
                    Debug.LogError($"[UIManager] UI Type {entry.Type}의 Prefab Reference가 비어 있습니다.");
                    succeeded = false;
                    continue;
                }

                if (!entry.PrefabReference.RuntimeKeyIsValid())
                {
                    Debug.LogError($"[UIManager] UI Type {entry.Type}의 Addressables Key가 올바르지 않습니다.");
                    succeeded = false;
                    continue;
                }

                Debug.Log($"[UIManager] UI 초기화 중: Type={entry.Type}, GUID={entry.PrefabReference.AssetGUID}");
                var instance = await _uiLoader.LoadAndInstantiateAsync<TBase>(entry.PrefabReference, layer);
                if (instance != null)
                {
                    // 초기화 대기 중 1프레임 노출 방지: 먼저 비활성화 후 초기화
                    instance.gameObject.SetActive(false);
                    await instance.InitializeAsync();
                    cache[entry.Type] = instance;
                    Debug.Log($"[UIManager] 로드 및 캐싱 완료: {entry.Type}");
                }
                else
                {
                    succeeded = false;
                    Debug.LogError($"[UIManager] UI 로드에 실패했습니다: Type={entry.Type}, GUID={entry.PrefabReference.AssetGUID}");
                }
            }
            catch (Exception ex)
            {
                succeeded = false;
                string entryType = entry != null ? entry.Type.ToString() : "<null>";
                Debug.LogError($"[UIManager] UI {entryType} 초기화 중 예기치 않은 오류가 발생했습니다: {ex.Message}");
            }
            finally
            {
                if (shouldAdvanceProgress)
                    AdvanceInitializationProgress(progress);
            }
        }

        return succeeded;
    }

    private async UniTask<UI_Loading> EnsureLoadingAsync()
    {
        if (_loadingUI == null)
        {
            Debug.LogError("[UIManager] Loading UI가 할당되지 않았습니다.");
            return null;
        }

        if (_loadingLayer != null && _loadingUI.transform.parent != _loadingLayer)
            _loadingUI.transform.SetParent(_loadingLayer, false);

        if (_isLoadingInitialized)
            return _loadingUI;

        _loadingUI.gameObject.SetActive(false);
        await _loadingUI.InitializeAsync();

        _isLoadingInitialized = true;
        return _loadingUI;
    }

    private async UniTask<TBase> EnsureLoadedAsync<TBase, TEnum, TEntry>(List<TEntry> entries, Transform layer, Dictionary<TEnum, TBase> cache, TEnum type)
        where TBase : UIBase
        where TEnum : Enum
        where TEntry : class, IUIEntry<TEnum>
    {
        if (cache.TryGetValue(type, out TBase cachedInstance) && cachedInstance != null)
            return cachedInstance;

        if (!TryGetEntry(entries, type, out TEntry entry))
        {
            Debug.LogError($"[UIManager] UI Type {type}에 해당하는 Registry Entry를 찾을 수 없습니다.");
            return null;
        }

        if (entry.PrefabReference == null)
        {
            Debug.LogError($"[UIManager] UI Type {entry.Type}의 Prefab Reference가 비어 있습니다.");
            return null;
        }

        if (!entry.PrefabReference.RuntimeKeyIsValid())
        {
            Debug.LogError($"[UIManager] UI Type {entry.Type}의 Addressables Key가 올바르지 않습니다.");
            return null;
        }

        Debug.Log($"[UIManager] 지연 로드 시작: Type={entry.Type}, Lifetime={entry.LifetimePolicy}");
        TBase instance = await _uiLoader.LoadAndInstantiateAsync<TBase>(entry.PrefabReference, layer);
        if (instance == null)
            return null;

        instance.gameObject.SetActive(false);
        await instance.InitializeAsync();
        cache[type] = instance;
        return instance;
    }

    private void CloseCachedOrTransientOnSwitch<TBase, TEnum>(ref TBase current, Dictionary<TEnum, TBase> cache)
        where TBase : UIBase
        where TEnum : Enum
    {
        if (current == null)
            return;

        if (typeof(TBase) == typeof(UIScene))
        {
            UIScene currentScene = current as UIScene;
            CloseManagedInstance(currentScene, _uiRegistry?.Scenes, _sceneCache);
        }
        else
        {
            current.Hide();
        }

        current = null;
    }

    private void CloseManagedInstance<TBase, TEnum, TEntry>(TBase instance, List<TEntry> entries, Dictionary<TEnum, TBase> cache)
        where TBase : UIBase
        where TEnum : Enum
        where TEntry : class, IUIEntry<TEnum>
    {
        if (instance == null)
            return;

        instance.Hide();

        if (!TryGetEntryByInstance(entries, cache, instance, out TEntry entry, out TEnum type))
            return;

        if (entry.LifetimePolicy != EUILifetimePolicy.Transient)
            return;

        cache.Remove(type);

        if (entry.ReleaseAssetOnClose)
            _uiLoader.TryRelease(entry.PrefabReference);

        Destroy(instance.gameObject);
    }

    private bool TryGetEntry<TEnum, TEntry>(List<TEntry> entries, TEnum type, out TEntry matchedEntry)
        where TEnum : Enum
        where TEntry : class, IUIEntry<TEnum>
    {
        if (entries != null)
        {
            foreach (var entry in entries)
            {
                if (entry != null && EqualityComparer<TEnum>.Default.Equals(entry.Type, type))
                {
                    matchedEntry = entry;
                    return true;
                }
            }
        }

        matchedEntry = null;
        return false;
    }

    private bool TryGetEntryByInstance<TBase, TEnum, TEntry>(List<TEntry> entries, Dictionary<TEnum, TBase> cache, TBase instance, out TEntry matchedEntry, out TEnum matchedType)
        where TBase : UIBase
        where TEnum : Enum
        where TEntry : class, IUIEntry<TEnum>
    {
        foreach (var pair in cache)
        {
            if (!ReferenceEquals(pair.Value, instance))
                continue;

            if (TryGetEntry(entries, pair.Key, out matchedEntry))
            {
                matchedType = pair.Key;
                return true;
            }

            break;
        }

        matchedEntry = null;
        matchedType = default;
        return false;
    }
}