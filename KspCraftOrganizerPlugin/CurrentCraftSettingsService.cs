using System;
using System.IO;
using System.Collections.Generic;

namespace KspCraftOrganizer
{
	public class CurrentCraftTagModel
	{

		public string name { get; set; }

		public bool selected { get; set;  } 

		public bool selectedDuringLastEdit { get; set; }

		public bool selectedOriginally { get; set; }

	}

	public class CurrentCraftSettingsService
	{
		public static CurrentCraftSettingsService instance = new CurrentCraftSettingsService();
		
		private SettingsService settingsService = SettingsService.instance;
		private EditorListenerService craftListenerService = EditorListenerService.instance;
		private FileLocationService fileLocationService = FileLocationService.instance;
		private IKspAl ksp = IKspAlProvider.instance;

		private SortedDictionary<string, CurrentCraftTagModel> _availableTagsCache;

		public CurrentCraftSettingsService() {
			craftListenerService.onEditorStarted += delegate () {
				_availableTagsCache = null;
			};

			craftListenerService.onShipSaved += delegate (string craftFile, bool craftSavedToNewFile) {
				saveTagsToCraftIfNeeded(craftFile, craftSavedToNewFile);
			};
		}

		public void resetToLastlyEditied() {
			foreach (CurrentCraftTagModel tagModel in availableTags) {
				tagModel.selected = tagModel.selectedDuringLastEdit;
			}
			refreshExistingTags();
		}

		public void saveIfPossible() {
			if (craftListenerService.canAutoSaveSomethingToDisk()) {
				saveTagsToCraftIfNeeded(craftListenerService.currentShipFile, false);
			}
			foreach (CurrentCraftTagModel tag in availableTags) {
				tag.selectedDuringLastEdit = tag.selected;
			}
		}

		internal void addAvailableTag(string newTagText) {
			ensureTagsCacheLoaded();
			if (!_availableTagsCache.ContainsKey(newTagText)) {
				settingsService.addAvailableTag(newTagText);
				addTagIfNeeded(newTagText);
			}
		}

		private void saveTagsToCraftIfNeeded(string craftFile, bool craftSavedToNewFile) {
			if (isDirty() || craftSavedToNewFile) {
				COLogger.logDebug("Writing craft settings from craft management window");

				CraftSettingsDto dto = new CraftSettingsDto();

				List<string> selectedTags = new List<string>();
				foreach (CurrentCraftTagModel tag in availableTags) {
					if (tag.selected) {
						selectedTags.Add(tag.name);
					}
					tag.selectedDuringLastEdit = tag.selected;
				}
				dto.craftName = ksp.getCurrentCraftName();
				dto.tags = selectedTags.ToArray();

				settingsService.writeCraftSettingsForCraftFile(fileLocationService.getCraftSettingsFileForCraftFile(craftFile), dto);
			} 
		}

		private bool isDirty() {
			if (_availableTagsCache != null) {
				foreach (CurrentCraftTagModel tag in availableTags) {
					if (tag.selected != tag.selectedOriginally) {
						return true;
					}
				}
			}
			return false;
		}

		public ICollection<CurrentCraftTagModel> availableTags
		{
			get {
				ensureTagsCacheLoaded();
				return _availableTagsCache.Values;
			}
		}

		private void ensureTagsCacheLoaded() {
			if (_availableTagsCache == null) {
				_availableTagsCache = new SortedDictionary<string, CurrentCraftTagModel>();
				foreach (string tag in settingsService.readProfileSettings().availableTags) {
					addTagIfNeeded(tag);
				}

				COLogger.logDebug("Reading current's craft tags from assuming its file " + craftListenerService.originalShipFile);
				if (!craftListenerService.isNewEditor() && File.Exists(craftListenerService.originalShipFile)) {
					foreach (string tag in settingsService.readCraftSettingsForCraftFile(craftListenerService.originalShipFile).tags) {
						addTagIfNeeded(tag);
						_availableTagsCache[tag].selectedDuringLastEdit = true;
						_availableTagsCache[tag].selectedOriginally = true;
						_availableTagsCache[tag].selected = true;
					}
				}
			}
		}

		private void refreshExistingTags() {
			if (_availableTagsCache == null) {
				ensureTagsCacheLoaded();
			} else {
				SortedList<string, string> tagsNow = new SortedList<string, string>();
				foreach (string tag in settingsService.readProfileSettings().availableTags) {
					addTagIfNeeded(tag);
					tagsNow.Add(tag, tag);
				}
				List<string> toRemove = new List<string>();
				foreach (string tag in _availableTagsCache.Keys) {
					if (!tagsNow.ContainsKey(tag)) {
						toRemove.Add(tag);
					}
				}
				foreach (string tagToRemove in toRemove) {
					_availableTagsCache.Remove(tagToRemove);
				}
			}
		}

		private void addTagIfNeeded(string tag) {
			if (!_availableTagsCache.ContainsKey(tag)) {
				CurrentCraftTagModel newTag = new CurrentCraftTagModel();
				newTag.name = tag;
				_availableTagsCache.Add(tag, newTag);
			}
		}

	}
}

