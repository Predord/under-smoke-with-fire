using System;
using System.IO;
using UnityEngine;

[Serializable]
public struct QuadCoordinates 
{
    public int X
    {
        get
        {
            return x;
        }
    }

    [SerializeField] private int x;

    public int Z
    {
        get
        {
            return z;
        }
    }

    [SerializeField] private int z;

    public QuadCoordinates(int x, int z)
    {
        this.x = x;
        this.z = z;
    }

    public override string ToString()
    {
        return "(" + X.ToString() + ", " + Z.ToString() + ") ";
    }

    public string ToStringOnSeparateLines()
    {
        return X.ToString() + "\n" + Z.ToString();
    }

    public float DistanceTo(QuadCoordinates toCoordinates)
    {
        float distance = 0f;
        int x = this.x;
        int z = this.z;
        int xDif = Math.Sign(x - toCoordinates.x);
        int zDif = Math.Sign(z - toCoordinates.z);
        while (x != toCoordinates.x && z != toCoordinates.z)
        {
            distance += 1.41421356f;
            x -= xDif;
            z -= zDif;
        }

        distance += (x - toCoordinates.x) * xDif + (z - toCoordinates.z) * zDif;
        return distance;
    }

    //mb change with corners form quadmetrics
    public QuadDirection GetRelativeDirection(QuadCoordinates toCoordinates)
    {
        if (z < toCoordinates.z)
        {
            if(x > toCoordinates.x)
            {
                return QuadDirection.NorthWest;
            }
            else if(x == toCoordinates.x)
            {
                return QuadDirection.North;
            }
            else
            {
                return QuadDirection.NorthEast;
            }
        }
        else if(z > toCoordinates.z)
        {
            if (x > toCoordinates.x)
            {
                return QuadDirection.SouthWest;
            }
            else if (x == toCoordinates.x)
            {
                return QuadDirection.South;
            }
            else
            {
                return QuadDirection.SouthEast;
            }
        }
        else
        {
            if(x < toCoordinates.x)
            {
                return QuadDirection.East;
            }
            else
            {
                return QuadDirection.West;
            }
        }
    }

    public static QuadCoordinates FromPosition(Vector3 position)
    {
        float x = position.x / (QuadMetrics.radius * 2f);
        float z = position.z / (QuadMetrics.radius * 2f);

        return new QuadCoordinates(Mathf.RoundToInt(x), Mathf.RoundToInt(z));
    }

    public void Save(BinaryWriter writer)
    {
        writer.Write(x);
        writer.Write(z);
    }

    public static QuadCoordinates Load(BinaryReader reader)
    {
        QuadCoordinates c;
        c.x = reader.ReadInt32();
        c.z = reader.ReadInt32();
        return c;
    }
}
