﻿using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Security.Cryptography;
using KspNalCommon;
using UniLinq;

namespace KspCraftOrganizer
{
	public class KspAlImpl: IKspAl {
		
		private static readonly int SETTINGS_VER_1 = 1;
		/**
		 * In profile settings: introduced separated settings for VABinSPH, SPHinSPH, etc
		 * In plugin settings: no changes
		 */
		private static readonly int SETTINGS_VER_2 = 2;
		/**
		 * In profile settings: Introduced new default tags
		 * In plugin settings: Introduced new default tags
		 */
		private static readonly int SETTINGS_VER_3 = 3;
		/**
		 * In profile settings: no changes
		 * In plugin settings: Eliminated bug that default tags may not be created
		 */
		private static readonly int SETTINGS_VER_4 = 4;
		/**
		 * In profile settings: aded sorting settings
		 * In plugin settings: no changes
		 */
		private static readonly int SETTINGS_VER_5 = 5;

		private List<string> NEW_TAGS_IN_VER3 = new List<string>(new string[] { "MasterpieceAmongCrafts"});

		private static readonly int SETTINGS_VER_NOW = SETTINGS_VER_5;

		private Dictionary<string, AvailablePart> _availablePartCache;
		private EditorFacility editorFacility;
		private bool onGuiInitialized = false;

		public KspAlImpl() {
			onEditorStarted();
		}

		public void start() {
			onEditorStarted();
			GameEvents.onEditorStarted.Add(onEditorStarted);
		}


		public void destroy() {
			GameEvents.onEditorStarted.Remove(onEditorStarted);
			unlockEditor();
		}

		public void onEditorStarted() {
			//
			//If "Go To" mod is used and user goes from VAB->SPH then there is a bug that during "OnDestroy" events etc new facility is returned instead of old.
			//This code corrects this by remembering facility at the beginning and returning it until it is truly changed.
			//
			this.editorFacility = EditorDriver.editorFacility;
			PluginLogger.logDebug("Setting editor facility to " + this.editorFacility);
			createKspSkin();
		}

		private GUISkin _kspSkin;

		private void createKspSkin() {
			this._kspSkin = MonoBehaviour.Instantiate(HighLogic.Skin);
			//
			//It will eliminate ugly rounded buttons:
			//
			_kspSkin.button.fixedHeight = 0;
			_kspSkin.button.padding.top = 6;
			_kspSkin.button.border.top = _kspSkin.button.border.bottom = 0;

			//
			//It will add default left/right padding:
			//
			_kspSkin.button.padding.left = _kspSkin.button.padding.right = 12;
			onGuiInitialized = false;

		}

		public void onGUI(GUISkin defaultGuiSkin)
		{
			if (!onGuiInitialized && _kspSkin != null)
			{
				if (GameSettings.UI_SCALE != 1.0f)
				{
					//
					//If UI Scale is not 1.0 then it seems font from ksp skin causes troubles with GUILayout.FlexibleSpace.
					//Lets change it to font from default unity gui style which unfortunately is not so crispy but
					//at least does not cause bugs
					//
					PluginLogger.logDebug("Changing font for GUI, UI scale: " + GameSettings.UI_SCALE + ". Old font: " + _kspSkin.font.name + ", new font: " + defaultGuiSkin.font.name);
					_kspSkin.font = defaultGuiSkin.font;
					_kspSkin.label = new GUIStyle(_kspSkin.label);
					_kspSkin.label.font = defaultGuiSkin.font;
				}
				onGuiInitialized = true;
			}	
		}

		public string getBaseCraftDirectory(){
			return Globals.combinePaths(getApplicationRootPath(), "saves", getNameOfSaveFolder(), "Ships");
		}


		public string getApplicationRootPath() {
			return KSPUtil.ApplicationRootPath;
		}

		public string getNameOfSaveFolder()
		{
			return Globals.normalizePath(HighLogic.SaveFolder);
		}

		public string getStockCraftDirectory()
		{
			return Path.Combine(KSPUtil.ApplicationRootPath, "Ships");
		}

		public bool isShowStockCrafts()
		{
			return HighLogic.CurrentGame.Parameters.Difficulty.AllowStockVessels;
		}

