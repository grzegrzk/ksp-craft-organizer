using System;
using System.IO;
using System.Collections.Generic;

namespace KspCraftOrganizer
{
	public class CurrentCraftTagEntity
	{

		public string name { get; set; }

		public bool selected { get; set;  } 				//is currently selected

		public bool selectedDuringLastEdit { get; set; }    //used to remove selection if user clicks cancel

		public bool selectedOriginally { get; set; } 		//used to tell if file id dirty

	}

	public class CurrentCraftTagsController
	{
		public static CurrentCraftTagsController instance = new CurrentCraftTagsController();
		
		private SettingsService settingsService = SettingsService.instance;
		private EditorListenerService craftListenerService = EditorListenerService.instance;
		private FileLocationService fileLocationService = FileLocationService.instance;
		private IKspAl ksp = IKspAlProvider.instance;

		private SortedDictionary<string, CurrentCraftTagEntity> _availableTagsCache;

		public CurrentCraftTagsController() {
			craftListenerService.onEditorStarted += delegate () {
				_availableTagsCache = null;
			};
			craftListenerService.onShipLoaded += delegate (string fileName) {
				_availableTagsCache = null;
			};

			craftListenerService.onShipSaved += delegate (string craftFile, bool craftSavedToNewFile) {
				saveTagsToCraftIfNeeded(craftFile, craftSavedToNewFile);
			};
		}

		public void resetToLastlyEditied() {
			foreach (CurrentCraftTagEntity tagModel in availableTags) {
				tagModel.selected = tagModel.selectedDuringLastEdit;
			}
		}

		public void saveIfPossible() {
			if (craftListenerService.canAutoSaveSomethingToDisk()) {
				saveTagsToCraftIfNeeded(craftListenerService.currentShipFile, false);
			}
			foreach (CurrentCraftTagEntity tag in availableTags) {
				tag.selectedDuringLastEdit = tag.selected;
			}
		}

		internal void userAddAvailableTag(string newTagText) {
			ensureTagsCacheLoaded();
			if (!_availableTagsCache.ContainsKey(newTagText)) {
				settingsService.addAvailableTag(ksp.getNameOfSaveFolder(), newTagText);
				addTagIfNeeded(newTagText);
			}
		}

		private void saveTagsToCraftIfNeeded(string craftFile, bool craftSavedToNewFile) {
			if (isDirty() || craftSavedToNewFile) {
				COLogger.logDebug("Writing craft settings from craft management window");

				CraftSettingsDto dto = new CraftSettingsDto();

				List<string> selectedTags = new List<string>();
				foreach (CurrentCraftTagEntity tag in availableTags) {
					if (tag.selected) {
						selectedTags.Add(tag.name);
					}
					tag.selectedDuringLastEdit = tag.selected;
				}
				dto.craftName = ksp.getCurrentCraftName();
				dto.tags = selectedTags.ToArray();
				COLogger.logDebug("Selected tags that will be saved: " + Globals.join(dto.tags, ", "));

				settingsService.writeCraftSettingsForCraftFile(fileLocationService.getCraftSettingsFileForCraftFile(craftFile), dto);
			}
			_availableTagsCache = null;
		}

		private bool isDirty() {
			if (_availableTagsCache != null) {
				foreach (CurrentCraftTagEntity tag in availableTags) {
					if (tag.selected != tag.selectedOriginally) {
						return true;
					}
				}
			}
			return false;
		}

		public ICollection<CurrentCraftTagEntity> availableTags
		{
			get {
				ensureTagsCacheLoaded();
				return _availableTagsCache.Values;
			}
		}

		private void ensureTagsCacheLoaded() {
			if (_availableTagsCache == null) {
				_availableTagsCache = new SortedDictionary<string, CurrentCraftTagEntity>();
				foreach (string tag in settingsService.readProfileSettings(ksp.getNameOfSaveFolder()).availableTags) {
					addTagIfNeeded(tag);
				}

				COLogger.logDebug("Reading current's craft tags assuming its file is " + craftListenerService.originalShipFile);
				if (!craftListenerService.isNewEditor() && File.Exists(craftListenerService.originalShipFile)) {
					ICollection<string> tags = settingsService.readCraftSettingsForCraftFile(craftListenerService.originalShipFile).tags;
					foreach (string tag in tags) {
						addTagIfNeeded(tag);
						_availableTagsCache[tag].selectedDuringLastEdit = true;
						_availableTagsCache[tag].selectedOriginally = true;
						_availableTagsCache[tag].selected = true;
					}
					COLogger.logDebug("Tags from file " + Globals.join(tags, ", "));
				} else {
					COLogger.logDebug("Tags will not be read - editor is new or file '" + craftListenerService.originalShipFile + "' does not exist");
				}

			}
		}

		private void addTagIfNeeded(string tag) {
			if (!_availableTagsCache.ContainsKey(tag)) {
				CurrentCraftTagEntity newTag = new CurrentCraftTagEntity();
				newTag.name = tag;
				_availableTagsCache.Add(tag, newTag);
			}
		}

	}
}

