using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum EGameScene
{
    TitleScene = 0,
    InGameScene = 1,
}

public class SceneLoadService : ISceneLoadService
{
    private const float SceneReadyProgress = 0.9f;

    public bool IsLoading { get; private set; }

    public UniTask<bool> LoadSceneAsync(EGameScene scene, string loadingMessage = null, LoadSceneMode loadSceneMode = LoadSceneMode.Single, float minimumLoadingSeconds = 0.35f)
    {
        return LoadSceneAsync(scene.ToString(), new SceneLoadOptions
        {
            LoadingMessage = loadingMessage,
            LoadSceneMode = loadSceneMode,
            MinimumLoadingSeconds = minimumLoadingSeconds,
        });
    }

    public UniTask<bool> LoadSceneAsync(string sceneName, string loadingMessage = null, LoadSceneMode loadSceneMode = LoadSceneMode.Single, float minimumLoadingSeconds = 0.35f)
    {
        return LoadSceneAsync(sceneName, new SceneLoadOptions
        {
            LoadingMessage = loadingMessage,
            LoadSceneMode = loadSceneMode,
            MinimumLoadingSeconds = minimumLoadingSeconds,
        });
    }

    public UniTask<bool> LoadSceneAsync(EGameScene scene, SceneLoadOptions options)
    {
        return LoadSceneAsync(scene.ToString(), options);
    }

    public async UniTask<bool> LoadSceneAsync(string sceneName, SceneLoadOptions options)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("[SceneLoadService] sceneName이 비어 있습니다.");
            return false;
        }

        if (IsLoading)
        {
            Debug.LogWarning($"[SceneLoadService] 이미 다른 씬 로딩이 진행 중입니다. 요청된 씬: {sceneName}");
            return false;
        }

        if (!UIManager.HasInstance)
        {
            Debug.LogError("[SceneLoadService] UIManager Instance가 없어 로딩 UI를 표시할 수 없습니다.");
            return false;
        }

        SceneLoadOptions normalizedOptions = NormalizeOptions(sceneName, options);
        IsLoading = true;
        float startedAt = Time.unscaledTime;
        UI_Loading loadingUI = null;

        try
        {
            loadingUI = await UIManager.Instance.ShowLoadingAsync();
            if (loadingUI == null)
            {
                Debug.LogError("[SceneLoadService] 런타임 Loading UI를 준비하지 못했습니다.");
                return false;
            }

            loadingUI.SetMessage(normalizedOptions.LoadingMessage);
            loadingUI.SetProgress(0f);
            await loadingUI.PlayShowAsync(normalizedOptions.FadeMode, normalizedOptions.FadeDuration);

            if (!await InvokeHookAsync(normalizedOptions.BeforeLoadAsync, "로딩 시작 전"))
            {
                await HideLoadingAsync(loadingUI, normalizedOptions);
                return false;
            }

            AsyncOperation sceneLoadOperation = SceneManager.LoadSceneAsync(sceneName, normalizedOptions.LoadSceneMode);
            if (sceneLoadOperation == null)
            {
                Debug.LogError($"[SceneLoadService] SceneManager.LoadSceneAsync가 null을 반환했습니다: {sceneName}");
                await HideLoadingAsync(loadingUI, normalizedOptions);
                return false;
            }

            sceneLoadOperation.allowSceneActivation = false;

            while (sceneLoadOperation.progress < SceneReadyProgress)
            {
                float normalizedProgress = Mathf.Clamp01(sceneLoadOperation.progress / SceneReadyProgress);
                loadingUI.SetProgress(normalizedProgress);
                await UniTask.Yield();
            }

            loadingUI.SetProgress(1f);

            float elapsed = Time.unscaledTime - startedAt;
            float remaining = Mathf.Max(0f, normalizedOptions.MinimumLoadingSeconds - elapsed);
            if (remaining > 0f)
                await UniTask.Delay(TimeSpan.FromSeconds(remaining), ignoreTimeScale: true);

            if (!await InvokeHookAsync(normalizedOptions.BeforeActivationAsync, "씬 활성화 전"))
            {
                await HideLoadingAsync(loadingUI, normalizedOptions);
                return false;
            }

            sceneLoadOperation.allowSceneActivation = true;
            await sceneLoadOperation.ToUniTask();

            await HideLoadingAsync(loadingUI, normalizedOptions);
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SceneLoadService] 씬 로딩 중 오류가 발생했습니다: {ex.Message}");
            await HideLoadingAsync(loadingUI, normalizedOptions);

            return false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private SceneLoadOptions NormalizeOptions(string sceneName, SceneLoadOptions options)
    {
        SceneLoadOptions normalizedOptions = options ?? new SceneLoadOptions();
        normalizedOptions.MinimumLoadingSeconds = Mathf.Max(0f, normalizedOptions.MinimumLoadingSeconds);
        normalizedOptions.FadeDuration = Mathf.Max(0f, normalizedOptions.FadeDuration);
        return normalizedOptions;
    }

    private async UniTask<bool> InvokeHookAsync(Func<UniTask> hook, string hookName)
    {
        if (hook == null)
            return true;

        try
        {
            await hook();
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SceneLoadService] {hookName} 콜백 실행 중 오류가 발생했습니다: {ex.Message}");
            return false;
        }
    }

    private async UniTask HideLoadingAsync(UI_Loading loadingUI, SceneLoadOptions options)
    {
        if (!UIManager.HasInstance)
            return;

        if (loadingUI != null)
            await loadingUI.PlayHideAsync(options.FadeMode, options.FadeDuration);

        UIManager.Instance.HideLoading();
    }
}
