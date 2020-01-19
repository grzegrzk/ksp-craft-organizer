using Microsoft.VisualStudio.TestTools.UnitTesting;
using KspCraftOrganizer;
using KspNalCommon;
using System.Collections.Generic;
using System.IO;

namespace KspCraftOrganizerPluginTests
{
    [TestClass]
    public class FileLocationServiceTest
    {
        private FileLocationService sud;
        private readonly KspAlMockImpl KspAlMockImpl = new KspAlMockImpl();

        public FileLocationServiceTest()
        {
            PluginLogger.instance = new PluginLoggerMock();
            IKspAlProvider.instance = KspAlMockImpl;
            sud = new FileLocationService();
        }

        [TestMethod]
        public void GetCraftSaveFilePathForCurrentShip_VabStandard()
        {
            //given:
            ConfigureForVabStandard();

            //expect:
            Assert.AreEqual(KspAlMockImpl.CRAFT_SAVE_FILE_VAB_STANDARD, sud.getCraftSaveFilePathForCurrentShip());
        }

        [TestMethod]        
        public void GetCraftSaveFilePathForCurrentShip_SphStandard()
        {
            //given:
            ConfigureForSphStandard();

            //expect:
            Assert.AreEqual(KspAlMockImpl.CRAFT_SAVE_FILE_SPH_STANDARD, sud.getCraftSaveFilePathForCurrentShip());
        }

        [TestMethod]
        public void GetCraftSaveFilePathForCurrentShip_VabMission()
        {
            //given:
            ConfigureForVabMission();

            //when:
            string toTest = sud.getCraftSaveFilePathForCurrentShip();

            //then:
            Assert.AreEqual(KspAlMockImpl.CRAFT_SAVE_FILE_VAB_MISSION, toTest);
        }


        [TestMethod]
        public void GetCraftSaveFilePathForCurrentShip_SphMission()
        {
            //given:
            ConfigureForSphMission();

            //when:
            string toTest = sud.getCraftSaveFilePathForCurrentShip();

            //then:
            Assert.AreEqual(KspAlMockImpl.CRAFT_SAVE_FILE_SPH_MISSION, toTest);
        }

        [TestMethod]
        public void GetCraftSaveDirectory_VabStandard()
        {
            //given:
            ConfigureForVabStandard();

            //when:
            string toTest = sud.getCraftSaveDirectory();

            //then:
            Assert.AreEqual(KspAlMockImpl.CRAFT_SAVE_DIRECTORY_VAB_STANDARD, toTest);
        }

        [TestMethod]
        public void GetCraftSaveDirectory_SphStandard()
        {
            //given:
            ConfigureForSphStandard();

            //when:
            string toTest = sud.getCraftSaveDirectory();

            //then:
            Assert.AreEqual(KspAlMockImpl.CRAFT_SAVE_DIRECTORY_SPH_STANDARD, toTest);
        }


        [TestMethod]
        public void GetCraftSaveDirectory_VabMission()
        {
            //given:
            ConfigureForVabMission();

            //when:
            string toTest = sud.getCraftSaveDirectory();

            //then:
            Assert.AreEqual(KspAlMockImpl.CRAFT_SAVE_DIRECTORY_VAB_MISSION, toTest);
        }

        [TestMethod]
        public void GetCraftSaveDirectory_SphMission()
        {
            //given:
            ConfigureForSphMission();

            //when:
            string toTest = sud.getCraftSaveDirectory();

            //then:
            Assert.AreEqual(KspAlMockImpl.CRAFT_SAVE_DIRECTORY_SPH_MISSION, toTest);
        }

        [TestMethod]
        public void GetCraftSettingsFileForCraftFile_VabStandard()
        {
            //given any settings:
            ConfigureForSphMission();

            //when:
            string toTest = sud.getCraftSettingsFileForCraftFile(KspAlMockImpl.CRAFT_SAVE_FILE_VAB_STANDARD);

            //then:
            Assert.AreEqual(Globals.combinePaths(KspAlMockImpl.CRAFT_SAVE_DIRECTORY_VAB_STANDARD,  "some-craft.crmgr"), toTest);
        }


        [TestMethod]
        public void GetCraftSettingsFileForCraftFile_SphStandard()
        {
            //given any settings:
            ConfigureForVabStandard();

            //when:
            string toTest = sud.getCraftSettingsFileForCraftFile(KspAlMockImpl.CRAFT_SAVE_FILE_SPH_STANDARD);

            //then:
            Assert.AreEqual(Globals.combinePaths(KspAlMockImpl.CRAFT_SAVE_DIRECTORY_SPH_STANDARD, "some-craft.crmgr"), toTest);
        }


        [TestMethod]
        public void GetCraftSettingsFileForCraftFile_VabMission()
        {
            //given any settings:
            ConfigureForVabStandard();

            //when:
            string toTest = sud.getCraftSettingsFileForCraftFile(KspAlMockImpl.CRAFT_SAVE_FILE_VAB_MISSION);

            //then:
            Assert.AreEqual(Globals.combinePaths(KspAlMockImpl.CRAFT_SAVE_DIRECTORY_VAB_MISSION, "some-craft.crmgr"), toTest);
        }


