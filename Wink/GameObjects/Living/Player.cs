﻿using System;
using Microsoft.Xna.Framework;
using System.Runtime.Serialization;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;

namespace Wink
{
    [Serializable]
    public class Player : Living, IGameObjectContainer, IGUIGameObject
    {
        public static string LocalPlayerName
        {
            get { return "player_" + GameEnvironment.GameSettingsManager.GetValue("user_name"); }
        }

        protected int exp;

        private MouseSlot mouseSlot;
        public MouseSlot MouseSlot { get { return mouseSlot; } }
        
        public override Point PointInTile
        {
            get { return new Point(Tile.TileWidth / 2, Tile.TileHeight / 2); }
        }

        public Player(string clientName, int layer,float FOVlength=8.5f) : base(layer, "player_" + clientName, FOVlength)
        {
            //Inventory
            mouseSlot = new MouseSlot(layer + 11, "mouseSlot");            
            SetStats();
            InitAnimation(); //not sure if overriden version gets played right without restating
        }

        protected override void DoBehaviour(List<GameObject> changedObjects)
        {
            Debug.WriteLine("Called Player.DoBehaviour, but players don't have automated behaviour.");
        }

        #region Serialization
        public Player(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            exp = info.GetInt32("exp");
            if (context.GetVars().GUIDSerialization)
                mouseSlot = context.GetVars().Local.GetGameObjectByGUID(Guid.Parse(info.GetString("mouseSlotGUID"))) as MouseSlot;
            else
                mouseSlot = info.GetValue("mouseSlot", typeof(MouseSlot)) as MouseSlot;
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (context.GetVars().GUIDSerialization)
                info.AddValue("mouseSlotGUID", mouseSlot.GUID.ToString());
            else
                info.AddValue("mouseSlot", mouseSlot);

            info.AddValue("exp", exp);
            base.GetObjectData(info, context);
        }
        #endregion

        public override void Replace(GameObject replacement)
        {
            if (mouseSlot != null && mouseSlot.GUID == replacement.GUID)
                mouseSlot = replacement as MouseSlot;

            base.Replace(replacement);
        }

        protected override void InitAnimation(string idleColor = "player")
        {            
            base.InitAnimation(idleColor);
            PlayAnimation("idle");
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            mouseSlot.Update(gameTime);

            if (exp >= RequiredExperience())
            {
                LevelUp();
            }
            TileField tf = GameWorld.Find("TileField") as TileField;
            FOVpos = Position - Origin;
            foreach (Tile t in tf.Objects) // darken the tiles out of range
            {
                t.Visible = false;
            }
            ShadowCast.ComputeVisibility(tf, (int)(FOVpos.X) / tf.CellWidth, (int)(FOVpos.Y) / tf.CellHeight, FOVlength);
            //skill idea: peek corner, allows the player to move its FOV position 1 tile in N,S,E or W direction,
            //allowing the player to peek around a corner into a halway whithout actualy stepping out
        }

        /// <summary>
        /// returns the experience a creature requires for its next level.
        /// </summary>
        /// <returns></returns>
        protected int RequiredExperience()
        {
            double mod = 36.79;
            double pow = Math.Pow(Math.E, Math.Sqrt(creatureLevel));
            int reqExp = (int)(mod * pow);
            return reqExp;
        }

        protected void LevelUp()
        {
            exp -= RequiredExperience();
            creatureLevel++;

            // + some amount of neutral stat points, distriputed by user discresion or increase stats based on picked hero
        }

        public override void HandleInput(InputHelper inputHelper)
        {
            Action onClick = () =>
            {
                Event e = new EndTurnEvent(this);
                Server.Send(e);
            };
            inputHelper.IfMouseLeftButtonPressedOn(Tile, onClick);
            base.HandleInput(inputHelper);
        }

        public override GameObject Find(Func<GameObject, bool> del)
        {
            if (del.Invoke(mouseSlot))
                return mouseSlot;

            return mouseSlot.Find(del) ?? base.Find(del);
        }

        public override List<GameObject> FindAll(Func<GameObject, bool> del)
        {
            List<GameObject> result = new List<GameObject>();
            if (del.Invoke(mouseSlot))
                result.Add(mouseSlot);

            result.AddRange(mouseSlot.FindAll(del));
            result.AddRange(base.FindAll(del));
            return result;
        }

        public void InitGUI(Dictionary<string, object> guiState)
        {
            if (Id == LocalPlayerName)
            {
                PlayingGUI gui = GameWorld.Find("PlayingGui") as PlayingGUI;
                SpriteFont textfieldFont = GameEnvironment.AssetManager.GetFont("Arial26");

                const int barX = 150;
                Vector2 HPBarPosition = new Vector2(barX, 14);
                Vector2 MPBarPosition = new Vector2(barX, HPBarPosition.Y + 32);

                //Healthbar
                Bar<Player> hpBar = new Bar<Player>(this, p => p.Health, p => p.MaxHealth, textfieldFont, Color.Red, 2, "HealthBar", 0, 2.5f);
                hpBar.Position = new Vector2(HPBarPosition.X, HPBarPosition.Y);
                gui.Add(hpBar);

                //Manabar
                Bar<Player> mpBar = new Bar<Player>(this, p => p.Mana, p => p.MaxMana, textfieldFont, Color.Blue, 2, "ManaBar", 0, 2.5f);
                mpBar.Position = new Vector2(MPBarPosition.X, MPBarPosition.Y);
                gui.Add(mpBar);

                //Action Points
                Bar<Player> apBar = new Bar<Player>(this, p => p.ActionPoints, p => MaxActionPoints, textfieldFont, Color.Yellow, 2, "ActionBar", 0, 2.5f);
                int screenWidth = GameEnvironment.Screen.X;
                Vector2 APBarPosition = new Vector2(screenWidth - barX - apBar.Width, HPBarPosition.Y);
                apBar.Position = new Vector2(APBarPosition.X, APBarPosition.Y);
                gui.Add(apBar);

                PlayerInventoryAndEquipment pie = new PlayerInventoryAndEquipment(Inventory, EquipmentSlots);
                pie.Position = new Vector2(screenWidth - pie.Width, 300);
                pie.Visible = guiState.ContainsKey("playerI&EVisibility") ? (bool)guiState["playerI&EVisibility"] : false;
                gui.Add(pie);

                gui.Add(mouseSlot);
            }
        }

        public void CleanupGUI(Dictionary<string, object> guiState)
        {
            if (Id == LocalPlayerName)
            {
                PlayingGUI gui = GameWorld.Find("PlayingGui") as PlayingGUI;
                PlayerInventoryAndEquipment pIaE = gui.Find(obj => obj is PlayerInventoryAndEquipment) as PlayerInventoryAndEquipment;
                guiState["playerI&EVisibility"] = pIaE.Visible;

                gui.RemoveImmediatly(gui.Find("HealthBar"));
                gui.RemoveImmediatly(gui.Find("ManaBar"));
                gui.RemoveImmediatly(gui.Find("ActionBar"));
                gui.RemoveImmediatly(pIaE);
                gui.RemoveImmediatly(mouseSlot);
            }
        }
    }
}
