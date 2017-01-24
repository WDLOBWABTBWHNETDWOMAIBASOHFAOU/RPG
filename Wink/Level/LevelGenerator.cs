﻿using Microsoft.Xna.Framework;
using XNAPoint = Microsoft.Xna.Framework.Point;
using System;
using System.Collections.Generic;
using TriangleNet;
using TriangleNet.Geometry;
using System.Linq;
using TriangleNet.Data;
using QuickGraph;
using QuickGraph.Algorithms;

namespace Wink
{
    public static class LevelExtensions
    {
        /// <summary>
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="side"></param>
        /// <returns>The middle of the side of the rectangle specified.</returns>
        public static Vector2 GetMiddleOfSide(this Rectangle rect, Collision.Side side)
        {
            switch (side)
            {
                case Collision.Side.Top:
                    return new Vector2(rect.Center.X, rect.Top);
                case Collision.Side.Bottom:
                    return new Vector2(rect.Center.X, rect.Bottom - 1);
                case Collision.Side.Left:
                    return new Vector2(rect.Left, rect.Center.Y);
                case Collision.Side.Right:
                    return new Vector2(rect.Right - 1, rect.Center.Y);
                default:
                    return Vector2.Zero;
            }
        }

        /// <summary>
        /// Use this to convert a Vector2 to a Point by rounding the component floating point values rather than casting.
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static XNAPoint ToRoundedPoint(this Vector2 vector)
        {
            return new XNAPoint((int)Math.Round(vector.X), (int)Math.Round(vector.Y));
        }
    }

    public partial class Level
    {
        /// <summary>
        /// Class to represent Rooms before the TileField is generated.
        /// </summary>
        private class Room : ICloneable
        {
            public Room(Vector2 location, XNAPoint size)
            {
                Location = location;
                Size = size;
            }

            public Vector2 Location { get; set; }
            public XNAPoint Size { get; set; }

            //public Vector2 Velocity { get; set; }
            public Rectangle BoundingBox
            {
                get
                {
                    return new Rectangle(Location.ToRoundedPoint(), Size);
                }
            }

            public object Clone()
            {
                return new Room(Location, Size);
            }
        }
        
        Random Random { get { return Treehugger.Random; } }

        //These are the minimum and maximum width and height of any room
        const int minDim = 5;
        const int maxDim = 13;

        //Values for the Guassian Distribution
        int gaussMean = (minDim + maxDim) / 2;
        const double gaussVariance = 5d;

        const double TargetSurfaceArea = 750;

        //Radius of the circle in which the rooms are placed.
        const double circleRadius = 7;
        double CircleArea
        {
            get { return Math.PI * Math.Pow(circleRadius, 2); }
        }

