using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace KspCraftOrganizer {
	public class CraftDaoDto {
		public string name { get; set; }
		public int stagesCount { get; set; }
		public float cost { get; set; }
		public int partCount { get; set; }
		public float mass { get; set; }
		public bool isStock { get; set; }
		public bool containsMissedParts { get; set; }
		public bool notEnoughScience { get; set; }
		public string description { get; internal set; }
	}

	public class PluginSettings{
		public bool debug;	
		public ICollection<string> defaultAvailableTags { get; set; }
	}

	public class GuiStyleOption {

		public static readonly GuiStyleOption Ksp = new GuiStyleOption("KSP", "KSP");
		public static readonly GuiStyleOption Default = new GuiStyleOption("Default", "Default");

		public static readonly string[] SKIN_DISPLAY_OPTIONS = { GuiStyleOption.Default.displayName, GuiStyleOption.Ksp.displayName };
		public static readonly GuiStyleOption[] SKIN_STATES = { GuiStyleOption.Default, GuiStyleOption.Ksp };

		private string _id;
		private string _displayName;

		public string id { get { return _id; }  }
		public string displayName { get { return _displayName;} }

		private GuiStyleOption(string id, string displayName) {
			this._id = id;
			this._displayName = displayName;
		}
		
	}

	public class ProfileFilterSettingsDto {

		public ProfileFilterSettingsDto() {
			this.selectedTextFilter = "";
			this.selectedFilterTags = new string[0];
			this.filterGroupsWithSelectedNoneOption = new List<string>();
		}

		public string selectedTextFilter { get; set; }
		
		public string[] selectedFilterTags { get; set; }

		public ICollection<string> filterGroupsWithSelectedNoneOption { get; set; }

	}


	public class ProfileAllFilterSettingsDto {
		public ProfileFilterSettingsDto filterVabInVab = new ProfileFilterSettingsDto();
		public ProfileFilterSettingsDto filterVabInSph = new ProfileFilterSettingsDto();
		public ProfileFilterSettingsDto filterSphInVab = new ProfileFilterSettingsDto();
		public ProfileFilterSettingsDto filterSphInSph = new ProfileFilterSettingsDto();
	}

	public class ProfileSettingsDto {
		public ICollection<string> availableTags { get; set; }

		public ProfileAllFilterSettingsDto allFilter { get; set; }

		public GuiStyleOption selectedGuiStyle { get; set; }

	}

	public class CraftSettingsDto{
		public ICollection<string> tags { get; set;}
		public string craftName { get; set; }
	}

	public static class IKspAlProvider{
		private static IKspAl _instance;

		public static IKspAl instance {
			get {
				if (_instance == null) {
					Type type = Type.GetType ("KspCraftOrganizer.KspAlImpl");
					if (type == null) {
						type  = Type.GetType ("KspCraftOrganizer.KspAlMock");
					}
					COLogger.logDebug("Using dao " + type);
					try{
						_instance = (IKspAl)Activator.CreateInstance (type);
						COLogger.logDebug("Dao created");
					}catch(Exception ex){
						COLogger.logDebug("Cannot create instance of DAO, creating mock instead " + ex.Message);
						_instance = new KspAlMock ();
					}
				}
				return _instance;
			}
		}
	}

	/**
	 * IKspAl = KSP Abstraction Layer. 
	 * 
	 * Original idea was to add all KSP-dependend code to the class inheriting from this interface to
	 * make it possible to test the solution in bare Unity runner. Later it turned out that Kramax Plugin Reloader works quite well
	 * so it is possible to change code on the fly without problems inside KSP. Because of that later there were introduced some 
	 * direct dependicies to KSP in other parts of the code as well, but for now this interface stays as a primary separation layer.
	 */
	public interface IKspAl{

		void start();

		void destroy();

		PluginSettings getPluginSettings(string fileName);

		string getBaseCraftDirectory ();

		CraftType getCurrentEditorFacilityType ();

		CraftDaoDto getCraftInfo (string craftFile);

		ProfileSettingsDto readProfileSettings (string fileName, ICollection<string> defaultTags);

		void renameCraftInsideFile (string fileName, string newName);

		void writeProfileSettings (string fileName, ProfileSettingsDto toWrite);

		void writeCraftSettings (string fileName, CraftSettingsDto settings);

		CraftSettingsDto readCraftSettings (string fileName);

		bool isCraftAlreadyLoadedInWorkspace ();

		void mergeCraftToWorkspace (string file);

		void loadCraftToWorkspace (string file);

		GUISkin kspSkin ();

		GUISkin editorSkin ();

		void lockEditor ();

		void unlockEditor ();

		bool isShowStockCrafts();

		string getStockCraftDirectory();

		string getAutoSaveCraftName();

		string getNameOfSaveFolder();

		string getApplicationRootPath();

		Texture2D getThumbnail(string url);

		string getCurrentCraftName();

		//string getSavePathForCraftName(string shipName);

		void saveCurrentCraft();
	}

}
