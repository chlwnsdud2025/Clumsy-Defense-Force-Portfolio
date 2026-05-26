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

    public Transform FollowTransform => Controller != null ? Controller.transform : transform;

#if UNITY_EDITOR
    [Header("Debug")]
    [Tooltip("에디터에서 InGameScene 직접 실행 시 사용할 캐릭터. Title 캐릭터 선택으로 진입하면 무시됩니다.")]
    [SerializeField] private CharacterDefinitionSO _debugCharacterDefinition;

    public CharacterDefinitionSO DebugCharacterDefinition => _debugCharacterDefinition;
#endif

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
    }

    public void BindRuntime(PlayerRuntime runtime)
    {
        CacheComponents();

        if (Controller != null)
            Controller.BindRuntime(runtime);

        if (DamageReceiver != null)
            DamageReceiver.BindRuntime(runtime);

        if (ExperienceCollector != null)
            ExperienceCollector.BindRuntime(runtime);

        if (WeaponSpawner != null)
            WeaponSpawner.BindRuntime(runtime);
    }

    public void UnbindRuntime() => BindRuntime(null);

    private void CacheComponents()
    {
        if (Visual == null)
            Visual = GetComponentInChildren<PlayerVisual>(true);

        if (Controller == null)
            Controller = GetComponentInChildren<PlayerController>(true);

        if (DamageReceiver == null)
            DamageReceiver = GetComponentInChildren<PlayerDamageReceiver>(true);

        if (ExperienceCollector == null)
            ExperienceCollector = GetComponentInChildren<PlayerExperiencePickupCollector>(true);

        if (WeaponSpawner == null)
            WeaponSpawner = GetComponentInChildren<PlayerWeaponSpawner>(true);

        if (Animator == null)
            Animator = GetComponentInChildren<Animator>(true);
    }
}
