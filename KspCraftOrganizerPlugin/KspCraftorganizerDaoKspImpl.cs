using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace KspCraftOrganizer
{
	public class KspCraftorganizerDaoKspImpl{

		public string getBaseCraftDirectory(){
			return "/Users/nal/Library/Application Support/Steam/steamapps/common/Kerbal Space Program/saves/sandbox/Ships";
		}

		public CraftType getCurrentCraftType(){
			return CraftType.SPH;
		}

		public CraftDaoDto getCraftInfo(string craftFile){
			COLogger.Log ("reading craft file from '" + craftFile + "'");
			System.Random r = new System.Random(craftFile.GetHashCode());
			CraftDaoDto toRet = new CraftDaoDto ();
			toRet.name = Path.GetFileNameWithoutExtension (craftFile);
			toRet.cost = r.Next () % 100000000;
			toRet.mass = r.Next () % 100000000;
			toRet.partCount = r.Next () % 10000;
			toRet.stagesCount = r.Next () % 100;
			return toRet;
		}

		public ProfileSettingsDto readProfileSettings(string fileName){
			COLogger.Log ("reading profile settings from '" + fileName + "'");
			ProfileSettingsDto settings = new ProfileSettingsDto ();

			List<string> tags = new List<string> ();
			//			tags.Add (@"Bodies\Moho");
			//			tags.Add (@"Bodies\Eve");
			//			tags.Add (@"Bodies\Eve\Gilly");
			tags.Add (@"Bodies\Kerbin");
			tags.Add (@"Bodies\Kerbin\Mun");
			tags.Add (@"Bodies\Kerbin\Minmus");
			tags.Add (@"Bodies\Duna");
			//			tags.Add (@"Bodies\Duna\Ike");
			//			tags.Add (@"Bodies\Dres");
			//			tags.Add (@"Bodies\Jool");
			//			tags.Add (@"Bodies\Jool\Laythe");
			//			tags.Add (@"Bodies\Jool\Vall");
			//			tags.Add (@"Bodies\Jool\Tylo");
			//			tags.Add (@"Bodies\Jool\Bop");
			//			tags.Add (@"Bodies\Jool\Pol");
			//			tags.Add (@"Bodies\Eeloo");
			//			tags.Add (@"Types\Lander\Crew");
			//			tags.Add (@"Types\Lander\Unmanned");
			//			tags.Add (@"Types\Orbit\Crew");
			//			tags.Add (@"Types\Orbit\Unmanned");
			//			tags.Add (@"Types\SpaceStation");
			//			tags.Add (@"Types\Rover\Crew");
			//			tags.Add (@"Types\Rover\Unmanned");
			//			tags.Add (@"Types\Satellite");

			tags.Add (@"Types\Lander");
			tags.Add (@"Types\Orbit");
			tags.Add (@"Types\SpaceStation");
			tags.Add (@"Types\Rover");
			tags.Add (@"Types\Satellite");

			tags.Add (@"BuildingStage\Prototype");
			tags.Add (@"BuildingStage\Final");
			//
			//			int count = 2;
			//			string[] availableTags = new string[count];
			//			for(int i = 0; i < count; ++i){
			//				availableTags [i] = "Test tag number " + i;
			//			}
			settings.availableTags = tags.ToArray();
			settings.selectedFilterTags = new string[]{ };
			settings.selectedTextFilter = "";
			return settings;
		}

		public void renameCraft(string fileName, string newName){
			COLogger.Log("Renaiming craft in file '" + fileName + "' to " + newName );
		}
		public void writeProfileSettings(string fileName, ProfileSettingsDto toWrite){
			COLogger.Log("Writing profile settings to '" + fileName + "'");
		}

		public void writeCraftSettings(string fileName, PerCraftSettingsDto settings){
			COLogger.Log("Writing craft settings to '" + fileName + "'");
		}

		public PerCraftSettingsDto readCraftSettings(string fileName ){
			COLogger.Log ("reading craft settings from '" + fileName + "'");
			PerCraftSettingsDto settings = new PerCraftSettingsDto ();
			settings.selectedTags = new string[] { "tag1", "tag2" };
			return settings;
		}

		public bool isCraftAlreadyLoadedInWorkspace(){
			return true;
		}

		public void mergeCraftToWorkspace(string file){
			COLogger.Log ("Mergin craft in '" + file + "' into workspace");
		}

		public void loadCraftToWorkspace(string file){
			COLogger.Log ("Loading craft in '" + file + "' into workspace");
		}

		public GUISkin guiSkin(){
			return HighLogic.Skin;
		}
	}
}

