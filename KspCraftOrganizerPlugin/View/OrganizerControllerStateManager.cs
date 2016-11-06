using System;
using System.Collections.Generic;
using KspNalCommon;

namespace KspCraftOrganizer {

	public delegate void OnChanged();

	public class ListenableSetting<T> {
		private T _v = default(T);
		private OnChanged onChanged;

		public ListenableSetting(OnChanged onChanged) {
			this.onChanged = onChanged;
		}

		public T v {
			get {
				return _v;
			}
			set {
				if (!EqualityComparer<T>.Default.Equals(_v, value)) {
					_v = value;
					onChanged();
				}
			}
		}
	}

	public class CraftTypeDependendSetting<T> {
		
		private OrganizerControllerStateManager parent;

		public ListenableSetting<T> vabInVab;
		public ListenableSetting<T> vabInSph;
		public ListenableSetting<T> sphInVab;
		public ListenableSetting<T> sphInSph;

		public CraftTypeDependendSetting(OrganizerControllerStateManager parent, OnChanged onChanged){
			this.parent = parent;
			vabInVab = new ListenableSetting<T>(onChanged);
			vabInSph = new ListenableSetting<T>(onChanged);
			sphInVab = new ListenableSetting<T>(onChanged);
			sphInSph = new ListenableSetting<T>(onChanged);
		}

		public T getForFacilityAndCraftType(CraftType currentFacility, CraftType selectedCraftType) {
			if (currentFacility == CraftType.VAB) {
				if (selectedCraftType == CraftType.VAB) {
					return this.vabInVab.v;
				} else {
					return this.sphInVab.v;
				}
			} else {
				if (selectedCraftType == CraftType.VAB) {
					return this.vabInSph.v;
				} else {
					return this.sphInSph.v;
				}
			}
		}

		public void setForFacilityAndCraftType(CraftType currentFacility, CraftType selectedCraftType, T value) {
			if (currentFacility == CraftType.VAB) {
				if (selectedCraftType == CraftType.VAB) {
					this.vabInVab.v = value;
				} else {
					this.sphInVab.v = value;
				}
			} else {
				if (selectedCraftType == CraftType.VAB) {
					this.vabInSph.v = value;
				} else {
					this.sphInSph.v = value;
				}
			}
		}

		public T current {
			get {
				CraftType currentFacility = parent.currentFacility;
				CraftType selectedCraftType = parent.selectedCraftType;
				return getForFacilityAndCraftType(currentFacility, selectedCraftType);
			}
			set {
				CraftType currentFacility = parent.currentFacility;
				CraftType selectedCraftType = parent.selectedCraftType;
				setForFacilityAndCraftType(currentFacility, selectedCraftType, value);
			}
		}
	}

	public class OrganizerControllerPrimaryTagState {
		public string tagName;

		public CraftTypeDependendSetting<bool> selectedForFiltering;

		public OrganizerControllerPrimaryTagState(OrganizerControllerStateManager parent) {
			selectedForFiltering = new CraftTypeDependendSetting<bool>(parent, ()=>parent.markPrimaryProfileDirty("Tag changed selected state"));
		}
	}

	public class OrganizerControllerPrimaryTagGroupState {
		public string groupName;

		public CraftTypeDependendSetting<bool> collapsedInFilters;
		public CraftTypeDependendSetting<bool> collapsedInManagement;
		public CraftTypeDependendSetting<bool> hasSelectedNoneInFilter;

		public OrganizerControllerPrimaryTagGroupState(OrganizerControllerStateManager parent) {
			collapsedInFilters = new CraftTypeDependendSetting<bool>(parent, ()=>parent.markPrimaryProfileDirty("group's collapsed state in filter changed"));
			collapsedInManagement = new CraftTypeDependendSetting<bool>(parent, () => parent.markPrimaryProfileDirty("group's collapsed state in management changed"));
			hasSelectedNoneInFilter = new CraftTypeDependendSetting<bool>(parent, () => parent.markPrimaryProfileDirty("group's 'none' option changed"));
		}
	}

