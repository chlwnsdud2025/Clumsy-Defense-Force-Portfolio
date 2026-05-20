using UnityEngine;
using System.Collections.Generic;

public class ConveyorBelt : MonoBehaviour
{
    [Header("컨베이어 벨트 설정")]
    [Tooltip("밀어낼 방향 (로컬 기준, 보통 Z나 -Z)")]
    public Vector3 moveDirection = Vector3.back;

    [Tooltip("밀려나는 속도")]
    public float beltSpeed = 5f;

    private List<CharacterController> _playersOnBelt = new List<CharacterController>();

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            CharacterController cc = other.GetComponent<CharacterController>();
            if (cc != null && !_playersOnBelt.Contains(cc))
            {
                _playersOnBelt.Add(cc);
            }
        }
        // (선택) 적(Enemy)도 밀려나게 하려면 여기에 적 추가 로직 작성
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            CharacterController cc = other.GetComponent<CharacterController>();
            if (cc != null && _playersOnBelt.Contains(cc))
            {
                _playersOnBelt.Remove(cc);
            }
        }
    }

    private void Update()
    {
        _playersOnBelt.RemoveAll(player => player == null);

        if (_playersOnBelt.Count > 0)
        {
            // 컨베이어 벨트가 향하는 월드 방향 계산
            Vector3 worldDirection = transform.TransformDirection(moveDirection.normalized);
            Vector3 movement = worldDirection * beltSpeed * Time.deltaTime;

            foreach (CharacterController player in _playersOnBelt)
            {
                // 외부 관성(ExternalImpact)을 건드리지 않고 직접 Move를 호출!
                // 이렇게 하면 미끄러지는 느낌 없이 아주 정직하게 벨트 속도만큼만 밀려납니다.
                player.Move(movement);
            }
        }
    }
}