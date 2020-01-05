﻿using System;
using System.Collections.Generic;
using System.IO;
using KspNalCommon;

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
			return getCraftFilePathFromPathAndCraftName(getCraftSaveDirectory(), shipName);
		}

		private string getCraftFilePathFromPathAndCraftName(string path, string shipName) {
			//we do not use shipName directly because it may contain special characters. We use translated file name
			// - the translation is done by KSP itself.
			String kraftPathProvidedByKsp = ksp.getSavePathForCraftName(shipName);
			String fileName = Path.GetFileName(kraftPathProvidedByKsp);
			PluginLogger.logDebug("getCraftFilePathFromPathAndCraftName. Craft name: " + shipName + ", file path provided by KSP: "+ kraftPathProvidedByKsp + ", final file name: " + fileName);
			String finalPath = Path.Combine(path, fileName);
			PluginLogger.logDebug("getCraftFilePathFromPathAndCraftName. Final path: " + finalPath);
			return finalPath;
		}


		public string getCraftFileFilter() {
			return "*.craft";
		}

		public string[] getAllCraftFilesInDirectory(string directory) {
			return Directory.GetFiles(directory, getCraftFileFilter());
		}

		public string getCraftSaveDirectory() {
			return Path.Combine(ksp.getBaseCraftDirectory(), ksp.getCurrentEditorFacilityType().directoryName);
		}

		public string getCraftSettingsFileForCraftFile(string craftFile) {
			string saveFolder;
			if (isPathInside(craftFile, getStockCraftDirectoryForCraftType(CraftType.SPH))) {
				saveFolder = Globals.combinePaths(ksp.getApplicationRootPath(), "saves", ksp.getNameOfSaveFolder(), "stock_ships_settings", CraftType.SPH.directoryName);
			}
			else if (isPathInside(craftFile, getStockCraftDirectoryForCraftType(CraftType.VAB))) {
				saveFolder = Globals.combinePaths(ksp.getApplicationRootPath(), "saves", ksp.getNameOfSaveFolder(), "stock_ships_settings", CraftType.VAB.directoryName);
			} else {
				saveFolder = Path.GetDirectoryName(craftFile);
			}	
			return Path.Combine(saveFolder, Path.GetFileNameWithoutExtension(craftFile)) + ".crmgr";
		}

		public ICollection<string> getAvailableSaveNames() {
			string savesFolder = Globals.combinePaths(ksp.getApplicationRootPath(), "saves");
			string[] directories = Directory.GetDirectories(savesFolder);
			List<string> toRet = new List<string>();
			foreach (string dir in directories) {
				if (Directory.Exists(Globals.combinePaths(dir, "Ships"))) {
					toRet.Add(Path.GetFileName(dir));
				}
			}
			return toRet;
		}


		public string getCraftDirectoryForCraftType(string saveName, CraftType type) {
			return Globals.combinePaths(ksp.getApplicationRootPath(), "saves", saveName, "Ships", type.directoryName);
		}


		public string getStockCraftDirectoryForCraftType(CraftType type) {
			return Path.Combine(ksp.getStockCraftDirectory(), type.directoryName);
		}

		public string getProfileSettingsFile(string saveName) { 
			return Globals.combinePaths(ksp.getApplicationRootPath(), "saves", saveName, "profile_settings.pcrmgr");  
		}

		public string renameCraft(string oldFile, string newName) {
			PluginLogger.logDebug("renameCraft");

			string newFile =  getCraftFilePathFromPathAndCraftName(Path.GetDirectoryName(oldFile), newName);

			File.Move(oldFile, newFile);
			ksp.renameCraftInsideFile(newFile, newName);

			string oldSettingsFile = getCraftSettingsFileForCraftFile(oldFile);
			if (File.Exists(oldSettingsFile))
			{
				string newSettingsFile = getCraftSettingsFileForCraftFile(newFile);
				PluginLogger.logDebug("renameCraft. Old settings file exists, renaming it. Old name: " + oldSettingsFile + ", new name: " + newSettingsFile);
				File.Move(oldSettingsFile, newSettingsFile);
				CraftSettingsDto craftSettings = ksp.readCraftSettings(newSettingsFile);
				craftSettings.craftName = newName;
				ksp.writeCraftSettings(newSettingsFile, craftSettings);
			}

			string oldThumbPath = getThumbPath(oldFile);
			if (File.Exists(oldThumbPath)) {
				string newThumbPath = getThumbPath(newFile);
				PluginLogger.logDebug("renameCraft. Old thumb file exists, renaming it. Old name: " + oldThumbPath + ", new name: " + newThumbPath);
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

		public string getThumbUrl(string craftPath) {
			PluginLogger.logDebug(String.Format("getThumbUrl: craftPath {0}", craftPath));
			if (isPathInside(craftPath, getStockCraftDirectoryForCraftType(CraftType.SPH))) {
				return "/Ships/@thumbs/SPH/" + Path.GetFileNameWithoutExtension(craftPath);
			}
			if (isPathInside(craftPath, getStockCraftDirectoryForCraftType(CraftType.VAB))) {
				return "/Ships/@thumbs/VAB/" + Path.GetFileNameWithoutExtension(craftPath);
			}
			string saveName = extractSaveNameFromCraftPath(craftPath);
			if (isPathInside(craftPath, getCraftDirectoryForCraftType(saveName, CraftType.SPH))) {
				return "/thumbs/" + saveName + "_SPH_" + Path.GetFileNameWithoutExtension(craftPath);
			}
			if (isPathInside(craftPath, getCraftDirectoryForCraftType(saveName, CraftType.VAB))) {
				return "/thumbs/" + saveName + "_VAB_" + Path.GetFileNameWithoutExtension(craftPath);
			}
			return "";

		}

		public bool isPathInside(String path, String pathSupposelyInside)
		{
			return Path.GetFullPath(path).StartsWith(Path.GetFullPath(pathSupposelyInside));
		}

		string extractSaveNameFromCraftPath(string craftPath) {
			string[] pathElements = 
				craftPath.Split(new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
			if (pathElements.Length >= 4) {
				return pathElements[pathElements.Length - 4];
			} else {
				return "";
			}
		}

		public string getAutoSaveShipPath() {
			return getCraftSaveFilePathForShipName(ksp.getAutoSaveCraftName());
		}

		public string getThisPluginDirectory() {
			return Globals.combinePaths(ksp.getApplicationRootPath(), "GameData", "KspCraftOrganizer");
		}

		public string getPluginSettingsPath() {
			return Path.Combine(getThisPluginDirectory(), "settings.conf");
		}
	}
}