		public CraftType getCurrentEditorFacilityType(){
			return editorFacility == EditorFacility.SPH ? CraftType.SPH : CraftType.VAB;
		}

		public CraftDaoDto getCraftInfo(CraftDataCacheContext craftDataCacheContext, string craftFile, string settingsFile){
			PluginLogger.logTrace ("reading craft file from '" + craftFile + "'");

			string fileChecksum = CalculateMD5(craftFile);
			PluginLogger.logTrace ("Calculated MD5 checksum: " + fileChecksum);
			CraftDaoDto mayabeCraftDaoFromCache =
				CacheNodeToMaybeCraftDaoDto(craftDataCacheContext, getCacheNodeFromSettings(settingsFile), fileChecksum);
			if (mayabeCraftDaoFromCache != null)
			{
				PluginLogger.logTrace("Craft data has been successfully read from cache");
				return mayabeCraftDaoFromCache;
			}

			System.Random r = new System.Random(craftFile.GetHashCode());
			CraftDaoDto toRet = new CraftDaoDto();
			toRet.name = Path.GetFileNameWithoutExtension(craftFile);
			toRet.cost = r.Next() % 100000000;
			toRet.mass = r.Next() % 100000000;
			toRet.partCount = r.Next() % 10000;
			toRet.stagesCount = r.Next() % 100;


			ConfigNode nodes = ConfigNode.Load(craftFile);
			ConfigNode[] parts = nodes.GetNodes("PART");

			int stagesCount = 0;
			float massSum = 0, costSum = 0;
			bool available = true;
			bool notEnoughScience = false;
			List<string> partNames = new List<string>();
			foreach (ConfigNode part in parts)
			{
				String stageString = part.GetValue("istg");
				if (stageString != null && stageString != "")
				{
					int partStage = int.Parse(stageString);
					if (partStage > stagesCount)
					{
						stagesCount = partStage;
					}
				}
				partNames.Add(getPartName(part));
				AvailablePart availablePart = getAvailablePartFor(part);

				if (availablePart == null)
				{
					available = false;
				}
				else {
					float dryCost;
					float fuelCost;

					float dryMass;
					float fuelMass;
					ShipConstruction.GetPartCostsAndMass(part, availablePart, out dryCost, out fuelCost, out dryMass, out fuelMass);
					//COLogger.Log("For part" + getPartName(part)  + " dry cost: " + dryCost + ", fuelCost: " + fuelCost + ", smth: " + partCostsSmth);
					costSum += dryCost + fuelCost;
					massSum += dryMass + fuelMass;
					if (!PartTechAvailable(craftDataCacheContext, getPartName(part))) {
						notEnoughScience = true;
					}
				}
			}
			partNames = partNames.Distinct().ToList();
			
			toRet.name = guardedGetStringValue(nodes, "ship", craftFile);
			toRet.description = guardedGetStringValue(nodes, "description", craftFile);
			toRet.stagesCount = stagesCount;
			toRet.partCount = parts.Length;
			toRet.mass = massSum;
			toRet.cost = costSum;
			toRet.allPartsAvailable = available;
			toRet.notEnoughScience = notEnoughScience;
			writeCraftSettings(settingsFile, readCraftSettings(settingsFile), craftDaoDtoToCacheNode(toRet, partNames, fileChecksum));

			toRet.name = maybeLocalize(toRet.name);
			toRet.description = maybeLocalize(toRet.description);

			return toRet;
		}

		private string maybeLocalize(string toLocalize)
		{
			if (toLocalize.StartsWith("#autoLOC"))
			{
				return KSP.Localization.Localizer.Format(toLocalize);
			}
			else
			{
				return toLocalize;
			}
		}
		private string CalculateMD5(string filename)
		{
			using (var md5 = MD5.Create())
			{
				using (var stream = File.OpenRead(filename))
				{
					var hash = md5.ComputeHash(stream);
					return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
				}
			}
		}
		
		private ConfigNode craftDaoDtoToCacheNode(CraftDaoDto craftDaoDto, List<string> partNames, string fileChecksum)
		{
			ConfigNode toRet = new ConfigNode();

			toRet.AddValue("origChecksum", fileChecksum);
			toRet.AddValue("cacheVersion", "2");
			toRet.AddValue("name", craftDaoDto.name);
			toRet.AddValue("stagesCount", craftDaoDto.stagesCount);
			toRet.AddValue("cost", craftDaoDto.cost);
			toRet.AddValue("partCount", craftDaoDto.partCount);
			toRet.AddValue("mass", craftDaoDto.mass);
			toRet.AddValue("description", craftDaoDto.description);
			writeAsListOfStrings(toRet, "partNames", partNames);

			return toRet;
		}

