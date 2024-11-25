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
using DOL.GS.PacketHandler;
using DOL.Language;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS.Effects;
using DOL.GS;
using DOL.GS.Geometry;
using DOL.GS.Scripts;
using System;
using log4net;

namespace DOL.GS.Spells
{
    [SpellHandler("IllusionSpell")]
    public class IllusionSpell : SpellHandler
    {
        private List<IllusionPet> illusionPets = new List<IllusionPet>();

        public IllusionSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        public override void OnEffectStart(GameSpellEffect effect)
        {
            base.OnEffectStart(effect);
            CreateIllusionPets(effect.Owner as GamePlayer);
        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            foreach (IllusionPet pet in illusionPets)
            {
                if (pet.IsAlive)
                {
                    foreach (GamePlayer player in pet.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                    {
                        player.Out.SendSpellEffectAnimation(pet, pet, 7202, 0, false, 1);
                    }
                    pet.Delete();
                }
            }
            illusionPets.Clear();

            return base.OnEffectExpires(effect, noMessages);
        }

        private void CreateIllusionPets(GamePlayer target)
        {
            int numPets = (int)Spell.Value;
            if (numPets < 1) numPets = 1;

            double angleIncrement = 360.0 / numPets;
            double currentAngle = 0;
            
            GameNpcInventoryTemplate npcInventory = new GameNpcInventoryTemplate();
            InventoryItem item;
            foreach (eInventorySlot slot in PlayerCloner.itemsToCopy)
            {
                item = target.Inventory.GetItem(slot);
                if (item != null)
                {
                    int itemModel = item.Model;

                    // keep clothing manager uses model IDs of 3800-3802 for guild cloaks... doesn't seem to work here :(
                    //if ( item.SlotPosition == (int)eInventorySlot.Cloak && item.Emblem > 0 )
                    //	itemModel = 3799 + (int)player.Realm;

                    npcInventory.AddNPCEquipment(slot, item.Model, item.Color, item.Effect, item.Extension);
                }
            }
            npcInventory.CloseTemplate();

            var styles = PlayerCloner.GetStyles(target);
            var spells = PlayerCloner.GetSpells(target);

            for (int i = 0; i < numPets; i++)
            {
                IllusionPet pet = CreatePet(target);
                if (pet == null)
                {
                    continue;
                }

                int distance = 150;
                double radians = currentAngle * (Math.PI / 180.0);

                pet.Inventory = npcInventory;
                pet.Styles = styles;
                pet.Spells = spells;
                pet.LoadedFromScript = true;
                pet.AutoRespawn = false;
                pet.GuildName = target.GuildName;
                pet.Realm = target.Realm;

                double xOffset = distance * Math.Cos(radians);
                double yOffset = distance * Math.Sin(radians);
                (pet.Brain as IllusionPetBrain).SetOffset(xOffset, yOffset);
                int x = (int)(Caster.Position.X + xOffset);
                int y = (int)(Caster.Position.Y + yOffset);
                int z = (int)Caster.Position.Z;

                // Set the position using Position.Create
                pet.Position = Position.Create(Caster.CurrentRegionID, x, y, z, Caster.Heading);

                pet.AddToWorld();
                illusionPets.Add(pet);

                foreach (GamePlayer player in pet.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                {
                    player.Out.SendSpellEffectAnimation(pet, pet, 7202, 0, false, 1);
                }

                currentAngle += angleIncrement;
            }
        }

        protected virtual IllusionPet CreatePet(GamePlayer target)
        {
            var pet = new IllusionPet(target, Spell.Duration);
            pet.Owner = Caster;
            pet.Level = target.Level;
            return pet;
        }

        public override bool ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
            if (target is not GamePlayer)
            {
                return false;
            }
            
            GameSpellEffect effect = new GameSpellEffect(this, Spell.Duration * 1000, 0, 1.0);
            effect.Start(target);
            return true;
        }
    }
}