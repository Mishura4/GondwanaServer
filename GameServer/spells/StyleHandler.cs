/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
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
using System;
using System.Collections;
using System.Collections.Generic;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.GS.PacketHandler.Client.v168;
using DOL.GS.Styles;

namespace DOL.GS.Spells
{
    [SpellHandlerAttribute("StyleHandler")]
    public class StyleHandler : SpellHandler
    {
        public StyleHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        /// <inheritdoc />
        public override bool StartSpell(GameLiving target, bool force = false)
        {
            int classID = Spell.AmnesiaChance == 0 ? (Caster as GamePlayer)?.CharacterClass.ID ?? 0 : Spell.AmnesiaChance;
            Style style = SkillBase.GetStyleByID((int)Spell.Value, classID);
            //Andraste - Vico : try to use classID=0 (easy way to implement CL Styles)
            if (style == null) style = SkillBase.GetStyleByID((int)Spell.Value, 0);
            if (style != null)
            {
                StyleProcessor.TryToUseStyle(Caster, target, style);
                return true;
            }
            else
            {
                (Caster as GamePlayer)?.Out.SendMessage("That style is not implemented!", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                return false;
            }
        }

        /// <inheritdoc />
        public override bool CastSpell(GameLiving targetObject)
        {
            bool success = StartSpell(targetObject, false);
            
            // This is critical to restore the casters state and allow them to cast another spell
            if (!IsCasting)
                OnAfterSpellCastSequence();
            
            return success;
        }

        public override IList<string> DelveInfo
        {
            get
            {
                var list = new List<string>();
                list.Add(Spell.Description);

                GamePlayer player = Caster as GamePlayer;

                if (player != null)
                {
                    list.Add(" ");

                    Style style = SkillBase.GetStyleByID((int)Spell.Value, 0);
                    if (style == null)
                    {
                        style = SkillBase.GetStyleByID((int)Spell.Value, player.CharacterClass.ID);
                    }

                    if (style != null)
                    {
                        DetailDisplayHandler.WriteStyleInfo(list, style, player.Client);
                    }
                    else
                    {
                        list.Add("Style not found.");
                    }
                }

                return list;
            }
        }

    }


}

