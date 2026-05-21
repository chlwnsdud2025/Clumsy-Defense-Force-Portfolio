using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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
    [Tooltip("과일이 맵에 머무는 최대 시간 (이 시간이 지나면 풀로 자동 반환)")]
    public float fruitLifeTime = 10f;

    // 풀링 전용 변수들
    private Dictionary<GameObject, Queue<GameObject>> fruitPools = new Dictionary<GameObject, Queue<GameObject>>();
    private Transform poolParent;

    // 코루틴 메모리 쓰레기(GC) 방지용 캐싱
    private WaitForSeconds spawnWait;
    private WaitForSeconds lifeWait;

    void Start()
    {
        // 반복 생성되는 대기 시간을 미리 캐싱
        spawnWait = new WaitForSeconds(spawnInterval);
        lifeWait = new WaitForSeconds(fruitLifeTime);

        // 하이어라키를 깔끔하게 해줄 부모 빈 오브젝트 생성
        GameObject parentObj = new GameObject($"Fruit_Pool_Container_{gameObject.name}");
        poolParent = parentObj.transform;

        // [수정된 부분] 생성된 풀 부모를 이 스크립트가 달린 오브젝트(this)의 자식으로 쏙 집어넣습니다!
        poolParent.SetParent(this.transform);

        // 인스펙터에 등록된 과일 종류마다 빈 큐(Queue) 생성
        foreach (GameObject prefab in fruitPrefabs)
        {
            if (!fruitPools.ContainsKey(prefab))
            {
                fruitPools.Add(prefab, new Queue<GameObject>());
            }
        }

        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            GameObject selectedPrefab = fruitPrefabs[Random.Range(0, fruitPrefabs.Length)];
            GameObject spawnedFruit = GetFruitFromPool(selectedPrefab);

            // "반드시 꺼져있는 상태"에서 위치와 회전을 먼저 잡아야 물리 렉이 안 걸립니다.
            spawnedFruit.transform.position = transform.position;
            spawnedFruit.transform.rotation = Random.rotation;

            // 위치를 완벽히 잡은 후에 비로소 오브젝트를 켭니다.
            spawnedFruit.SetActive(true);

            // 물리 힘 가하기 (켜진 직후에 초기화)
            Rigidbody rb = spawnedFruit.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.AddForce(transform.forward * shootForce, ForceMode.Impulse);
                rb.AddTorque(new Vector3(Random.Range(-5f, 5f), Random.Range(-5f, 5f), Random.Range(-5f, 5f)), ForceMode.Impulse);
            }

            // 지정된 시간 뒤에 다시 풀로 돌려보내는 코루틴 실행
            StartCoroutine(ReturnToPoolAfterTime(spawnedFruit, selectedPrefab));

            // 캐싱된 WaitForSeconds 사용
            yield return spawnWait;
        }
    }

    // 풀에서 과일을 꺼내는 함수
    private GameObject GetFruitFromPool(GameObject prefabKey)
    {
        // 혹시라도 완전히 파괴된(Destroy) 껍데기가 큐에 있을까 봐 검사
        while (fruitPools[prefabKey].Count > 0)
        {
            GameObject obj = fruitPools[prefabKey].Dequeue();
            if (obj != null)
            {
                return obj;
            }
        }

        // 큐가 비어있다면 새로 생성하되, 생성 즉시 꺼서(SetActive(false)) 물리 연산을 막습니다.
        GameObject newObj = Instantiate(prefabKey, poolParent);
        newObj.SetActive(false);
        return newObj;
    }

    // 일정 시간 뒤에 Destroy 대신 풀로 반환하는 코루틴
    private IEnumerator ReturnToPoolAfterTime(GameObject obj, GameObject prefabKey)
    {
        yield return lifeWait;

        // 오브젝트가 살아있고, 현재 활성화되어 있을 때만 큐에 넣음
        if (obj != null && obj.activeSelf)
        {
            obj.SetActive(false);
            fruitPools[prefabKey].Enqueue(obj);
        }
    }
}