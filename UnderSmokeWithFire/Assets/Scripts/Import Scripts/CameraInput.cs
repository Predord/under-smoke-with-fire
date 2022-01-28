using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraInput : MonoBehaviour
{
    [Header("Camera Position")]
    public Vector2 offset;

    public float startingZoomLevel;
    public float rotateMovementSpeed;
    public float screenMoveSpeed;

    private bool isMovingToPosition;
    private bool isRotating;
    private bool isTopDownView;
    private Camera _camera;
    private Rigidbody _rigidBody;
    private float mousePositionOnRotateStart;
    private float currentZoomLevel;
    private float minZoom = -12f;
    private float maxZoom = -4f;
    private Coroutine moveCamera;
    private Coroutine rotateCamera;
    private Vector2 MoveDirection;
    private Vector3 normalisedCameraPosition;
    private Vector3 moveToPosition;
    private Transform _transform;

    private CameraMain _cameraMain;

    public static bool Locked
    {
        set
        {
            locked = value;
        }
    }

    private static bool locked;

    private void Awake()
    {
        _cameraMain = GetComponent<CameraMain>();
    }

    private void Start()
    {
        _transform = transform;
        _rigidBody = GetComponent<Rigidbody>();
        _camera = GetComponentInChildren<Camera>();
        normalisedCameraPosition = new Vector3(
            0f,
            Mathf.Abs(offset.y),
            -Mathf.Abs(offset.x)).normalized;
        currentZoomLevel = startingZoomLevel;
        PositionCamera();
    }

    public void OnCameraScroll(InputAction.CallbackContext context)
    {
        if (context.performed && !isTopDownView && !locked && (!Player.Instance || !Player.Instance.playerInput.AttackMode))
        {
            float value = context.ReadValue<float>() / 120f;

            if (value > 0f)
            {
                if (currentZoomLevel <= minZoom) return;
                currentZoomLevel = Mathf.Max(currentZoomLevel - value, minZoom);
                PositionCamera();
            }
            else if (value < 0f)
            {
                if (currentZoomLevel >= maxZoom) return;
                currentZoomLevel = Mathf.Min(currentZoomLevel - value, maxZoom);
                PositionCamera();
            }
        }
    }

    public void OnTopDownViewButton(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (!isTopDownView)
            {
                currentZoomLevel = startingZoomLevel;
                PositionCamera();
                _transform.rotation = Quaternion.Euler(45f, 0f, 0f);
                rotateCamera = null;

                isTopDownView = true;
            }
            else
            {
                _transform.rotation = Quaternion.identity;

                isTopDownView = false;
            }
        }
    }

    public void OnCameraRotate(InputAction.CallbackContext context)
    {
        if (context.performed && !locked)
        {
            isRotating = !isRotating;
            mousePositionOnRotateStart = GameManager.Instance.mouse.position.ReadValue().x;

            if (rotateCamera == null && !isTopDownView)
                rotateCamera = StartCoroutine(Rotate());
        }
    }

    public void OnCameraMoveButton(InputAction.CallbackContext context)
    {
        if (context.performed && !locked)
        {
            MoveDirection = context.ReadValue<Vector2>();

            if (moveCamera == null)
                moveCamera = StartCoroutine(Move());
        }
    }

    private IEnumerator Move()
    {
        if (!locked)
        {
            while (isMovingToPosition && (Mathf.Abs(_transform.position.x - moveToPosition.x) > .08f || Mathf.Abs(_transform.position.z - moveToPosition.z) > .08f))
            {
                _rigidBody.drag = 1f;
                if (Mathf.Abs(_transform.position.x - moveToPosition.x) < 10f && Mathf.Abs(_transform.position.z - moveToPosition.z) < 10f)
                    _rigidBody.drag = 2.5f;

                Vector3 force = (new Vector3(moveToPosition.x, _transform.position.y, moveToPosition.z) - _transform.position).normalized
                    * screenMoveSpeed * Time.deltaTime;

                _rigidBody.AddForce(force);

                yield return new WaitForFixedUpdate();
            }

            if (isMovingToPosition)
            {
                _rigidBody.velocity = Vector3.zero;
                isMovingToPosition = false;
            }

            while (MoveDirection != Vector2.zero)
            {
                _rigidBody.drag = 1f;
                Vector3 force = new Vector3(MoveDirection.x, 0, MoveDirection.y) * screenMoveSpeed * Time.deltaTime;
                _rigidBody.AddRelativeForce(force);

                yield return new WaitForFixedUpdate();
            }

            _rigidBody.drag = 3f;

            moveCamera = null;
        }
    }

    private IEnumerator Rotate()
    {
        while (isRotating && !locked)
        {
            float currentMousePosition = GameManager.Instance.mouse.position.ReadValue().x;

            if (mousePositionOnRotateStart != currentMousePosition)
            {
                float rotateAmount = currentMousePosition < mousePositionOnRotateStart ? -1f : 1f;
                float mouseSpeedRotation = Mathf.Abs(mousePositionOnRotateStart - currentMousePosition) / Screen.width;
                mousePositionOnRotateStart = currentMousePosition;                
                rotateAmount *= rotateMovementSpeed * Time.deltaTime * mouseSpeedRotation;

                _transform.rotation *= Quaternion.Euler(0f, rotateAmount, 0f);
            }
            yield return null;
        }

        rotateCamera = null;
    } 

    private void PositionCamera()
    {
        _camera.transform.localPosition = normalisedCameraPosition * currentZoomLevel;
        _camera.transform.localPosition = new Vector3(
            0f,
            _camera.transform.localPosition.y,
            _camera.transform.localPosition.z - 22f);
        _camera.transform.LookAt(new Vector3(_transform.position.x, 
            0f,
            _transform.position.z));
    }

    public void MoveTowardsPosition(Vector3 position)
    {
        moveToPosition = position;
        isMovingToPosition = true;

        if (moveCamera == null)
            moveCamera = StartCoroutine(Move());
    }
}
