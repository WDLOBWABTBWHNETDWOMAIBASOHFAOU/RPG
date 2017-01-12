﻿using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace Wink
{
    [Serializable]
    public abstract partial class Living : AnimatedGameObject, ITileObject
    {
        private int timeleft;
        private bool startTimer;
        //public bool isTurn;

        protected string idleAnimation, moveAnimation, dieAnimation;
        private string dieSound;
        
        public Tile Tile
        {
            get
            {
                if (parent != null)
                    return parent.Parent as Tile;
                else
                    return null;
            }
        }

        public virtual Point PointInTile
        {
            get { return new Point(Tile.TileWidth / 2, Tile.TileHeight); }
        }

        public virtual bool BlocksTile
        {
            get { return true; }
        }

        public Living(int layer = 0, string id = "", float scale = 1.0f) : base(layer, id, scale)
        {
            SetStats();
            InitAnimation();
            timeleft = 1000;
        }

        #region Serialization
        public Living(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            timeleft = info.GetInt32("timeleft");
            startTimer = info.GetBoolean("startTimer");
            //isTurn = info.GetBoolean("isTurn");

            idleAnimation = info.GetString("idleAnimation");
            moveAnimation = info.GetString("moveAnimation");
            dieAnimation = info.GetString("dieAnimation");
            dieSound = info.GetString("dieSound");

            //tile = info.GetValue("tile", typeof(Tile)) as Tile;

            manaPoints = info.GetInt32("manaPoints");
            healthPoints = info.GetInt32("healthPoints");
            actionPoints = info.GetInt32("actionPoints");
            baseAttack = info.GetInt32("baseAttack");
            strength = info.GetInt32("strength");
            dexterity = info.GetInt32("dexterity");
            intelligence = info.GetInt32("intelligence");
            creatureLevel = info.GetInt32("creatureLevel");
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("timeleft", timeleft);
            info.AddValue("startTimer", startTimer);
            //info.AddValue("isTurn", isTurn);

            info.AddValue("idleAnimation", idleAnimation);
            info.AddValue("moveAnimation", moveAnimation);
            info.AddValue("dieAnimation", dieAnimation);
            info.AddValue("dieSound", dieSound);

            //info.AddValue("tile", tile);

            info.AddValue("manaPoints", manaPoints);
            info.AddValue("healthPoints", healthPoints);
            info.AddValue("actionPoints", actionPoints);
            info.AddValue("baseAttack", baseAttack);
            info.AddValue("strength", strength);
            info.AddValue("dexterity", dexterity);
            info.AddValue("intelligence", intelligence);
            info.AddValue("creatureLevel", creatureLevel);
        }
        #endregion

        public List<GameObject> DoAllBehaviour()
        {
            List<GameObject> changedObjects = new List<GameObject>();
            if (Health > 0)
            {
                int previousActionPoints = int.MinValue;
                while (actionPoints > 0 && actionPoints != previousActionPoints)
                {
                    previousActionPoints = actionPoints;
                    DoBehaviour(changedObjects);
                }
            }
            else
                actionPoints = 0;
            return changedObjects;
        }

        protected abstract void DoBehaviour(List<GameObject> changedObjects);

        protected virtual void InitAnimation(string idleColor = "empty:64:64:10:Magenta")
        {
            //General animations
            idleAnimation = idleColor;
            moveAnimation = "empty:64:64:10:DarkBlue";
            dieAnimation = "empty:64:64:10:LightBlue";
            LoadAnimation(idleAnimation, "idle", true);
            LoadAnimation(moveAnimation, "move", true, 0.05f);
            LoadAnimation(dieAnimation, "die", false);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if(healthPoints <= 0)
            {
                startTimer = true;
                DeathFeedback("die", dieSound);
                if (startTimer)
                {
                    if (timeleft <= 0)
                        Death();
                    else
                        timeleft -= gameTime.TotalGameTime.Seconds;
                }
            }
        }
        
        public virtual void MoveTo(Tile t)
        {
            if (Tile != null)
                Tile.Remove(this);

            t.PutOnTile(this);
        }
    }
}
