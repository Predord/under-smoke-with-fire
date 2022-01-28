using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class QuadMesh : MonoBehaviour
{
    public bool useCollider;
    public bool castShadows;
    public bool useCellData;

    private Mesh quadMesh;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;

    [NonSerialized] private List<Vector3> vertices;
    [NonSerialized] private List<Vector4> cellIndices;
    [NonSerialized] private List<int> triangles;
    [NonSerialized] private List<Color> cellWeights;

    private void Awake()
    {
        GetComponent<MeshFilter>().mesh = quadMesh = new Mesh();
        meshRenderer = gameObject.GetComponent<MeshRenderer>();
        if (useCollider)
        {
            meshCollider = gameObject.AddComponent<MeshCollider>();
            gameObject.layer = 8;
        }
        quadMesh.name = "Quad Mesh";
    }

    public void Clear()
    {
        quadMesh.Clear();
        vertices = ListPool<Vector3>.Get();
        if (useCellData)
        {
            cellWeights = ListPool<Color>.Get();
            cellIndices = ListPool<Vector4>.Get();
        }
        triangles = ListPool<int>.Get();
    }

    public void Apply()
    {
        quadMesh.SetVertices(vertices);
        ListPool<Vector3>.Add(vertices);
        if (useCellData)
        {
            quadMesh.SetColors(cellWeights);
            ListPool<Color>.Add(cellWeights);
            quadMesh.SetUVs(0, cellIndices);
            ListPool<Vector4>.Add(cellIndices);
        }
        quadMesh.SetTriangles(triangles, 0);
        ListPool<int>.Add(triangles);
        quadMesh.RecalculateNormals();
        if (useCollider)
        {
            meshCollider.sharedMesh = quadMesh;
        }
        if (castShadows)
        {
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            meshRenderer.receiveShadows = true;
        }
        else
        {
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
        }
    }

    public void AddTriangle(Vector3 vertix1, Vector3 vertix2, Vector3 vertix3)
    {
        int vertexIndex = vertices.Count;
        vertices.Add(QuadMetrics.Perturb(vertix1));
        vertices.Add(QuadMetrics.Perturb(vertix2));
        vertices.Add(QuadMetrics.Perturb(vertix3));
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
    }

    public void AddTriangleCellData(Vector4 indices, Color weights1, Color weights2, Color weights3)
    {
        cellIndices.Add(indices);
        cellIndices.Add(indices);
        cellIndices.Add(indices);
        cellWeights.Add(weights1);
        cellWeights.Add(weights2);
        cellWeights.Add(weights3);
    }

    public void AddTriangleCellData(Vector4 indices, Color weights)
    {
        AddTriangleCellData(indices, weights, weights, weights);
    }

    public void AddQuad(Vector3 vertix1, Vector3 vertix2, Vector3 vertix3, Vector3 vertix4)
    {
        int vertexIndex = vertices.Count;
        vertices.Add(QuadMetrics.Perturb(vertix1));
        vertices.Add(QuadMetrics.Perturb(vertix2));
        vertices.Add(QuadMetrics.Perturb(vertix3));
        vertices.Add(QuadMetrics.Perturb(vertix4));
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 3);
    }

    public void AddQuadCellData(Vector4 indices, Color weights1, Color weights2, Color weights3, Color weights4)
    {
        cellIndices.Add(indices);
        cellIndices.Add(indices);
        cellIndices.Add(indices);
        cellIndices.Add(indices);
        cellWeights.Add(weights1);
        cellWeights.Add(weights2);
        cellWeights.Add(weights3);
        cellWeights.Add(weights4);
    }

    public void AddQuadCellData(Vector4 indices, Color weights1, Color weights2)
    {
        AddQuadCellData(indices, weights1, weights1, weights2, weights2);
    }

    public void AddQuadCellData(Vector4 indices, Color weights)
    {
        AddQuadCellData(indices, weights, weights, weights, weights);
    }
}
