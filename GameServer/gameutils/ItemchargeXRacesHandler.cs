
using DOL.Database;
using Google.Protobuf.WellKnownTypes;

namespace DOL.GS
{
    public class ItemchargeXRacesHandler
    {
        private double m_value;
        private double m_damage;
        private int m_duration;

        public double Value
        {
            get { return m_value; }
            set { m_value = value; }
        }

        public double Damage
        {
            get { return m_damage; }
            set { m_damage = value; }
        }

        public int Duration
        {
            get { return m_duration; }
            set { m_duration = value; }
        }

        // Method to get the race multiplicator
        public double GetRaceMultiplier(GamePlayer player, string itemTemplate)
        {
            var parameters = new[]
            {
                new QueryParameter("@ItemTemplate", itemTemplate)
            };

            ItemchargeXRaces itemChargeRace = GameServer.Database.SelectObject<ItemchargeXRaces>("ItemTemplate = @ItemTemplate", parameters);

            if (itemChargeRace == null)
            {
                return 1.0;
            }

            double raceMultiplier;
            switch ((short)player.Race)
            {
                case (short)eRace.Briton: raceMultiplier = itemChargeRace.BritonRace; break;
                case (short)eRace.Avalonian: raceMultiplier = itemChargeRace.AvalonianRace; break;
                case (short)eRace.Highlander: raceMultiplier = itemChargeRace.HighlanderRace; break;
                case (short)eRace.Saracen: raceMultiplier = itemChargeRace.SaracenRace; break;
                case (short)eRace.Norseman: raceMultiplier = itemChargeRace.NorsemanRace; break;
                case (short)eRace.Troll: raceMultiplier = itemChargeRace.TrollRace; break;
                case (short)eRace.Dwarf: raceMultiplier = itemChargeRace.DwarfRace; break;
                case (short)eRace.Kobold: raceMultiplier = itemChargeRace.KoboldRace; break;
                case (short)eRace.Celt: raceMultiplier = itemChargeRace.CeltRace; break;
                case (short)eRace.Firbolg: raceMultiplier = itemChargeRace.FirbolgRace; break;
                case (short)eRace.Elf: raceMultiplier = itemChargeRace.ElfRace; break;
                case (short)eRace.Lurikeen: raceMultiplier = itemChargeRace.LurikeenRace; break;
                case (short)eRace.Inconnu: raceMultiplier = itemChargeRace.InconnuRace; break;
                case (short)eRace.Valkyn: raceMultiplier = itemChargeRace.ValkynRace; break;
                case (short)eRace.Sylvan: raceMultiplier = itemChargeRace.SylvanRace; break;
                case (short)eRace.HalfOgre: raceMultiplier = itemChargeRace.HalfOgreRace; break;
                case (short)eRace.Frostalf: raceMultiplier = itemChargeRace.FrostalfRace; break;
                case (short)eRace.Shar: raceMultiplier = itemChargeRace.SharRace; break;
                case (short)eRace.AlbionMinotaur: raceMultiplier = itemChargeRace.AlbionMinotaurRace; break;
                case (short)eRace.MidgardMinotaur: raceMultiplier = itemChargeRace.MidgardMinotaurRace; break;
                case (short)eRace.HiberniaMinotaur: raceMultiplier = itemChargeRace.HiberniaMinotaurRace; break;
                default: raceMultiplier = itemChargeRace.UnknownRace; break;
            }

            if (raceMultiplier < -100)
            {
                raceMultiplier = -100;
            }

            return raceMultiplier;
        }

        public bool IsDurationMultiplied(string itemTemplate)
        {
            var parameters = new[]
            {
                new QueryParameter("@ItemTemplate", itemTemplate)
            };

            ItemchargeXRaces itemChargeRace = GameServer.Database.SelectObject<ItemchargeXRaces>("ItemTemplate = @ItemTemplate", parameters);

            if (itemChargeRace == null)
            {
                return false;
            }

            return itemChargeRace.IsDurationMultiplied;
        }

        public (int spellID, double multiplier) GetAlternativeSpellIDAndMultiplier(double raceMultiplier, int originalSpellID)
        {
            if (raceMultiplier > 99.9)
            {
                return ((int)raceMultiplier, 1.0);
            }

            return (originalSpellID, raceMultiplier);
        }
    }
}