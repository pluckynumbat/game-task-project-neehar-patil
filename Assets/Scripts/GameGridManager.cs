using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles all the functionality related to creating and modifying the game grid(s)
/// which are the data / Model part of the game board
/// </summary>
public class GameGridManager : MonoBehaviour
{
    private GameGridCell[][] mainGrid; // the main / active grid in the level
    
    private int gridLength; // both height and width of the grid are the same, store in this variable

    private void Awake()
    {
        GameEvents.LevelDataReadyEvent -= OnLevelDataReady;
        GameEvents.LevelDataReadyEvent += OnLevelDataReady;
        
        GameEvents.ActiveTileTappedEvent -= OnActiveTileTapped;
        GameEvents.ActiveTileTappedEvent += OnActiveTileTapped;
    }

    private void OnDestroy()
    {
        GameEvents.LevelDataReadyEvent -= OnLevelDataReady;
        GameEvents.ActiveTileTappedEvent -= OnActiveTileTapped;
    }

    private void OnLevelDataReady(LevelData data)
    {
        SetupGameGrid(data);
        GameEvents.RaiseGameGridReadyEvent(mainGrid);
    }
    
    // does the initial setup of the game grid when a new level begins
    private void SetupGameGrid(LevelData levelData)
    {
        gridLength = levelData.gridLength;

        bool isGridRandom = levelData.isStartingGridRandom;
        
        mainGrid = new GameGridCell[gridLength][];
        for (int y = 0; y < gridLength; y++)
        {
            mainGrid[y] = new GameGridCell[gridLength];
            for (int x = 0; x < gridLength; x++)
            {
                mainGrid[y][x] = new GameGridCell(y, x);
                if (isGridRandom)
                {
                    mainGrid[y][x].Color = GetRandomGridCellColor(levelData.colorCount);
                }
                else // get the grid specified in the level data
                {
                    //mapping from a top first list, to a bottom first 2D array
                    int mapping =  ((gridLength - 1 - y) * gridLength) + x;
                    mainGrid[y][x].Color = GetGridCellColorFromString(levelData.startingGrid[mapping]);
                }
                mainGrid[y][x].Occupied = true;
            }
        }
    }
    
    // randomly generate a new grid cell color
    private GameGridCell.GridCellColor GetRandomGridCellColor(int validColorCount)
    {
        //TODO init seed?
        int randomRoll = Random.Range(0, validColorCount);
        return (GameGridCell.GridCellColor)randomRoll;
    }

    //helper to select color based on string input
    private GameGridCell.GridCellColor GetGridCellColorFromString(string colorString)
    {
        switch (colorString)
        {
            case "R":
                return GameGridCell.GridCellColor.Red;
            
            case "G":
                return GameGridCell.GridCellColor.Green;
            
            case "B":
                return GameGridCell.GridCellColor.Blue;
            
            case "Y":
                return GameGridCell.GridCellColor.Yellow;
        }
        
        Debug.LogError($"Invalid color string: {colorString}");
        return GameGridCell.GridCellColor.None;
    }
    
    // player attempted a move on the game board, process the move
    private void OnActiveTileTapped(int gridY, int gridX)
    {
        // check if the grid position is valid, and grid item is valid
        if (!IsWithinGridBounds(gridY, gridX))
        {
            GameEvents.RaiseInvalidMoveEvent(gridY, gridX);
            Debug.LogError($"Invalid input in OnActiveTileTapped (out of bounds), x: {gridX}, y: {gridY}");
            return;
        }

        GameGridCell cell = mainGrid[gridY][gridX];
        if (!cell.Occupied || cell.Color == GameGridCell.GridCellColor.None)
        {
            GameEvents.RaiseInvalidMoveEvent(gridY, gridX);
            Debug.LogError($"Invalid input in OnActiveTileTapped (cell is empty), x: {gridX}, y: {gridY}");
            return;
        }
        
        //TODO: remove this log later? 
        Debug.Log($"Active tile tapped: X: {gridX}, Y: {gridY}");
        
        // Start actual processing
        
        //1. Check if the grid cell has neighbors with the same color
        if (!AnyNeighborWithSameColor(gridY, gridX, cell.Color, mainGrid))
        {
            // if not, raise invalid move event, and return
            GameEvents.RaiseInvalidMoveEvent(gridY, gridX);
            //TODO: some feedback here or in the tiles manager to let the player know that a single tile cannot be removed?
            Debug.Log($"(single cell cannot be removed), x: {gridX}, y: {gridY}");
            return;
        }
        
        //2. Collect all neighbors with the same color using a Breadth First Search starting at the cell at the tapped tile's location
        List<GameGridCell> gridCellsToRemove = CollectNeighborsWithSameColor(tappedCell, mainGrid);
        
        //3. Grid Cell 'Removal'
        
        //3a. Mark all the cells to remove as empty
        foreach (GameGridCell cell in gridCellsToRemove)
        {
            cell.Occupied = false;
            cell.Color = GameGridCell.GridCellColor.None;
        }
        
        //3b. Let other systems know that these cells have been 'removed' so they can deal with the information
        // (e.g. tiles manager will have to actually remove tiles based on this list)
        GameEvents.RaiseGridCellsRemovedEvent(gridCellsToRemove);
        
    }
    
