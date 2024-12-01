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
using DOL.GS.Styles;
using DOL.GS.Utils;
using System;
using log4net;
using System.Collections;
using System.Numerics;
using Vector = DOL.GS.Geometry.Vector;

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
        
        public enum eSpawnType
        {
            Random = 0,
            Circle,
            Line
        }

        private void CreateIllusionPets(GamePlayer target)
        {
            int numPets = (int)Spell.Value;
            if (numPets < 1) numPets = 1;
            
            GameNpcInventoryTemplate npcInventory = new GameNpcInventoryTemplate();
            InventoryItem item;
            foreach (eInventorySlot slot in PlayerCloner.itemsToCopy)
            {
                item = target.Inventory.GetItem(slot);
                if (item != null)
                {
                    // keep clothing manager uses model IDs of 3800-3802 for guild cloaks... doesn't seem to work here :(
                    //if ( item.SlotPosition == (int)eInventorySlot.Cloak && item.Emblem > 0 )
                    //	itemModel = 3799 + (int)player.Realm;

                    npcInventory.AddNPCEquipment(slot, item.Model, item.Color, item.Effect, item.Extension);
                }
            }
            npcInventory.CloseTemplate();

            var styles = PlayerCloner.GetStyles(target);
            var spells = PlayerCloner.GetSpells(target);

            int maxDistance = Spell.Radius == 0 ? 150 : Spell.Radius;
            Vector[] offsets = new Vector[numPets + 1];
            IllusionPet.eIllusionFlags mode = 0;

            switch ((eSpawnType)this.Spell.DamageType)
            {
                case eSpawnType.Random:
                    {
                        // We don't spawn them at actual random positions, because randomness is clumpy: https://conversableeconomist.blogspot.com/2015/03/randomness-is-lumpy-pareidolia.html
                        // Instead, we spawn them in a circle and slightly change the positions.
                        double increment = 2 * Math.PI / numPets;
                        double currentAngle = 0.0;
                        for (int i = 0; i < numPets; i++)
                        {
                            double angle = currentAngle + (Util.RandomDouble() * increment) * 1.5 - (0.5 * increment);
                            double distance = Util.RandomDouble();
                            distance = maxDistance * (1 - (distance * distance)); // Randomize distance closer to the edge...
                            offsets[i] = Vector.Create((int)Math.Round(distance * Math.Cos(angle)), (int)Math.Round(distance * Math.Sin(angle), 0));
                            currentAngle += increment;
                        }
                        offsets[numPets] = Vector.Zero;
                        mode |= IllusionPet.eIllusionFlags.RandomizePositions;
                    }
                    break;

                case eSpawnType.Circle:
                    {
                        double increment = 2 * Math.PI / numPets;
                        double currentAngle = 0.0;
                        for (int i = 0; i < numPets; i++)
                        {
                            double angle = currentAngle + increment;
                            offsets[i] = Vector.Create((int)Math.Round(maxDistance * Math.Cos(angle)), (int)Math.Round(maxDistance * Math.Sin(angle), 0));
                            currentAngle += increment;
                        }
                        offsets[numPets] = Vector.Zero;
                    }
                    break;
                
                case eSpawnType.Line:
                    {
                        int distance = Spell.Radius == 0 ? 30 : (int)Math.Round(Spell.Radius / (numPets + 1.00));
                        double angle = target.Orientation.InRadians;
                        var direction = Vector.Create((int)Math.Round(distance * Math.Cos(angle)), (int)Math.Round(distance * Math.Sin(angle), 0));
                        int i = 0;
                        int midPoint = numPets / 2;
                        while (i < midPoint)
                        {
                            offsets[i] = direction * (i - midPoint);
                            ++i;
                        }
                        while (i < numPets + 1)
                        {
                            offsets[i] = direction * ((i + 1) - midPoint);
                            ++i;
                        }
                        offsets[numPets] = Vector.Zero;
                    }
                    break;
            }
            bool hasLOS = LosCheckMgr.HasDataFor(target.CurrentRegion);
            Util.Shuffle(offsets);
            Coordinate masterCoord = target.Coordinate + offsets[numPets];
            for (int i = 0; i < numPets; i++)
            {
                IllusionPet pet = CreatePet(target, mode, styles, spells, npcInventory);
                if (pet == null)
                {
                    continue;
                }

                var offset = offsets[i];
                Coordinate coord = target.Coordinate + offset;
                var walkOffset = coord - masterCoord;
                (pet.Brain as IllusionPetBrain).SetOffset(walkOffset.X, walkOffset.Y);
                if (hasLOS) // Check LOS for initial placements & teleport, but then walk to the planned offset
                {
                    RaycastStats stats = new();
                    var point = PathingMgr.LocalPathingMgr.GetClosestPointAsync(target.CurrentZone, coord, maxDistance, maxDistance, maxDistance);
                    if (point != null)
                    {
                        coord = Coordinate.Create(point.Value);
                        offset = coord - target.Coordinate;
                    }
                    float dist = LosCheckMgr.GetCollisionDistance(target.CurrentRegion, target.Coordinate, coord, ref stats);
                    if (dist < float.MaxValue)
                    {
                        var normalDistance = offset.Length;
                        var ratio = (dist / normalDistance);
                        offset = Vector.Create((int)Math.Round(offset.X * ratio), (int)Math.Round(offset.Y * ratio), (int)Math.Round(offset.Z * ratio));
                    }
                    pet.Position = target.Position + offset;
                }
                else // Walk to destination
                {
                    pet.Position = target.Position;
                }

                illusionPets.Add(pet);
            }
            for (int i = 0; i < numPets; ++i)
            {
                var living = illusionPets[i];
                living.AddToWorld();
                living.PathTo(target.Coordinate + offsets[i], target.MaxSpeed);
            }
            if (hasLOS)
            {
                target.MoveTo(target.Position + offsets[numPets]);
            }
            foreach (GamePlayer player in target.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                player.Out.SendSpellEffectAnimation(target, target, 7202, 0, false, 1);
                foreach (var living in illusionPets)
                {
                    player.Out.SendSpellEffectAnimation(living, living, 7202, 0, false, 1);
                }
            }
        }
        
        protected virtual IllusionPet CreatePet(GamePlayer target, IllusionPet.eIllusionFlags mode, List<Style> styles, IList spells, GameNpcInventoryTemplate npcInventory)
        {
            var pet = new IllusionPet(target, mode);
            pet.Owner = Caster;
            pet.Level = target.Level;

            pet.Inventory = npcInventory;
            pet.Styles = styles;
            pet.Spells = spells;
            pet.LoadedFromScript = true;
            pet.AutoRespawn = false;
            pet.GuildName = target.GuildName;
            pet.Realm = target.Realm;
            return pet;
        }

        public override bool ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
            if (target is not GamePlayer)
            {
                return false;
            }
            
            GameSpellEffect effect = new GameSpellEffect(this, Spell.Duration, 0, 1.0);
            effect.Start(target);
            return true;
        }
    }
}