		private CraftDaoDto CacheNodeToMaybeCraftDaoDto(CraftDataCacheContext craftDataCacheContext, ConfigNode cacheConfigNode, string expectedChecksum)
		{
			if (cacheConfigNode == null)
			{
				return null;
			}

			string cacheVersion = cacheConfigNode.GetValue("cacheVersion");
			if (cacheVersion != "2")
			{
				return null;
			}
			string actualChecksum = cacheConfigNode.GetValue("origChecksum");
			if (actualChecksum != expectedChecksum)
			{
				return null;
			}

			CraftDaoDto toRet = new CraftDaoDto();
			toRet.name = maybeLocalize(cacheConfigNode.GetValue("name"));
			toRet.stagesCount = int.Parse(cacheConfigNode.GetValue("stagesCount"));
			toRet.cost = float.Parse(cacheConfigNode.GetValue("cost"));
			toRet.partCount = int.Parse(cacheConfigNode.GetValue("partCount"));
			toRet.mass = float.Parse(cacheConfigNode.GetValue("mass"));
			toRet.description = maybeLocalize(cacheConfigNode.GetValue("description"));

			List<string> partNames = readAsListOfStrings(cacheConfigNode, "partNames");

			toRet.allPartsAvailable = true;
			toRet.notEnoughScience = false;
			foreach (string partName in partNames)
			{
				AvailablePart availablePart = getAvailablePartFor(partName);
				if (availablePart == null)
				{
					toRet.allPartsAvailable = false;
				}
				else if (!PartTechAvailable(craftDataCacheContext, partName))
				{
					toRet.notEnoughScience = true;
				}
			}
			return toRet;
		}

		private bool PartTechAvailable(CraftDataCacheContext craftDataCacheContext, string partName)
		{
			if (craftDataCacheContext.PartTechIsAvailable.ContainsKey(partName))
			{
				return craftDataCacheContext.PartTechIsAvailable[partName];
			}

			bool toRet =  ResearchAndDevelopment.PartTechAvailable(getAvailablePartFor(partName));
			craftDataCacheContext.PartTechIsAvailable[partName] = toRet;

			return toRet;
		}
		
		string guardedGetStringValue(ConfigNode nodes, String propertyName, String fileName) {
			String v = nodes.GetValue(propertyName);
			if (v == null) {
				PluginLogger.logDebug("Value of '" + propertyName + "' is null in '" + fileName + "', coercing it to empty string.");
				return "";
			} else {
				return v;
			}
		}

		public string getAutoSaveCraftName() {
			return EditorLogic.autoShipName;
		}

		private AvailablePart getAvailablePartFor(ConfigNode part)
		{
			string partName = getPartName(part);
			return getAvailablePartFor(partName);
		}

		private AvailablePart getAvailablePartFor(string originalPartName)
		{
			//COLogger.Log("Finding part '" + partName + "'");

			string partName;
			if (availablePartCache.ContainsKey(originalPartName))
			{
				partName = originalPartName;
			}
			else
			{
				string replacementName = PartLoader.GetPartReplacementName(originalPartName);
				if(replacementName != null && replacementName.Length > 0)
				{
					partName = replacementName;
				}
				else
				{
					partName = originalPartName;
				}
			}

			if (availablePartCache.ContainsKey(partName))
			{
				return availablePartCache[partName];
			}

			PluginLogger.logTrace("Part '" + partName + "' not found");
			return null;
		}
		
		private string getPartName(ConfigNode part)
		{
			return part.GetValue("part").Split('_')[0];
		}

		private Dictionary<string, AvailablePart> availablePartCache{
			get {
				if (_availablePartCache == null)
				{
					_availablePartCache = new Dictionary<string, AvailablePart>();
					foreach (AvailablePart part in PartLoader.LoadedPartsList)
					{
						if (!_availablePartCache.ContainsKey(part.name))
							_availablePartCache.Add(part.name, part);
					}

				}
				return _availablePartCache;
			}
		}

