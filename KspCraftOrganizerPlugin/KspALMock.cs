using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace KspCraftOrganizer
{
	public class KspAlMock: IKspAl{

		public void start() {
		}

		public void destroy() {
		}

		public string getBaseCraftDirectory(){
			return "/Users/nal/Library/Application Support/Steam/steamapps/common/Kerbal Space Program/saves/sandbox/Ships";
		}

		public string getStockCraftDirectory()
		{
			return "/Users/nal/Library/Application Support/Steam/steamapps/common/Kerbal Space Program/Ships";
		}

		public string getApplicationRootPath() {
			return "/Users/nal/Library/Application Support/Steam/steamapps/common/Kerbal Space Program";
		}

		public string getNameOfSaveFolder() {
			return "sandbox";
		}

		public bool isShowStockCrafts()
		{
			return true;
		}

		public CraftType getCurrentEditorFacilityType(){
			return CraftType.SPH;
		}

		public CraftDaoDto getCraftInfo(string craftFile){
			COLogger.logDebug ("reading craft file from '" + craftFile + "'");
			System.Random r = new System.Random(craftFile.GetHashCode());
			CraftDaoDto toRet = new CraftDaoDto ();
			toRet.name = Path.GetFileNameWithoutExtension (craftFile);
			toRet.cost = r.Next () % 100000000;
			toRet.mass = r.Next () % 100000000;
			toRet.partCount = r.Next () % 10000;
			toRet.stagesCount = r.Next () % 100;
			return toRet;
		}

		public ProfileSettingsDto readProfileSettings(string fileName, ICollection<string> defaultTags){
			COLogger.logDebug ("reading profile settings from '" + fileName + "'");
			ProfileSettingsDto settings = new ProfileSettingsDto ();

			List<string> tags = new List<string> ();
			tags.AddRange(defaultTags);
			settings.availableTags = tags.ToArray();
			settings.allFilter = new ProfileAllFilterSettingsDto();

			fillFilterSettings(settings.allFilter.filterSphInSph);
			fillFilterSettings(settings.allFilter.filterSphInVab);
			fillFilterSettings(settings.allFilter.filterVabInSph);
			fillFilterSettings(settings.allFilter.filterVabInVab);

			return settings;
		}

		void fillFilterSettings(ProfileFilterSettingsDto settings) {
			settings.selectedFilterTags = new string[] { };
			settings.selectedTextFilter = "";
			settings.filterGroupsWithSelectedNoneOption = new List<string>();
		}

		public void renameCraftInsideFile(string fileName, string newName){
			COLogger.logDebug("Renaiming craft in file '" + fileName + "' to " + newName );
		}
		public void writeProfileSettings(string fileName, ProfileSettingsDto toWrite){
			COLogger.logDebug("Writing profile settings to '" + fileName + "'");
		}

		public void writeCraftSettings(string fileName, CraftSettingsDto settings){
			COLogger.logDebug("Writing craft settings to '" + fileName + "'");
		}

		public CraftSettingsDto readCraftSettings(string fileName ){
			COLogger.logDebug ("reading craft settings from '" + fileName + "'");
			CraftSettingsDto settings = new CraftSettingsDto ();
			settings.tags = new string[] { "tag1", "tag2" };
			settings.craftName = "";
			return settings;
		}

		public bool isCraftAlreadyLoadedInWorkspace(){
			return true;
		}

		public void mergeCraftToWorkspace(string file){
			COLogger.logDebug ("Merging craft in '" + file + "' into workspace");
		}

		public void loadCraftToWorkspace(string file){
			COLogger.logDebug ("Loading craft in '" + file + "' into workspace");
		}

		public GUISkin kspSkin(){
			return GUI.skin;
		}


		public GUISkin editorSkin(){
			return GUI.skin;
		}

		public void lockEditor (){
			COLogger.logDebug ("Locking editor");
		}

		public void unlockEditor (){
			COLogger.logDebug ("Unlocking editor");
		}

		public string getAutoSaveCraftName() {
			return "Auto-Save";
		}

		public Texture2D getThumbnail(string url) {
			Texture2D t = new Texture2D(38, 38);
			return t;
		}

		public string getCurrentCraftName() {
			return "My craft";
		}

		public string getSavePathForCraftName(string shipName) {
			return Path.Combine(getBaseCraftDirectory(), shipName + ".craft");	
		}
		public void saveCurrentCraft() {
			COLogger.logDebug("Saving current craft");
		}


		public PluginSettings getPluginSettings(string fileName) {
			PluginSettings toRet = new PluginSettings();
			toRet.debug = true;
			toRet.defaultAvailableTags = new List<string>();
			//			toRet.defaultAvailableTags .Add (@"Bodies\Moho");
			//			toRet.defaultAvailableTags .Add (@"Bodies\Eve");
			//			toRet.defaultAvailableTags .Add (@"Bodies\Eve\Gilly");
			toRet.defaultAvailableTags.Add(@"Bodies\Kerbin");
			toRet.defaultAvailableTags.Add(@"Bodies\Kerbin\Mun");
			toRet.defaultAvailableTags.Add(@"Bodies\Kerbin\Minmus");
			toRet.defaultAvailableTags.Add(@"Bodies\Duna");
			//			toRet.defaultAvailableTags.Add (@"Bodies\Duna\Ike");
			//			toRet.defaultAvailableTags.Add (@"Bodies\Dres");
			//			toRet.defaultAvailableTags.Add (@"Bodies\Jool");
			//			toRet.defaultAvailableTags.Add (@"Bodies\Jool\Laythe");
			//			toRet.defaultAvailableTags.Add (@"Bodies\Jool\Vall");
			//			toRet.defaultAvailableTags.Add (@"Bodies\Jool\Tylo");
			//			toRet.defaultAvailableTags.Add (@"Bodies\Jool\Bop");
			//			toRet.defaultAvailableTags.Add (@"Bodies\Jool\Pol");
			//			toRet.defaultAvailableTags.Add (@"Bodies\Eeloo");
			//			toRet.defaultAvailableTags.Add (@"Types\Lander\Crew");
			//			toRet.defaultAvailableTags.Add (@"Types\Lander\Unmanned");
			//			toRet.defaultAvailableTags.Add (@"Types\Orbit\Crew");
			//			toRet.defaultAvailableTags.Add (@"Types\Orbit\Unmanned");
			//			toRet.defaultAvailableTags.Add (@"Types\SpaceStation");
			//			toRet.defaultAvailableTags.Add (@"Types\Rover\Crew");
			//			toRet.defaultAvailableTags.Add (@"Types\Rover\Unmanned");
			//			toRet.defaultAvailableTags.Add (@"Types\Satellite");

			toRet.defaultAvailableTags.Add(@"Types\Lander");
			toRet.defaultAvailableTags.Add(@"Types\Orbit");
			toRet.defaultAvailableTags.Add(@"Types\SpaceStation");
			toRet.defaultAvailableTags.Add(@"Types\Rover");
			toRet.defaultAvailableTags.Add(@"Types\Satellite");

			toRet.defaultAvailableTags.Add(@"BuildingStage\Prototype");
			toRet.defaultAvailableTags.Add(@"BuildingStage\Final");
			return toRet;
		}
	}
}

