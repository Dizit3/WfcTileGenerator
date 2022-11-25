using Assembly_CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class GeneratorWfc : MonoBehaviour
{


    public List<VoxelTile> TilePrefabs;
    public Vector2Int MapSize = new Vector2Int(10, 10);

    private VoxelTile[,] spawnedTiles;

    private List<VoxelTile>[,] possibleTiles;
    private Queue<Vector2Int> recalcPossibleTilesQueue = new Queue<Vector2Int>();


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

        Generate();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            foreach (VoxelTile spawnedTile in spawnedTiles)
            {
                if (spawnedTile != null) Destroy(spawnedTile.gameObject);
            }
            Generate();
        }
    }


    private void Generate()
    {
        possibleTiles = new List<VoxelTile>[MapSize.x, MapSize.y];

        int maxAttempts = 10;
        int attempts = 0;
        while (attempts++ < maxAttempts)
        {

            for (int x = 0; x < MapSize.x; x++)
                for (int y = 0; y < MapSize.y; y++)
                {
                    possibleTiles[x, y] = new List<VoxelTile>(TilePrefabs);
                }

            VoxelTile tileInCenter = GetRandomTile(TilePrefabs);
            possibleTiles[MapSize.x / 2, MapSize.y / 2] = new List<VoxelTile> { tileInCenter };


            recalcPossibleTilesQueue.Clear();
            EnqueueNeighboursToRecalc(new Vector2Int(MapSize.x / 2, MapSize.y / 2));

            bool success = GenerateAllPossibleTiles();


            if (success) break;
           
        }



        PlaceAllTiles();
    }

    private bool GenerateAllPossibleTiles()
    {
        int maxIteration = MapSize.x * MapSize.y;

        int iterations = 0;

        while (iterations++ < maxIteration)
        {
            int maxInnerIteration = 500;
            int iterationsInner = 0;
            while (recalcPossibleTilesQueue.Count > 0 && iterationsInner++ < maxInnerIteration)
            {
                Vector2Int position = recalcPossibleTilesQueue.Dequeue();

                if (position.x == 0 || position.y == 0 ||
                    position.x == MapSize.x - 1 || position.y == MapSize.y - 1)
                {
                    continue;
                }

                List<VoxelTile> possibleTilesHere = possibleTiles[position.x, position.y];

                int countRemoved = possibleTilesHere.RemoveAll(t => !IsTilePossible(t, position));

                if (countRemoved > 0) EnqueueNeighboursToRecalc(position);

                //TODO: Что если possibleTileHere - Пустой ? 
                if (possibleTilesHere.Count == 0)
                {
                    possibleTilesHere.AddRange(TilePrefabs);
                    possibleTiles[position.x + 1, position.y] = new List<VoxelTile>(TilePrefabs);
                    possibleTiles[position.x - 1, position.y] = new List<VoxelTile>(TilePrefabs);
                    possibleTiles[position.x, position.y + 1] = new List<VoxelTile>(TilePrefabs);
                    possibleTiles[position.x, position.y - 1] = new List<VoxelTile>(TilePrefabs);

                    EnqueueNeighboursToRecalc(position);
                }

            }

            if (maxInnerIteration == maxIteration) break;


            List<VoxelTile> maxCountTile = possibleTiles[1, 1];
            Vector2Int maxCountTilePositions = new Vector2Int(1, 1);

            for (int x = 1; x < MapSize.x - 1; x++)
                for (int y = 1; y < MapSize.y - 1; y++)
                {
                    if (possibleTiles[x, y].Count > maxCountTile.Count)
                    {
                        maxCountTile = possibleTiles[x, y];
                        maxCountTilePositions = new Vector2Int(x, y);
                    }
                }


            if (maxCountTile.Count == 1)
            {
                Debug.Log($"Generated for {iterations} iterations");
                return true;
            }

            VoxelTile tileToCollapse = GetRandomTile(maxCountTile);
            possibleTiles[maxCountTilePositions.x, maxCountTilePositions.y] = new List<VoxelTile> { tileToCollapse };

            EnqueueNeighboursToRecalc(maxCountTilePositions);

        }

        Debug.Log("Failed, run out of iteration");
        return false;
    }

    private bool IsTilePossible(VoxelTile tile, Vector2Int position)
    {
        bool isAllRightImpossible = possibleTiles[position.x - 1, position.y]
            .All(rightTile => !CanAppendTile(tile, rightTile, Direction.Right));
        if (isAllRightImpossible) return false;

        bool isAllLeftImpossible = possibleTiles[position.x + 1, position.y]
            .All(leftTile => !CanAppendTile(tile, leftTile, Direction.Left));
        if (isAllLeftImpossible) return false;

        bool isAllForwardImpossible = possibleTiles[position.x, position.y - 1]
            .All(forwardTile => !CanAppendTile(tile, forwardTile, Direction.Forward));
        if (isAllForwardImpossible) return false;

        bool isAllBackImpossible = possibleTiles[position.x, position.y + 1]
            .All(backTile => !CanAppendTile(tile, backTile, Direction.Back));
        if (isAllBackImpossible) return false;


        return true;
    }

    private void PlaceAllTiles()
    {
        for (int x = 1; x < MapSize.x - 1; x++)
            for (int y = 1; y < MapSize.y - 1; y++)
            {
                PlaceTile(x, y);
            }
    }

    private void EnqueueNeighboursToRecalc(Vector2Int positions)
    {
        recalcPossibleTilesQueue.Enqueue(new Vector2Int(positions.x + 1, positions.y));
        recalcPossibleTilesQueue.Enqueue(new Vector2Int(positions.x - 1, positions.y));
        recalcPossibleTilesQueue.Enqueue(new Vector2Int(positions.x, positions.y + 1));
        recalcPossibleTilesQueue.Enqueue(new Vector2Int(positions.x, positions.y - 1));
    }

    private void PlaceTile(int x, int y)
    {

        if (possibleTiles[x, y].Count == 0) return;


        VoxelTile selectedTile = GetRandomTile(possibleTiles[x, y]);

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
