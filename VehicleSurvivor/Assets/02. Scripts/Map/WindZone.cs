using UnityEngine;
using System.Collections.Generic;

public class WindZone : MonoBehaviour
{
    [Header("트랙터 빔 설정")]
    [Tooltip("초당 위로 솟구치는 Y축 속도 (자연스럽게 튕겨나가려면 값을 10~15 정도로 올려보세요!)")]
    public float liftSpeed = 12f;

    [Tooltip("바람의 중심점으로부터 올라갈 수 있는 최대 높이(거리)")]
    public float maxDistance = 15f;

    private List<PlayerExternalMechanics> _playersInWind = new List<PlayerExternalMechanics>();

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerExternalMechanics mechanics = other.GetComponent<PlayerExternalMechanics>();
            if (mechanics != null && !_playersInWind.Contains(mechanics))
            {
                _playersInWind.Add(mechanics);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerExternalMechanics mechanics = other.GetComponent<PlayerExternalMechanics>();
            if (mechanics != null && _playersInWind.Contains(mechanics))
            {
                mechanics.RemoveWindForce();
                _playersInWind.Remove(mechanics);
            }
        }
    }

    private void Update()
    {
        _playersInWind.RemoveAll(player => player == null);

        if (_playersInWind.Count > 0)
        {
            // 리스트에서 요소를 삭제할 때는 역순(for문 뒤에서부터)으로 돌아야 에러가 안 납니다.
            for (int i = _playersInWind.Count - 1; i >= 0; i--)
            {
                PlayerExternalMechanics player = _playersInWind[i];
                float currentHeight = player.transform.position.y - transform.position.y;

                // 1. 아직 목표 높이에 도달하지 않았을 때 (바람으로 밀어올림)
                if (currentHeight < maxDistance)
                {
                    player.SetWindForce(liftSpeed);
                }
                // 2. 최대 높이(정점)를 돌파했을 때!
                else
                {
                    // [유저님 아이디어 적용] 
                    // 목표에 도달하면 윈드존이 강제로 멈추지 않고 바람 지원을 딱 끊어버립니다.
                    // 그러면 플레이어는 마지막으로 받던 가속도(liftSpeed)를 유지한 채 위로 붕~ 점프하게 됩니다!
                    player.RemoveWindForce();

                    // 더 이상 바람의 영향을 받지 않으므로 리스트에서 뺍니다.
                    _playersInWind.RemoveAt(i);
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, Vector3.up * maxDistance);
        Gizmos.DrawWireSphere(transform.position + (Vector3.up * maxDistance), 1f);
    }
}