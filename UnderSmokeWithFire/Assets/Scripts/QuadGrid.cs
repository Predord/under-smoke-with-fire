using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.IO;
using System.Collections.Generic;

public class QuadGrid : MonoBehaviour
{
    public int cellCountX = 140;
    public int cellCountZ = 120;

    public int explorableCountX = 20;
    public int explorableCountZ = 20;

    public int searchFrontierPhase;
    public RectTransform cellLabelPrefab;
    public QuadCell cellPrefab;
    public QuadCell currentSearchToCell;
    public Texture2D noiseSource;
    public Sprite coverFullSprite, coverHalfSprite;
    public RectTransform coverPrefab;
    public QuadGridChunk chunkPrefab;
    public Entity[] unitPrefabs;
    public QuadCellEffects effectsPrefab;
    public QuadCellShaderData cellShaderData;
    public AbilityProjectile projectilePrefab;
    public List<SpecialZone> specialZones = new List<SpecialZone>();
    public List<Entity> units = new List<Entity>();
    public List<EnemyCamp> camps = new List<EnemyCamp>();
    public List<AbilityProjectile> abilityProjectiles = new List<AbilityProjectile>();
    public Dictionary<QuadCell, int> cellsWithHazards = new Dictionary<QuadCell, int>();

    private bool currentDistanceExists;
    private bool currentVisionExists;
    private int chunkCountX;
    private int chunkCountZ;
    private int previousMaxDistance;
    private int previousMaxVision;
    private QuadCoordinates previousSearchFromCoordinates;
    private QuadCoordinates previousVisionFromCoordinates;
    private QuadCell[] cells;
    private QuadGridChunk[] chunks;
    private List<QuadCellEffects> cellsEffects = new List<QuadCellEffects>();
    private Dictionary<int, List<QuadCell>> specialIndexCells = new Dictionary<int, List<QuadCell>>();

    private int[] hazardsTurnsToLast = { 0, 10, 5};

    [System.NonSerialized] private List<QuadCell> frontier;
    [System.NonSerialized] private CellPriorityQueue queueFrontier;

    public bool HasPath { get; set; }

    private void Awake()
    {
        GameManager.Instance.grid = this;
        QuadMetrics.noiseSource = noiseSource;
        QuadMetrics.InitializeHashGrid(GameManager.Instance.seed);
        cellShaderData = gameObject.AddComponent<QuadCellShaderData>();

        CreateMap(cellCountX, cellCountZ);
    }

    private void OnEnable()
    {
        if (!QuadMetrics.noiseSource)
        {
            QuadMetrics.noiseSource = noiseSource;
            QuadMetrics.InitializeHashGrid(GameManager.Instance.seed);
            ResetVisibility();
        }
    }

    public bool CreateMap(int x, int z)
    {
        ClearHighlights();
        ClearUnits();
        ClearProjectiles();
        ClearVariables();

        if (x <= 0 || x % QuadMetrics.chunkSizeX != 0 || z <= 0 || z % QuadMetrics.chunkSizeZ != 0)
        {
            Debug.LogError("Unsupported map size.");
            return false;
        }

        if (chunks != null)
        {
            for (int i = 0; i < chunks.Length; i++)
            {
                Destroy(chunks[i].gameObject);
            }
        }

        cellCountX = x;
        cellCountZ = z;
        chunkCountX = cellCountX / QuadMetrics.chunkSizeX;
        chunkCountZ = cellCountZ / QuadMetrics.chunkSizeZ;

        cellShaderData.Initialize(cellCountX, cellCountZ);
        CreateChunks();
        CreateCells();

        if (CameraMain.Instance)
        {
            CameraMain.Instance.SetBorderCoordinates(
                cells[cellCountX * explorableCountZ + explorableCountX],
                cells[cells.Length - cellCountX * explorableCountZ - explorableCountX]);
        }

        return true;
    }

    private void CreateChunks()
    {
        chunks = new QuadGridChunk[chunkCountX * chunkCountZ];

        for(int z = 0, i = 0; z < chunkCountZ; z++)
        {
            for(int x = 0; x < chunkCountX; x++)
            {
                QuadGridChunk chunk = chunks[i++] = Instantiate(chunkPrefab);
                chunk.transform.SetParent(transform);
            }
        }
    }

    private void CreateCells()
    {
        cells = new QuadCell[cellCountX * cellCountZ];

        for (int z = 0, i = 0; z < cellCountZ; z++)
        {
            for (int x = 0; x < cellCountX; x++)
            {
                CreateCell(x, z, i++);
            }
        }
    }

    private void CreateCell(int x, int z, int i)
    {
        Vector3 position;
        position = new Vector3(x * QuadMetrics.radius * 2f, 0f, z * QuadMetrics.radius * 2f);

        QuadCell cell = cells[i] = Instantiate(cellPrefab);
        cell.transform.localPosition = position;
        cell.coordinates = new QuadCoordinates(x, z);
        cell.Index = i;

        cell.Explorable = x > (explorableCountX - 1) && z > (explorableCountZ - 1) && x < cellCountX - explorableCountX && z < cellCountZ - explorableCountZ;

        if (x > 0)
        {
            cell.SetNeighbor(QuadDirection.West, cells[i - 1]);
        }
        if(z > 0)
        {
            cell.SetNeighbor(QuadDirection.South, cells[i - cellCountX]);
            if((x + 1) % cellCountX != 0)
            {
                cell.SetNeighbor(QuadDirection.SouthEast, cells[i - cellCountX + 1]);
            }
            if(x != 0)
            {
                cell.SetNeighbor(QuadDirection.SouthWest, cells[i - cellCountX - 1]);
            }
        }

        RectTransform label = Instantiate(cellLabelPrefab);
        label.anchoredPosition = new Vector2(position.x, position.z);
        cell.uiRect = label;
        cell.Elevation = 0;

        AddCellToChunk(x, z, cell);
    }

    private void AddCellToChunk(int x, int z, QuadCell cell)
    {
        int chunkX = x / QuadMetrics.chunkSizeX;
        int chunkZ = z / QuadMetrics.chunkSizeZ;
        QuadGridChunk chunk = chunks[chunkX + chunkZ * chunkCountX];

        int localX = x - chunkX * QuadMetrics.chunkSizeX;
        int localZ = z - chunkZ * QuadMetrics.chunkSizeZ;
        chunk.AddCell(localX + localZ * QuadMetrics.chunkSizeX, cell);
    }
    
    public QuadCell GetCell(Vector3 position)
    {
        QuadCoordinates coordinates = QuadCoordinates.FromPosition(position);
        int index = coordinates.X + coordinates.Z * cellCountX;
        return cells[index];
    }

    public QuadCell GetCell(QuadCoordinates coordinates)
    {
        int z = coordinates.Z;
        if (z < 0 || z >= cellCountZ)
            return null;
        int x = coordinates.X;
        if (x < 0 || x >= cellCountX)
            return null;
        return cells[x + z * cellCountX];
    }

    public void AddLargeSpecialCell(QuadCell cell)
    {
        if (specialIndexCells.ContainsKey(cell.LargeSpecialIndex))
        {
            if (!specialIndexCells[cell.LargeSpecialIndex].Any(c => c.coordinates.X == cell.coordinates.X && c.coordinates.Z == cell.coordinates.Z))
            {
                specialIndexCells[cell.LargeSpecialIndex].Add(cell);
            }            
        }
        else
        {
            specialIndexCells.Add(cell.LargeSpecialIndex, new List<QuadCell>());
            specialIndexCells[cell.LargeSpecialIndex].Add(cell);
        }
    }

    public void RemoveLargeSpecialCell(QuadCell cell)
    {
        specialIndexCells[cell.LargeSpecialIndex].Remove(cell);

        foreach (var current in specialIndexCells[cell.LargeSpecialIndex])
        {
            current.RemoveLargeSpecialFeature();
        }

        specialIndexCells.Remove(cell.LargeSpecialIndex);
        cellShaderData.RemoveResetSpecialCellsVisibilityIndices(cell.LargeSpecialIndex);
    }

    public void SetSpecialCellsVisibility(List<int> indices)
    {
        foreach(int key in indices)
        {
            if (IsSpecialCellsVisible(key))
            {
                foreach (var cell in specialIndexCells[key])
                {
                    if (!cell.IsVisible)
                    {
                        cell.SetVisibility(true);
                    }
                }
            }
            else
            {
                foreach (var cell in specialIndexCells[key])
                {
                    if (cell.IsVisible)
                    {
                        cell.SetVisibility(false);
                    }
                }
            }
        }
    }

    private bool IsSpecialCellsVisible(int largeSpecialIndex)
    {
        foreach(var cell in specialIndexCells[largeSpecialIndex])
        {
            if (cell.isPartiallyVisible)
            {
                return true;
            }                
        }

        return false;
    }

