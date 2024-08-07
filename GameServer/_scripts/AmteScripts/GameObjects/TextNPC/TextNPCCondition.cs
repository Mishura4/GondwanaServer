using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DOL.GS.Scripts
{
    /// <summary>
    /// Conditions:
    ///  - Heure (min - max)
    ///  - Level (min - max)
    ///  - guilde (interdites)
    ///  - guildeA (autorisées)
    ///  - race (interdites)
    ///  - classe (interdites)
    ///  - prp (min - max)
    /// </summary>
    public class TextNPCCondition
    {
        public int Level_min = 1;
        public int Level_max = 50;

        /// <summary>
        /// Noms de guilde interdits
        /// </summary>
        public List<string> GuildNames = new List<string>();

        /// <summary>
        /// Noms de guilde autorisées
        /// </summary>
        public List<string> GuildNamesA = new List<string>();

        /// <summary>
        /// Races interdites
        /// </summary>
        public List<string> Races = new List<string>();

        /// <summary>
        /// Classes interdites
        /// </summary>
        public List<string> Classes = new List<string>();

        public int Heure_min;
        public int Heure_max = 24;

        public eQuestIndicator CanGiveQuest = eQuestIndicator.None;



        /// <summary>
        /// Parse la variable cond
        /// </summary>
        /// <param name="cond">Texte à parser, exemple:
        /// level/10/50
        /// guild/Legion Noire/Caste des Chasseurs
        /// race/Troll/Elf
        /// class/Animist/Paladin
        /// prp/1/200</param>
        public TextNPCCondition(string cond)
        {
            GuildNamesA.Add("ALL");

            #region Parse condition

            if (!string.IsNullOrEmpty(cond))
            {
                foreach (string txt in cond.Split('\n'))
                {
                    if (txt == "") continue;
                    string[] condition = txt.Split('/');
                    bool first = true;
                    switch (condition[0])
                    {
                        case "level":
                            try
                            {
                                Level_min = int.Parse(condition[1]);
                                Level_max = int.Parse(condition[2]);
                            }
                            catch
                            {
                                Level_min = 0;
                                Level_max = 50;
                            }

                            break;

                        case "guild":
                            GuildNames = new List<string>();
                            foreach (string name in condition)
                            {
                                if (first)
                                    first = false;
                                else
                                    GuildNames.Add(name);
                            }

                            break;

                        case "guildA":
                            GuildNamesA = new List<string>();
                            foreach (string name in condition)
                            {
                                if (first) first = false;
                                else GuildNamesA.Add(name);
                            }

                            break;

                        case "race":
                            Races = new List<string>();
                            foreach (string name in condition)
                            {
                                if (first)
                                    first = false;
                                else
                                    Races.Add(name);
                            }

                            break;

                        case "class":
                            Classes = new List<string>();
                            foreach (string name in condition)
                            {
                                if (first)
                                    first = false;
                                else
                                    Classes.Add(name);
                            }

                            break;

                        case "hour":
                            try
                            {
                                Heure_min = int.Parse(condition[1]);
                                Heure_max = int.Parse(condition[2]);
                            }
                            catch
                            {
                                Heure_min = 0;
                                Heure_max = 24;
                            }
                            break;

                        case "quest":
                            if (condition.Length <= 1 || !Enum.TryParse(condition[1], true, out CanGiveQuest))
                                CanGiveQuest = eQuestIndicator.Available;
                            break;
                    }
                }
            }

            #endregion
        }

        /// <summary>
        /// Retourne la condition pour être sauvegardé dans la db
        /// </summary>
        public string GetConditionString()
        {
            StringBuilder txt = new StringBuilder();

            if (Level_min != 0 || Level_max != 50)
                txt.Append("level/" + Level_min + "/" + Level_max + "\n");

            if (GuildNames.Count >= 1)
            {
                txt.Append("guild");
                foreach (string guild in GuildNames)
                    txt.Append("/" + guild);
                txt.Append("\n");
            }

            if (GuildNamesA.Count >= 1)
            {
                txt.Append("guildA");
                foreach (string guild in GuildNamesA)
                    txt.Append("/" + guild);
                txt.Append("\n");
            }

            if (Races.Count >= 1)
            {
                txt.Append("race");
                foreach (string race in Races)
                    txt.Append("/" + race);
                txt.Append("\n");
            }

            if (Classes.Count >= 1)
            {
                txt.Append("class");
                foreach (string classe in Classes)
                    txt.Append("/" + classe);
                txt.Append("\n");
            }

            if (Heure_min > 0 || Heure_max < 24)
                txt.Append("hour/").Append(Heure_min).Append('/').Append(Heure_max).Append('\n');

            if (CanGiveQuest != eQuestIndicator.None)
                txt.Append($"quest/{CanGiveQuest}\n");

            return txt.ToString();
        }
        
        public bool IsActiveAtTick(uint tick)
        {
            if (Heure_min <= 0 && Heure_max >= 24)
            {
                return true;
            }
            
            uint minTick = ((uint)Heure_min) * 60 * 60 * 1000;
            uint maxTick = ((uint)Heure_max) * 60 * 60 * 1000;

            //Heure
            if (maxTick < minTick && (minTick > tick || tick <= maxTick))
                return false;
            if (maxTick > minTick && (minTick > tick || tick >= maxTick))
                return false;
            if (maxTick == minTick && tick != minTick)
                return false;
            return true;
        }
        
        public bool CheckAccess(GamePlayer player)
        {
            //level
            if (Level_min > player.Level || player.Level > Level_max)
                return false;
            //Karma
            //			if(!KarmaScriptMgr.Instance().CheckTextNPCAcces(player, Karma_min, Karma_max))
            //				return false;

            //Guilde
            if (GuildNames.Contains(player.GuildName) || (player.GuildName == "" && GuildNames.Contains("NO GUILD")))
                return false;
            if (!GuildNamesA.Contains("ALL") && (!GuildNamesA.Contains(player.GuildName) ||
                                                 (player.GuildName == "" && !GuildNamesA.Contains("NO GUILD"))))
                return false;
            //Classe
            if (Classes.Contains(((eCharacterClass)player.CharacterClass.ID).ToString().ToLower()))
                return false;
            //Race
            if (Races.Contains(player.RaceName.ToLower()))
                return false;

            if (!IsActiveAtTick(WorldMgr.GetCurrentGameTime(player)))
                return false;

            return true;
        }
    }
}
