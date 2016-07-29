using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace KspCraftOrganizer {
	public class OrganizerServiceCraftList {

		public delegate bool CraftFilterPredicate(OrganizerCraftModel craft, out bool shouldBeVisibleByDefault);

		private List<OrganizerCraftModel> cachedAvailableCrafts;
		private OrganizerCraftModel[] cachedFilteredCrafts;
		private OrganizerCraftModel _primaryCraft;
		private int cachedSelectedCraftsCount;
		private CraftType _craftType;
		private Dictionary<CraftType, List<OrganizerCraftModel>> craftTypeToAvailableCraftsLazy = new Dictionary<CraftType, List<OrganizerCraftModel>>();
			
		private IKspAl ksp = IKspAlProvider.instance;
		private FileLocationService fileLocationService = FileLocationService.instance;
		private SettingsService settingsService = SettingsService.instance;
		private OrganizerService parent;

		public OrganizerServiceCraftList(OrganizerService parent) {
			this.parent = parent;
			_craftType = ksp.getCurrentEditorFacilityType();
		}

		public bool selectAllFiltered {
			get;
			private set;
		}

		public bool forceUncheckSelectAllFiltered {
			get;
			set; 
		}

		public void update(bool selectAll, bool filterChanged) {
			if (this.cachedFilteredCrafts == null || filterChanged) {
				this.cachedFilteredCrafts = createFilteredCrafts(parent.craftFilterPredicate);
				parent.markFilterAsUpToDate();
			}
			updateSelectedCrafts(selectAll);
		}

		public bool craftsAreFiltered { get; private set; }

		public OrganizerCraftModel[] filteredCrafts {
			get {
				if (cachedFilteredCrafts == null) {
					return new OrganizerCraftModel[0];
				}
				return cachedFilteredCrafts;
			}
		}

		private OrganizerCraftModel[] createFilteredCrafts(CraftFilterPredicate craftFilterPredicate) {
			COLogger.logDebug("Creating filtered crafts");
			List<OrganizerCraftModel> filtered = new List<OrganizerCraftModel>();
			craftsAreFiltered = false;
			foreach (OrganizerCraftModel craft in availableCrafts) {
				bool shouldBeVisibleByDefault;
				if (craftFilterPredicate(craft, out shouldBeVisibleByDefault)) {
					filtered.Add(craft);
					if (!shouldBeVisibleByDefault) {
						craftsAreFiltered = true;
					}
				} else {
					if (shouldBeVisibleByDefault) {
						craftsAreFiltered = true;
					}
					if (craft.isSelectedPrimary) {
						primaryCraft = null;
					}
					craft.setSelectedInternal(false);
				}
			}
			return filtered.ToArray();
		}

		public List<OrganizerCraftModel> availableCrafts {
			get {
				if (cachedAvailableCrafts == null) {
					cachedAvailableCrafts = getCraftsOfType(_craftType);
				}
				return cachedAvailableCrafts;
			}
		}

		public List<OrganizerCraftModel> getCraftsOfType(CraftType type) {
			if (!craftTypeToAvailableCraftsLazy.ContainsKey(type)) {
				craftTypeToAvailableCraftsLazy.Add(type, fetchAvailableCrafts(type));
			}
			return craftTypeToAvailableCraftsLazy[type];
		}

		private List<OrganizerCraftModel> fetchAvailableCrafts(CraftType type) {
			COLogger.logDebug("fetching '" + type + "' crafts from disk");

			string craftDirectory = fileLocationService.getCraftDirectoryForCraftType(type);
			List<OrganizerCraftModel> toRetList = new List<OrganizerCraftModel>();
			toRetList.AddRange(fetchAvailableCrafts(craftDirectory, type, false));
			if (ksp.isShowStockCrafts()) {
				craftDirectory = fileLocationService.getStockCraftDirectoryForCraftType(type);
				toRetList.AddRange(fetchAvailableCrafts(craftDirectory, type, true));
			}


			toRetList.Sort(delegate (OrganizerCraftModel c1, OrganizerCraftModel c2) {
				int craftComparisonResult = -c1.isAutosaved.CompareTo(c2.isAutosaved);
				if (craftComparisonResult == 0) {
					craftComparisonResult = c1.name.CompareTo(c2.name);
				}
				return craftComparisonResult;
			});
			return toRetList;
		}

		private OrganizerCraftModel[] fetchAvailableCrafts(String craftDirectory, CraftType type, bool isStock) {
			COLogger.logDebug("fetching '" + type + "' crafts from disk from " + craftDirectory);
			float startLoadingTime = Time.realtimeSinceStartup;
			string[] craftFiles = fileLocationService.getAllCraftFilesInDirectory(craftDirectory);
			OrganizerCraftModel[] toRet = new OrganizerCraftModel[craftFiles.Length];

			for (int i = 0; i < craftFiles.Length; ++i) {
				toRet[i] = new OrganizerCraftModel(parent, craftFiles[i]);
				toRet[i].isAutosaved = ksp.getAutoSaveCraftName() == Path.GetFileNameWithoutExtension(craftFiles[i]);
				toRet[i].isStock = isStock;

				CraftSettingsDto craftSettings = settingsService.readCraftSettingsForCraftFile(toRet[i].craftFile);
				foreach (string craftTag in craftSettings.tags) {
					parent.addAvailableTag(craftTag);
					toRet[i].addTag(craftTag);
				}
				toRet[i].nameFromSettingsFile = craftSettings.craftName;
				toRet[i].craftSettingsFileIsDirty = false;

				toRet[i].finishCreationMode();
			}
			float endLoadingTime = Time.realtimeSinceStartup;
			COLogger.logDebug("Finished fetching " + craftFiles.Length + " crafts, it took " + (endLoadingTime - startLoadingTime) + "s");
			return toRet;
		}

		public OrganizerCraftModel primaryCraft {
			set {
				if (_primaryCraft != null) {
					_primaryCraft.setSelectedPrimaryInternal(false);
				}
				_primaryCraft = value;
				if (_primaryCraft != null) {
					_primaryCraft.setSelectedPrimaryInternal(true);
				}
			}
			get {
				return _primaryCraft;
			}
		}

		public void renameCraft(OrganizerCraftModel model, string newName) {
			string newFile = fileLocationService.renameCraft(model.craftFile, newName);
			model.setCraftFileInternal(newFile);
			model.craftDto.name = newName;
		}


		internal void deleteCraft(OrganizerCraftModel model) {
			fileLocationService.deleteCraft(model.craftFile);
			availableCrafts.Remove(model);
			clearCaches("craft deleted");
		}

		public void unselectAllCrafts() {
			foreach (OrganizerCraftModel craft in filteredCrafts) {
				craft.isSelected = false;
			}
		}

		private void updateSelectedCrafts(bool selectAll) {
			if (forceUncheckSelectAllFiltered) {
				selectAllFiltered = false;
				forceUncheckSelectAllFiltered = false;
			} else {
				if (selectAllFiltered != selectAll || selectAll) {
					foreach (OrganizerCraftModel craft in filteredCrafts) {
						craft.isSelected = selectAll;
					}
					selectAllFiltered = selectAll;
				}
			}
			cachedSelectedCraftsCount = 0;
			foreach (OrganizerCraftModel craft in filteredCrafts) {
				if (craft.isSelected) {
					cachedSelectedCraftsCount += 1;
				}
			}
		}

		public ICollection<List<OrganizerCraftModel>> alreadyLoadedCrafts {
			get {
				return craftTypeToAvailableCraftsLazy.Values;
			}
		}

		public int selectedCraftsCount {
			get {
				return cachedSelectedCraftsCount;
			}
		}

		public CraftType craftType {
			get {
				return _craftType;
			}

			set {
				if (value != _craftType) {
					_craftType = value;
					clearCaches("New craft type: " + value);
					if (this._primaryCraft != null) {
						this._primaryCraft.setSelectedPrimaryInternal(false);
					}
					this._primaryCraft = null;
				}
			}
		}

		private void clearCaches(string reason) {
			COLogger.logDebug("Clearing caches in OrganizerServiceCraftList, reason: " + reason);
			this.cachedAvailableCrafts = null;
			this.cachedFilteredCrafts = null;
			this.cachedSelectedCraftsCount = 0;
		}
	}
}

