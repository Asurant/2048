using System.Collections;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using NUnit.Framework.Internal;

public class TileBoard : MonoBehaviour
{
    public ManagerGame managerGame;
    public Tile tilePrefab;
    public TileState[] tileStates;
    private TileGrid grid;
    private List<Tile> tiles;
    private bool waiting;

    private void Awake()
    {
        grid = GetComponentInChildren<TileGrid>();
        tiles = new List<Tile>(16);
    }

    public void ClearBoard()
    {
        foreach (var cell in grid.cells)
        {
            cell.tile = null;
        }

        foreach (var tile in tiles)
        {
            Destroy(tile.gameObject);
        }

        tiles.Clear();
    }

    public void CreateTile()
    {
        Tile tile = Instantiate(tilePrefab, grid.transform);

        float roll = UnityEngine.Random.value;
        TileSpecial special = TileSpecial.Normal;

        if (roll < 0.05f)
            special = TileSpecial.Doubler;
        else if (roll < 0.10f)
            special = TileSpecial.Halver;

        tile.SetState(tileStates[0], 2, special);
        tile.Spawn(grid.GetRandomEmptyCell());
        tiles.Add(tile);
    }

    public void Update()
    {
        if (!waiting)
        {
            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                MoveTiles(Vector2Int.up, 0, 1, grid.height - 2, -1);
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                MoveTiles(Vector2Int.left, 1, 1, 0, 1);
            }
            else if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                MoveTiles(Vector2Int.down, 0, 1, 1, 1);
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                MoveTiles(Vector2Int.right, grid.width - 2, -1, 0, 1);
            }
        }
    }

    public void MoveTiles(Vector2Int direction, int StartX, int incrementX, int StartY, int incrementY)
    {
        bool changed = false;

        for (int x = StartX; x >= 0 && x < grid.width; x += incrementX)
        {
            for (int y = StartY; y >= 0 && y < grid.height; y += incrementY)
            {
                TileCell cell = grid.GetCell(x, y);

                if (cell.occupied)
                {
                    changed = MoveTile(cell.tile, direction);
                }
            }
        }
        if (changed)
        {
            StartCoroutine(WaitForChanges());
        }
    }

    private bool MoveTile(Tile tile, Vector2Int direction)
    {
        TileCell newCell = null;
        TileCell adjacent = grid.GetAdjacentCell(tile.cell, direction);

        while (adjacent != null)
        {
            if (adjacent.occupied)
            {
                if (CanMerge(tile, adjacent.tile))
                {
                    Merge(tile, adjacent.tile);
                    return true;
                }
                break;
            }

            newCell = adjacent;
            adjacent = grid.GetAdjacentCell(adjacent, direction);
        }

        if (newCell != null)
        {
            tile.MoveTo(newCell);
            return true;
        }
        return false;
    }

    private IEnumerator WaitForChanges()
    {
        waiting = true;

        yield return new WaitForSeconds(0.1f);

        waiting = false;

        foreach (var tile in tiles)
        {
            tile.locked = false;
        }

        if (tiles.Count != grid.size)
        {
            CreateTile();
        }

        if (CheckForGameOver())
        {
            managerGame.GameOver();
        }
    }

    private bool CheckForGameOver()
    {
        if (tiles.Count != grid.size)
        {
            return false;
        }

        foreach (var tile in tiles)
        {
            TileCell up = grid.GetAdjacentCell(tile.cell, Vector2Int.up);
            TileCell down = grid.GetAdjacentCell(tile.cell, Vector2Int.down);
            TileCell left = grid.GetAdjacentCell(tile.cell, Vector2Int.left);
            TileCell right = grid.GetAdjacentCell(tile.cell, Vector2Int.right);

            if (up != null && CanMerge(tile, up.tile))
            {
                return false;
            }
            if (down != null && CanMerge(tile, down.tile))
            {
                return false;
            }
            if (left != null && CanMerge(tile, left.tile))
            {
                return false;
            }
            if (right != null && CanMerge(tile, right.tile))
            {
                return false;
            }
        }
        return true;
    }

    private bool CanMerge(Tile a, Tile b)
    {
        if (a.locked || b.locked)
            return false;

        if (a.number == b.number || a.special != TileSpecial.Normal || b.special != TileSpecial.Normal)
            return true;

        return false;
    }


    private void Merge(Tile a, Tile b)
    {
        Tile target;
        Tile consumed;

        if (a.special == TileSpecial.Doubler)
        {
            target = b;
            consumed = a;
            target.SetState(target.state, target.number * 2, TileSpecial.Normal);
        }
        else if (b.special == TileSpecial.Doubler)
        {
            target = a;
            consumed = b;
            target.SetState(target.state, target.number * 2, TileSpecial.Normal);
        }
        else if (a.special == TileSpecial.Halver)
        {
            target = b;
            consumed = a;
            target.SetState(target.state, Mathf.Max(2, target.number / 2), TileSpecial.Normal);
        }
        else if (b.special == TileSpecial.Halver)
        {
            target = a;
            consumed = b;
            target.SetState(target.state, Mathf.Max(2, target.number / 2), TileSpecial.Normal);
        }
        else if (a.number == b.number)
        {
            tiles.Remove(a);
            a.Merge(b.cell);
            int number = b.number * 2;
            int index = Mathf.Clamp(IndexOf(b.state) + 1, 0, tileStates.Length - 1);
            b.SetState(tileStates[index], number, TileSpecial.Normal);

            if (number == 2048)
            {
                DestroyAdjacentTiles(b);
            }
            return;
        }
        else
        {
            return;
        }

        tiles.Remove(consumed);
        consumed.Merge(target.cell);
    }




    private void DestroyAdjacentTiles(Tile tile)
    {
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        foreach (var dir in directions)
        {
            TileCell adjacent = grid.GetAdjacentCell(tile.cell, dir);
            if (adjacent != null && adjacent.occupied)
            {
                Tile t = adjacent.tile;
                tiles.Remove(t);
                Destroy(t.gameObject);
                adjacent.tile = null;
            }
        }
    }

    private int IndexOf(TileState state)
    {
        for (int i = 0; i < tileStates.Length; i++)
        {
            if (state == tileStates[i])
            {
                return i;
            }
        }
        return -1;
    }
}