    // helper function to check if given y and x co-ordinates are valid for the game grid(s)
    private bool IsWithinGridBounds(int y, int x)
    {
        return  0 <= y && y < gridLength && x >= 0 && x <gridLength;
    }
    
    // helper function to check for a given cell Y and X indices, if there exists at least 1 neighbor with the same color in the given grid
    private bool AnyNeighborWithSameColor(int y, int x, GameGridCell.GridCellColor color, GameGridCell[][] grid)
    {
        // north neighbor
        if (IsWithinGridBounds(y + 1, x) && grid[y + 1][x].Color == color)
        {
            return true;
        }
        
        // east neighbor
        if (IsWithinGridBounds(y, x + 1) && grid[y][x + 1].Color == color)
        {
            return true;
        }
        
        // south neighbor
        if (IsWithinGridBounds(y - 1, x) && grid[y - 1][x].Color == color)
        {
            return true;
        }
        
        // west neighbor
        if (IsWithinGridBounds(y, x - 1) && grid[y][x - 1].Color == color)
        {
            return true;
        }

        return false;
    }
    
    //Collect all neighbors with the same color using a Breadth First Search starting at the cell at the tapped tile's location
    private List<GameGridCell> CollectNeighborsWithSameColor(GameGridCell tappedCell, GameGridCell[][] grid)
    {
        List<GameGridCell> relevantCells = new List<GameGridCell>(); // these will be sent to the caller at the end of the method

        // reset the visited array
        for (int y = 0; y < gridLength; y++)
        {
            for (int x = 0; x < gridLength; x++)
            {
                visited[y][x] = false;
            } 
        }
        
        // create a queue and add the starting cell to it
        Queue<GameGridCell> queue = new Queue<GameGridCell>();
        queue.Enqueue(tappedCell);

        while (queue.Count > 0)
        {
            // get the next cell to process
            GameGridCell current = queue.Dequeue();
            
            // skip if visited already
            if (visited[current.Y][current.X])
            {
                continue;
            }

            // mark as visited and add to list of relevant cells
            visited[current.Y][current.X] = true;
            relevantCells.Add(current);
            
            // check and visit neighbors (within grid bounds) with same color that are unvisited
            
            // north neighbor
            if (IsWithinGridBounds(current.Y + 1, current.X) &&
                grid[current.Y + 1][current.X].Color == tappedCell.Color &&
                !visited[current.Y + 1][current.X])
            {
                queue.Enqueue(grid[current.Y + 1][current.X]);
            }
            
            // east neighbor
            if (IsWithinGridBounds(current.Y, current.X + 1) &&
                grid[current.Y][current.X + 1].Color == tappedCell.Color &&
                !visited[current.Y][current.X + 1])
            {
                queue.Enqueue(grid[current.Y][current.X + 1]);
            }
            
            // south neighbor
            if (IsWithinGridBounds(current.Y - 1, current.X) &&
                grid[current.Y - 1][current.X].Color == tappedCell.Color &&
                !visited[current.Y - 1][current.X])
            {
                queue.Enqueue(grid[current.Y - 1][current.X]);
            }
            
            // west neighbor
            if (IsWithinGridBounds(current.Y, current.X - 1) &&
                grid[current.Y][current.X - 1].Color == tappedCell.Color &&
                !visited[current.Y][current.X - 1])
            {
                queue.Enqueue(grid[current.Y][current.X - 1]);
            }
        }
        
        return relevantCells;
    }
}
