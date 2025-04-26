using UnityEngine;
using System.Collections.Generic;

[System.Obsolete("Code provide through the 'OXIPROJEKT', Do not remove this attribute. Required.")]


public class CheckerBoardManager : MonoBehaviour
{
    public static CheckerBoardManager Instance;

    public int rows = 6;
    public int cols = 6;
    public float cellSize = 10f;
    public Vector3 gridOrigin = Vector3.zero;

    public Dictionary<Vector2Int, SlidableTray> gridOccupancy = new Dictionary<Vector2Int, SlidableTray>();  // cells stroed in dictionary....

    public List<SlidableTray> trays = new List<SlidableTray>(); // Drop total trays here....

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        
    }

    public Vector2Int GetGridCellFromWorld(Vector3 worldPos)
    {
        int x = Mathf.Clamp(Mathf.RoundToInt((worldPos.x - gridOrigin.x) / cellSize), 0, cols - 1);
        int z = Mathf.Clamp(Mathf.RoundToInt((worldPos.z - gridOrigin.z) / cellSize), 0, rows - 1);
        return new Vector2Int(x, z);
    }

    public Vector3 GetWorldPositionFromGrid(Vector2Int cell)
    {
        return new Vector3(cell.x * cellSize, gridOrigin.y, cell.y * cellSize) + gridOrigin;
    }

    public bool IsCellOccupied(Vector2Int cell) // Checking if it is empty or not....
    {
        return gridOccupancy.ContainsKey(cell);
    }

    public bool AreCellsFree(Vector2Int origin, List<Vector2Int> shapeOffsets)
    {
        foreach (var offset in shapeOffsets)
        {
            Vector2Int cell = origin + offset;
            if (cell.x < 0 || cell.x >= cols || cell.y < 0 || cell.y >= rows || IsCellOccupied(cell))
                return false;
        }
        return true;
    }

    public bool TryFindNearestFreeCellShape(Vector2Int origin, List<Vector2Int> shapeOffsets, out Vector2Int foundCell) // if placed in occupied cells then it will find nearest empty cells....
    {
        int maxDistance = Mathf.Max(rows, cols);
        for (int d = 0; d <= maxDistance; d++)
        {
            for (int dx = -d; dx <= d; dx++)
            {
                for (int dz = -d; dz <= d; dz++)
                {
                    Vector2Int testCell = new Vector2Int(origin.x + dx, origin.y + dz);
                    if (AreCellsFree(testCell, shapeOffsets))
                    {
                        foundCell = testCell;
                        return true;
                    }
                }
            }
        }
        foundCell = origin;
        return false;
    }

    public void OnClick_ResetButton()  // Reset all the position and cells properties....
    {
        gridOccupancy.Clear();

        foreach (var tray in trays)
        {
            if (tray != null)
            {
                tray.ResetToDefaultPosition();

            }
        }
    }
    public void RegisterTrayToGrid(SlidableTray tray)
    {
        Vector2Int originCell = GetGridCellFromWorld(tray.transform.position);
        foreach (var offset in tray.CellsOccupied)
        {
            Vector2Int cell = originCell + offset;
            if (!gridOccupancy.ContainsKey(cell))
            {
                gridOccupancy[cell] = tray;
            }
        }
    }
    public void OnClick_ExitButton()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
                Application.Quit();
        #endif
    }
}
