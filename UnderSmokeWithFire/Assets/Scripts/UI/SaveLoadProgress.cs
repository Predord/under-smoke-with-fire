using System.IO;
using UnityEngine;

public static class SaveLoadProgress 
{
    public static void Save()
    {
        string saveFilePath = Path.Combine(Application.persistentDataPath, "Save.dat");

        using (BinaryWriter writer = new BinaryWriter(File.Open(saveFilePath, FileMode.Create)))
        {
            writer.Write(GameManager.Instance.progressFileVersion);
            writer.Write(GameManager.Instance.seed);

            TravelPath.Save(writer);
            PlayerInfo.Save(writer);
            DailyActions.Save(writer);
        }
    }

    public static void Load()
    {
        string saveFilePath = Path.Combine(Application.persistentDataPath, "Save.dat");

        using (BinaryReader reader = new BinaryReader(File.OpenRead(saveFilePath)))
        {
            int header = reader.ReadInt32();
            if (header <= GameManager.Instance.progressFileVersion)
            {
                GameManager.Instance.seed = reader.ReadInt32();

                TravelPath.Load(reader, header);
                PlayerInfo.Load(reader, header);
                DailyActions.Load(reader, header);
            }
            else
            {
                Debug.LogError("Unknown progress format " + header);
            }
        }
    }

    public static bool SaveFileExists()
    {
        string saveFilePath = Path.Combine(Application.persistentDataPath, "Save.dat");

        return File.Exists(saveFilePath);
    }
}