    public void ApplyOnEntityAbility(float power, string title, CellHazards hazard, Entity targetedEntity, List<QuadCell> targetedCells)
    {
        GameObject effect = Resources.Load("Abilities/Effects/" + title + "OnEntity") as GameObject;
        StartCoroutine(targetedEntity.PlayOnEntityAbilityAnimation(effect));

        GameManager.Instance.grid.ApplyCellsDamage(power, targetedCells);
        GameManager.Instance.grid.ApplyHazardToTargetedCells(targetedCells, hazard);
    }

    public void InstantiateTrapAbility(int duration, float power, string title, CellHazards hazard, QuadCell mainCell, List<QuadCell> targetedCells)
    {
        Player.Instance.AddTrap(duration, power, title, hazard, mainCell, targetedCells);
    }   

    public void InstantiateAbilityProjectile(bool leaveTrail, int turns, float speedMultiplier, float power, string title, 
        CellHazards hazard, List<QuadCell> targetedCells, List<Vector3> points, List<Vector3> normals)
    {
        AbilityProjectile projectile = Instantiate(projectilePrefab);
        projectile.transform.SetParent(transform, true);
        abilityProjectiles.Add(projectile);
        projectile.FireProjectile(leaveTrail, turns, speedMultiplier, power, title, hazard, targetedCells, points, normals);
    }

    public void SetEffectsClear(QuadCell cell)
    {
        QuadCellEffects effects = cellsEffects.Find(x => x.cell == cell);
        cellsEffects.Remove(effects);
        effects.ClearEffects();
    }

    public void SetCellOnFire(QuadCell cell)
    {
        if(cellsEffects.Exists(effect => effect.cell == cell))
        {
            cellsEffects.Find(effect => effect.cell == cell).StartFire();
        }
        else
        {
            cellsEffects.Add(Instantiate(effectsPrefab));
            cellsEffects[cellsEffects.Count - 1].InstantiateEffect(cell);
            cellsEffects[cellsEffects.Count - 1].StartFire();
        }
    }

    public void SetFireColor(QuadCell cell)
    {
        cellsEffects.Find(x => x.cell == cell).SetFireColor(cell.IsVisible);
    }

    public void SetCellSmoke(QuadCell cell)
    {
        if (cellsEffects.Exists(effect => effect.cell == cell))
        {
            cellsEffects.Find(effect => effect.cell == cell).StartSmoke();
        }
        else
        {
            cellsEffects.Add(Instantiate(effectsPrefab));
            cellsEffects[cellsEffects.Count - 1].InstantiateEffect(cell);
            cellsEffects[cellsEffects.Count - 1].StartSmoke();
        }
    }

    public void GetMoveToCell(QuadCell cell)
    {
        if(currentSearchToCell != null && Player.Instance.Location.coordinates.DistanceTo(currentSearchToCell.coordinates) <= 1.41421356f &&
            Player.Instance.IsNearCellReachable(Player.Instance.Location, currentSearchToCell, Player.Instance.Location.coordinates.GetRelativeDirection(currentSearchToCell.coordinates)))
        {
            currentSearchToCell.EnableHighlight(Color.white);
            currentSearchToCell.DisableCoverHighlight();
        }

        cell.EnableHighlight(Color.blue);
        EnableCoverHighlight(cell);
        currentSearchToCell = cell;
        HasPath = true;
    }

    public float GetPathCost(List<QuadCell> cells)
    {
        float cost;
        //add slope
        if (cells[1].Elevation - 3 >= cells[0].Elevation)
        {
            cost = cells[1].Elevation - cells[0].Elevation;
        }
        else
        {
            cost = 2f;
        }

        for (int i = 1; i < cells.Count - 1; i++)
        {
            if(cells[i + 1].Elevation - 3 >= cells[i].Elevation)
            {
                cost += cells[i + 1].Elevation - cells[i].Elevation;
            }
            else
            {
                if(cells[i + 1].Elevation == cells[i].Elevation || ((cells[i + 1].Slope || cells[i].Slope) &&
                    (cells[i + 1].Elevation - cells[i].Elevation == 1 || cells[i].Elevation - cells[i + 1].Elevation == 1)))
                {
                    cost += 1f;
                }
                else
                {
                    cost += 2f;
                }
            }
        }

        return cost;
    }
    /*
     public List<QuadCell> FindPath(float maxDistance, QuadCell fromCell, QuadCell toCell)
     {
         bool isClimbing = false;
         QuadCell current = toCell;
         List<QuadCell> path = ListPool<QuadCell>.Get();

         while (current != fromCell)
         {
             isClimbing = false;
             if(current.Elevation - 3 >= fromCell.Elevation)
             {
                 isClimbing = true;
                 path.Clear();
             }

             path.Add(current);
             current = current.PathFrom;
         }

         if (!isClimbing && path[0].Elevation - 3 >= fromCell.Elevation)
         {
             path.RemoveAt(0);
         }

         path.Add(current);
         path.Reverse();

         return path.Count > maxDistance ? path.GetRange(0, (int)(1f + maxDistance)) : path;
     }*/

    public List<QuadCell> FindPathClear(QuadCell fromCell, QuadCell toCell)
    {
        QuadCell current = toCell;
        List<QuadCell> path = ListPool<QuadCell>.Get();

        while (current != fromCell)
        {
            path.Add(current);
            current = current.PathFrom;
        }

        path.Add(current);
        path.Reverse();

        return path;
    }

    public List<QuadCell> FindEnemyPath(QuadCell fromCell, QuadCell toCell)
    {
        if(fromCell == toCell)
            return new List<QuadCell> { fromCell };

        QuadCell current = toCell;

        while (current.PathFrom != fromCell)
        {
            current = current.PathFrom;
        }

        return new List<QuadCell> { fromCell, current };
    }

    public void FindPlayerDistance()
    {
        searchFrontierPhase += 2;
        previousMaxDistance = 2;
        previousSearchFromCoordinates = Player.Instance.Location.coordinates;
        Player.Instance.Location.EnableHighlight(Color.white);

        for (QuadDirection direction = QuadDirection.North; direction <= QuadDirection.NorthWest; direction++)
        {
            QuadCell neighbor = Player.Instance.Location.GetNeighbor(direction);
            if(Player.Instance.IsNearCellReachable(Player.Instance.Location, neighbor, direction))
            {
                neighbor.SearchDistancePhase = searchFrontierPhase + 1;
                neighbor.PathFrom = Player.Instance.Location;
                neighbor.EnableHighlight(Color.white);
            }
        }

        currentDistanceExists = true;
        HasPath = false;
    }

    public bool FindDistanceHeuristic(Entity unit, QuadCell fromCell, QuadCell toCell, bool ignoreCellUnit = false, bool checkCellExplored = false, bool checkLockedCells = false, bool checkLockedCover = false)
    {
        if (toCell == null || (toCell.IsSpecial && !toCell.IsSpecialWalkable) || (toCell.Unit && !ignoreCellUnit) || !toCell.Explorable || (checkCellExplored && !toCell.IsExplored) ||
            (checkLockedCells && toCell.LockedForTravel) || (checkLockedCover && toCell.LockedForCover))
        {
            return false;
        }

        for (QuadDirection direction = QuadDirection.North; direction <= QuadDirection.NorthWest; direction++)
        {
            QuadCell cell = toCell.GetNeighbor(direction);
            if (unit.GetMovePriority(cell, toCell, direction.Opposite()) > 0f)
                break;

            if (direction == QuadDirection.NorthWest)
            {
                return false;
            }
        }

        searchFrontierPhase += 2;
        if (queueFrontier == null)
        {
            queueFrontier = new CellPriorityQueue();
        }
        else
        {
            queueFrontier.Clear();
        }

        fromCell.SearchDistancePhase = searchFrontierPhase;
        fromCell.Distance = 0;
        queueFrontier.Enqueue(fromCell);

        while (queueFrontier.Count > 0)
        {
            QuadCell current = queueFrontier.Dequeue();
            current.SearchDistancePhase += 1;

            if (current == toCell)
            {
                return true;
            }

            for (QuadDirection direction = QuadDirection.North; direction <= QuadDirection.NorthWest; direction++)
            {
                QuadCell neighbor = current.GetNeighbor(direction);
                //mb change
                if (neighbor == null || (neighbor.IsSpecial && !neighbor.IsSpecialWalkable) || neighbor.SearchDistancePhase > searchFrontierPhase ||
                    (neighbor.Unit && (neighbor != toCell || !ignoreCellUnit)) || !neighbor.Explorable || (checkCellExplored && !neighbor.IsExplored) ||
                    (checkLockedCells && neighbor.LockedForTravel))
                {
                    continue;
                }

                float movePriority = unit.GetMovePriority(current, neighbor, direction);

                if (movePriority < 0f)
                    continue;

                if (neighbor.SearchDistancePhase < searchFrontierPhase)
                {
                    neighbor.SearchDistancePhase = searchFrontierPhase;
                    neighbor.Distance = movePriority;
                    neighbor.PathFrom = current;
                    neighbor.SearchHeuristic = neighbor.coordinates.DistanceTo(toCell.coordinates);
                    queueFrontier.Enqueue(neighbor);
                }
                else if (movePriority < neighbor.Distance)
                {
                    float oldPriority = neighbor.SearchPriority;
                    neighbor.Distance = movePriority;
                    neighbor.PathFrom = current;
                    queueFrontier.Change(neighbor, oldPriority);
                }
            }
        }

        return false;
    }

