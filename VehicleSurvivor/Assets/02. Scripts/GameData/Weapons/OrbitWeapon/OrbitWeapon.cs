using System.Collections.Generic;
using UnityEngine;

public class OrbitWeapon : WeaponBase
{

    //Damage = 접촉 데미지
    //Cooldown = 같은 대상 재타격 간격
    //Radius = 회전 반경
    //ProjectileCount = 회전 오브젝트 개수
    //ProjectileSpeed 또는 BulletSpeed = 회전 속도

    [Header("Orbit References")]
    public GameObject orbitObjectPrefab;

    [Header("Rotation Option")]
    public Vector3 orbitRotationOffset;

    private readonly List<OrbitObject> orbitObjects =
        new List<OrbitObject>();

    protected override void OnInitialized()
    {
        SyncOrbitObjects(GetCurrentLevelData());
    }

    protected override void OnLevelDataChanged(WeaponLevelData data)
    {
        SyncOrbitObjects(data);
    }

    protected override void Update()
    {
        base.Update();

        RotateOrbitObjects();
    }

    protected override void Attack(WeaponLevelData data)
    {
        // Orbit은 공격 타이밍에 생성/발사가 아니라
        // 회전 오브젝트가 직접 충돌 데미지를 담당함.
        SyncOrbitObjects(data);
    }

    private void SyncOrbitObjects(WeaponLevelData data)
    {
        if (data == null)
            return;

        if (orbitObjectPrefab == null)
        {
            Debug.LogWarning("[OrbitWeapon] Orbit Object Prefab이 비어있습니다.");
            return;
        }

        int targetCount = Mathf.Max(
            1,
            GetFinalProjectileCount(data.ProjectileCount)
        );

        DamageInfo damageInfo = GetFinalDamage(data.Damage);

        while (orbitObjects.Count < targetCount)
        {
            GameObject orbitObject = Instantiate(
                orbitObjectPrefab,
                transform.position,
                orbitObjectPrefab.transform.rotation * Quaternion.Euler(orbitRotationOffset),
                transform
            );

            OrbitObject orbit = orbitObject.GetComponent<OrbitObject>();

            if (orbit == null)
            {
                orbit = orbitObject.AddComponent<OrbitObject>();
            }

            orbit.Initialize(
                damageInfo,
                data.Cooldown
            );

            orbitObjects.Add(orbit);
        }

        while (orbitObjects.Count > targetCount)
        {
            OrbitObject removeTarget =
                orbitObjects[orbitObjects.Count - 1];

            orbitObjects.RemoveAt(orbitObjects.Count - 1);

            if (removeTarget != null)
            {
                Destroy(removeTarget.gameObject);
            }
        }

        foreach (OrbitObject orbit in orbitObjects)
        {
            if (orbit == null)
                continue;

            orbit.Initialize(
                damageInfo,
                data.Cooldown
            );
        }
    }

    private void RotateOrbitObjects()
    {
        WeaponLevelData data = GetCurrentLevelData();

        if (data == null)
            return;

        if (orbitObjects.Count == 0)
            return;

        float radius = GetFinalWeaponScale(data.Radius);
        float rotateSpeed = GetOrbitRotateSpeed(data);

        int count = orbitObjects.Count;
        float angleStep = 360f / count;
        float currentAngle = Time.time * rotateSpeed;

        for (int i = 0; i < count; i++)
        {
            OrbitObject orbit = orbitObjects[i];

            if (orbit == null)
                continue;

            float angle = currentAngle + angleStep * i;
            float radian = angle * Mathf.Deg2Rad;

            Vector3 offset = new Vector3(
    Mathf.Cos(radian),
    0f,
    Mathf.Sin(radian)
) * radius;

            Vector3 orbitPosition =
                transform.position + offset;

            orbitPosition.y =
                transform.position.y + 1f;

            orbit.transform.position =
                orbitPosition;
        }
    }

    private float GetOrbitRotateSpeed(WeaponLevelData data)
    {
        if (data.ProjectileSpeed > 0f)
            return data.ProjectileSpeed;

        return data.BulletSpeed;
    }
}
