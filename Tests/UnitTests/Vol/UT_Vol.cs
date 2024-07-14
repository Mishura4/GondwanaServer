using DOL.GS;
using DOL.GS.Commands;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DOL.Database;
using DOL.Network;
using DOL.Config;
using DOL.GS.Finance;
using DOL.UnitTests.Gameserver;

namespace DOL.UnitTests.Gameserver.Vol
{
    [TestFixture]
    public class UT_Vol
    {
        internal class StealTestPlayer : FakePlayer
        {
            public bool m_isStealthed;
            
            /// <inheritdoc />
            public override bool IsStealthed
            {
                get => m_isStealthed;
            }
            

            public override bool HasAbility(string keyName)
            {
                return true;
            }

            /// <inheritdoc />
            public override int GetBaseSpecLevel(string keyName)
            {
                return 10;
            }
        }
        
        StealTestPlayer stealer;
        StealTestPlayer target;

        [OneTimeSetUp]
        public void Init()
        {
            stealer = new StealTestPlayer();
            target = new StealTestPlayer();
        }

        [SetUp]
        public void Setup()
        {
            stealer.m_isStealthed = true;
            target.AddMoney(Currency.Copper.Mint(100));
        }

        [Test]
        public void ShouldLowLevelPlayerCanNotVol()
        {
            stealer.Level = 15;
            target.Level = 15;

            Assert.AreEqual(false, StealCommandHandler.CanVol(stealer, target));
        }

        [Test]
        public void ShouldHigherPlayerCanNotVolLowLevels()
        {
            stealer.Level = 25;
            target.Level = 15;

            Assert.AreEqual(false, StealCommandHandler.CanVol(stealer, target));
        }

        [Test]
        public void ShoulStealdHigherPlayer()
        {
            stealer.Level = 25;
            target.Level = 30;

            Assert.AreEqual(true, StealCommandHandler.CanVol(stealer, target));
        }

        [Test]
        public void ShouldHigherPlayerCanNotLostStealthAgainstLowerLevels()
        {
            stealer.Level = 45;
            target.Level = 24;
            var res = StealCommandHandler.Vol(stealer, target);
            Assert.AreEqual(true, res.Status != StealResultStatus.STEALTHLOST);
        }

        [Test]
        public void ShouldPlayerWithHigherTargetLevelLostStealth()
        {
            stealer.Level = 25;
            target.Level = 48;

            Assert.AreEqual(StealResultStatus.STEALTHLOST, StealCommandHandler.Vol(stealer, target).Status);
        }

        [Test]
        public void ShouldPlayerWithLowerTargetLevelNotLostStealth()
        {
            stealer.Level = 48;
            target.Level = 25;

            var res = StealCommandHandler.Vol(stealer, target);
            Assert.AreEqual(true, res.Status != StealResultStatus.STEALTHLOST);
        }

        [Test]
        public void ShouldPlayerLostStealth()
        {
            stealer.Level = 25;
            target.Level = 48;

            Assert.AreEqual(StealResultStatus.STEALTHLOST, StealCommandHandler.Vol(stealer, target).Status);
        }

        /*   
        Je pense qu'en effet il faudrait faire en sorte que la somme volée 
        soit multipliée par un facteur correspondant au level du joueur.
        Aussi, fixer cette somme random entre 5% et 70% de l'argent en poche chez le joueur,
        pour ne pas non plus qu'il perde tout, ce serait trop sinon.

        Cela pourrait être en effet atténué selon le level du joueur.
     
         */

        [Test]
        public void VolAbility_ShouldSameLevelRangeShouldNotLostStealth()
        {
            stealer.Level = 30;
            target.Level = 35;

            var result = StealCommandHandler.Vol(stealer, target);

            Assert.AreNotEqual(StealResultStatus.STEALTHLOST, result.Status);
        }



    }
}