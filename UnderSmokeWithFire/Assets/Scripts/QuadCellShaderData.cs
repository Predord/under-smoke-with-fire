using System.Collections.Generic;
using UnityEngine;

public class QuadCellShaderData : MonoBehaviour
{
    private List<int> resetSpecialCellsVisibilityIndices = new List<int>();

    private bool needsVisibilityReset;
    private Texture2D cellTexture;
    private Color32[] cellTextureData;

    private void LateUpdate()
    {
        if (resetSpecialCellsVisibilityIndices.Count != 0)
        {
            GameManager.Instance.grid.SetSpecialCellsVisibility(resetSpecialCellsVisibilityIndices);
            resetSpecialCellsVisibilityIndices.Clear();
        }
        
        if (needsVisibilityReset)
        {
            needsVisibilityReset = false;
            GameManager.Instance.grid.ResetVisibility();
        }
        cellTexture.SetPixels32(cellTextureData);
        cellTexture.Apply();
        enabled = false;
    }

    public void Initialize(int x, int z)
    {
        if (cellTexture)
        {
            cellTexture.Resize(x, z);
        }
        else
        {
            cellTexture = new Texture2D(x, z, TextureFormat.RGBA32, false, true)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            Shader.SetGlobalTexture("_QuadCellData", cellTexture);
        }
        Shader.SetGlobalVector("_QuadCellData_TexelSize", new Vector4(1f / x, 1f / z, x, z));

        if (cellTextureData == null || cellTextureData.Length != x * z)
        {
            cellTextureData = new Color32[x * z];
            for (int i = 0; i < cellTextureData.Length; i++)
            {
                cellTextureData[i].b = 255;
            }
        }
        else
        {
            for(int i = 0; i < cellTextureData.Length; i++)
            {
                cellTextureData[i] = new Color32(0, 0, 255, 0);
            }
        }

        enabled = true;
    }

    public void ViewElevationChanged()
    {
        needsVisibilityReset = true;
        enabled = true;
    }

    public void RefreshTerrain(QuadCell cell)
    {
        cellTextureData[cell.Index].a = (byte)cell.TerrainTypeIndex;
        enabled = true;
    }

    public void RefreshVisibility(QuadCell cell)
    {
        int index = cell.Index;
        cellTextureData[index].r = cell.IsVisible ? (byte)255 : (byte)0;
        cellTextureData[index].g = cell.IsExplored ? (byte)255 : (byte)0;
        enabled = true;
    }

    public void AddResetSpecialCellsVisibilityIndices(int index)
    {
        if (!resetSpecialCellsVisibilityIndices.Contains(index))
            resetSpecialCellsVisibilityIndices.Add(index);
    }

    public void RemoveResetSpecialCellsVisibilityIndices(int index)
    {
        if (resetSpecialCellsVisibilityIndices.Contains(index))
            resetSpecialCellsVisibilityIndices.Remove(index);
    }

    public void RefreshTarget(QuadCell cell)
    {
        cellTextureData[cell.Index].b = cell.IsTargeted ? (byte)0 : (byte)255;
        enabled = true;
    }
}
