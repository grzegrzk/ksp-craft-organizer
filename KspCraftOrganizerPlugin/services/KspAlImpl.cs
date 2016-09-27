using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using KspNalCommon;

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

		private List<string> NEW_TAGS_IN_VER3 = new List<string>(new string[] { "MasterpieceAmongCrafts"});

		private static readonly int SETTINGS_VER_NOW = SETTINGS_VER_4;

		private Dictionary<string, AvailablePart> _availablePartCache;
		private EditorFacility editorFacility;

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

		}

		public string getBaseCraftDirectory(){
			return Globals.combinePaths(getApplicationRootPath(), "saves", getNameOfSaveFolder(), "Ships");
		}


		public string getApplicationRootPath() {
			return KSPUtil.ApplicationRootPath;
		}

		public string getNameOfSaveFolder() {
			return HighLogic.SaveFolder;
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

		public CraftDaoDto getCraftInfo(string craftFile){
			PluginLogger.logTrace ("reading craft file from '" + craftFile + "'");

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
					if (!ResearchAndDevelopment.PartTechAvailable(availablePart)) {
						notEnoughScience = true;
					}
				}
			}
			toRet.name = nodes.GetValue("ship");
			toRet.description = nodes.GetValue("description");
			toRet.stagesCount = stagesCount;
			toRet.partCount = parts.Length;
			toRet.mass = massSum;
			toRet.cost = costSum;
			toRet.containsMissedParts = available;
			toRet.notEnoughScience = notEnoughScience;
			return toRet;
		}

		public string getAutoSaveCraftName() {
			return EditorLogic.autoShipName;
		}

		private AvailablePart getAvailablePartFor(ConfigNode part)
		{
			string partName = getPartName(part);
			//COLogger.Log("Finding part '" + partName + "'");
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


					//if (settingsVersion == SETTINGS_VER_3) {
					//	COLogger.logDebug("Profile settings were in version " + settingsVersion + " which had bug that default tags were not created. Adding them now.");
					//	tags.AddRange(createDefaultTags());
					//}

					string styleId = node.GetValue("guiStyle");
					foreach (GuiStyleOption candidateStyle in GuiStyleOption.SKIN_STATES) {
						if (candidateStyle.id == styleId) {
							style = candidateStyle;
							break;
						}
					}
				}
			} else {
				tags.AddRange(defaultTags);
			}


			settings.availableTags = tags.ToArray();
			settings.selectedGuiStyle = style;
			settings.allFilter = allFilterSettings;
			return settings;
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
			saveNode(nodes, craftFile);
		}

		public void writeCraftSettings(string fileName, CraftSettingsDto settings){
			PluginLogger.logDebug("Writing craft " + settings.craftName + " settings to '" + fileName + "'");
			ConfigNode node = new ConfigNode();
			foreach (string tag in settings.tags)
			{
				node.AddValue("tag", tag);
			}
			node.AddValue("craftName", settings.craftName);
			saveNode(node, fileName);

		}

		public CraftSettingsDto readCraftSettings(string fileName ){
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

