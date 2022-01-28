using System.IO;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TravelPath : MonoBehaviour
{
    public int rowsCount;
    public int minRowLocationCount;
    public int maxRowLocationCount;
    public int maxLocationDistanceConnection;
    public float stepLength;
    public float rowLocationStep;
    public float locationsPositionPerturb;

    public RectTransform _transform;

    public RectTransform finalLocationStrengthPanel;

    public RectTransform connections;
    public RectTransform locations;

    public RectTransform earlyBuildPanel;
    public RectTransform noAbilityWarningPanel;
    public RectTransform locationInfoPanel;
    public TMP_Text locationInfoText;

    public Location startLocation;
    public Location finalLocation;

    public RectTransform strengthSlotPrefab;
    public RectTransform stepPrefab;
    public Connection connectionPrefab;
    public Location locationPrefab;
    public LocationRow locationRowPrefab;

    public RectTransform[] locationInfoPanelStrengthLevels;
    public LocationRow[] locationRows;
    public Sprite[] locationSprites;

    [System.NonSerialized]
    public Location currentLocation;

    [System.NonSerialized]
    public Location playerLocation;

    public static List<int> passedLocations = new List<int>();

    public static int maxArmyRange;
    private static int startingArmyPosition;
    private static int armyStep;    
    private static int armyRadiusStrength;

    public const int maxLocationStrength = 5;
    public static int turnsForArmyMove;

    private const int adjustResolutionWidth = 1920;

    public int CurrentArmyPosition
    {
        get
        {
            return currentArmyPosition;
        }
        set
        {
            currentArmyPosition = Mathf.Clamp(value, startingArmyPosition, rowsCount);
            FinalLocationStrength = currentArmyPosition - rowsCount / 2;

            for (int i = 0; i < locationRows.Length; i++)
            {
                for (int j = 0; j < locationRows[i].locationCount; j++)
                {
                    if (locationRows[i].locations[j].locationType != LocationType.None)
                    {
                        locationRows[i].locations[j].SetGarnisonStrength();
                    }
                }
            }

            if (currentArmyPosition == rowsCount)
                armyStep = 1;
        }
    }

    private static int currentArmyPosition;

    public int FinalLocationStrength
    {
        get
        {
            return finalLocationStrength;
        }
        set
        {
            if (value == finalLocationStrength)
                return;

            finalLocationStrength = Mathf.Clamp(value, 0, maxLocationStrength * 2);

            int i = 0;
            for(; i < finalLocationStrength; i++)
            {
                finalLocationStrengthPanel.GetChild(i).GetChild(0).gameObject.SetActive(false);
            }

            for (; i < maxLocationStrength * 2; i++)
            {
                finalLocationStrengthPanel.GetChild(i).GetChild(0).gameObject.SetActive(true);
            }
           
            if(finalLocationStrength == maxLocationStrength * 2)
            {
                GameManager.Instance.GameOverActionMap();
            }
        }
    }

    private int finalLocationStrength;

    private void Awake()
    {
        SetUpPath();
        ShowPath();
        InitializeFinalLocationPanel();

        if (GameManager.Instance.isNewGame && GameManager.Instance.isLoading)
        {
            InitializeArmy();

            CurrentArmyPosition = startingArmyPosition;
            GameManager.Instance.isNewGame = false;
            GameManager.Instance.isLoading = false;
        }
        else if (!GameManager.Instance.isNewGame && GameManager.Instance.isLoading)
        {
            InitializeArmy();
            OnLoadNewDay();
            GameManager.Instance.isNewGame = false;
            GameManager.Instance.isLoading = false;
            Debug.Log(playerLocation.rowIndex);
        }
        else
        {
            OnNewDayStart();
        }

        _transform.anchoredPosition = new Vector3(
            Mathf.Clamp(playerLocation.transform.localPosition.x + 650f, adjustResolutionWidth - _transform.rect.width, 0f), _transform.localPosition.y, _transform.localPosition.z);

        SaveLoadProgress.Save();
    }

    public LocationRow GetRow(int rowIndex)
    {
        return locationRows[rowIndex - 1];
    }

    public Location GetLocationInRow(int rowIndex, int locationIndex)
    {
        return locationRows[rowIndex - 1].locations[locationIndex];
    }

    private void SetUpPath()
    {
        locationRows = new LocationRow[rowsCount];
        finalLocation.rowIndex = rowsCount + 1;

        Random.State currentState = Random.state;
        Random.InitState(GameManager.Instance.seed);

        Vector3 rowLocationsDistance = (finalLocation.transform.position - startLocation.transform.position).normalized * rowLocationStep;
        rowLocationsDistance = Quaternion.Euler(0f, 0f, 90f) * rowLocationsDistance;

        for (int i = 1; i <= rowsCount / 2; i++)
        {
            LocationRow row = locationRows[i - 1] = Instantiate(locationRowPrefab);
            row.transform.SetParent(locations);
            row.index = i;
            row.locationCount = minRowLocationCount + Mathf.FloorToInt((i - 1) * (maxRowLocationCount - minRowLocationCount) / (rowsCount / 2 - 1f));
            row.path = this;
            row.CreateRow(Vector3.Lerp(startLocation.transform.position, finalLocation.transform.position, i / (rowsCount + 1f)), rowLocationsDistance);
        }

        for(int i = 1 + rowsCount / 2; i <= rowsCount; i++)
        {
            LocationRow row = locationRows[i - 1] = Instantiate(locationRowPrefab);
            row.transform.SetParent(locations);
            row.index = i;
            row.locationCount = minRowLocationCount + Mathf.FloorToInt((maxRowLocationCount - minRowLocationCount) * (1f - (i - 1f - rowsCount / 2) / (rowsCount - rowsCount / 2 - 1f)));
            row.path = this;
            row.CreateRow(Vector3.Lerp(startLocation.transform.position, finalLocation.transform.position, i / (rowsCount + 1f)), rowLocationsDistance);
        }

        for(int i = 0; i < locationRows[rowsCount - 1].locations.Length; i++)
        {
            finalLocation.CreateConnection(locationRows[rowsCount - 1].locations[i].transform, finalLocation.transform, locationRows[rowsCount - 1].locations[i]);
        }

        Random.state = currentState;
    }

    public void LoadMap(bool checkAbilitiesCount)
    {
        if(checkAbilitiesCount && PlayerInfo.activeAbilities.Count == 0)
        {
            noAbilityWarningPanel.gameObject.SetActive(true);
            locationInfoPanel.gameObject.SetActive(false);
        }
        else
        {
            PlayerInfo.HasScoutedArea = currentLocation.playerScoutedArea;
            GameManager.Instance.CurrentMapStrength = currentLocation.GarnisonStrength;
            //playerLocation = currentLocation;
            for (int i = 0; i < GetRow(currentLocation.rowIndex).locationCount; i++)
            {
                if (GetRow(currentLocation.rowIndex).locations[i] == currentLocation)
                {
                    passedLocations.Add(i);
                    break;
                }
            }

            //playerLocation.ShowConnectedLocations();
            SceneLoader.Instance.LoadActionMapScene();
        }
    }

    public void OnNewDayStart()
    {
        if (playerLocation.rowIndex + 1 == locationRows.Length)
            return;

        for(int i = 0; i < GetRow(playerLocation.rowIndex + 1).locationCount; i++)
        {
            GetRow(playerLocation.rowIndex + 1).locations[i].Refresh();
        }

        turnsForArmyMove--;
        if(turnsForArmyMove == 0)
        {
            turnsForArmyMove = armyStep;
            CurrentArmyPosition++;
        }
        else
        {
            CurrentArmyPosition = currentArmyPosition;
        }
    }

    private void OnLoadNewDay()
    {
        if (playerLocation.rowIndex + 1 == locationRows.Length)
            return;

        for (int i = 0; i < GetRow(playerLocation.rowIndex + 1).locationCount; i++)
        {
            GetRow(playerLocation.rowIndex + 1).locations[i].Refresh();
        }

        CurrentArmyPosition = currentArmyPosition;
    }

    public void CloseInfoPanel()
    {
        locationInfoPanel.gameObject.SetActive(false);
    }

    public void CloseWarningPanel()
    {
        noAbilityWarningPanel.gameObject.SetActive(false);
    }

    private void ShowPath()
    {
        startLocation.ShowConnectedLocations();

        //change
        if (passedLocations.Count == 0)
        {
            playerLocation = startLocation;
        }
        else
        {
            for(int i = 0; i < passedLocations.Count; i++)
            {
                locationRows[i].locations[passedLocations[i]].ShowConnectedLocations();
            }

            playerLocation = locationRows[passedLocations.Count - 1].locations[passedLocations[passedLocations.Count - 1]];
        }        
    }

    private void InitializeFinalLocationPanel()
    {
        for(int i = 0; i < maxLocationStrength * 2; i++)
        {
            RectTransform instance = Instantiate(strengthSlotPrefab);
            instance.SetParent(finalLocationStrengthPanel, false);
        }
    }

    private void InitializeArmy()
    {
        startingArmyPosition = rowsCount / 2;
        turnsForArmyMove = armyStep = 2;
        maxArmyRange = rowsCount / 2;
        armyRadiusStrength = maxArmyRange / maxLocationStrength;
    }

    public static void Save(BinaryWriter writer)
    {
        writer.Write(passedLocations.Count);

        for (int i = 0; i < passedLocations.Count; i++)
        {
            writer.Write(passedLocations[i]);
        }

        writer.Write(turnsForArmyMove);
        writer.Write(currentArmyPosition);
    }

    public static void Load(BinaryReader reader, int header)
    {
        passedLocations.Clear();

        int passedLocationCount = reader.ReadInt32();

        for (int i = 0; i < passedLocationCount; i++)
        {
            passedLocations.Add(reader.ReadInt32());
        }

        turnsForArmyMove = reader.ReadInt32();
        currentArmyPosition = reader.ReadInt32();
    }
}