		public ProfileSettingsDto readProfileSettings(string fileName, ICollection<string> defaultTags){
			PluginLogger.logDebug ("reading profile settings from '" + fileName + "'");
			ProfileSettingsDto settings = new ProfileSettingsDto ();

			List<string> tags = new List<string> ();
			List<CraftSortingEntry> craftSorting = new List<CraftSortingEntry>();
			GuiStyleOption style = GuiStyleOption.Ksp;

			ProfileAllFilterSettingsDto allFilterSettings = new ProfileAllFilterSettingsDto();

			if (File.Exists(fileName)) {
				ConfigNode node = ConfigNode.Load(fileName);
				if (node != null) {
					int settingsVersion = readSettingsVersion(node);

					if (settingsVersion == SETTINGS_VER_1) {
						readFilterSettingsToDto(allFilterSettings.filterSphInSph, node, "");
						readFilterSettingsToDto(allFilterSettings.filterSphInVab, node, "");
						readFilterSettingsToDto(allFilterSettings.filterVabInSph, node, "");
						readFilterSettingsToDto(allFilterSettings.filterVabInVab, node, "");
					} else {
						readFilterSettingsToDto(allFilterSettings.filterSphInSph, node, "SphInSph_");
						readFilterSettingsToDto(allFilterSettings.filterSphInVab, node, "SphInVab_");
						readFilterSettingsToDto(allFilterSettings.filterVabInSph, node, "VabInSph_");
						readFilterSettingsToDto(allFilterSettings.filterVabInVab, node, "VabInVab_");
					}

					foreach (string tag in node.GetValues("availableTag")) {
						tags.Add(tag);
					}

					if (settingsVersion <= SETTINGS_VER_2) {
						PluginLogger.logDebug("Profile settings were in version " + settingsVersion + ", adding new default tags");
						tags.AddRange(NEW_TAGS_IN_VER3);
					}


					if (settingsVersion <= SETTINGS_VER_4) {
						PluginLogger.logDebug("Profile settings were in version " + settingsVersion + ", adding craft sorting");
						craftSorting.Add(createDefaultSorting());
					}


					foreach (ConfigNode craftSortingNode in node.GetNodes("craftSorting")) {

						CraftSortingEntry craftSortingEntry = new CraftSortingEntry();
						craftSortingEntry.sortingId = craftSortingNode.GetValue("sortingId");
						craftSortingEntry.sortingData = craftSortingNode.GetValue("sortingData");
						craftSortingEntry.isReversed = readBoolFromSettings(craftSortingNode, "isReversed", false);

						craftSorting.Add(craftSortingEntry);
					}

					string styleId = node.GetValue("guiStyle");
					foreach (GuiStyleOption candidateStyle in GuiStyleOption.SKIN_STATES) {
						if (candidateStyle.id == styleId) {
							style = candidateStyle;
							break;
						}
					}
				}
			} else {
				craftSorting.Add(createDefaultSorting());

				tags.AddRange(defaultTags);
			}

			settings.craftSorting = craftSorting;
			settings.availableTags = tags.ToArray();
			settings.selectedGuiStyle = style;
			settings.allFilter = allFilterSettings;
			return settings;
		}

		public CraftSortingEntry createDefaultSorting() {
			CraftSortFunction defaultSorting = CraftSortFunction.SORT_CRAFTS_BY_NAME;
			CraftSortingEntry defaultSortingEntry = new CraftSortingEntry();
			defaultSortingEntry.sortingId = defaultSorting.functionTypeId;
			defaultSortingEntry.sortingData = defaultSorting.functionData;
			defaultSortingEntry.isReversed = defaultSorting.isReversed;
			return defaultSortingEntry;
		}

		static int readSettingsVersion(ConfigNode node) {
			int settingsVersion;
			string settingsVersionString = node.GetValue("version");
			if (settingsVersionString == null || settingsVersionString == "") {
				settingsVersion = SETTINGS_VER_1;
			} else {
				settingsVersion = int.Parse(settingsVersionString);
			}

			return settingsVersion;
		}

