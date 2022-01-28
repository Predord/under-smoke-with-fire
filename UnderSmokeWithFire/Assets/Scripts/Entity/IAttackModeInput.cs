using UnityEngine;

public interface IAttackModeInput
{
    bool AttackMode { get; }
    void OnAttackModeIterationEnd();
    Vector3 RotationDirection();
}
