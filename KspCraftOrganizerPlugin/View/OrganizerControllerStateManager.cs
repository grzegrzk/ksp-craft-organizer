using System;
using System.Collections.Generic;

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
		
		private SortedList<string, OrganizerControllerPrimaryTagState> primaryTagsSettings = new SortedList<string, OrganizerControllerPrimaryTagState>();
		private SortedList<string, OrganizerControllerPrimaryTagGroupState> primaryGroupsSettings = new SortedList<string, OrganizerControllerPrimaryTagGroupState>();
		private CraftTypeDependendSetting<bool> restTagsCollapsedInFilter;
		private CraftTypeDependendSetting<bool> restTagsCollapsedInManagement;
		private CraftTypeDependendSetting<string> craftNameFilter;

		public GuiStyleOption selectedGuiStyle;

		private SortedList<string, OrganizerControllerPerSaveSettings> perSaveSettings = new SortedList<string, OrganizerControllerPerSaveSettings>();

		private IKspAl ksp = IKspAlProvider.instance;
		private SettingsService settingsService = SettingsService.instance;

		public OrganizerControllerStateManager(OrganizerController parent) {
			this.parent = parent;
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
			getTag(tagName).selectedForFiltering.current = selected;
		}

		public bool isTagSelectedForFiltering(string tagName) {
			return getTag(tagName).selectedForFiltering.current;
		}

		private OrganizerControllerPrimaryTagState getTag(string tagName) {
			if (!primaryTagsSettings.ContainsKey(tagName)) {
				primaryTagsSettings.Add(tagName, new OrganizerControllerPrimaryTagState(this));
			}
			return primaryTagsSettings[tagName];
		}

		public void setGroupCollapsedInFilters(string groupName, bool collapsed) {
			getGroup(groupName).collapsedInFilters.current = collapsed;
		}

		public bool isGroupCollapsedInFilters(string groupName) {
			return getGroup(groupName).collapsedInFilters.current;
		}

		public void setGroupHasSelectedNoneFilter(string groupName, bool collapsed) {
			getGroup(groupName).hasSelectedNoneInFilter.current = collapsed;
		}

		public bool isGroupHasSelectedNoneFilter(string groupName) {
			return getGroup(groupName).hasSelectedNoneInFilter.current;
		}

		public void setGroupCollapsedInManagement(string groupName, bool collapsed) {
			getGroup(groupName).collapsedInManagement.current = collapsed;
		}

		public bool isGroupCollapsedInManagement(string groupName) {
			return getGroup(groupName).collapsedInManagement.current;
		}

		private OrganizerControllerPrimaryTagGroupState getGroup(string groupName) {
			if (!primaryGroupsSettings.ContainsKey(groupName)) {
				primaryGroupsSettings.Add(groupName, new OrganizerControllerPrimaryTagGroupState(this));
			}
			return primaryGroupsSettings[groupName];
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

		public GuiStyleOption getSelectedGuiStyle() {
			return selectedGuiStyle;
		}

		public void setSelectedGuiStyle(GuiStyleOption selectedGuiStyle) {
			this.selectedGuiStyle = selectedGuiStyle;
		}

		public CraftType currentFacility { get { return ksp.getCurrentEditorFacilityType(); } }


		public CraftType selectedCraftType { get { return parent.craftType; } }


		//private void persistFiltersState() {
		//	ProfileFilterSettingsDto filterDto = getFilterDtoFor(craftList.craftType);
		//	filter.assignCurrentFilterSettingsToDto(filterDto);
		//	managementTagsGroups.assignCurrentFilterSettingsToDto(filterDto);
		//	filterDto.restManagementTagsCollapsed = this.restTagsInManagementCollapsed;
		//}

		void readPrimarySaveSettings() {
			ProfileSettingsDto dto = settingsService.readProfileSettings(ksp.getNameOfSaveFolder());//new ProfileSettingsDto ();

			ProfileAllFilterSettingsDto allFilter = dto.allFilter;
			readFilterSettings(allFilter, CraftType.SPH, CraftType.VAB);
			readFilterSettings(allFilter, CraftType.SPH, CraftType.SPH);
			readFilterSettings(allFilter, CraftType.VAB, CraftType.VAB);
			readFilterSettings(allFilter, CraftType.VAB, CraftType.SPH);

			this.selectedGuiStyle = dto.selectedGuiStyle;

			primarySettingsChanged = false;
		}

		void readFilterSettings(ProfileAllFilterSettingsDto dto, CraftType facility, CraftType craftType) {
			ProfileFilterSettingsDto filterDto = dto.getFilterDtoFor(facility, craftType);

			craftNameFilter.setForFacilityAndCraftType(facility, craftType, filterDto.selectedTextFilter);


			foreach (KeyValuePair<string, OrganizerControllerPrimaryTagState> tagState in primaryTagsSettings) {
				tagState.Value.selectedForFiltering.setForFacilityAndCraftType(facility, craftType, false);
			}
			foreach (string selectedTag in filterDto.selectedFilterTags) {
				getTag(selectedTag).selectedForFiltering.setForFacilityAndCraftType(facility, craftType, true);
			}


			foreach (KeyValuePair<string, OrganizerControllerPrimaryTagGroupState> groupState in primaryGroupsSettings) {
				groupState.Value.collapsedInFilters.setForFacilityAndCraftType(facility, craftType, false);
				groupState.Value.collapsedInManagement.setForFacilityAndCraftType(facility, craftType, false);
				groupState.Value.hasSelectedNoneInFilter.setForFacilityAndCraftType(facility, craftType, false);
			}

			foreach (string groupName in filterDto.filterGroupsWithSelectedNoneOption) {
				getGroup(groupName).hasSelectedNoneInFilter.setForFacilityAndCraftType(facility, craftType, true);
			}
			foreach (string groupName in filterDto.collapsedFilterGroups) {
				getGroup(groupName).collapsedInFilters.setForFacilityAndCraftType(facility, craftType, true);
			}
			foreach (string groupName in filterDto.collapsedManagementGroups) {
				getGroup(groupName).collapsedInManagement.setForFacilityAndCraftType(facility, craftType, true);
			}

			restTagsCollapsedInFilter.setForFacilityAndCraftType(facility, craftType, filterDto.restFilterTagsCollapsed);
			restTagsCollapsedInManagement.setForFacilityAndCraftType(facility, craftType, filterDto.restManagementTagsCollapsed);
		}

		public void writeAllDirtySettings(bool doNotWriteTagSettingsToDisk) {
			if (primarySettingsChanged) {
				ProfileSettingsDto dto = settingsService.readProfileSettings(ksp.getNameOfSaveFolder());//new ProfileSettingsDto ();

				//persistFiltersState();

				ProfileAllFilterSettingsDto allFilter = new ProfileAllFilterSettingsDto();
				saveFilterSettings(allFilter, CraftType.SPH, CraftType.VAB);
				saveFilterSettings(allFilter, CraftType.SPH, CraftType.SPH);
				saveFilterSettings(allFilter, CraftType.VAB, CraftType.VAB);
				saveFilterSettings(allFilter, CraftType.VAB, CraftType.SPH);

				dto.allFilter = allFilter;
				dto.selectedGuiStyle = this.selectedGuiStyle;

				//dto.allFilter = allFiltersDto;
				//dto.selectedGuiStyle = _selectedGuiStyle;

				settingsService.writeProfileSettings(ksp.getNameOfSaveFolder(), dto);

				//markProfileSettingsAsNotDirty("Settings were just written to the disk");
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

		void saveFilterSettings(ProfileAllFilterSettingsDto dto, CraftType facility, CraftType craftType) {
			ProfileFilterSettingsDto filterDto = dto.getFilterDtoFor(facility, craftType);

			filterDto.selectedTextFilter = craftNameFilter.getForFacilityAndCraftType(facility, craftType);

			List<string> selectedFilterTags = new List<string>();
			foreach (KeyValuePair<string, OrganizerControllerPrimaryTagState> tagState in primaryTagsSettings) {
				if (tagState.Value.selectedForFiltering.getForFacilityAndCraftType(facility, craftType)) {
					selectedFilterTags.Add(tagState.Key);
				}
			}
			filterDto.selectedFilterTags = selectedFilterTags.ToArray();

			List<string> filterGroupsWithSelectedNoneOption = new List<string>();
			List<string> collapsedFilterGroups = new List<string>();
			List<string> collapsedManagementGroups = new List<string>();
			foreach (KeyValuePair<string, OrganizerControllerPrimaryTagGroupState> groupState in primaryGroupsSettings) {
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
			COLogger.logDebug("Marking primary profile settings as dirty, reason: " + reason);
			this.primarySettingsChanged = true;
		}
		public void markCurrentSaveSettingsAsDirty(string reason) {
			string currentSave = parent.currentSave;
			COLogger.logDebug("Marking settings in '" + currentSave + "' as dirty, reason: " + reason);
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
			if (primaryTagsSettings.ContainsKey(oldName)) {
				if (primaryTagsSettings.ContainsKey(newName)) {
					primaryTagsSettings.Remove(newName);
				}
				primaryTagsSettings.Add(newName, primaryTagsSettings[oldName]);
			}

			availableTagsForCurrentSave.Remove(oldName);
			availableTagsForCurrentSave.Add(newName);

			markCurrentSaveSettingsAsDirty("renamed tag");
			markPrimaryProfileDirty("removed tag");
		}
	}

}

