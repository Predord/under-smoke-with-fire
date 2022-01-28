using UnityEngine;

public class CameraMain : Singleton<CameraMain>
{
    public bool isMoving;
    public Camera _camera;
    public CameraInput cameraInput;
    public AbilityLines abilityLines;

    private float minX = 0f;
    private float minZ = 0f;
    private float maxX = float.MaxValue;
    private float maxZ = float.MaxValue;
    private Transform _transform;

    private void Awake()
    {
        if (!RegisterMe())
        {
            return;
        }

        _camera = GetComponentInChildren<Camera>();
        cameraInput = GetComponent<CameraInput>();    
    }

    private void Start()
    {
        _transform = transform;

        SetBorderCoordinates(
            GameManager.Instance.grid.GetCell(new QuadCoordinates(GameManager.Instance.grid.explorableCountX, GameManager.Instance.grid.explorableCountZ)),
            GameManager.Instance.grid.GetCell(new QuadCoordinates(GameManager.Instance.grid.cellCountX - GameManager.Instance.grid.explorableCountX - 1, GameManager.Instance.grid.cellCountZ - GameManager.Instance.grid.explorableCountZ - 1)));
    }

    private void Update()
    {
        _transform.position = new Vector3(
            Mathf.Clamp(_transform.position.x, minX, maxX), _transform.position.y, Mathf.Clamp(_transform.position.z, minZ, maxZ));
    }

    public void SetBorderCoordinates(QuadCell bottomLeft, QuadCell topRight)
    {
        minX = bottomLeft.transform.position.x;
        minZ = bottomLeft.transform.position.z;
        maxX = topRight.transform.position.x;
        maxZ = topRight.transform.position.z;
    }

    public void FocusCameraOnCellInstantly(QuadCell cell)
    {
        if (cell == null)
            return;

        _transform.position = new Vector3(cell.transform.position.x, _transform.position.y, cell.transform.position.z);
    }

    public void FocusCameraOnCell(QuadCell cell)
    {
        if (cell == null)
            return;

        cameraInput.MoveTowardsPosition(cell.transform.position);
    }
}
