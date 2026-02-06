using PathPilot.Core.Models;
using System.Collections.Generic;

namespace PathPilot.Core.Services;

public class QuestDataService
{
    public List<Quest> GetAllQuests()
    {
        return new List<Quest>
        {
            // === SKILL POINT QUESTS ===

            // Act 1 (2 points)
            new() { Name = "The Dweller of the Deep", Act = 1, Reward = QuestReward.SkillPoint, RewardDescription = "+1 Passive", Location = "The Flooded Depths", RecommendedLevel = 8 },
            new() { Name = "The Marooned Mariner", Act = 1, Reward = QuestReward.SkillPoint, RewardDescription = "+1 Passive", Location = "The Ship Graveyard Cave", RecommendedLevel = 10 },

            // Act 2 (2-3 points)
            new() { Name = "The Great White Beast", Act = 2, Reward = QuestReward.SkillPoint, RewardDescription = "+1 Passive", Location = "The Den", RecommendedLevel = 15 },
            new() { Name = "Deal with the Bandits", Act = 2, Reward = QuestReward.SkillPoint, RewardDescription = "+2 Passives (kill all)", Location = "Forest Encampment", RecommendedLevel = 18 },

            // Act 3 (2 points)
            new() { Name = "Victario's Secrets", Act = 3, Reward = QuestReward.SkillPoint, RewardDescription = "+1 Passive", Location = "The Sewers", RecommendedLevel = 24 },
            new() { Name = "Piety's Pets", Act = 3, Reward = QuestReward.SkillPoint, RewardDescription = "+1 Passive", Location = "The Lunaris Temple Level 2", RecommendedLevel = 26 },

            // Act 4 (1 point)
            new() { Name = "An Indomitable Spirit", Act = 4, Reward = QuestReward.SkillPoint, RewardDescription = "+1 Passive", Location = "The Mines Level 2 (Deshret)", RecommendedLevel = 32 },

            // Act 5 (2 points)
            new() { Name = "In Service to Science", Act = 5, Reward = QuestReward.SkillPoint, RewardDescription = "+1 Passive", Location = "The Control Blocks", RecommendedLevel = 38 },
            new() { Name = "Kitava's Torments", Act = 5, Reward = QuestReward.SkillPoint, RewardDescription = "+1 Passive", Location = "The Reliquary", RecommendedLevel = 40 },

            // Act 6 (3 points)
            new() { Name = "The Father of War", Act = 6, Reward = QuestReward.SkillPoint, RewardDescription = "+1 Passive", Location = "The Karui Fortress", RecommendedLevel = 45 },
            new() { Name = "The Puppet Mistress", Act = 6, Reward = QuestReward.SkillPoint, RewardDescription = "+1 Passive", Location = "The Mud Flats (Tukohama)", RecommendedLevel = 42 },
            new() { Name = "The Cloven One", Act = 6, Reward = QuestReward.SkillPoint, RewardDescription = "+1 Passive", Location = "The Prisoner's Gate (Abberath)", RecommendedLevel = 44 },

            // Act 7 (3 points)
            new() { Name = "The Master of a Million Faces", Act = 7, Reward = QuestReward.SkillPoint, RewardDescription = "+1 Passive", Location = "The Ashen Fields (Greust)", RecommendedLevel = 48 },
            new() { Name = "Queen of Despair", Act = 7, Reward = QuestReward.SkillPoint, RewardDescription = "+1 Passive", Location = "The Causeway (Gruthkul)", RecommendedLevel = 50 },
            new() { Name = "Kishara's Star", Act = 7, Reward = QuestReward.SkillPoint, RewardDescription = "+1 Passive", Location = "The Causeway", RecommendedLevel = 50 },

            // Act 8 (3 points)
            new() { Name = "Love is Dead", Act = 8, Reward = QuestReward.SkillPoint, RewardDescription = "+1 Passive", Location = "The Quay (Clarissa)", RecommendedLevel = 54 },
            new() { Name = "Reflection of Terror", Act = 8, Reward = QuestReward.SkillPoint, RewardDescription = "+1 Passive", Location = "The High Gardens (Yugul)", RecommendedLevel = 55 },
            new() { Name = "The Gemling Legion", Act = 8, Reward = QuestReward.SkillPoint, RewardDescription = "+1 Passive", Location = "The Grain Gate", RecommendedLevel = 55 },

            // Act 9 (2 points)
            new() { Name = "Queen of the Sands", Act = 9, Reward = QuestReward.SkillPoint, RewardDescription = "+1 Passive", Location = "The Oasis (Shakari)", RecommendedLevel = 60 },
            new() { Name = "The Ruler of Highgate", Act = 9, Reward = QuestReward.SkillPoint, RewardDescription = "+1 Passive", Location = "The Quarry (Kira)", RecommendedLevel = 62 },

            // Act 10 (2 points)
            new() { Name = "Vilenta's Vengeance", Act = 10, Reward = QuestReward.SkillPoint, RewardDescription = "+1 Passive", Location = "The Control Blocks", RecommendedLevel = 64 },
            new() { Name = "An End to Hunger", Act = 10, Reward = QuestReward.SkillPoint, RewardDescription = "+1 Passive", Location = "The Feeding Trough", RecommendedLevel = 66 },

            // === ASCENDANCY TRIALS ===

            // Normal Lab Trials (Act 1-3) - unlock Normal Labyrinth
            new() { Name = "Trial of Ascendancy", Act = 1, Reward = QuestReward.AscendancyTrial, RewardDescription = "Normal Lab", Location = "The Lower Prison", TrapType = "Spike Traps", RecommendedLevel = 9, IsOptional = true },
            new() { Name = "Trial of Ascendancy", Act = 2, Reward = QuestReward.AscendancyTrial, RewardDescription = "Normal Lab", Location = "The Crypt Level 1", TrapType = "Spinning Blades", RecommendedLevel = 15, IsOptional = true },
            new() { Name = "Trial of Ascendancy", Act = 2, Reward = QuestReward.AscendancyTrial, RewardDescription = "Normal Lab", Location = "The Chamber of Sins Level 2", TrapType = "Sawblades", RecommendedLevel = 18, IsOptional = true },
            new() { Name = "Trial of Ascendancy", Act = 3, Reward = QuestReward.AscendancyTrial, RewardDescription = "Normal Lab", Location = "The Crematorium", TrapType = "Furnace Traps", RecommendedLevel = 25, IsOptional = true },
            new() { Name = "Trial of Ascendancy", Act = 3, Reward = QuestReward.AscendancyTrial, RewardDescription = "Normal Lab", Location = "The Catacombs", TrapType = "Blade Sentries", RecommendedLevel = 27, IsOptional = true },
            new() { Name = "Trial of Ascendancy", Act = 3, Reward = QuestReward.AscendancyTrial, RewardDescription = "Normal Lab", Location = "The Imperial Gardens", TrapType = "Spike Traps & Dart Traps", RecommendedLevel = 29, IsOptional = true },

            // Cruel Lab Trials (Act 6-7) - unlock Cruel Labyrinth
            new() { Name = "Trial of Ascendancy", Act = 6, Reward = QuestReward.AscendancyTrial, RewardDescription = "Cruel Lab", Location = "The Prison (Act 6)", TrapType = "Spike Traps", RecommendedLevel = 44, IsOptional = true },
            new() { Name = "Trial of Ascendancy", Act = 7, Reward = QuestReward.AscendancyTrial, RewardDescription = "Cruel Lab", Location = "The Crypt (Act 7)", TrapType = "Spinning Blades", RecommendedLevel = 48, IsOptional = true },
            new() { Name = "Trial of Ascendancy", Act = 7, Reward = QuestReward.AscendancyTrial, RewardDescription = "Cruel Lab", Location = "The Chamber of Sins Level 2 (Act 7)", TrapType = "Sawblades", RecommendedLevel = 52, IsOptional = true },

            // Merciless Lab Trials (Act 8-10) - unlock Merciless Labyrinth
            new() { Name = "Trial of Ascendancy", Act = 8, Reward = QuestReward.AscendancyTrial, RewardDescription = "Merciless Lab", Location = "The Bath House", TrapType = "Spike Traps & Blade Sentries", RecommendedLevel = 58, IsOptional = true },
            new() { Name = "Trial of Ascendancy", Act = 9, Reward = QuestReward.AscendancyTrial, RewardDescription = "Merciless Lab", Location = "The Tunnel", TrapType = "Sawblades & Spinning Blades", RecommendedLevel = 61, IsOptional = true },
            new() { Name = "Trial of Ascendancy", Act = 10, Reward = QuestReward.AscendancyTrial, RewardDescription = "Merciless Lab", Location = "The Ossuary", TrapType = "Dart Traps & Furnace Traps", RecommendedLevel = 65, IsOptional = true },

            // === LABYRINTH ===
            new() { Name = "The Labyrinth (Normal)", Act = 3, Reward = QuestReward.Labyrinth, RewardDescription = "+2 Ascendancy Points", Location = "The Aspirants' Plaza", RecommendedLevel = 33, IsOptional = true },
            new() { Name = "The Labyrinth (Cruel)", Act = 7, Reward = QuestReward.Labyrinth, RewardDescription = "+2 Ascendancy Points", Location = "The Aspirants' Plaza", RecommendedLevel = 55, IsOptional = true },
            new() { Name = "The Labyrinth (Merciless)", Act = 10, Reward = QuestReward.Labyrinth, RewardDescription = "+2 Ascendancy Points", Location = "The Aspirants' Plaza", RecommendedLevel = 68, IsOptional = true },
            new() { Name = "The Labyrinth (Eternal)", Act = 10, Reward = QuestReward.Labyrinth, RewardDescription = "+2 Ascendancy Points", Location = "The Aspirants' Plaza (Maps)", RecommendedLevel = 75, IsOptional = true },
        };
    }

    public List<Quest> GetSkillPointQuests() =>
        GetAllQuests().FindAll(q => q.Reward == QuestReward.SkillPoint);

    public List<Quest> GetTrialQuests() =>
        GetAllQuests().FindAll(q => q.Reward == QuestReward.AscendancyTrial);

    public List<Quest> GetLabQuests() =>
        GetAllQuests().FindAll(q => q.Reward == QuestReward.Labyrinth);
}
