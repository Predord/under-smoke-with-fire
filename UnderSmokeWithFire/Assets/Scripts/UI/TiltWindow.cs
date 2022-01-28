using UnityEngine;

public class TiltWindow : MonoBehaviour
{
    public Vector2 range = new Vector2(5f, 3f);

	private Transform _transform;
	private Quaternion _rotationStart;
	private Vector2 _rotationCurrent = Vector2.zero;

	private void Start()
	{
		_transform = transform;
		_rotationStart = _transform.localRotation;
	}

	private void Update()
	{
		Vector2 pos = GameManager.Instance.mouse.position.ReadValue();

		float halfWidth = Screen.width * 0.5f;
		float halfHeight = Screen.height * 0.5f;
		float x = Mathf.Clamp((pos.x - halfWidth) / halfWidth, -1f, 1f);
		float y = Mathf.Clamp((pos.y - halfHeight) / halfHeight, -1f, 1f);
		_rotationCurrent = Vector2.Lerp(_rotationCurrent, new Vector2(x, y), Time.deltaTime * 5f);

		_transform.localRotation = _rotationStart * Quaternion.Euler(-_rotationCurrent.y * range.y, _rotationCurrent.x * range.x, 0f);
	}
}
