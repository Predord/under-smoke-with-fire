using UnityEngine;

[System.Serializable]
public struct QuadFeatureCollection
{
    public Transform[] prefabs;

    public Transform Pick(float choice)
    {
        return prefabs[(int)(choice * prefabs.Length)];
    }
}
