using Cysharp.Threading.Tasks;
using UnityEngine;

public class GameManager : MonoSingleton<GameManager>
{
    private async void Start()
    {
        if (!await InitializeServicesAsync())
            return;

        await UniTask.Yield();
    }

    private async UniTask<bool> InitializeServicesAsync()
    {
        if (!await ServiceBootstrapper.InitializeAsync())
        {
            Debug.LogError("[GameManager] 서비스 초기화에 실패했습니다. 콘솔 로그를 확인하세요.");
            return false;
        }

        return true;
    }
}
