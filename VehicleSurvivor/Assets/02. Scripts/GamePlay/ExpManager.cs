using System.Collections.Generic;
using UnityEngine;

public class ExpManager : MonoBehaviour
{
    public static ExpManager Instance;

    [Header("Prefabs")]
    public GameObject smallOrbPrefab;
    public GameObject mediumOrbPrefab;
    public GameObject largeOrbPrefab;

    [Header("Pool Settings")]
    public int smallPoolSize = 500;
    public int mediumPoolSize = 200;
    public int largePoolSize = 100;

    // [수정됨] 비활성화된(대기 중인) 구슬들
    private Dictionary<ExpType, Queue<ExpOrb>> orbPools;

    // [추가됨] 활성화된(필드에 있는) 구슬들의 순서를 추적하기 위한 연결 리스트
    // 큐(Queue)가 아닌 LinkedList를 쓰는 이유: 플레이어가 중간에 있는 구슬을 먹었을 때 O(1) 속도로 리스트에서 빼내기 위함입니다.
    private Dictionary<ExpType, LinkedList<ExpOrb>> activeOrbs;

    // [추가됨] 예비 생성을 위한 프리팹 및 부모 트랜스폼 캐싱
    private Dictionary<ExpType, GameObject> prefabDict;
    private Dictionary<ExpType, Transform> parentDict;

    void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    void Start()
    {
        InitializePools();
    }

    private void InitializePools()
    {
        orbPools = new Dictionary<ExpType, Queue<ExpOrb>>();
        activeOrbs = new Dictionary<ExpType, LinkedList<ExpOrb>>();
        prefabDict = new Dictionary<ExpType, GameObject>();
        parentDict = new Dictionary<ExpType, Transform>();

        // 각 타입별 풀 생성
        CreatePool(ExpType.Small, smallOrbPrefab, smallPoolSize);
        CreatePool(ExpType.Medium, mediumOrbPrefab, mediumPoolSize);
        CreatePool(ExpType.Large, largeOrbPrefab, largePoolSize);
    }

    private void CreatePool(ExpType type, GameObject prefab, int poolSize)
    {
        Queue<ExpOrb> pool = new Queue<ExpOrb>();
        LinkedList<ExpOrb> activeList = new LinkedList<ExpOrb>();

        GameObject poolParent = new GameObject(type.ToString() + " Pool");
        poolParent.transform.SetParent(this.transform);

        // 예비 생성을 위해 저장
        prefabDict.Add(type, prefab);
        parentDict.Add(type, poolParent.transform);

        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(prefab, poolParent.transform);
            obj.SetActive(false);

            ExpOrb orbScript = obj.GetComponent<ExpOrb>();
            orbScript.Setup(type, this);

            pool.Enqueue(orbScript);
        }

        orbPools.Add(type, pool);
        activeOrbs.Add(type, activeList); // 활성 리스트도 초기화
    }

    public void SpawnExp(Vector3 position, int expAmount)
    {
        ExpType typeToDrop = ExpType.Small;
        if (expAmount >= 50) typeToDrop = ExpType.Large;
        else if (expAmount >= 10) typeToDrop = ExpType.Medium;

        ExpOrb orb = null;

        // 측정용: 보조 풀 우회 → 매번 생성
        if (BenchmarkFlags.BypassAuxPools)
        {
            BenchmarkFlags.AuxInstantiateCount++;
            GameObject fresh = Instantiate(prefabDict[typeToDrop], parentDict[typeToDrop]);
            orb = fresh.GetComponent<ExpOrb>();
            orb.Setup(typeToDrop, this);
        }
        // 1. 대기 중인 풀이 있는 경우
        else if (orbPools[typeToDrop].Count > 0)
        {
            orb = orbPools[typeToDrop].Dequeue();
        }
        else
        {
            // 2. 풀이 바닥났을 경우: 가장 먼저 활성화된(가장 오래된) 구슬을 재사용!
            if (activeOrbs[typeToDrop].Count > 0)
            {
                orb = activeOrbs[typeToDrop].First.Value;

                // 기존 활성 리스트에서 제거하고 오브젝트를 잠시 끔
                activeOrbs[typeToDrop].RemoveFirst();
                orb.gameObject.SetActive(false);
            }
            else
            {
                // 3. 예외 상황 (이론상 불가능하지만 활성 구슬마저 없다면 예비로 새로 생성)
                GameObject obj = Instantiate(prefabDict[typeToDrop], parentDict[typeToDrop]);
                orb = obj.GetComponent<ExpOrb>();
                orb.Setup(typeToDrop, this);
            }
        }

        // 구슬을 활성 리스트의 '맨 뒤(가장 최신)'에 추가하고, 자기 자신의 노드 위치를 기억하게 함
        orb.activeNode = activeOrbs[typeToDrop].AddLast(orb);

        // 구슬 스폰
        orb.Spawn(position, expAmount);
    }

    // [추가됨] 구슬이 수명을 다하거나 플레이어에게 먹혀서 풀로 돌아갈 때
    public void ReturnToPool(ExpOrb orb)
    {
        if (orb == null || !orb.gameObject.activeSelf) return;

        orb.gameObject.SetActive(false);

        // 활성화 리스트에서 제거 (자신의 노드를 알고 있으므로 탐색 없이 초고속으로 삭제됨)
        if (orb.activeNode != null && orb.activeNode.List != null)
        {
            orb.activeNode.List.Remove(orb.activeNode);
            orb.activeNode = null;
        }

        if (BenchmarkFlags.BypassAuxPools) { BenchmarkFlags.AuxDestroyCount++; Destroy(orb.gameObject); return; } // 측정용

        // 다시 대기 풀에 넣기
        orbPools[orb.orbType].Enqueue(orb);
    }
}