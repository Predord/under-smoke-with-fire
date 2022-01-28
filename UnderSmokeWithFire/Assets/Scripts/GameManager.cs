using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization;
using System.IO;
using System.Collections;
using System.Linq;

public class GameManager : Singleton<GameManager>
{
    public bool isNewGame = true;
    public bool isLoading = false;
    public bool stopCurrentAction;
    public int seed;
    public int searchingForPathEntityIndex = 1;
    public QuadGrid grid;
    public QuadMapEditor editor;
    public TextAsset jsonAbilities;
    public TextAsset jsonBuffsDebuffs;
    public TextAsset jsonBuffsDebuffsAM;
    public TextAsset currentMap;
    public Mouse mouse;

    public int mapFileVersion = 0;
    public int progressFileVersion = 0;

    public static bool paused;

    public const string masterVolumePlayerPrefKey = "masterVolume";
    public const string musicVolumePlayerPrefKey = "musicVolume";
    public const string SFXVolumePlayerPrefKey = "SFXVolume";
    public const string resolutionWidthPlayerPrefKey = "resolutionWidth";
    public const string resolutionHeightPlayerPrefKey = "resolutionHeight";
    public const string resolutionRefreshRatePlayerPrefKey = "refreshRate";
    public const string fullScreenPlayerPrefKey = "fullScreen";
    public const string qualityPlayerPrefKey = "quality";

    public bool IsActionMap
    {
        get
        {
            return isActionMap;
        }
        set
        {
            isActionMap = value;

            if (GameUI.Instance)
            {
                GameUI.Instance.OnChangeScene(isActionMap);
            }          
        }
    }

    private bool isActionMap;

    public bool IsPlayerInAction
    {
        get
        {
            return isPlayerInAction;
        }
        set
        {
            isPlayerInAction = value;
            if (value)
            {
                grid.ClearDistance();
                foreach (Enemy enemy in grid.units.Where(x => x.index != 0))
                {
                    enemy.IsActionPossible = true;
                    enemy.ActionPoints = Player.Instance.ActionPoints;
                    enemy.SetNewAction();
                }
                foreach (AbilityProjectile projectile in grid.abilityProjectiles)
                {
                    projectile.TurnsToTravel = Player.Instance.ActionPoints;
                    projectile.Moving = true;
                }
            }
            else
            {
                Player.Instance.IsActionPossible = true;
            }
        }
    }

    private bool isPlayerInAction;

    public int CurrentMapStrength
    {
        get
        {
            return currentMapStrength;
        }
        set
        {
            if (value == currentMapStrength || isActionMap)
                return;

            currentMapStrength = value;
        }
    }

    private int currentMapStrength;

    public float GameRunningSpeed
    {
        get
        {
            return 1.8f;
        }
    }

    private void Awake()
    {
        if (!RegisterMe())
        {
            return;
        }

        Initialize();

        PlayerInfo.InitializePlayersInfo();
        //temp
        isActionMap = SceneManager.GetActiveScene().name == "QuadMap";
    }

    private void Start()
    {
        InitializePlayerPrefs();
        //temp 
        if (GameUI.instance)
        {
            GameUI.Instance.OnChangeScene(isActionMap);
        }
    }

    private void InitializePlayerPrefs()
    {        
        AudioManager.Instance.MasterVolume = PlayerPrefs.GetFloat(masterVolumePlayerPrefKey, 0f);
        AudioManager.Instance.MusicVolume = PlayerPrefs.GetFloat(musicVolumePlayerPrefKey, 0f);
        AudioManager.Instance.SFXVolume = PlayerPrefs.GetFloat(SFXVolumePlayerPrefKey, 0f);

        Screen.SetResolution(
            PlayerPrefs.GetInt(resolutionWidthPlayerPrefKey, Screen.currentResolution.width),
            PlayerPrefs.GetInt(resolutionHeightPlayerPrefKey, Screen.currentResolution.height),
            (FullScreenMode)PlayerPrefs.GetInt(fullScreenPlayerPrefKey, (int)Screen.fullScreenMode),
            PlayerPrefs.GetInt(resolutionRefreshRatePlayerPrefKey, Screen.currentResolution.refreshRate));

        QualitySettings.SetQualityLevel(PlayerPrefs.GetInt(qualityPlayerPrefKey, QualitySettings.GetQualityLevel()));
    }

