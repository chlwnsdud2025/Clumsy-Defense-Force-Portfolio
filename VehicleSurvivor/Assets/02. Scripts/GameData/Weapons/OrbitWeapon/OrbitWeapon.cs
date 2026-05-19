using System.Collections.Generic;
using UnityEngine;

public class OrbitWeapon : WeaponBase
{
    [Header("회전 무기 데이터")]
    public OrbitWeaponDataSO orbitData;

    [Header("임시 테스트 스탯")]
    public float damage = 10f;
    public float radius = 3f;
    public float rotateSpeed = 120f;
    public int orbitCount = 1;
    public float hitCooldown = 0.3f;

    private readonly List<GameObject> orbitObjects =
        new List<GameObject>();

    private void Start()
    {
        SyncOrbitObjects();
    }

    private void Update()
    {
        RotateOrbitObjects();
    }

    public override void LevelUpWeapon()
    {
        base.LevelUpWeapon();

        SyncOrbitObjects();
    }

    private void SyncOrbitObjects()
    {
        if (orbitData == null || orbitData.orbitPrefab == null)
            return;

        int targetCount = Mathf.Max(
            1,
            GetFinalProjectileCount(orbitCount)
        );

        while (orbitObjects.Count < targetCount)
        {
            GameObject orbitObject = Instantiate(
                orbitData.orbitPrefab,
                transform.position,
                Quaternion.identity,
                transform
            );

            OrbitHitbox hitbox =
                orbitObject.GetComponent<OrbitHitbox>();

            if (hitbox == null)
            {
                hitbox = orbitObject.AddComponent<OrbitHitbox>();
            }

            hitbox.Init(
                GetFinalDamage(damage),
                hitCooldown,
                orbitData.hitEffectPrefab
            );

            orbitObjects.Add(orbitObject);
        }

        while (orbitObjects.Count > targetCount)
        {
            GameObject removeTarget =
                orbitObjects[orbitObjects.Count - 1];

            orbitObjects.RemoveAt(orbitObjects.Count - 1);

            Destroy(removeTarget);
        }
    }

    private void RotateOrbitObjects()
    {
        if (orbitObjects.Count == 0)
            return;

        int count = orbitObjects.Count;
        float angleStep = 360f / count;
        float currentAngle = Time.time * rotateSpeed;

        for (int i = 0; i < count; i++)
        {
            if (orbitObjects[i] == null)
                continue;

            float angle = currentAngle + angleStep * i;
            float radian = angle * Mathf.Deg2Rad;

            Vector3 offset = new Vector3(
                Mathf.Cos(radian),
                0f,
                Mathf.Sin(radian)
            ) * radius;

            orbitObjects[i].transform.position =
                transform.position + offset;
        }
    }
}
