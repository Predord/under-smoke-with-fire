using System;
using UnityEngine;
using Random = UnityEngine.Random;

public static class QuadMetrics 
{
    public const float radius = 0.75f;
    public const float solidFactor = 0.8f;
    public const float blendFactor = 1 - solidFactor;
    public const float elevationStep = 0.65f;
    public const float leanElevationFactor = blendFactor / 2f;
    public const float straightElevationFactor = 1 - leanElevationFactor;
    public const float cellPerturbStrength = 0.25f;
    public const float noiseScale = 0.03f;
    public const float elevationPerturbStrength = 0.12f;
    public const int chunkSizeX = 5;
    public const int chunkSizeZ = 5;
    public const float waterElevationOffset = -0.6f;
    public const int hashGridSize = 256;
    public const float hashGridScale = 2.75f;
    public static Texture2D noiseSource;

    private static QuadHash[] hashGrid;
    private static Vector3[] corners =
    {
        new Vector3(0f, 0f, radius),
        new Vector3(radius, 0f, radius),
        new Vector3(radius, 0f, 0f),
        new Vector3(radius, 0f, -radius),
        new Vector3(0f, 0f, -radius),
        new Vector3(-radius, 0f, -radius),
        new Vector3(-radius, 0f, 0f),
        new Vector3(-radius, 0f, radius),
        new Vector3(0f, 0f, radius)
    };

    private static float[][] featureThresholds = {
        new float[] {0.0f, 0.0f, 0.4f},
        new float[] {0.0f, 0.4f, 0.6f},
        new float[] {0.4f, 0.6f, 0.8f}
    };

    private static Color[] zoneColors =
    {
        Color.blue,
        Color.red,
        Color.black,
        Color.cyan, 
        Color.yellow
    };

    public static Color GetZoneColor(SpecialZoneType type)
    {
        return zoneColors[(int)type];
    }

    public static Vector3 GetFirstSolidCorner(QuadDirection direction)
    {
        return corners[(int)direction] * solidFactor;
    }

    public static Vector3 GetSecondSolidCorner(QuadDirection direction)
    {
        return corners[(int)direction + 1] * solidFactor;
    }

    public static Vector3 GetFirstCorner(QuadDirection direction)
    {
        return corners[(int)direction];
    }

    public static Vector3 GetSecondCorner(QuadDirection direction)
    {
        return corners[(int)direction + 1];
    }

    public static Vector3 GetBridge(QuadDirection direction)
    {
        return ((int)direction & 1) == 0 ? corners[(int)direction] * blendFactor : corners[(int)direction.Next()] * blendFactor;
    }

    public static Vector3 SetVertixElevation(Vector3 vertix, float elevation)
    {
        vertix.y = elevation;
        return vertix;
    }

    public static bool CheckLadderDirection(QuadDirection direction, int cellDirections)
    {
        return (cellDirections & (1 << (((int)direction) / 2))) != 0;
    }

    public static int GetElevationDifference(int elevationCell, int elevationNeighbor)
    {
        if (elevationCell > elevationNeighbor)
        {
            return -1;
        }
        else if (elevationCell < elevationNeighbor)
        {
            return 1;
        }
        return 0;
    }

    public static int GetCornerPointElevation(int elevationDifference1, int elevationDifference2, int elevationDifference3)
    {
        if (elevationDifference1 == 0 ? (elevationDifference2 == 0) && (elevationDifference3 == 0) : false)
            return 0;

        if (IsTwoNeighborsFlat(elevationDifference1, elevationDifference2, elevationDifference3, out int elevationDifference))
        {
            return elevationDifference;
        }
        else
        {
            if (elevationDifference1 != 0 && elevationDifference3 != 0)
            {
                return Math.Sign(Math.Sign(elevationDifference1) + Math.Sign(elevationDifference3));
            }
            return Math.Sign(Math.Sign(elevationDifference1) + Math.Sign(elevationDifference2) + Math.Sign(elevationDifference3));
        }
    }

    private static bool IsTwoNeighborsFlat(int elevationDifference1, int elevationDifference2, int elevationDifference3, out int elevationDifference)
    {
        bool result;
        elevationDifference = int.MinValue;
        result = elevationDifference1 == 0 ? (elevationDifference2 == 0 || elevationDifference3 == 0) : (elevationDifference2 == 0 && elevationDifference3 == 0);
        if (result)
        {
            if(elevationDifference1 != 0)
            {
                elevationDifference = elevationDifference1;
            }
            else if(elevationDifference2 != 0)
            {
                elevationDifference = elevationDifference2;
            }
            else
            {
                elevationDifference = elevationDifference3;
            }
        }

        return result;
    }

