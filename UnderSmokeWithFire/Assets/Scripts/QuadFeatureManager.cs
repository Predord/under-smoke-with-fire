using UnityEngine;

public class QuadFeatureManager : MonoBehaviour
{
    public QuadFeatureCollection[] stoneCollections;
    public QuadFeatureCollection[] plantCollections;
    public QuadFeatureCollection[] specialCollections;

    public Transform ladderPrefab;

    private Transform container;

    public void Clear()
    {
        if (container)
        {
            Destroy(container.gameObject);
        }
        container = new GameObject("Features Container").transform;
        container.SetParent(transform, false);
    }

    public void AddFeature(QuadCell cell, Vector3 position)
    {
        QuadHash hash = QuadMetrics.SampleHashGrid(position);
        Transform prefab = PickPrefab(stoneCollections, cell.StoneDetailLevel, hash.a, hash.c);
        Transform otherPrefab = PickPrefab(plantCollections, cell.PlantDetailLevel, hash.b, hash.c);
        if (prefab)
        {
            if (otherPrefab && hash.b < hash.a)
            {
                prefab = otherPrefab;
            }
        }
        else if (otherPrefab)
        {
            prefab = otherPrefab;
        }
        else
        {
            return;
        }
        Transform instance = Instantiate(prefab);
        position.y += instance.localScale.y * 0.5f;
        instance.localPosition = QuadMetrics.Perturb(position);
        instance.localRotation = Quaternion.Euler(0f, 360f * hash.d, 0f);
        instance.SetParent(container, false);
    }

    public void AddFeature(QuadCell cell, Vector3 position, Vector2 rotation)
    {
        QuadHash hash = QuadMetrics.SampleHashGrid(position);
        Transform prefab = PickPrefab(stoneCollections, cell.StoneDetailLevel, hash.a, hash.c);
        Transform otherPrefab = PickPrefab(plantCollections, cell.PlantDetailLevel, hash.b, hash.c);
        if (prefab)
        {
            if(otherPrefab && hash.b < hash.a)
            {
                prefab = otherPrefab;
            }
        }
        else if(otherPrefab)
        {
            prefab = otherPrefab;
        }
        else
        {
            return;
        }
        Transform instance = Instantiate(prefab);
        position.y += instance.localScale.y * 0.5f;
        instance.localPosition = position;
        instance.localRotation = Quaternion.Euler(rotation.x, 360f * hash.d, rotation.y);
        instance.SetParent(container, false);
    }

    //change scale when adding trees
    public void AddSpecial(int specialIndex, Vector3 position, QuadDirection specialDirection)
    {
        QuadHash hash = QuadMetrics.SampleHashGrid(position);
        Transform instance = Instantiate(specialCollections[specialIndex - 1].Pick(hash.a));
        position.y += instance.localScale.y * 0.5f;
        instance.localPosition = QuadMetrics.Perturb(position);
        instance.localRotation = Quaternion.Euler(0, 45f * (int)specialDirection, 0);
        instance.SetParent(container, false);
    }

    public void AddLadder(float rotation, int elevationDifference, Vector3 position)
    {
        for (int i = 0; i < elevationDifference; i++)
        {
            Transform instance = Instantiate(ladderPrefab);
            position.y += QuadMetrics.elevationStep * 0.5f;
            instance.localPosition = position;
            instance.localRotation = Quaternion.Euler(i % 2 * 180f, rotation, 0);
            instance.SetParent(container, false);
            position.y += QuadMetrics.elevationStep * 0.5f;
        }
    }

    private Transform PickPrefab(QuadFeatureCollection[] collection, int level, float hash, float choice)
    {
        if (level > 0)
        {
            float[] thresholds = QuadMetrics.GetFeatureThresholds(level - 1);
            for (int i = 0; i < thresholds.Length; i++)
            {
                if (hash < thresholds[i])
                {
                    return collection[i].Pick(choice);
                }
            }
        }
        return null;
    }
}
