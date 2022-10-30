namespace DOL.GS
{
    public class GameBossNPC : GameNPC
    {

        public GameBossNPC()
            : base()
        {
            LoadedFromScript = false;
        }

        public override int MaxHealth
        {
            get
            {
                return base.MaxHealth * 10;
            }
        }
    }

    // Les boss de challenge
    // très dur
    class GameChallengeNPC : GameNPC
    {
        public GameChallengeNPC()
            : base()
        {
            LoadedFromScript = false;
        }

        public override int MaxHealth
        {
            get
            {
                return base.MaxHealth * 50;
            }
        }
    }

    // Hardest mob
    // Très dur, mais prévu pour les event avec beaucoup de joueur
    class GameMaxLifeNPC : GameNPC
    {

        public GameMaxLifeNPC()
            : base()
        {
            LoadedFromScript = false;
        }

        public override int MaxHealth
        {
            get
            {
                return base.MaxHealth * 100;
            }
        }
    }
}