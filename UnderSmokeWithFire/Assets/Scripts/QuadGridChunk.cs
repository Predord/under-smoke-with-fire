using System;
using UnityEngine;

public class QuadGridChunk : MonoBehaviour
{
    public QuadMesh terrain;
    public QuadMesh water;
    public QuadFeatureManager features;

    private QuadCell[] cells;
    private Canvas gridCanvas;

    private static Color weights1 = new Color(1f, 0f, 0f, 0f);
    private static Color weights2 = new Color(0f, 1f, 0f, 0f);
    private static Color weights3 = new Color(0f, 0f, 1f, 0f);
    private static Color weights4 = new Color(0f, 0f, 0f, 1f);
    private static Color weights5 = new Color(0.5f, 0.5f, 0f, 0f);

    private void Awake()
    {
        gridCanvas = GetComponentInChildren<Canvas>();
        cells = new QuadCell[QuadMetrics.chunkSizeX * QuadMetrics.chunkSizeZ];
    }

    private void LateUpdate()
    {
        Triangulate();
        enabled = false;
    }

    public void AddCell(int index, QuadCell cell)
    {
        cells[index] = cell;
        cell.chunk = this;
        cell.transform.SetParent(transform, false);
        cell.uiRect.SetParent(gridCanvas.transform, false);
    }

    public void ShowUI(bool visible)
    {
        gridCanvas.gameObject.SetActive(visible);
    }

    public void Refresh()
    {
        enabled = true;
    }

    public void Triangulate()
    {
        terrain.Clear();
        water.Clear();
        features.Clear();
        for (int i = 0; i < cells.Length; i++)
        {
            Triangulate(cells[i]);
        }
        terrain.Apply();
        water.Apply();
    }

    private void Triangulate(QuadCell cell)
    {
        for (QuadDirection direction = QuadDirection.North; direction <= QuadDirection.NorthWest; direction++)
        {
            Triangulate(direction, cell);
        }
        if (!cell.IsUnderwater)
        {
            if (cell.SpecialIndex > 0)
            {
                features.AddSpecial(cell.SpecialIndex, cell.Position, cell.SpecialFeatureDirection);
            }
            else
            {
                if (cell.Slope)
                {
                    Vector2 rotation = QuadMetrics.GetSlopeAngle(cell.SlopeDirection);
                    features.AddFeature(cell, QuadMetrics.Perturb(cell.Position) + Vector3.up * (QuadMetrics.elevationStep / 2f), rotation);
                }
                else
                {
                    features.AddFeature(cell, cell.Position);
                }
            }
        }
    }

    private void Triangulate(QuadDirection direction, QuadCell cell)
    {
        if (((int)direction & 1) != 0)
            return;

        Vector3 center = cell.Position;
        EdgeVertices edge = new EdgeVertices(
            center + QuadMetrics.GetFirstSolidCorner(direction.Previous()),
            center + QuadMetrics.GetSecondSolidCorner(direction)
        );

        if (cell.Slope)
        {
            TriangulateSlope(direction, cell, ref edge, center);
            if (!cell.IsUnderwater)
            {
                Vector2 rotation = QuadMetrics.GetSlopeAngle(cell.SlopeDirection);
                center.y += QuadMetrics.elevationStep / 2f;

                features.AddFeature(cell, (QuadMetrics.Perturb(center) + QuadMetrics.Perturb(edge.vertix3)) * 0.5f, rotation);
                features.AddFeature(cell, (QuadMetrics.Perturb(center) + QuadMetrics.Perturb(edge.vertix5)) * 0.5f, rotation);
                return;
            }
        }
        else
        {
            TriangulateEdgeFan(center, edge, cell.Index);
            TriangulateConnection(direction, cell, edge);
            if (!cell.IsUnderwater && !cell.IsSpecial)
            {
                features.AddFeature(cell, (center + edge.vertix3) * 0.5f);
                features.AddFeature(cell, (center + edge.vertix5) * 0.5f);
                return;
            }
        }

        if (cell.IsUnderwater)
        {
            TriangulateWater(direction, cell, center);
        }
    }

    private void TriangulateWater(QuadDirection direction, QuadCell cell, Vector3 center)
    {
        center.y = cell.WaterSurfaceY;

        QuadCell neighbor = cell.GetNeighbor(direction);
        if(neighbor != null && !neighbor.IsUnderwater && !(cell.Slope && cell.WaterLevel - cell.Elevation == 1 && cell.SlopeDirection == direction))
        {
            TriangulateWaterShore(direction, cell, neighbor, center);
        }
        else
        {
            TriangulateOpenWater(direction, cell, neighbor, center);
        }
    }

    private void TriangulateOpenWater(QuadDirection direction, QuadCell cell, QuadCell neighbor, Vector3 center)
    {
        Vector3 vertix1 = center + QuadMetrics.GetFirstSolidCorner(direction.Previous());
        Vector3 vertix2 = center + QuadMetrics.GetSecondSolidCorner(direction);

        water.AddTriangle(center, vertix1, vertix2);
        Vector4 indices;
        indices.x = indices.y = indices.z = indices.w = cell.Index;
        water.AddTriangleCellData(indices, weights1);

        if (neighbor == null || (cell.Slope && cell.WaterLevel - cell.Elevation == 1 && cell.SlopeDirection == direction))
            return;

        Vector3 bridge = QuadMetrics.GetBridge(direction);
        Vector3 vertix3 = vertix1 + bridge;
        Vector3 vertix4 = vertix2 + bridge;

        water.AddQuad(vertix1, vertix2, vertix3, vertix4);
        indices.y = neighbor.Index;
        water.AddQuadCellData(indices, weights1, weights5);

        QuadCell secondNeighbor = cell.GetNeighbor(direction.Next());
        if (secondNeighbor == null || (cell.Slope && cell.WaterLevel - cell.Elevation == 1 && cell.SlopeDirection == direction.Next2()))
            return;

        bridge = QuadMetrics.GetBridge(direction.Next2());
        water.AddQuad(vertix4, vertix2, vertix4 + bridge, vertix2 + bridge);
        indices.z = secondNeighbor.Index;
        indices.w = cell.GetNeighbor(direction.Next2()).Index;
        water.AddQuadCellData(indices, weights5, weights1, new Color(0.25f, 0.25f, 0.25f, 0.25f), new Color(0.5f, 0f, 0f, 0.5f));
    }

    private void TriangulateWaterShore(QuadDirection direction, QuadCell cell, QuadCell neighbor, Vector3 center)
    {
        EdgeVertices edge1 = new EdgeVertices(
            center + QuadMetrics.GetFirstSolidCorner(direction.Previous()),
            center + QuadMetrics.GetSecondSolidCorner(direction)
        );
        water.AddTriangle(center, edge1.vertix1, edge1.vertix2);
        water.AddTriangle(center, edge1.vertix2, edge1.vertix3);
        water.AddTriangle(center, edge1.vertix3, edge1.vertix4);
        water.AddTriangle(center, edge1.vertix4, edge1.vertix5);
        Vector4 indices;
        indices.x = indices.y = indices.z = indices.w = cell.Index;
        water.AddTriangleCellData(indices, weights1);
        water.AddTriangleCellData(indices, weights1);
        water.AddTriangleCellData(indices, weights1);
        water.AddTriangleCellData(indices, weights1);

        Vector3 bridge = QuadMetrics.GetBridge(direction);
        EdgeVertices edge2 = new EdgeVertices(
            edge1.vertix1 + bridge,
            edge1.vertix5 + bridge
        );
        water.AddQuad(edge1.vertix1, edge1.vertix2, edge2.vertix1, edge2.vertix2);
        water.AddQuad(edge1.vertix2, edge1.vertix3, edge2.vertix2, edge2.vertix3);
        water.AddQuad(edge1.vertix3, edge1.vertix4, edge2.vertix3, edge2.vertix4);
        water.AddQuad(edge1.vertix4, edge1.vertix5, edge2.vertix4, edge2.vertix5);
        indices.y = neighbor.Index;
        water.AddQuadCellData(indices, weights1, weights5);
        water.AddQuadCellData(indices, weights1, weights5);
        water.AddQuadCellData(indices, weights1, weights5);
        water.AddQuadCellData(indices, weights1, weights5);

        QuadCell secondNeighbor = cell.GetNeighbor(direction.Next());
        if (secondNeighbor == null || (cell.Slope && cell.WaterLevel - cell.Elevation == 1 && cell.SlopeDirection == direction.Next2()))
            return;

        bridge = QuadMetrics.GetBridge(direction.Next2());
        water.AddQuad(
            edge2.vertix5,
            edge1.vertix5,
            edge2.vertix5 + bridge,
            edge1.vertix5 + bridge
        );
        indices.z = secondNeighbor.Index;
        indices.w = cell.GetNeighbor(direction.Next2()).Index;
        water.AddQuadCellData(indices, weights5, weights1, new Color(0.25f, 0.25f, 0.25f, 0.25f), new Color(0.5f, 0f, 0f, 0.5f));
    }

    private void TriangulateSlope(QuadDirection direction, QuadCell cell, ref EdgeVertices edge, Vector3 center)
    {
        center.y += QuadMetrics.elevationStep / 2f;
        QuadDirection slopeDirection = cell.SlopeDirection;

        cell.LadderDirections = 0;
        cell.SpecialIndex = 0;

        if (direction == slopeDirection)
        {
            edge.AddPosY(QuadMetrics.elevationStep * (1 - QuadMetrics.leanElevationFactor));

            TriangulateEdgeFan(center, edge, cell.Index);
            TriangulateConnection(direction, cell, edge);
            return;
        }
        if (direction == slopeDirection.Opposite())
        {
            edge.AddPosY(QuadMetrics.leanElevationFactor * QuadMetrics.elevationStep);

            TriangulateEdgeFan(center, edge, cell.Index);
            TriangulateConnection(direction, cell, edge);
            return;
        }
        if (direction == slopeDirection.Next2())
        {
            edge.SetSideSlopeCoordinates(
                cell.Position.y + (1 - QuadMetrics.leanElevationFactor) * QuadMetrics.elevationStep,
                cell.Position.y + QuadMetrics.leanElevationFactor * QuadMetrics.elevationStep
            );
        }
        else
        {
            edge.SetSideSlopeCoordinates(
                cell.Position.y + QuadMetrics.leanElevationFactor * QuadMetrics.elevationStep,
                cell.Position.y + (1 - QuadMetrics.leanElevationFactor) * QuadMetrics.elevationStep
            );
        }

        TriangulateEdgeFan(center, edge, cell.Index);
        TriangulateConnection(direction, cell, edge);
        return;
    }

    private void TriangulateConnection(QuadDirection direction, QuadCell cell, EdgeVertices edge1)
    {
        QuadCell neighbor = cell.GetNeighbor(direction);
        if (neighbor == null)
        {
            return;
        }

        Vector3 bridge = QuadMetrics.GetBridge(direction);
        EdgeVertices edge2 = new EdgeVertices(edge1.vertix1 + bridge, edge1.vertix5 + bridge);

        QuadType connectionType = cell.GetEdgeType(direction);
        int elevationDifference = cell.GetCellNeighborConnectionElevationDifference(connectionType, direction);

        if (connectionType == QuadType.Flat)
        {
            edge2.AddPosY((neighbor.Position.y - cell.Position.y) * 0.5f);

            TriangulateEdgeStrip(
                edge1, weights1, cell.Index,
                edge2, weights5, neighbor.Index
            );
            TriangulateCorner(direction, connectionType, cell, neighbor, elevationDifference, edge1.vertix5, edge2.vertix5);
            return;
        }

        if (connectionType == QuadType.Cliff)
        {
            edge2.AddPosY(elevationDifference * QuadMetrics.elevationStep * QuadMetrics.leanElevationFactor);

            TriangulateEdgeStrip(
                edge1, weights1, cell.Index,
                edge2, weights1, cell.Index
            );
            if (direction == QuadDirection.North || direction == QuadDirection.East)
            {
                TriangulateConnectionCliff(cell, neighbor, edge2, elevationDifference);
            }
            if(elevationDifference == 1 && neighbor.Elevation - cell.Elevation > 2  && QuadMetrics.CheckLadderDirection(direction, cell.LadderDirections))
            {
                cell.SpecialIndex = 0;
                if(direction == QuadDirection.North)
                {
                    features.AddLadder(-90f, neighbor.Elevation - cell.Elevation, edge1.vertix3);
                }
                else if (direction == QuadDirection.East)
                {
                    features.AddLadder(0f, neighbor.Elevation - cell.Elevation, edge1.vertix3);
                }
                else if (direction == QuadDirection.South)
                {
                    features.AddLadder(90f, neighbor.Elevation - cell.Elevation, edge1.vertix3);
                }
                else
                {
                    features.AddLadder(180f, neighbor.Elevation - cell.Elevation, edge1.vertix3);
                }                   
            }
            TriangulateCorner(direction, connectionType, cell, neighbor, elevationDifference, edge1.vertix5, edge2.vertix5);
            return;
        }

        if (connectionType == QuadType.SlopeFlat)
        {
            edge2.AddPosY((neighbor.Position.y - cell.Position.y) * 0.5f);
            TriangulateEdgeStrip(
                edge1, weights1, cell.Index,
                edge2, weights5, neighbor.Index
            );
            TriangulateCorner(direction, connectionType, cell, neighbor, elevationDifference, edge1.vertix5, edge2.vertix5);
            return;
        }

        if (connectionType == QuadType.SlopeCliff)
        {
            if (cell.Slope)
            {
                TriangulateConnectionSlopeCliff(cell, neighbor, edge1, edge2, direction, elevationDifference);
            }
            else
            {
                if(QuadMetrics.CheckLadderDirection(direction, cell.LadderDirections))
                {
                    cell.LadderDirections -= (int)Math.Pow(2.0, ((int)direction) / 2);
                }
                TriangulateConnectionCliffSlope(cell, neighbor, edge1, edge2, direction, elevationDifference);
            }

            elevationDifference = cell.GetCellNeighborSlopeCornerElevationDifference(direction, false);

            if (cell.Slope && (direction == cell.SlopeDirection || direction.Opposite() == cell.SlopeDirection || elevationDifference == 0))
            {
                int additionalSlopeElevation = cell.SlopeDirection == direction || cell.SlopeDirection == direction.Next2() ? 1 : 0;
                edge2.vertix5.y = cell.Position.y + additionalSlopeElevation * QuadMetrics.elevationStep;
                if (elevationDifference == 0)
                    edge2.vertix5.y += (neighbor.Position.y - cell.Position.y - additionalSlopeElevation * QuadMetrics.elevationStep) * 0.5f;
            }
            else
            {
                edge2.vertix5.y += elevationDifference * QuadMetrics.leanElevationFactor * QuadMetrics.elevationStep;
                if (elevationDifference == 0)
                    edge2.vertix5.y += (neighbor.Position.y + (cell.Elevation - neighbor.Elevation) * QuadMetrics.elevationStep - cell.Position.y) * 0.5f;
            }
            TriangulateCorner(direction, connectionType, cell, neighbor, elevationDifference, edge1.vertix5, edge2.vertix5);
            return;
        }

        if (connectionType == QuadType.SlopeSlope)
        {
            TriangulateConnectionSlopeSlope(cell, neighbor, edge1, edge2, direction, elevationDifference);
            elevationDifference = cell.GetCellNeighborSlopeCornerElevationDifference(direction, false);

            if (direction == cell.SlopeDirection || direction.Opposite() == cell.SlopeDirection || elevationDifference == 0)
            {
                int additionalSlopeElevation = cell.SlopeDirection == direction || cell.SlopeDirection == direction.Next2() ? 1 : 0;
                edge2.vertix5.y = cell.Position.y + additionalSlopeElevation * QuadMetrics.elevationStep;
                if (elevationDifference == 0)
                    edge2.vertix5.y += (neighbor.Position.y + (cell.Elevation - neighbor.Elevation) * QuadMetrics.elevationStep - cell.Position.y) * 0.5f;
            }
            else
            {
                edge2.vertix5.y += elevationDifference * QuadMetrics.leanElevationFactor * QuadMetrics.elevationStep;
                if (elevationDifference == 0)
                    edge2.vertix5.y += (neighbor.Position.y + (cell.Elevation - neighbor.Elevation) * QuadMetrics.elevationStep - cell.Position.y) * 0.5f;
            }
            TriangulateCorner(direction, connectionType, cell, neighbor, elevationDifference, edge1.vertix5, edge2.vertix5);
            return;
        }

        if (connectionType == QuadType.SlopeIntersection)
        {
            TriangulateConnectionSlopeIntersection(cell, neighbor, edge1, edge2, direction, elevationDifference);
            edge2.vertix5.y = edge1.vertix5.y + elevationDifference * QuadMetrics.leanElevationFactor * QuadMetrics.elevationStep;
            TriangulateCorner(direction, connectionType, cell, neighbor, elevationDifference, edge1.vertix5, edge2.vertix5);
            return;
        }
    }

