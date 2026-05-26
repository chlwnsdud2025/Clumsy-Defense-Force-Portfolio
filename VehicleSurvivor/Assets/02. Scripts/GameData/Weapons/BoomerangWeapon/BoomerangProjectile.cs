using System.Collections.Generic;
using UnityEngine;

public class BoomerangProjectile : MonoBehaviour
{
    [Header("부메랑 데이터")]
    public float speed;
    public float maxDistance;

    private DamageInfo damageInfo;

    [Header("회전 연출")]
    public float rotateSpeed = 720f;

    private Transform owner;
    private Vector3 startPosition;
    private Vector3 moveDirection;
    private bool isReturning;

    private LayerMask enemyLayer;

    private readonly HashSet<Enemy> hitEnemies =
        new HashSet<Enemy>();

    public void Init(
        Transform newOwner,
        DamageInfo newDamageInfo,
        float newSpeed,
        float newMaxDistance,
        LayerMask newEnemyLayer
    )
    {
        owner = newOwner;
        damageInfo = newDamageInfo;
        speed = newSpeed;
        maxDistance = newMaxDistance;
        enemyLayer = newEnemyLayer;

        startPosition = transform.position;
        moveDirection = transform.forward;
    }

    private void Update()
    {
        RotateVisual();
        Move();
    }

    private void Move()
    {
        if (owner == null)
        {
            Destroy(gameObject);
            return;
        }

        if (!isReturning)
        {
            transform.position +=
                moveDirection * speed * Time.deltaTime;

            float distance =
                Vector3.Distance(startPosition, transform.position);

            if (distance >= maxDistance)
            {
                isReturning = true;
            }
        }
        else
        {
            Vector3 directionToOwner =
                owner.position - transform.position;

            directionToOwner.y = 0f;

            if (directionToOwner.magnitude <= 0.5f)
            {
                Destroy(gameObject);
                return;
            }

            transform.position +=
                directionToOwner.normalized * speed * Time.deltaTime;

            transform.rotation =
                Quaternion.LookRotation(directionToOwner.normalized);
        }
    }

    private void RotateVisual()
    {
        transform.Rotate(
            Vector3.up,
            rotateSpeed * Time.deltaTime,
            Space.Self
        );
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Enemy"))
            return;

        Enemy enemy = other.GetComponentInParent<Enemy>();

        if (enemy == null)
            return;

        if (hitEnemies.Contains(enemy))
            return;

        enemy.TakeDamage(damageInfo);
        hitEnemies.Add(enemy);

        Debug.Log($"[Boomerang] {enemy.name} 피격 / 데미지: {damageInfo.Amount}");
    }
}