using System;
using System.Collections;
using System.Timers;
using DOL.GS;
using DOL.Database;
using DOL.GS.Scripts;
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.GS.SkillHandler;
using DOL.GS.Spells;
using DOL.GS.Effects;
using DOL.AI.Brain;

namespace DOL.GS.Scripts
{
    public class Antipass : GameNPC
    {
        public override bool AddToWorld()
        {
            this.SetOwnBrain(new AntipassBrain());
            Brain.Start();
            base.AddToWorld();
            Name = "No Pass";
            Flags |= GameNPC.eFlags.PEACE;
            //Flags |= (uint)GameNPC.eFlags.CANTTARGET;
            Flags |= GameNPC.eFlags.FLYING;
            Model = 10;
            Size = 50;
            Level = 90;
            MaxSpeedBase = 0;
            return true;
        }
    }
}

namespace DOL.AI.Brain
{
    public class AntipassBrain : StandardMobBrain
    {
        public AntipassBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 400;
        }

        /// <inheritdoc />
        public override int ThinkInterval => 50;

        public override void Think()
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius((ushort)AggroRange))
            {
                if (player.Client.Account.PrivLevel != 3)
                {
                    double angle = 0.00153248422;
                    player.MoveTo(player.CurrentRegionID, (int)(Body.Position.X - ((AggroRange + 10) * Math.Sin(angle * Body.Heading))), (int)(Body.Position.Y + ((AggroRange + 10) * Math.Cos(angle * Body.Heading))), Body.Position.Z, player.Heading);
                }
            }
        }
    }
}