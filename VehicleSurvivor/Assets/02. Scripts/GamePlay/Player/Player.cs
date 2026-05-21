using Cysharp.Threading.Tasks;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class Player : MonoBehaviour
{
    public PlayerVisual Visual { get; private set; }
    public PlayerController Controller { get; private set; }
    public PlayerDamageReceiver DamageReceiver { get; private set; }
    public PlayerExperiencePickupCollector ExperienceCollector { get; private set; }
    public PlayerWeaponSpawner WeaponSpawner { get; private set; }
    public Animator Animator { get; private set; }
    public PlayerRuntime Runtime { get; private set; }

    public Transform FollowTransform => Controller != null ? Controller.transform : transform;

    private void Awake()
    {
        CacheComponents();
    }

    public async UniTask InitializeVisualAsync(CharacterDefinitionSO character)
    {
        CacheComponents();

        if (Visual == null)
        {
            Debug.LogError("[Player] PlayerVisual was not found.", this);
            return;
        }

        await Visual.InitializeFromCharacterAsync(character);
        CacheComponents();
    }

    public void BindRuntime(PlayerRuntime runtime)
    {
        CacheComponents();
        Runtime = runtime;

        if (DamageReceiver != null)
        {
            DamageReceiver.BindRuntime(runtime);
        }

        if (ExperienceCollector != null)
        {
            ExperienceCollector.BindRuntime(runtime);
        }

        if (WeaponSpawner != null)
        {
            WeaponSpawner.BindRuntime(runtime);
        }
    }

    private void CacheComponents()
    {
        Visual = GetComponentInChildren<PlayerVisual>(true);
        Controller = GetComponentInChildren<PlayerController>(true);
        DamageReceiver = GetComponentInChildren<PlayerDamageReceiver>(true);
        ExperienceCollector = GetComponentInChildren<PlayerExperiencePickupCollector>(true);
        WeaponSpawner = GetComponentInChildren<PlayerWeaponSpawner>(true);
        Animator = GetComponentInChildren<Animator>(true);
    }
}
