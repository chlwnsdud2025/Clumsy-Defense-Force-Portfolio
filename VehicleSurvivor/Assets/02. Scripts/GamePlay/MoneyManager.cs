using System.Collections.Generic;
using UnityEngine;

public class MoneyManager : MonoBehaviour
{
    public static MoneyManager Instance;

    [Header("Prefabs & Settings")]
    [Tooltip("돈(코인/지폐 등) 프리팹을 연결하세요.")]
    public GameObject moneyPrefab;
    public int poolSize = 100;

    private Queue<MoneyItem> moneyPool;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        InitializePool();
    }

    private void InitializePool()
    {
        moneyPool = new Queue<MoneyItem>();
        GameObject poolParent = new GameObject("Money Pool");
        poolParent.transform.SetParent(this.transform);

        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(moneyPrefab, poolParent.transform);
            obj.SetActive(false);

            MoneyItem moneyScript = obj.GetComponent<MoneyItem>();
            moneyScript.Setup(this);

            moneyPool.Enqueue(moneyScript);
        }
    }

    // 몹/보스가 죽을 때 호출할 함수
    public void SpawnMoney(Vector3 position, int amount, Transform playerTransform)
    {
        if (moneyPool.Count > 0)
        {
            MoneyItem coin = moneyPool.Dequeue();
            coin.Spawn(position, amount, playerTransform);
        }
    }

    public void ReturnToPool(MoneyItem coin)
    {
        coin.gameObject.SetActive(false);
        moneyPool.Enqueue(coin);
    }
}