    private void TriangulateConnectionCliff(
        QuadCell cell, QuadCell neighbor,
        EdgeVertices edge1, int elevationDifference
    )
    {
        EdgeVertices edge2 = edge1;
        if (elevationDifference == 1)
        {
            if ((cell.Elevation + 1) != neighbor.Elevation)
            {
                edge2.SetPosY(cell.Position.y + QuadMetrics.straightElevationFactor * QuadMetrics.elevationStep);

                TriangulateEdgeStrip(
                    edge1, weights1, cell.Index,
                    edge2, weights2, neighbor.Index
                );
                edge1.SetPosY(neighbor.Position.y - QuadMetrics.leanElevationFactor * QuadMetrics.elevationStep);
                TriangulateEdgeStrip(
                    edge2, weights2, neighbor.Index,
                    edge1, weights2, neighbor.Index
                );
                return;
            }

            edge2.SetPosY(neighbor.Position.y - QuadMetrics.leanElevationFactor * QuadMetrics.elevationStep);
            TriangulateEdgeStrip(
                edge1, weights1, cell.Index,
                edge2, weights2, neighbor.Index
            );
        }
        else
        {
            if ((cell.Elevation - 1) != neighbor.Elevation)
            {
                edge2.SetPosY(neighbor.Position.y + QuadMetrics.straightElevationFactor * QuadMetrics.elevationStep);
                TriangulateEdgeStrip(
                    edge1, weights1, cell.Index,
                    edge2, weights1, cell.Index
                );
                edge1.SetPosY(neighbor.Position.y + QuadMetrics.leanElevationFactor * QuadMetrics.elevationStep);
                TriangulateEdgeStrip(
                    edge2, weights1, cell.Index,
                    edge1, weights2, neighbor.Index
                );
                return;
            }

            edge2.SetPosY(neighbor.Position.y + QuadMetrics.leanElevationFactor * QuadMetrics.elevationStep);
            TriangulateEdgeStrip(
                edge1, weights1, cell.Index,
                edge2, weights2, neighbor.Index
            );
        }
    }

    private void TriangulateConnectionCliffSlope(
        QuadCell cell, QuadCell neighbor,
        EdgeVertices edge1, EdgeVertices edge2,
        QuadDirection direction, int elevationDifference
    )
    {
        bool isOnSide = false;
        if (direction.Next2() == neighbor.SlopeDirection || direction.Previous2() == neighbor.SlopeDirection)
            isOnSide = true;

        if (isOnSide && ((elevationDifference == -1 && cell.Elevation == neighbor.Elevation + 1) || (elevationDifference == 1 && cell.Elevation == neighbor.Elevation)))
        {
            bool exchangeVertices = (direction == neighbor.SlopeDirection.Previous2() && elevationDifference == -1)
                || (direction == neighbor.SlopeDirection.Next2() && elevationDifference == 1);

            edge2.AddPosY(elevationDifference * QuadMetrics.leanElevationFactor * QuadMetrics.elevationStep);
            float additionalElevation = Math.Sign(-elevationDifference + 1) * QuadMetrics.elevationStep;
            if (exchangeVertices)
            {
                edge2.vertix4.y += (neighbor.Position.y + (0.25f - 0.5f * elevationDifference * QuadMetrics.leanElevationFactor) * QuadMetrics.elevationStep
                    + additionalElevation * 0.5f - edge2.vertix4.y) * 0.5f;
                edge2.vertix5.y = (neighbor.Position.y + additionalElevation + cell.Position.y) * 0.5f;

                TriangulateIncompleteRightEdgeStrip(edge1, edge2, weights5, weights5, cell.Index, neighbor.Index);
            }
            else
            {
                edge2.vertix2.y += (neighbor.Position.y + (0.25f - 0.5f * elevationDifference * QuadMetrics.leanElevationFactor) * QuadMetrics.elevationStep
                    + additionalElevation * 0.5f - edge2.vertix4.y) * 0.5f;
                edge2.vertix1.y = (neighbor.Position.y + additionalElevation + cell.Position.y) * 0.5f;

                TriangulateIncompleteLeftEdgeStrip(edge1, edge2, weights5, weights5, cell.Index, neighbor.Index);
            }

            if (direction == QuadDirection.North || direction == QuadDirection.East)
            {
                if (exchangeVertices)
                {                    
                    edge1 = new EdgeVertices(
                        QuadMetrics.SetVertixElevation(edge2.vertix5, neighbor.Position.y + additionalElevation),
                        QuadMetrics.SetVertixElevation(edge2.vertix1, neighbor.Position.y + (additionalElevation == 0 ? QuadMetrics.elevationStep : 0) -
                            2f * elevationDifference * QuadMetrics.leanElevationFactor * QuadMetrics.elevationStep)
                    );
                    edge2.MirrorVerices();
                    edge1.vertix2.y = edge2.vertix2.y;
                    TriangulateEdgeTriangleSlopeCliff(edge1, weights2, cell.Index, edge2, weights1, neighbor.Index);
                }
                else
                {
                    edge1 = new EdgeVertices(
                        QuadMetrics.SetVertixElevation(edge2.vertix1, neighbor.Position.y + additionalElevation),
                        QuadMetrics.SetVertixElevation(edge2.vertix5, neighbor.Position.y + (additionalElevation == 0 ? QuadMetrics.elevationStep : 0) -
                            2f * elevationDifference * QuadMetrics.leanElevationFactor * QuadMetrics.elevationStep)
                    );
                    TriangulateEdgeTriangleSlopeCliff(edge2, weights1, cell.Index, edge1, weights2, neighbor.Index);
                }
            }

            return;
        }
        else
        {
            if (elevationDifference != 0)
            {
                edge2.AddPosY(elevationDifference * QuadMetrics.elevationStep * QuadMetrics.leanElevationFactor);
                TriangulateEdgeStrip(
                    edge1, weights1, cell.Index,
                    edge2, weights1, cell.Index
                );
            }
            else
            {
                edge2.SetPosY(cell.Position.y +
                    (neighbor.Position.y + (neighbor.SlopeDirection != direction ? 1 : 0) * QuadMetrics.elevationStep - cell.Position.y) * 0.5f);
                TriangulateEdgeStrip(
                    edge1, weights1, cell.Index,
                    edge2, weights5, neighbor.Index
                );
                return;
            }
        }

        if (direction == QuadDirection.North || direction == QuadDirection.East)
        {
            TriangulateConnectionWithSlope(
                elevationDifference, cell.Index, neighbor.Index,
                cell.Position.y, neighbor.Position.y,
                direction, neighbor.SlopeDirection, edge2, weights1, weights2, isOnSide
            );
        }
    }

    private void TriangulateConnectionSlopeCliff(
        QuadCell cell, QuadCell neighbor,
        EdgeVertices edge1, EdgeVertices edge2,
        QuadDirection direction, int elevationDifference
    )
    {
        if (cell.SlopeDirection == direction)
        {
            edge2.AddPosY(QuadMetrics.leanElevationFactor * QuadMetrics.elevationStep);

            if (elevationDifference == 0)
            {
                edge2.AddPosY((neighbor.Position.y - cell.Position.y - QuadMetrics.elevationStep) * 0.5f);
                TriangulateEdgeStrip(
                    edge1, weights1, cell.Index,
                    edge2, weights5, neighbor.Index
                );
                return;
            }

            TriangulateEdgeStrip(
                edge1, weights1, cell.Index,
                edge2, weights1, cell.Index
            );
        }
        else if (cell.SlopeDirection == direction.Opposite())
        {
            edge2.AddPosY(-QuadMetrics.leanElevationFactor * QuadMetrics.elevationStep);

            if (elevationDifference == 0)
            {
                edge2.AddPosY((neighbor.Position.y - cell.Position.y) * 0.5f);
                TriangulateEdgeStrip(
                    edge1, weights1, cell.Index,
                    edge2, weights5, neighbor.Index
                );
                return;
            }

            TriangulateEdgeStrip(
                edge1, weights1, cell.Index,
                edge2, weights1, cell.Index
            );
        }
        else
        {
            bool exchangeVertices = cell.SlopeDirection == direction.Next2();
            exchangeVertices = elevationDifference == 1 ? exchangeVertices : !exchangeVertices;

            edge2.AddPosY(elevationDifference * QuadMetrics.leanElevationFactor * QuadMetrics.elevationStep);
            if (cell.Elevation + Math.Sign(elevationDifference + 1) == neighbor.Elevation)
            {
                float additionalElevation = Math.Sign(elevationDifference + 1) * QuadMetrics.elevationStep;
                if (exchangeVertices)
                {
                    edge2.vertix4.y += (neighbor.Position.y - elevationDifference * QuadMetrics.leanElevationFactor * QuadMetrics.elevationStep - edge2.vertix4.y) * 0.5f;
                    edge2.vertix5.y = (neighbor.Position.y + cell.Position.y + additionalElevation) * 0.5f;

                    TriangulateIncompleteRightEdgeStrip(edge1, edge2, weights5, weights5, cell.Index, neighbor.Index);
                }
                else
                {
                    edge2.vertix2.y += (neighbor.Position.y - elevationDifference * QuadMetrics.leanElevationFactor * QuadMetrics.elevationStep - edge2.vertix2.y) * 0.5f;
                    edge2.vertix1.y = (neighbor.Position.y + cell.Position.y + additionalElevation) * 0.5f;

                    TriangulateIncompleteLeftEdgeStrip(edge1, edge2, weights5, weights5, cell.Index, neighbor.Index);
                }
                if (direction == QuadDirection.North || direction == QuadDirection.East)
                {
                    if (exchangeVertices)
                    {
                        edge1 = new EdgeVertices(
                            QuadMetrics.SetVertixElevation(edge2.vertix5, neighbor.Position.y - elevationDifference * QuadMetrics.leanElevationFactor * QuadMetrics.elevationStep),
                            QuadMetrics.SetVertixElevation(edge2.vertix1, neighbor.Position.y - elevationDifference * QuadMetrics.leanElevationFactor * QuadMetrics.elevationStep)
                        );
                        edge2.MirrorVerices();
                        edge1.vertix2.y = edge2.vertix2.y;
                        TriangulateEdgeTriangleSlopeCliff(edge1, weights2, cell.Index, edge2, weights1, neighbor.Index);
                    }
                    else
                    {
                        edge1 = new EdgeVertices(
                            QuadMetrics.SetVertixElevation(edge2.vertix1, neighbor.Position.y - elevationDifference * QuadMetrics.leanElevationFactor * QuadMetrics.elevationStep),
                            QuadMetrics.SetVertixElevation(edge2.vertix5, neighbor.Position.y - elevationDifference * QuadMetrics.leanElevationFactor * QuadMetrics.elevationStep)
                        );
                        TriangulateEdgeTriangleSlopeCliff(edge2, weights1, cell.Index, edge1, weights2, neighbor.Index);
                    }
                }
                return;
            }

            TriangulateEdgeStrip(
                edge1, weights1, cell.Index,
                edge2, weights1, cell.Index
            );
        }

        if (direction == QuadDirection.North || direction == QuadDirection.East)
        {
            edge2.SetPosY(neighbor.Position.y - elevationDifference * QuadMetrics.leanElevationFactor * QuadMetrics.elevationStep);
            edge2.MirrorVerices();
            TriangulateConnectionWithSlope(
                -elevationDifference, neighbor.Index, cell.Index,
                neighbor.Position.y, cell.Position.y,
                direction.Opposite(), cell.SlopeDirection, edge2, weights1, weights2,
                direction == cell.SlopeDirection.Next2() || direction == cell.SlopeDirection.Previous2()
            );
        }
    }

