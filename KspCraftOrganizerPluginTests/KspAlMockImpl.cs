using KspCraftOrganizer;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace KspCraftOrganizerPluginTests
{
    class KspAlMockImpl : IKspAl
    {

        public static string APPLICATION_ROOT_PATH = @"test-resources\ksp1";
        public static string NAME_OF_SAVE_FOLDER = @"SaveName";
        public static string STOCK_SHIP_SETTINGS_VAB_DIRECTORY = @"test-resources\ksp1\saves\SaveName\stock_ships_settings\VAB";
        public static string STOCK_SHIP_SETTINGS_SPH_DIRECTORY = @"test-resources\ksp1\saves\SaveName\stock_ships_settings\SPH";

        public static string BASE_CRAFT_DIRECTORY_STANDARD = @"test-resources\ksp1\saves\SaveName\Ships";
        public static string BASE_CRAFT_DIRECTORY_MISSION = @"test-resources\ksp1\saves\test_missions\New Mission\Ships";

        public static string CRAFT_SAVE_FILE_SPH_STANDARD = @"test-resources\ksp1\saves\SaveName\Ships\SPH\some-craft.craft";
        public static string CRAFT_SAVE_FILE_VAB_STANDARD = @"test-resources\ksp1\saves\SaveName\Ships\VAB\some-craft.craft";
        public static string CRAFT_SAVE_FILE_SPH_MISSION = @"test-resources\ksp1\saves\test_missions\New Mission\Ships\SPH\some-craft.craft";
        public static string CRAFT_SAVE_FILE_VAB_MISSION = @"test-resources\ksp1\saves\test_missions\New Mission\Ships\VAB\some-craft.craft";

        public static string CRAFT_SAVE_DIRECTORY_SPH_STANDARD = @"test-resources\ksp1\saves\SaveName\Ships\SPH";
        public static string CRAFT_SAVE_DIRECTORY_VAB_STANDARD = @"test-resources\ksp1\saves\SaveName\Ships\VAB";
        public static string CRAFT_SAVE_DIRECTORY_SPH_MISSION = @"test-resources\ksp1\saves\test_missions\New Mission\Ships\SPH";
        public static string CRAFT_SAVE_DIRECTORY_VAB_MISSION = @"test-resources\ksp1\saves\test_missions\New Mission\Ships\VAB";

        public static string STOCK_CRAFT_DIRECTORY = @"test-resources\ksp1\Ships";
        public static string CRAFT_SAVE_FILE_SPH_STOCK = @"test-resources\ksp1\Ships\SPH\some-craft.craft";
        public static string CRAFT_SAVE_FILE_VAB_STOCK = @"test-resources\ksp1\Ships\VAB\some-craft.craft";

        public void destroy()
        {
            throw new NotImplementedException();
        }

        public GUISkin editorSkin()
        {
            throw new NotImplementedException();
        }

        public string ApplicationRootPath {get; set; }

        public string getAutoSaveCraftName()
        {
            throw new NotImplementedException();
        }

        public double getAvailableFunds()
        {
            throw new NotImplementedException();
        }

        public string BaseCraftDirectory { get; set; }

        public CraftDaoDto getCraftInfo(CraftDataCacheContext cacheContext, string craftFile, string settingsFile)
        {
            throw new NotImplementedException();
        }

        public string getCurrentCraftName()
        {
            return "!!Some craft name with strange characters @^/\\";
        }

        public CraftType CurrentEditorFacilityType { get; set; }
        public string StaticSavePathForCraftName { get; set; }

        public string NameOfSaveFolder { get; set; }

        public PluginSettings getPluginSettings(string fileName)
        {
            throw new NotImplementedException();
        }

        public string GetSavePathForCraftName(string shipName)
        {
            return StaticSavePathForCraftName;
        }

        public string StockCraftDirectory { get; set; }

        public Texture2D getThumbnail(string url)
        {
            throw new NotImplementedException();
        }

        public bool isCraftAlreadyLoadedInWorkspace()
        {
            throw new NotImplementedException();
        }

        public bool isShowStockCrafts()
        {
            throw new NotImplementedException();
        }

        public GUISkin kspSkin()
        {
            throw new NotImplementedException();
        }

        public void loadCraftToWorkspace(string file)
        {
            throw new NotImplementedException();
        }

        public void lockEditor()
        {
            throw new NotImplementedException();
        }

        public void mergeCraftToWorkspace(string file)
        {
            throw new NotImplementedException();
        }

        public void onGUI(GUISkin originalSkin)
        {
            throw new NotImplementedException();
        }

        public CraftSettingsDto readCraftSettings(string fileName)
        {
            throw new NotImplementedException();
        }

        public ProfileSettingsDto readProfileSettings(string fileName, ICollection<string> defaultTags)
        {
            throw new NotImplementedException();
        }

        public void renameCraftInsideFile(string fileName, string newName)
        {
            throw new NotImplementedException();
        }

        public void saveCurrentCraft()
        {
            throw new NotImplementedException();
        }

        public void start()
        {
            throw new NotImplementedException();
        }

        public void unlockEditor()
        {
            throw new NotImplementedException();
        }

        public void writeCraftSettings(string fileName, CraftSettingsDto settings)
        {
            throw new NotImplementedException();
        }

        public void writeProfileSettings(string fileName, ProfileSettingsDto toWrite)
        {
            throw new NotImplementedException();
        }
    }
}