    public QuadCell FindCellWithCover(Entity unit, QuadDirection priorityDirection1, QuadDirection priorityDirection2, float maxDistance)
    {
        searchFrontierPhase += 2;
        if (frontier == null)
        {
            frontier = ListPool<QuadCell>.Get();
        }

        unit.Location.SearchDistancePhase = searchFrontierPhase;
        unit.Location.Distance = 0;
        frontier.Add(unit.Location);
        QuadCell priorityCell = unit.Location;

        while (frontier.Count > 0)
        {
            QuadCell current = frontier[0];
            frontier.RemoveAt(0);
            current.SearchDistancePhase += 1;

            if(current.HasCoverInDirection((int)priorityDirection1))
            {
                if(priorityDirection1 != priorityDirection2 && 
                    ((priorityDirection1 == QuadDirection.North && 
                        Player.Instance.Location.coordinates.Z - current.coordinates.Z < Player.Instance.Location.coordinates.X - current.coordinates.X) ||
                    (priorityDirection1 == QuadDirection.East &&
                        current.coordinates.Z - Player.Instance.Location.coordinates.Z > Player.Instance.Location.coordinates.X - current.coordinates.X) ||
                    (priorityDirection1 == QuadDirection.South &&
                        current.coordinates.X - Player.Instance.Location.coordinates.X > current.coordinates.Z - Player.Instance.Location.coordinates.Z) ||
                    (priorityDirection1 == QuadDirection.West &&
                        current.coordinates.X - Player.Instance.Location.coordinates.X < Player.Instance.Location.coordinates.Z - current.coordinates.Z)))
                {
                    priorityCell = current;
                }
                else
                {
                    ListPool<QuadCell>.Add(frontier);
                    return current;
                }
            }
            else if (current.HasCoverInDirection((int)priorityDirection2))
            {
                if (priorityDirection1 != priorityDirection2 && 
                    ((priorityDirection1 == QuadDirection.North &&
                        Player.Instance.Location.coordinates.X - current.coordinates.X < Player.Instance.Location.coordinates.Z - current.coordinates.Z) ||
                    (priorityDirection1 == QuadDirection.East &&
                        current.coordinates.X - Player.Instance.Location.coordinates.X > Player.Instance.Location.coordinates.Z - current.coordinates.Z) ||
                    (priorityDirection1 == QuadDirection.South &&
                        current.coordinates.Z - Player.Instance.Location.coordinates.Z > current.coordinates.X - Player.Instance.Location.coordinates.X) ||
                    (priorityDirection1 == QuadDirection.West &&
                        current.coordinates.Z - Player.Instance.Location.coordinates.Z < Player.Instance.Location.coordinates.X - current.coordinates.X)))
                {
                    priorityCell = current;
                }
                else
                {
                    ListPool<QuadCell>.Add(frontier);
                    return current;
                }
            }

            if (current.Distance > maxDistance)
            {
                continue;
            }

            for (QuadDirection direction = QuadDirection.North; direction <= QuadDirection.NorthWest; direction++)
            {
                QuadCell neighbor = current.GetNeighbor(direction);
                if (neighbor == null || (neighbor.IsSpecial && !neighbor.IsSpecialWalkable) || neighbor.SearchDistancePhase > searchFrontierPhase 
                    || neighbor.Unit || !neighbor.Explorable || neighbor.LockedForCover)
                {
                    continue;
                }

                float movePriority = unit.GetMovePriority(current, neighbor, direction);

                if (movePriority < 0f)
                    continue;

                if (neighbor.SearchDistancePhase < searchFrontierPhase)
                {
                    neighbor.SearchDistancePhase = searchFrontierPhase;
                    neighbor.Distance = movePriority;
                    neighbor.PathFrom = current;
                    frontier.Add(neighbor);
                }
                else if (movePriority < neighbor.Distance)
                {
                    neighbor.Distance = movePriority;
                    neighbor.PathFrom = current;
                }
            }
        }

        ListPool<QuadCell>.Add(frontier);
        return priorityCell;
    }

    public void EnablePathHighlights(QuadCell fromCell, QuadCell toCell)
    {
        QuadCell current = toCell;

        while (current != fromCell)
        {
            current.EnableHighlight(Color.blue);
            current = current.PathFrom;
        }

        current.EnableHighlight(Color.white);
        EnableCoverHighlight(toCell);
        currentSearchToCell = toCell;
    }

    public void DisablePathHighlights(List<QuadCell> cells)
    {
        foreach(QuadCell cell in cells)
        {
            cell.DisableHighlight();
        }

        if (currentSearchToCell)
        {
            currentSearchToCell.DisableCoverHighlight();
        }
    }

    public void ClearDistance()
    {
        if (currentDistanceExists)
        {
            int minX = previousSearchFromCoordinates.X - previousMaxDistance < 0 ? 0 : previousSearchFromCoordinates.X - previousMaxDistance;
            int maxX = previousSearchFromCoordinates.X + previousMaxDistance >= cellCountX ? cellCountX - 1 : previousSearchFromCoordinates.X + previousMaxDistance;
            for (int z = previousSearchFromCoordinates.Z - previousMaxDistance < 0 ? 0 : previousSearchFromCoordinates.Z - previousMaxDistance; 
                z <= (previousSearchFromCoordinates.Z + previousMaxDistance >= cellCountZ ? cellCountZ - 1 : previousSearchFromCoordinates.Z + previousMaxDistance); z++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    cells[x + z * cellCountX].DisableHighlight();
                }
            }
        }

