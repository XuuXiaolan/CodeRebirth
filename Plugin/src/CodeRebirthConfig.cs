
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;

namespace RatchetnClank.Configs {
    public class RatchetnClankConfig {
        public ConfigEntry<int> ConfigHammerCost { get; private set; }
        public ConfigEntry<bool> ConfigHammerEnabled { get; private set; }
        public ConfigEntry<string> ConfigHammerRarity { get; private set; }
        public ConfigEntry<bool> ConfigHammerScrapEnabled { get; private set; }   
        public RatchetnClankConfig(ConfigFile configFile) {
            ConfigHammerScrapEnabled = configFile.Bind("Scrap Options",
                                                "Hammer Scrap | Enabled",
                                                true,
                                                "Enables/Disables the spawning of the scrap (sets rarity to 0 if false on all moons)");
            ConfigHammerRarity = configFile.Bind("Scrap Options",   
                                                "Hammer Scrap | Rarity",  
                                                "Modded@1,ExperimentationLevel@1,AssuranceLevel@1,VowLevel@1,OffenseLevel@1,MarchLevel@1,RendLevel@1,DineLevel@1,TitanLevel@1", 
                                                "Rarity of Hammer scrap appearing on every moon");
            ConfigHammerEnabled = configFile.Bind("Shop Options",   
                                                "Hammer Item | Enabled",  
                                                true, 
                                                "Enables/Disables the Hammer showing up in shop");
            ConfigHammerCost = configFile.Bind("Shop Options",   
                                                "Hammer Item | Cost",
                                                60, 
                                                "Cost of Hammer");     
            ClearUnusedEntries(configFile);
            Plugin.Logger.LogInfo("Setting up config for RatchetnClank plugin...");
        }
        private void ClearUnusedEntries(ConfigFile configFile) {
            // Normally, old unused config entries don't get removed, so we do it with this piece of code. Credit to Kittenji.
            PropertyInfo orphanedEntriesProp = configFile.GetType().GetProperty("OrphanedEntries", BindingFlags.NonPublic | BindingFlags.Instance);
            var orphanedEntries = (Dictionary<ConfigDefinition, string>)orphanedEntriesProp.GetValue(configFile, null);
            orphanedEntries.Clear(); // Clear orphaned entries (Unbinded/Abandoned entries)
            configFile.Save(); // Save the config file to save these changes
        }
    }
}