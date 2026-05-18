using System.Collections.Generic;
using UnityEngine;

public class OrbitWeapon : WeaponBase
{
    [Header("회전 무기 설정")]
    public GameObject orbitPrefab;

    [Header("타격 설정")]
    public float hitCooldown = 0.3f;

    private readonly List<GameObject> orbitObjects =
        new List<GameObject>();

    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        SyncOrbitObjects(
            GetCurrentLevelData()
        );
    }

    protected override void Update()
    {
        base.Update();
        RotateOrbitObjects();//TODO : 이거 attack에 합쳐도 될듯?
    }

    protected override void Attack(
        WeaponLevelData data
    )
    {
        SyncOrbitObjects(data);
    }

    private void SyncOrbitObjects(
        WeaponLevelData data
    )
    {
        if (data == null)
            return;

        if (orbitPrefab == null)
            return;

        int targetCount =
            Mathf.Max(
                1,
                GetFinalProjectileCount(
                    data.projectileCount
                )
            );

        while (
            orbitObjects.Count
            < targetCount
        )
        {
            GameObject orbitObject =
                Instantiate(
                    orbitPrefab,
                    transform.position,
                    Quaternion.identity,
                    transform
                );

            OrbitHitbox hitbox =
                orbitObject
                .GetComponent
                <OrbitHitbox>();

            if (
                hitbox == null
            )
            {
                hitbox =
                    orbitObject
                    .AddComponent
                    <OrbitHitbox>();
            }

            hitbox.Init(
                GetFinalDamage(
                    data.damage
                ),
                hitCooldown
            );

            orbitObjects.Add(
                orbitObject
            );
        }

        while (
            orbitObjects.Count
            > targetCount
        )
        {
            GameObject removeTarget =
                orbitObjects[
                    orbitObjects.Count - 1
                ];

            orbitObjects.RemoveAt(
                orbitObjects.Count - 1
            );

            Destroy(
                removeTarget
            );
        }
    }

    private void RotateOrbitObjects()
    {
        WeaponLevelData data =
            GetCurrentLevelData();

        if (data == null)
            return;

        int count =
            orbitObjects.Count;

        if (count == 0)
            return;

        float angleStep =
            360f / count;

        float currentAngle =
            Time.time
            * data.bulletSpeed;

        for (
            int i = 0;
            i < count;
            i++
        )
        {
            if (
                orbitObjects[i]
                == null
            )
                continue;

            float angle =
                currentAngle
                + angleStep * i;

            float radian =
                angle
                * Mathf.Deg2Rad;

            Vector3 offset =
                new Vector3(
                    Mathf.Cos(radian),
                    0f,
                    Mathf.Sin(radian)
                )
                * data.radius;

            orbitObjects[i]
                .transform
                .position =
                transform.position
                + offset;
        }
    }
}

public class OrbitHitbox : MonoBehaviour
{
    private float damage;
    private float hitCooldown;

    private readonly Dictionary<Enemy, float>
        hitTimers =
        new Dictionary<Enemy, float>();

    public void Init(
        float newDamage,
        float newHitCooldown
    )
    {
        damage =
            newDamage;

        hitCooldown =
            newHitCooldown;
    }

    private void OnTriggerStay(
        Collider other
    )
    {
        if (
            !other.CompareTag(
                "Enemy"
            )
        )
            return;

        Enemy enemy =
            other
            .GetComponentInParent
            <Enemy>();

        if (
            enemy == null
        )
            return;

        if (
            hitTimers
            .ContainsKey(enemy)
        )
        {
            if (
                Time.time
                < hitTimers[enemy]
            )
                return;

            hitTimers[enemy] =
                Time.time
                + hitCooldown;
        }
        else
        {
            hitTimers.Add(
                enemy,
                Time.time
                + hitCooldown
            );
        }

        enemy.TakeDamage(
            damage
        );

        Debug.Log(
            $"[Orbit] {enemy.name} 피격 / 데미지: {damage}"
        );
    }
}