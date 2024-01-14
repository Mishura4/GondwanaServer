/*
 * GONDWANA SERVER DAOC RP/PvP/GvG server, following Amtenael and Avalonia
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */

using System.Reflection;
using DOL.GS.PacketHandler;
using DOL.Language;
using DOL.Numbers;
using log4net;

namespace DOL.GS;

public class CraftAction : RegionAction
{
    /// <summary>
    /// Defines a logger for this class.
    /// </summary>
    private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

    /// <summary>
    /// The current recipe being crafted
    /// </summary>
    private Recipe m_recipe;

    /// <summary>
    /// The current recipe being crafted
    /// </summary>
    public Recipe CraftedRecipe
    {
        get { return m_recipe; }
    }

    /// <summary>
    /// The time for the current recipe
    /// </summary>
    public int RecipeTime { get; }

    /// <summary>
    /// The crafting skill being used
    /// </summary>
    public AbstractCraftingSkill CraftingSkill { get; }

    /// <summary>
    /// Whether we are finished crafting
    /// </summary>
    private bool m_finished;

    /// <summary>
    /// The time spent crafting the current item
    /// </summary>
    private int m_craftingTime;

    public static readonly int TICK_RATE = 50;

    public CraftAction(GamePlayer player, AbstractCraftingSkill skill, Recipe craftedRecipe, int craftingTime) : base(player)
    {
        m_recipe = craftedRecipe;
        RecipeTime = craftingTime * 1000;
        CraftingSkill = skill;
        m_craftingTime = 0;
        Interval = TICK_RATE;
        Start(TICK_RATE);
    }

    public override void Stop()
    {
        GamePlayer player = m_actionSource as GamePlayer;
        player.Out.SendCloseTimerWindow();
        player.TempProperties.removeProperty("CraftQueueRemaining");
        base.Stop();
    }

    protected virtual void MakeItem()
    {
        GamePlayer player = m_actionSource as GamePlayer;
        int queue = player.TempProperties.getProperty<int>("CraftQueueLength");
        int remainingToCraft = player.TempProperties.getProperty<int>("CraftQueueRemaining");

        if (player == null || m_recipe == null/* || skill == null */)
        {
            player?.Out.SendMessage("Could not find recipe or item to craft!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
            log.Error("Crafting.MakeItem: Could not retrieve player, recipe, or raw materials to craft from CraftTimer.");
            m_finished = true;
            return;
        }
 
        if (queue > 1 && remainingToCraft == 0)
            remainingToCraft = queue;

        if (Util.Chance(CraftingSkill.CalculateChanceToMakeItem(player, m_recipe.Level)))
        {
            if (!CraftingSkill.RemoveUsedMaterials(player, m_recipe))
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "AbstractCraftingSkill.MakeItem.NotAllMaterials"), eChatType.CT_System, eChatLoc.CL_SystemWindow);

                if (player.Client.Account.PrivLevel == 1)
                {
                    Stop();
                    return;
                }
            }
            CraftingSkill.BuildCraftedItem(player, m_recipe);
            CraftingSkill.GainCraftingSkillPoints(player, m_recipe);
        }
        else
        {
            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "AbstractCraftingSkill.MakeItem.LoseNoMaterials", m_recipe.Product.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            player.Out.SendPlaySound(eSoundType.Craft, 0x02);
        }

        if (remainingToCraft > 1)
        {
            if (CraftingSkill.CheckRawMaterials(player, m_recipe))
            {
                player.TempProperties.setProperty("CraftQueueRemaining", remainingToCraft - 1);
                m_craftingTime = 0;
                player.Out.SendTimerWindow(LanguageMgr.GetTranslation(player.Client.Account.Language, "AbstractCraftingSkill.CraftItem.CurrentlyMaking", m_recipe.Product.Name), CraftingSkill.GetCraftingTime(player, m_recipe));
                m_finished = false;
            }
            else
                m_finished = true;
        }
        else
        {
            player.TempProperties.removeProperty("CraftQueueRemaining");
            m_finished = true;
        }
    }

    /// <inheritdoc />
    protected override void OnTick()
    {
        GamePlayer player = m_actionSource as GamePlayer;

        if (player.IsMezzed || player.IsStunned)
        {
            Stop();
            return;
        }

        if (player.CurrentSpellHandler?.Spell.Uninterruptible == false)
        {
            Stop();
            return;
        }

        if (player.IsMoving)
        {
            Stop();
            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "AbstractCraftingSkill.CraftItem.MoveAndInterrupt"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            return;
        }

        m_craftingTime += TICK_RATE;
        if (m_craftingTime > RecipeTime)
        {
            MakeItem();
            if (m_finished)
            {
                Stop();
            }
        }
    }
}