	public class OrganizerControllerPerSaveSettings {
		public string saveName;
		public bool changed;
		public List<string> availableTags;
	}

	public class OrganizerControllerStateManager {

		private OrganizerController parent;

		private bool primarySettingsChanged;
		
		private SortedList<string, OrganizerControllerPrimaryTagState> primaryTagsState = new SortedList<string, OrganizerControllerPrimaryTagState>();
		private SortedList<string, OrganizerControllerPrimaryTagGroupState> primaryGroupsState = new SortedList<string, OrganizerControllerPrimaryTagGroupState>();
		private CraftTypeDependendSetting<bool> restTagsCollapsedInFilter;
		private CraftTypeDependendSetting<bool> restTagsCollapsedInManagement;
		private CraftTypeDependendSetting<string> craftNameFilter;

		private ListenableSetting<GuiStyleOption> _selectedGuiStyle;

		private SortedList<string, OrganizerControllerPerSaveSettings> perSaveSettings = new SortedList<string, OrganizerControllerPerSaveSettings>();

		private List<ICraftSortFunction> craftSortingFunctions = new List<ICraftSortFunction>();

		private IKspAl ksp = IKspAlProvider.instance;
		private SettingsService settingsService = SettingsService.instance;
		private bool lockMarkingPrimarySettingsAsChanged = false;

		public OrganizerControllerStateManager(OrganizerController parent) {
			this.parent = parent;
			_selectedGuiStyle = new ListenableSetting<GuiStyleOption>(() => markPrimaryProfileDirty("Gui style changed"));
			restTagsCollapsedInFilter = new CraftTypeDependendSetting<bool>(this, ()=>markPrimaryProfileDirty("rest tags collapsed state changed in filters"));
			restTagsCollapsedInManagement = new CraftTypeDependendSetting<bool>(this, ()=>markPrimaryProfileDirty("rest tags collapsed state changed in management"));
			craftNameFilter = new CraftTypeDependendSetting<string>(this, ()=>markPrimaryProfileDirty("craft name filter changed"));

			readPrimarySaveSettings();
		}

		public List<string> availableTagsForCurrentSave {
			get {
				return perSaveSettingsFor(parent.currentSave).availableTags;
			}
		}

		public GuiStyleOption selectedGuiStyle {
			get {
				return _selectedGuiStyle.v;
			}
			set {
				_selectedGuiStyle.v = value;
			}
		}

		public OrganizerControllerPerSaveSettings perSaveSettingsFor(string save) {
			if (!perSaveSettings.ContainsKey(save)) {
				OrganizerControllerPerSaveSettings v = new OrganizerControllerPerSaveSettings();
				v.changed = false;
				v.saveName = save;
				v.availableTags = new List<string>(settingsService.readProfileSettings(save).availableTags);
				perSaveSettings.Add(save, v);
			}
			return perSaveSettings[save];
		}

		public void setTagSelectedForFiltering(string tagName, bool selected) {
			getPrimaryTagState(tagName).selectedForFiltering.current = selected;
		}

		public bool isTagSelectedForFiltering(string tagName) {
			return getPrimaryTagState(tagName).selectedForFiltering.current;
		}

		private OrganizerControllerPrimaryTagState getPrimaryTagState(string tagName) {
			if (!primaryTagsState.ContainsKey(tagName)) {
				primaryTagsState.Add(tagName, new OrganizerControllerPrimaryTagState(this));
			}
			return primaryTagsState[tagName];
		}

		public void setGroupCollapsedInFilters(string groupName, bool collapsed) {
			getPrimaryGroupState(groupName).collapsedInFilters.current = collapsed;
		}

		public bool isGroupCollapsedInFilters(string groupName) {
			return getPrimaryGroupState(groupName).collapsedInFilters.current;
		}

		public void setGroupHasSelectedNoneFilter(string groupName, bool collapsed) {
			getPrimaryGroupState(groupName).hasSelectedNoneInFilter.current = collapsed;
		}

