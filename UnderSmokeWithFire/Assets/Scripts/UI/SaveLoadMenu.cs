using System.IO;
using UnityEngine;
using TMPro;
using System.Linq;

public class SaveLoadMenu : MonoBehaviour
{
    public RectTransform listContent;
    public SaveLoadItem itemPrefab;
    public TMP_Text menuLabel, actionButtonLabel;
    public TMP_InputField nameInput;

    private bool saveMode;

    public void Open(bool saveMode)
    {
        this.saveMode = saveMode;
        if (saveMode)
        {
            menuLabel.text = "Save Map";
            actionButtonLabel.text = "Save";
        }
        else
        {
            menuLabel.text = "Load Map";
            actionButtonLabel.text = "Load";
        }
        FillList();
        gameObject.SetActive(true);
        CameraInput.Locked = true;
    }

    public void Close()
    {
        gameObject.SetActive(false);
        CameraInput.Locked = false;
    }
    /*
    public void Delete()
    {
        string path = GetSelectedPathString();
        if (path == null)
        {
            return;
        }
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        nameInput.text = "";
        FillList();
    }*/

    public void SelectItem(string name)
    {
        nameInput.text = name;
    }

    private string GetSelectedPathString()
    {
        string mapName = nameInput.text;
        if (mapName.Length == 0)
        {
            return null;
        }

        return Path.Combine(Application.persistentDataPath, mapName + ".bytes");
    }

    private TextAsset GetSelectedPathText()
    {
        string mapName = nameInput.text;
        if (mapName.Length == 0)
        {
            return null;
        }

        TextAsset mapFile = Resources.Load<TextAsset>("Maps/" + mapName);
        return mapFile;
    }

    public void Action()
    {
        if (saveMode)
        {
            string path = GetSelectedPathString();
            if (path == null)
            {
                return;
            }
            Save(path);
        }
        else
        {
            TextAsset mapFile = GetSelectedPathText();
            if (mapFile == null)
            {
                return;
            }
            Load(mapFile);
        }
        Close();
    }

    private void Save(string path)
    {
        using (BinaryWriter writer = new BinaryWriter(File.Open(path, FileMode.Create)))
        {
            writer.Write(GameManager.Instance.mapFileVersion);
            GameManager.Instance.grid.Save(writer);
        }
    }

    private void Load(TextAsset mapFile)
    {
        GameManager.Instance.Load(mapFile);
    }

    private void FillList()
    {
        for (int i = 0; i < listContent.childCount; i++)
        {
            Destroy(listContent.GetChild(i).gameObject);
        }

        var paths = Resources.LoadAll("Maps", typeof(TextAsset)).Cast<TextAsset>().ToArray();
        for (int i = 0; i < paths.Length; i++)
        {
            SaveLoadItem item = Instantiate(itemPrefab);
            item.menu = this;
            item.MapName = paths[i].name; 
            item.transform.SetParent(listContent, false);
        }
    }
}
