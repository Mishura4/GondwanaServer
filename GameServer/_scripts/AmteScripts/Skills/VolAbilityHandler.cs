using DOL.GS.Commands;
using DOL.GS.PacketHandler;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS.SkillHandler
{
    /// <summary>
    /// Vol Ability Handler
    /// </summary>
    [SkillHandlerAttribute(Abilities.Vol)]
    public class VolAbilityHandler : IAbilityActionHandler
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        // On peut voler toutes les minutes
        public static int DISABLE_DURATION = ServerProperties.Properties.VOL_DELAY * 60 * 1000;
        public const string DISABLE_PROPERTY = "vol_disable_save";

        public void Execute(Ability Ab, GamePlayer player)
        {
            if (!IsClassAllowedToSteal(player))
            {
                log.Debug("VOL : VolAbilityHandler.Execute(): Class Not Permit to Steal");
                return;
            }

            if (log.IsDebugEnabled)
                log.Debug("VOL : Trying to execute the ability");

            if (player.IsMezzed)
            {
                player.Out.SendMessage("Vous ne pouvez voler en étant hypnotisé!",
                    eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            if (player.IsStunned)
            {
                player.Out.SendMessage("Vous ne pouvez pas voler en étant assomé(e)!",
                    eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }
            if (player.PlayerAfkMessage != null)
            {
                player.Out.SendMessage("Vous ne pouvez pas voler lorsque vous " +
                    "êtes afk! Tapez /afk pour le désactiver.",
                    eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }
            if (!player.IsAlive)
            {
                player.Out.SendMessage("Vous ne pouvez voler en étant mort(e)!",
                    eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            if (player.TargetObject != null && player.TargetObject is GamePlayer)
            {
                if (log.IsDebugEnabled)
                    log.Debug("VOL : Entering StealCommandHandler.OnCommand()");

                new StealCommandHandler().OnCommand(player.Client,
                    new string[] { "/vol" });

                if (log.IsDebugEnabled)
                    log.Debug("VOL : StealCommandHandler.OnCommand() exited");
            }
            else
            {
                player.Out.SendMessage("Vous devez sélectionner un " +
                    "personnage joueur!",
                    eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }
        }

        private bool IsClassAllowedToSteal(GamePlayer player)
        {
            switch (player.CharacterClass.ID)
            {
                case (byte)eCharacterClass.AlbionRogue:
                case (byte)eCharacterClass.MidgardRogue:
                case (byte)eCharacterClass.Stalker:
                case (byte)eCharacterClass.Minstrel:
                case (byte)eCharacterClass.Infiltrator:
                case (byte)eCharacterClass.Scout:
                case (byte)eCharacterClass.Hunter:
                case (byte)eCharacterClass.Shadowblade:
                case (byte)eCharacterClass.Ranger:
                case (byte)eCharacterClass.Nightshade:
                    return true;

                default:
                    return false;
            }
        }
    }
}