		public bool isGroupHasSelectedNoneFilter(string groupName) {
			return getPrimaryGroupState(groupName).hasSelectedNoneInFilter.current;
		}

		public void setGroupCollapsedInManagement(string groupName, bool collapsed) {
			getPrimaryGroupState(groupName).collapsedInManagement.current = collapsed;
		}

		public bool isGroupCollapsedInManagement(string groupName) {
			return getPrimaryGroupState(groupName).collapsedInManagement.current;
		}

		private OrganizerControllerPrimaryTagGroupState getPrimaryGroupState(string groupName) {
			if (!primaryGroupsState.ContainsKey(groupName)) {
				primaryGroupsState.Add(groupName, new OrganizerControllerPrimaryTagGroupState(this));
			}
			return primaryGroupsState[groupName];
		}

		public bool isRestTagsCollapsedInFilter() {
			return restTagsCollapsedInFilter.current;
		}

		public void setRestTagsCollapsedInFilter(bool collapsed) {
			restTagsCollapsedInFilter.current = collapsed;
		}

		public bool isRestTagsCollapsedInManagement() {
			return restTagsCollapsedInManagement.current;
		}

		public void setRestTagsCollapsedInManagement(bool collapsed) {
			restTagsCollapsedInManagement.current = collapsed;
		}


		public string getCraftNameFilter() {
			return craftNameFilter.current;
		}

		public void setCraftNameFilter(string craftNameFilter) {
			this.craftNameFilter.current = craftNameFilter;
		}

		public CraftType currentFacility { get { return ksp.getCurrentEditorFacilityType(); } }


		public CraftType selectedCraftType { get { return parent.craftType; } }


		void readPrimarySaveSettings() {
			lockMarkingPrimarySettingsAsChanged = true;
			try {
				ProfileSettingsDto dto = settingsService.readProfileSettings(ksp.getNameOfSaveFolder());

				this.craftSortingFunctions = readCraftSortingfunctions(dto.craftSorting);

				ProfileAllFilterSettingsDto allFilter = dto.allFilter;
				readFilterSettings(allFilter, CraftType.SPH, CraftType.VAB);
				readFilterSettings(allFilter, CraftType.SPH, CraftType.SPH);
				readFilterSettings(allFilter, CraftType.VAB, CraftType.VAB);
				readFilterSettings(allFilter, CraftType.VAB, CraftType.SPH);

				this.selectedGuiStyle = dto.selectedGuiStyle;
			} finally {
				lockMarkingPrimarySettingsAsChanged = false;
			}

			primarySettingsChanged = false;
		}

		List<ICraftSortFunction> readCraftSortingfunctions(ICollection<CraftSortingEntry> craftSorting) {
			List<ICraftSortFunction> toRet = new List<ICraftSortFunction>();
			foreach (CraftSortingEntry entry in craftSorting) {
				toRet.Add(CraftSortFunctionFactory.createFunction(entry));
			}
			return toRet;
		}

		void readFilterSettings(ProfileAllFilterSettingsDto dto, CraftType facility, CraftType craftType) {
			ProfileFilterSettingsDto filterDto = dto.getFilterDtoFor(facility, craftType);

			craftNameFilter.setForFacilityAndCraftType(facility, craftType, filterDto.selectedTextFilter);


			foreach (KeyValuePair<string, OrganizerControllerPrimaryTagState> tagState in primaryTagsState) {
				tagState.Value.selectedForFiltering.setForFacilityAndCraftType(facility, craftType, false);
			}
			foreach (string selectedTag in filterDto.selectedFilterTags) {
				getPrimaryTagState(selectedTag).selectedForFiltering.setForFacilityAndCraftType(facility, craftType, true);
			}


			foreach (KeyValuePair<string, OrganizerControllerPrimaryTagGroupState> groupState in primaryGroupsState) {
				groupState.Value.collapsedInFilters.setForFacilityAndCraftType(facility, craftType, false);
				groupState.Value.collapsedInManagement.setForFacilityAndCraftType(facility, craftType, false);
				groupState.Value.hasSelectedNoneInFilter.setForFacilityAndCraftType(facility, craftType, false);
			}

			foreach (string groupName in filterDto.filterGroupsWithSelectedNoneOption) {
				getPrimaryGroupState(groupName).hasSelectedNoneInFilter.setForFacilityAndCraftType(facility, craftType, true);
			}
			foreach (string groupName in filterDto.collapsedFilterGroups) {
				getPrimaryGroupState(groupName).collapsedInFilters.setForFacilityAndCraftType(facility, craftType, true);
			}
			foreach (string groupName in filterDto.collapsedManagementGroups) {
				getPrimaryGroupState(groupName).collapsedInManagement.setForFacilityAndCraftType(facility, craftType, true);
			}

			restTagsCollapsedInFilter.setForFacilityAndCraftType(facility, craftType, filterDto.restFilterTagsCollapsed);
			restTagsCollapsedInManagement.setForFacilityAndCraftType(facility, craftType, filterDto.restManagementTagsCollapsed);
		}