		private void readFilterSettingsToDto(ProfileFilterSettingsDto dto, ConfigNode node, string optionsPrefix) {

			List<string> selectedTags = readAsListOfStrings(node, optionsPrefix + "filterTag");
			List<string> filterGroupsWithSelectedNoneOption = readAsListOfStrings(node, optionsPrefix + "filterGroupsWithSelectedNoneOption");

			List<string> collapsedFilterGroups = readAsListOfStrings(node, optionsPrefix + "collapsedFilterTagGroups");
			dto.restFilterTagsCollapsed = readBoolFromSettings(node, optionsPrefix + "isRestFilterTagsCollapsed", false);

			List<string> collapsedManagementGroups = readAsListOfStrings(node, optionsPrefix + "collapsedManagementTagGroups");
			dto.restManagementTagsCollapsed = readBoolFromSettings(node, optionsPrefix + "isRestManagementTagsCollapsed", false);

			string filterText = "";

			filterText = node.GetValue(optionsPrefix + "filterText");
			if (filterText == null) {
				filterText = "";
			}

			dto.selectedFilterTags = selectedTags.ToArray();
			dto.selectedTextFilter = filterText;
			dto.filterGroupsWithSelectedNoneOption = filterGroupsWithSelectedNoneOption;
			dto.collapsedFilterGroups = collapsedFilterGroups;
			dto.collapsedManagementGroups = collapsedManagementGroups;
		}

		private bool readBoolFromSettings(ConfigNode node, string name, bool defaultValue) {
			string valAsString = node.GetValue(name);
			if (valAsString == null || valAsString == "") {
				return defaultValue;
			} else {
				return valAsString.ToUpper().Equals("TRUE");
			}
		}

		public List<string> readAsListOfStrings(ConfigNode node, string name) {
			List<string> toRet = new List<string>();
			foreach (string v in node.GetValues(name)) {
				toRet.Add(v);
			}
			return toRet;
		}	

		public void writeProfileSettings(string fileName, ProfileSettingsDto toWrite) {
			PluginLogger.logDebug("Writing profile settings to '" + fileName);
			ConfigNode node = new ConfigNode();
			writeSettingsVersion(node);

			foreach (string availableTag in toWrite.availableTags) {
				node.AddValue("availableTag", availableTag);
			}

			foreach (CraftSortingEntry sortingEntry in toWrite.craftSorting) {
				ConfigNode sortingNode = new ConfigNode();
				sortingNode.AddValue("sortingId", sortingEntry.sortingId);
				sortingNode.AddValue("sortingData", sortingEntry.sortingData);
				sortingNode.AddValue("isReversed", sortingEntry.isReversed);
				node.AddNode("craftSorting", sortingNode);
			}

			writeFilterSettingsFromDto(toWrite.allFilter.filterSphInSph, node, "SphInSph_");
			writeFilterSettingsFromDto(toWrite.allFilter.filterSphInVab, node, "SphInVab_");
			writeFilterSettingsFromDto(toWrite.allFilter.filterVabInSph, node, "VabInSph_");
			writeFilterSettingsFromDto(toWrite.allFilter.filterVabInVab, node, "VabInVab_");

			if (toWrite.selectedGuiStyle != null) {
				node.AddValue("guiStyle", toWrite.selectedGuiStyle.id);
			}
			saveNode(node, fileName);
		}

		static void writeSettingsVersion(ConfigNode node) {
			node.AddValue("version", SETTINGS_VER_NOW);
		}

		private void writeFilterSettingsFromDto(ProfileFilterSettingsDto dto, ConfigNode node, string optionsPrefix) {
			writeAsListOfStrings(node, optionsPrefix + "filterTag", dto.selectedFilterTags);
			writeAsListOfStrings(node, optionsPrefix + "filterGroupsWithSelectedNoneOption", dto.filterGroupsWithSelectedNoneOption);

			writeAsListOfStrings(node, optionsPrefix + "collapsedFilterTagGroups", dto.collapsedFilterGroups);
			node.AddValue(optionsPrefix + "isRestFilterTagsCollapsed", dto.restFilterTagsCollapsed);

			writeAsListOfStrings(node, optionsPrefix + "collapsedManagementTagGroups", dto.collapsedManagementGroups);
			node.AddValue(optionsPrefix + "isRestManagementTagsCollapsed", dto.restManagementTagsCollapsed);

			node.AddValue(optionsPrefix + "filterText", dto.selectedTextFilter);

		}

