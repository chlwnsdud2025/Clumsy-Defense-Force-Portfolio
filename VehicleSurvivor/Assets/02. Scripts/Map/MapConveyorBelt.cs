using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class MapConveyorBelt : MonoBehaviour
{
    [SerializeField] private Vector3 moveDirection = Vector3.back;
    [SerializeField] private float beltSpeed = 5f;

    private readonly List<CharacterController> playersOnBelt = new List<CharacterController>();

    private void OnTriggerEnter(Collider other)
    {
        CharacterController controller = ResolvePlayerController(other);
        if (controller != null && !playersOnBelt.Contains(controller))
            playersOnBelt.Add(controller);
    }

    private void OnTriggerExit(Collider other)
    {
        CharacterController controller = ResolvePlayerController(other);
        if (controller != null)
            playersOnBelt.Remove(controller);
    }

    private void Update()
    {
        playersOnBelt.RemoveAll(controller => controller == null);

        if (playersOnBelt.Count == 0)
            return;

        Vector3 worldDirection = transform.TransformDirection(moveDirection.normalized);
        Vector3 movement = worldDirection * beltSpeed * Time.deltaTime;

        for (int i = 0; i < playersOnBelt.Count; i++)
            playersOnBelt[i].Move(movement);
    }

    private static CharacterController ResolvePlayerController(Collider other)
    {
        if (other == null)
            return null;

        Player player = other.GetComponentInParent<Player>();
        if (player == null && !other.CompareTag("Player"))
            return null;

        return other.GetComponentInParent<CharacterController>();
    }
}