		public void writeAllDirtySettings(bool doNotWriteTagSettingsToDisk) {
			if (primarySettingsChanged) {
				ProfileSettingsDto dto = settingsService.readProfileSettings(ksp.getNameOfSaveFolder());//new ProfileSettingsDto ();

				ProfileAllFilterSettingsDto allFilter = new ProfileAllFilterSettingsDto();
				saveFilterSettings(allFilter, CraftType.SPH, CraftType.VAB);
				saveFilterSettings(allFilter, CraftType.SPH, CraftType.SPH);
				saveFilterSettings(allFilter, CraftType.VAB, CraftType.VAB);
				saveFilterSettings(allFilter, CraftType.VAB, CraftType.SPH);

				dto.allFilter = allFilter;
				dto.selectedGuiStyle = this.selectedGuiStyle;
				dto.craftSorting = createCraftSortingSettings();

				settingsService.writeProfileSettings(ksp.getNameOfSaveFolder(), dto);
			}

			if (!doNotWriteTagSettingsToDisk) {
				foreach (KeyValuePair<string, OrganizerControllerPerSaveSettings> saveAndState in perSaveSettings) {
					if (saveAndState.Value.changed) {
						ProfileSettingsDto dto = settingsService.readProfileSettings(saveAndState.Key);
						dto.availableTags = saveAndState.Value.availableTags;
						settingsService.writeProfileSettings(saveAndState.Key, dto);
					}
				}
				foreach (List<OrganizerCraftEntity> crafts in parent.alreadyLoadedCrafts) {
						foreach (OrganizerCraftEntity craft in crafts) {
							if (craft.craftSettingsFileIsDirty) {
								CraftSettingsDto dto = new CraftSettingsDto();
								dto.tags = craft.tags;
								dto.craftName = craft.name;
								settingsService.writeCraftSettingsForCraftFile(craft.craftFile, dto);

								craft.craftSettingsFileIsDirty = false;
							}
						}
					}
				}
		}

		ICollection<CraftSortingEntry> createCraftSortingSettings() {
			List<CraftSortingEntry> toRet = new List<CraftSortingEntry>();
			foreach (ICraftSortFunction f in this.craftSortingFunctions) {
				CraftSortingEntry craftSortingEntry = new CraftSortingEntry();
				craftSortingEntry.sortingId = f.functionTypeId;
				craftSortingEntry.sortingData = f.functionData;
				craftSortingEntry.isReversed = f.isReversed;
				toRet.Add(craftSortingEntry);
			}
			return toRet;
		}

