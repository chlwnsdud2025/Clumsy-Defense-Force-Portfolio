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
    public float heightOffset = 1f;

    [Tooltip("SO의 스피드 스탯에 곱해질 실제 회전 속도 배율입니다. (추천: 30 ~ 50)")]
    public float rotateSpeedMultiplier = 50f; // 🌟 [추가됨] 시원시원한 회전을 위한 배율

    private readonly List<OrbitObject> orbitObjects =
        new List<OrbitObject>();

    private GameObject orbitAnchor;

    protected override void OnInitialized()
    {
        CreateOrbitAnchor();
        SyncOrbitObjects(GetCurrentLevelData());
    }

    protected override void OnLevelDataChanged(WeaponLevelData data)
    {
        SyncOrbitObjects(data);
    }

    protected override void Update()
    {
        base.Update();
    }

    private void LateUpdate()
    {
        TrackAndRotateAnchor();
    }

    protected override void Attack(WeaponLevelData data)
    {
        SyncOrbitObjects(data);
    }

    private void CreateOrbitAnchor()
    {
        if (orbitAnchor != null)
            return;

        orbitAnchor = new GameObject($"[{weaponName}_OrbitAnchor]");
        orbitAnchor.transform.position = transform.position;
    }

    private void TrackAndRotateAnchor()
    {
        WeaponLevelData data = GetCurrentLevelData();
        if (data == null || orbitAnchor == null)
            return;

        Vector3 targetPosition = transform.position;
        targetPosition.y += heightOffset;
        orbitAnchor.transform.position = targetPosition;

        float rotateSpeed = GetOrbitRotateSpeed(data);
        orbitAnchor.transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
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

        CreateOrbitAnchor();

        int targetCount = Mathf.Max(
            1,
            GetFinalProjectileCount(data.ProjectileCount)
        );

        DamageInfo damageInfo = GetFinalDamage(data.Damage);
        float radius = GetFinalWeaponScale(data.Radius);

        while (orbitObjects.Count < targetCount)
        {
            // [복구됨] 인스펙터에 설정한 orbitRotationOffset이 정상적으로 적용되도록 수정
            GameObject orbitObject = Instantiate(
                orbitObjectPrefab,
                orbitAnchor.transform.position,
                orbitObjectPrefab.transform.rotation * Quaternion.Euler(orbitRotationOffset),
                orbitAnchor.transform
            );

            OrbitObject orbit = orbitObject.GetComponent<OrbitObject>();
            if (orbit == null)
            {
                orbit = orbitObject.AddComponent<OrbitObject>();
            }

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

        float angleStep = 360f / targetCount;
        for (int i = 0; i < orbitObjects.Count; i++)
        {
            if (orbitObjects[i] == null)
                continue;

            orbitObjects[i].Initialize(damageInfo, data.Cooldown);

            float angle = angleStep * i;
            float radian = angle * Mathf.Deg2Rad;

            orbitObjects[i].transform.localPosition = new Vector3(
                Mathf.Cos(radian),
                0f,
                Mathf.Sin(radian)
            ) * radius;
        }
    }

    // 🌟 [핵심 수정] 가져온 SO 스탯 값에 배율(Multiplier)을 곱해줍니다.
    private float GetOrbitRotateSpeed(WeaponLevelData data)
    {
        float baseSpeed = data.ProjectileSpeed > 0f ? data.ProjectileSpeed : data.BulletSpeed;
        return baseSpeed * rotateSpeedMultiplier;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (orbitAnchor != null)
        {
            Destroy(orbitAnchor);
        }
        orbitObjects.Clear();
    }
}