        if (currentSearchToCell)
        {
            currentSearchToCell.DisableCoverHighlight();
        }
        currentDistanceExists = false;
    }

    public void ApplyCellsDamage(float damage, List<QuadCell> cells)
    {
        foreach(QuadCell cell in cells)
        {
            if(cell.Unit != null)
            {
                cell.Unit.Health -= damage;
            }
        }
    }

    public void ApplyHazardToTargetedCells(List<QuadCell> cells, CellHazards hazard)
    {
        if (hazard == CellHazards.None)
            return;

        List<QuadCell> affectedCells = new List<QuadCell>();

        //change different cell distribution for smoke
        if (hazard == CellHazards.Fire || hazard == CellHazards.Smoke)
        {
            for(int i = 0; i < cells.Count - 1; i++)
            {
                affectedCells.Add(cells[i]);
                //mb change to enum
                /*
                if (cells[i].TerrainTypeIndex == 1 && !cells[i].IsUnderwater && cells[i].CellHazard != hazard)
                {
                    
                }*/
            }

            /*
            if(cells[cells.Count - 1].TerrainTypeIndex == 1 && !cells[cells.Count - 1].IsUnderwater && cells[cells.Count - 1].CellHazard != hazard)
            {
                cells[cells.Count - 1].CellHazard = hazard;
            }*/
        }

        if (affectedCells.Count == 0)
            return;

        System.Random random = new System.Random();
        float affectedCellsRange =
            random.Next(Mathf.Max(1, affectedCells.Count / 2), Mathf.Min(affectedCells.Count, Mathf.Max(1, affectedCells.Count / 2) + 1));

        for (int i = 0; i < affectedCells.Count; i++)
        {
            if (Random.Range(0f, 1f) <= affectedCellsRange / (affectedCells.Count - i))
            {
                affectedCells[i].CellHazard = hazard;
                affectedCellsRange--;
            }
        }
    }

    public void AddCellWithHazard(QuadCell cell)
    {
        if (cellsWithHazards.ContainsKey(cell))
        {
            cellsWithHazards[cell] = hazardsTurnsToLast[(int)cell.CellHazard];
        }
        else
        {
            cellsWithHazards.Add(cell, hazardsTurnsToLast[(int)cell.CellHazard]);
        }
    }

    public void RemoveCellWithHazard(QuadCell cell)
    {
        cellsWithHazards.Remove(cell);
    }

    public void RemoveTurnFromCellsWithHazards()
    {
        foreach (var cell in cellsWithHazards.Keys.ToList())
        {
            cellsWithHazards[cell] -= 1;
            if(cellsWithHazards[cell] <= 0)
            {
                cell.CellHazard = CellHazards.None;
            }
        }
    } 

    public void HighlightAll()
    {
        if (cells != null)
        {
            for (int z = explorableCountZ; z < cellCountZ - explorableCountZ; z++)
            {
                for (int x = explorableCountX; x < cellCountX - explorableCountX; x++)
                {
                    cells[x + z * cellCountX].EnableHighlight(Color.white);
                }
            }
        }
    }

    public void ClearHighlightsWithBoundaries()
    {
        if(cells != null)
        {
            for (int z = 0; z < cellCountZ; z++)
            {
                for (int x = 0; x < cellCountX; x++)
                {
                    cells[x + z * cellCountX].DisableHighlight();
                }
            }

            if (currentSearchToCell)
            {
                currentSearchToCell.DisableCoverHighlight();
            }
        }
    }

    public void ClearHighlightsAroundPlayer()
    {
        Player.Instance.Location.DisableHighlight();
        for(int i = 0; i < 8; i++)
        {
            Player.Instance.Location.GetNeighbor((QuadDirection)i).DisableHighlight();
        }
    }

    public void ClearHighlights()
    {
        if (cells != null)
        {
            for (int z = explorableCountZ; z < cellCountZ - explorableCountZ; z++)
            {
                for (int x = explorableCountX; x < cellCountX - explorableCountX; x++)
                {
                    cells[x + z * cellCountX].DisableHighlight();                  
                }
            }

            if (currentSearchToCell)
            {
                currentSearchToCell.DisableCoverHighlight();
            }

            foreach (var specialZone in specialZones)
            {
                specialZone.ClearZoneHighlights();
            }
        }
    }

    public void SetVisibleCells(QuadCell cell, float range)
    {
        previousMaxVision = Mathf.FloorToInt(range + 1f);
        previousVisionFromCoordinates = cell.coordinates;
        cell.IsVisible = true;

        float cellViewElevation = cell.Position.y + Player.Instance.Height + (cell.Slope ? 1f : 0f);
        for(QuadDirection direction = 0; direction <= QuadDirection.NorthWest; direction++)
        {
            QuadCell neighbor = cell.GetNeighbor(direction);
            if (neighbor == null)
                continue;

            if (!neighbor.Explorable)
            {
                neighbor.IsVisible = true;
                continue;
            }

            if (((int)direction & 1) != 0)
            {
                if (cellViewElevation >= neighbor.TargetViewElevation)
                {
                    if (cell.GetNeighbor(direction.Previous()).BlockViewElevation < cellViewElevation + 1f ||
                        cell.GetNeighbor(direction.Next()).BlockViewElevation < cellViewElevation + 1f)
                    {
                        neighbor.IsVisible = true;
                    }
                }
                else
                {
                    if (cell.GetNeighbor(direction.Previous()).BlockViewElevation < neighbor.TargetViewElevation ||
                        cell.GetNeighbor(direction.Next()).BlockViewElevation < neighbor.TargetViewElevation)
                    {
                        neighbor.IsVisible = true;
                    }
                }
            }
            else
            {
               neighbor.IsVisible = true;
            }
        }

        for (int i = 0; i < 4; i++)
        {
            QuadDirection direction = (QuadDirection)(i * 2);
            QuadCell currentBlock = cell.GetNeighbor(direction);
            if (currentBlock == null)
                continue;

            QuadCell current = currentBlock.GetNeighbor(direction);

            if (current == null)
                continue;

            float currentDistance = 2f;
            while (currentBlock.BlockViewElevation <= cellViewElevation && currentDistance <= range)
            {                
                if (current.TargetViewElevation >= currentBlock.BlockViewElevation ||
                    cell.IsCellVisible(new Vector3(currentBlock.Position.x, currentBlock.BlockViewElevation, currentBlock.Position.z) + QuadMetrics.GetFirstSolidCorner(direction),
                    current.Position + QuadMetrics.GetFirstSolidCorner(direction) + Vector3.up * (current.TargetViewElevation - current.BlockViewElevation)))
                {
                    current.IsVisible = true;
                    if(current.BlockViewElevation == currentBlock.BlockViewElevation)
                    {
                        currentBlock = current;
                    }
                    else
                    {
                        if(current.BlockViewElevation >= currentBlock.BlockViewElevation ||
                            cell.IsCellVisible(new Vector3(currentBlock.Position.x, currentBlock.BlockViewElevation, currentBlock.Position.z) + QuadMetrics.GetFirstSolidCorner(direction), 
                            current.Position + QuadMetrics.GetFirstSolidCorner(direction)))
                        {
                            currentBlock = current;
                        }
                    }             
                }

                current = current.GetNeighbor(direction);
                currentDistance++;
                if (current == null)
                    break;
            }

            if (current == null)
                continue;

            for(; currentDistance <= range; currentDistance++)
            {
                if (current.TargetViewElevation > currentBlock.BlockViewElevation &&
                    cell.IsCellVisible(new Vector3(currentBlock.Position.x, currentBlock.BlockViewElevation, currentBlock.Position.z) + QuadMetrics.GetFirstSolidCorner(direction.Opposite()),
                    current.Position + QuadMetrics.GetFirstSolidCorner(direction.Opposite()) + Vector3.up * (current.TargetViewElevation - current.BlockViewElevation), true))
                {
                    current.IsVisible = true;
                    if (current.BlockViewElevation == currentBlock.BlockViewElevation)
                    {
                        currentBlock = current;
                    }
                    else
                    {
                        if (current.BlockViewElevation > currentBlock.BlockViewElevation &&
                            cell.IsCellVisible(new Vector3(currentBlock.Position.x, currentBlock.BlockViewElevation, currentBlock.Position.z) + QuadMetrics.GetFirstSolidCorner(direction.Opposite()), 
                            current.Position + QuadMetrics.GetFirstSolidCorner(direction.Opposite())))
                        {
                            currentBlock = current;
                        }
                    }
                }

                current = current.GetNeighbor(direction);
                if (current == null)
                    break;
            }
        }

        for (int i = 0; i < 4; i++)
        {
            QuadDirection direction1 = (QuadDirection)(i * 2);
            QuadDirection direction2 = direction1.Next();
            QuadDirection direction3 = direction1.Next2();
            QuadCell diagonalCell = cell.GetNeighbor(direction2);

            if (diagonalCell == null || (cell.GetNeighbor(direction1).BlockViewElevation >= cellViewElevation &&
                cell.GetNeighbor(direction3).BlockViewElevation >= cellViewElevation))
                continue;

            Vector3 diagonalBlock1, diagonalBlock2;
            if (diagonalCell.BlockViewElevation >= diagonalCell.GetNeighbor(direction3.Opposite()).BlockViewElevation ||
                diagonalCell.BlockViewElevation >= diagonalCell.GetNeighbor(direction1.Opposite()).BlockViewElevation)
            {
                if(diagonalCell.BlockViewElevation > cellViewElevation)
                {
                    diagonalBlock1 = diagonalCell.Position + QuadMetrics.GetFirstCorner(direction2.Opposite());
                    diagonalBlock1 = diagonalBlock2 = new Vector3(diagonalBlock1.x, diagonalCell.BlockViewElevation, diagonalBlock1.z);
                }
                else
                {
                    diagonalBlock1 = diagonalCell.Position + QuadMetrics.GetFirstCorner(direction2);
                    diagonalBlock1 = diagonalBlock2 = new Vector3(diagonalBlock1.x, diagonalCell.BlockViewElevation, diagonalBlock1.z);
                }
            }
            else
            {
                diagonalBlock1 = diagonalCell.GetNeighbor(direction3.Opposite()).Position + QuadMetrics.GetFirstCorner(direction2.Opposite());
                diagonalBlock1 = new Vector3(diagonalBlock1.x, diagonalCell.GetNeighbor(direction3.Opposite()).BlockViewElevation, diagonalBlock1.z);
                diagonalBlock2 = diagonalCell.GetNeighbor(direction1.Opposite()).Position + QuadMetrics.GetFirstCorner(direction2.Opposite());
                diagonalBlock2 = new Vector3(diagonalBlock2.x, diagonalCell.GetNeighbor(direction1.Opposite()).BlockViewElevation, diagonalBlock2.z);
            }

            QuadCell currentUp = diagonalCell.GetNeighbor(direction1);
            QuadCell currentRight = diagonalCell.GetNeighbor(direction3);

            float diagonalDistance = 1.41421356f;
            float upDistance = 2.41421356f;
            float rightDistance = 2.41421356f;

            while (true)
            {
                GetDiagonalTriangleVision(cell, currentUp, direction1, upDistance, range);
                GetDiagonalTriangleVision(cell, currentRight, direction3, rightDistance, range);

                diagonalDistance += 1.41421356f;
                diagonalCell = diagonalCell.GetNeighbor(direction2);
                if (diagonalDistance > range || diagonalCell == null)
                    break;

                upDistance = rightDistance = diagonalDistance + 1f;

                currentUp = diagonalCell.GetNeighbor(direction1);
                currentRight = diagonalCell.GetNeighbor(direction3);

                GetDiagonalCellVision(cellViewElevation, direction1, direction2, direction3, ref diagonalBlock1, ref diagonalBlock2, cell, diagonalCell);
            }
        }
        currentVisionExists = true;
    }

    public void GetDiagonalTriangleVision(QuadCell cell, QuadCell current, QuadDirection direction, float distance, float range)
    {
        if (current != null)
        {
            while (distance <= range)
            {
                Vector3 position = new Vector3(current.Position.x - cell.Position.x, 0f, current.Position.z - cell.Position.z).normalized
                    * QuadMetrics.radius * 0.98f;
                position = Quaternion.Euler(0, 90f, 0) * position;

                //mb change - 0.1f to smth more accurate
                //increase method calls for more accurate results
                Vector2 center = new Vector2(current.Position.x, current.Position.z);
                Vector2 edgePosition1 = new Vector2(current.Position.x - position.x, current.Position.z - position.z);
                Vector2 edgePosition2 = new Vector2(current.Position.x + position.x, current.Position.z + position.z);

                current.IsVisible = GetCellAngleVision(cell, current, center, current.TargetViewElevation, Player.Instance.Height) ||
                    GetCellAngleVision(cell, current, Vector2.Lerp(center, edgePosition1, 1f / 3f), current.TargetViewElevation, Player.Instance.Height) ||
                    GetCellAngleVision(cell, current, Vector2.Lerp(center, edgePosition2, 1f / 3f), current.TargetViewElevation, Player.Instance.Height) ||
                    GetCellAngleVision(cell, current, Vector2.Lerp(center, edgePosition1, 2f / 3f), current.TargetViewElevation, Player.Instance.Height) ||
                    GetCellAngleVision(cell, current, Vector2.Lerp(center, edgePosition2, 2f / 3f), current.TargetViewElevation, Player.Instance.Height) ||
                    GetCellAngleVision(cell, current, edgePosition1, current.TargetViewElevation, Player.Instance.Height) ||
                    GetCellAngleVision(cell, current, edgePosition2, current.TargetViewElevation, Player.Instance.Height);

                current = current.GetNeighbor(direction);
                if (current == null)
                    break;

                distance++;
            }
        }
    }

    public void GetDiagonalCellVision(float cellViewElevation, QuadDirection direction1, QuadDirection direction2, QuadDirection direction3, 
        ref Vector3 diagonalBlock1, ref Vector3 diagonalBlock2, QuadCell cell, QuadCell diagonalCell)
    {
        Vector3 tempBlock = diagonalCell.GetNeighbor(direction3.Opposite()).Position + QuadMetrics.GetSecondCorner(direction3);
        tempBlock = new Vector3(tempBlock.x, diagonalCell.GetNeighbor(direction3.Opposite()).BlockViewElevation, tempBlock.z);

        if (diagonalBlock1.y > cellViewElevation)
        {
            if (tempBlock.y - diagonalBlock1.y > 0.5f * QuadMetrics.elevationStep && cell.IsCellVisible(diagonalBlock1, tempBlock, true))
            {
                diagonalBlock1 = tempBlock;
            }
        }
        else
        {
            if (tempBlock.y - diagonalBlock1.y > -0.5f * QuadMetrics.elevationStep || cell.IsCellVisible(diagonalBlock1, tempBlock))
            {
                diagonalBlock1 = tempBlock;
            }
        }

        tempBlock = diagonalCell.GetNeighbor(direction1.Opposite()).Position + QuadMetrics.GetFirstCorner(direction1.Previous());
        tempBlock = new Vector3(tempBlock.x, diagonalCell.GetNeighbor(direction1.Opposite()).BlockViewElevation, tempBlock.z);

        if (diagonalBlock2.y > cellViewElevation)
        {
            if (tempBlock.y - diagonalBlock2.y > 0.5f * QuadMetrics.elevationStep && cell.IsCellVisible(diagonalBlock2, tempBlock, true))
            {
                diagonalBlock2 = tempBlock;
            }
        }
        else
        {
            if (tempBlock.y - diagonalBlock2.y > -0.5f * QuadMetrics.elevationStep || cell.IsCellVisible(diagonalBlock2, tempBlock))
            {
                diagonalBlock2 = tempBlock;
            }
        }

        float additionalViewElevation = diagonalCell.TargetViewElevation - diagonalCell.BlockViewElevation;
        if (diagonalCell.TargetViewElevation > cellViewElevation)
        {
            //check if x && z equals
            //+ Vector3.up * additionalViewElevation
            tempBlock = diagonalCell.Position + QuadMetrics.GetFirstCorner(direction2.Opposite());
            tempBlock = new Vector3(tempBlock.x, diagonalCell.BlockViewElevation + additionalViewElevation, tempBlock.z);

            if ((diagonalCell.TargetViewElevation - diagonalBlock1.y > 0.5f * QuadMetrics.elevationStep && cell.IsCellVisible(diagonalBlock1, tempBlock, true)) ||
                (diagonalCell.TargetViewElevation - diagonalBlock2.y > 0.5f * QuadMetrics.elevationStep && cell.IsCellVisible(diagonalBlock2, tempBlock, true)))
            {
                diagonalCell.IsVisible = true;
                if (additionalViewElevation == 0)
                {
                    diagonalBlock1 = diagonalBlock2 = tempBlock;
                }
                else
                {
                    tempBlock -= Vector3.up * additionalViewElevation;
                    if ((diagonalCell.BlockViewElevation - diagonalBlock1.y > 0.5f * QuadMetrics.elevationStep && cell.IsCellVisible(diagonalBlock1, tempBlock, true)) ||
                        (diagonalCell.BlockViewElevation - diagonalBlock2.y > 0.5f * QuadMetrics.elevationStep && cell.IsCellVisible(diagonalBlock2, tempBlock, true)))
                    {
                        diagonalBlock1 = diagonalBlock2 = tempBlock;
                    }
                }
            }
        }
        else
        {
            //check if x && z equals
            //+ Vector3.up * additionalViewElevation
            tempBlock = diagonalCell.Position + QuadMetrics.GetFirstCorner(direction2);
            tempBlock = new Vector3(tempBlock.x, diagonalCell.BlockViewElevation + additionalViewElevation, tempBlock.z);

            if (diagonalCell.TargetViewElevation - diagonalBlock1.y > -0.5f * QuadMetrics.elevationStep || cell.IsCellVisible(diagonalBlock1, tempBlock) ||
                diagonalCell.TargetViewElevation - diagonalBlock2.y > -0.5f * QuadMetrics.elevationStep || cell.IsCellVisible(diagonalBlock2, tempBlock))
            {
                diagonalCell.IsVisible = true;
                if (additionalViewElevation == 0)
                {
                    diagonalBlock1 = diagonalBlock2 = tempBlock;
                }
                else
                {
                    tempBlock -= Vector3.up * additionalViewElevation;
                    if (diagonalCell.BlockViewElevation - diagonalBlock1.y > -0.5f * QuadMetrics.elevationStep || cell.IsCellVisible(diagonalBlock1, tempBlock) ||
                        diagonalCell.BlockViewElevation - diagonalBlock2.y > -0.5f * QuadMetrics.elevationStep || cell.IsCellVisible(diagonalBlock2, tempBlock))
                    {
                        diagonalBlock1 = diagonalBlock2 = tempBlock;
                    }
                }
            }
        }
    }

    public void CheckCellVisible(QuadCell cell, float height)
    {
        if (!cell.IsVisible && (cell.coordinates.DistanceTo(Player.Instance.Location.coordinates) <= Player.Instance.VisionRange))
        {
            if (cell.coordinates.X != Player.Instance.Location.coordinates.X && cell.coordinates.Z != Player.Instance.Location.coordinates.Z)
            {
                if (Mathf.Abs(cell.coordinates.X - Player.Instance.Location.coordinates.X) == 1 &&
                    Mathf.Abs(cell.coordinates.Z - Player.Instance.Location.coordinates.Z) == 1)
                {
                    QuadDirection direction;
                    if (cell.coordinates.X > Player.Instance.Location.coordinates.X)
                    {
                        if (cell.coordinates.Z > Player.Instance.Location.coordinates.Z)
                        {
                            direction = QuadDirection.NorthEast;
                        }
                        else
                        {
                            direction = QuadDirection.SouthEast;
                        }
                    }
                    else
                    {
                        if (cell.coordinates.Z > Player.Instance.Location.coordinates.Z)
                        {
                            direction = QuadDirection.NorthWest;
                        }
                        else
                        {
                            direction = QuadDirection.SouthWest;
                        }
                    }
                    cell.IsVisible = GameManager.Instance.grid.GetCellRoundVision(direction, height);
                }
                else
                {
                    Vector3 position = new Vector3(cell.Position.x - Player.Instance.Location.Position.x, 0f, cell.Position.z - Player.Instance.Location.Position.z).normalized
                        * QuadMetrics.radius * 0.98f;
                    position = Quaternion.Euler(0, 90f, 0) * position;
                    //cell.Unit change
                    cell.IsVisible = GameManager.Instance.grid.GetCellAngleVision(Player.Instance.Location, cell,
                            new Vector2(cell.Position.x - 0.1f * QuadMetrics.radius, cell.Position.z - 0.1f * QuadMetrics.radius),
                            cell.TargetViewElevation + (cell.Unit ? 0f : height), Player.Instance.Height) ||
                        GameManager.Instance.grid.GetCellAngleVision(Player.Instance.Location, cell,
                            new Vector2(cell.Position.x - position.x, cell.Position.z - position.z),
                            cell.TargetViewElevation + (cell.Unit ? 0f : height), Player.Instance.Height) ||
                        GameManager.Instance.grid.GetCellAngleVision(Player.Instance.Location, cell,
                            new Vector2(cell.Position.x + position.x, cell.Position.z + position.z),
                            cell.TargetViewElevation + (cell.Unit ? 0f : height), Player.Instance.Height);
                }
            }
            else
            {
                if (Mathf.Abs(cell.coordinates.X - Player.Instance.Location.coordinates.X) == 1 ||
                    Mathf.Abs(cell.coordinates.Z - Player.Instance.Location.coordinates.Z) == 1)
                {
                    cell.IsVisible = true;
                }
                else
                {
                    QuadDirection direction;
                    if (cell.coordinates.X == Player.Instance.Location.coordinates.X)
                    {
                        if (cell.coordinates.Z > Player.Instance.Location.coordinates.Z)
                        {
                            direction = QuadDirection.North;
                        }
                        else
                        {
                            direction = QuadDirection.South;
                        }
                    }
                    else
                    {
                        if (cell.coordinates.X > Player.Instance.Location.coordinates.X)
                        {
                            direction = QuadDirection.East;
                        }
                        else
                        {
                            direction = QuadDirection.West;
                        }
                    }
                    cell.IsVisible = GameManager.Instance.grid.GetCellLineVision(Player.Instance.Location, cell, direction, 
                        Player.Instance.Location.coordinates.DistanceTo(cell.coordinates), Player.Instance.Height, height);
                }
            }
        }
    }

    private bool GetCellRoundVision(QuadDirection direction, float unitHeight)
    {
        QuadCell neighbor = Player.Instance.Location.GetNeighbor(direction);

        float cellViewElevation = Player.Instance.Location.Position.y + Player.Instance.Height + (Player.Instance.Location.Slope ? 1f : 0f);
        float targetViewElevation = neighbor.TargetViewElevation + (neighbor.Unit ? 0f : unitHeight);
        if (cellViewElevation >= targetViewElevation)
        {
            //change with lean mb
            if (Player.Instance.Location.GetNeighbor(direction.Previous()).BlockViewElevation < cellViewElevation + 1f ||
                Player.Instance.Location.GetNeighbor(direction.Next()).BlockViewElevation < cellViewElevation + 1f)
            {
                return true;
            }
        }
        else
        {
            if (Player.Instance.Location.GetNeighbor(direction.Previous()).BlockViewElevation < targetViewElevation ||
                Player.Instance.Location.GetNeighbor(direction.Next()).BlockViewElevation < targetViewElevation)
            {
                return true;
            }
        }

        return false;
    }

    public bool GetCellLineVision(QuadCell location, QuadCell target, QuadDirection direction, float range, float unitHeight, float targetUnitHeight)
    {
        QuadCell currentBlock = location.GetNeighbor(direction);
        QuadCell current = currentBlock.GetNeighbor(direction);
        //check if leaning

        float currentDistance = 2f;
        float cellViewElevation = location.Position.y + unitHeight + (location.Slope ? 1f : 0f);
        while (currentBlock.BlockViewElevation <= cellViewElevation && currentDistance < range)
        {
            if (current.TargetViewElevation >= currentBlock.BlockViewElevation ||
                location.IsCellVisible(currentBlock.Position + QuadMetrics.GetFirstSolidCorner(direction),
                current.Position + QuadMetrics.GetFirstSolidCorner(direction) + Vector3.up * (current.TargetViewElevation - current.BlockViewElevation)))
            {
                if (current.BlockViewElevation == currentBlock.BlockViewElevation)
                {
                    currentBlock = current;
                }
                else
                {
                    if (current.BlockViewElevation >= currentBlock.BlockViewElevation ||
                        location.IsCellVisible(currentBlock.Position + QuadMetrics.GetFirstSolidCorner(direction), current.Position + QuadMetrics.GetFirstSolidCorner(direction)))
                    {
                        currentBlock = current;
                    }
                }
            }

            current = current.GetNeighbor(direction);
            currentDistance++;
        }

        float targetViewElevation = target.TargetViewElevation + (target.Unit ? 0f : targetUnitHeight);
        if (currentDistance >= range)
        {
            if (targetViewElevation >= currentBlock.BlockViewElevation ||
                location.IsCellVisible(currentBlock.Position + QuadMetrics.GetFirstSolidCorner(direction),
                target.Position + QuadMetrics.GetFirstSolidCorner(direction) + Vector3.up * (targetViewElevation - target.BlockViewElevation)))
            {
                return true;
            }

            return false;
        }

        for (; currentDistance < range; currentDistance++)
        {
            if (current.TargetViewElevation > currentBlock.BlockViewElevation &&
                location.IsCellVisible(currentBlock.Position + QuadMetrics.GetFirstSolidCorner(direction.Opposite()),
                current.Position + QuadMetrics.GetFirstSolidCorner(direction.Opposite()) + Vector3.up * (current.TargetViewElevation - current.BlockViewElevation), true))
            {
                if (current.BlockViewElevation == currentBlock.BlockViewElevation)
                {
                    currentBlock = current;
                }
                else
                {
                    if (current.BlockViewElevation > currentBlock.BlockViewElevation &&
                        location.IsCellVisible(currentBlock.Position + QuadMetrics.GetFirstSolidCorner(direction.Opposite()), current.Position + QuadMetrics.GetFirstSolidCorner(direction.Opposite())))
                    {
                        currentBlock = current;
                    }
                }
            }

            current = current.GetNeighbor(direction);
        }

        if (targetViewElevation > currentBlock.BlockViewElevation &&
            location.IsCellVisible(currentBlock.Position + QuadMetrics.GetFirstSolidCorner(direction.Opposite()),
            target.Position + QuadMetrics.GetFirstSolidCorner(direction.Opposite()) + Vector3.up * (targetViewElevation - target.BlockViewElevation), true))
        {
            return true;
        }

        return false;
    }

    public bool GetCellAngleVision(QuadCell cell, QuadCell currentTarget, Vector2 position, float targetViewElevation, float height)
    {
        QuadDirection direction = cell.coordinates.GetRelativeDirection(currentTarget.coordinates);
        QuadDirection direction1, direction2, activeDirection;

        float cellViewElevation = cell.Position.y + height + (cell.Slope ? 1f : 0f);
        float angle1, angle2;
        float angleTan1, angleTan2;
        float xSign, zSign;
        Vector3 position1, position2;
        Vector3 corner1, corner2;
        QuadCell current;

        if (direction == QuadDirection.SouthEast || direction == QuadDirection.NorthWest)
        {
            direction1 = direction.Next();
            direction2 = direction.Previous();
            if(direction == QuadDirection.SouthEast)
            {
                xSign = 1f;
                zSign = -1f;
            }
            else
            {
                xSign = -1f;
                zSign = 1f;
            }
            corner1 = QuadMetrics.GetSecondCorner(direction1);
            corner2 = QuadMetrics.GetFirstCorner(direction2.Previous());
        }
        else
        {
            direction1 = direction.Previous();
            direction2 = direction.Next();
            if (direction == QuadDirection.NorthEast)
            {
                xSign = 1f;
                zSign = 1f;
            }
            else
            {
                xSign = -1f;
                zSign = -1f;
            }

            corner1 = QuadMetrics.GetFirstCorner(direction1.Previous());
            corner2 = QuadMetrics.GetSecondCorner(direction2);
        }

        angleTan1 = Mathf.Abs(position.x - cell.Position.x) / Mathf.Abs(position.y - cell.Position.z);
        angleTan2 = 1f / angleTan1;
        angle1 = Mathf.Atan(angleTan1) * Mathf.Rad2Deg;
        angle2 = 90f - angle1;

        if (angle1 < angle2)
        {
            position1 = cell.Position + QuadMetrics.GetFirstCorner(direction1);
            position1.x += xSign * QuadMetrics.radius * angleTan1;
            position1.y = 0f;
            current = cell.GetNeighbor(direction1);
            activeDirection = direction1;
        }
        else
        {
            position1 = cell.Position + QuadMetrics.GetFirstCorner(direction2);
            position1.z += zSign * QuadMetrics.radius * angleTan2;
            position1.y = 0f;
            current = cell.GetNeighbor(direction2);
            activeDirection = direction2;
        }

        if (activeDirection == direction1)
        {
            position2 = current.Position + corner2;

            if (angle2 < Mathf.Atan2(2f * QuadMetrics.radius, Mathf.Abs(position1.x - position2.x)) * Mathf.Rad2Deg)
            {
                activeDirection = direction2;
                position2.z += zSign * angleTan2 * Mathf.Abs(position1.x - position2.x);
                position2.y = 0f;
            }
            else
            {
                position2 = position1;
                position2.x += xSign * 2f * QuadMetrics.radius / angleTan2;
                position2.z += zSign * 2f * QuadMetrics.radius;
            }
        }
        else
        {
            position2 = current.Position + corner1;

            if (angle1 < Mathf.Atan2(2f * QuadMetrics.radius, Mathf.Abs(position1.z - position2.z)) * Mathf.Rad2Deg)
            {
                activeDirection = direction1;
                position2.x += xSign * angleTan1 * Mathf.Abs(position1.z - position2.z);
                position2.y = 0f;
            }
            else
            {
                position2 = position1;
                position2.z += zSign * 2f * QuadMetrics.radius / angleTan1;
                position2.x += xSign * 2f * QuadMetrics.radius;
            }
        }

        Vector3 blockPosition1 = position1, blockPosition2 = position2;
        position1 = position2;
        bool isBlocked = false;
        float blockViewElevation = cell.BlockViewElevation;

        if (!current.isBlockingViewIgnored)
        {
            isBlocked = true;
            blockViewElevation = current.BlockViewElevation;
        }

        current = current.GetNeighbor(activeDirection);

        while (current != currentTarget)
        {
            if (activeDirection == direction1)
            {
                position2 = current.Position + corner2;

                if (angle2 < Mathf.Atan2(2f * QuadMetrics.radius, Mathf.Abs(position1.x - position2.x)) * Mathf.Rad2Deg)
                {
                    activeDirection = direction2;
                    position2.z += zSign * angleTan2 * Mathf.Abs(position1.x - position2.x);
                    position2.y = 0f;
                }
                else
                {
                    position2 = position1;
                    position2.x += xSign * 2f * QuadMetrics.radius / angleTan2;
                    position2.z += zSign * 2f * QuadMetrics.radius;
                }
            }
            else
            {
                position2 = current.Position + corner1;

                if (angle1 < Mathf.Atan2(2f * QuadMetrics.radius, Mathf.Abs(position1.z - position2.z)) * Mathf.Rad2Deg)
                {
                    activeDirection = direction1;
                    position2.x += xSign * angleTan1 * Mathf.Abs(position1.z - position2.z);
                    position2.y = 0f;
                }
                else
                {
                    position2 = position1;
                    position2.z += zSign * 2f * QuadMetrics.radius / angleTan1;
                    position2.x += xSign * 2f * QuadMetrics.radius;
                }
            }

            if (current.IsVisible && !current.isBlockingViewIgnored)
            {
                isBlocked = true;

                if (blockViewElevation > cellViewElevation)
                {
                    if (blockViewElevation < current.BlockViewElevation &&
                        cell.IsCellVisible(
                            new Vector3(blockPosition1.x, blockViewElevation, blockPosition1.z),
                            new Vector3(position1.x, current.BlockViewElevation, position1.z), true))
                    {
                        blockPosition1 = position1;
                        blockPosition2 = position2;
                        blockViewElevation = current.BlockViewElevation;
                    }
                }
                else
                {
                    if (blockViewElevation <= current.BlockViewElevation)
                    {
                        blockPosition1 = position1;
                        blockPosition2 = position2;
                        blockViewElevation = current.BlockViewElevation;                    
                    }
                    else
                    {
                        if (cell.IsCellVisible(
                            new Vector3(blockPosition2.x, blockViewElevation, blockPosition2.z),
                            new Vector3(position2.x, current.BlockViewElevation, position2.z)))
                        {
                            blockPosition1 = position1;
                            blockPosition2 = position2;
                            blockViewElevation = current.BlockViewElevation;
                        }
                    }
                }
            }

            position1 = position2;
            current = current.GetNeighbor(activeDirection);
        }

        if(!isBlocked)
            return false;

        if (activeDirection == direction1)
        {
            position2 = current.Position + corner2;

            if (angle2 < Mathf.Atan2(2f * QuadMetrics.radius, Mathf.Abs(position1.x - position2.x)) * Mathf.Rad2Deg)
            {
                position2.z += zSign * angleTan2 * Mathf.Abs(position1.x - position2.x);
                position2.y = 0f;
            }
            else
            {
                position2 = position1;
                position2.x += xSign * 2f * QuadMetrics.radius / angleTan2;
                position2.z += zSign * 2f * QuadMetrics.radius;
            }
        }
        else
        {
            position2 = current.Position + corner1;

            if (angle1 < Mathf.Atan2(2f * QuadMetrics.radius, Mathf.Abs(position1.z - position2.z)) * Mathf.Rad2Deg)
            {
                position2.x += xSign * angleTan1 * Mathf.Abs(position1.z - position2.z);
                position2.y = 0f;
            }
            else
            {
                position2 = position1;
                position2.z += zSign * 2f * QuadMetrics.radius / angleTan1;
                position2.x += xSign * 2f * QuadMetrics.radius;
            }
        }

        if (blockViewElevation > cellViewElevation)
        {
            return targetViewElevation > blockViewElevation && cell.IsCellVisible(
                new Vector3(blockPosition1.x, blockViewElevation, blockPosition1.z),
                new Vector3(position1.x, targetViewElevation, position1.z), true);
        }
        else
        {
            return targetViewElevation >= blockViewElevation || cell.IsCellVisible(
                new Vector3(blockPosition2.x, blockViewElevation, blockPosition2.z),
                new Vector3(position2.x, targetViewElevation, position2.z));
        }
    }

    public void ClearVision()
    {
        if (currentVisionExists)
        {
            int minX = previousVisionFromCoordinates.X - previousMaxVision < 0 ? 0 : previousVisionFromCoordinates.X - previousMaxVision;
            int maxX = previousVisionFromCoordinates.X + previousMaxVision >= cellCountX ? cellCountX - 1 : previousVisionFromCoordinates.X + previousMaxVision;
            for (int z = previousVisionFromCoordinates.Z - previousMaxVision < 0 ? 0 : previousVisionFromCoordinates.Z - previousMaxVision;
                z <= (previousVisionFromCoordinates.Z + previousMaxVision >= cellCountZ ? cellCountZ - 1 : previousVisionFromCoordinates.Z + previousMaxVision); z++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    cells[x + z * cellCountX].IsVisible = false;
                }
            }
        }
        currentVisionExists = false;
    }

    public void ResetVisibility()
    {
        for (int i = 0; i < cells.Length; i++)
        {
            cells[i].ResetVisibility();
        }

        if (Player.Instance != null)
        {
            SetVisibleCells(Player.Instance.Location, Player.Instance.VisionRange);
        }
    }

    public bool IsCellAffectedByBlastLine(Vector3 origin, QuadCell originCell, QuadCell targetCell, QuadDirection direction)
    {
        origin.y += 1.5f * QuadMetrics.elevationStep;
        origin.x += System.Math.Sign(targetCell.coordinates.X - originCell.coordinates.X) * QuadMetrics.blendFactor * QuadMetrics.radius * 1.5f;
        origin.z += System.Math.Sign(targetCell.coordinates.Z - originCell.coordinates.Z) * QuadMetrics.blendFactor * QuadMetrics.radius * 1.5f;

        QuadCell current = GetCell(origin);

        while(current != targetCell)
        {
            if (origin.y < current.Position.y)
                return false;

            current = current.GetNeighbor(direction);
        }

        return origin.y >= targetCell.Position.y;
    }

    public bool IsCellAffectedByBlastDiagonal(Vector3 origin, Vector3 target, QuadCell targetCell, QuadDirection direction)
    {
        QuadDirection direction1, direction2, activeDirection;

        float originElevation;
        float angle1, angle2;
        float angleTan1, angleTan2;
        float xSign, zSign;
        float elevationCoefficient;   
        Vector3 position;
        Vector3 corner1, corner2;
        QuadCell current = GetCell(origin);

        originElevation = current.Elevation;

        if (originElevation == targetCell.Elevation)
        {
            elevationCoefficient = 0f;
        }
        else
        {
            elevationCoefficient = Vector3.Distance(target, new Vector3(target.x, origin.y, target.z))
                / Vector3.Distance(origin, new Vector3(target.x, origin.y, target.z));

            elevationCoefficient *= originElevation < targetCell.Elevation ? 1f : -1f;
        }

        if (direction == QuadDirection.SouthEast || direction == QuadDirection.NorthWest)
        {
            direction1 = direction.Next();
            direction2 = direction.Previous();
            if (direction == QuadDirection.SouthEast)
            {
                xSign = 1f;
                zSign = -1f;
            }
            else
            {
                xSign = -1f;
                zSign = 1f;
            }

            corner1 = QuadMetrics.GetSecondCorner(direction1);
            corner2 = QuadMetrics.GetFirstCorner(direction2.Previous());
        }
        else
        {
            direction1 = direction.Previous();
            direction2 = direction.Next();
            if (direction == QuadDirection.NorthEast)
            {
                xSign = 1f;
                zSign = 1f;
            }
            else
            {
                xSign = -1f;
                zSign = -1f;
            }

            corner1 = QuadMetrics.GetFirstCorner(direction1.Previous());
            corner2 = QuadMetrics.GetSecondCorner(direction2);
        }

        angleTan1 = Mathf.Abs(origin.x - target.x) / Mathf.Abs(origin.z - target.z);
        angleTan2 = 1f / angleTan1;
        angle1 = Mathf.Atan(angleTan1) * Mathf.Rad2Deg;
        angle2 = 90f - angle1;
        position = current.Position + QuadMetrics.GetFirstCorner(direction);

        if (angle1 < Mathf.Atan(Mathf.Abs(origin.x - position.x) / Mathf.Abs(origin.z - position.z)) * Mathf.Rad2Deg)
        {
            position = new Vector3(origin.x, origin.y, zSign * QuadMetrics.radius + current.Position.z);
            position.x += xSign * Vector3.Distance(position, origin) * angleTan1;

            if(Mathf.Abs(position.z - origin.z) > 0.25f * QuadMetrics.radius && Mathf.Abs(position.x - origin.x) > 0.25f * QuadMetrics.radius &&
                originElevation + elevationCoefficient * Vector3.Distance(origin, position) < current.Elevation)
            {
                return false;
            }

            current = current.GetNeighbor(direction1);
            activeDirection = direction1;
        }
        else
        {
            position = new Vector3(xSign * QuadMetrics.radius + current.Position.x, origin.y, origin.z);
            position.z += zSign * Vector3.Distance(position, origin) * angleTan2;

            if (Mathf.Abs(position.z - origin.z) > 0.25f * QuadMetrics.radius && Mathf.Abs(position.x - origin.x) > 0.25f * QuadMetrics.radius &&
                originElevation + elevationCoefficient * Vector3.Distance(origin, position) < current.Elevation)
            {
                return false;
            }

            current = current.GetNeighbor(direction2);
            activeDirection = direction2;
        }

        while (current != targetCell)
        {
            if (originElevation + elevationCoefficient * Vector3.Distance(origin, position) < current.Elevation)
            {
                return false;
            }

            if (activeDirection == direction1)
            {
                float previousX = position.x;
                float previousZ = position.z;
                position = current.Position + corner2;

                if (angle2 < Mathf.Atan2(2f * QuadMetrics.radius, Mathf.Abs(previousX - position.x)) * Mathf.Rad2Deg)
                {
                    activeDirection = direction2;
                    position.z += zSign * angleTan2 * Mathf.Abs(previousX - position.x);
                }
                else
                {
                    position.x = previousX + xSign * 2f * QuadMetrics.radius / angleTan2;
                    position.z = previousZ + zSign * 2f * QuadMetrics.radius;
                }

                position.y = origin.y;

            }
            else
            {
                float previousX = position.x;
                float previousZ = position.z;
                position = current.Position + corner1;

                if (angle1 < Mathf.Atan2(2f * QuadMetrics.radius, Mathf.Abs(previousZ - position.z)) * Mathf.Rad2Deg)
                {
                    activeDirection = direction1;
                    position.x += xSign * angleTan1 * Mathf.Abs(previousZ - position.z);

                }
                else
                {
                    position.z = previousZ + zSign * 2f * QuadMetrics.radius / angleTan1;
                    position.x = previousX + xSign * 2f * QuadMetrics.radius;
                }

                position.y = origin.y;
            }

            if (originElevation + elevationCoefficient * Vector3.Distance(origin, position) < current.Elevation)
            {
                return false;
            }

            current = current.GetNeighbor(activeDirection);
        }

        return originElevation + elevationCoefficient * Vector3.Distance(origin, position) >= current.Elevation;
    }

    public void ShowUI(bool visible)
    {
        for (int i = 0; i < chunks.Length; i++)
        {
            chunks[i].ShowUI(visible);
        }
    }

    public void EnableCoverHighlight(QuadCell cell)
    {
        if (cell.uiRect.childCount == 1)
        {
            for(int i = 0; i < 4; i++)
            {
                if(cell.HasCoverInDirection(i * 2))
                {
                    RectTransform cover = Instantiate(coverPrefab);
                    cover.SetParent(cell.uiRect, false);
                    if (cell.IsSmallCover(i * 2))
                    {
                        cover.GetComponent<Image>().sprite = coverHalfSprite;
                    }
                    else
                    {
                        cover.GetComponent<Image>().sprite = coverFullSprite;
                    }

                    if(i % 2 == 0)
                    {
                        cover.localEulerAngles = new Vector3(0f, -90f, 90f);
                        cover.localPosition = new Vector3(0.52f * (i == 0 ? 1f : -1f), 0f, -1f);
                    }
                    else
                    {
                        cover.localEulerAngles = new Vector3(-90f, -90f, 90f);
                        cover.localPosition = new Vector3(0f, 0.52f * (i == 1 ? -1f : 1f), -1f);
                    }
                }
            }
        }

        cell.EnableCoverHighlight(Color.cyan);
    }

    public void AddUnit(float orientation, Entity unit, QuadCell location , int strengthLevelSpawn = 0)
    {
        if(unit.index == 0)
        {
            if (Player.Instance != null)
            {
                units.Remove(Player.Instance);
                Player.Instance.Die();
                ClearHighlights();
            }
            Instantiate(unit);
            units.Add(Player.Instance);
            Player.Instance.transform.SetParent(transform, false);
            Player.Instance.Location = location;
            Player.Instance.Orientation = orientation;
        }
        else
        {
            Entity instance = Instantiate(unit);
            units.Add(instance);
            instance.transform.SetParent(transform, false);
            instance.Location = location;
            instance.Orientation = orientation;
            instance.strengthLevelSpawn = strengthLevelSpawn;
        }
    }

    public void OrderUnits()
    {
        //temp make order by distance;
        units.OrderBy(i => i.index);
    }

    public void RemoveProjectile(AbilityProjectile abilityProjectile)
    {
        abilityProjectiles.Remove(abilityProjectile);
        Destroy(abilityProjectile.gameObject);
    }

    public void RemoveUnit(Entity unit)
    {
        units.Remove(unit);
        unit.Die();
        units.OrderBy(i => i.index);
    }

    private void ClearProjectiles()
    {
        for(int i = 0; i < abilityProjectiles.Count; i++)
        {
            Destroy(abilityProjectiles[i].gameObject);
        }

        abilityProjectiles.Clear();
    }

    private void ClearUnits()
    {
        for (int i = 0; i < units.Count; i++)
        {
            units[i].Die();
        }
        units.Clear();
    }

    private void ClearVariables()
    {
        currentDistanceExists = false;
        currentVisionExists = false;
        searchFrontierPhase = 0;
        previousMaxDistance = 0;
        previousMaxVision = 0;
        previousSearchFromCoordinates = new QuadCoordinates(0, 0);
        previousVisionFromCoordinates = new QuadCoordinates(0, 0);
    }

    public QuadCell GetCell(Ray ray)
    {
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            return GetCell(transform.InverseTransformPoint(hit.point));
        }
        return null;
    }

    public void Save(BinaryWriter writer)
    {
        writer.Write(cellCountX);
        writer.Write(cellCountZ);       

        for (int i = 0; i < cells.Length; i++)
        {
            cells[i].Save(writer);
        }

        writer.Write((byte)specialZones.Count);

        for (int i = 0; i < specialZones.Count; i++)
        {
            specialZones[i].Save(writer);
        }

        writer.Write((byte)camps.Count);

        for (int i = 0; i < camps.Count; i++)
        {
            camps[i].Save(writer);
        }

        writer.Write((byte)units.Count);

        for (int i = 0; i < units.Count; i++)
        {
            units[i].Save(writer);
            units[i].SpecificsSave(writer);
        }

        ObjectiveManager.Instance.Save(writer);
    }

    public void Load(BinaryReader reader, int header)
    {
        if (!CreateMap(reader.ReadInt32(), reader.ReadInt32()))
            return;      
        
        for (int i = 0; i < cells.Length; i++)
        {
            cells[i].Load(reader, header);
        }

        for (int i = 0; i < chunks.Length; i++)
        {
            chunks[i].Refresh();
        }
        
        int specialZonesCount = reader.ReadByte();

        for (int i = 0; i < specialZonesCount; i++)
        {
            specialZones.Add(SpecialZone.Load(reader, header));

            if (PlayerInfo.HasScoutedArea && specialZones[i].zoneType == SpecialZoneType.ScoutedArea)
            {
                foreach (var cell in specialZones[i].GetSpecialZoneCells())
                {
                    cell.IsVisible = true;
                }
            }
        }
        
        int campsCount = reader.ReadByte();

        for (int i = 0; i < campsCount; i++)
        {
            EnemyCamp.Load(reader, header);
        }

        int unitCount = reader.ReadByte();

        for (int i = 0, j = 0; i < unitCount; i++)
        {
            if(Entity.Load(reader, header))
            {
                units[j].SpecificsLoad(reader, header);
                j++;
            }
            else
            {
                Enemy.SpecificsLoadEmpty(reader, header);
            }
        }

        ObjectiveManager.Instance.Load(reader, header);

        for (int i = 0; i < cells.Length; i++)
        {
            cells[i].SetTargetViewElevation();
        }

        cellShaderData.ViewElevationChanged();

        if (Player.Instance)
        {            
            FindPlayerDistance();
        }
        else if(specialZones.Any(zone => zone.zoneType == SpecialZoneType.PlayerSpawn))
        {
            AddUnit(Random.Range(0f, 360f), unitPrefabs[0], specialZones[specialZones.FindIndex(zone => zone.zoneType == SpecialZoneType.PlayerSpawn)].GetRandomCell());
            FindPlayerDistance();
        }

        OrderUnits();
    }
}
