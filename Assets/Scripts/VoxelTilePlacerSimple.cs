using Assembly_CSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class VoxelTilePlacerSimple : MonoBehaviour
{

    public List<VoxelTile> TilePrefabs;
    public Vector2Int MapSize = new Vector2Int(10, 10);

    private VoxelTile[,] spawnedTiles;

    private void Start()
    {
        spawnedTiles = new VoxelTile[MapSize.x, MapSize.y];

        foreach (VoxelTile tilePrefab in TilePrefabs)
        {
            tilePrefab.CalculateSideColors();
        }

        int countBeforeAdding = TilePrefabs.Count;

        VoxelTile clone;

        for (int i = 0; i < countBeforeAdding; i++)
        {
            switch (TilePrefabs[i].Rotation)
            {
                case VoxelTile.RotationType.OnlyRotation:
                    break;
                case VoxelTile.RotationType.TwoRotation:

                    TilePrefabs[i].Weight /= 2;
                    if (TilePrefabs[i].Weight <= 0) TilePrefabs[i].Weight = 0;
                   

                    clone = Instantiate(TilePrefabs[i], TilePrefabs[i].transform.position + Vector3.back, Quaternion.identity);
                    clone.Rotate90();
                    clone.Rotate90();
                    TilePrefabs.Add(clone);
                    break;
                case VoxelTile.RotationType.FourRotation:

                    TilePrefabs[i].Weight /= 4;
                    if (TilePrefabs[i].Weight <= 0) TilePrefabs[i].Weight = 0;


                    clone = Instantiate(TilePrefabs[i], TilePrefabs[i].transform.position + Vector3.back, Quaternion.identity);
                    clone.Rotate90();
                    TilePrefabs.Add(clone);


                    clone = Instantiate(TilePrefabs[i], TilePrefabs[i].transform.position + Vector3.back * 2, Quaternion.identity);
                    clone.Rotate90();
                    clone.Rotate90();
                    TilePrefabs.Add(clone);


                    clone = Instantiate(TilePrefabs[i], TilePrefabs[i].transform.position + Vector3.back * 3, Quaternion.identity);
                    clone.Rotate90();
                    clone.Rotate90();
                    clone.Rotate90();
                    TilePrefabs.Add(clone);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        StartCoroutine(Generate());
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            StopAllCoroutines();

            foreach (VoxelTile spawnedTile in spawnedTiles)
            {
                if (spawnedTile != null) Destroy(spawnedTile.gameObject);



            }

            StartCoroutine(Generate());
        }


    }


    public IEnumerator Generate()
    {


        for (int x = 1; x < MapSize.x - 1; x++)
        {
            for (int y = 1; y < MapSize.y - 1; y++)
            {
                yield return new WaitForSeconds(0);

                PlaceTile(x, y);
            }
        }
    }


    private void PlaceTile(int x, int y)
    {
        List<VoxelTile> availbleTiles = new List<VoxelTile>();

        foreach (VoxelTile tilePrefab in TilePrefabs)
        {
            if (CanAppendTile(spawnedTiles[x - 1, y], tilePrefab, Direction.Left) &&
                CanAppendTile(spawnedTiles[x + 1, y], tilePrefab, Direction.Right) &&
                CanAppendTile(spawnedTiles[x, y - 1], tilePrefab, Direction.Back) &&
                CanAppendTile(spawnedTiles[x, y + 1], tilePrefab, Direction.Forward))
            {
                availbleTiles.Add(tilePrefab);

            }
        }

        if (availbleTiles.Count == 0) return;


        VoxelTile selectedTile = GetRandomTile(availbleTiles);

        Vector3 positions = new Vector3(x, 0, y) * selectedTile.VoxelSize * selectedTile.TileSideVoxels;

        spawnedTiles[x, y] = Instantiate(selectedTile, positions, selectedTile.transform.rotation); 
    }
    private VoxelTile GetRandomTile(List<VoxelTile> availableTiles)
    {
        List<float> chances = new List<float>();
        for (int i = 0; i < availableTiles.Count; i++)
        {
            chances.Add(availableTiles[i].Weight);
        }

        float value = Random.Range(0, chances.Sum());
        float sum = 0;

        for (int i = 0; i < chances.Count; i++)
        {
            sum += chances[i];
            if (value < sum)
            {
                return availableTiles[i];
            }
        }

        return availableTiles[availableTiles.Count - 1];
    }


    private bool CanAppendTile(VoxelTile existingTile, VoxelTile tileToAppend, Direction direction)
    {
        if (existingTile == null) return true;

        if (direction == Direction.Right)
        {
            return Enumerable.SequenceEqual(existingTile.ColorsRight, tileToAppend.ColorsLeft);
        }
        else if (direction == Direction.Left)
        {
            return Enumerable.SequenceEqual(existingTile.ColorsLeft, tileToAppend.ColorsRight);
        }
        else if (direction == Direction.Forward)
        {
            return Enumerable.SequenceEqual(existingTile.ColorsForward, tileToAppend.ColorsBack);
        }
        else if (direction == Direction.Back)
        {
            return Enumerable.SequenceEqual(existingTile.ColorsBack, tileToAppend.ColorsForward);
        }
        else
        {
            throw new ArgumentException("Wrong direction value", nameof(direction));
        }
    }
}
