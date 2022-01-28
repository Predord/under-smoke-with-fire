using UnityEngine;
using System.Collections.Generic;
using System.IO;

public struct SpecialZone 
{
    public int xLength;
    public int zLength;
    public QuadCoordinates bottomLeftCoordinates;
    public SpecialZoneType zoneType;

    public List<QuadCell> GetSpecialZoneCells()
    {
        List<QuadCell> cells = new List<QuadCell>();

        if(xLength > 0 && zLength > 0)
        {
            for (int z = bottomLeftCoordinates.Z; z < bottomLeftCoordinates.Z + zLength; z++)
            {
                for (int x = bottomLeftCoordinates.X; x < bottomLeftCoordinates.X + xLength; x++)
                {
                    cells.Add(GameManager.Instance.grid.GetCell(new QuadCoordinates(x, z)));
                }
            }
        }

        return cells;
    }

    public void ShowZoneHighlights(Color color)
    {
        if (xLength > 0 && zLength > 0)
        {
            for (int z = bottomLeftCoordinates.Z; z < bottomLeftCoordinates.Z + zLength; z++)
            {
                for (int x = bottomLeftCoordinates.X; x < bottomLeftCoordinates.X + xLength; x++)
                {
                    GameManager.Instance.grid.GetCell(new QuadCoordinates(x, z)).EnableHighlight(color);
                }
            }
        }
    }

    public void ClearZoneHighlights()
    {
        if (xLength > 0 && zLength > 0)
        {
            for (int z = bottomLeftCoordinates.Z; z < bottomLeftCoordinates.Z + zLength; z++)
            {
                for (int x = bottomLeftCoordinates.X; x < bottomLeftCoordinates.X + xLength; x++)
                {
                    GameManager.Instance.grid.GetCell(new QuadCoordinates(x, z)).DisableHighlight();
                }
            }
        }
    }

    public bool IsCellInsideZone(QuadCell cell)
    {
        for (int z = bottomLeftCoordinates.Z; z < bottomLeftCoordinates.Z + zLength; z++)
        {
            for (int x = bottomLeftCoordinates.X; x < bottomLeftCoordinates.X + xLength; x++)
            {
                if(GameManager.Instance.grid.GetCell(new QuadCoordinates(x, z)) == cell)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public QuadCell GetRandomCell(QuadCell ignoredCell, bool ignoreUnits = true, bool checkLockedForCover = false)
    {
        int coordX = Random.Range(bottomLeftCoordinates.X, bottomLeftCoordinates.X + xLength);
        int coordZ = Random.Range(bottomLeftCoordinates.Z, bottomLeftCoordinates.Z + zLength);

        QuadCell cell = GameManager.Instance.grid.GetCell(new QuadCoordinates(coordX, coordZ));

        int endlessLoopCheck = 0;
        while ((cell == ignoredCell || (cell.IsSpecial && !cell.IsSpecialWalkable) || (!ignoreUnits && cell.Unit) || (checkLockedForCover && cell.LockedForCover)) && endlessLoopCheck < 10000)
        {
            coordX = Random.Range(bottomLeftCoordinates.X, bottomLeftCoordinates.X + xLength);
            coordZ = Random.Range(bottomLeftCoordinates.Z, bottomLeftCoordinates.Z + zLength);

            cell = GameManager.Instance.grid.GetCell(new QuadCoordinates(coordX, coordZ));
            endlessLoopCheck++;
        }

        if(endlessLoopCheck == 10000)
        {
            Debug.LogError("No available exists inside zone");
        }

        return cell;
    }

    public QuadCell GetRandomCell()
    {
        int coordX = Random.Range(bottomLeftCoordinates.X, bottomLeftCoordinates.X + xLength);
        int coordZ = Random.Range(bottomLeftCoordinates.Z, bottomLeftCoordinates.Z + zLength);

        QuadCell cell = GameManager.Instance.grid.GetCell(new QuadCoordinates(coordX, coordZ));

        int endlessLoopCheck = 0;
        while ((cell.IsSpecial && !cell.IsSpecialWalkable) && endlessLoopCheck < 10000)
        {
            coordX = Random.Range(bottomLeftCoordinates.X, bottomLeftCoordinates.X + xLength);
            coordZ = Random.Range(bottomLeftCoordinates.Z, bottomLeftCoordinates.Z + zLength);

            cell = GameManager.Instance.grid.GetCell(new QuadCoordinates(coordX, coordZ));
            endlessLoopCheck++;
        }

        if (endlessLoopCheck == 10000)
        {
            Debug.LogError("No available exists inside zone");
        }

        return cell;
    }

    public float DistanceToZone(QuadCoordinates fromCoordinates)
    {
        if(fromCoordinates.Z >= bottomLeftCoordinates.Z && fromCoordinates.Z <= bottomLeftCoordinates.Z + zLength - 1)
        {
            if(fromCoordinates.X < bottomLeftCoordinates.X)
            {
                return bottomLeftCoordinates.X - fromCoordinates.X;
            }
            else if(fromCoordinates.X > bottomLeftCoordinates.X + xLength - 1)
            {
                return fromCoordinates.X - bottomLeftCoordinates.X + xLength - 1;
            }
            else
            {
                return 0;
            }
        }
        else if(fromCoordinates.X >= bottomLeftCoordinates.X && fromCoordinates.X <= bottomLeftCoordinates.X + xLength - 1)
        {
            if (fromCoordinates.Z < bottomLeftCoordinates.Z)
            {
                return bottomLeftCoordinates.Z - fromCoordinates.Z;
            }
            else if (fromCoordinates.Z > bottomLeftCoordinates.Z + zLength - 1)
            {
                return fromCoordinates.Z - bottomLeftCoordinates.Z + zLength - 1;
            }
            else
            {
                return 0;
            }
        }
        else
        {
            if(fromCoordinates.Z < bottomLeftCoordinates.Z)
            {
                if(fromCoordinates.X < bottomLeftCoordinates.X)
                {
                    return fromCoordinates.DistanceTo(bottomLeftCoordinates);
                }
                else
                {
                    return fromCoordinates.DistanceTo(new QuadCoordinates(bottomLeftCoordinates.X + xLength - 1, bottomLeftCoordinates.Z));
                }
            }
            else
            {
                if (fromCoordinates.X < bottomLeftCoordinates.X)
                {
                    return fromCoordinates.DistanceTo(new QuadCoordinates(bottomLeftCoordinates.X, bottomLeftCoordinates.Z + zLength - 1));
                }
                else
                {
                    return fromCoordinates.DistanceTo(new QuadCoordinates(bottomLeftCoordinates.X + xLength - 1, bottomLeftCoordinates.Z + zLength - 1));
                }
            }
        }
    }

    public void Save(BinaryWriter writer)
    {
        writer.Write(xLength);
        writer.Write(zLength);
        bottomLeftCoordinates.Save(writer);
        writer.Write((byte)zoneType);
    }

    public static SpecialZone Load(BinaryReader reader, int header)
    {
        SpecialZone zone;
        zone.xLength = reader.ReadInt32();
        zone.zLength = reader.ReadInt32();
        zone.bottomLeftCoordinates = QuadCoordinates.Load(reader);
        zone.zoneType = (SpecialZoneType)reader.ReadByte();
        return zone;
    }
}
