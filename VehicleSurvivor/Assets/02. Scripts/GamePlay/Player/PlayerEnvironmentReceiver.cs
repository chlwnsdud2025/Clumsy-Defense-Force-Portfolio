using UnityEngine;

[RequireComponent(typeof(PlayerMotor))]
public class PlayerEnvironmentReceiver : MonoBehaviour
{
    private PlayerMotor _motor;

    private void Awake()
    {
        _motor = GetComponent<PlayerMotor>();
    }

    public void ApplyTargetHeightJump(float targetHeight, float horizontalForce, Vector3 direction)
    {
        if (_motor == null)
            return;

        _motor.LaunchJump(targetHeight, horizontalForce, direction);
    }

    public void SetWindForce(float liftSpeed)
    {
        if (_motor == null)
            return;

        _motor.SetWindForce(liftSpeed);
    }

    public void RemoveWindForce()
    {
        if (_motor == null)
            return;

        _motor.RemoveWindForce();
    }
}
