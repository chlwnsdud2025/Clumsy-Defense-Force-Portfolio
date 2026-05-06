using UnityEngine;

public class Shoot : MonoBehaviour
{
    public float speed = 10f;

    private Vector3 starPos;

    void Start()
    {
        starPos = transform.position;
    }

    void Update()
    {
        //앞으로 이동
        transform.position += transform.forward * speed * Time.deltaTime;
        //20이상 날아가면 삭제
        float distance = Vector3.Distance(starPos, transform.position);

        if(distance >= 20f)
        {
            Destroy(gameObject);
        }
        
    }

}
