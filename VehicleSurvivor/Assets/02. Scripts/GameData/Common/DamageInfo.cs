using UnityEngine;

public readonly struct DamageInfo
{
    public float Amount { get; }
    public bool IsCritical { get; }
    public PlayerRuntime SourcePlayer { get; }

    public DamageInfo(float amount, bool isCritical)
        : this(amount, isCritical, null)
    {
    }

    public DamageInfo(float amount, bool isCritical, PlayerRuntime sourcePlayer)
    {
        Amount = Mathf.Round(amount);
        IsCritical = isCritical;
        SourcePlayer = sourcePlayer;
    }

    public static DamageInfo Normal(float amount) => new DamageInfo(amount, false);
    public DamageInfo WithAmount(float amount) => new DamageInfo(amount, IsCritical, SourcePlayer);
}
