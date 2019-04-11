﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    public GameObject[] mazeGeneratorPrefab;
    public Transform playerHead;
    public int mazeCount;
    public int mazeRows;
    public int mazeCols;
    public float tileWidth = 1f;
    public float wallWidth = 0f;
    public int startRow;
    public int startCol;

    public Vector3 playAreaSize;
    public enum PortalGenerationType
    {
        Everywhere,
        Hallways
    }
    public PortalGenerationType portalGenerationLocation;
    public TileInfo[] portalInfo;
    public MapInfo[] mapSequence;

    void Start()
    {
        //3x3 is the minimum size
        if (mazeRows < 3)
            mazeRows = 3;
        if (mazeCols < 3)
            mazeCols = 3;

        if (mapSequence.Length > 1)
            portalInfo = new TileInfo[mapSequence.Length - 1];

        playAreaSize = GetCameraRigSize();
        GetStartSeedFromPlayerPosition(out startCol, out startRow);

        if (startRow < 0 || startRow >= mazeRows || startCol < 0 || startCol >= mazeCols)
        {
            startRow = 0;
            startCol = 0;
            Debug.Log("Player was out of game area, Maze starts from (0;0).");
        }
        if (portalGenerationLocation == PortalGenerationType.Everywhere)
            GenerateMapSequence();
        else
            GenerateMapSequenceHallway();
        OffsetMap();
        /*
        //This is to debug the portal infos in the console
        for (int i = 0; i < portalInfo.Length; i++)
        {
            Debug.Log("pp for maze: " + i + " r: " + portalInfo[i].row + " c: " + portalInfo[i].column + " d: " + portalInfo[i].direction);
        }
        */
        //maybe add script to find player head so we don't have to drag it in
    }

    void GenerateMapSequence()
    {
        if (mapSequence.Length > 0)
        {
            mapSequence[0].startSeed.row = startRow;
            mapSequence[0].startSeed.column = startCol;
            mapSequence[0].startSeed.direction = GenerateRandomStartDirection(startRow, startCol);
        }

        for (int i = 0; i < mapSequence.Length; i++)
        {
            Vector3 mapSpawnPoint = new Vector3(transform.position.x + i * (mazeCols * tileWidth + 1), 0, 0);
            GameObject tempMap = Instantiate(mazeGeneratorPrefab[(int)mapSequence[i].mapType], mapSpawnPoint, Quaternion.identity);
            tempMap.name = i.ToString() + " - " + mapSequence[i].mapType.ToString();
            tempMap.transform.parent = transform;

            MapGenerator mapScript = tempMap.GetComponent<MapGenerator>();
            mapScript.SetDimensions(mazeRows, mazeCols, tileWidth, wallWidth);
            mapScript.Initialize();

            //calculate start seed
            if (i > 0)
            {
                mapSequence[i].startSeed = new TileInfo(mapSequence[i - 1].endSeed);
                mapSequence[i].startSeed.direction = PortalPositionHelper.GetRandomPortalExit(mapSequence[i].startSeed.row, mapSequence[i].startSeed.column, mapSequence[i - 1].endSeed.direction);
            }
            if ((int)mapSequence[i].mapType == 1)
            {
                if (!((mapSequence[i].startSeed.row == 0 || mapSequence[i].startSeed.row == mazeRows - 1) && (mapSequence[i].startSeed.column == 0 || mapSequence[i].startSeed.column == mazeCols - 1)))
                {
                    mapSequence[i].startSeed.row = 0;
                    mapSequence[i].startSeed.column = 0;
                    mapSequence[i].startSeed.direction = PortalPositionHelper.GetRandomPortalExit(mapSequence[i].startSeed.row, mapSequence[i].startSeed.column);
                }
            }
            else
            {
                if (i + 1 < mapSequence.Length && (int)mapSequence[i + 1].mapType == 1) //Change this so we can use the enum
                {
                    mapSequence[i].isEndSeeded = true;
                    mapSequence[i].endSeed = GenerateRandomConrner(mapSequence[i].startSeed); //this will introduce errors if they are next to each other, need to fix
                }

            }
            mapScript.Generate(mapSequence[i]);
            if (mapSequence[i].isEndSeeded == false)
                mapSequence[i].endSeed = mapScript.GetRandomDeadEnd(mapSequence[i].startSeed);
            if (i < portalInfo.Length)
                portalInfo[i] = new TileInfo(mapSequence[i].endSeed);
        }
    }

    void GenerateMapSequenceHallway()
    {
        if (mapSequence.Length > 0)
        {
            mapSequence[0].startSeed.row = startRow;
            mapSequence[0].startSeed.column = startCol;
            mapSequence[0].startSeed.direction = GenerateRandomStartDirection(startRow, startCol);
        }

        for (int i = 0; i < mapSequence.Length; i++)
        {
            Debug.Log("Starting Maze " + i);
            Vector3 mapSpawnPoint = new Vector3(transform.position.x + i * (mazeCols * tileWidth + 1), 0, 0);
            GameObject tempMap = Instantiate(mazeGeneratorPrefab[(int)mapSequence[i].mapType], mapSpawnPoint, Quaternion.identity);
            tempMap.name = i.ToString() + " - " + mapSequence[i].mapType.ToString();
            tempMap.transform.parent = transform;

            MapGenerator mapScript = tempMap.GetComponent<MapGenerator>();
            mapScript.SetDimensions(mazeRows, mazeCols, tileWidth, wallWidth);
            mapScript.Initialize();
            Debug.Log("Maze " + i + " Initialized!");

            //calculate start seed
            if (i > 0)
            {
                mapSequence[i].startSeed = new TileInfo(mapSequence[i - 1].endSeed);
                mapSequence[i].startSeed.direction = (mapSequence[i - 1].endSeed.direction + 2) % 4; //rotate 180 degrees

                //mapSequence[i].startSeed.direction = PortalPositionHelper.GetRandomPortalExit(mapSequence[i].startSeed.row, mapSequence[i].startSeed.column, mapSequence[i - 1].endSeed.direction);
            }
            if ((int)mapSequence[i].mapType == 1)
            {
                Debug.Log("Can't do rooms with this method yet...");
                continue;
            }
            /* else
            {
                if (i + 1 < mapSequence.Length && (int)mapSequence[i + 1].mapType == 1) //Change this so we can use the enum
                {
                    Debug.Log("Can't do rooms with this method yet...");
                    //i++;
                }
            } */
            if (i + 1 < mapSequence.Length)
            {
                mapSequence[i].isEndSeeded = true;
                Debug.Log("Generating End Seed For Maze " + i);
                mapSequence[i].endSeed = GenerateRandomHallwayDeadEnd(mapSequence[i].startSeed);
            }
            mapScript.Generate(mapSequence[i]);
            //if (mapSequence[i].isEndSeeded == false)
            mapSequence[i].endSeed = mapScript.GetRandomDeadEndHallway(mapSequence[i].startSeed);
            if (i < portalInfo.Length)
                portalInfo[i] = new TileInfo(mapSequence[i].endSeed);
        }
    }

    int GenerateRandomStartDirection(int row, int col)
    {
        return PortalPositionHelper.GetRandomPortalExit(row, col);
    }

    TileInfo GenerateRandomHallwayDeadEnd(TileInfo flag)
    {
        TileInfo startCoord = new TileInfo(flag.row, flag.column, -1);
        List<TileInfo> possibleCoordinates = new List<TileInfo>();
        for (int i = 0; i < mazeRows; i++)
        {
            for (int j = 0; j < mazeCols; j++)
            {
                possibleCoordinates.Add(new TileInfo(i, j, -1));
            }
        }

        //Remove Start
        if (possibleCoordinates.Remove(startCoord))
            Debug.Log("Removed Start (" + startCoord.row + ";" + startCoord.column + ")");

        //Remove Lead-in
        if (possibleCoordinates.Remove(flag.GetNeighbourCoord()))
            Debug.Log("Removed Lead-in (" + flag.GetNeighbourCoord().row + ";" + flag.GetNeighbourCoord().column + ")");

        //Remove corners
        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                TileInfo cornerTile = new TileInfo(i * (mazeRows - 1), j * (mazeCols - 1), -1);
                if (possibleCoordinates.Remove(cornerTile))
                    Debug.Log("Removed corner (" + cornerTile.row + ";" + cornerTile.column + ")");
            }
        }

        //Remove corner shutoffs
        List<TileInfo> shutoffCorners = PortalPositionHelper.GetShutoffList(startCoord);
        if (shutoffCorners.Count > 0)
        {
            Debug.Log("Start (" + startCoord.row + ";" + startCoord.column + ") Shuts off corners.");
            foreach (TileInfo t in shutoffCorners)
            {
                if (possibleCoordinates.Remove(t))
                    Debug.Log("(" + t.row + ";" + t.column + ") Removed.");
            }
        }

        //Generate possible directions
        Debug.Log("All possible coordinates\n---------------------");
        foreach (TileInfo t in possibleCoordinates)
        {
            Debug.Log("(" + t.row + ";" + t.column + ")");
        }

        List<TileInfo> possibleTiles = new List<TileInfo>();
        foreach (TileInfo t in possibleCoordinates)
        {
            int[] possibleDirections = PortalPositionHelper.GetEntranceArray(t.row, t.column);
            for (int i = 0; i < possibleDirections.Length; i++)
            {
                TileInfo tileToAdd = new TileInfo(t.row, t.column, possibleDirections[i]);
                possibleTiles.Add(tileToAdd);
            }
        }

        //Remove perpendicular
        List<TileInfo> tilesToRemove = new List<TileInfo>();
        foreach (TileInfo t in possibleTiles)
        {
            if (t.IsPerpendicular())
            {
                tilesToRemove.Add(t);
                Debug.Log("Tile (" + t.row + ";" + t.column + ";" + t.direction + ") is perpendicular");
            }
            else if (t.IsLeadingIntoEntrance(flag))
            {
                tilesToRemove.Add(t);
                Debug.Log("Tile (" + t.row + ";" + t.column + ";" + t.direction + ") leads into entrance");
            }
        }

        foreach (TileInfo t in tilesToRemove)
        {
            if (possibleTiles.Remove(t))
            {
                Debug.Log("Tile (" + t.row + ";" + t.column + ";" + t.direction + ") removed");
            }
        }

        Debug.Log("All possible Tiles\n---------------------");
        foreach (TileInfo t in possibleTiles)
        {
            Debug.Log("(" + t.row + ";" + t.column + ";" + t.direction + ")");
        }

        int idx = Random.Range(0, possibleTiles.Count);
        return possibleTiles[idx];
    }

    TileInfo GenerateRandomConrner(TileInfo flag)
    {
        int row, col, dir;
        do
        {
            row = Mathf.RoundToInt(Random.value) * (mazeRows - 1);
            col = Mathf.RoundToInt(Random.value) * (mazeCols - 1);
            dir = GenerateRandomStartDirection(row, col);
        }
        while (row == flag.row && col == flag.column);
        return new TileInfo(row, col, dir);
    }

    public Vector3 GetCameraRigSize()
    {
        Vector3 size = new Vector3(playAreaSize.x, 0, playAreaSize.z);
        var chaperone = Valve.VR.OpenVR.Chaperone;
        float x = 0, z = 0;
        if (chaperone != null)
        {
            chaperone.GetPlayAreaSize(ref x, ref z);
            Debug.Log("got here"); //expert debugging right here
            size = new Vector3(Mathf.Round(x), 0, Mathf.Round(z));
        }
        return size;
    }

    void OffsetMap()
    {
        transform.Translate(-playAreaSize.x / 2f + tileWidth / 2f, 0, playAreaSize.z / 2f - tileWidth / 2f);
    }

    void GetStartSeedFromPlayerPosition(out int col, out int row)
    {
        col = Mathf.RoundToInt(Mathf.Abs((playerHead.position.x - (-playAreaSize.x / 2f + tileWidth / 2f)) / tileWidth));
        row = Mathf.RoundToInt(Mathf.Abs((playerHead.position.z - (playAreaSize.z / 2f - tileWidth / 2f)) / tileWidth));

        return;
    }
}