		void writeAsListOfStrings(ConfigNode node, string name, IEnumerable<string> list) {
			foreach (string v in list) {
				node.AddValue(name, v);
			}
		}

		private void saveNode(ConfigNode node, string file) {
			Directory.CreateDirectory(Path.GetDirectoryName(file));
			node.Save(file);
		}

		public void renameCraftInsideFile(string craftFile, string newName){
			PluginLogger.logDebug("Renaiming craft in file '" + craftFile + "' to " + newName );

			ConfigNode nodes = ConfigNode.Load(craftFile);
			nodes.SetValue("ship", newName);
			DateTime lastWriteTIme = File.GetLastWriteTime(craftFile);
			saveNode(nodes, craftFile);
			File.SetLastWriteTime(craftFile, lastWriteTIme);
		}

		ConfigNode getCacheNodeFromSettings(string settingsFilePath)
		{
			
			ConfigNode settingsNode = ConfigNode.Load(settingsFilePath);
			if (settingsNode != null)
			{
				return settingsNode.GetNode("cachedCraftData");
			}

			return null;
		}

		public void writeCraftSettings(string fileName, CraftSettingsDto settings, ConfigNode cachedCraftData)
		{
			PluginLogger.logDebug("Writing craft " + settings.craftName + " settings to '" + fileName + "'");
			ConfigNode node = new ConfigNode();
			foreach (string tag in settings.tags)
			{
				node.AddValue("tag", tag);
			}
			node.AddValue("craftName", settings.craftName);
			if (cachedCraftData != null)
			{
				node.AddNode("cachedCraftData", cachedCraftData);
			}

			saveNode(node, fileName);
		}

		public void writeCraftSettings(string fileName, CraftSettingsDto settings){
			writeCraftSettings(fileName, settings, getCacheNodeFromSettings(fileName));
		}

		public CraftSettingsDto readCraftSettings(string fileName){
			PluginLogger.logTrace ("reading craft settings from '" + fileName + "'");
			CraftSettingsDto settings = new CraftSettingsDto ();

			List<string> tags = new List<string>();
			string craftName = "";
			if (File.Exists(fileName))
			{
				ConfigNode node = ConfigNode.Load(fileName);
				if (node != null) {
					foreach (string tag in node.GetValues("tag")) {
						tags.Add(tag);
					}
					craftName = node.GetValue("craftName");
				}
			}

			settings.tags = tags.ToArray();
			settings.craftName = craftName;
			return settings;
		}

		public bool isCraftAlreadyLoadedInWorkspace(){
			return EditorLogic.fetch.ship != null && EditorLogic.fetch.ship.Count > 0;
		}

		public void mergeCraftToWorkspace(string file){
			PluginLogger.logDebug ("Mergin craft in '" + file + "' into workspace");

			ConfigNode nodes = ConfigNode.Load(file);
			ShipConstruct shipConstruct = new ShipConstruct();
			shipConstruct.LoadShip (nodes);
			EditorLogic.fetch.SpawnConstruct (shipConstruct);
		}

		public void loadCraftToWorkspace(string file){
			PluginLogger.logDebug ("Loading craft in '" + file + "' into workspace");

			EditorLogic.LoadShipFromFile(file);
			PluginLogger.logDebug("Craft loaded");
		}

		public GUISkin kspSkin(){
			return _kspSkin;
		}

		public GUISkin editorSkin(){
			return EditorLogic.fetch.shipBrowserSkin;
		}

		public void lockEditor (){
			KSPBasics.instance.lockEditor();
		}

		public void unlockEditor (){
			KSPBasics.instance.unlockEditor();
		}

		public Texture2D getThumbnail(string url) {
			return ShipConstruction.GetThumbnail(url);
		}

		public string getCurrentCraftName() {
			return EditorLogic.fetch.ship.shipName;
		}

		public string getSavePathForCraftName(string shipName) {
			return ShipConstruction.GetSavePath(shipName);
		}

		public void saveCurrentCraft() {
			string savePath = ShipConstruction.GetSavePath(EditorLogic.fetch.ship.shipName);
			PluginLogger.logDebug("Saving current shipt to " + savePath);
			EditorLogic.fetch.ship.SaveShip().Save(savePath);
			PluginLogger.logDebug("Done Saving current shipt");
		}

