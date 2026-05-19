using UnityEngine;
using System.Collections.Generic;

public class WindZone : MonoBehaviour
{
    [Header("트랙터 빔 (끌어올리기) 설정")]
    [Tooltip("밀어낼 로컬 축 방향 (예: 위로 올리려면 Y=1)")]
    public Vector3 pushAxis = Vector3.up;

    [Tooltip("밀어내는 속도")]
    public float pushSpeed = 10f;

    [Tooltip("바람의 중심점으로부터 밀어낼 최대 거리")]
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
                _playersInWind.Remove(mechanics);
            }
        }
    }

    private void LateUpdate() // 중력 계산 이후에 적용
    {
        _playersInWind.RemoveAll(player => player == null);

        if (_playersInWind.Count > 0)
        {
            Vector3 worldDirection = transform.TransformDirection(pushAxis.normalized);

            foreach (PlayerExternalMechanics player in _playersInWind)
            {
                Vector3 offset = player.transform.position - transform.position;
                float currentDistance = Vector3.Dot(offset, worldDirection);

                // 1. 아직 최대 거리에 도달하지 않았을 때 (끌어올리는 중)
                if (currentDistance < maxDistance)
                {
                    float distanceLeft = maxDistance - currentDistance;
                    float moveStep = pushSpeed * Time.deltaTime;

                    // 만약 이번 프레임에 이동할 거리가 남은 거리보다 크다면? (목표치 초과 방지)
                    if (moveStep >= distanceLeft)
                    {
                        // 남은 거리만큼만 딱 맞춰서 이동시키고, 중력을 정지시킴 (정점 도달)
                        Vector3 moveDelta = worldDirection * distanceLeft;
                        player.ApplyTractorBeam(moveDelta, true);
                    }
                    else
                    {
                        // 정상적으로 끌어올리는 중
                        Vector3 moveDelta = worldDirection * moveStep;
                        player.ApplyTractorBeam(moveDelta, false);
                    }
                }
                // 2. 이미 최대 거리(정점)에 도달해 있을 때
                else
                {
                    // 더 이상 위로 밀지 않고, 중력만 0으로 만들어 완벽하게 공중에 고정시킴
                    player.ApplyTractorBeam(Vector3.zero, true);
                }
            }
        }
    }

    // 에디터에서 바람이 끝나는 최대 거리(범위)를 보기 쉽게 파란색 선으로 그려줍니다.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 worldDirection = transform.TransformDirection(pushAxis.normalized);
        Gizmos.DrawRay(transform.position, worldDirection * maxDistance);
        Gizmos.DrawWireSphere(transform.position + worldDirection * maxDistance, 1f);
    }
}