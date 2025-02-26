using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles all the functionality related to creating and modifying the game tiles
/// which are the View part of the game board
/// </summary>
public class GameTilesManager : MonoBehaviour
{
    public GameObject tilePrefab; // prefab for the tiles
    public Vector2 tileContainerPosition; // where should the tile container be placed
    public Sprite[] tileSpriteOptions;
    
    private Transform tileContainer; // parent gameObject for active tiles (part of the main grid)
    
    private Dictionary<int, GameTile> activeTilesDictionary; // collection of all the active tiles (part of the main grid)
    
    private int gridLength; // cache the grid length in this
    
    private void Awake()
    {
        GameEvents.GameGridReadyEvent -= OnGameGridReady;
        GameEvents.GameGridReadyEvent += OnGameGridReady;
    }
    private void OnDestroy()
    {
        GameEvents.GameGridReadyEvent -= OnGameGridReady;
    }

    // main grid is ready at the beginning of a level, create tiles 
    private void OnGameGridReady(GameGridCell[][] gameGrid)
    {
        CreateTiles(gameGrid);
    }
    
    // main grid is ready at the beginning of a level, create tiles 
    private void CreateTiles(GameGridCell[][] gameGrid)
    {
        // 1. create other required structures
        CreateTileContainers();
        CreateTileCollections();
    }
    
    private void CreateTileContainers()
    {
        if (tileContainer == null)
        {
            tileContainer = new GameObject("TileContainer").transform;
            tileContainer.transform.position = tileContainerPosition;
        }
    }
    
    private void CreateTileCollections()
    {
        if (activeTilesDictionary == null)
        {
            activeTilesDictionary = new Dictionary<int, GameTile>();
        }
    }
    
    
    
    // provides mapping from a tile's Y & X indices in the game grid to its position in the world, based on the container it is a part of
    private Vector2 GetWorldPositionFromGridPositionAndContainer(int gridY, int gridX, Transform container)
    {
        // grids go from 0 to gridLength, tile containers are centered on the X axis
        float mappingY = gridY - (gridLength * 0.5f) + 0.5f;
        float mappingX = gridX - (gridLength * 0.5f) + 0.5f;
        return new Vector2(container.position.x + (mappingX), container.position.y + (mappingY));
    }
    
    // helper function to select the sprite from the available options based on the input color
    public Sprite GetSpriteBasedOnColor(GameGridCell.GridCellColor color)
    {
        switch (color)
        {
            case GameGridCell.GridCellColor.Red:
                return tileSpriteOptions[0];
            
            case GameGridCell.GridCellColor.Green:
                return tileSpriteOptions[1];
            
            case GameGridCell.GridCellColor.Blue:
                return tileSpriteOptions[2];
            
            case GameGridCell.GridCellColor.Yellow:
                return tileSpriteOptions[3];
        }

        return null;
    }
}
