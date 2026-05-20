using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BumperCar : MonoBehaviour
{
    [Header("범퍼카 설정")]
    public float moveSpeed = 15f;
    public float knockbackForce = 25f;
    public float knockbackHeight = 3f;

    [Header("경기장 이탈 방지 (투명 벽)")]
    [Tooltip("경기장의 중심 좌표")]
    public Vector3 arenaCenter = Vector3.zero;

    [Tooltip("범퍼카가 튕겨 나올 경기장의 반지름(크기)")]
    public float arenaRadius = 20f;

    private Rigidbody _rb;
    private float _lastHitTime = 0f;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        Vector3 randomDir = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
        _rb.linearVelocity = randomDir * moveSpeed;
    }

    void FixedUpdate()
    {
        Vector2 currentPos2D = new Vector2(transform.position.x, transform.position.z);
        Vector2 centerPos2D = new Vector2(arenaCenter.x, arenaCenter.z);
        float distanceFromCenter = Vector2.Distance(currentPos2D, centerPos2D);

        // 1. 경기장 밖으로 나갔는지 검사 (투명 벽 충돌)
        if (distanceFromCenter >= arenaRadius)
        {
            // [거미줄 해결] 중심에서 차를 향하는 방향을 구한 뒤, 
            // 반지름보다 '0.5'만큼 안쪽으로 차를 강제로 끌어당겨서 무한 루프를 막습니다.
            Vector2 dirFromCenter2D = (currentPos2D - centerPos2D).normalized;
            Vector2 safePos2D = centerPos2D + dirFromCenter2D * (arenaRadius - 0.5f);

            transform.position = new Vector3(safePos2D.x, transform.position.y, safePos2D.y);

            // [당구공 쿠션 반사 로직]
            // 벽의 수직 방향(Normal, 중심을 향하는 방향)을 구합니다.
            Vector3 wallNormal = new Vector3(-dirFromCenter2D.x, 0, -dirFromCenter2D.y);

            // 현재 속도와 벽의 수직 방향을 계산해 '입사각/반사각'으로 자연스럽게 튕겨냅니다.
            Vector3 reflectedDir = Vector3.Reflect(_rb.linearVelocity.normalized, wallNormal);
            reflectedDir.y = 0f; // 튕길 때 위로 솟구치는 것 방지

            _rb.linearVelocity = reflectedDir.normalized * moveSpeed;
        }
        // 2. 벽에 닿지 않았을 때의 일반 주행 로직
        else
        {
            Vector3 currentVelocity = _rb.linearVelocity;
            Vector3 flatVelocity = new Vector3(currentVelocity.x, 0f, currentVelocity.z);

            // 정면 충돌 등으로 차가 멈추면 새로운 무작위 방향으로 재출발
            if (flatVelocity.magnitude < 0.1f)
            {
                flatVelocity = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
            }

            flatVelocity = flatVelocity.normalized * moveSpeed;
            _rb.linearVelocity = new Vector3(flatVelocity.x, currentVelocity.y, flatVelocity.z);
        }

        // 차가 이동하는 방향을 자연스럽게 바라보게 회전
        if (_rb.linearVelocity.sqrMagnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(new Vector3(_rb.linearVelocity.x, 0, _rb.linearVelocity.z));
            _rb.rotation = Quaternion.Slerp(_rb.rotation, targetRotation, Time.fixedDeltaTime * 10f);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 플레이어 타격 로직 (이전과 동일)
        if (other.CompareTag("Player") && Time.time >= _lastHitTime + 0.5f)
        {
            PlayerExternalMechanics mechanics = other.GetComponent<PlayerExternalMechanics>();
            if (mechanics != null)
            {
                _lastHitTime = Time.time;
                Vector3 pushDirection = other.transform.position - transform.position;
                pushDirection.y = 0f;
                mechanics.ApplyTargetHeightJump(knockbackHeight, knockbackForce, pushDirection.normalized);
            }
        }
    }

    // 에디터에서 투명 벽의 크기(반지름)를 보기 쉽게 그려줍니다!
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(arenaCenter, arenaRadius);
    }
}