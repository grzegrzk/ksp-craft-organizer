using System;
using System.IO;

namespace KspCraftOrganizer {
	

	public class FileLocationService {

		private static FileLocationService _instance;

		public static FileLocationService instance {
			get {
				if (_instance == null) {
					_instance = new FileLocationService();
				}
				return _instance;

			}
		}

		private IKspAl ksp = IKspAlProvider.instance;

		public string getCraftSaveFilePathForCurrentShip() {
			return getCraftSaveFilePathForShipName(ksp.getCurrentCraftName());
		}

		public string getCraftSaveFilePathForShipName(string shipName) {
			return ksp.getSavePathForCraftName(shipName);
			//return Path.Combine(getFileSaveDirectory(), shipName) + ".craft";
		}

		public string getCraftFileFilter() {
			return "*.craft";
		}

		public string[] getAllCraftFilesInDirectory(string directory) {
			return Directory.GetFiles(directory, getCraftFileFilter());
		}

		public string getCraftSaveDirectory() {
			return Path.Combine(ksp.getBaseCraftDirectory(), ksp.getCurrentCraftType().directoryName);
		}

		public string getCraftSettingsFileForCraftFile(string craftFile) {
			string saveFolder;
			if (craftFile.StartsWith(getStockCraftDirectoryForCraftType(CraftType.SPH))) {
				saveFolder = Globals.combinePaths(ksp.getApplicationRootPath(), "saves", ksp.getNameOfSaveFolder(), "stock_ships_settings", CraftType.SPH.directoryName);
			}
			else if (craftFile.StartsWith(getStockCraftDirectoryForCraftType(CraftType.VAB))) {
				saveFolder = Globals.combinePaths(ksp.getApplicationRootPath(), "saves", ksp.getNameOfSaveFolder(), "stock_ships_settings", CraftType.VAB.directoryName);
			} else {
				saveFolder = Path.GetDirectoryName(craftFile);
			}	
			return Path.Combine(saveFolder, Path.GetFileNameWithoutExtension(craftFile)) + ".crmgr";
		}

		public string getCraftDirectoryForCraftType(CraftType type) {
			return Path.Combine(ksp.getBaseCraftDirectory(), type.directoryName);
		}


		public string getStockCraftDirectoryForCraftType(CraftType type) {
			return Path.Combine(ksp.getStockCraftDirectory(), type.directoryName);
		}

		public string getProfileSettingsFile() { 
			return Path.Combine(ksp.getBaseCraftDirectory(), "profile_settings.pcrmgr");  
		}

		public string renameCraft(string oldFile, string newName) {
			
			string newFile = Path.Combine(Path.GetDirectoryName(oldFile), newName + ".craft");

			File.Move(oldFile, newFile);
			ksp.renameCraftInsideFile(newFile, newName);

			string oldSettingsFile = getCraftSettingsFileForCraftFile(oldFile);
			if (File.Exists(oldSettingsFile)) {
				string newSettingsFile = getCraftSettingsFileForCraftFile(newFile);
				File.Move(oldSettingsFile, newSettingsFile);
				CraftSettingsDto craftSettings = ksp.readCraftSettings(newSettingsFile);
				craftSettings.craftName = newName;
				ksp.writeCraftSettings(newSettingsFile, craftSettings);
			}

			string oldThumbPath = getThumbPath(oldFile);
			if (File.Exists(oldThumbPath)) {
				string newThumbPath = getThumbPath(newFile);
				File.Move(oldThumbPath, newThumbPath);
			}

			return newFile;
		}

		internal void deleteCraft(string craftFile) {

			File.Delete(craftFile);

			string settingsFile = getCraftSettingsFileForCraftFile(craftFile);
			if (File.Exists(settingsFile)) {
				File.Delete(settingsFile);
			}

			string thumbPath = getThumbPath(craftFile);
			if (File.Exists(thumbPath)) {
				File.Delete(thumbPath);
			}
		}


		public string getThumbPath(string filePath) {
			string thumbUrl = getThumbUrl(filePath);
			if (thumbUrl == "") {
				return "";
			} else {
				return Path.Combine(ksp.getApplicationRootPath(), thumbUrl.Substring(1)) + ".png";
			}

		}

		public string getThumbUrl(string filePath) {
			if (filePath.StartsWith(getStockCraftDirectoryForCraftType(CraftType.SPH))) {
				return "/Ships/@thumbs/SPH/" + Path.GetFileNameWithoutExtension(filePath);
			}
			if (filePath.StartsWith(getStockCraftDirectoryForCraftType(CraftType.VAB))) {
				return "/Ships/@thumbs/VAB/" + Path.GetFileNameWithoutExtension(filePath);
			}
			if (filePath.StartsWith(getCraftDirectoryForCraftType(CraftType.SPH))) {
				return "/thumbs/" + ksp.getNameOfSaveFolder() + "_SPH_" + Path.GetFileNameWithoutExtension(filePath);
			}
			if (filePath.StartsWith(getCraftDirectoryForCraftType(CraftType.VAB))) {
				return "/thumbs/" + ksp.getNameOfSaveFolder() + "_VAB_" + Path.GetFileNameWithoutExtension(filePath);
			}
			return "";

		}

		public string getAutoSaveShipPath() {
			return getCraftSaveFilePathForShipName(ksp.getAutoSaveCraftName());
		}

		public string getThisPluginDirectory() {
			return Globals.combinePaths(ksp.getApplicationRootPath(), "GameData", "KspCraftOrganizerPlugin");
		}

		public string getPluginSettingsPath() {
			return Path.Combine(getThisPluginDirectory(), "settings.conf");
		}
	}
}