    private void TriangulateConnectionSlopeSlope(
        QuadCell cell, QuadCell neighbor,
        EdgeVertices edge1, EdgeVertices edge2,
        QuadDirection direction, int elevationDifference
    )
    {
        if (cell.SlopeDirection == direction || cell.SlopeDirection == direction.Opposite())
        {
            bool isOnSide = neighbor.SlopeDirection == direction.Next2() || neighbor.SlopeDirection == direction.Previous2();
            edge2.AddPosY(elevationDifference * QuadMetrics.leanElevationFactor * QuadMetrics.elevationStep);

            if ((neighbor.Elevation - cell.Elevation == elevationDifference && cell.SlopeDirection == neighbor.SlopeDirection) ||
                (neighbor.Elevation == cell.Elevation && cell.SlopeDirection.Opposite() == neighbor.SlopeDirection))
            {
                edge2.AddPosY((neighbor.Position.y - cell.Position.y -
                    (neighbor.Elevation - cell.Elevation == 0 ? 0 : elevationDifference) * QuadMetrics.elevationStep) * 0.5f);
                TriangulateEdgeStrip(
                    edge1, weights1, cell.Index,
                    edge2, weights5, neighbor.Index
                );
                return;
            }

            if (isOnSide && (cell.Elevation == neighbor.Elevation || neighbor.Elevation - cell.Elevation == elevationDifference))
            {
                if ((elevationDifference == 1 && ((neighbor.SlopeDirection == direction.Next2() && cell.Elevation == neighbor.Elevation) ||
                    (neighbor.SlopeDirection == direction.Previous2() && cell.Elevation != neighbor.Elevation))) ||
                    (elevationDifference == -1 && ((neighbor.SlopeDirection == direction.Next2() && cell.Elevation != neighbor.Elevation) ||
                    (neighbor.SlopeDirection == direction.Previous2() && cell.Elevation == neighbor.Elevation))))
                {
                    edge2.vertix5.y += (neighbor.Position.y - cell.Position.y -
                        (neighbor.Elevation - cell.Elevation == 0 ? 0 : elevationDifference) * QuadMetrics.elevationStep) * 0.5f;
                    TriangulateIncompleteRightEdgeStrip(edge1, edge2, weights1 * 0.625f + weights2 * 0.375f, weights5, cell.Index, neighbor.Index);
                }
                else
                {
                    edge2.vertix1.y += (neighbor.Position.y - cell.Position.y -
                        (neighbor.Elevation - cell.Elevation == 0 ? 0 : elevationDifference) * QuadMetrics.elevationStep) * 0.5f;
                    TriangulateIncompleteLeftEdgeStrip(edge1, edge2, weights1 * 0.625f + weights2 * 0.375f, weights5, cell.Index, neighbor.Index);
                }
            }
            else
            {
                TriangulateEdgeStrip(
                    edge1, weights1, cell.Index,
                    edge2, weights1, cell.Index
                );
            }

            if (direction == QuadDirection.North || direction == QuadDirection.East)
            {
                int additionalElevation;
                if (!isOnSide)
                {
                    if (cell.Elevation == neighbor.Elevation)
                    {
                        additionalElevation = Math.Sign(elevationDifference + 1);
                        elevationDifference = -elevationDifference;
                    }
                    else
                    {
                        additionalElevation = cell.SlopeDirection == direction ? 1 : 0;
                        elevationDifference = cell.Elevation < neighbor.Elevation ? 1 : -1;
                    }
                }
                else
                {
                    additionalElevation = Math.Sign(elevationDifference + 1);
                    additionalElevation = cell.Elevation + additionalElevation == neighbor.Elevation ? 2 : cell.Elevation + additionalElevation == neighbor.Elevation + 1 ? -2 : additionalElevation;
                    if (additionalElevation == 2 || additionalElevation == -2)
                    {
                        if ((additionalElevation == 2 && neighbor.SlopeDirection == direction.Next2()) ||
                            (additionalElevation == -2 && neighbor.SlopeDirection == direction.Previous2()))
                        {
                            edge1 = new EdgeVertices(
                                QuadMetrics.SetVertixElevation(edge2.vertix1, neighbor.Position.y + Math.Sign(-additionalElevation + 2) * QuadMetrics.elevationStep),
                                QuadMetrics.SetVertixElevation(edge2.vertix5, neighbor.Position.y + 
                                    (additionalElevation / 2) * ((Math.Sign(additionalElevation + 2) - 2f * QuadMetrics.leanElevationFactor) * QuadMetrics.elevationStep))
                            );
                           
                            edge1.vertix1.y = edge2.vertix1.y;
                            TriangulateEdgeTriangleSlopeSlope(edge2, edge1, weights5, weights1, weights2, cell.Index, neighbor.Index);
                        }
                        else
                        {
                            edge2.MirrorVerices();
                            edge1 = new EdgeVertices(
                                QuadMetrics.SetVertixElevation(edge2.vertix1, neighbor.Position.y + Math.Sign(-additionalElevation + 2) * QuadMetrics.elevationStep),
                                QuadMetrics.SetVertixElevation(edge2.vertix5, neighbor.Position.y +
                                    (additionalElevation / 2) * ((Math.Sign(additionalElevation + 2) - 2f * QuadMetrics.leanElevationFactor) * QuadMetrics.elevationStep))
                            );
                            edge1.vertix1.y = edge2.vertix1.y;
                            TriangulateEdgeTriangleSlopeSlope(edge1, edge2, weights5, weights2, weights1, cell.Index, neighbor.Index);
                        }
                        return;
                    }
                    additionalElevation = cell.SlopeDirection == direction ? 1 : 0;
                    elevationDifference = cell.Elevation < neighbor.Elevation ? 1 : -1;
                }

                TriangulateConnectionWithSlope(
                    elevationDifference, cell.Index, neighbor.Index,
                    cell.Position.y + additionalElevation * QuadMetrics.elevationStep, neighbor.Position.y,
                    direction, neighbor.SlopeDirection, edge2, weights1, weights2, isOnSide
                );
            }
        }
        else
        {
            bool exchangeVertices = cell.SlopeDirection == direction.Next2();
            if (elevationDifference == 1)
            {
                edge2.AddPosY(QuadMetrics.elevationStep * QuadMetrics.leanElevationFactor);
                if (direction != QuadDirection.North && direction != QuadDirection.East)
                {
                    if ((neighbor.SlopeDirection == direction.Opposite() && cell.Elevation == neighbor.Elevation) ||
                        ((neighbor.SlopeDirection == direction || neighbor.SlopeDirection == cell.SlopeDirection.Opposite()) && cell.Elevation + 1 == neighbor.Elevation))
                    {
                        float additionalElevation = (neighbor.Elevation - cell.Elevation == 0 ? 0 : 1)
                            * QuadMetrics.elevationStep;
                        if (exchangeVertices)
                        {
                            edge2.vertix5.y += (neighbor.Position.y - cell.Position.y - additionalElevation) * 0.5f;
                            TriangulateIncompleteRightEdgeStrip(edge1, edge2, 0.625f * weights1 + 0.375f * weights2, weights5, cell.Index, neighbor.Index);
                            return;
                        }

                        edge2.vertix1.y += (neighbor.Position.y - cell.Position.y - additionalElevation) * 0.5f;
                        TriangulateIncompleteLeftEdgeStrip(edge1, edge2, 0.625f * weights1 + 0.375f * weights2, weights5, cell.Index, neighbor.Index);
                        return;
                    }

                    TriangulateEdgeStrip(
                        edge1, weights1, cell.Index,
                        edge2, weights1, cell.Index
                    );
                    return;
                }

                if (exchangeVertices)
                {
                    if (neighbor.SlopeDirection == cell.SlopeDirection.Opposite() && cell.Elevation + 1 == neighbor.Elevation)
                    {
                        edge2.vertix5.y += (neighbor.Position.y - cell.Position.y - QuadMetrics.elevationStep) * 0.5f;
                        TriangulateEdgeStripVSlopeSlope(edge2.MirrorVerices(), weights2, cell.Index, edge1.MirrorVerices(), weights1, neighbor.Index);

                        edge1 = new EdgeVertices(
                            QuadMetrics.SetVertixElevation(edge2.vertix1, neighbor.Position.y),
                            QuadMetrics.SetVertixElevation(edge2.vertix5, neighbor.Position.y + (1 - 2f * QuadMetrics.leanElevationFactor) * QuadMetrics.elevationStep)
                        );
                        edge1.vertix1.y = edge2.vertix1.y;          
                        
                        terrain.AddQuad(
                            QuadMetrics.SetVertixElevation(edge1.vertix5, cell.Position.y + QuadMetrics.elevationStep),
                            QuadMetrics.SetVertixElevation(edge1.vertix4, edge1.vertix5.y + (cell.Position.y + QuadMetrics.elevationStep - edge1.vertix5.y) * 0.75f),
                            edge1.vertix5,
                            edge1.vertix4
                        );
                        terrain.AddTriangle(
                            QuadMetrics.SetVertixElevation(edge1.vertix4, edge1.vertix5.y + (cell.Position.y + QuadMetrics.elevationStep - edge1.vertix5.y) * 0.75f), edge1.vertix4, edge1.vertix3
                        );

                        Vector4 indices = Vector4.one * neighbor.Index;
                        terrain.AddQuadCellData(indices, weights2);
                        terrain.AddTriangleCellData(indices, weights2);

                        edge1.vertix4.y = edge1.vertix5.y + (cell.Position.y + QuadMetrics.elevationStep - edge1.vertix5.y) * 0.75f;
                        edge1.vertix5.y = cell.Position.y + QuadMetrics.elevationStep;
                        TriangulateVSlopeSlopeConnection(edge1, weights2, cell.Index, edge2, weights1, neighbor.Index);
                        return;
                    }

                    if (cell.Elevation == neighbor.Elevation || (neighbor.SlopeDirection == direction && cell.Elevation + 1 == neighbor.Elevation))
                    {
                        int additionalElevation = cell.Elevation == neighbor.Elevation ? 0 : 1;
                        edge2.vertix5.y += (neighbor.Position.y - cell.Position.y - additionalElevation * QuadMetrics.elevationStep) * 0.5f;
                        TriangulateIncompleteRightEdgeStrip(edge1, edge2, 0.625f * weights1 + 0.375f * weights2, weights5, cell.Index, neighbor.Index);
                        edge2.MirrorVerices();                       
                        edge1 = new EdgeVertices(
                            QuadMetrics.SetVertixElevation(edge2.vertix1, neighbor.Position.y + Math.Sign(-additionalElevation + 1) * QuadMetrics.elevationStep),
                            QuadMetrics.SetVertixElevation(edge2.vertix5, neighbor.Position.y + Math.Sign(-additionalElevation + 1) * QuadMetrics.elevationStep)
                        );
                        edge1.vertix1.y = edge2.vertix1.y;
                        TriangulateEdgeTriangleSlopeSlope(edge1, edge2, weights5, weights2, weights1, cell.Index, neighbor.Index);
                        return;
                    }

                    TriangulateEdgeStrip(
                        edge1, weights1, cell.Index,
                        edge2, weights1, cell.Index
                    );

                    if (cell.Elevation + 1 == neighbor.Elevation && neighbor.SlopeDirection == cell.SlopeDirection)
                    {                        
                        edge1 = new EdgeVertices(
                            QuadMetrics.SetVertixElevation(edge2.vertix1, neighbor.Position.y),
                            QuadMetrics.SetVertixElevation(edge2.vertix5, neighbor.Position.y + (1 - 2f * QuadMetrics.leanElevationFactor) * QuadMetrics.elevationStep)
                        );
                        TriangulateEdgeStrip(
                            edge2, weights1, cell.Index,
                            edge1, weights2, neighbor.Index
                        );
                        return;
                    }

                    if ((cell.Elevation + 1 == neighbor.Elevation && neighbor.SlopeDirection == direction.Opposite()) ||
                        (cell.Elevation + 2 == neighbor.Elevation && neighbor.SlopeDirection == direction))
                    {
                        float additionalElevation = (neighbor.SlopeDirection == direction ? 0 : 1) * QuadMetrics.elevationStep;                        
                        edge1 = new EdgeVertices(
                            QuadMetrics.SetVertixElevation(edge2.vertix1, cell.Position.y + QuadMetrics.elevationStep),
                            QuadMetrics.SetVertixElevation(edge2.vertix5, neighbor.Position.y + additionalElevation)
                        );
                        TriangulateEdgeStrip(
                            edge2, weights1, cell.Index,
                            edge1, weights2, neighbor.Index
                        );
                        edge1.MirrorVerices();                        
                        edge2 = new EdgeVertices(
                            edge1.vertix1,
                            QuadMetrics.SetVertixElevation(edge1.vertix5, neighbor.Position.y + additionalElevation)
                        );
                        TriangulateEdgeTriangleSlopeSlope(edge2, edge1, weights2, weights2, weights2, neighbor.Index, neighbor.Index);
                        return;
                    }

                    if (cell.Elevation + 2 == neighbor.Elevation && neighbor.SlopeDirection == cell.SlopeDirection.Opposite())
                    {
                        edge1 = new EdgeVertices(
                            QuadMetrics.SetVertixElevation(edge2.vertix1, cell.Position.y + QuadMetrics.elevationStep),
                            QuadMetrics.SetVertixElevation(edge2.vertix5, neighbor.Position.y)
                        );
                        TriangulateEdgeStrip(
                            edge2, weights1, cell.Index,
                            edge1, weights2, neighbor.Index
                        );
                        edge1.MirrorVerices();
                        edge2 = new EdgeVertices(
                            edge1.vertix1,
                            QuadMetrics.SetVertixElevation(edge1.vertix5, neighbor.Position.y + (1 - 2f * QuadMetrics.leanElevationFactor) * QuadMetrics.elevationStep)
                        );
                        TriangulateEdgeTriangleSlopeSlope(edge2, edge1, weights2, weights2, weights2, neighbor.Index, neighbor.Index);
                        return;
                    }
                    
                    edge1 = new EdgeVertices(
                        QuadMetrics.SetVertixElevation(edge2.vertix1, cell.Position.y + QuadMetrics.elevationStep),
                        QuadMetrics.SetVertixElevation(edge2.vertix5, cell.Position.y + (1 + QuadMetrics.straightElevationFactor) * QuadMetrics.elevationStep)
                    );
                    TriangulateEdgeStrip(
                        edge2, weights1, cell.Index,
                        edge1, weights2, neighbor.Index
                    );
                }
                else
                {
                    if (neighbor.SlopeDirection == cell.SlopeDirection.Opposite() && cell.Elevation + 1 == neighbor.Elevation)
                    {
                        edge2.vertix1.y += (neighbor.Position.y - cell.Position.y - QuadMetrics.elevationStep) * 0.5f;
                        TriangulateEdgeStripVSlopeSlope(edge1, weights1, cell.Index, edge2, weights2, neighbor.Index);
                       
                        edge1 = new EdgeVertices(
                            QuadMetrics.SetVertixElevation(edge2.vertix1, neighbor.Position.y),
                            QuadMetrics.SetVertixElevation(edge2.vertix5, neighbor.Position.y + (1 - 2f * QuadMetrics.leanElevationFactor) * QuadMetrics.elevationStep)
                        );
                        edge1.vertix1.y = edge2.vertix1.y;    
                        
                        terrain.AddQuad(
                            QuadMetrics.SetVertixElevation(edge1.vertix4, edge1.vertix5.y + (cell.Position.y + QuadMetrics.elevationStep - edge1.vertix5.y) * 0.75f),
                            QuadMetrics.SetVertixElevation(edge1.vertix5, cell.Position.y + QuadMetrics.elevationStep),
                            edge1.vertix4,
                            edge1.vertix5
                        );                   
                        terrain.AddTriangle(
                            QuadMetrics.SetVertixElevation(edge1.vertix4, edge1.vertix5.y + (cell.Position.y + QuadMetrics.elevationStep - edge1.vertix5.y) * 0.75f), edge1.vertix3, edge1.vertix4
                        );

                        Vector4 indices = Vector4.one * neighbor.Index;
                        terrain.AddQuadCellData(indices, weights2);
                        terrain.AddTriangleCellData(indices, weights2);

                        edge1.vertix4.y = edge1.vertix5.y + (cell.Position.y + QuadMetrics.elevationStep - edge1.vertix5.y) * 0.75f;
                        edge1.vertix5.y = cell.Position.y + QuadMetrics.elevationStep;
                        TriangulateVSlopeSlopeConnection(edge2, weights1, cell.Index, edge1, weights2, neighbor.Index);
                        return;
                    }

                    if (cell.Elevation == neighbor.Elevation || (neighbor.SlopeDirection == direction && cell.Elevation + 1 == neighbor.Elevation))
                    {
                        int additionalElevation = cell.Elevation == neighbor.Elevation ? 0 : 1;
                        edge2.vertix1.y += (neighbor.Position.y - cell.Position.y - additionalElevation * QuadMetrics.elevationStep) * 0.5f;
                        TriangulateIncompleteLeftEdgeStrip(edge1, edge2, 0.625f * weights1 + 0.375f * weights2, weights5, cell.Index, neighbor.Index);
                        edge1 = new EdgeVertices(
                            QuadMetrics.SetVertixElevation(edge2.vertix1, neighbor.Position.y + Math.Sign(-additionalElevation + 1) * QuadMetrics.elevationStep),
                            QuadMetrics.SetVertixElevation(edge2.vertix5, neighbor.Position.y + Math.Sign(-additionalElevation + 1) * QuadMetrics.elevationStep)
                        );
                        edge1.vertix1.y = edge2.vertix1.y;
                        TriangulateEdgeTriangleSlopeSlope(edge2, edge1, weights5, weights1, weights2, cell.Index, neighbor.Index);
                        return;
                    }

                    TriangulateEdgeStrip(
                        edge1, weights1, cell.Index,
                        edge2, weights1, cell.Index
                    );

                    if (cell.Elevation + 1 == neighbor.Elevation && neighbor.SlopeDirection == cell.SlopeDirection)
                    {                        
                        edge1 = new EdgeVertices(
                            QuadMetrics.SetVertixElevation(edge2.vertix1, neighbor.Position.y + (1 - 2f * QuadMetrics.leanElevationFactor) * QuadMetrics.elevationStep),
                            QuadMetrics.SetVertixElevation(edge2.vertix5, neighbor.Position.y)
                        );
                        TriangulateEdgeStrip(
                            edge2, weights1, cell.Index,
                            edge1, weights2, neighbor.Index
                        );
                        return;
                    }

                    if ((cell.Elevation + 1 == neighbor.Elevation && neighbor.SlopeDirection == direction.Opposite()) ||
                        (cell.Elevation + 2 == neighbor.Elevation && neighbor.SlopeDirection == direction))
                    {
                        float additionalElevation = (neighbor.SlopeDirection == direction ? 0 : 1) * QuadMetrics.elevationStep;                        
                        edge1 = new EdgeVertices(
                            QuadMetrics.SetVertixElevation(edge2.vertix1, neighbor.Position.y + additionalElevation),
                            QuadMetrics.SetVertixElevation(edge2.vertix5, cell.Position.y + QuadMetrics.elevationStep)
                        );
                        TriangulateEdgeStrip(
                            edge2, weights1, cell.Index,
                            edge1, weights2, neighbor.Index
                        );
                        edge2 = new EdgeVertices(
                            edge1.vertix1,
                            QuadMetrics.SetVertixElevation(edge1.vertix5, neighbor.Position.y + additionalElevation)
                        );
                        TriangulateEdgeTriangleSlopeSlope(edge1, edge2, weights2, weights2, weights2, neighbor.Index, neighbor.Index);
                        return;
                    }

                    if (cell.Elevation + 2 == neighbor.Elevation && neighbor.SlopeDirection == cell.SlopeDirection.Opposite())
                    {                        
                        edge1 = new EdgeVertices(
                            QuadMetrics.SetVertixElevation(edge2.vertix1, neighbor.Position.y),
                            QuadMetrics.SetVertixElevation(edge2.vertix5, cell.Position.y + QuadMetrics.elevationStep)
                        );
                        TriangulateEdgeStrip(
                            edge2, weights1, cell.Index,
                            edge1, weights2, neighbor.Index
                        );                       
                        edge2 = new EdgeVertices(
                            edge1.vertix1,
                            QuadMetrics.SetVertixElevation(edge1.vertix5, neighbor.Position.y + (1 - 2f * QuadMetrics.leanElevationFactor) * QuadMetrics.elevationStep)
                        );
                        TriangulateEdgeTriangleSlopeSlope(edge1, edge2, weights2, weights2, weights2, cell.Index, neighbor.Index);
                        return;
                    }
                   
                    edge1 = new EdgeVertices(
                        QuadMetrics.SetVertixElevation(edge2.vertix1, cell.Position.y + (1 + QuadMetrics.straightElevationFactor) * QuadMetrics.elevationStep),
                        QuadMetrics.SetVertixElevation(edge2.vertix5, cell.Position.y + QuadMetrics.elevationStep)
                    );
                    TriangulateEdgeStrip(
                        edge2, weights1, cell.Index,
                        edge1, weights2, neighbor.Index
                    );
                }

                if (cell.SlopeDirection == neighbor.SlopeDirection || cell.SlopeDirection.Opposite() == neighbor.SlopeDirection)
                {
                    if (direction != neighbor.SlopeDirection.Next2())
                    {                        
                        edge2 = new EdgeVertices(
                            QuadMetrics.SetVertixElevation(edge1.vertix1, neighbor.Position.y),
                            QuadMetrics.SetVertixElevation(edge1.vertix5, neighbor.Position.y + QuadMetrics.elevationStep * (1 - 2f * QuadMetrics.leanElevationFactor))
                        );
                        TriangulateEdgeStrip(
                            edge1, weights2, neighbor.Index,
                            edge2, weights2, neighbor.Index
                        );
                    }
                    else
                    {                       
                        edge2 = new EdgeVertices(
                            QuadMetrics.SetVertixElevation(edge1.vertix1, neighbor.Position.y + QuadMetrics.elevationStep * (1 - 2f * QuadMetrics.leanElevationFactor)),
                            QuadMetrics.SetVertixElevation(edge1.vertix5, neighbor.Position.y)
                        );
                        TriangulateEdgeStrip(
                            edge1, weights2, neighbor.Index,
                            edge2, weights2, neighbor.Index
                        );
                    }
                }
                else
                {
                    float neighborElevation = neighbor.Position.y + (direction == neighbor.SlopeDirection ? 0 : 1) * QuadMetrics.elevationStep;
                    edge2 = new EdgeVertices(
                        QuadMetrics.SetVertixElevation(edge1.vertix1, neighborElevation),
                        QuadMetrics.SetVertixElevation(edge1.vertix5, neighborElevation)
                    );
                    TriangulateEdgeStrip(
                        edge1, weights2, neighbor.Index,
                        edge2, weights2, neighbor.Index
                    );
                }
            }
            else
            {
                edge2.AddPosY(-QuadMetrics.elevationStep * QuadMetrics.leanElevationFactor);
                if (direction != QuadDirection.North && direction != QuadDirection.East)
                {
                    if ((neighbor.SlopeDirection == direction && cell.Elevation == neighbor.Elevation) ||
                        ((neighbor.SlopeDirection == direction.Opposite() || neighbor.SlopeDirection == cell.SlopeDirection.Opposite()) && cell.Elevation - 1 == neighbor.Elevation))
                    {
                        float additionalElevation = (neighbor.Elevation - cell.Elevation == 0 ? 0 : 1)
                            * QuadMetrics.elevationStep;
                        if (!exchangeVertices)
                        {
                            edge2.vertix5.y += (neighbor.Position.y + additionalElevation - cell.Position.y) * 0.5f;
                            TriangulateIncompleteRightEdgeStrip(edge1, edge2, 0.625f * weights1 + 0.375f * weights2, weights5, cell.Index, neighbor.Index);
                            return;
                        }
                        edge2.vertix1.y += (neighbor.Position.y + additionalElevation - cell.Position.y) * 0.5f;
                        TriangulateIncompleteLeftEdgeStrip(edge1, edge2, 0.625f * weights1 + 0.375f * weights2, weights5, cell.Index, neighbor.Index);
                        return;
                    }
                    TriangulateEdgeStrip(
                        edge1, weights1, cell.Index,
                        edge2, weights1, cell.Index
                    );
                    return;
                }

                if (!exchangeVertices)
                {
                    if (neighbor.SlopeDirection == cell.SlopeDirection.Opposite() && cell.Elevation - 1 == neighbor.Elevation)
                    {
                        edge2.vertix5.y += (neighbor.Position.y + QuadMetrics.elevationStep - cell.Position.y) * 0.5f;
                        TriangulateEdgeStripVSlopeSlope(edge2.MirrorVerices(), weights2, cell.Index, edge1.MirrorVerices(), weights1, neighbor.Index);                        
                        edge1 = new EdgeVertices(
                            QuadMetrics.SetVertixElevation(edge2.vertix1, neighbor.Position.y + QuadMetrics.elevationStep),
                            QuadMetrics.SetVertixElevation(edge2.vertix5, neighbor.Position.y + 2f * QuadMetrics.leanElevationFactor * QuadMetrics.elevationStep)
                        );
                        edge1.vertix1.y = edge2.vertix1.y;

                        terrain.AddQuad(
                            edge2.vertix5,
                            edge2.vertix4,
                            QuadMetrics.SetVertixElevation(edge2.vertix5, neighbor.Position.y + QuadMetrics.elevationStep),
                            QuadMetrics.SetVertixElevation(edge2.vertix4, edge2.vertix5.y + (neighbor.Position.y + QuadMetrics.elevationStep - edge2.vertix5.y) * 0.75f)
                        );                 
                        terrain.AddTriangle(QuadMetrics.SetVertixElevation(
                            edge2.vertix4, edge2.vertix5.y + (neighbor.Position.y + QuadMetrics.elevationStep - edge2.vertix5.y) * 0.75f), edge2.vertix3, edge2.vertix4
                        );

                        Vector4 indices = Vector4.one * cell.Index;
                        terrain.AddQuadCellData(indices, weights1);
                        terrain.AddTriangleCellData(indices, weights1);

                        edge2.vertix4.y = edge2.vertix5.y + (neighbor.Position.y + QuadMetrics.elevationStep - edge2.vertix5.y) * 0.75f;
                        edge2.vertix5.y = neighbor.Position.y + QuadMetrics.elevationStep;
                        TriangulateVSlopeSlopeConnection(edge1, weights2, cell.Index, edge2, weights1, neighbor.Index);
                        return;
                    }

                    if (cell.Elevation == neighbor.Elevation || (neighbor.SlopeDirection == direction.Opposite() && cell.Elevation - 1 == neighbor.Elevation))
                    {
                        float additionalElevation = (cell.Elevation == neighbor.Elevation ? 0 : 1) * QuadMetrics.elevationStep;
                        edge2.vertix5.y += (neighbor.Position.y - cell.Position.y + additionalElevation) * 0.5f;
                        TriangulateIncompleteRightEdgeStrip(edge1, edge2, 0.625f * weights1 + 0.375f * weights2, weights5, cell.Index, neighbor.Index);
                        edge2.MirrorVerices();
                        edge1 = new EdgeVertices(
                            QuadMetrics.SetVertixElevation(edge2.vertix1, neighbor.Position.y + additionalElevation),
                            QuadMetrics.SetVertixElevation(edge2.vertix5, neighbor.Position.y + additionalElevation)
                        );
                        edge1.vertix1.y = edge2.vertix1.y;
                        TriangulateEdgeTriangleSlopeSlope(edge1, edge2, weights5, weights2, weights1, cell.Index, neighbor.Index);
                        return;
                    }
                    
                    TriangulateEdgeStrip(
                        edge1, weights1, cell.Index,
                        edge2, weights1, cell.Index
                    );

                    if (neighbor.SlopeDirection == cell.SlopeDirection && cell.Elevation - 1 == neighbor.Elevation)
                    {
                        edge1 = new EdgeVertices(
                            QuadMetrics.SetVertixElevation(edge2.vertix1, neighbor.Position.y + QuadMetrics.elevationStep),
                            QuadMetrics.SetVertixElevation(edge2.vertix5, neighbor.Position.y + 2f * QuadMetrics.leanElevationFactor * QuadMetrics.elevationStep)
                        );
                        TriangulateEdgeStrip(
                            edge2, weights1, cell.Index,
                            edge1, weights2, neighbor.Index
                        );
                        return;
                    }
                }
                else
                {
                    if (neighbor.SlopeDirection == cell.SlopeDirection.Opposite() && cell.Elevation - 1 == neighbor.Elevation)
                    {
                        edge2.vertix1.y += (neighbor.Position.y + QuadMetrics.elevationStep - cell.Position.y) * 0.5f;
                        TriangulateEdgeStripVSlopeSlope(edge1, weights1, cell.Index, edge2, weights2, neighbor.Index);
                        
                        edge1 = new EdgeVertices(
                            QuadMetrics.SetVertixElevation(edge2.vertix1, neighbor.Position.y + QuadMetrics.elevationStep),
                            QuadMetrics.SetVertixElevation(edge2.vertix5, neighbor.Position.y + 2f * QuadMetrics.leanElevationFactor * QuadMetrics.elevationStep)
                        );
                        edge1.vertix1.y = edge2.vertix1.y;

                        terrain.AddQuad(
                            edge2.vertix4,
                            edge2.vertix5,
                            QuadMetrics.SetVertixElevation(edge2.vertix4, edge2.vertix5.y + (neighbor.Position.y + QuadMetrics.elevationStep - edge2.vertix5.y) * 0.75f),
                            QuadMetrics.SetVertixElevation(edge2.vertix5, neighbor.Position.y + QuadMetrics.elevationStep)
                        );
                        terrain.AddTriangle(
                            QuadMetrics.SetVertixElevation(edge2.vertix4, edge2.vertix5.y + (neighbor.Position.y + QuadMetrics.elevationStep - edge2.vertix5.y) * 0.75f), edge2.vertix4, edge2.vertix3
                        );

                        Vector4 indices = Vector4.one * cell.Index;
                        terrain.AddQuadCellData(indices, weights1);
                        terrain.AddTriangleCellData(indices, weights1);

                        edge2.vertix4.y = edge2.vertix5.y + (neighbor.Position.y + QuadMetrics.elevationStep - edge2.vertix5.y) * 0.75f;
                        edge2.vertix5.y = neighbor.Position.y + QuadMetrics.elevationStep;
                        TriangulateVSlopeSlopeConnection(edge2, weights1, cell.Index, edge1, weights2, neighbor.Index);
                        return;
                    }
                    
                    if (cell.Elevation == neighbor.Elevation || (neighbor.SlopeDirection == direction.Opposite() && cell.Elevation - 1 == neighbor.Elevation))
                    {
                        float additionalElevation = (cell.Elevation == neighbor.Elevation ? 0 : 1) * QuadMetrics.elevationStep;
                        edge2.vertix1.y += (neighbor.Position.y - cell.Position.y + additionalElevation) * 0.5f;
                        TriangulateIncompleteLeftEdgeStrip(edge1, edge2, 0.625f * weights1 + 0.375f * weights2, weights5, cell.Index, neighbor.Index);
                        edge1 = new EdgeVertices(
                            QuadMetrics.SetVertixElevation(edge2.vertix1, neighbor.Position.y + additionalElevation),
                            QuadMetrics.SetVertixElevation(edge2.vertix5, neighbor.Position.y + additionalElevation)
                        );
                        edge1.vertix1.y = edge2.vertix1.y;
                        TriangulateEdgeTriangleSlopeSlope(edge2, edge1, weights5, weights1, weights2, cell.Index, neighbor.Index);
                        return;
                    }

                    TriangulateEdgeStrip(
                        edge1, weights1, cell.Index,
                        edge2, weights1, cell.Index
                    );
                    
                    if (neighbor.SlopeDirection == cell.SlopeDirection && cell.Elevation - 1 == neighbor.Elevation)
                    {
                        edge1 = new EdgeVertices(
                            QuadMetrics.SetVertixElevation(edge2.vertix1, neighbor.Position.y + 2f * QuadMetrics.leanElevationFactor * QuadMetrics.elevationStep),
                            QuadMetrics.SetVertixElevation(edge2.vertix5, neighbor.Position.y + QuadMetrics.elevationStep)
                        );
                        TriangulateEdgeStrip(
                            edge2, weights1, cell.Index,
                            edge1, weights2, neighbor.Index
                        );
                        return;
                    }
                }

                TriangulateConnectionWithSlope(
                    elevationDifference, cell.Index, neighbor.Index,
                    cell.Position.y + (cell.SlopeDirection == neighbor.SlopeDirection.Opposite() ? 0 : QuadMetrics.elevationStep),
                    neighbor.Position.y, direction, neighbor.SlopeDirection, edge2, weights1, weights2,
                    cell.SlopeDirection == neighbor.SlopeDirection || cell.SlopeDirection.Opposite() == neighbor.SlopeDirection
                );
            }
        }
    }

