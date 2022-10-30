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

                if (template != null)
                {
                    var firecamp = new FeuDeCamp()
                    {
                        Template_ID = firecampItem.FeuxCampItemId_nb,
                        Model = (ushort)template.Model,
                        Radius = (ushort)firecampItem.Radius,
                        Lifetime = firecampItem.Lifetime,
                        EndurancePercentRate = firecampItem.EnduranceRatePercent,
                        IsHealthType = firecampItem.IsHealthType,
                        IsManaType = firecampItem.IsManaType,
                        IsManaTrapType = firecampItem.IsManaTrapType,
                        IsHealthTrapType = firecampItem.IsHealthType,
                        ManaTrapDamagePercent = firecampItem.ManaTrapDamagePercent,
                        HealthTrapDamagePercent = firecampItem.HealthTrapDamagePercent,
                        IsEnduranceType = firecampItem.IsEnduranceType,
                        HealthPercentRate = firecampItem.HealthRatePercent,
                        ManaPercentRate = firecampItem.ManaRatePercent
                    };

                    if (m_firecamps.ContainsKey(firecampItem.FeuxCampXItem_ID))
                    {
                        m_firecamps[firecampItem.FeuxCampXItem_ID] = firecamp;
                    }
                    else
                    {
                        m_firecamps.Add(firecampItem.FeuxCampXItem_ID, firecamp);
                    }
                }
            }

            return true;
        }


    }
}