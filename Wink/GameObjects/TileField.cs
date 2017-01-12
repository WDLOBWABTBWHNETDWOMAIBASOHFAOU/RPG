﻿using System;
using System.Runtime.Serialization;

namespace Wink
{
    /*
     * This class came from the TickTick Game.
     */
    [Serializable]
    public class TileField : GameObjectGrid, ICellGrid
    {
        public int xDim
        {
            get
            {
                return Columns;
            }
        }

        public int yDim
        {
            get
            {
                return Rows;
            }
        }

        public TileField(int rows, int columns, int layer = 0, string id = "") : base(rows, columns, layer, id)
        {
            CellWidth = Tile.TileWidth;
            CellHeight = Tile.TileHeight;
        }

        public TileField(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public TileType GetTileType(int x, int y)
        {
            if (x < 0 || x >= Columns)
            {
                return TileType.Normal;
            }
            if (y < 0 || y >= Rows)
            {
                return TileType.Background;
            }
            Tile current = Objects[x, y] as Tile;
            return current.TileType;
        }

        public bool IsWall(int x, int y)
        {
            Tile t = grid[x, y] as Tile;
            return !t.Passable; //TODO, make separate property in Tile that describes whether or not it obstructs line of sight. (!Passable as placeholder) 
        }

        public void SetLight(int x, int y, float distanceSquared)
        {
            //TODO, system to change the visibility of a tile
            Tile t = grid[x, y] as Tile;
            t.Visible = true;
        }
    }
}