    private void Initialize()
    {
        AbilityDatabase.InitializeAbilitiesDatabase(jsonAbilities);
        BuffsDebuffsDatabase.InitializebuffsDebuffsDatabase(jsonBuffsDebuffs);
        BuffsDebuffsActionMapDatabase.InitializeBuffsDebuffsAMDatabase(jsonBuffsDebuffsAM);

        mouse = Mouse.current;
    }

    public void UnPause()
    {
        paused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    public void Pause()
    {
        paused = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;
    }

    public void SetEditMode(bool toggle)
    {
        if (toggle && !IsEnterEditModePossible())
            return;

        enabled = !toggle;

        grid.ClearHighlightsWithBoundaries();
        if (toggle)
        {
            Shader.EnableKeyword("QUAD_MAP_EDIT_MODE");
            for(int i = 1; i < grid.units.Count; i++)
            {
                grid.units[i].HandleEditMode(toggle);
            }
        }
        else
        {
            Shader.DisableKeyword("QUAD_MAP_EDIT_MODE");
            for (int i = 1; i < grid.units.Count; i++)
            {
                grid.units[i].HandleEditMode(toggle);
            }
            if (Player.Instance != null)
            {
                grid.FindPlayerDistance();
            }
        }
    }

    public bool IsEnterEditModePossible()
    {
        if (Player.Instance)
        {
            return !Player.Instance.InAction;
        }
        else
        {
            return true;
        }
    }

    public void CheckForActionsFinished()
    {
        if (Player.Instance.InAction)
            return;

        foreach (Entity entity in grid.units.Where(x => x.index != 0))
        {
            if (entity.IsActionPossible)
            {
                return;
            }               
        }

        foreach (AbilityProjectile projectile in grid.abilityProjectiles)
        {
            if (projectile.Moving)
            {
                return;
            }               
        }

        IsPlayerInAction = false;
    }

    public void Load(TextAsset mapFile)
    {
        using (BinaryReader reader = new BinaryReader(new MemoryStream(mapFile.bytes)))
        {
            int header = reader.ReadInt32();
            if (header <= mapFileVersion)
            {
                if(editor != null)
                {
                    editor.ClearSelectedCells();
                }
                grid.Load(reader, header);
            }
            else
            {
                Debug.LogError("Unknown map format " + header);
            }
        }
    }

    public void CheckForExitZone(QuadCell currentCell)
    {
        foreach(var exitZone in grid.specialZones.Where(zone => zone.zoneType == SpecialZoneType.Exit))
        {
            if (exitZone.IsCellInsideZone(currentCell) && ObjectiveManager.Instance.completedObjectivesForEscape <= 0)
            {
                ActionUI.instance.leaveMapWindow.gameObject.SetActive(true);
                break;
            }
        }
    }

    public void GameOverActionMap()
    {
        if (ActionUI.Instance != null)
        {
            ActionUI.Instance.OpenGameOverWindow();
        }        

        string saveFilePath = Path.Combine(Application.persistentDataPath, "Save.dat");
        File.Delete(saveFilePath);

        StartCoroutine(WaitForAnyKeyPressed());
    }

    private IEnumerator WaitForAnyKeyPressed()
    {
        yield return new WaitUntil(() => Keyboard.current.anyKey.wasPressedThisFrame || mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame
            || Keyboard.current.anyKey.isPressed || mouse.leftButton.isPressed || mouse.rightButton.isPressed);

        SceneLoader.Instance.LoadMenuScene();
    }
}