		public PluginSettings getPluginSettings(string fileName) {
			PluginLogger.logDebug("Reading plugin settings from " + fileName);

			PluginSettings toRet = new PluginSettings();
			toRet.debug = false;

			List<string> defaultAvailableTags = new List<string>();
			toRet.defaultAvailableTags = defaultAvailableTags;

			bool settingsChanged = false;

			if (!File.Exists(fileName)) {
				
				PluginLogger.logDebug("Plugin settings do not exist, creating default file in " + fileName);
				toRet.defaultAvailableTags = getDefaultTags();

				writePluginSettings(toRet, fileName);
			}

			ConfigNode settings = ConfigNode.Load(fileName);
			if (settings != null) {
				int settingsVersion = readSettingsVersion(settings);
				foreach (string tag in settings.GetValues("defaultAvailableTag")) {
					defaultAvailableTags.Add(tag);
				}
				if (settingsVersion <= SETTINGS_VER_2) {
					PluginLogger.logDebug("Plugin settings were in version " + settingsVersion + ", adding new default tags");
					defaultAvailableTags.AddRange(NEW_TAGS_IN_VER3);
					settingsChanged = true;
				}
				if (settingsVersion == SETTINGS_VER_3) {
					PluginLogger.logDebug("Plugin settings were in version " + settingsVersion + " which had bug that default tags may not be created. Creating them now.");
					defaultAvailableTags.AddUniqueRange(getDefaultTags());
					settingsChanged = true;
				}
				toRet.debug = readBoolFromSettings(settings, "debug", false);
			}

			if (settingsChanged) {
				writePluginSettings(toRet, fileName);
			}

			return toRet;
		}

		private ICollection<string> getDefaultTags() {
			List<string> tags = new List<string>();
			//			tags.Add (@"Bodies\Moho");
			//			tags.Add (@"Bodies\Eve");
			//			tags.Add (@"Bodies\Eve\Gilly");
			tags.Add(@"-Archived?");
			tags.Add(@"Bodies\Kerbin");
			tags.Add(@"Bodies\Kerbin\Mun");
			tags.Add(@"Bodies\Kerbin\Minmus");
			tags.Add(@"Bodies\Duna");
			//			tags.Add (@"Bodies\Duna\Ike");
			//			tags.Add (@"Bodies\Dres");
			//			tags.Add (@"Bodies\Jool");
			//			tags.Add (@"Bodies\Jool\Laythe");
			//			tags.Add (@"Bodies\Jool\Vall");
			//			tags.Add (@"Bodies\Jool\Tylo");
			//			tags.Add (@"Bodies\Jool\Bop");
			//			tags.Add (@"Bodies\Jool\Pol");
			//			tags.Add (@"Bodies\Eeloo");
			//			tags.Add (@"Type\Lander\Crew");
			//			tags.Add (@"Type\Lander\Unmanned");
			//			tags.Add (@"Type\Orbit\Crew");
			//			tags.Add (@"Type\Orbit\Unmanned");
			//			tags.Add (@"Type\SpaceStation");
			//			tags.Add (@"Type\Rover\Crew");
			//			tags.Add (@"Type\Rover\Unmanned");
			//			tags.Add (@"Type\Satellite");

			tags.Add(@"Type\Lander");
			tags.Add(@"Type\Orbit");
			tags.Add(@"Type\SpaceStation");
			tags.Add(@"Type\Rover");
			tags.Add(@"Type\Satellite");

			tags.Add(@"Status\Prototype");
			tags.Add(@"Status\Final");

			tags.AddRange(NEW_TAGS_IN_VER3);

			return tags;
		}

		private void writePluginSettings(PluginSettings settings, string fileName) {
			ConfigNode settingsToWrite = new ConfigNode();

			writeSettingsVersion(settingsToWrite);

			foreach (string tag in settings.defaultAvailableTags) {
				settingsToWrite.AddValue("defaultAvailableTag", tag);
			}

			settingsToWrite.AddValue("debug", settings.debug);

			settingsToWrite.Save(fileName);
		}

		public double getAvailableFunds() {
			if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER) {
				return Funding.Instance.Funds;
			} else {
				return -1;
			}
		}
	}
}