        [TestMethod]
        public void GetCraftSettingsFileForCraftFile_SphMission()
        {
            //given any settings:
            ConfigureForVabStandard();

            //when:
            string toTest = sud.getCraftSettingsFileForCraftFile(KspAlMockImpl.CRAFT_SAVE_FILE_SPH_MISSION);

            //then:
            Assert.AreEqual(Globals.combinePaths(KspAlMockImpl.CRAFT_SAVE_DIRECTORY_SPH_MISSION, "some-craft.crmgr"), toTest);
        }

        [TestMethod]
        public void GetCraftSettingsFileForCraftFile_VabStock()
        {
            //given any settings:
            ConfigureForVabStandard();

            //when:
            string toTest = sud.getCraftSettingsFileForCraftFile(KspAlMockImpl.CRAFT_SAVE_FILE_VAB_STOCK);

            //then:
            Assert.AreEqual(Globals.combinePaths(KspAlMockImpl.STOCK_SHIP_SETTINGS_VAB_DIRECTORY, "some-craft.crmgr"), toTest);
        }


        [TestMethod]
        public void GetCraftSettingsFileForCraftFile_SphStock()
        {
            //given any settings:
            ConfigureForVabStandard();

            //when:
            string toTest = sud.getCraftSettingsFileForCraftFile(KspAlMockImpl.CRAFT_SAVE_FILE_SPH_STOCK);

            //then:
            Assert.AreEqual(Globals.combinePaths(KspAlMockImpl.STOCK_SHIP_SETTINGS_SPH_DIRECTORY, "some-craft.crmgr"), toTest);
        }

        [TestMethod]
        public void GetAvailableSaveNames()
        {
            //given any settings:
            ConfigureForVabStandard();

            //when:
            List<string> toTest = new List<string>(sud.getAvailableSaveNames());
            toTest.Sort();

            //then:            
            Assert.AreEqual("missions/Dawn of the Space Age", toTest[0]);
            Assert.AreEqual("SaveName", toTest[1]);
            Assert.AreEqual("SaveName2", toTest[2]);
            Assert.AreEqual("test_missions/New Mission", toTest[3]);
            Assert.AreEqual(4, toTest.Count);
        }


        private void ConfigureForVabStandard()
        {
            KspAlMockImpl.BaseCraftDirectory = KspAlMockImpl.BASE_CRAFT_DIRECTORY_STANDARD;
            KspAlMockImpl.CurrentEditorFacilityType = CraftType.VAB;
            KspAlMockImpl.StaticSavePathForCraftName = KspAlMockImpl.CRAFT_SAVE_FILE_VAB_STANDARD;
            KspAlMockImpl.StockCraftDirectory = KspAlMockImpl.STOCK_CRAFT_DIRECTORY;
            KspAlMockImpl.ApplicationRootPath = KspAlMockImpl.APPLICATION_ROOT_PATH;
            KspAlMockImpl.NameOfSaveFolder = KspAlMockImpl.NAME_OF_SAVE_FOLDER;
        }

        private void ConfigureForSphStandard()
        {
            KspAlMockImpl.BaseCraftDirectory = KspAlMockImpl.BASE_CRAFT_DIRECTORY_STANDARD;
            KspAlMockImpl.CurrentEditorFacilityType = CraftType.SPH;
            KspAlMockImpl.StaticSavePathForCraftName = KspAlMockImpl.CRAFT_SAVE_FILE_SPH_STANDARD;
            KspAlMockImpl.StockCraftDirectory = KspAlMockImpl.STOCK_CRAFT_DIRECTORY;
            KspAlMockImpl.ApplicationRootPath = KspAlMockImpl.APPLICATION_ROOT_PATH;
            KspAlMockImpl.NameOfSaveFolder = KspAlMockImpl.NAME_OF_SAVE_FOLDER;
        }

        private void ConfigureForVabMission()
        {
            KspAlMockImpl.BaseCraftDirectory = KspAlMockImpl.BASE_CRAFT_DIRECTORY_MISSION;
            KspAlMockImpl.CurrentEditorFacilityType = CraftType.VAB;
            KspAlMockImpl.StaticSavePathForCraftName = KspAlMockImpl.CRAFT_SAVE_FILE_VAB_MISSION;
            KspAlMockImpl.StockCraftDirectory = KspAlMockImpl.STOCK_CRAFT_DIRECTORY;
            KspAlMockImpl.ApplicationRootPath = KspAlMockImpl.APPLICATION_ROOT_PATH;
            KspAlMockImpl.NameOfSaveFolder = KspAlMockImpl.NAME_OF_SAVE_FOLDER;
        }

        private void ConfigureForSphMission()
        {
            KspAlMockImpl.BaseCraftDirectory = KspAlMockImpl.BASE_CRAFT_DIRECTORY_MISSION;
            KspAlMockImpl.CurrentEditorFacilityType = CraftType.SPH;
            KspAlMockImpl.StaticSavePathForCraftName = KspAlMockImpl.CRAFT_SAVE_FILE_SPH_MISSION;
            KspAlMockImpl.StockCraftDirectory = KspAlMockImpl.STOCK_CRAFT_DIRECTORY;
            KspAlMockImpl.ApplicationRootPath = KspAlMockImpl.APPLICATION_ROOT_PATH;
            KspAlMockImpl.NameOfSaveFolder = KspAlMockImpl.NAME_OF_SAVE_FOLDER;
        }
    }
}
