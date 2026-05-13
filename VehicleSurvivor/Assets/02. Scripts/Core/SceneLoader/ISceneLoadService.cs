using Cysharp.Threading.Tasks;
using System;
using UnityEngine.SceneManagement;

public enum LoadingFadeMode
{
    None = 0,
    FadeInOnly = 1,
    FadeOutOnly = 2,
    FadeInOut = 3,
}

public sealed class SceneLoadOptions
{
    public string LoadingMessage { get; set; } = string.Empty;
    public LoadSceneMode LoadSceneMode { get; set; } = LoadSceneMode.Single;
    public float MinimumLoadingSeconds { get; set; } = 0.35f;
    public LoadingFadeMode FadeMode { get; set; } = LoadingFadeMode.FadeInOut;
    public float FadeDuration { get; set; } = 1f;
    public Func<UniTask> BeforeLoadAsync { get; set; }
    public Func<UniTask> BeforeActivationAsync { get; set; }
}

public interface ISceneLoadService
{
    bool IsLoading { get; }

    UniTask<bool> LoadSceneAsync(EGameScene scene, string loadingMessage = null, LoadSceneMode loadSceneMode = LoadSceneMode.Single, float minimumLoadingSeconds = 0.35f);
    UniTask<bool> LoadSceneAsync(string sceneName, string loadingMessage = null, LoadSceneMode loadSceneMode = LoadSceneMode.Single, float minimumLoadingSeconds = 0.35f);
    UniTask<bool> LoadSceneAsync(EGameScene scene, SceneLoadOptions options);
    UniTask<bool> LoadSceneAsync(string sceneName, SceneLoadOptions options);
}
