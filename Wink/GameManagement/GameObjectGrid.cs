﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Wink;

[Serializable]
public class GameObjectGrid : GameObject, IGameObjectContainer
{
    protected GameObject[,] grid;
    protected int cellWidth, cellHeight;

    public GameObjectGrid(int rows, int columns, int layer = 0, string id = "") : base(layer, id)
    {
        grid = new GameObject[columns, rows];
    }

    #region Serialization
    public GameObjectGrid(SerializationInfo info, StreamingContext context) : base(info, context)
    {
        SerializationHelper.Variables vars = context.Context as SerializationHelper.Variables;
        
        grid = (GameObject[,])info.GetValue("grid", typeof(GameObject[,]));
        
        cellWidth = info.GetInt32("cellWidth");
        cellHeight = info.GetInt32("cellHeight");
    }

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue("grid", grid);
        
        info.AddValue("cellWidth", cellWidth);
        info.AddValue("cellHeight", cellHeight);
        base.GetObjectData(info, context);
    }
    #endregion

    public override void Replace(GameObject replacement)
    {
        for (int x = 0; x < Columns; x++)
        {
            for (int y = 0; y < Rows; y++)
            {
                GameObject go = grid[x, y];
                if (go != null)
                {
                    go.Replace(replacement);
                    if (go.GUID == replacement.GUID)
                        grid[x, y] = replacement;
                }
            }
        }
        base.Replace(replacement);
    }

    public virtual void Add(GameObject obj, int x, int y)
    {
        if (obj != null)
        {
            grid[x, y] = obj;
            obj.Parent = this;
            obj.Position = new Vector2(x * cellWidth, y * cellHeight);
        }
    }
    public void Remove(int x, int y)
    {
        grid[x, y] = null;
    }

    public GameObject this[int x, int y]
    {
        get
        {
            return Get(x, y);
        }
    }

    public GameObject Get(int x, int y)
    {
        if (x >= 0 && x < grid.GetLength(0) && y >= 0 && y < grid.GetLength(1))
        {
            return grid[x, y];
        }
        else
        {
            return null;
        }
    }
    
    public GameObject[,] Objects
    {
        get
        {
            return grid;
        }
    }

    public Vector2 GetAnchorPosition(GameObject s)
    {
        for (int x = 0; x < Columns; x++)
        {
            for (int y = 0; y < Rows; y++)
            {
                if (grid[x, y] == s)
                {
                    return new Vector2(x * cellWidth, y * cellHeight);
                }
            }
        }
        return Vector2.Zero;
    }
    
    public int Rows
    {
        get { return grid.GetLength(1); }
    }
    
    public int Columns
    {
        get { return grid.GetLength(0); }
    }
    
    public int CellWidth
    {
        get { return cellWidth; }
        set { cellWidth = value; }
    }
    
    public int CellHeight
    {
        get { return cellHeight; }
        set { cellHeight = value; }
    }

    public override void HandleInput(InputHelper inputHelper)
    {
        foreach (GameObject obj in grid)
        {
            if (obj != null)
            {
                obj.HandleInput(inputHelper);
            }
        }
    }

    public override void Update(GameTime gameTime)
    {
        foreach (GameObject obj in grid)
        {
            if (obj != null)
            {
                obj.Update(gameTime);
            }
        }
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch, Camera camera)
    {
        for (int x = 0; x < Columns; x++)
        {
            for (int y = 0; y < Rows; y++)
            {
                GameObject obj = this[x, y];
                if (obj != null)
                {
                    obj.Draw(gameTime, spriteBatch, camera);
                }
            }
        }
    }

    public override void DrawDebug(GameTime gameTime, SpriteBatch spriteBatch, Camera camera)
    {
        foreach (GameObject obj in grid)
        {
            if (obj != null)
            {
                obj.DrawDebug(gameTime, spriteBatch, camera);
            }
        }
    }

    public override void Reset()
    {
        base.Reset();
        foreach (GameObject obj in grid)
        {
            obj.Reset();
        }
    }

    public GameObject Find(Func<GameObject, bool> del)
    {
        foreach (GameObject obj in grid)
        {
            if (obj != null)
            {
                if (del.Invoke(obj))
                {
                    return obj;
                }
                else if (obj is IGameObjectContainer)
                {
                    IGameObjectContainer objContainer = obj as IGameObjectContainer;
                    GameObject subObj = objContainer.Find(del);
                    if (subObj != null)
                    {
                        return subObj;
                    }
                }
            }
        }
        return null;
    }

    public List<GameObject> FindAll(Func<GameObject, bool> del)
    {
        List<GameObject> result = new List<GameObject>();
        foreach (GameObject obj in grid)
        {
            if (obj != null)
            {
                if (del.Invoke(obj))
                {
                    result.Add(obj);
                }
                if (obj is IGameObjectContainer)
                {
                    IGameObjectContainer objContainer = obj as IGameObjectContainer;
                    result.AddRange(objContainer.FindAll(del));
                }
            }
        }
        return result;
    }

    public override Rectangle BoundingBox
    {
        get
        {
            return new Rectangle((int)GlobalPosition.X, (int)GlobalPosition.Y, Columns*CellWidth, Rows*CellHeight);
        }
    }
}
