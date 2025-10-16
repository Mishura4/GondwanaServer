using System.Reflection;
using System.Collections;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Effects;
using log4net;
using DOL.Language;
using System.Numerics;

namespace DOL.GS.RealmAbilities
{
    public class BadgeOfValorAbilityHandler : RR5RealmAbility
    {
        public BadgeOfValorAbilityHandler(DBAbility dba, int level) : base(dba, level) { }

        int m_reuseTimer = 900;

        public override void Execute(GameLiving living)
        {
            #region preCheck
            if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;

            if (living.EffectList.CountOfType<BadgeOfValorEffect>() > 0)
            {
                if (living is GamePlayer gp)
                    gp.Out.SendMessage(LanguageMgr.GetTranslation(gp.Client, "DashingDefenseAbility.Execute.AlreadyEffect"), eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                return;
            }

            #endregion


            //send spelleffect
            foreach (GamePlayer visPlayer in living.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                visPlayer.Out.SendSpellEffectAnimation(living, living, 7057, 0, false, 0x01);

            new BadgeOfValorEffect().Start(living);
            living.DisableSkill(this, m_reuseTimer * 1000);
        }
    }
}
