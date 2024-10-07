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
using System.Linq;
using DOL.AI.Brain;
using DOL.GS.PacketHandler;
using DOL.GS;
using DOL.Database;
using System.Collections.Generic;

namespace DOL.GS
{
    public class IllusionPet : GameNPC
    {
        public GameLiving IllusionOwner { get; set; }

        public IllusionPet(GamePlayer owner, int durationSeconds)
        {
            IllusionOwner = owner;
            CopyAppearanceFromOwner(owner);
        }

        protected void CopyAppearanceFromOwner(GamePlayer owner)
        {
            if (owner == null)
                return;

            Name = owner.Name;
            Level = owner.Level;
            Model = owner.Model;
            Size = (byte)owner.Size;
            Realm = owner.Realm;
            Race = owner.Race;
            Gender = owner.Gender;
            MaxSpeedBase = owner.MaxSpeedBase;
            MaxHealth = owner.MaxHealth;
            Health = MaxHealth;

            // Copy equipment
            CopyEquipment(owner);

            // Copy styles if needed
            var ownerStyles = owner.GetStyleList();
            if (ownerStyles != null)
            {
                Styles = ownerStyles.Cast<Styles.Style>().ToList();
            }

            // Copy other appearance details
            GuildName = owner.GuildName;
            IsCloakHoodUp = owner.IsCloakHoodUp;
            IsCloakInvisible = owner.IsCloakInvisible;
            IsHelmInvisible = owner.IsHelmInvisible;

            // Copy stats
            Strength = (short)owner.Strength;
            Constitution = (short)owner.Constitution;
            Dexterity = (short)owner.Dexterity;
            Quickness = (short)owner.Quickness;
            Intelligence = (short)owner.Intelligence;
            Empathy = (short)owner.Empathy;
            Piety = (short)owner.Piety;
            Charisma = (short)owner.Charisma;
        }

        private void CopyEquipment(GamePlayer owner)
        {
            if (owner == null)
                return;

            GameNpcInventoryTemplate npcInventory = new GameNpcInventoryTemplate();
            foreach (eInventorySlot slot in Enum.GetValues(typeof(eInventorySlot)))
            {
                InventoryItem item = owner.Inventory.GetItem(slot);
                if (item != null)
                {
                    npcInventory.AddNPCEquipment(slot, item.Model, item.Color, item.Effect, item.Extension);
                }
            }
            npcInventory.CloseTemplate();
            Inventory = npcInventory;

            // Set active weapon slot to match owner
            if (owner.AttackWeapon != null)
            {
                if (owner.AttackWeapon.Hand == 2)
                    SwitchWeapon(GameLiving.eActiveWeaponSlot.TwoHanded);
                else
                    SwitchWeapon(GameLiving.eActiveWeaponSlot.Standard);
            }
            else
            {
                SwitchWeapon(GameLiving.eActiveWeaponSlot.Standard);
            }

            BroadcastLivingEquipmentUpdate();
        }

        public override void Die(GameObject killer)
        {
            foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                player.Out.SendSpellEffectAnimation(this, this, 7202, 0, false, 1);
            }

            base.Die(killer);
        }
    }
}