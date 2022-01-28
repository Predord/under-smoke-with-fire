using System;
using UnityEngine;

using Random = UnityEngine.Random;

public class LocationRow : MonoBehaviour
{
    public int index;
    public int locationCount;
    public TravelPath path;
    public Location[] locations;

    public void CreateRow(Vector3 origin, Vector3 step)
    {
        locations = new Location[locationCount];

        if(locationCount % 2 == 0)
        {
            origin -= step / 2f;
        }

        origin += locationCount / 2 * step;

        for(int i = 0; i < locationCount; i++)
        {
            LocationType locationType = (LocationType)Random.Range(0, Enum.GetNames(typeof(LocationType)).Length - 1);
            Location location = locations[i] = Instantiate(path.locationPrefab);
            location.transform.SetParent(transform);
            location.transform.position = origin - step * i + (Vector3.right + Vector3.up) * Random.Range(-path.locationsPositionPerturb, path.locationsPositionPerturb);

            location.path = path;
            location.rowIndex = index;
            location.locationType = locationType;
            location.SetLocation(index, i);
            location.SetLocationMap();
            location.gameObject.SetActive(false);
        }

        if(index > 1)
        {
            path.GetRow(index - 1).CheckForLocationsWithoutConnection();
        }
    }

    public void CheckForLocationsWithoutConnection()
    {
        for(int i = 0; i < locations.Length; i++)
        {
            locations[i].CreateConnection(i);
        }
    }
}
