using UnityEngine;
using System.Collections.Generic;

[DefaultExecutionOrder(-150)]
public class ConveyorBelt : MonoBehaviour
{
    [Header("컨베이어 벨트 설정")]
    [Tooltip("밀어낼 방향 (로컬 기준, 보통 Z나 -Z)")]
    public Vector3 moveDirection = Vector3.back;

    [Tooltip("밀려나는 속도")]
    public float beltSpeed = 5f;

    private readonly List<PlayerMotor> _playersOnBelt = new List<PlayerMotor>();

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerMotor motor = other.GetComponentInParent<PlayerMotor>();
            if (motor != null && !_playersOnBelt.Contains(motor))
                _playersOnBelt.Add(motor);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerMotor motor = other.GetComponentInParent<PlayerMotor>();
            if (motor != null && _playersOnBelt.Contains(motor))
                _playersOnBelt.Remove(motor);
        }
    }

    private void FixedUpdate()
    {
        _playersOnBelt.RemoveAll(player => player == null);

        if (_playersOnBelt.Count == 0)
            return;

        Vector3 worldDirection = transform.TransformDirection(moveDirection.normalized);
        Vector3 conveyorVelocity = worldDirection * beltSpeed;

        foreach (PlayerMotor player in _playersOnBelt)
            player.SetConveyorVelocity(conveyorVelocity);
    }
}
