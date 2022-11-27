using System;
using DOL.AI.Brain;
using System.Linq;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.MobGroups;
using System.Collections.Generic;
using DOL.GS.Spells;

namespace DOL.GS.Scripts
{
    public class AreaEffect : GameNPC
    {
        public int SpellEffect;
        public int IntervalMin;
        public int IntervalMax;
        public int HealHarm;
        public int AddMana;
        public int AddEndurance;
        public int Radius;
        public int MissChance;
        public int SpellID;
        public string Message = "";
        public string Group_Mob_Id = "";
        public bool Group_Mob_Turn;
        public ushort AreaEffectFamily;
        public ushort OrderInFamily;
        public bool OneUse;

        private long LastApplyEffectTick;
        private int Interval;
        private DBAreaEffect AreaEffectDB;
        private bool enable;
        private bool disable;

        protected static SpellLine m_mobSpellLine = SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells);

        public bool Disable
        {
            get { return disable; }
            set
            {
                enable = !value;
                disable = value;
            }
        }

        public void CallAreaEffect()
        {
            enable = true;
            Brain.Think();
        }

        #region AddToWorld
        public override bool AddToWorld()
        {
            if (!base.AddToWorld()) return false;
            if (!(Brain is AreaEffectBrain))
                SetOwnBrain(new AreaEffectBrain());
            enable = !Disable;
            return true;
        }
        #endregion