    private void TriangulateConnectionWithSlope(
        int elevationDifference, int index1, int index2, float elevation1, float elevation2,
        QuadDirection direction, QuadDirection slopeDirection,
        EdgeVertices edge1, Color weight1, Color weight2, bool isOnSide
    )
    {
        EdgeVertices edge2;
        if (elevationDifference == 1)
        {
            if (!isOnSide)
            {
                edge2 = edge1;
                float neighborElevation = elevation2 + ((direction.Opposite() == slopeDirection ? 1 : 0) * QuadMetrics.elevationStep);
                if (Mathf.Round(elevation1 / QuadMetrics.elevationStep) + 1 != Mathf.Round(neighborElevation / QuadMetrics.elevationStep))
                {
                    edge2.SetPosY(elevation1 + QuadMetrics.straightElevationFactor * QuadMetrics.elevationStep);
                    TriangulateEdgeStrip(edge1, weight1, index1, edge2, weight2, index2);
                    edge1.SetPosY(neighborElevation);
                    TriangulateEdgeStrip(edge2, weight2, index2, edge1, weight2, index2);
                    return;
                }

                edge2.SetPosY(neighborElevation);
                TriangulateEdgeStrip(edge1, weight1, index1, edge2, weight2, index2);
            }
            else
            {
                if (Mathf.Round(elevation1 / QuadMetrics.elevationStep) + 1 == Mathf.Round(elevation2 / QuadMetrics.elevationStep))
                {
                    if (direction == slopeDirection.Next2())
                    {
                        edge1.MirrorVerices();
                        edge2 = edge1;
                        edge2.SetPosY(elevation1 + QuadMetrics.straightElevationFactor * QuadMetrics.elevationStep);
                        edge2.vertix1.y = elevation2;
                        TriangulateEdgeStrip(edge2, weight2, index1, edge1, weight1, index2);
                        edge1 = new EdgeVertices(
                            edge2.vertix1,
                            QuadMetrics.SetVertixElevation(edge2.vertix5, elevation2 + QuadMetrics.elevationStep * (1 - 2f * QuadMetrics.leanElevationFactor))
                        );
                        TriangulateEdgeTriangleSlopeSlope(edge1, edge2, weight2, weight2, weight2, index2, index2);
                    }
                    else
                    {
                        edge2 = edge1;
                        edge2.SetPosY(elevation1 + QuadMetrics.straightElevationFactor * QuadMetrics.elevationStep);
                        edge2.vertix1.y = elevation2;
                        TriangulateEdgeStrip(edge1, weight1, index1, edge2, weight2, index2);
                        edge1 = new EdgeVertices(
                            edge2.vertix1,
                            QuadMetrics.SetVertixElevation(edge2.vertix5, elevation2 + QuadMetrics.elevationStep * (1 - 2f * QuadMetrics.leanElevationFactor))
                        );
                        TriangulateEdgeTriangleSlopeSlope(edge2, edge1, weight2, weight2, weight2, index2, index2);
                    }
                    return;
                }

                if (direction == slopeDirection.Next2())
                {
                    edge1.MirrorVerices();
                    edge2 = edge1;
                    edge2.SetPosY(elevation1 + QuadMetrics.straightElevationFactor * QuadMetrics.elevationStep);
                    TriangulateEdgeStrip(edge2, weight2, index1, edge1, weight1, index2);
                    edge1 = new EdgeVertices(
                        QuadMetrics.SetVertixElevation(edge2.vertix1, elevation2),
                        QuadMetrics.SetVertixElevation(edge2.vertix5, elevation2 + QuadMetrics.elevationStep * (1 - 2f * QuadMetrics.leanElevationFactor))
                    );
                    TriangulateEdgeStrip(edge1, weight2, index2, edge2, weight2, index2);
                }
                else
                {
                    edge2 = edge1;
                    edge2.SetPosY(elevation1 + QuadMetrics.straightElevationFactor * QuadMetrics.elevationStep);
                    TriangulateEdgeStrip(edge1, weight1, index1, edge2, weight2, index2);
                    edge1 = new EdgeVertices(
                        QuadMetrics.SetVertixElevation(edge2.vertix1, elevation2),
                        QuadMetrics.SetVertixElevation(edge2.vertix5, elevation2 + QuadMetrics.elevationStep * (1 - 2f * QuadMetrics.leanElevationFactor))
                    );
                    TriangulateEdgeStrip(edge2, weight2, index2, edge1, weight2, index2);
                }
            }
        }
        else
        {
            if (!isOnSide)
            {
                edge2 = edge1;
                float neighborElevation = elevation2 + ((direction == slopeDirection.Opposite() ? 1 : 0) * QuadMetrics.elevationStep);

                if (Mathf.Round(elevation1 / QuadMetrics.elevationStep) != Mathf.Round(neighborElevation / QuadMetrics.elevationStep) + 1)
                {
                    edge2.SetPosY(neighborElevation + QuadMetrics.straightElevationFactor * QuadMetrics.elevationStep);
                    TriangulateEdgeStrip(edge1, weight1, index1, edge2, weight1, index1);
                    edge1.SetPosY(neighborElevation);
                    TriangulateEdgeStrip(edge2, weight1, index1, edge1, weight2, index2);
                    return;
                }

                edge1.SetPosY(neighborElevation);
                TriangulateEdgeStrip(edge2, weight1, index1, edge1, weight2, index2);
            }
            else
            {
                if (Mathf.Round(elevation1 / QuadMetrics.elevationStep) - 2 == Mathf.Round(elevation2 / QuadMetrics.elevationStep))
                {
                    if (direction == slopeDirection.Next2())
                    {
                        edge2 = edge1;
                        edge2 = new EdgeVertices(edge1.vertix1, QuadMetrics.SetVertixElevation(edge1.vertix5, elevation2 + QuadMetrics.elevationStep));
                        TriangulateEdgeTriangleSlopeSlope(edge1, edge2, weight1, weight1, weight1, index1, index1);
                        edge1 = new EdgeVertices(
                            QuadMetrics.SetVertixElevation(edge1.vertix1, elevation2 + QuadMetrics.elevationStep),
                            QuadMetrics.SetVertixElevation(edge1.vertix5, elevation2 + 2f * QuadMetrics.leanElevationFactor * QuadMetrics.elevationStep)
                        );
                        TriangulateEdgeStrip(edge2, weight1, index1, edge1, weight2, index2);
                    }
                    else
                    {                        
                        edge1.MirrorVerices();
                        edge2 = edge1;
                        edge2 = new EdgeVertices(edge1.vertix1, QuadMetrics.SetVertixElevation(edge1.vertix5, elevation2 + QuadMetrics.elevationStep));
                        TriangulateEdgeTriangleSlopeSlope(edge2, edge1, weight1, weight1, weight1, index1, index1);
                        edge1 = new EdgeVertices(
                            QuadMetrics.SetVertixElevation(edge1.vertix1, elevation2 + QuadMetrics.elevationStep),
                            QuadMetrics.SetVertixElevation(edge1.vertix5, elevation2 + 2f * QuadMetrics.leanElevationFactor * QuadMetrics.elevationStep)
                        );
                        TriangulateEdgeStrip(edge1, weight2, index1, edge2, weight1, index2);
                    }
                    return;
                }
                if (direction == slopeDirection.Next2())
                {
                    edge2 = new EdgeVertices(
                        QuadMetrics.SetVertixElevation(edge1.vertix1, elevation2 + (1 + QuadMetrics.straightElevationFactor) * QuadMetrics.elevationStep),
                        QuadMetrics.SetVertixElevation(edge1.vertix5, elevation2 + QuadMetrics.elevationStep)
                    );
                    TriangulateEdgeStrip(edge1, weight1, index1, edge2, weight1, index1);
                    edge1 = new EdgeVertices(
                        QuadMetrics.SetVertixElevation(edge1.vertix1, elevation2 + QuadMetrics.elevationStep),
                        QuadMetrics.SetVertixElevation(edge1.vertix5, elevation2 + 2f * QuadMetrics.leanElevationFactor * QuadMetrics.elevationStep)
                    );
                }
                else
                {
                    edge2 = new EdgeVertices(
                        QuadMetrics.SetVertixElevation(edge1.vertix1, elevation2 + QuadMetrics.elevationStep),
                        QuadMetrics.SetVertixElevation(edge1.vertix5, elevation2 + (1 + QuadMetrics.straightElevationFactor) * QuadMetrics.elevationStep)
                    );
                    TriangulateEdgeStrip(edge1, weight1, index1, edge2, weight1, index1);
                    edge1 = new EdgeVertices(
                        QuadMetrics.SetVertixElevation(edge1.vertix1, elevation2 + 2f * QuadMetrics.leanElevationFactor * QuadMetrics.elevationStep),
                        QuadMetrics.SetVertixElevation(edge1.vertix5, elevation2 + QuadMetrics.elevationStep)
                    );
                }
                TriangulateEdgeStrip(edge2, weight1, index1, edge1, weight2, index2);
            }
        }
    }