		void saveFilterSettings(ProfileAllFilterSettingsDto dto, CraftType facility, CraftType craftType) {
			ProfileFilterSettingsDto filterDto = dto.getFilterDtoFor(facility, craftType);

			filterDto.selectedTextFilter = craftNameFilter.getForFacilityAndCraftType(facility, craftType);

			List<string> selectedFilterTags = new List<string>();
			foreach (KeyValuePair<string, OrganizerControllerPrimaryTagState> tagState in primaryTagsState) {
				if (tagState.Value.selectedForFiltering.getForFacilityAndCraftType(facility, craftType)) {
					selectedFilterTags.Add(tagState.Key);
				}
			}
			filterDto.selectedFilterTags = selectedFilterTags.ToArray();

			List<string> filterGroupsWithSelectedNoneOption = new List<string>();
			List<string> collapsedFilterGroups = new List<string>();
			List<string> collapsedManagementGroups = new List<string>();
			foreach (KeyValuePair<string, OrganizerControllerPrimaryTagGroupState> groupState in primaryGroupsState) {
				if (groupState.Value.hasSelectedNoneInFilter.getForFacilityAndCraftType(facility, craftType)) {
					filterGroupsWithSelectedNoneOption.Add(groupState.Key);
				}
				if (groupState.Value.collapsedInFilters.getForFacilityAndCraftType(facility, craftType)) {
					collapsedFilterGroups.Add(groupState.Key);
				}
				if (groupState.Value.collapsedInManagement.getForFacilityAndCraftType(facility, craftType)) {
					collapsedManagementGroups.Add(groupState.Key);
				}
			}
			filterDto.filterGroupsWithSelectedNoneOption = filterGroupsWithSelectedNoneOption;
			filterDto.collapsedFilterGroups = collapsedFilterGroups;
			filterDto.collapsedManagementGroups = collapsedManagementGroups;

			filterDto.restFilterTagsCollapsed = restTagsCollapsedInFilter.getForFacilityAndCraftType(facility, craftType);
			filterDto.restManagementTagsCollapsed = restTagsCollapsedInManagement.getForFacilityAndCraftType(facility, craftType);
		}

		public void markPrimaryProfileDirty(string reason) {
			if (!lockMarkingPrimarySettingsAsChanged) {
				PluginLogger.logDebug("Marking primary profile settings as dirty, reason: " + reason);
				this.primarySettingsChanged = true;
			}
		}

		public void markCurrentSaveSettingsAsDirty(string reason) {
			string currentSave = parent.currentSave;
			PluginLogger.logDebug("Marking settings in '" + currentSave + "' as dirty, reason: " + reason);
			perSaveSettingsFor(currentSave).changed = true;
		}

		public void addAvailableTag(string newTag) {
			if (!availableTagsForCurrentSave.Contains(newTag)) {
				availableTagsForCurrentSave.Add(newTag);
				markCurrentSaveSettingsAsDirty("added new tag");
			}
		}

		public void removeTag(string tag) {
			availableTagsForCurrentSave.Remove(tag);
			markCurrentSaveSettingsAsDirty("removed tag");
		}

		public void renameTag(string oldName, string newName) {
			if (primaryTagsState.ContainsKey(oldName)) {
				if (primaryTagsState.ContainsKey(newName)) {
					primaryTagsState.Remove(newName);
				}
				primaryTagsState.Add(newName, primaryTagsState[oldName]);
			}

			availableTagsForCurrentSave.Remove(oldName);
			availableTagsForCurrentSave.Add(newName);

			markCurrentSaveSettingsAsDirty("renamed tag");
			markPrimaryProfileDirty("removed tag");
		}

		public void addCraftSortingFunction(ICraftSortFunction function) {
			craftSortingFunctions.Add(function);
			if (craftSortingFunctions.Count > 10) {
				craftSortingFunctions.RemoveAt(0);
			}
			markPrimaryProfileDirty("added craft sorting function");
		}

		public ICraftSortFunction getLastSortFunction() {
			return craftSortingFunctions.Count == 0 ? null : craftSortingFunctions[craftSortingFunctions.Count - 1];
		}

		public void removeLastSortingFunction() {
			if (craftSortingFunctions.Count > 0) {
				craftSortingFunctions.RemoveAt(craftSortingFunctions.Count - 1);
			}
			markPrimaryProfileDirty("removed craft sorting function");
		}

		public IEnumerable<ICraftSortFunction> getCraftSortFunctions() {
			return craftSortingFunctions;
		}
	}

}

