using UnityEngine;

public class FloatingTextHandler : MonoBehaviour
{
    public TextMesh text;

    private void Start()
    {
        Destroy(gameObject, 2f);

        transform.localPosition += Vector3.up * 0.2f;
    }

    private void Update()
    {
        transform.localRotation = Quaternion.LookRotation(CameraMain.Instance._camera.transform.forward);
    }
}