        #region ApplyEffect
        public void ApplyEffect()
        {
            if (!enable) return;
            if (Radius == 0 || SpellEffect == 0) return;
            if (LastApplyEffectTick > CurrentRegion.Time - Interval) return;

            foreach (GamePlayer player in GetPlayersInRadius((ushort)Radius))
            {
                if (!player.IsAlive) continue;
                if (Util.Chance(MissChance)) continue;

                var health = HealHarm + (HealHarm * Util.Random(-5, 5) / 100);
                if (health < 0 && player.Client.Account.PrivLevel == 1)
                {
					player.TakeDamage(this, eDamageType.Natural, -health, 0);
                }
                if (health > 0 && player.Health < player.MaxHealth)
                    player.Health += health;
                if (AddMana > 0 && player.Mana < player.MaxMana)
                    player.Mana += AddMana;
                if (AddEndurance > 0 && player.Endurance < player.MaxEndurance)
                    player.Endurance += AddEndurance;

                if (Message != "" && (health != 0 || AddMana != 0 || AddEndurance != 0))
                {
                    player.Out.SendMessage(
                        string.Format(Message, Math.Abs(health), Math.Abs(AddMana), Math.Abs(AddEndurance)),
                        eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
                }

                if (player.Client.Account.PrivLevel == 1)
                    foreach (GamePlayer plr in player.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                        plr.Out.SendSpellEffectAnimation(this, player, (ushort)SpellEffect, 0, false, 1);
            }
            foreach (GamePlayer plr in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                plr.Out.SendSpellEffectAnimation(this, this, (ushort)SpellEffect, 0, false, 1);

            LastApplyEffectTick = CurrentRegion.Time;
            Interval = Util.Random(IntervalMin, Math.Max(IntervalMax, IntervalMin)) * 1000;
        }

        public void ApplySpell()
        {
            if (!enable) return;
            if (Radius == 0 || SpellID == 0) return;
            if (LastApplyEffectTick > CurrentRegion.Time - Interval) return;
            DBSpell dbspell = GameServer.Database.SelectObjects<DBSpell>("`SpellID` = @SpellID", new QueryParameter("@SpellID", SpellID)).FirstOrDefault();
            Spell spell = new Spell(dbspell, 0);

            foreach (GamePlayer player in GetPlayersInRadius((ushort)Radius))
            {
                if ((spell.Duration == 0 || !player.HasEffect(spell) || spell.SpellType.ToUpper() == "DIRECTDAMAGEWITHDEBUFF"))
                {
                    TurnTo(player);

                    this.CastSpellOnOwnerAndPets(player, spell, m_mobSpellLine);
                }
            }
            foreach (GamePlayer plr in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                plr.Out.SendSpellEffectAnimation(this, this, spell.ClientEffect, 0, false, 1);
            LastApplyEffectTick = CurrentRegion.Time;
            Interval = Util.Random(IntervalMin, Math.Max(IntervalMax, IntervalMin)) * 1000;
        }

        public override bool CastSpell(Spell spell, SpellLine line)
        {
            ISpellHandler spellhandler = ScriptMgr.CreateSpellHandler(this, spell, line);
            if (spellhandler != null)
            {
                if (spell.CastTime > 0)
                {
                    m_runningSpellHandler = spellhandler;
                    spellhandler.CastingCompleteEvent += new CastingCompleteCallback(OnAfterSpellCastSequence);
                }
                return spellhandler.CastSpell();
            }
            return false;
        }

        #endregion

        public void CheckGroupMob()
        {
            if (!String.IsNullOrEmpty(Group_Mob_Id) && MobGroupManager.Instance.Groups.ContainsKey(Group_Mob_Id))
            {
                bool allDead = MobGroupManager.Instance.Groups[Group_Mob_Id].NPCs.All(m => !m.IsAlive);
                if (!allDead)
                    enable = Group_Mob_Turn;
                else
                    enable = !Group_Mob_Turn;
            }
        }

        public AreaEffect CheckFamily()
        {
            if(enable && AreaEffectFamily != 0)
            {
                List<DBAreaEffect> areaList = GameServer.Database.SelectObjects<DBAreaEffect>("`AreaEffectFamily` = @AreaEffectFamily", new QueryParameter("@AreaEffectFamily", AreaEffectFamily)).OrderBy((area) => area.OrderInFamily).ToList();
                // search the next 
                foreach (DBAreaEffect area in areaList)
                    if (area.OrderInFamily > OrderInFamily)
                    {
                        Mob mob = GameServer.Database.SelectObjects<Mob>("`Mob_ID` = @MobID", new QueryParameter("@MobID", area.MobID)).FirstOrDefault();
                        if(mob != null)
                        {
                            if (OneUse)
                                enable = false;
                            return WorldMgr.GetNPCsByName(mob.Name, (eRealm)mob.Realm).FirstOrDefault((npc) => npc.InternalID == mob.ObjectId) as AreaEffect;
                        }

                    }

            }
            if (OneUse)
                enable = false;
            return null;
        }

        #region Database
        public override void LoadFromDatabase(DataObject obj)
        {
            base.LoadFromDatabase(obj);
            DBAreaEffect data = null;
            try
            {
                data = GameServer.Database.SelectObject<DBAreaEffect>(a => a.MobID == obj.ObjectId);
                if (data == null)
                    return;
            }
            catch
            {
                DBAreaEffect.Init();
            }
            if (data == null)
                data = GameServer.Database.SelectObject<DBAreaEffect>(a => a.MobID == obj.ObjectId);
            if (data == null)
                return;

            AreaEffectDB = data;
            SpellEffect = AreaEffectDB.Effect;
            IntervalMin = AreaEffectDB.IntervalMin;
            IntervalMax = AreaEffectDB.IntervalMax;
            HealHarm = AreaEffectDB.HealHarm;
            AddMana = AreaEffectDB.Mana;
            AddEndurance = AreaEffectDB.Endurance;
            Radius = AreaEffectDB.Radius;
            MissChance = AreaEffectDB.MissChance;
            Message = AreaEffectDB.Message;
            SpellID = AreaEffectDB.SpellID;
            Group_Mob_Id = AreaEffectDB.Group_Mob_Id;
            Group_Mob_Turn = AreaEffectDB.Group_Mob_Turn;
            AreaEffectFamily = AreaEffectDB.AreaEffectFamily;
            Disable = AreaEffectDB.Disable;
            OrderInFamily = AreaEffectDB.OrderInFamily;
            OneUse = AreaEffectDB.OnuUse;
        }


        public override void SaveIntoDatabase()
        {
            base.SaveIntoDatabase();
            bool New = false;
            if (AreaEffectDB == null)
            {
                AreaEffectDB = new DBAreaEffect();
                AreaEffectDB.MobID = InternalID;
                New = true;
            }

            AreaEffectDB.Effect = SpellEffect;
            AreaEffectDB.IntervalMin = IntervalMin;
            AreaEffectDB.IntervalMax = IntervalMax;
            AreaEffectDB.HealHarm = HealHarm;
            AreaEffectDB.Mana = AddMana;
            AreaEffectDB.Endurance = AddEndurance;
            AreaEffectDB.Radius = Radius;
            AreaEffectDB.MissChance = MissChance;
            AreaEffectDB.Message = Message;
            AreaEffectDB.SpellID = SpellID;
            AreaEffectDB.Group_Mob_Id = Group_Mob_Id;
            AreaEffectDB.Group_Mob_Turn = Group_Mob_Turn;
            AreaEffectDB.AreaEffectFamily = AreaEffectFamily;
            AreaEffectDB.Disable = Disable;
            AreaEffectDB.OrderInFamily = OrderInFamily;
            AreaEffectDB.OnuUse = OneUse;

            if (New)
                GameServer.Database.AddObject(AreaEffectDB);
            else
                GameServer.Database.SaveObject(AreaEffectDB);
        }
        #endregion

        public override List<string> CustomInfo()
        {
            List<string> text = base.CustomInfo();
            text.Add("");
            text.Add("-- AreaEffect --");
            text.Add(" + Effet: " + (HealHarm > 0 ? "heal " : "harm ") + HealHarm + " points de vie (+/- 10%).");
            text.Add(" + Rayon: " + Radius);
            text.Add(" + effect: " + SpellEffect);
            text.Add(" + Interval: " + IntervalMin + " à " + IntervalMax + " secondes");
            text.Add(" + Chance de miss: " + MissChance + "%");
            text.Add(" + Message: " + Message);
            text.Add(" + Spell: " + SpellID);
            text.Add(" + Family: " + AreaEffectFamily);
            text.Add(" + Order in family: " + OrderInFamily);
            text.Add(" + Groupmob: " + Group_Mob_Id);
            text.Add(" + Groupmob ON/OFF: " + Group_Mob_Turn);
            text.Add(" + Enable: " + !Disable);
            text.Add(" + OneUse: " + OneUse);
            return text;
        }

        public override void CustomCopy(GameObject source)
        {
            base.CustomCopy(source);
            AreaEffect areaSource = source as AreaEffect;
            if (areaSource != null)
            {
                DBAreaEffect lastarea = GameServer.Database.SelectObjects<DBAreaEffect>("`AreaEffectFamily` = @AreaEffectFamily", new QueryParameter("@AreaEffectFamily", areaSource.AreaEffectFamily)).OrderBy((area) => area.OrderInFamily).ToList().LastOrDefault();
                ushort order = 0;
                if (lastarea != null)
                    order = (ushort)(lastarea.OrderInFamily + 1);
                HealHarm = areaSource.HealHarm;
                Radius = areaSource.Radius;
                SpellEffect = areaSource.SpellEffect;
                IntervalMin = areaSource.IntervalMin;
                IntervalMax = areaSource.IntervalMax;
                MissChance = areaSource.MissChance;
                Message = areaSource.Message;
                SpellID = areaSource.SpellID;
                AreaEffectFamily = areaSource.AreaEffectFamily;
                OrderInFamily = order;
                Group_Mob_Id = areaSource.Group_Mob_Id;
                Group_Mob_Turn = areaSource.Group_Mob_Turn;
                Disable = areaSource.Disable;
                OneUse = areaSource.OneUse;
            }
        }
    }
}