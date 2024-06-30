using DOL.Database;
using DOL.GS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServerScripts.Utils
{
    public class FeuxCampMgr
    {
        private static FeuxCampMgr instance;

        public Dictionary<string, FeuDeCamp> m_firecamps;

        public static FeuxCampMgr Instance => instance ?? (instance = new FeuxCampMgr());

        public bool Init()
        {
            var firecamps = GameServer.Database.SelectAllObjects<FeuxCampXItem>();
            m_firecamps = new Dictionary<string, FeuDeCamp>();
            foreach (var firecampItem in firecamps)
            {
                var template = GameServer.Database.FindObjectByKey<ItemTemplate>(firecampItem.FeuxCampItemId_nb);

                if (template == null)
                    continue;
                
                var firecamp = new FeuDeCamp()
                {
                    Template_ID = firecampItem.FeuxCampItemId_nb,
                    Realm = template.Realm,
                    Model = (ushort)template.Model,
                    Radius = (ushort)firecampItem.Radius,
                    Lifetime = firecampItem.Lifetime,
                    EndurancePercentRate = firecampItem.EnduranceRatePercent,
                    ManaTrapDamagePercent = firecampItem.ManaTrapDamagePercent,
                    HealthTrapDamagePercent = firecampItem.HealthTrapDamagePercent,
                    HealthPercentRate = firecampItem.HealthRatePercent,
                    ManaPercentRate = firecampItem.ManaRatePercent,
                    OwnerImmuneToTrap = firecampItem.OwnerImmuneToTrap,
                };

                m_firecamps[firecampItem.FeuxCampXItem_ID] = firecamp;
            }

            return true;
        }
    }
}