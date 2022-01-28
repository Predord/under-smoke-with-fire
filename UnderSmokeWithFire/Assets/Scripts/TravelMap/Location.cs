using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Location : MonoBehaviour
{
    public bool playerScoutedArea;
    public bool isMainArmyLocation;

    public int rowIndex;

    public LocationType locationType;
    public Image image;
    public TravelPath path;
    public TextAsset mapName;

    public List<Location> connectedLocations = new List<Location>();
    public List<Connection> connections = new List<Connection>();

    public bool PlayerAlertedGarnison
    {
        get
        {
            return playerAlertedGarnison;
        }
        set
        {
            if (value == playerAlertedGarnison)
                return;

            playerAlertedGarnison = value;
            if (path.locationInfoPanel.gameObject.activeSelf)
            {
                SetLocationInfoPanelStrengthLevels();
            }
        }
    }

    private bool playerAlertedGarnison;

    public int GarnisonStrength
    {
        get
        {
            return garnisonStrength + (PlayerAlertedGarnison ? DailyActions.additionalGarnisonStrengthWhenAlerted : 0);
        }
        set
        {
            if (value == garnisonStrength)
                return;

            garnisonStrength = value;
            isMainArmyLocation = garnisonStrength > TravelPath.maxLocationStrength;
        }
    }

    private int garnisonStrength;

    public void SetLocation(int rowIndex, int locationIndex)
    {
        if(locationType != LocationType.None)
        {
            image.sprite = path.locationSprites[(int)locationType - 1];

            if (rowIndex == 1)
            {
                CreateConnection(path.startLocation.transform, transform, path.startLocation);
            }
            else
            {
                if(locationIndex == 0 || locationIndex == path.GetRow(rowIndex).locationCount - 1)
                {
                    bool pathExist = false;
                    int currentLocationModifier;
                    int maxLocationIndex;
                    Location currentLocation;

                    if(locationIndex == 0)
                    {
                        currentLocationModifier = 1;
                        maxLocationIndex = 0;
                    }
                    else
                    {
                        currentLocationModifier = -1;
                        maxLocationIndex = path.GetRow(rowIndex - 1).locationCount - 1;
                    }

                    for(int i = 0; i <= System.Math.Min(path.GetRow(rowIndex - 1).locationCount - 1, path.maxLocationDistanceConnection); i++)
                    {
                        currentLocation = path.GetLocationInRow(rowIndex - 1, maxLocationIndex + currentLocationModifier * i);
                        pathExist = CreateConnection(currentLocation.transform, transform, currentLocation);
                    }

                    if (!pathExist)
                    {
                        int currentLocationIndex = path.maxLocationDistanceConnection + 1;

                        while (!pathExist && maxLocationIndex + currentLocationModifier * currentLocationIndex >= 0 && maxLocationIndex + currentLocationModifier * currentLocationIndex < path.GetRow(rowIndex - 1).locationCount)
                        {
                            currentLocation = path.GetLocationInRow(rowIndex - 1, maxLocationIndex + currentLocationModifier * currentLocationIndex);
                            pathExist = CreateConnection(currentLocation.transform, transform, currentLocation);

                            currentLocationIndex++;
                        }
                    }
                }
                else
                {
                    bool pathExist = false;
                    int currentLocationIndex = Mathf.FloorToInt(locationIndex / (path.GetRow(rowIndex).locationCount - 1f) * (path.GetRow(rowIndex - 1).locationCount - 1));
                    Location currentLocation;

                    for(int i = currentLocationIndex; i >= System.Math.Max(0, currentLocationIndex - path.maxLocationDistanceConnection); i--)
                    {
                        currentLocation = path.GetLocationInRow(rowIndex - 1, i);
                        pathExist = CreateConnection(currentLocation.transform, transform, currentLocation);
                    }

                    for (int i = currentLocationIndex + 1; i <= System.Math.Min(path.GetRow(rowIndex - 1).locationCount - 1, currentLocationIndex + path.maxLocationDistanceConnection); i++)
                    {
                        currentLocation = path.GetLocationInRow(rowIndex - 1, i);
                        pathExist = CreateConnection(currentLocation.transform, transform, currentLocation);
                    }

                    if (!pathExist)
                    {
                        int currentLocationDistance = path.maxLocationDistanceConnection + 1;

                        while(!pathExist && (currentLocationIndex - currentLocationDistance >= 0 || currentLocationIndex + currentLocationDistance < path.GetRow(rowIndex - 1).locationCount))
                        {
                            if(currentLocationIndex - currentLocationDistance >= 0)
                            {
                                currentLocation = path.GetLocationInRow(rowIndex - 1, currentLocationIndex - currentLocationDistance);
                                pathExist = CreateConnection(currentLocation.transform, transform, currentLocation);
                            }

                            if(currentLocationIndex + currentLocationDistance < path.GetRow(rowIndex - 1).locationCount)
                            {
                                currentLocation = path.GetLocationInRow(rowIndex - 1, currentLocationIndex + currentLocationDistance);

                                if (pathExist)
                                {
                                    CreateConnection(currentLocation.transform, transform, currentLocation);
                                }
                                else
                                {
                                    pathExist = CreateConnection(currentLocation.transform, transform, currentLocation);
                                }
                            }

                            currentLocationDistance++;
                        }
                    }
                }
            }
        }
        else
        {
            image.color = new Color(0f, 0f, 0f, 0f);
        }
    }

    public void CreateConnection(int locationIndex)
    {
        if(locationType != LocationType.None && connectedLocations.Count == 0)
        {
            bool pathExist = false;
            int currentLocationIndex = Mathf.FloorToInt(locationIndex / (path.GetRow(rowIndex).locationCount - 1f) * (path.GetRow(rowIndex + 1).locationCount - 1));
            int currentLocationDistance = 1;
            Location currentLocation;

            while (!pathExist && (currentLocationIndex - currentLocationDistance >= 0 || currentLocationIndex + currentLocationDistance < path.GetRow(rowIndex + 1).locationCount))
            {
                if (currentLocationIndex - currentLocationDistance >= 0)
                {
                    currentLocation = path.GetLocationInRow(rowIndex + 1, currentLocationIndex - currentLocationDistance);
                    if(currentLocation.locationType != LocationType.None)
                    {
                        pathExist = currentLocation.CreateConnection(transform, currentLocation.transform, this);
                    }
                }

                if (currentLocationIndex + currentLocationDistance < path.GetRow(rowIndex + 1).locationCount)
                {
                    currentLocation = path.GetLocationInRow(rowIndex + 1, currentLocationIndex + currentLocationDistance);

                    if (currentLocation.locationType != LocationType.None)
                    {
                        if (pathExist)
                        {
                            currentLocation.CreateConnection(transform, currentLocation.transform, this);
                        }
                        else
                        {
                            pathExist = currentLocation.CreateConnection(transform, currentLocation.transform, this);
                        }
                    }
                }

                currentLocationDistance++;
            }
        }
    }

    public bool CreateConnection(Transform start, Transform finish, Location fromLocation)
    {
        if (fromLocation.locationType != LocationType.None)
        {
            fromLocation.connectedLocations.Add(this);
            Connection connection = Instantiate(path.connectionPrefab);
            fromLocation.connections.Add(connection);
            connection.transform.SetParent(path.connections);
            connection.path = path;
            connection.SetConnection(path.stepLength, start, finish);
            connection.gameObject.SetActive(false);

            return true;
        }

        return false;
    }

    public void SetLocationMap()
    {
        var paths = Resources.LoadAll("Maps", typeof(TextAsset)).Cast<TextAsset>().ToArray();
        mapName = paths[Random.Range(0, paths.Length)];
    }

    public void OpenInfoPanel()
    {
        if (path.playerLocation.connectedLocations.Contains(this))
        {
            if(locationType == LocationType.EscapePath)
            {
                path.earlyBuildPanel.gameObject.SetActive(true);

                GameManager.Instance.GameOverActionMap();
            }
            else
            {
                //transfer to independent method
                path.currentLocation = this;
                GameManager.Instance.currentMap = mapName;

                DailyActions.Instance.CurrentChoosenLocation = this;

                path.noAbilityWarningPanel.gameObject.SetActive(false);
                path.locationInfoPanel.gameObject.SetActive(true);
                path.locationInfoPanel.transform.position = transform.position;
                path.locationInfoText.text = mapName.name;
                SetLocationInfoPanelStrengthLevels();
            }
        }
    }

    public void ShowConnectedLocations()
    {
        for (int i = 0; i < connectedLocations.Count; i++)
        {
            connectedLocations[i].gameObject.SetActive(true);
            connections[i].gameObject.SetActive(true);
        }
    }

    public void Refresh()
    {
        playerScoutedArea = false;
        PlayerAlertedGarnison = false;
    }

    public void SetGarnisonStrength()
    {
        int distanceToMainArmy = Mathf.Abs(rowIndex - path.CurrentArmyPosition);

        if (distanceToMainArmy > TravelPath.maxArmyRange)
        {
            GarnisonStrength = 0;
        }
        else
        {
            GarnisonStrength = 1 + (TravelPath.maxArmyRange - distanceToMainArmy) / 2;
        }
    }

    private void SetLocationInfoPanelStrengthLevels()
    {
        int i = 0;
        for (; i < GarnisonStrength; i++)
        {
            path.locationInfoPanelStrengthLevels[i].gameObject.SetActive(false);
        }
        for (; i < TravelPath.maxLocationStrength; i++)
        {
            path.locationInfoPanelStrengthLevels[i].gameObject.SetActive(true);
        }
    }
}
