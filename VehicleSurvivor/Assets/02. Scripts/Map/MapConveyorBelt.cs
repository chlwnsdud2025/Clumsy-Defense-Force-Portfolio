using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-150)]
public sealed class MapConveyorBelt : MonoBehaviour
{
    [SerializeField] private Vector3 moveDirection = Vector3.back;
    [SerializeField] private float beltSpeed = 5f;

    private readonly List<PlayerMotor> playersOnBelt = new List<PlayerMotor>();

    private void OnTriggerEnter(Collider other)
    {
        PlayerMotor motor = ResolvePlayerMotor(other);
        if (motor != null && !playersOnBelt.Contains(motor))
            playersOnBelt.Add(motor);
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerMotor motor = ResolvePlayerMotor(other);
        if (motor != null)
            playersOnBelt.Remove(motor);
    }

    private void FixedUpdate()
    {
        playersOnBelt.RemoveAll(motor => motor == null);

        if (playersOnBelt.Count == 0)
            return;

        Vector3 worldDirection = transform.TransformDirection(moveDirection.normalized);
        Vector3 conveyorVelocity = worldDirection * beltSpeed;

        for (int i = 0; i < playersOnBelt.Count; i++)
            playersOnBelt[i].SetConveyorVelocity(conveyorVelocity);
    }

    private static PlayerMotor ResolvePlayerMotor(Collider other)
    {
        if (other == null)
            return null;

        Player player = other.GetComponentInParent<Player>();
        if (player == null && !other.CompareTag("Player"))
            return null;

        return other.GetComponentInParent<PlayerMotor>();
    }
}
