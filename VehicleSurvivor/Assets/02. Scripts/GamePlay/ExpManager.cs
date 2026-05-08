using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.Universal;

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

    private Dictionary<ExpType, Queue<ExpOrb>> orbPools;

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

        // 각 타입별 풀 생성
        CreatePool(ExpType.Small, smallOrbPrefab, smallPoolSize);
        CreatePool(ExpType.Medium, mediumOrbPrefab, mediumPoolSize);
        CreatePool(ExpType.Large, largeOrbPrefab, largePoolSize);
    }

    private void CreatePool(ExpType type, GameObject prefab, int poolSize)
    {
        Queue<ExpOrb> pool = new Queue<ExpOrb>();

        GameObject poolParent = new GameObject(type.ToString() + " Pool");
        poolParent.transform.SetParent(this.transform);

        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(prefab, poolParent.transform);
            obj.SetActive(false);

            ExpOrb orbScript = obj.GetComponent<ExpOrb>();
            orbScript.Setup(type, this); // 구슬에게 타입과 매니저를 알려줌

            pool.Enqueue(orbScript);
        }

        orbPools.Add(type, pool); // 딕셔너리에 완성된 풀 추가
    }

    public void SpawnExp(ExpType type, Vector3 position)
    {
        // 해당 타입의 풀에 여유가 있다면
        if (orbPools[type].Count > 0)
        {
            ExpOrb orb = orbPools[type].Dequeue();
            orb.Spawn(position);
        }
        else
        {
            // 맵에 있는 가장 먼 구슬을 당겨오게 만들예정
        }
    }

    public void ReturnToPool(ExpOrb orb)
    {
        orb.gameObject.SetActive(false);
        orbPools[orb.orbType].Enqueue(orb); 
    }
}