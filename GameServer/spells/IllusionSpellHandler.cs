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
using DOL.GS.Effects;
using DOL.GS;
using DOL.GS.Geometry;
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
            CreateIllusionPets();
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

        private void CreateIllusionPets()
        {
            int numPets = (int)Spell.Value;
            if (numPets < 1) numPets = 1;

            double angleIncrement = 360.0 / numPets;
            double currentAngle = 0;

            for (int i = 0; i < numPets; i++)
            {
                IllusionPet pet = CreatePet();
                if (pet == null)
                {
                    continue;
                }

                SetupPet(pet);

                int distance = 150;
                double radians = currentAngle * (Math.PI / 180.0);

                int x = (int)(Caster.Position.X + distance * Math.Cos(radians));
                int y = (int)(Caster.Position.Y + distance * Math.Sin(radians));
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

        protected virtual IllusionPet CreatePet()
        {
            return new IllusionPet(Caster as GamePlayer, Spell.Duration);
        }

        protected void SetupPet(IllusionPet pet)
        {
            if (pet == null)
                return;

            pet.Level = Caster.Level;

            double damageMultiplier = Spell.Damage / 100.0;
            pet.Effectiveness = damageMultiplier;

            double hpMultiplier = Spell.AmnesiaChance / 100.0;
            if (hpMultiplier <= 0) hpMultiplier = 0.01;
            pet.MaxHealth = (int)(Caster.MaxHealth * hpMultiplier);
            pet.Health = pet.MaxHealth;

            pet.Model = Spell.LifeDrainReturn == 0 ? Caster.Model : (ushort)Spell.LifeDrainReturn;

            pet.Name = Caster.Name;

            pet.Realm = Caster.Realm;
            pet.CurrentRegion = Caster.CurrentRegion;
            pet.Heading = Caster.Heading;

            // Use SetOwnBrain to assign the pet's brain
            pet.SetOwnBrain(new IllusionPetBrain(Caster));
        }

        public override bool ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
            GameSpellEffect effect = new GameSpellEffect(this, Spell.Duration * 1000, 0, 1.0);
            effect.Start(target);
            return true;
        }
    }
}