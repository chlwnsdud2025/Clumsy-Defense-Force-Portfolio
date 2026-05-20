using System.Collections.Generic;
using UnityEngine;

public class OrbitWeapon : WeaponBase
{
    [Header("Orbit Data")]
    public OrbitWeaponDataSO orbitData;

    [Header("Legacy Stats")]
    public float damage = 10f;
    public float radius = 3f;
    public float rotateSpeed = 120f;
    public int orbitCount = 1;
    public float hitCooldown = 0.3f;

    private readonly List<GameObject> orbitObjects = new List<GameObject>();

    private float currentDamage;
    private float currentRadius;
    private float currentRotateSpeed;
    private int currentOrbitCount;
    private float currentHitCooldown;

    private void Start()
    {
        ApplyOrbitSettings(GetCurrentLevelData());
        SyncOrbitObjects();
    }

    protected override void Update()
    {
        RotateOrbitObjects();
    }

    protected override void OnLevelDataChanged(WeaponLevelData data)
    {
        ApplyOrbitSettings(data);
        SyncOrbitObjects();
    }

    private void ApplyOrbitSettings(WeaponLevelData data)
    {
        if (data == null)
        {
            currentDamage = GetFinalDamage(damage);
            currentRadius = radius;
            currentRotateSpeed = rotateSpeed;
            currentOrbitCount = Mathf.Max(1, GetFinalProjectileCount(orbitCount));
            currentHitCooldown = hitCooldown;
            return;
        }

        currentDamage = GetFinalDamage(data.Damage);
        currentRadius = data.Range > 0f ? data.Range : data.radius;
        currentRotateSpeed = data.ProjectileSpeed > 0f ? data.ProjectileSpeed : rotateSpeed;
        currentOrbitCount = Mathf.Max(1, GetFinalProjectileCount(data.ProjectileCount));
        currentHitCooldown = data.TickInterval > 0f ? data.TickInterval : hitCooldown;
    }

    private void SyncOrbitObjects()
    {
        if (orbitData == null || orbitData.orbitPrefab == null)
            return;

        int targetCount = Mathf.Max(1, currentOrbitCount);

        while (orbitObjects.Count < targetCount)
        {
            GameObject orbitObject = Instantiate(
                orbitData.orbitPrefab,
                transform.position,
                Quaternion.identity,
                transform
            );

            OrbitHitbox hitbox = orbitObject.GetComponent<OrbitHitbox>();

            if (hitbox == null)
            {
                hitbox = orbitObject.AddComponent<OrbitHitbox>();
            }

            orbitObjects.Add(orbitObject);
        }

        while (orbitObjects.Count > targetCount)
        {
            GameObject removeTarget = orbitObjects[orbitObjects.Count - 1];
            orbitObjects.RemoveAt(orbitObjects.Count - 1);
            Destroy(removeTarget);
        }

        RefreshOrbitHitboxes();
    }

    private void RefreshOrbitHitboxes()
    {
        for (int i = 0; i < orbitObjects.Count; i++)
        {
            GameObject orbitObject = orbitObjects[i];

            if (orbitObject == null)
                continue;

            OrbitHitbox hitbox = orbitObject.GetComponent<OrbitHitbox>();

            if (hitbox == null)
                continue;

            hitbox.Init(
                currentDamage,
                currentHitCooldown,
                orbitData.hitEffectPrefab
            );
        }
    }

    private void RotateOrbitObjects()
    {
        if (orbitObjects.Count == 0)
            return;

        int count = orbitObjects.Count;
        float angleStep = 360f / count;
        float currentAngle = Time.time * currentRotateSpeed;

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
            ) * currentRadius;

            orbitObjects[i].transform.position = transform.position + offset;
        }
    }
}
