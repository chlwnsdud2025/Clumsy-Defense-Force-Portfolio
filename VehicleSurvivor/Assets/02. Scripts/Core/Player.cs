using UnityEngine;

public class Player : MonoBehaviour
{
    public float movespeed = 5;
    public GameObject shootPrefeb;
    public Transform shootPoint;

    public float fireRate = 0.5f;
    private float fireTimer;

    private void Update()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        Vector3 moveDir = new Vector3(x, 0f, z).normalized;

        transform.position += moveDir * movespeed * Time.deltaTime;
        
        if(moveDir != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(moveDir);  
        }

        fireTimer += Time.deltaTime;
        if(fireTimer >= fireRate)
        {
            Instantiate(shootPrefeb, shootPoint.position, transform.rotation);

            fireTimer = 0f;
        }
    }
}
