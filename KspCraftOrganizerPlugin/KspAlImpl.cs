using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace KspCraftOrganizer
{
	public class KspAlImpl: IKspAl {

		private static readonly String LOCK_NAME = "KspAlImpl";
		private static readonly String SETTINGS_VER_1 = "1";
		private static readonly String SETTINGS_VER_2 = "2";

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
		}

		public void onEditorStarted() {
			//
			//If "Go To" mod is used and user goes from VAB->SPH then there is a bug that during "OnDestroy" events etc new facility is returned instead of old.
			//This code corrects this by remembering facility at the beginning and returning it until it is truly changed.
			//
			this.editorFacility = EditorDriver.editorFacility;
			COLogger.logDebug("Setting editor facility to " + this.editorFacility);
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
			COLogger.logTrace ("reading craft file from '" + craftFile + "'");

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

					ShipConstruction.GetPartCosts(part, availablePart, out dryCost, out fuelCost);
					//COLogger.Log("For part" + getPartName(part)  + " dry cost: " + dryCost + ", fuelCost: " + fuelCost + ", smth: " + partCostsSmth);
					costSum += dryCost + fuelCost;


					float dryMass;
					float fuelMass;
					ShipConstruction.GetPartTotalMass(part, availablePart, out dryMass, out fuelMass);
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

			COLogger.logTrace("Part '" + partName + "' not found");
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
			COLogger.logDebug ("reading profile settings from '" + fileName + "'");
			ProfileSettingsDto settings = new ProfileSettingsDto ();

			List<string> tags = new List<string> ();
			GuiStyleOption style = GuiStyleOption.Ksp;

			ProfileAllFilterSettingsDto allFilterSettings = new ProfileAllFilterSettingsDto();

			if (File.Exists(fileName)) {
				ConfigNode node = ConfigNode.Load(fileName);
				if (node != null) {
					string settingsVersion = node.GetValue("version");
					if (settingsVersion == null || settingsVersion == "") {
						settingsVersion = SETTINGS_VER_1;
					}

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

		private void readFilterSettingsToDto(ProfileFilterSettingsDto dto, ConfigNode node, string optionsPrefix) {

			List<string> selectedTags = new List<string>();
			List<string> filterGroupsWithSelectedNoneOption = new List<string>();
			string filterText = "";

			foreach (string tag in node.GetValues(optionsPrefix + "filterTag")) {
				selectedTags.Add(tag);
			}
			foreach (string groupName in node.GetValues(optionsPrefix + "filterGroupsWithSelectedNoneOption")) {
				filterGroupsWithSelectedNoneOption.Add(groupName);
			}
			filterText = node.GetValue(optionsPrefix + "filterText");
			if (filterText == null) {
				filterText = "";
			}

			dto.selectedFilterTags = selectedTags.ToArray();
			dto.selectedTextFilter = filterText;
			dto.filterGroupsWithSelectedNoneOption = filterGroupsWithSelectedNoneOption;
		}

		public void writeProfileSettings(string fileName, ProfileSettingsDto toWrite) {
			COLogger.logDebug("Writing profile settings to '" + fileName);
			ConfigNode node = new ConfigNode();

			node.AddValue("version", SETTINGS_VER_2);

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

		private void writeFilterSettingsFromDto(ProfileFilterSettingsDto dto, ConfigNode node, string optionsPrefix) {
			foreach (string filterTag in dto.selectedFilterTags) {
				node.AddValue(optionsPrefix + "filterTag", filterTag);
			}
			foreach (string groupName in dto.filterGroupsWithSelectedNoneOption) {
				node.AddValue(optionsPrefix + "filterGroupsWithSelectedNoneOption", groupName);
			}
			node.AddValue(optionsPrefix + "filterText", dto.selectedTextFilter);

		}

		private void saveNode(ConfigNode node, string file) {
			Directory.CreateDirectory(Path.GetDirectoryName(file));
			node.Save(file);
		}

		public void renameCraftInsideFile(string craftFile, string newName){
			COLogger.logDebug("Renaiming craft in file '" + craftFile + "' to " + newName );

			ConfigNode nodes = ConfigNode.Load(craftFile);
			nodes.SetValue("ship", newName);
			saveNode(nodes, craftFile);
		}

		public void writeCraftSettings(string fileName, CraftSettingsDto settings){
			COLogger.logDebug("Writing craft " + settings.craftName + " settings to '" + fileName + "'");
			ConfigNode node = new ConfigNode();
			foreach (string tag in settings.tags)
			{
				node.AddValue("tag", tag);
			}
			node.AddValue("craftName", settings.craftName);
			saveNode(node, fileName);

		}

		public CraftSettingsDto readCraftSettings(string fileName ){
			COLogger.logTrace ("reading craft settings from '" + fileName + "'");
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
			COLogger.logDebug ("Mergin craft in '" + file + "' into workspace");

			ConfigNode nodes = ConfigNode.Load(file);
			ShipConstruct shipConstruct = new ShipConstruct();
			shipConstruct.LoadShip (nodes);
			EditorLogic.fetch.SpawnConstruct (shipConstruct);
		}

		public void loadCraftToWorkspace(string file){
			COLogger.logDebug ("Loading craft in '" + file + "' into workspace");

			EditorLogic.LoadShipFromFile(file);
			COLogger.logDebug("Craft loaded");
		}

		public GUISkin kspSkin(){
			return HighLogic.Skin;
		}

		public GUISkin editorSkin(){
			return EditorLogic.fetch.shipBrowserSkin;
		}

		public void lockEditor (){
			EditorLogic.fetch.toolsUI.enabled = false;
			EditorLogic.fetch.enabled = false;
			EditorLogic.fetch.Lock (true, true, true, LOCK_NAME);
		}

		public void unlockEditor (){
			if (EditorLogic.fetch != null) {
				if (EditorLogic.fetch.toolsUI != null) {
					EditorLogic.fetch.toolsUI.enabled = true;
				}
				EditorLogic.fetch.enabled = true;
				EditorLogic.fetch.Unlock(LOCK_NAME);
			}
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
			COLogger.logDebug("Saving current shipt to " + savePath);
			EditorLogic.fetch.ship.SaveShip().Save(savePath);
			COLogger.logDebug("Done Saving current shipt");
		}

		public PluginSettings getPluginSettings(string fileName) {
			COLogger.logDebug("Reading plugin settings from " + fileName);
			PluginSettings toRet = new PluginSettings();
			toRet.debug = false;
			toRet.defaultAvailableTags = new List<string>();
			if (!File.Exists(fileName)){
				COLogger.logDebug("Plugin settings do not exist, creating default file in " + fileName);

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

				ConfigNode settingsToWrite = new ConfigNode();
				foreach (string tag in tags) {
					settingsToWrite.AddValue("defaultAvailableTag", tag);
				}
				settingsToWrite.AddValue("debug", "false");
				settingsToWrite.Save(fileName);
			}
			ConfigNode settings = ConfigNode.Load(fileName);
			if (settings != null) {
				foreach (string tag in settings.GetValues("defaultAvailableTag")) {
					toRet.defaultAvailableTags.Add(tag);
				}
				toRet.debug = "true" == settings.GetValue("debug");
			}
			return toRet;
		}
	}
}

