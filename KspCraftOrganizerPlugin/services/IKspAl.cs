using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using KspNalCommon;

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

	public class ProfileFilterSettingsDto {

		public ProfileFilterSettingsDto() {
			this.selectedTextFilter = "";
			this.selectedFilterTags = new string[0];
			this.filterGroupsWithSelectedNoneOption = new List<string>();
			this.collapsedFilterGroups = new List<string>();
			this.collapsedManagementGroups = new List<string>();
		}

		public string selectedTextFilter { get; set; }
		
		public string[] selectedFilterTags { get; set; }

		public ICollection<string> filterGroupsWithSelectedNoneOption { get; set; }

		public ICollection<string> collapsedFilterGroups { get; set; }

		public bool restFilterTagsCollapsed { get; set; }

		public ICollection<string> collapsedManagementGroups { get; set; }

		public bool restManagementTagsCollapsed { get; set; }
	}


	public class ProfileAllFilterSettingsDto {
		public ProfileFilterSettingsDto filterVabInVab = new ProfileFilterSettingsDto();
		public ProfileFilterSettingsDto filterVabInSph = new ProfileFilterSettingsDto();
		public ProfileFilterSettingsDto filterSphInVab = new ProfileFilterSettingsDto();
		public ProfileFilterSettingsDto filterSphInSph = new ProfileFilterSettingsDto();

		public ProfileFilterSettingsDto getFilterDtoFor(CraftType currentFacility, CraftType selectedCraftType) {
			if (currentFacility == CraftType.VAB) {
				if (selectedCraftType == CraftType.VAB) {
					return this.filterVabInVab;
				} else {
					return this.filterSphInVab;
				}
			} else {
				if (selectedCraftType == CraftType.VAB) {
					return this.filterVabInSph;
				} else {
					return this.filterSphInSph;
				}
			}
		}
	}

	public class CraftSortingEntry {
		public string sortingId;
		public string sortingData;
		public bool isReversed;
	}

	public class ProfileSettingsDto {
		
		public ICollection<string> availableTags { get; set; }

		public ICollection<CraftSortingEntry> craftSorting { get; set; }

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
					PluginLogger.logDebug("Using dao " + type);
					try{
						_instance = (IKspAl)Activator.CreateInstance (type);
						PluginLogger.logDebug("Dao created");
					}catch(Exception ex){
						PluginLogger.logDebug("Cannot create instance of DAO, creating mock instead " + ex.Message);
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

		double getAvailableFunds();
		
		void onGUI(GUISkin originalSkin);
	}

}