    private void TriangulateConnectionSlopeIntersection(
        QuadCell cell, QuadCell neighbor,
        EdgeVertices edge1, EdgeVertices edge2,
        QuadDirection direction, int elevationDifference
    )
    {
        edge2.vertix1.y -= elevationDifference * QuadMetrics.elevationStep * QuadMetrics.leanElevationFactor;
        edge2.vertix2.y -= elevationDifference * QuadMetrics.elevationStep * QuadMetrics.leanElevationFactor;
        edge2.vertix3.y += (neighbor.Position.y - cell.Position.y) * 0.5f;
        edge2.vertix4.y += elevationDifference * QuadMetrics.elevationStep * QuadMetrics.leanElevationFactor;
        edge2.vertix5.y += elevationDifference * QuadMetrics.elevationStep * QuadMetrics.leanElevationFactor;

        terrain.AddQuad(edge1.vertix1, edge1.vertix2, edge2.vertix1, edge2.vertix2);
        terrain.AddQuad(edge1.vertix2, edge1.vertix3, edge2.vertix2, edge2.vertix3);
        terrain.AddQuad(edge1.vertix3, edge1.vertix4, edge2.vertix3, edge2.vertix4);
        terrain.AddQuad(edge1.vertix4, edge1.vertix5, edge2.vertix4, edge2.vertix5);

        Vector4 indices;
        indices.x = indices.z = indices.w = cell.Index;
        indices.y = neighbor.Index;
        terrain.AddQuadCellData(indices, weights1, weights1, weights1, 0.75f * weights1 + 0.25f * weights2);
        terrain.AddQuadCellData(indices, weights1, weights1, 0.75f * weights1 + 0.25f * weights2, weights5);
        terrain.AddQuadCellData(indices, weights1, weights1, weights5, 0.75f * weights1 + 0.25f * weights2);
        terrain.AddQuadCellData(indices, weights1, weights1, 0.75f * weights1 + 0.25f * weights2, weights1);

        if (direction == QuadDirection.North || direction == QuadDirection.East)
        {
            edge1 = edge2;

            edge1.vertix1.y = neighbor.Position.y + (Math.Sign(-elevationDifference + 1) + elevationDifference * 2f * QuadMetrics.leanElevationFactor) * QuadMetrics.elevationStep;
            edge1.vertix2.y = neighbor.Position.y + (0.5f + elevationDifference * (-0.25f + 1.5f * QuadMetrics.leanElevationFactor)) * QuadMetrics.elevationStep;
            edge1.vertix3.y = edge2.vertix3.y;
            edge1.vertix4.y = neighbor.Position.y + (0.5f + elevationDifference * (0.25f - 1.5f * QuadMetrics.leanElevationFactor)) * QuadMetrics.elevationStep;
            edge1.vertix5.y = neighbor.Position.y + (Math.Sign(elevationDifference + 1) - elevationDifference * 2f * QuadMetrics.leanElevationFactor) * QuadMetrics.elevationStep;

            terrain.AddQuad(edge2.vertix1, edge2.vertix2, edge1.vertix1, edge1.vertix2);
            terrain.AddTriangle(edge2.vertix3, edge2.vertix2, edge1.vertix2);
            terrain.AddTriangle(edge2.vertix3, edge1.vertix4, edge2.vertix4);
            terrain.AddQuad(edge2.vertix4, edge2.vertix5, edge1.vertix4, edge1.vertix5);

            terrain.AddQuadCellData(indices, weights1, 0.75f * weights1 + 0.25f * weights2, weights2, 0.75f * weights2 + 0.25f * weights1);
            terrain.AddTriangleCellData(indices, weights5, 0.75f * weights1 + 0.25f * weights2, 0.75f * weights2 + 0.25f * weights1);
            terrain.AddTriangleCellData(indices, weights5, 0.75f * weights2 + 0.25f * weights1, 0.75f * weights1 + 0.25f * weights2);
            terrain.AddQuadCellData(indices, 0.75f * weights1 + 0.25f * weights2, weights1, 0.75f * weights2 + 0.25f * weights1, weights2);
        }
    }

    private void TriangulateCorner(
        QuadDirection direction, QuadType typeNeighbor,
        QuadCell cell, QuadCell neighbor, int elevationDifference,
        Vector3 vertix1, Vector3 vertix2)
    {
        QuadCell secondNeighbor = cell.GetNeighbor(direction.Next());
        if (secondNeighbor != null)
        {
            int additionalSlopeElevation = 0;
            if (cell.Slope && (cell.SlopeDirection == direction || cell.SlopeDirection == direction.Next2()))
                additionalSlopeElevation = 1;
            QuadDirection directionRight = direction.Next2();
            QuadType typeNeighborRight = cell.GetEdgeType(directionRight);
            QuadCell thirdNeighbor = cell.GetNeighbor(directionRight);
            Vector3 vertix3 = vertix1 + QuadMetrics.GetBridge(directionRight);
            Vector3 vertix4 = vertix2 + QuadMetrics.GetBridge(directionRight);

            int elevationSecondDifference = cell.GetCellSecondNeighborElevationDifference(direction);
            int elevationThirdDifference = cell.GetCellNeighborSlopeCornerElevationDifference(directionRight, true);

            if (cell.Slope && typeNeighborRight != QuadType.SlopeFlat && (directionRight == cell.SlopeDirection || directionRight.Opposite() == cell.SlopeDirection || elevationThirdDifference == 0))
            {
                vertix3.y = cell.Position.y + additionalSlopeElevation * QuadMetrics.elevationStep;
                if (elevationThirdDifference == 0)
                    vertix3.y += (thirdNeighbor.Position.y + (cell.Elevation - thirdNeighbor.Elevation) * QuadMetrics.elevationStep - cell.Position.y) * 0.5f;
            }
            else
            {
                if (elevationThirdDifference == 0)
                {
                    vertix3.y += (thirdNeighbor.Position.y + (cell.Elevation - thirdNeighbor.Elevation) * QuadMetrics.elevationStep - cell.Position.y) * 0.5f;
                }
                else
                {
                    vertix3.y += elevationThirdDifference * QuadMetrics.leanElevationFactor * QuadMetrics.elevationStep;
                }
            }
            int cellCornerElevation;

            if (typeNeighbor == QuadType.SlopeFlat || typeNeighborRight == QuadType.SlopeFlat ||
                (secondNeighbor.Slope && elevationSecondDifference == 0 && (secondNeighbor.GetEdgeType(direction.Opposite()) == QuadType.SlopeFlat || secondNeighbor.GetEdgeType(directionRight.Opposite()) == QuadType.SlopeFlat)))
            {
                cellCornerElevation = 0;
            }
            else
            {
                cellCornerElevation = QuadMetrics.GetCornerPointElevation(elevationDifference, elevationSecondDifference, elevationThirdDifference);
            }

            vertix4.y = cell.GetCentralPointPosition(elevationDifference, elevationSecondDifference, elevationThirdDifference, additionalSlopeElevation, direction)
                + cellCornerElevation * QuadMetrics.leanElevationFactor * QuadMetrics.elevationStep;

            terrain.AddQuad(vertix2, vertix1, vertix4, vertix3);
            terrain.AddQuadCellData(
                new Vector4(cell.Index, neighbor.Index, secondNeighbor.Index, thirdNeighbor.Index),
                QuadMetrics.GetEdgePointWeight(elevationDifference, weights1, weights2),
                    weights1,
                QuadMetrics.GetCentralPointWeight(elevationDifference, elevationSecondDifference, elevationThirdDifference, weights1, weights2, weights3, weights4),
                QuadMetrics.GetEdgePointWeight(elevationThirdDifference, weights1, weights4)
            );

            if (direction == QuadDirection.North || direction == QuadDirection.South)
            {
                if (elevationDifference != 0)
                {
                    int neighborCornerElevation;
                    int neighborCellElevationDifference = -elevationDifference;

                    int neighborSecondNeighborElevationDifference = neighbor.GetCellNeighborSlopeCornerElevationDifference(directionRight, false);

                    int neighborThirdneighborDifference = neighbor.GetCellSecondNeighborElevationDifference(directionRight);

                    if (neighbor.GetEdgeType(directionRight) == QuadType.SlopeFlat ||
                        (thirdNeighbor.Slope && neighborThirdneighborDifference == 0 &&
                        (thirdNeighbor.GetEdgeType(direction) == QuadType.SlopeFlat || typeNeighborRight == QuadType.SlopeFlat)))
                    {
                        neighborCornerElevation = 0;
                    }
                    else
                    {
                        neighborCornerElevation = QuadMetrics.GetCornerPointElevation(
                            neighborSecondNeighborElevationDifference,
                            neighborThirdneighborDifference,
                            neighborCellElevationDifference
                        );
                    }

                    int additionalNeighborSlopeElevation = 0;
                    if (neighbor.Slope && (direction.Opposite() == neighbor.SlopeDirection || direction.Next2() == neighbor.SlopeDirection))
                        additionalNeighborSlopeElevation = 1;

                    float centralPosition = neighbor.GetCentralPointPosition(neighborSecondNeighborElevationDifference,
                        neighborThirdneighborDifference, neighborCellElevationDifference, additionalNeighborSlopeElevation, directionRight);

                    if (elevationDifference == 1)
                    {
                        Vector3 vertix5 = QuadMetrics.SetVertixElevation(vertix4, centralPosition + neighborCornerElevation * QuadMetrics.leanElevationFactor * QuadMetrics.elevationStep);
                        Vector3 vertix6 = QuadMetrics.SetVertixElevation(vertix2, neighbor.Position.y + (additionalNeighborSlopeElevation -
                            (!neighbor.Slope ? 1f : direction.Next2() == neighbor.SlopeDirection ? 2f : 0) * QuadMetrics.leanElevationFactor) * QuadMetrics.elevationStep);

                        TriangulateCornerConnection(direction, directionRight, cell, neighbor, secondNeighbor, thirdNeighbor,
                            vertix2, vertix4, vertix5, vertix6);
                    }
                    else
                    {
                        Vector3 vertix5 = QuadMetrics.SetVertixElevation(vertix4, centralPosition + neighborCornerElevation * QuadMetrics.leanElevationFactor * QuadMetrics.elevationStep);
                        Vector3 vertix6 = QuadMetrics.SetVertixElevation(vertix2, neighbor.Position.y + (additionalNeighborSlopeElevation +
                             (!neighbor.Slope ? 1f : direction.Previous2() == neighbor.SlopeDirection ? 2f : 0) * QuadMetrics.leanElevationFactor) * QuadMetrics.elevationStep);

                        TriangulateCornerConnection(direction.Opposite(), directionRight, neighbor, cell, thirdNeighbor, secondNeighbor,
                            vertix5, vertix6, vertix2, vertix4, true);
                    }
                }

                if (elevationThirdDifference != 0)
                {
                    int neighborCornerElevation;
                    int thirdNeighborCellElevationDifference = -elevationThirdDifference;

                    int thirdNeighborNeighborDifference = thirdNeighbor.GetCellSecondNeighborElevationDifference(directionRight.Opposite());

                    int thirdNeighborSecondNeighborDifference = thirdNeighbor.GetCellNeighborSlopeCornerElevationDifference(direction, true);

                    if (thirdNeighbor.GetEdgeType(direction) == QuadType.SlopeFlat ||
                        (neighbor.Slope && thirdNeighborNeighborDifference == 0 &&
                        (typeNeighbor == QuadType.SlopeFlat || neighbor.GetEdgeType(directionRight) == QuadType.SlopeFlat)))
                    {
                        neighborCornerElevation = 0;
                    }
                    else
                    {
                        neighborCornerElevation = QuadMetrics.GetCornerPointElevation(
                            thirdNeighborCellElevationDifference,
                            thirdNeighborNeighborDifference,
                            thirdNeighborSecondNeighborDifference
                        );
                    }

                    int additionalThirdNeighborSlopeElevation = 0;
                    if (thirdNeighbor.Slope && (directionRight.Opposite() == thirdNeighbor.SlopeDirection || directionRight.Previous2() == thirdNeighbor.SlopeDirection))
                        additionalThirdNeighborSlopeElevation = 1;

                    float centralPosition = thirdNeighbor.GetCentralPointPosition(thirdNeighborCellElevationDifference, thirdNeighborNeighborDifference,
                        thirdNeighborSecondNeighborDifference, additionalThirdNeighborSlopeElevation, directionRight.Opposite());

                    if (elevationThirdDifference == 1)
                    {                        
                        Vector3 vertix5 = QuadMetrics.SetVertixElevation(vertix4, centralPosition + neighborCornerElevation * QuadMetrics.leanElevationFactor * QuadMetrics.elevationStep);
                        Vector3 vertix6 = QuadMetrics.SetVertixElevation(vertix3, thirdNeighbor.Position.y + (additionalThirdNeighborSlopeElevation -
                            (!thirdNeighbor.Slope ? 1f : directionRight.Previous2() == thirdNeighbor.SlopeDirection ? 2f : 0) * QuadMetrics.leanElevationFactor) * QuadMetrics.elevationStep);

                        TriangulateCornerConnection(directionRight, direction, cell, thirdNeighbor, secondNeighbor, neighbor,
                            vertix4, vertix3, vertix6, vertix5, true);
                    }
                    else
                    {
                        Vector3 vertix5 = QuadMetrics.SetVertixElevation(vertix4, centralPosition + neighborCornerElevation * QuadMetrics.leanElevationFactor * QuadMetrics.elevationStep);
                        Vector3 vertix6 = QuadMetrics.SetVertixElevation(vertix3, thirdNeighbor.Position.y + (additionalThirdNeighborSlopeElevation +
                            (!thirdNeighbor.Slope ? 1f : directionRight.Next2() == thirdNeighbor.SlopeDirection ? 2f : 0) * QuadMetrics.leanElevationFactor) * QuadMetrics.elevationStep);

                        TriangulateCornerConnection(directionRight.Opposite(), direction, thirdNeighbor, cell, neighbor, secondNeighbor,
                            vertix6, vertix5, vertix4, vertix3);
                    }
                }
            }
        }
    }

