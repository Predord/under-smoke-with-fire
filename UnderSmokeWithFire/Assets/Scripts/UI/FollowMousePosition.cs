using UnityEngine;

public class FollowMousePosition : MonoBehaviour
{
    private Transform _transform;

    private void Awake()
    {
        _transform = transform;
    }

    private void LateUpdate()
    {
        _transform.position = GameManager.Instance.mouse.position.ReadValue();
    }
}
