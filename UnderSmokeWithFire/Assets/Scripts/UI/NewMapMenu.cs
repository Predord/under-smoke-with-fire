using UnityEngine;

public class NewMapMenu : MonoBehaviour
{
    public void Open()
    {
        gameObject.SetActive(true);
        CameraInput.Locked = true;
    }

    public void Close()
    {
        gameObject.SetActive(false);
        CameraInput.Locked = false;
    }

    public void CreateSmallMap()
    {
        CreateMap(140, 120);
    }

    public void CreateMediumMap()
    {
        CreateMap(200, 180);
    }

    public void CreateLargeMap()
    {
        CreateMap(260, 240);
    }

    private void CreateMap(int x, int z)
    {
        GameManager.Instance.grid.CreateMap(x, z);
        Close();
    }
}