    public static Color GetEdgePointWeight(int elevationDifference, Color color1, Color color2)
    {
        if (elevationDifference == 0)
        {
            return (color1 + color2) / 2f;
        }
        else
        {
            return color1;
        }
    }

    public static Color GetCentralPointWeight(
        int elevationDifference, int elevationSecondDifference, int elevationThirdDifference, 
        Color color1, Color color2, Color color3, Color color4
    )
    {
        float flatCount = 1f;

        if (elevationDifference == 0 || (elevationDifference == 1 && elevationSecondDifference == -1))
        {
            flatCount++;
            color1 += color2;
        }

        if (elevationSecondDifference == 0 || (elevationSecondDifference == 1 && (elevationDifference == -1 || elevationThirdDifference == -1)))
        {
            flatCount++;
            color1 += color3;
        }

        if (elevationThirdDifference == 0 || (elevationThirdDifference == 1 && elevationSecondDifference == -1))
        {
            flatCount++;
            color1 += color4;
        }

        return color1 / flatCount;
    }

    public static Color GetCentralPointWeight(
        int elevation1, int elevation2, int elevation3, int elevation4, 
        Color color1, Color color2, Color color3, Color color4
    )
    {
        float flatCount = 1f;

        if (elevation1 == elevation2 || (elevation1 < elevation2 && elevation1 > elevation3))
        {
            flatCount++;
            color1 += color2;
        }

        if (elevation1 == elevation3 || (elevation1 < elevation3 && (elevation1 > elevation2 || elevation1 > elevation4)))
        {
            flatCount++;
            color1 += color3;
        }

        if (elevation1 == elevation4 || (elevation1 < elevation4 && elevation1 > elevation3))
        {
            flatCount++;
            color1 += color4;
        }

        return color1 / flatCount;
    }

    public static Color GetCentralUpperPointWeight(
        int elevation1, int elevation2, int elevation3, int elevation4, 
        Color color1, Color color2, Color color3, Color color4
    )
    {
        float flatCount = 0f;
        Color color = Color.clear;

        if (elevation2 > elevation1)
        {
            flatCount++;
            color += color2;
        }

        if (elevation4 > elevation1)
        {
            flatCount++;
            color += color4;
        }

        if (elevation3 > elevation1 &&
            (elevation1 >= elevation2 || elevation1 >= elevation4))
        {
            flatCount++;
            color += color3;
        }

        if (flatCount == 0)
            return color1;

        return color / flatCount;
    }

    public static Vector2 GetSlopeAngle(QuadDirection slopeDirection)
    {
        float slopeAngle = Mathf.Atan2(elevationStep, 2f * radius) * Mathf.Rad2Deg;
        Vector2 rotation;
        rotation.x = Math.Sign(-(((int)slopeDirection / 2) & 1) + 1) * Math.Sign((((int)slopeDirection / 2) & 2) - 1) * slopeAngle;
        rotation.y = (((int)slopeDirection / 2) & 1) * Math.Sign(-(((int)slopeDirection / 2) & 2) + 1) * slopeAngle;
        return rotation;
    }

    public static Vector4 SampleNoise(Vector3 position)
    {
        return noiseSource.GetPixelBilinear(position.x * noiseScale, position.z * noiseScale);
    }

    public static Vector3 Perturb(Vector3 position)
    {
        Vector4 sample = SampleNoise(position);
        position.x += (sample.x * 2f - 1f) * cellPerturbStrength;
        position.z += (sample.z * 2f - 1f) * cellPerturbStrength;
        return position;
    }

    public static void InitializeHashGrid(int seed)
    {
        hashGrid = new QuadHash[hashGridSize * hashGridSize];
        Random.State currentState = Random.state;
        Random.InitState(seed);
        for(int i = 0; i < hashGrid.Length; i++)
        {
            hashGrid[i] = QuadHash.Create();
        }
        Random.state = currentState;
    }

    public static QuadHash SampleHashGrid(Vector3 position)
    {
        int x = (int)(position.x * hashGridScale) % hashGridSize;
        if (x < 0)
        {
            x += hashGridSize;
        }
        int z = (int)(position.z * hashGridScale) % hashGridSize;
        if (z < 0)
        {
            z += hashGridSize;
        }
        return hashGrid[x + z * hashGridSize];
    }

    public static float[] GetFeatureThresholds(int level)
    {
        return featureThresholds[level];
    }
}
