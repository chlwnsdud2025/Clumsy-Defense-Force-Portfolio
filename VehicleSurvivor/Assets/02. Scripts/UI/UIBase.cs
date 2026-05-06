using Cysharp.Threading.Tasks;
using UnityEngine;

public class UIBase : MonoBehaviour
{
    public virtual void Show()
    {
        gameObject.SetActive(true);
        OnShow();
    }

    public virtual void OnShow()
    {
    }

    public virtual void Hide()
    {
        gameObject.SetActive(false);
        OnHide();
    }

    public virtual void OnHide()
    {
    }

    public virtual UniTask InitializeAsync() => UniTask.CompletedTask;
}