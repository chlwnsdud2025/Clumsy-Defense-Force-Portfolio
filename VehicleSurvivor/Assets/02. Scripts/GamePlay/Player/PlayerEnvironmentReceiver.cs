using UnityEngine;

[RequireComponent(typeof(PlayerMotor))]
public class PlayerEnvironmentReceiver : MonoBehaviour
{
    private PlayerMotor _motor;

    private void Awake()
    {
        _motor = GetComponent<PlayerMotor>();
    }

    // 파라미터 2개(resetYVelocity, resetXZVelocity)가 추가되었습니다!
    public void ApplyTargetHeightJump(float targetHeight, float horizontalForce, Vector3 direction, bool resetYVelocity = true, bool resetXZVelocity = false)
    {
        if (_motor == null)
            return;

        _motor.LaunchJump(targetHeight, horizontalForce, direction, resetYVelocity, resetXZVelocity);
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