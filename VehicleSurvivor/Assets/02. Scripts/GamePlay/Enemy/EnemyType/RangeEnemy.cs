using UnityEngine;

public class RangedEnemy : Enemy
{
    [Header("Ranged Specifics")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float fireSpeed = 15f;

    
    protected override void Attack()
    {
        if (playerTarget == null) return;

        Vector3 fireDir = playerTarget.position - firePoint.position;
        fireDir.y = 0;

        GameObject bulletObj = Instantiate(bulletPrefab, firePoint.position, Quaternion.LookRotation(fireDir));

        EnemyBullet bullet = bulletObj.GetComponent<EnemyBullet>();
        if (bullet != null)
        {
            bullet.Initialize(fireDir, fireSpeed, 10f); // 임시 데미지 10
        }

        Debug.Log("원거리 공격 발사!");
    }
}