using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace KspCraftOrganizer
{
	public class SettingsService
	{
		private static readonly float PLUGIN_READ_TIME_THRESHOLD = 30;
		private float lastPluginSettingsReadingTime;
		private IKspAl ksp = IKspAlProvider.instance;
		private FileLocationService fileLocationService = FileLocationService.instance;
		private PluginSettings cachedPluginSettings;

		public static readonly SettingsService instance = new SettingsService();

		public ProfileSettingsDto readProfileSettings(){
			return ksp.readProfileSettings(fileLocationService.getProfileSettingsFile(), getPluginSettings().defaultAvailableTags);
		}

		public void writeProfileSettings(ProfileSettingsDto dto)
		{
			ksp.writeProfileSettings(fileLocationService.getProfileSettingsFile(), dto);
		}

		public void writeCraftSettingsForCraftFile(string craftFilePath, CraftSettingsDto dto) {
			ksp.writeCraftSettings(fileLocationService.getCraftSettingsFileForCraftFile(craftFilePath), dto);
		}

		public CraftSettingsDto readCraftSettingsForCraftFile(string craftFilePath) {
			return ksp.readCraftSettings(fileLocationService.getCraftSettingsFileForCraftFile(craftFilePath));
		}

		public CraftSettingsDto readCraftSettingsForCurrentCraft() {
			return ksp.readCraftSettings(fileLocationService.getCraftSettingsFileForCraftFile(fileLocationService.getCraftSaveFilePathForCurrentShip()));
		}

		public void addAvailableTag(string newTag) {
			ProfileSettingsDto profileSettings = readProfileSettings();
			SortedList<string, string> tags = new SortedList<string, string>();
			foreach (string t in profileSettings.availableTags) {
				if (!tags.ContainsKey(t)) {
					tags.Add(t, t);
				}
			}
			if (!tags.ContainsKey(newTag)) {
				tags.Add(newTag, newTag);
				profileSettings.availableTags = tags.Keys;
				writeProfileSettings(profileSettings);
			}
		}

		public PluginSettings getPluginSettings() {
			if (cachedPluginSettings == null || (Time.realtimeSinceStartup - lastPluginSettingsReadingTime) > PLUGIN_READ_TIME_THRESHOLD) {
				cachedPluginSettings = ksp.getPluginSettings(fileLocationService.getPluginSettingsPath());
				lastPluginSettingsReadingTime = Time.realtimeSinceStartup;
			}
			return cachedPluginSettings;
		}
	}
}

