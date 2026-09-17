using UnityEngine;

/// <summary>
/// [측정용] EnemyManager.useManagedUpdate 를 끈 경우에만 부착된다.
/// Unity가 개체마다 Update()를 호출하던 '적용 전' 방식을 재현해 ManagedUpdate 와 비교한다.
/// EnemyManager.Update 뒤에 실행되도록 순서를 늦춰, 같은 프레임의 GameplayActive 값을 읽는다.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(50)]
public class EnemySelfUpdater : MonoBehaviour
{
    private Enemy enemy;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
    }

    private void Update()
    {
        EnemyManager manager = EnemyManager.Instance;
        if (manager == null || manager.useManagedUpdate || !manager.GameplayActive || !manager.IsRegistered(enemy))
            return;

        enemy.ManagedUpdate(Time.deltaTime);
    }
}