        /// <summary>
        /// Gaussian/Normal Distribution Function
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        double GaussianDistribution(double x)
        {
            double a = 1 / (Math.Sqrt(2 * gaussVariance * Math.PI));
            double b = -Math.Pow(x - gaussMean, 2) / 2 * gaussVariance;
            return a * Math.Pow(Math.E, b);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        double GaussianDistribution(Vector2 v)
        {
            return (GaussianDistribution(v.X) + GaussianDistribution(v.Y)) / 2;
        }

        /// <summary>
        /// Method to get a random point in a circle.
        /// Source: http://www.gamasutra.com/blogs/AAdonaac/20150903/252889/Procedural_Dungeon_Generation_Algorithm.php
        /// </summary>
        /// <returns></returns>
        Vector2 GetRandomPointInCircle()
        {
            double t = 2 * Math.PI * Random.NextDouble();
            double u = Random.NextDouble() + Random.NextDouble();
            double r;
            if (u > 1)
                r = 2;
            else
                r = u;

            return new Vector2((float)(circleRadius * r * Math.Cos(t)), (float)(circleRadius * r * Math.Sin(t)));
        }

        /// <summary>
        /// Generates a List of randomly sized and placed Rooms that do not overlap.
        /// </summary>
        /// <returns></returns>
        private List<Room> GenerateRooms()
        {
            //For every possible room size multiply the area by the likelihood of their inclusion and add it to the total.
            double averageTotalArea = 0;
            for (int x = minDim; x <= maxDim; x++)
            {
                for (int y = minDim; y <= maxDim; y++)
                {
                    averageTotalArea += GaussianDistribution(new Vector2(x, y)) * x * y;
                }
            }
            
            double multiplier = 100 * CircleArea / averageTotalArea;

            //Make a list in which the possible room sizes occur as often as specified by the Gaussian distribution.
            List<Room> allRooms = new List<Room>();
            for (int x = minDim; x <= maxDim; x++)
            {
                for (int y = minDim; y <= maxDim; y++)
                {
                    XNAPoint p = new XNAPoint(x, y);
                    double gaussianDistribution = GaussianDistribution(p.ToVector2());
                    int roomAmount = (int)(multiplier * gaussianDistribution);
                    for (int i = 0; i < roomAmount; i++)
                    {
                        allRooms.Add(new Room(new Vector2(), p));
                    }
                }
            }

            //Select the rooms that are going to be used, by randomly selecting from the list until the target surface area is reached.
            List<Room> roomSelection = new List<Room>();
            int totalRoomArea = 0;
            while (totalRoomArea < TargetSurfaceArea)
            {
                int randomIndex = Random.Next(allRooms.Count);
                Room toAdd = allRooms[randomIndex].Clone() as Room;
                Vector2 pointInCircle = GetRandomPointInCircle();
                toAdd.Location = pointInCircle - toAdd.Size.ToVector2() / 2;
                roomSelection.Add(toAdd);

                totalRoomArea += toAdd.Size.X * toAdd.Size.Y;
            }

            //A simple physics simulation that pushes rooms apart.
            int collisions = int.MaxValue;
            XNAPoint buffer = new XNAPoint(2, 2);
            while (collisions > 0)
            {
                collisions = 0;
                for (int i = 0; i < roomSelection.Count; i++)
                {
                    Room room1 = roomSelection[i];
                    Rectangle r1 = new Rectangle(room1.BoundingBox.Location + buffer, room1.BoundingBox.Size + buffer + buffer);
                    for (int j = i + 1; j < roomSelection.Count; j++)
                    {
                        Room room2 = roomSelection[j];
                        Rectangle r2 = new Rectangle(room2.BoundingBox.Location + buffer, room2.BoundingBox.Size + buffer + buffer);
                        if (room1 != room2 && r1.Intersects(r2))
                        {
                            Rectangle intersection = Rectangle.Intersect(r1, r2);
                            room1.Location += new Vector2((float)Random.NextDouble() / 10) + (r1.Center.ToVector2() - intersection.Center.ToVector2()) / 8;
                            room2.Location += new Vector2((float)Random.NextDouble() / 10) + (r2.Center.ToVector2() - intersection.Center.ToVector2()) / 8;
                            collisions++;
                        }
                    }
                }
            }

            //Get the lowest coordinates so we can adjust and bring everything into the positive.
            float lowestX = float.MaxValue;
            float lowestY = float.MaxValue;
            foreach (Room r in roomSelection)
            {
                if (r.Location.X < lowestX)
                    lowestX = r.Location.X;
                if (r.Location.Y < lowestY)
                    lowestY = r.Location.Y;
            }
            foreach (Room r in roomSelection)
            {
                r.Location -= new Vector2(lowestX - 1, lowestY - 2);
            }

            return roomSelection;
        }

        private List<Tuple<Room, Room>> GenerateHallwayPairs(List<Room> rooms)
        {
            //Add the center of each room to an InputGeometry, in order to use the data in the TriangleNet library.
            InputGeometry ig = new InputGeometry();
            foreach (Room r in rooms)
            {
                double centerX = r.BoundingBox.Center.X;
                double centerY = r.BoundingBox.Center.Y;
                ig.AddPoint(centerX, centerY);
            }

            //use Triangulate method to generate Delaunay triangulation based on the points added to the InputGeometry.
            Mesh mesh = new Mesh();
            mesh.Triangulate(ig);

            //List of vertices.
            List<Vertex> vertices = mesh.Vertices.ToList();

            //Add all the vertices and edges to a QuickGraph(library) graph.
            UndirectedGraph<int, TaggedUndirectedEdge<int, string>> graph = new UndirectedGraph<int, TaggedUndirectedEdge<int, string>>();
            for (int i = 0; i < vertices.Count; i++)
                graph.AddVertex(i);

            foreach (Edge e in mesh.Edges)
                graph.AddEdge(new TaggedUndirectedEdge<int, string>(e.P0, e.P1, ""));
            
            //Use QuickGraph to find the Minimum Spanning Tree using the distance as the weight.
            var mst = graph.MinimumSpanningTreePrim(edge =>
            {
                double dx = vertices[edge.Source].X - vertices[edge.Target].X;
                double dy = vertices[edge.Source].Y - vertices[edge.Target].Y;
                return Math.Sqrt(Math.Pow(dx, 2) + Math.Pow(dy, 2));
            }).ToList();

            //Randomly add back 10 percent of edges.
            int tenPercent = graph.EdgeCount / 10;
            List<TaggedUndirectedEdge<int, string>> allEdges = graph.Edges.ToList();
            for (int i = 0; i < tenPercent; i++)
            {
                int randomEdge = Random.Next(graph.EdgeCount);
                if (!mst.Contains(allEdges[randomEdge]))
                {
                    mst.Add(allEdges[randomEdge]);
                }
            }

            //Convert mst to a list of Room pairs.
            List<Tuple<Room, Room>> roomPairs = new List<Tuple<Room, Room>>();
            foreach (TaggedUndirectedEdge<int, string> edge in mst)
            {
                Room r1 = null;
                Room r2 = null;
                foreach(Room r in rooms)
                {
                    if (r.BoundingBox.Center.X == vertices[edge.Source].X && r.BoundingBox.Center.Y == vertices[edge.Source].Y)
                    {
                        r1 = r;
                    }
                    if (r.BoundingBox.Center.X == vertices[edge.Target].X && r.BoundingBox.Center.Y == vertices[edge.Target].Y)
                    {
                        r2 = r;
                    }
                }
                Tuple<Room, Room> roomPair = new Tuple<Room, Room>(r1, r2);
                roomPairs.Add(roomPair);
            }
            return roomPairs;
        }

        private TileField GenerateTiles(List<Room> rooms, List<Tuple<Room,Room>> hallwayPairs)
        {
            //Get the highest coordinates so we know the size of the TileField.
            int highestX = int.MinValue;
            int highestY = int.MinValue;
            foreach (Room r in rooms)
            {
                if (r.BoundingBox.Right > highestX)
                    highestX = r.BoundingBox.Right;
                if (r.BoundingBox.Bottom > highestY)
                    highestY = r.BoundingBox.Bottom;
            }
            
            //Make the tilefield and fill with default Tiles.
            TileField tf = new TileField(highestY + 1, highestX + 1, 0, "TileField");
            Add(tf);
            for (int x = 0; x < tf.Columns; x++)
            {
                for (int y = 0; y < tf.Rows; y++)
                {
                    tf.Add(new Tile(), x, y);
                }
            }

            List<Tuple<XNAPoint, XNAPoint>> hallways = new List<Tuple<XNAPoint, XNAPoint>>();

            //Find good points for the hallways connect to.
            foreach (Tuple<Room, Room> pair in hallwayPairs)
            {
                Vector2 center1 = pair.Item1.BoundingBox.Center.ToVector2();
                Vector2 center2 = pair.Item2.BoundingBox.Center.ToVector2();

                //Vector from center of room1 to center of room2 and vice versa
                Vector2 v1 = center2 - center1;
                Vector2 v2 = center1 - center2;

                Collision.Side s1 = CalculateExitSide(pair.Item1, v1);
                Collision.Side s2 = CalculateExitSide(pair.Item2, v2);

                v1.Normalize();
                v2.Normalize();

                XNAPoint cRelExit1 = CalculateExit(v1, center1, s1, pair.Item1.BoundingBox.GetMiddleOfSide(s1)).ToPoint();
                XNAPoint cRelExit2 = CalculateExit(v2, center2, s2, pair.Item2.BoundingBox.GetMiddleOfSide(s2)).ToPoint();
                XNAPoint absExit1 = cRelExit1 + pair.Item1.BoundingBox.Center;
                XNAPoint absExit2 = cRelExit2 + pair.Item2.BoundingBox.Center;

                hallways.Add(new Tuple<XNAPoint, XNAPoint>(absExit1, absExit2));
            }
            
            //for each room, add floor tiles to the tilefield.
            for (int i = 0; i < rooms.Count; i++)
            {
                Room r = rooms[i];
                int width = r.BoundingBox.Width;
                int height = r.BoundingBox.Height;

                XNAPoint relCenter = r.BoundingBox.Center - r.BoundingBox.Location;

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        Tile tile = LoadFloorTile();
                        if (x == relCenter.X && y == relCenter.Y)
                        {
                            tile.AddDebugTag("Room", ""+i);
                        }
                        tf.Add(tile, x + r.BoundingBox.X, y + r.BoundingBox.Y);
                    }
                }
            }

