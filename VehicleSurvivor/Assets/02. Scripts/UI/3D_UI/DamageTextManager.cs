using System.Collections.Generic;
using UnityEngine;

public class DamageTextPool : MonoBehaviour
{
    public static DamageTextPool Instance; // 싱글톤 접근용

    [Header("Pool Settings")]
    public GameObject damageTextPrefab; // 아까 만든 프리팹을 넣을 곳
    public int poolSize = 100;          // 미리 생성해둘 개수

    // 풀링된 오브젝트들을 담아둘 큐(Queue)
    private Queue<GameObject> poolQueue = new Queue<GameObject>();

    void Awake()
    {
        // 싱글톤 초기화
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // 게임 시작 시 poolSize만큼 미리 생성해서 큐에 넣기
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(damageTextPrefab, transform); // 매니저의 자식으로 생성해서 정리
            obj.SetActive(false); // 일단 화면에 안 보이게 비활성화
            poolQueue.Enqueue(obj); // 큐에 집어넣기
        }
    }

    // 텍스트가 필요할 때 꺼내 쓰는 함수
    public GameObject GetDamageText()
    {
        // 측정용: 보조 풀 우회 → 매번 생성
        if (BenchmarkFlags.BypassAuxPools)
        {
            BenchmarkFlags.AuxInstantiateCount++;
            GameObject fresh = Instantiate(damageTextPrefab, transform);
            fresh.SetActive(true);
            return fresh;
        }

        if (poolQueue.Count > 0)
        {
            GameObject obj = poolQueue.Dequeue(); // 큐에서 하나 빼기
            obj.SetActive(true); // 활성화
            return obj;
        }
        else
        {
            // 만약 100개를 다 꺼내 썼는데 또 필요하다면? -> 새로 생성해서 줌 (유연한 대처)
            GameObject obj = Instantiate(damageTextPrefab, transform);
            obj.SetActive(true);
            return obj;
        }
    }

    // 텍스트 연출이 끝나고 다시 풀로 돌려보내는 함수
    public void ReturnToPool(GameObject obj)
    {
        if (BenchmarkFlags.BypassAuxPools) { BenchmarkFlags.AuxDestroyCount++; Destroy(obj); return; } // 측정용
        obj.SetActive(false); // 비활성화
        poolQueue.Enqueue(obj); // 다시 큐에 집어넣기
    }
}