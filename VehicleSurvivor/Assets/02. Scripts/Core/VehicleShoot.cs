using UnityEngine;

public class VehicleShoot : MonoBehaviour
{
    public GameObject bulletPrefab;
    public Transform shootPoint;

    public float fireRate = 0.3f;
    private float fireTimer;

    void Update()
    {
        fireTimer += Time.deltaTime;

        if (fireTimer >= fireRate)
        {
            Instantiate(
                bulletPrefab,
                shootPoint.position,
                shootPoint.rotation
            );

            fireTimer = 0f;
        }
    }
}