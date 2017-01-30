﻿using System;
using Microsoft.Xna.Framework;
using System.Runtime.Serialization;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Wink
{

    public enum PlayerType { warrior, archer, mage, random }
    [Serializable]
    public class Player : Living, IGameObjectContainer, IGUIGameObject
    {
        public static string LocalPlayerName
        {
            get { return "player_" + GameEnvironment.GameSettingsManager.GetValue("user_name"); }
        }

        protected int exp;
        public int freeStatPoints;
        private TextGameObject playerNameTitle;

        private MouseSlot mouseSlot;
        public MouseSlot MouseSlot
        {
            get { return mouseSlot; }
        }
        
        public override Point PointInTile
        {
            get { return new Point(Tile.TileWidth / 2, Tile.TileHeight / 2); }
        }

        public Player(string clientName, int layer, PlayerType playerType, float FOVlength = 8.5f) : base(layer, "player_" + clientName, FOVlength)
        {
            //Inventory
            mouseSlot = new MouseSlot(layer + 11, "mouseSlot");  
            SetupType(playerType);
        }

        private void PlayerNameTitle()
        {
            playerNameTitle = new TextGameObject("Arial26", 1, 0, "playerName" + guid.ToString());
            playerNameTitle.Text = Id.Split('_')[1];
            playerNameTitle.Color = Color.Red;
            playerNameTitle.Position = GlobalPosition - new Vector2((playerNameTitle.Size.X / 2), 64 + playerNameTitle.Size.Y);
        }

        private void SetupType(PlayerType ptype)
        {
            if (ptype == PlayerType.random)
            {
                //select random armorType
                Array pTypeValues = Enum.GetValues(typeof(PlayerType));
                ptype = (PlayerType)pTypeValues.GetValue(GameEnvironment.Random.Next(pTypeValues.Length - 1));
            }

            RestrictedItemSlot weaponslot = EquipmentSlots.Find("weaponSlot") as RestrictedItemSlot;
            RestrictedItemSlot bodyslot = EquipmentSlots.Find("bodySlot") as RestrictedItemSlot;
            int EquipmentStartingStrenght = 3;

            ItemSlot slot_0_0 = Inventory[0, 0] as ItemSlot;
            slot_0_0.ChangeItem(new Potion("empty:64:64:10:Red", PotionType.Health, PotionPower.minor, 5));//some starting healt potions


            ItemSlot slot_2_2 = Inventory[2, 2] as ItemSlot;
            slot_2_2.ChangeItem(new Heal());
            ItemSlot slot_3_2 = Inventory[3, 2] as ItemSlot;
            slot_3_2.ChangeItem(new MagicBolt());

            switch (ptype)
            {
                case PlayerType.warrior:
                    weaponslot.ChangeItem(new WeaponEquipment(EquipmentStartingStrenght, WeaponType.melee));
                    bodyslot.ChangeItem(new BodyEquipment(EquipmentStartingStrenght, 2, ArmorType.heavy));
                    SetStats(1, 4, 4, 1, 1, 1, 1);
                    break;

                case PlayerType.archer:
                    weaponslot.ChangeItem(new WeaponEquipment(EquipmentStartingStrenght, WeaponType.bow));
                    bodyslot.ChangeItem(new BodyEquipment(EquipmentStartingStrenght, 2, ArmorType.normal));
                    SetStats(1, 1, 1, 4, 1, 1, 4);
                    break;

                case PlayerType.mage:
                    weaponslot.ChangeItem(new WeaponEquipment(EquipmentStartingStrenght, WeaponType.staff));
                    bodyslot.ChangeItem(new BodyEquipment(EquipmentStartingStrenght, 2, ArmorType.robes));
                    SetStats(1, 1, 1, 1, 4, 4, 1);
                    ItemSlot slot_1_0 = Inventory[1, 0] as ItemSlot;
                    slot_1_0.ChangeItem(new Potion("empty:64:64:10:Blue", PotionType.Mana, PotionPower.minor, 5));//some starting mana potions
                    break;

                default:
                    throw new Exception("invalid enemy type");
            }
        }

        protected override void DoBehaviour(HashSet<GameObject> changedObjects)
        {
            Debug.WriteLine("Called Player.DoBehaviour, but players don't have automated behaviour.");
        }

        #region Serialization
        public Player(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            exp = info.GetInt32("exp");
            freeStatPoints = info.GetInt32("freeStatPoints");
            playerNameTitle = info.GetValue("playerNameTitle", typeof(TextGameObject)) as TextGameObject;

            mouseSlot = info.TryGUIDThenFull<MouseSlot>(context, "mouseSlot");
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            SerializationHelper.Variables v = context.GetVars();
            if (v.FullySerializeEverything || v.FullySerialized.Contains(mouseSlot.GUID))
                info.AddValue("mouseSlot", mouseSlot); 
            else
                info.AddValue("mouseSlotGUID", mouseSlot.GUID.ToString());

            info.AddValue("playerNameTitle",playerNameTitle);
            info.AddValue("exp", exp);
            info.AddValue("freeStatPoints", freeStatPoints);
            base.GetObjectData(info, context);
        }
        #endregion

        public override void Replace(GameObject replacement)
        {
            if (mouseSlot != null && mouseSlot.GUID == replacement.GUID)
            {
                mouseSlot = replacement as MouseSlot;
                InitMouseSlot();
            }
            
            base.Replace(replacement);
        }
        
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            mouseSlot.Update(gameTime);

            if (exp >= RequiredExperience())
            {
                LevelUp();
            }
        }

        /// <summary>
        /// Adds exp to the players current exp. Can be a flat value or calculated based on the killed enemy his "power"
        /// </summary>
        /// <param name="expGained"></param>
        /// <param name="killedEnemy"></param>
        public void ReceiveExp(int expGained, Enemy killedEnemy = null)
        {
            if (killedEnemy != null)
            {
                int expmod = 100;
                float statAverige = (killedEnemy.Strength + killedEnemy.Dexterity + killedEnemy.Intelligence + killedEnemy.Wisdom + killedEnemy.Vitality + killedEnemy.Luck) / 6;
                expGained = (int)(statAverige * expmod);
            }

            exp += expGained;
        }

        /// <summary>
        /// returns the experience a creature requires for its next level.
        /// </summary>
        /// <returns></returns>
        protected int RequiredExperience()
        {
            double mod = 36.79;//chose this because it gives a 100 exp requirement for leveling from lvl 1 to 2
            double pow = Math.Pow(Math.E, Math.Sqrt(creatureLevel));
            int reqExp = (int)(mod * pow);
            return reqExp;
        }

        // handle a lvlup
        protected void LevelUp()
        {
            exp -= RequiredExperience();
            creatureLevel++;
            freeStatPoints = 3;
        }

        protected override void InitAnimationVariables()
        {
            idleAnimation = "player";
            moveAnimation = "empty:64:64:10:DarkBlue";
            dieAnimation = "empty:64:64:10:LightBlue";
        }

        public override void HandleInput(InputHelper inputHelper)
        {
            Action onLeftClick = () =>
            {
                Event e = new EndTurnEvent(this);
                Server.Send(e);
            };
            if (Tile != null)
                inputHelper.IfMouseLeftButtonPressedOn(Tile, onLeftClick);

            Action onRightClick = () =>
            {
                SkillEvent SkE = new SkillEvent((GameWorld.Find(Player.LocalPlayerName) as Player), this);
                Server.Send(SkE);
            };
            inputHelper.IfMouseRightButtonPressedOn(this, onRightClick);

            //TODO: make this less copypasty
            #region SkillSelection
            if (inputHelper.KeyPressed(Keys.D1))
            {
                currentSkill = (SkillList.Find("skillSlot0") as RestrictedItemSlot).SlotItem as Skill;
                ChangedSkillEvent CSK = new ChangedSkillEvent(this, CurrentSkill);
                Server.Send(CSK);
            }

            if (inputHelper.KeyPressed(Keys.D2))
            {
                currentSkill = (SkillList.Find("skillSlot1") as RestrictedItemSlot).SlotItem as Skill;
                ChangedSkillEvent CSK = new ChangedSkillEvent(this, CurrentSkill);
                Server.Send(CSK);
            }

            if (inputHelper.KeyPressed(Keys.D3))
            {
                currentSkill = (SkillList.Find("skillSlot2") as RestrictedItemSlot).SlotItem as Skill;
                ChangedSkillEvent CSK = new ChangedSkillEvent(this, CurrentSkill);
                Server.Send(CSK);
            }

            if (inputHelper.KeyPressed(Keys.D4))
            {
                currentSkill = (SkillList.Find("skillSlot3") as RestrictedItemSlot).SlotItem as Skill;
                ChangedSkillEvent CSK = new ChangedSkillEvent(this, CurrentSkill);
                Server.Send(CSK);
            }

            if (inputHelper.KeyPressed(Keys.D5))
            {
                currentSkill = (SkillList.Find("skillSlot4") as RestrictedItemSlot).SlotItem as Skill;
                ChangedSkillEvent CSK = new ChangedSkillEvent(this, CurrentSkill);
                Server.Send(CSK);
            }

            if (inputHelper.KeyPressed(Keys.D6))
            {
                currentSkill = (SkillList.Find("skillSlot5") as RestrictedItemSlot).SlotItem as Skill;
                ChangedSkillEvent CSK = new ChangedSkillEvent(this, CurrentSkill);
                Server.Send(CSK);
            }

            if (inputHelper.KeyPressed(Keys.D7))
            {
                currentSkill = (SkillList.Find("skillSlot6") as RestrictedItemSlot).SlotItem as Skill;
                ChangedSkillEvent CSK = new ChangedSkillEvent(this, CurrentSkill);
                Server.Send(CSK);
            }

            if (inputHelper.KeyPressed(Keys.D8))
            {
                currentSkill = (SkillList.Find("skillSlot7") as RestrictedItemSlot).SlotItem as Skill;
                ChangedSkillEvent CSK = new ChangedSkillEvent(this, CurrentSkill);
                Server.Send(CSK);
            }

            if (inputHelper.KeyPressed(Keys.D9))
            {
                currentSkill = (SkillList.Find("skillSlot8") as RestrictedItemSlot).SlotItem as Skill;
                ChangedSkillEvent CSK = new ChangedSkillEvent(this, CurrentSkill);
                Server.Send(CSK);
            }

            if (inputHelper.KeyPressed(Keys.D0))
            {
                currentSkill = (SkillList.Find("skillSlot9") as RestrictedItemSlot).SlotItem as Skill;
                ChangedSkillEvent CSK = new ChangedSkillEvent(this, CurrentSkill);
                Server.Send(CSK);
            }

            #endregion

            base.HandleInput(inputHelper);
        }

        public override GameObject Find(Func<GameObject, bool> del)
        {
            if (del.Invoke(mouseSlot))
                return mouseSlot;

            return mouseSlot.Find(del) ?? base.Find(del);
        }
        
        public enum Stat { vitality, strength, dexterity, wisdom, luck, intelligence }

        public void AddStatPoint(Stat stat)
        {
            switch (stat)
            {
                case Stat.vitality:
                    vitality++;
                    break;
                case Stat.strength:
                    strength++;
                    break;
                case Stat.dexterity:
                    dexterity++;
                    break;
                case Stat.wisdom:
                    wisdom++;
                    break;
                case Stat.luck:
                    luck++;
                    break;
                case Stat.intelligence:
                    intelligence++;
                    break;
                default:
                    break;
            }
            freeStatPoints--;
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

        private void InitMouseSlot()
        {
            PlayingGUI gui = GameWorld.Find("PlayingGui") as PlayingGUI;
            gui.Add(mouseSlot);
        }

        public void InitGUI(Dictionary<string, object> guiState)
        {
            PlayingGUI gui = GameWorld.Find("PlayingGui") as PlayingGUI;
            PlayerNameTitle();
            gui.Add(playerNameTitle);
            int screenWidth = GameEnvironment.Screen.X;
            int screenHeight = GameEnvironment.Screen.Y;

            if (Id == LocalPlayerName)
            {
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
                Vector2 APBarPosition = new Vector2(screenWidth - barX - apBar.Width, HPBarPosition.Y);
                apBar.Position = new Vector2(APBarPosition.X, APBarPosition.Y);
                gui.Add(apBar);

                PlayerInventoryAndEquipment pie = new PlayerInventoryAndEquipment(Inventory, EquipmentSlots);
                pie.Position = guiState.ContainsKey("playerIaEPosition") ? (Vector2)guiState["playerIaEPosition"] : new Vector2(screenWidth - pie.Width, 300);
                pie.Visible = guiState.ContainsKey("playerIaEVisibility") ? (bool)guiState["playerIaEVisibility"] : false;
                gui.Add(pie);

                SkillBar skillBar = new SkillBar(SkillList);
                skillBar.Position = guiState.ContainsKey("skillbarPosition") ? (Vector2)guiState["skillbarPosition"] : new Vector2((screenWidth-skillBar.Width)/2,(gui.Find("TopBar")as SpriteGameObject).Sprite.Height);
                skillBar.Visible = guiState.ContainsKey("skillBarVisibility") ? (bool)guiState["skillBarVisibility"] : true;
                gui.Add(skillBar);

                InitMouseSlot();
            }
        }

        public void CleanupGUI(Dictionary<string, object> guiState)
        {
            PlayingGUI gui = GameWorld.Find("PlayingGui") as PlayingGUI;
            gui.Remove(gui.Find("playerName" + guid.ToString()));
            if (Id == LocalPlayerName && GameWorld != null)
            {
                PlayerInventoryAndEquipment pIaE = gui.Find(obj => obj is PlayerInventoryAndEquipment) as PlayerInventoryAndEquipment;
                guiState.Add("playerIaEVisibility", pIaE.Visible);
                guiState.Add("playerIaEPosition", pIaE.Position);

                SkillBar skillbar = gui.Find(obj=> obj is SkillBar) as SkillBar;
                guiState.Add("skillBarVisibility", skillbar.Visible);
                guiState.Add("skillbarPosition", skillbar.Position);

                gui.RemoveImmediatly(gui.Find("HealthBar"));
                gui.RemoveImmediatly(gui.Find("ManaBar"));
                gui.RemoveImmediatly(gui.Find("ActionBar"));
                gui.RemoveImmediatly(pIaE);
                gui.RemoveImmediatly(mouseSlot);
                gui.RemoveImmediatly(skillbar);
            }
        }
    }
}
