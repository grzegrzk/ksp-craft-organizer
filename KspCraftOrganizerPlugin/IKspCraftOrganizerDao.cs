using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace KspCraftOrganizer
{
	public class CraftDaoDto{
		public string name {get ;set; }
		public int stagesCount {get ;set; }
		public int cost { get ; set; }
		public int partCount { get ; set; }
		public int mass { get ; set; }
	}

	public class ProfileSettingsDto{
		public string[] availableTags { get; set;}

		public string[] selectedFilterTags { get; set; }

		public string selectedTextFilter { get; set; }

	}

	public class PerCraftSettingsDto{
		public ICollection<string> selectedTags { get; set;}
	}

	public static class IKspCraftorganizerDaoProvider{
		private static IKspCraftorganizerDao _instance;

		public static IKspCraftorganizerDao instance {
			get {
				if (_instance == null) {
					Type type = Type.GetType ("KspCraftOrganizer.KspCraftorganizerDaoKspImpl");
					if (type == null) {
						type  = Type.GetType ("KspCraftOrganizer.KspCraftorganizerDaoMockImpl");
					}
					COLogger.Log("Using dao " + type);
					_instance = (IKspCraftorganizerDao)Activator.CreateInstance (type);
					COLogger.Log("Dao created");
				}
				return _instance;
			}
		}
	}

	public interface IKspCraftorganizerDao{
		

		string getBaseCraftDirectory ();

		CraftType getCurrentCraftType ();

		CraftDaoDto getCraftInfo (string craftFile);

		ProfileSettingsDto readProfileSettings (string fileName);

		void renameCraft (string fileName, string newName);

		void writeProfileSettings (string fileName, ProfileSettingsDto toWrite);

		void writeCraftSettings (string fileName, PerCraftSettingsDto settings);

		PerCraftSettingsDto readCraftSettings (string fileName);

		bool isCraftAlreadyLoadedInWorkspace ();

		void mergeCraftToWorkspace (string file);

		void loadCraftToWorkspace (string file);

		GUISkin guiSkin ();
	}
	
}