    private void TriangulateCornerConnection(
        QuadDirection direction, QuadDirection directionThirdNeighbor,
        QuadCell cell, QuadCell neighbor, QuadCell secondNeighbor, QuadCell thirdNeighbor,
        Vector3 vertix1, Vector3 vertix2, Vector3 vertix3, Vector3 vertix4, bool reverseVertices = false
    )
    {
        Color mixedWeight, neighborMixedWeight;
        int elevation1 = cell.Slope && (cell.SlopeDirection == direction || cell.SlopeDirection == directionThirdNeighbor) ? cell.Elevation + 1 : cell.Elevation;
        int elevation2 = neighbor.Slope && (neighbor.SlopeDirection == direction.Opposite() || neighbor.SlopeDirection == directionThirdNeighbor) ? neighbor.Elevation + 1 : neighbor.Elevation;
        int elevation3 = secondNeighbor.Slope && (secondNeighbor.SlopeDirection == direction.Opposite() || secondNeighbor.SlopeDirection == directionThirdNeighbor.Opposite()) ? secondNeighbor.Elevation + 1 : secondNeighbor.Elevation;
        int elevation4 = thirdNeighbor.Slope && (thirdNeighbor.SlopeDirection == direction || thirdNeighbor.SlopeDirection == directionThirdNeighbor.Opposite()) ? thirdNeighbor.Elevation + 1 : thirdNeighbor.Elevation;
        Vector4 indices = new Vector4(cell.Index, neighbor.Index, secondNeighbor.Index, thirdNeighbor.Index);

        if (!reverseVertices)
        {
            mixedWeight = QuadMetrics.GetCentralPointWeight(
                elevation1, elevation2, elevation3, elevation4, weights1, weights2, weights3, weights4
            );
            neighborMixedWeight = QuadMetrics.GetCentralPointWeight(
                elevation2, elevation3, elevation4, elevation1, weights2, weights3, weights4, weights1
            );
        }
        else
        {
            mixedWeight = QuadMetrics.GetCentralPointWeight(
                elevation1, elevation4, elevation3, elevation2, weights1, weights4, weights3, weights2
            );
            neighborMixedWeight = QuadMetrics.GetCentralPointWeight(
                elevation2, elevation1, elevation4, elevation3, weights2, weights1, weights4, weights3
            );
        }

        if (elevation2 - elevation1 == 1)
        {
            terrain.AddQuad(vertix3, vertix4, vertix2, vertix1);
            if (!reverseVertices)
            {
                terrain.AddQuadCellData(indices, neighborMixedWeight, weights2, mixedWeight, weights1);
            }
            else
            {
                terrain.AddQuadCellData(indices, weights2, neighborMixedWeight, weights1, mixedWeight);
            }
            return;
        }

        float bottomElevationValue, innerBottomElevation;
        if (elevation4 >= elevation2 || ((elevation1 >= elevation3 || elevation3 >= elevation2) && elevation4 <= elevation1))
        {
            bottomElevationValue = innerBottomElevation = cell.Position.y + (elevation1 - cell.Elevation + QuadMetrics.straightElevationFactor) * QuadMetrics.elevationStep;
            if (elevation1 == elevation3)
                bottomElevationValue += (secondNeighbor.Position.y + (cell.Elevation - secondNeighbor.Elevation) * QuadMetrics.elevationStep - cell.Position.y) * 0.5f;
            if (elevation2 <= elevation3 && elevation1 == elevation4)
                bottomElevationValue += (thirdNeighbor.Position.y + (cell.Elevation - thirdNeighbor.Elevation) * QuadMetrics.elevationStep - cell.Position.y) * 0.5f;
            if (cell.Slope && (cell.SlopeDirection == direction.Next2() || cell.SlopeDirection == direction.Previous2()))
            {
                innerBottomElevation += Math.Sign(-1 * (elevation1 - cell.Elevation) + 1) * QuadMetrics.leanElevationFactor * QuadMetrics.elevationStep;
            }

            if (!reverseVertices)
            {
                terrain.AddQuad(QuadMetrics.SetVertixElevation(vertix3, bottomElevationValue), QuadMetrics.SetVertixElevation(vertix4, innerBottomElevation), vertix2, vertix1);
                terrain.AddQuad(vertix3, vertix4, QuadMetrics.SetVertixElevation(vertix3, bottomElevationValue), QuadMetrics.SetVertixElevation(vertix4, innerBottomElevation));

                Color upperMixedWeight = QuadMetrics.GetCentralUpperPointWeight(
                    elevation1, elevation2, elevation3, elevation4, weights1, weights2, weights3, weights4
                );
                terrain.AddQuadCellData(indices, upperMixedWeight, weights2, mixedWeight, weights1);
                terrain.AddQuadCellData(indices, neighborMixedWeight, weights2, upperMixedWeight, weights2);
            }
            else
            {
                terrain.AddQuad(QuadMetrics.SetVertixElevation(vertix3, innerBottomElevation), QuadMetrics.SetVertixElevation(vertix4, bottomElevationValue), vertix2, vertix1);
                terrain.AddQuad(vertix3, vertix4, QuadMetrics.SetVertixElevation(vertix3, innerBottomElevation), QuadMetrics.SetVertixElevation(vertix4, bottomElevationValue));
                Color upperMixedWeight = QuadMetrics.GetCentralUpperPointWeight(
                    elevation1, elevation4, elevation3, elevation2, weights1, weights4, weights3, weights2
                );
                terrain.AddQuadCellData(indices, weights2, upperMixedWeight, weights1, mixedWeight);
                terrain.AddQuadCellData(indices, weights2, neighborMixedWeight, weights2, upperMixedWeight);
            }
            return;
        }
        else if (elevation1 < elevation4 && elevation4 < elevation2)
        {
            int cornerElevation;
            int elevationDifference1 = thirdNeighbor.GetCellNeighborSlopeCornerElevationDifference(directionThirdNeighbor.Opposite(), reverseVertices);
            int elevationDifference2 = thirdNeighbor.GetCellSecondNeighborElevationDifference(reverseVertices ? direction : directionThirdNeighbor.Opposite());
            int elevationDifference3 = thirdNeighbor.GetCellNeighborSlopeCornerElevationDifference(direction, !reverseVertices);
            if (thirdNeighbor.GetEdgeType(direction) == QuadType.SlopeFlat)
            {
                cornerElevation = 0;
            }
            else
            {
                cornerElevation = QuadMetrics.GetCornerPointElevation(elevationDifference1, elevationDifference2, elevationDifference3);
            }

            Color currentNeighborMixedWeight;
            if (elevation4 - elevation1 == 1)
            {
                innerBottomElevation = cell.Position.y + (elevation1 - cell.Elevation + QuadMetrics.straightElevationFactor) * QuadMetrics.elevationStep;
                if (cell.Slope && (cell.SlopeDirection == direction.Next2() || cell.SlopeDirection == direction.Previous2()))
                {
                    innerBottomElevation += Math.Sign(-1 * (elevation1 - cell.Elevation) + 1) * QuadMetrics.leanElevationFactor * QuadMetrics.elevationStep;
                }

                if (!reverseVertices)
                {
                    bottomElevationValue = thirdNeighbor.GetCentralPointPosition(elevationDifference1, elevationDifference2, elevationDifference3, (elevation4 - thirdNeighbor.Elevation), directionThirdNeighbor.Opposite())
                        + cornerElevation * QuadMetrics.leanElevationFactor * QuadMetrics.elevationStep;

                    terrain.AddQuad(QuadMetrics.SetVertixElevation(vertix3, bottomElevationValue), QuadMetrics.SetVertixElevation(vertix4, innerBottomElevation), vertix2, vertix1);
                    currentNeighborMixedWeight = QuadMetrics.GetCentralPointWeight(
                        elevationDifference1, elevationDifference2, elevationDifference3, weights4, weights1, weights2, weights3
                    );
                    terrain.AddQuadCellData(indices, currentNeighborMixedWeight, weights2, mixedWeight, weights1);
                }
                else
                {
                    bottomElevationValue = thirdNeighbor.GetCentralPointPosition(elevationDifference3, elevationDifference2, elevationDifference1, (elevation4 - thirdNeighbor.Elevation), direction)
                        + cornerElevation * QuadMetrics.leanElevationFactor * QuadMetrics.elevationStep;

                    terrain.AddQuad(QuadMetrics.SetVertixElevation(vertix3, innerBottomElevation), QuadMetrics.SetVertixElevation(vertix4, bottomElevationValue), vertix2, vertix1);
                    currentNeighborMixedWeight = QuadMetrics.GetCentralPointWeight(
                        elevationDifference3, elevationDifference2, elevationDifference1, weights4, weights3, weights2, weights1
                    );
                    terrain.AddQuadCellData(indices, weights2, currentNeighborMixedWeight, weights1, mixedWeight);
                }
            }
            else
            {
                innerBottomElevation = cell.Position.y + (elevation1 - cell.Elevation + QuadMetrics.straightElevationFactor) * QuadMetrics.elevationStep;
                if (cell.Slope && (cell.SlopeDirection == direction.Next2() || cell.SlopeDirection == direction.Previous2()))
                {
                    innerBottomElevation += Math.Sign(-1 * (elevation1 - cell.Elevation) + 1) * QuadMetrics.leanElevationFactor * QuadMetrics.elevationStep;
                }

                if (!reverseVertices)
                {
                    bottomElevationValue = thirdNeighbor.GetCentralPointPosition(elevationDifference1, elevationDifference2, elevationDifference3, (elevation4 - thirdNeighbor.Elevation), directionThirdNeighbor.Opposite())
                        + cornerElevation * QuadMetrics.leanElevationFactor * QuadMetrics.elevationStep;

                    terrain.AddQuad(QuadMetrics.SetVertixElevation(vertix3, innerBottomElevation), QuadMetrics.SetVertixElevation(vertix4, innerBottomElevation), vertix2, vertix1);
                    terrain.AddQuad(
                        QuadMetrics.SetVertixElevation(vertix3, bottomElevationValue),
                        QuadMetrics.SetVertixElevation(vertix4, bottomElevationValue),
                        QuadMetrics.SetVertixElevation(vertix3, innerBottomElevation),
                        QuadMetrics.SetVertixElevation(vertix4, innerBottomElevation)
                    );
                    Color upperMixedWeight = QuadMetrics.GetCentralUpperPointWeight(
                        elevation1, elevation2, elevation3, elevation4, weights1, weights2, weights3, weights4
                    );
                    currentNeighborMixedWeight = QuadMetrics.GetCentralPointWeight(
                        elevationDifference1, elevationDifference2, elevationDifference3, weights4, weights1, weights2, weights3
                    );

                    terrain.AddQuadCellData(indices, upperMixedWeight, weights2, mixedWeight, weights1);
                    terrain.AddQuadCellData(indices, currentNeighborMixedWeight, weights2, upperMixedWeight, weights2);
                }
                else
                {
                    bottomElevationValue = thirdNeighbor.GetCentralPointPosition(elevationDifference3, elevationDifference2, elevationDifference1, (elevation4 - thirdNeighbor.Elevation), direction)
                        + cornerElevation * QuadMetrics.leanElevationFactor * QuadMetrics.elevationStep;

                    terrain.AddQuad(QuadMetrics.SetVertixElevation(vertix3, innerBottomElevation), QuadMetrics.SetVertixElevation(vertix4, innerBottomElevation), vertix2, vertix1);
                    terrain.AddQuad(
                        QuadMetrics.SetVertixElevation(vertix3, bottomElevationValue),
                        QuadMetrics.SetVertixElevation(vertix4, bottomElevationValue),
                        QuadMetrics.SetVertixElevation(vertix3, innerBottomElevation),
                        QuadMetrics.SetVertixElevation(vertix4, innerBottomElevation)
                    );
                    Color upperMixedWeight = QuadMetrics.GetCentralUpperPointWeight(
                        elevation1, elevation4, elevation3, elevation2, weights1, weights4, weights3, weights2
                    );
                    currentNeighborMixedWeight = QuadMetrics.GetCentralPointWeight(
                        elevationDifference3, elevationDifference2, elevationDifference1, weights4, weights3, weights2, weights1
                    );

                    terrain.AddQuadCellData(indices, weights2, upperMixedWeight, weights1, mixedWeight);
                    terrain.AddQuadCellData(indices, weights2, currentNeighborMixedWeight, weights2, upperMixedWeight);
                }
                innerBottomElevation = bottomElevationValue;
            }

            if (elevation4 < elevation3 && elevation3 < elevation2)
            {
                elevationDifference1 = secondNeighbor.GetCellNeighborSlopeCornerElevationDifference(direction.Opposite(), reverseVertices);
                elevationDifference2 = secondNeighbor.GetCellSecondNeighborElevationDifference(reverseVertices ? directionThirdNeighbor.Opposite() : direction.Opposite());
                elevationDifference3 = secondNeighbor.GetCellNeighborSlopeCornerElevationDifference(directionThirdNeighbor.Opposite(), !reverseVertices);
                cornerElevation = QuadMetrics.GetCornerPointElevation(elevationDifference1, elevationDifference2, elevationDifference3);

                if (elevation3 - elevation4 == 1)
                {
                    float elevationHolder;
                    Color weightHolder;
                    if (!reverseVertices)
                    {
                        elevationHolder = secondNeighbor.GetCentralPointPosition(elevationDifference1, elevationDifference2, elevationDifference3, (elevation3 - secondNeighbor.Elevation), direction.Opposite())
                            + cornerElevation * QuadMetrics.leanElevationFactor * QuadMetrics.elevationStep;

                        terrain.AddQuad(
                            QuadMetrics.SetVertixElevation(vertix3, elevationHolder),
                            QuadMetrics.SetVertixElevation(vertix4, thirdNeighbor.Position.y + (elevation4 - thirdNeighbor.Elevation + QuadMetrics.straightElevationFactor) * QuadMetrics.elevationStep),
                            QuadMetrics.SetVertixElevation(vertix3, bottomElevationValue),
                            QuadMetrics.SetVertixElevation(vertix4, innerBottomElevation)
                        );
                        weightHolder = QuadMetrics.GetCentralPointWeight(
                            elevationDifference1, elevationDifference2, elevationDifference3, weights3, weights4, weights1, weights2 
                        );
                        terrain.AddQuadCellData(indices, weightHolder, weights2, currentNeighborMixedWeight, weights2);
                    }
                    else
                    {
                        elevationHolder = secondNeighbor.GetCentralPointPosition(elevationDifference3, elevationDifference2, elevationDifference1, (elevation3 - secondNeighbor.Elevation), directionThirdNeighbor.Opposite())
                            + cornerElevation * QuadMetrics.leanElevationFactor * QuadMetrics.elevationStep;

                        terrain.AddQuad(
                            QuadMetrics.SetVertixElevation(vertix3, thirdNeighbor.Position.y + (elevation4 - thirdNeighbor.Elevation + QuadMetrics.straightElevationFactor) * QuadMetrics.elevationStep),
                            QuadMetrics.SetVertixElevation(vertix4, elevationHolder),
                            QuadMetrics.SetVertixElevation(vertix3, innerBottomElevation),
                            QuadMetrics.SetVertixElevation(vertix4, bottomElevationValue)
                        );
                        weightHolder = QuadMetrics.GetCentralPointWeight(
                            elevationDifference3, elevationDifference2, elevationDifference1, weights3, weights2, weights1, weights4
                        );
                        terrain.AddQuadCellData(indices, weights2, weightHolder, weights2, currentNeighborMixedWeight);
                    }

                    innerBottomElevation = thirdNeighbor.Position.y + (elevation4 - thirdNeighbor.Elevation + QuadMetrics.straightElevationFactor) * QuadMetrics.elevationStep;
                    bottomElevationValue = elevationHolder;
                    currentNeighborMixedWeight = weightHolder;
                }
                else
                {
                    float elevationHolder;
                    if (!reverseVertices)
                    {
                        elevationHolder = thirdNeighbor.Position.y + (elevation4 - thirdNeighbor.Elevation + QuadMetrics.straightElevationFactor) * QuadMetrics.elevationStep;

                        terrain.AddQuad(
                            QuadMetrics.SetVertixElevation(vertix3, elevationHolder),
                            QuadMetrics.SetVertixElevation(vertix4, elevationHolder),
                            QuadMetrics.SetVertixElevation(vertix3, bottomElevationValue),
                            QuadMetrics.SetVertixElevation(vertix4, innerBottomElevation)
                        );
                        bottomElevationValue = innerBottomElevation = secondNeighbor.GetCentralPointPosition(elevationDifference1, elevationDifference2, elevationDifference3, (elevation3 - secondNeighbor.Elevation), direction.Opposite())
                            + cornerElevation * QuadMetrics.leanElevationFactor * QuadMetrics.elevationStep;
                        terrain.AddQuad(
                            QuadMetrics.SetVertixElevation(vertix3, bottomElevationValue),
                            QuadMetrics.SetVertixElevation(vertix4, bottomElevationValue),
                            QuadMetrics.SetVertixElevation(vertix3, elevationHolder),
                            QuadMetrics.SetVertixElevation(vertix4, elevationHolder)
                        );

                        Color upperMixedWeight = QuadMetrics.GetCentralUpperPointWeight(
                            elevation4, elevation1, elevation2, elevation3, weights4, weights1, weights2, weights3
                        );
                        currentNeighborMixedWeight = QuadMetrics.GetCentralPointWeight(
                            elevationDifference1, elevationDifference2, elevationDifference3, weights3, weights4, weights1, weights2
                        );
                        terrain.AddQuadCellData(indices, upperMixedWeight, weights2, currentNeighborMixedWeight, weights2);
                        terrain.AddQuadCellData(indices, currentNeighborMixedWeight, weights2, upperMixedWeight, weights2);
                    }
                    else
                    {
                        elevationHolder = thirdNeighbor.Position.y + (elevation4 - thirdNeighbor.Elevation + QuadMetrics.straightElevationFactor) * QuadMetrics.elevationStep;

                        terrain.AddQuad(
                            QuadMetrics.SetVertixElevation(vertix3, elevationHolder),
                            QuadMetrics.SetVertixElevation(vertix4, elevationHolder),
                            QuadMetrics.SetVertixElevation(vertix3, innerBottomElevation),
                            QuadMetrics.SetVertixElevation(vertix4, bottomElevationValue)
                        );
                        bottomElevationValue = innerBottomElevation = secondNeighbor.GetCentralPointPosition(elevationDifference3, elevationDifference2, elevationDifference1, (elevation3 - secondNeighbor.Elevation), directionThirdNeighbor.Opposite())
                            + cornerElevation * QuadMetrics.leanElevationFactor * QuadMetrics.elevationStep;
                        terrain.AddQuad(
                            QuadMetrics.SetVertixElevation(vertix3, bottomElevationValue),
                            QuadMetrics.SetVertixElevation(vertix4, bottomElevationValue),
                            QuadMetrics.SetVertixElevation(vertix3, elevationHolder),
                            QuadMetrics.SetVertixElevation(vertix4, elevationHolder)
                        );

                        Color upperMixedColor = QuadMetrics.GetCentralUpperPointWeight(
                            elevation4, elevation3, elevation2, elevation1, weights4, weights3, weights2, weights1
                        );
                        currentNeighborMixedWeight = QuadMetrics.GetCentralPointWeight(
                            elevationDifference3, elevationDifference2, elevationDifference1, weights3, weights2, weights1, weights4
                        );
                        terrain.AddQuadCellData(indices, weights2, upperMixedColor, weights2, currentNeighborMixedWeight);
                        terrain.AddQuadCellData(indices, weights2, currentNeighborMixedWeight, weights2, upperMixedColor);
                    }
                }

                if (elevation2 - elevation3 == 1)
                {
                    if (!reverseVertices)
                    {
                        terrain.AddQuad(vertix3, vertix4, QuadMetrics.SetVertixElevation(vertix3, bottomElevationValue), QuadMetrics.SetVertixElevation(vertix4, innerBottomElevation));
                        terrain.AddQuadCellData(indices, neighborMixedWeight, weights2, currentNeighborMixedWeight, weights2);
                    }
                    else
                    {
                        terrain.AddQuad(vertix3, vertix4, QuadMetrics.SetVertixElevation(vertix3, innerBottomElevation), QuadMetrics.SetVertixElevation(vertix4, bottomElevationValue));
                        terrain.AddQuadCellData(indices, weights2, neighborMixedWeight, weights2, currentNeighborMixedWeight);
                    }
                }
                else
                {
                    float elevationHolder = secondNeighbor.Position.y + (elevation3 - secondNeighbor.Elevation + QuadMetrics.straightElevationFactor) * QuadMetrics.elevationStep;
                    if (!reverseVertices)
                    {
                        terrain.AddQuad(
                            QuadMetrics.SetVertixElevation(vertix3, elevationHolder),
                            QuadMetrics.SetVertixElevation(vertix4, elevationHolder),
                            QuadMetrics.SetVertixElevation(vertix3, bottomElevationValue),
                            QuadMetrics.SetVertixElevation(vertix4, innerBottomElevation)
                        );
                        terrain.AddQuad(vertix3, vertix4, QuadMetrics.SetVertixElevation(vertix3, elevationHolder), QuadMetrics.SetVertixElevation(vertix4, elevationHolder));

                        Color upperMixedWeight = QuadMetrics.GetCentralUpperPointWeight(
                            elevation3, elevation4, elevation1, elevation2, weights3, weights4, weights1, weights2
                        );
                        terrain.AddQuadCellData(indices, upperMixedWeight, weights2, currentNeighborMixedWeight, weights2);
                        terrain.AddQuadCellData(indices, neighborMixedWeight, weights2, upperMixedWeight, weights2);
                    }
                    else
                    {
                        terrain.AddQuad(
                            QuadMetrics.SetVertixElevation(vertix3, elevationHolder),
                            QuadMetrics.SetVertixElevation(vertix4, elevationHolder),
                            QuadMetrics.SetVertixElevation(vertix3, innerBottomElevation),
                            QuadMetrics.SetVertixElevation(vertix4, bottomElevationValue)
                        );
                        terrain.AddQuad(vertix3, vertix4, QuadMetrics.SetVertixElevation(vertix3, elevationHolder), QuadMetrics.SetVertixElevation(vertix4, elevationHolder));

                        Color upperMixedWeight = QuadMetrics.GetCentralUpperPointWeight(
                            elevation3, elevation2, elevation1, elevation4, weights3, weights2, weights1, weights4
                        );
                        terrain.AddQuadCellData(indices, weights2, upperMixedWeight, weights2, currentNeighborMixedWeight);
                        terrain.AddQuadCellData(indices, weights2, neighborMixedWeight, weights2, upperMixedWeight);
                    }
                }
            }
            else
            {
                if (elevation2 - elevation4 == 1)
                {
                    if (!reverseVertices)
                    {
                        terrain.AddQuad(vertix3, vertix4, QuadMetrics.SetVertixElevation(vertix3, bottomElevationValue), QuadMetrics.SetVertixElevation(vertix4, innerBottomElevation));
                        terrain.AddQuadCellData(indices, neighborMixedWeight, weights2, currentNeighborMixedWeight, weights2);
                    }
                    else
                    {
                        terrain.AddQuad(vertix3, vertix4, QuadMetrics.SetVertixElevation(vertix3, innerBottomElevation), QuadMetrics.SetVertixElevation(vertix4, bottomElevationValue));
                        terrain.AddQuadCellData(indices, weights2, neighborMixedWeight, weights2, currentNeighborMixedWeight);
                    }
                }
                else
                {
                    float elevationHolder1, elevationHolder2;
                    elevationHolder1 = elevationHolder2 = thirdNeighbor.Position.y + (elevation4 - thirdNeighbor.Elevation + QuadMetrics.straightElevationFactor) * QuadMetrics.elevationStep;
                    if (elevation3 == elevation4)
                        elevationHolder1 += (secondNeighbor.Position.y + (thirdNeighbor.Elevation - secondNeighbor.Elevation) * QuadMetrics.elevationStep - thirdNeighbor.Position.y) * 0.5f;

                    if (!reverseVertices)
                    {
                        terrain.AddQuad(
                            QuadMetrics.SetVertixElevation(vertix3, elevationHolder1),
                            QuadMetrics.SetVertixElevation(vertix4, elevationHolder2),
                            QuadMetrics.SetVertixElevation(vertix3, bottomElevationValue),
                            QuadMetrics.SetVertixElevation(vertix4, innerBottomElevation)
                        );
                        terrain.AddQuad(vertix3, vertix4, QuadMetrics.SetVertixElevation(vertix3, elevationHolder1), QuadMetrics.SetVertixElevation(vertix4, elevationHolder2));

                        Color upperMixedWeight = QuadMetrics.GetCentralUpperPointWeight(
                            elevation4, elevation1, elevation2, elevation3, weights4, weights1, weights2, weights3
                        );
                        terrain.AddQuadCellData(indices, upperMixedWeight, weights2, currentNeighborMixedWeight, weights2);
                        terrain.AddQuadCellData(indices, neighborMixedWeight, weights2, upperMixedWeight, weights2);
                    }
                    else
                    {
                        terrain.AddQuad(
                            QuadMetrics.SetVertixElevation(vertix3, elevationHolder2),
                            QuadMetrics.SetVertixElevation(vertix4, elevationHolder1),
                            QuadMetrics.SetVertixElevation(vertix3, innerBottomElevation),
                            QuadMetrics.SetVertixElevation(vertix4, bottomElevationValue)
                        );
                        terrain.AddQuad(vertix3, vertix4, QuadMetrics.SetVertixElevation(vertix3, elevationHolder2), QuadMetrics.SetVertixElevation(vertix4, elevationHolder1));

                        Color upperMixedWeight = QuadMetrics.GetCentralUpperPointWeight(
                            elevation4, elevation3, elevation2, elevation1, weights4, weights3, weights2, weights1
                        );
                        terrain.AddQuadCellData(indices, weights2, upperMixedWeight, weights2, currentNeighborMixedWeight);
                        terrain.AddQuadCellData(indices, weights2, neighborMixedWeight, weights2, upperMixedWeight);
                    }
                }
            }
        }
        else
        {
            int elevationDifference1 = secondNeighbor.GetCellNeighborSlopeCornerElevationDifference(direction.Opposite(), reverseVertices);
            int elevationDifference2 = secondNeighbor.GetCellSecondNeighborElevationDifference(reverseVertices ? directionThirdNeighbor.Opposite() : direction.Opposite());
            int elevationDifference3 = secondNeighbor.GetCellNeighborSlopeCornerElevationDifference(directionThirdNeighbor.Opposite(), !reverseVertices);
            int cornerElevation = QuadMetrics.GetCornerPointElevation(elevationDifference1, elevationDifference2, elevationDifference3);

            Color secondNeighborMixedWeight;
            if (elevation3 - elevation1 == 1)
            {
                innerBottomElevation = cell.Position.y + (elevation1 - cell.Elevation + QuadMetrics.straightElevationFactor) * QuadMetrics.elevationStep;
                if (cell.Slope && (cell.SlopeDirection == direction.Next2() || cell.SlopeDirection == direction.Previous2()))
                {
                    innerBottomElevation += Math.Sign(-1 * (elevation1 - cell.Elevation) + 1) * QuadMetrics.leanElevationFactor * QuadMetrics.elevationStep;
                }

                if (!reverseVertices)
                {
                    bottomElevationValue = secondNeighbor.GetCentralPointPosition(elevationDifference1, elevationDifference2, elevationDifference3, (elevation3 - secondNeighbor.Elevation), direction.Opposite())
                        + cornerElevation * QuadMetrics.leanElevationFactor * QuadMetrics.elevationStep;

                    terrain.AddQuad(QuadMetrics.SetVertixElevation(vertix3, bottomElevationValue), QuadMetrics.SetVertixElevation(vertix4, innerBottomElevation), vertix2, vertix1);
                    secondNeighborMixedWeight = QuadMetrics.GetCentralPointWeight(
                        elevationDifference1, elevationDifference2, elevationDifference3, weights3, weights4, weights1, weights2
                    );
                    terrain.AddQuadCellData(indices, secondNeighborMixedWeight, weights2, mixedWeight, weights1);
                }
                else
                {
                    bottomElevationValue = secondNeighbor.GetCentralPointPosition(elevationDifference3, elevationDifference2, elevationDifference1, (elevation3 - secondNeighbor.Elevation), directionThirdNeighbor.Opposite())
                        + cornerElevation * QuadMetrics.leanElevationFactor * QuadMetrics.elevationStep;

                    terrain.AddQuad(QuadMetrics.SetVertixElevation(vertix3, innerBottomElevation), QuadMetrics.SetVertixElevation(vertix4, bottomElevationValue), vertix2, vertix1);
                    secondNeighborMixedWeight = QuadMetrics.GetCentralPointWeight(
                        elevationDifference3, elevationDifference2, elevationDifference1, weights3, weights2, weights1, weights4
                    );
                    terrain.AddQuadCellData(indices, weights2, secondNeighborMixedWeight, weights1, mixedWeight);
                }
            }
            else
            {
                float outerBottomElevation;
                innerBottomElevation = outerBottomElevation = cell.Position.y + (elevation1 - cell.Elevation + QuadMetrics.straightElevationFactor) * QuadMetrics.elevationStep;
                if (cell.Slope && (cell.SlopeDirection == direction.Next2() || cell.SlopeDirection == direction.Previous2()))
                {
                    innerBottomElevation += Math.Sign(-1 * (elevation1 - cell.Elevation) + 1) * QuadMetrics.leanElevationFactor * QuadMetrics.elevationStep;
                }

                if (elevation1 == elevation4)
                    outerBottomElevation += (thirdNeighbor.Position.y + (cell.Elevation - thirdNeighbor.Elevation) * QuadMetrics.elevationStep - cell.Position.y) * 0.5f;

                if (!reverseVertices)
                {
                    bottomElevationValue = secondNeighbor.GetCentralPointPosition(elevationDifference1, elevationDifference2, elevationDifference3, (elevation3 - secondNeighbor.Elevation), direction.Opposite())
                        + cornerElevation * QuadMetrics.leanElevationFactor * QuadMetrics.elevationStep;

                    terrain.AddQuad(QuadMetrics.SetVertixElevation(vertix3, outerBottomElevation), QuadMetrics.SetVertixElevation(vertix4, innerBottomElevation), vertix2, vertix1);
                    terrain.AddQuad(
                        QuadMetrics.SetVertixElevation(vertix3, bottomElevationValue),
                        QuadMetrics.SetVertixElevation(vertix4, bottomElevationValue),
                        QuadMetrics.SetVertixElevation(vertix3, outerBottomElevation),
                        QuadMetrics.SetVertixElevation(vertix4, innerBottomElevation)
                    );

                    Color upperMixedWeight = QuadMetrics.GetCentralUpperPointWeight(
                        elevation1, elevation2, elevation3, elevation4, weights1, weights2, weights3, weights4
                    );
                    secondNeighborMixedWeight = QuadMetrics.GetCentralPointWeight(
                        elevationDifference1, elevationDifference2, elevationDifference3, weights3, weights4, weights1, weights2
                    );
                    terrain.AddQuadCellData(indices, upperMixedWeight, weights2, mixedWeight, weights1);
                    terrain.AddQuadCellData(indices, secondNeighborMixedWeight, weights2, upperMixedWeight, weights2);
                }
                else
                {
                    bottomElevationValue = secondNeighbor.GetCentralPointPosition(elevationDifference3, elevationDifference2, elevationDifference1, (elevation3 - secondNeighbor.Elevation), directionThirdNeighbor.Opposite())
                        + cornerElevation * QuadMetrics.leanElevationFactor * QuadMetrics.elevationStep;

                    terrain.AddQuad(QuadMetrics.SetVertixElevation(vertix3, innerBottomElevation), QuadMetrics.SetVertixElevation(vertix4, outerBottomElevation), vertix2, vertix1);
                    terrain.AddQuad(
                        QuadMetrics.SetVertixElevation(vertix3, bottomElevationValue),
                        QuadMetrics.SetVertixElevation(vertix4, bottomElevationValue),
                        QuadMetrics.SetVertixElevation(vertix3, innerBottomElevation),
                        QuadMetrics.SetVertixElevation(vertix4, outerBottomElevation)
                    );

                    Color upperMixedColor = QuadMetrics.GetCentralUpperPointWeight(
                        elevation1, elevation4, elevation3, elevation2, weights1, weights4, weights3, weights2
                    );
                    secondNeighborMixedWeight = QuadMetrics.GetCentralPointWeight(
                        elevationDifference3, elevationDifference2, elevationDifference1, weights3, weights2, weights1, weights4
                    );
                    terrain.AddQuadCellData(indices, weights2, upperMixedColor, weights1, mixedWeight);
                    terrain.AddQuadCellData(indices, weights2, secondNeighborMixedWeight, weights2, upperMixedColor);
                }
                innerBottomElevation = bottomElevationValue;
            }

            if (elevation2 - elevation3 == 1)
            {
                if (!reverseVertices)
                {
                    terrain.AddQuad(vertix3, vertix4, QuadMetrics.SetVertixElevation(vertix3, bottomElevationValue), QuadMetrics.SetVertixElevation(vertix4, innerBottomElevation));
                    terrain.AddQuadCellData(indices, neighborMixedWeight, weights2, secondNeighborMixedWeight, weights2);
                }
                else
                {
                    terrain.AddQuad(vertix3, vertix4, QuadMetrics.SetVertixElevation(vertix3, innerBottomElevation), QuadMetrics.SetVertixElevation(vertix4, bottomElevationValue));
                    terrain.AddQuadCellData(indices, weights2, neighborMixedWeight, weights2, secondNeighborMixedWeight);
                }
            }
            else
            {
                float elevationHolder = secondNeighbor.Position.y + (elevation3 - secondNeighbor.Elevation + QuadMetrics.straightElevationFactor) * QuadMetrics.elevationStep;
                if (!reverseVertices)
                {
                    terrain.AddQuad(
                        QuadMetrics.SetVertixElevation(vertix3, elevationHolder),
                        QuadMetrics.SetVertixElevation(vertix4, elevationHolder),
                        QuadMetrics.SetVertixElevation(vertix3, bottomElevationValue),
                        QuadMetrics.SetVertixElevation(vertix4, innerBottomElevation)
                    );
                    terrain.AddQuad(vertix3, vertix4, QuadMetrics.SetVertixElevation(vertix3, elevationHolder), QuadMetrics.SetVertixElevation(vertix4, elevationHolder));

                    Color upperMixedWeight = QuadMetrics.GetCentralUpperPointWeight(
                        elevation3, elevation4, elevation1, elevation2, weights3, weights4, weights1, weights2
                    );
                    terrain.AddQuadCellData(indices, upperMixedWeight, weights2, secondNeighborMixedWeight, weights2);
                    terrain.AddQuadCellData(indices, neighborMixedWeight, weights2, upperMixedWeight, weights2);
                }
                else
                {
                    terrain.AddQuad(
                        QuadMetrics.SetVertixElevation(vertix3, elevationHolder),
                        QuadMetrics.SetVertixElevation(vertix4, elevationHolder),
                        QuadMetrics.SetVertixElevation(vertix3, innerBottomElevation),
                        QuadMetrics.SetVertixElevation(vertix4, bottomElevationValue)
                    );
                    terrain.AddQuad(vertix3, vertix4, QuadMetrics.SetVertixElevation(vertix3, elevationHolder), QuadMetrics.SetVertixElevation(vertix4, elevationHolder));

                    Color upperMixedWeight = QuadMetrics.GetCentralUpperPointWeight(
                        elevation3, elevation2, elevation1, elevation4, weights3, weights2, weights1, weights4
                     );
                    terrain.AddQuadCellData(indices, weights2, upperMixedWeight, weights2, secondNeighborMixedWeight);
                    terrain.AddQuadCellData(indices, weights2, neighborMixedWeight, weights2, upperMixedWeight);
                }
            }
        }
    }

