using UnityEngine;

public readonly struct DamageInfo
{
    public float Amount { get; }
    public bool IsCritical { get; }

    public DamageInfo(float amount, bool isCritical)
    {
        Amount = Mathf.Round(amount);
        IsCritical = isCritical;
    }

    public static DamageInfo Normal(float amount) => new DamageInfo(amount, false);
}
