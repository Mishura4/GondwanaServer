using DOL.AI.Brain;
using DOL.commands.playercommands;
using DOL.Database;
using DOL.Events;
using DOL.Geometry;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.Language;
using DOLDatabase.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace DOL.gameobjects.CustomNPC
{
    public class ShadowNPC : GameNPC
    {
        private GamePlayer player;
        private string keyWord = "";
        private short index = 0;
        const short LIMIT = 6;

        public override bool IsAttackable
        {
            get
            {
                return false;
            }
        }

        public override bool IsAggressive
        {
            get
            {
                return false;
            }
        }

        public override eRealm Realm { get => eRealm.None; set => base.Realm = value; }

        public override bool IsAvailable => false;

        public override bool IsVisibleTo(GameObject checkObject)
        {
            return checkObject is GamePlayer pl && pl == player;
        }

        // never die
        public override int Health { get => base.Health; set => base.Health = MaxHealth; }

        public override ushort Model { get => 667; set => base.Model = value; }

        public override eFlags Flags { get => eFlags.DONTSHOWNAME | eFlags.PEACE; set => base.Flags = value; }

        public ShadowNPC(GamePlayer player) : base()
        {
            this.player = player;
            IControlledBrain brain = new ShadowBrain(player);
            SetOwnBrain(brain as AI.ABrain);
            int x, y, z;
            ushort heading;
            Region region;

            GetPlayerLocation(out x, out y, out z, out heading, out region);

            Position = new System.Numerics.Vector3(x, y, z);
            Heading = heading;
            CurrentRegion = region;
            AddToWorld();
        }

        public override string Name { get => "Combine List"; set => base.Name = value; }
        public string KeyWord
        {
            get => keyWord;
            set
            {
                index = 0;
                keyWord = value;
            }
        }

        public override bool WhisperReceive(GameLiving source, string text)
        {
            if (!base.WhisperReceive(source, text))
                return false;
            if (source is GamePlayer player)
            {
                string message;
                if (text == "Menu")
                {
                    message = Combine.BuildMessage(player.Client);
                }
                else
                {
                    string title = text;
                    switch (text)
                    {
                        case "Weaponcrafting":
                        case "Forge":
                            KeyWord = "WEAP";
                            break;
                        case "Armorcrafting":
                        case "Arumerie":
                            KeyWord = "ARMO";
                            break;
                        case "Sieecrafting":
                        case "Ingénierie":
                            KeyWord = "SIEG";
                            break;
                        case "Alchemy":
                        case "Alchimie":
                            KeyWord = "ALCH";
                            break;
                        case "Metalcrafting":
                        case "Metallurgie":
                            KeyWord = "METL";
                            break;
                        case "Leathercrafting":
                        case "Maroquinerie":
                            KeyWord = "LEAT";
                            break;
                        case "Clothworking":
                        case "Tissage":
                            KeyWord = "CLOT";
                            break;
                        case "Gemcutting":
                        case "Joaillerie":
                            KeyWord = "GEMM";
                            break;
                        case "Herbalcrafting":
                        case "Herboristerie":
                            KeyWord = "HERB";
                            break;
                        case "Tailoring":
                        case "Couture":
                            KeyWord = "TAIL";
                            break;
                        case "Spellcrafting":
                        case "Arcanisme":
                            KeyWord = "SPLL";
                            break;
                        case "Woodworking":
                        case "Menuiserie":
                            KeyWord = "WOOD";
                            break;
                        case "Fletching":
                        case "Empennage":
                            KeyWord = "FLET";
                            break;
                        case "Bountycrafting":
                        case "Epique":
                            KeyWord = "BOUN";
                            break;
                        case "Cooking":
                        case "Cuisine":
                            KeyWord = "COOK";
                            break;
                        case "Scholar":
                        case "Chercheur":
                            KeyWord = "SCHO";
                            break;
                        case "Previous":
                        case "Précédent":
                            index--;
                            break;
                        case "Next":
                        case "Suivant":
                            index++;
                            break;
                    }
                    message = BuildList(title);
                }
                player.Out.SendMessage(message, eChatType.CT_System, eChatLoc.CL_PopupWindow);
            }
            return true;
        }

        protected static int CompareCharacterXCombineItem(CharacterXCombineItem item1, CharacterXCombineItem item2)
        {
            return item1.CombinationId.CompareTo(item2.CombinationId);
        }

        private string BuildList(string title)
        {
            string result = title + ":\n\n";

            List<CharacterXCombineItem> characterXCombineItem = new List<CharacterXCombineItem>(GameServer.Database.SelectObjects<CharacterXCombineItem>(string.Format("Character_ID='{0}' AND CombinationId like '{1}%'", player.InternalID, KeyWord)));
            characterXCombineItem.Sort(CompareCharacterXCombineItem);

            short size = (short)characterXCombineItem.Count;
            short min = (short)(index * LIMIT);
            short max;
            if (index * LIMIT + LIMIT >= size)
                max = size;
            else
                max = LIMIT;

            if (size == 0)
            {
                result += LanguageMgr.GetTranslation(player.Client, "Commands.Players.Combine.NoData") + "\n";
            }
            else
            {
                for (short i = 0; i < max; i++)
                {
                    CharacterXCombineItem cXci = characterXCombineItem[min + i];
                    CombineItemDb ci = GameServer.Database.SelectObject<CombineItemDb>(string.Format("CombinationId like '{0}%'", cXci.CombinationId));

                    string[] splitresult = ci.ItemTemplateId.Split('|');
                    ItemTemplate resultTemplate = GameServer.Database.SelectObject<ItemTemplate>(string.Format("Id_nb='{0}'", splitresult[0]));
                    result += resultTemplate.Name + " X" + splitresult[1] + "\n";

                    if (!string.IsNullOrEmpty(ci.ToolKit))
                    {
                        ItemTemplate toolTemplate = GameServer.Database.SelectObject<ItemTemplate>(string.Format("Id_nb='{0}'", ci.ToolKit));
                        result += LanguageMgr.GetTranslation(player.Client, "Commands.Players.Combine.Tool") + ": " + toolTemplate.Name + "\n";
                    }

                    result += LanguageMgr.GetTranslation(player.Client, "Commands.Players.Combine.Ingredients") + ":\n";
                    string[] splitingredients = ci.ItemsIds.Split(';');
                    foreach (string ingredient in splitingredients)
                    {
                        string[] splitingredient = ingredient.Split('|');
                        ItemTemplate ingredientTemplate = GameServer.Database.SelectObject<ItemTemplate>(string.Format("Id_nb='{0}'", splitingredient[0]));
                        result += " - " + ingredientTemplate.Name + " (X" + splitingredient[1] + ")\n";
                    }
                    result += "/////////////////////////////////////\n";
                }
            }



            if (index > 0)
                result += LanguageMgr.GetTranslation(player.Client, "Commands.Players.Combine.Previous");
            result += LanguageMgr.GetTranslation(player.Client, "Commands.Players.Combine.Menu");
            if (index * LIMIT + LIMIT < size)
                result += LanguageMgr.GetTranslation(player.Client, "Commands.Players.Combine.Next");

            return result;
        }

        protected virtual void GetPlayerLocation(out int x, out int y, out int z, out ushort heading, out Region region)
        {
            Vector2 point = player.GetPointFromHeading(player.Heading, 64);
            x = (int)point.X;
            y = (int)point.Y;
            z = (int)player.Position.Z;
            heading = (ushort)((player.Heading + 2048) % 4096);
            region = player.CurrentRegion;
        }

        public virtual void MoveToPlayer()
        {
            int x, y, z;
            ushort heading;
            Region region;

            GetPlayerLocation(out x, out y, out z, out heading, out region);

            MoveTo(region.ID, x, y, z, heading);
        }

        public void Interact(string message)
        {
            Notify(GameObjectEvent.Interact, this, new InteractEventArgs(player));
            player.Notify(GameObjectEvent.InteractWith, player, new InteractWithEventArgs(this));
            TurnTo(player);
            player.Out.SendMessage(message, eChatType.CT_System, eChatLoc.CL_PopupWindow);
        }
    }

    public class ShadowBrain : FollowOwnerBrain
    {
        public ShadowBrain(GameLiving owner) : base(owner)
        {
            m_isMainPet = false;
        }

        public override GameLiving GetLivingOwner()
        {
            return Owner;
        }
    }
}