    private void TriangulateEdgeFan(Vector3 center, EdgeVertices edge, float index)
    {
        terrain.AddTriangle(center, edge.vertix1, edge.vertix2);
        terrain.AddTriangle(center, edge.vertix2, edge.vertix3);
        terrain.AddTriangle(center, edge.vertix3, edge.vertix4);
        terrain.AddTriangle(center, edge.vertix4, edge.vertix5);

        Vector4 indices;
        indices.x = indices.y = indices.z = indices.w = index;
        terrain.AddTriangleCellData(indices, weights1);
        terrain.AddTriangleCellData(indices, weights1);
        terrain.AddTriangleCellData(indices, weights1);
        terrain.AddTriangleCellData(indices, weights1);
    }

    private void TriangulateEdgeTriangleSlopeCliff(
        EdgeVertices edge1, Color weight1, float index1,
        EdgeVertices edge2, Color weight2, float index2
    )
    {
        terrain.AddTriangle(edge1.vertix2, edge2.vertix3, edge1.vertix3);
        terrain.AddQuad(edge1.vertix3, edge1.vertix4, edge2.vertix3, edge2.vertix4);
        terrain.AddQuad(edge1.vertix4, edge1.vertix5, edge2.vertix4, edge2.vertix5);

        Vector4 indices;
        indices.x = indices.z = indices.w = index1;
        indices.y = index2;
        terrain.AddTriangleCellData(indices, weights5, weights5 * (2f / 3f) + weight2 * (1f / 3f), weights5 * (2f / 3f) + weight1 * (1f / 3f));
        terrain.AddQuadCellData(indices,
            weights5 * (2f / 3f) + weight1 * (1f / 3f),
            weights5 * (1f / 3f) + weight1 * (2f / 3f),
            weights5 * (2f / 3f) + weight2 * (1f / 3f),
            weights5 * (1f / 3f) + weight2 * (2f / 3f)
        );
        terrain.AddQuadCellData(indices, weights5 * (1f / 3f) + weight1 * (2f / 3f), weight1, weights5 * (1f / 3f) + weight2 * (2f / 3f), weight2);
    }

