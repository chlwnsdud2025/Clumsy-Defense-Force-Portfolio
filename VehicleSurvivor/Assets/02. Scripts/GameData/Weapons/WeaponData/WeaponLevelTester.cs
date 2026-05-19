using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponLevelTester : MonoBehaviour
{
    [Header("테스트할 무기")]
    public WeaponBase weapon;

    [Header("레벨업 키")]
    public Key levelUpKey = Key.L;

    private Keyboard keyboard;

    private void Start()
    {
        keyboard = Keyboard.current;
    }

    private void Update()
    {
        if (keyboard == null)
            return;

        if (keyboard[levelUpKey].wasPressedThisFrame)
        {
            weapon.LevelUpWeapon();

            Debug.Log("무기 레벨업 테스트!");
        }
    }
}