            //Generate hallways
            foreach (Tuple<XNAPoint, XNAPoint> pair in hallways)
            {
                //Use the pathfinder to get a path from exitpoint to exitpoint.
                PathFinder pf = new PathFinder(tf);
                pf.EnableStraightLines();
                List<Tile> path = pf.ShortestPath(tf[pair.Item1.X, pair.Item1.Y] as Tile, tf[pair.Item2.X, pair.Item2.Y] as Tile, tile => true);

                Tile t = tf[pair.Item1.X, pair.Item1.Y] as Tile;
                t.AddDebugTag("ExitConnectionPoint", pair.Item2.X + "," + pair.Item2.Y);
                path.Add(t);

                //Add a floor tile for every tile in the path.
                foreach (Tile tile in path)
                {
                    Tile newTile = LoadFloorTile();
                    newTile.AddDebugTags(tile.DebugTags);
                    tf.Add(newTile, tile.TilePosition.X, tile.TilePosition.Y);
                }
            }

            //Exchange all remaining background tiles for Wall tiles.
            for (int x = 0; x < tf.Columns; x++)
            {
                for (int y = 0; y < tf.Rows; y++)
                {
                    Tile currentTile = tf[x, y] as Tile;
                    Tile aboveTile = tf[x, y - 1] as Tile;
                    if (currentTile != null && aboveTile != null && currentTile.TileType == TileType.Background)
                    {
                        Tile newTile = LoadWallTile(x, y);
                        newTile.AddDebugTags(currentTile.DebugTags);
                        tf.Add(newTile, x, y);
                    }
                }
            }

