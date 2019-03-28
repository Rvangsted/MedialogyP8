﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class MapGenerator : MonoBehaviour
{
    public int mazeRows;
    public int mazeColumns;
    public float tileWidth;
    public float wallWidth;
    public GameObject tilePrefab;
    public Tile[,] tileArray;
    bool matCheck = false;
    public int[,] mazeIntArray;

    public void Initialize()
    {
        tileArray = new Tile[mazeRows, mazeColumns];
        //float mazeHalfWidth = mazeRows / 2f; // Add scalability with tile width!
        //float mazeHalfHeight = mazeColumns / 2f; // Add scalability with tile height!
        for (int i = 0; i < mazeRows; i++)
        {
            for (int j = 0; j < mazeColumns; j++)
            {
                Vector3 tileSpawnPosition = new Vector3(transform.position.x + j * tileWidth, 0, transform.position.z - i * tileWidth);
                GameObject emptyTile = Instantiate(tilePrefab, tileSpawnPosition, Quaternion.identity);
                emptyTile.name = "Tile " + (mazeColumns * i + j).ToString();
                emptyTile.transform.parent = transform;
                tileArray[i, j] = emptyTile.GetComponent<Tile>();
                tileArray[i, j].SetWidth(tileWidth);
            }
        }
        //Debug.Log(name + " initialized.");
    }

    protected void GenerateIntArray()
    {
        mazeIntArray = new int[mazeRows, mazeColumns];
        for (int i = 0; i < mazeRows; i++)
        {
            for (int j = 0; j < mazeColumns; j++)
            {
                EventCallbacks.GenerateTerrainEvent gtei = new EventCallbacks.GenerateTerrainEvent();
                mazeIntArray[i, j] = tileArray[i, j].GetTileID();
                gtei.go = tileArray[i, j].gameObject;
                gtei.wallArray = tileArray[i, j].GetWallArray();
                gtei.tileWidth = tileWidth;

                //ID Changing when creating new tile
                gtei.FireEvent();
                //Debug.Log(tileWidth + " generated int array");
            }
        }
    }

    public void SetDimensions(int rows, int cols, float width, float wWidth)
    {
        mazeRows = rows;
        mazeColumns = cols;
        tileWidth = width;
        wallWidth = wWidth;
    }
    public List<int[]> GetDeadEndList()
    {
        List<int[]> deadEnd = new List<int[]>();
        for (int i = 0; i < mazeRows; i++)
        {
            for (int j = 0; j < mazeColumns; j++)
            {
                if (mazeIntArray[i, j] == 1 || mazeIntArray[i, j] == 2 || mazeIntArray[i, j] == 4 || mazeIntArray[i, j] == 8)
                {
                    deadEnd.Add(new int[] { i, j });
                    //Debug.Log("" + i + " " + j);
                }
            }
        }
        return deadEnd;
    }

    public int[] GetFirstDeadEnd(int entranceRow, int entranceCol)
    {
        List<int[]> deadEndList = GetDeadEndList();
        foreach (int[] deadEnd in deadEndList)
        {
            if (deadEnd[0] == entranceRow && deadEnd[1] == entranceCol)
                continue;
            else
                return deadEnd;
        }
        return new int[] { -1, -1 };
    }

    public int[] GetRandomDeadEnd(int entranceRow, int entranceCol)
    {
        List<int[]> deadEndList = GetDeadEndList();
        int[] deadEnd = new int[] { -1, -1 };
        do
        {
            int idx = Random.Range(0,deadEndList.Count);
            deadEnd = deadEndList[idx];
        }
        while (deadEnd[0] == entranceRow && deadEnd[1] == entranceCol);
        return deadEnd;
    }

    public TileInfo GetRandomDeadEnd(TileInfo entrance)
    {
        int[] deadEnd = GetRandomDeadEnd(entrance.row, entrance.column);
        int deadEndDirection = (int)Mathf.Log(mazeIntArray[deadEnd[0], deadEnd[1]], 2);
        return new TileInfo(deadEnd[0], deadEnd[1], deadEndDirection);
    }

    public abstract void Generate();
    public abstract void Generate(MapInfo info);
    public abstract void Generate(TileInfo info);
    public abstract void Generate(int startRow, int startCol, int startDir);
    public abstract void Generate(int startRow, int startCol, int startDir, int endRow, int endCol, int endDir);
}
