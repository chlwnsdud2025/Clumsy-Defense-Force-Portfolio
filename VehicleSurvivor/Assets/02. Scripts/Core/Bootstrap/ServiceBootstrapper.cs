using Cysharp.Threading.Tasks;
using System;
using System.ComponentModel.Design;
using UnityEngine;

public static class ServiceBootstrapper
{
    private static bool _isInitialized;

    public static bool IsInitialized => _isInitialized;

    public static async UniTask<bool> InitializeAsync()
    {
        if (_isInitialized)
            return true;

        try
        {
            bool registered = await RegisterCoreServicesAsync();
            if (!registered)
            {
                _isInitialized = false;
                return false;
            }

            _isInitialized = true;
            return true;
        }
        catch (Exception ex)
        {
            _isInitialized = false;
            Debug.LogError($"[ServiceBootstrapper] 서비스 등록 중 오류가 발생했습니다: {ex.Message}");
            return false;
        }
    }

    private static async UniTask<bool> RegisterCoreServicesAsync()
    {
        ServiceLocator.Clear();

        return true;
    }
}