            //Add starttiles
            for (int p = 0; p < 4; p++)
                tf.Add(LoadStartTile(p + 1), rooms[0].Location.ToRoundedPoint().X + 1 + p % 2, rooms[0].Location.ToRoundedPoint().Y + 1 + p / 2);

            //Generate EndTile
            List<Room> forEnd = new List<Room>();
            List<Room> usedRooms = new List<Room>();
            for (int a = 0; a < hallwayPairs.Count; a++)
            {
                if (!forEnd.Contains(hallwayPairs[a].Item1))
                {
                    forEnd.Add(hallwayPairs[a].Item1);
                    usedRooms.Add(hallwayPairs[a].Item1);
                }
                if (!forEnd.Contains(hallwayPairs[a].Item2))
                {
                    forEnd.Add(hallwayPairs[a].Item2);
                    usedRooms.Add(hallwayPairs[a].Item2);
                }
            }

            for (int a = 0; a < hallwayPairs.Count; a++)
            {
                if (hallwayPairs[a].Item1 == rooms[0] || hallwayPairs[a].Item2 == rooms[0])
                {
                    if (forEnd.Contains(hallwayPairs[a].Item1))
                    {
                        forEnd.Remove(hallwayPairs[a].Item1);
                    }
                    if (forEnd.Contains(hallwayPairs[a].Item2))
                    {
                        forEnd.Remove(hallwayPairs[a].Item2);
                    }
                }
            }
            tf.Add(LoadEndTile(), forEnd[0].Location.ToRoundedPoint().X + 1, forEnd[0].Location.ToRoundedPoint().Y + 1);
            tf.Add(LoadChestTile(levelIndex), forEnd[0].Location.ToRoundedPoint().X + forEnd[0].Size.X - 2, forEnd[0].Location.ToRoundedPoint().Y + 1);
            
            //Door spawn
            for (int i = 0; i < usedRooms.Count; i++)
            {
                for (int x = rooms[i].Location.ToRoundedPoint().X; x < rooms[i].Location.ToRoundedPoint().X + rooms[i].Size.X; x++)
                {
                    if (!tf.IsWall(x, rooms[i].Location.ToRoundedPoint().Y - 1))
                    {
                        if (tf.IsWall(x + 1, rooms[i].Location.ToRoundedPoint().Y - 1) && tf.IsWall(x - 1, rooms[i].Location.ToRoundedPoint().Y - 1))
                        {
                            tf.Add(LoadDoorTile(), x, rooms[i].Location.ToRoundedPoint().Y - 1);
                        }
                    }
                    if (!tf.IsWall(x, rooms[i].Location.ToRoundedPoint().Y + rooms[i].Size.Y))
                    {
                        if (tf.IsWall(x + 1, rooms[i].Location.ToRoundedPoint().Y + rooms[i].Size.Y) && tf.IsWall(x - 1, rooms[i].Location.ToRoundedPoint().Y + rooms[i].Size.Y))
                        {
                            tf.Add(LoadDoorTile(), x, rooms[i].Location.ToRoundedPoint().Y + rooms[i].Size.Y);
                        }
                    }
                }
            }
            