    private void TriangulateEdgeTriangleSlopeSlope(
        EdgeVertices edge1, EdgeVertices edge2, 
        Color weight1, Color weight2, Color weight3,
        float index1, float index2
    )
    {
        terrain.AddTriangle(edge1.vertix1, edge2.vertix2, edge1.vertix2);
        terrain.AddQuad(edge1.vertix2, edge1.vertix3, edge2.vertix2, edge2.vertix3);
        terrain.AddQuad(edge1.vertix3, edge1.vertix4, edge2.vertix3, edge2.vertix4);
        terrain.AddQuad(edge1.vertix4, edge1.vertix5, edge2.vertix4, edge2.vertix5);

        Vector4 indices;
        indices.x = indices.z = indices.w = index1;
        indices.y = index2;
        terrain.AddTriangleCellData(indices, weight1, weight1 * 0.75f + weight3 * 0.25f, weight1 * 0.75f + weight2 * 0.25f);
        terrain.AddQuadCellData(indices, 
            weight1 * 0.75f + weight2 * 0.25f, 
            (weight1 + weight2) * 0.5f,
            weight1 * 0.75f + weight3 * 0.25f, 
            (weight1 + weight3) * 0.5f
        );
        terrain.AddQuadCellData(indices,
            (weight1 + weight2) * 0.5f,
            weight1 * 0.25f + weight2 * 0.75f,
            (weight1 + weight3) * 0.5f,
            weight1 * 0.25f + weight3 * 0.75f
        );
        terrain.AddQuadCellData(indices, weight1 * 0.25f + weight2 * 0.75f, weight2, weight1 * 0.25f + weight3 * 0.75f, weight3);
    }

    private void TriangulateEdgeStrip(
        EdgeVertices edge1, Color weight1, float index1, 
        EdgeVertices edge2, Color weight2, float index2
    )
    {
        terrain.AddQuad(edge1.vertix1, edge1.vertix2, edge2.vertix1, edge2.vertix2);       
        terrain.AddQuad(edge1.vertix2, edge1.vertix3, edge2.vertix2, edge2.vertix3);        
        terrain.AddQuad(edge1.vertix3, edge1.vertix4, edge2.vertix3, edge2.vertix4);        
        terrain.AddQuad(edge1.vertix4, edge1.vertix5, edge2.vertix4, edge2.vertix5);       

        Vector4 indices;
        indices.x = indices.z = indices.w = index1;
        indices.y = index2;
        terrain.AddQuadCellData(indices, weight1, weight2);
        terrain.AddQuadCellData(indices, weight1, weight2);
        terrain.AddQuadCellData(indices, weight1, weight2);
        terrain.AddQuadCellData(indices, weight1, weight2);
    }

    private void TriangulateIncompleteLeftEdgeStrip(EdgeVertices edge1, EdgeVertices edge2, Color weight1, Color weight2, float index1, float index2)
    {
        terrain.AddQuad(edge1.vertix1, edge1.vertix2, edge2.vertix1, edge2.vertix2);
        terrain.AddQuad(edge1.vertix2, edge1.vertix3, edge2.vertix2, edge2.vertix3);
        terrain.AddQuad(edge1.vertix3, edge1.vertix4, edge2.vertix3, edge2.vertix4);
        terrain.AddQuad(edge1.vertix4, edge1.vertix5, edge2.vertix4, edge2.vertix5);

        Vector4 indices;
        indices.x = indices.z = indices.w = index1;
        indices.y = index2;
        terrain.AddQuadCellData(indices, weights1, weights1, weight2, weight1);
        terrain.AddQuadCellData(indices, weights1, weights1, weight1, weights1 * 1f / 3f + weight1 * 2f / 3f);
        terrain.AddQuadCellData(indices, weights1, weights1, weights1 * 1f / 3f + weight1 * 2f / 3f, weights1 * 2f / 3f + weight1 * 1f / 3f);
        terrain.AddQuadCellData(indices, weights1, weights1, weights1 * 2f / 3f + weight1 * 1f / 3f, weights1);
    }

    private void TriangulateIncompleteRightEdgeStrip(EdgeVertices edge1, EdgeVertices edge2, Color weight1, Color weight2, float index1, float index2)
    {
        terrain.AddQuad(edge1.vertix1, edge1.vertix2, edge2.vertix1, edge2.vertix2);
        terrain.AddQuad(edge1.vertix2, edge1.vertix3, edge2.vertix2, edge2.vertix3);
        terrain.AddQuad(edge1.vertix3, edge1.vertix4, edge2.vertix3, edge2.vertix4);
        terrain.AddQuad(edge1.vertix4, edge1.vertix5, edge2.vertix4, edge2.vertix5);

        Vector4 indices;
        indices.x = indices.z = indices.w = index1;
        indices.y = index2;
        terrain.AddQuadCellData(indices, weights1, weights1, weights1, weights1 * 2f / 3f + weight1 * 1f / 3f);
        terrain.AddQuadCellData(indices, weights1, weights1, weights1 * 2f / 3f + weight1 * 1f / 3f, weights1 * 1f / 3f + weight1 * 2f / 3f);
        terrain.AddQuadCellData(indices, weights1, weights1, weights1 * 1f / 3f + weight1 * 2f / 3f, weight1);
        terrain.AddQuadCellData(indices, weights1, weights1, weight1, weight2);
    }

    private void TriangulateEdgeStripVSlopeSlope(
        EdgeVertices edge1, Color weight1, float index1, 
        EdgeVertices edge2, Color weight2, float index2
    )
    {
        terrain.AddQuad(edge1.vertix1, edge1.vertix2, edge2.vertix1, edge2.vertix2);
        terrain.AddQuad(edge1.vertix2, edge1.vertix3, edge2.vertix2, edge2.vertix3);
        terrain.AddQuad(edge1.vertix3, edge1.vertix4, edge2.vertix3, edge2.vertix4);
        terrain.AddQuad(edge1.vertix4, edge1.vertix5, edge2.vertix4, edge2.vertix5);

        Vector4 indices;
        indices.x = indices.z = indices.w = index1;
        indices.y = index2;
        terrain.AddQuadCellData(indices, 
            (weight1 + weights1) / 2f, 0.75f * weights1 + 0.25f * weight1, 
            (weight2 + weights1) / 2f, 0.75f * weights1 + 0.25f * weight2
        );
        terrain.AddQuadCellData(indices, 0.75f * weights1 + 0.25f * weight1, weights1, 0.75f * weights1 + 0.25f * weight2, weights1);
        terrain.AddQuadCellData(indices, weights1);
        terrain.AddQuadCellData(indices, weights1);
    }

    private void TriangulateVSlopeSlopeConnection(
        EdgeVertices edge1, Color weight1, float index1,
        EdgeVertices edge2, Color weight2, float index2
    )
    {
        terrain.AddQuad(edge1.vertix4, edge1.vertix5, edge2.vertix4, edge2.vertix5);
        terrain.AddQuad(edge1.vertix3, edge1.vertix4, edge2.vertix3, edge2.vertix4);
        terrain.AddQuad(edge1.vertix2, edge1.vertix3, edge2.vertix2, edge2.vertix3);
        terrain.AddTriangle(edge2.vertix1, edge2.vertix2, edge1.vertix2);

        Vector4 indices;
        indices.x = indices.z = indices.w = index1;
        indices.y = index2;
        terrain.AddQuadCellData(indices, weight1, weight2);
        terrain.AddQuadCellData(indices, weight1, weight2);
        terrain.AddQuadCellData(indices, 0.75f * weight1 + 0.25f * weight2, weight1, 0.75f * weight2 + 0.25f * weight1, weight2);
        terrain.AddTriangleCellData(indices, (weight1 + weight2) / 2f, 0.75f * weight2 + 0.25f * weight1, 0.75f * weight1 + 0.25f * weight2);
    }
}
