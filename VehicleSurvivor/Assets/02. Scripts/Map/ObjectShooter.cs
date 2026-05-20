using UnityEngine;
using System.Collections;

public class ObjectShooter : MonoBehaviour
{
    [Header("발사기 설정")]
    [Tooltip("발사할 과일 프리팹들 (여러 개 넣으면 랜덤 발사)")]
    public GameObject[] fruitPrefabs;

    [Tooltip("과일을 발사하는 주기 (초)")]
    public float spawnInterval = 3f;

    [Tooltip("발사할 때 앞으로 뱉어내는 힘")]
    public float shootForce = 15f;

    [Header("최적화 설정")]
    [Tooltip("과일이 맵에 머무는 최대 시간 (이 시간이 지나면 자동 소멸)")]
    public float fruitLifeTime = 10f; // 10초 뒤 자동 삭제

    void Start()
    {
        // 게임 시작과 동시에 코루틴(반복 발사) 시작
        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            // 랜덤 과일 하나 선택
            GameObject prefabToSpawn = fruitPrefabs[Random.Range(0, fruitPrefabs.Length)];

            // 대포 위치에 과일 생성
            GameObject spawnedFruit = Instantiate(prefabToSpawn, transform.position, Random.rotation);

            Destroy(spawnedFruit, fruitLifeTime);

            // Rigidbody를 찾아서 대포가 바라보는 방향(보통 아래쪽 경사 방향)으로 쏴줍니다!
            Rigidbody rb = spawnedFruit.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // 약간의 회전(Torque)을 섞어주면 훨씬 자연스럽게 굴러갑니다.
                rb.AddForce(transform.forward * shootForce, ForceMode.Impulse);
                rb.AddTorque(new Vector3(Random.Range(-5f, 5f), Random.Range(-5f, 5f), Random.Range(-5f, 5f)), ForceMode.Impulse);
            }

            // 다음 발사까지 대기
            yield return new WaitForSeconds(spawnInterval);
        }
    }
}