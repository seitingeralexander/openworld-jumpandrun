namespace JumpAndRun.Simulation
{
    /// <summary>
    /// RPG-like statistics for the player.
    /// </summary>
    public class PlayerStats
    {
        public int Level { get; set; } = 1;
        public int Experience { get; set; } = 0;
        public int ExperienceToNextLevel { get; set; } = 100;
        
        public int MaxHealth { get; set; } = 100;
        public int CurrentHealth { get; set; } = 100;
        
        public int MaxStamina { get; set; } = 100;
        public int CurrentStamina { get; set; } = 100;
        
        // Base stats
        public int Strength { get; set; } = 10;
        public int Agility { get; set; } = 10;
        public int Intelligence { get; set; } = 10;
        public int Luck { get; set; } = 10;

        public void AddExperience(int amount)
        {
            Experience += amount;
            while (Experience >= ExperienceToNextLevel)
            {
                Experience -= ExperienceToNextLevel;
                LevelUp();
            }
        }

        private void LevelUp()
        {
            Level++;
            ExperienceToNextLevel = Level * 100; // Simple scaling
            MaxHealth += 10;
            CurrentHealth = MaxHealth;
            MaxStamina += 5;
            CurrentStamina = MaxStamina;
        }
    }
}