            //Test chest spawn
            int chestAmount = Random.Next(1, 2);
            usedRooms.Remove(rooms[0]);
            for (int i = 0; i < chestAmount; i++)
            {
                int chestRoom = Random.Next(usedRooms.Count);
                usedRooms.Remove(rooms[chestRoom]);
                int xChest = rooms[chestRoom].Location.ToRoundedPoint().X + rooms[chestRoom].Size.X / 2;
                int yChest = rooms[chestRoom].Location.ToRoundedPoint().Y + rooms[chestRoom].Size.Y / 2;
                tf.Add(LoadChestTile(levelIndex), xChest, yChest);
            }
            //End test

            //Test enemy spawn
            int numberOfEnemys = 8;
            for (int n = 0; n < numberOfEnemys; n++)
            {
                Enemy enemy = new Enemy(0, Index, EnemyType.random, "Enemy");
                List<GameObject> spawnLocations = tf.FindAll(obj => obj is Tile && (obj as Tile).Passable && !(obj as Tile).Blocked);
                Tile spawnLocation = spawnLocations[GameEnvironment.Random.Next(spawnLocations.Count)] as Tile;
                spawnLocation.PutOnTile(enemy);
            }
            //End test            

            //Must be last statement, executed after the Tilefield is done.
            tf.InitSpriteSheetIndexation();
            return tf;
        }

        private Collision.Side CalculateExitSide(Room r, Vector2 relVector)
        {
            Vector2 center = r.BoundingBox.Center.ToVector2();

            //Calculate angles of vector.
            double relAngle = Math.Atan2(relVector.Y, relVector.X);

            //Calculate the angles of the line 
            Vector2[] cornerVectors = new Vector2[4];
            cornerVectors[0] = new Vector2(r.BoundingBox.Left, r.BoundingBox.Top) - center;
            cornerVectors[1] = new Vector2(r.BoundingBox.Right, r.BoundingBox.Top) - center;
            cornerVectors[2] = new Vector2(r.BoundingBox.Right, r.BoundingBox.Bottom) - center;
            cornerVectors[3] = new Vector2(r.BoundingBox.Left, r.BoundingBox.Bottom) - center;
            //Use Math.Atan2 to convert vectors to angles.
            double[] cornerAngles = new double[4];
            cornerAngles[0] = Math.Atan2(cornerVectors[0].Y, cornerVectors[0].X);
            cornerAngles[1] = Math.Atan2(cornerVectors[1].Y, cornerVectors[1].X);
            cornerAngles[2] = Math.Atan2(cornerVectors[2].Y, cornerVectors[2].X);
            cornerAngles[3] = Math.Atan2(cornerVectors[3].Y, cornerVectors[3].X);

            //Calculate Sides based on angle
            Collision.Side s = default(Collision.Side);
            if (relAngle >= cornerAngles[0] && relAngle < cornerAngles[1])
                s = Collision.Side.Top;
            else if (relAngle >= cornerAngles[1] && relAngle < cornerAngles[2])
                s = Collision.Side.Right;
            else if (relAngle >= cornerAngles[2] && relAngle < cornerAngles[3])
                s = Collision.Side.Bottom;
            else if (relAngle >= cornerAngles[3] || relAngle < cornerAngles[0])
                s = Collision.Side.Left;

            return s;
        }

        private Vector2 CalculateExit(Vector2 direction, Vector2 center, Collision.Side s, Vector2 middleOfSide)
        {
            Vector2 exitPoint = middleOfSide - center;
            switch (s)
            {
                case Collision.Side.Top:
                case Collision.Side.Bottom:
                    exitPoint.X = direction.X * (exitPoint.Y / direction.Y);
                    break;
                case Collision.Side.Left:
                case Collision.Side.Right:
                    exitPoint.Y = direction.Y * (exitPoint.X / direction.X);
                    break;
            }

            return exitPoint;
        }
    }
}