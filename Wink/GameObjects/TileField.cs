﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wink
{
    /*
     * This class came from the TickTick Game.
     */
    class TileField : GameObjectGrid
    {
        public TileField(int rows, int columns, int layer = 0, string id = "") : base(rows, columns, layer, id)
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
    }
}
