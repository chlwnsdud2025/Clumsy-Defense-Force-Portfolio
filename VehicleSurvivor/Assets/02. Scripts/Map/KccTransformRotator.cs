using KinematicCharacterController;
using UnityEngine;

[DefaultExecutionOrder(-200)]
public class KccTransformRotator : MonoBehaviour
{
    public enum RotationAxis
    {
        X,
        Y,
        Z
    }

    public RotationAxis rotationAxis = RotationAxis.Y;
    public float rotationSpeed = 50f;

    private void FixedUpdate()
    {
        Vector3 axis = rotationAxis switch
        {
            RotationAxis.X => Vector3.right,
            RotationAxis.Y => Vector3.up,
            RotationAxis.Z => Vector3.forward,
            _ => Vector3.up
        };

        transform.Rotate(axis, rotationSpeed * Time.fixedDeltaTime, Space.Self);
    }
}
