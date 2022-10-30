using DOL.AI.Brain;
using System.Text.RegularExpressions;

namespace DOL.GS
{
    public class MobChieftain : GameNPC
    {
        public static ushort LINK_DISTANCE = 2000;

        public MobChieftain()
            : base()
        {
            LoadedFromScript = false;
            this.SetOwnBrain(new AmteMobBrain());
        }

        public override void StartAttack(GameObject attackTarget)
        {
            //We leave if this attacker is already handled by the chieftain (meaning he has already called his minions) 
            if (AttackState)
            {
                base.StartAttack(attackTarget);
                return;
            }
            base.StartAttack(attackTarget);
            foreach (GameNPC npc in GetNPCsInRadius(LINK_DISTANCE))
            {
                var match = Regex.Match(this.Name, @"\s(" + npc.GuildName + ")$");
                if (npc is GameNPC && match != null && match.Length > 1 && !npc.InCombat)
                {
                    npc.StartAttack(attackTarget);
                }
            }
        }
    }
}