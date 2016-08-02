using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace KspCraftOrganizer {

	public class OrganizerServiceFilter {

		private IKspAl ksp = IKspAlProvider.instance;
		private SortedList<string, OrganizerTagModel> _availableTags;
		private string _craftNameFilter;
		private SortedList<string, OrganizerTagModel> _usedTags = new SortedList<string, OrganizerTagModel>();
		public OrganizerServiceFilterGroupsOfTagModel groupsModel { get; private set; }
		private OrganizerService parent;
		private ProfileAllFilterSettingsDto allFiltersDto;
		private bool afterSettingsChanged = true;


		public OrganizerServiceFilter(OrganizerService parent, ProfileSettingsDto profileSettigngs) {
			this.parent = parent;
			this.allFiltersDto = profileSettigngs.allFilter;

			groupsModel = new OrganizerServiceFilterGroupsOfTagModel(parent);

			_availableTags = new SortedList<string, OrganizerTagModel>();
			foreach (string tagName in profileSettigngs.availableTags) {
				addAvailableTag(tagName);
			}

			craftNameFilter = "";
		}

		public ProfileAllFilterSettingsDto getFilterDto(){
			assignCurrentFilterSettingsToDto(getFilterDtoFor(parent.craftType));
			return allFiltersDto;
		}

		public void init() {
			applyFilterSettings(getFilterDtoFor(parent.craftType));
		}


		private void applyFilterSettings(ProfileFilterSettingsDto dto) {
			foreach (OrganizerTagModel tag in _availableTags.Values) {
				tag.selectedForFiltering = false;
			}

			foreach (string tagName in dto.selectedFilterTags) {
				if (_availableTags.ContainsKey(tagName)) {
					_availableTags[tagName].selectedForFiltering = true;
				}
			}

			groupsModel.setInitialGroupsWithSelectedNone(dto.filterGroupsWithSelectedNoneOption);
			craftNameFilter = dto.selectedTextFilter;
			if (craftNameFilter == null) {
				craftNameFilter = "";
			}
			groupsModel.setCollapsedGroups(dto.collapsedFilterGroups);


			COLogger.logDebug("applyFilterSettings, selected tags: " + Globals.join(dto.selectedFilterTags, ", "));
			COLogger.logDebug("applyFilterSettings, groupsWithSelectedNoneOption: " + Globals.join(dto.filterGroupsWithSelectedNoneOption, ", "));
			COLogger.logDebug("applyFilterSettings, collapsed groups: " + Globals.join(dto.collapsedFilterGroups, ", "));
		}

		public void onCraftTypeChanged(CraftType oldCraftType, CraftType newCraftType) {
			COLogger.logDebug("filter.onCraftTypeChanged, old: " + oldCraftType + ", new : " + newCraftType);
			assignCurrentFilterSettingsToDto(getFilterDtoFor(oldCraftType));
			applyFilterSettings(getFilterDtoFor(newCraftType));
			afterSettingsChanged = true;
			COLogger.logDebug("filter.onCraftTypeChanged -- end");
		}

		private ProfileFilterSettingsDto getFilterDtoFor(CraftType craftType) {
			if (ksp.getCurrentEditorFacilityType() == CraftType.VAB) {
				if (craftType == CraftType.VAB) {
					return allFiltersDto.filterVabInVab;
				} else {
					return allFiltersDto.filterSphInVab;
				}
			} else {
				if (craftType == CraftType.VAB) {
					return allFiltersDto.filterVabInSph;
				} else {
					return allFiltersDto.filterSphInSph;
				}
			}
		}

		private void assignCurrentFilterSettingsToDto(ProfileFilterSettingsDto dto) {
			List<string> selectedTags = new List<string>();
			foreach (OrganizerTagModel tag in availableTags) {
				if (tag.selectedForFiltering) {
					selectedTags.Add(tag.name);
				}
			}
			dto.filterGroupsWithSelectedNoneOption = new List<string>(groupsWithSelectedNoneOption);
			dto.selectedFilterTags = selectedTags.ToArray();
			dto.selectedTextFilter = craftNameFilter;
			if (dto.selectedTextFilter == null) {
				dto.selectedTextFilter = "";
			}
			dto.collapsedFilterGroups = new List<string>(groupsModel.collapsedFilterGroups);
			COLogger.logDebug("assignCurrentFilterSettingsToDto, selected tags: " + Globals.join(selectedTags, ", "));
			COLogger.logDebug("assignCurrentFilterSettingsToDto, groupsWithSelectedNoneOption: " + Globals.join(dto.filterGroupsWithSelectedNoneOption, ", "));
			COLogger.logDebug("assignCurrentFilterSettingsToDto, collapsed groups: " + Globals.join(dto.collapsedFilterGroups, ", "));
		}

		public ICollection<string> groupsWithSelectedNoneOption {
			get {
				return groupsModel.groupsWithSelectedNoneOption;
			}
		}

		public bool filterChanged { get; private set; }

		public ICollection<OrganizerTagModel> availableTags {
			get {
				return new ReadOnlyCollection<OrganizerTagModel>(new List<OrganizerTagModel>(_availableTags.Values));
			}
		}

		public ICollection<string> availableTagsNames {
			get {
				return _availableTags.Keys;
			}
		}

		public string craftNameFilter {
			get {
				return _craftNameFilter;
			}
			set {
				if (_craftNameFilter != value) {
					_craftNameFilter = value;
					markFilterAsChanged();
				}
			}
		}

		public void markFilterAsChanged() {
			filterChanged = true;
			parent.markProfileSettingsAsDirty("Filter changed");
		}


		public void markFilterAsUpToDate() {
			filterChanged = false;
		}

		public OrganizerServiceCraftList.CraftFilterPredicate createCraftFilterPredicate() {
			string upperFilter = craftNameFilter.ToUpper();
			return delegate (OrganizerCraftModel craft, out bool shouldBeVisibleByDefault) {
				return doesCraftPassFilter(upperFilter, craft, out shouldBeVisibleByDefault);
			};
		}

		private bool doesCraftPassFilter(string upperFilter, OrganizerCraftModel craft, out bool shouldBeVisibleByDefault) {
			shouldBeVisibleByDefault = true;
			bool pass = true;
			pass = pass && (craft.nameToDisplay.ToUpper().Contains(upperFilter) || craftNameFilter == "");
			pass = groupsModel.doesCraftPassFilter(craft, out shouldBeVisibleByDefault) && pass;
			return pass;
		}


		public void clearFilters() {
			craftNameFilter = "";
			groupsModel.clearFilters();
			foreach (OrganizerTagModel tag in availableTags) {
				tag.selectedForFiltering = false;
				if (YesNoTag.isByDefaultNegativeTag(tag.name)) {
					groupsModel.setGroupHasSelectedNoneFilter(tag.name, true);
				}
				if (YesNoTag.isByDefaultPositiveTag(tag.name)) {
					tag.selectedForFiltering = true;
				}
			}
		}

		public void setGroupHasSelectedNoneFilter(string groupName, bool selectedNoneFilter) {
			groupsModel.setGroupHasSelectedNoneFilter(groupName, selectedNoneFilter);
		}

		public void update() {
			foreach (OrganizerTagModel tag in _availableTags.Values) {
				tag.countOfSelectedCraftsWithThisTag = 0;
			}
			foreach (OrganizerCraftModel craft in parent.filteredCrafts) {
				if (craft.isSelected) {
					foreach (string tag in craft.tags) {
						++_availableTags[tag].countOfSelectedCraftsWithThisTag;
					}
				}
			}
			foreach (OrganizerTagModel tag in _availableTags.Values) {
				tag.updateTagState();
			}
			updateUsedTags();
			groupsModel.update();
			if (afterSettingsChanged) {
				applyFilterSettings(getFilterDtoFor(parent.craftType));
				afterSettingsChanged = false;
			}
		}

		public ICollection<OrganizerTagModel> usedTags {
			get {
				return new ReadOnlyCollection<OrganizerTagModel>(_usedTags.Values);
			}
		}

		public void updateUsedTags() {
			_usedTags.Clear();
			foreach (OrganizerCraftModel craft in parent.availableCrafts) {
				foreach (string tag in craft.tags) {
					if (!_usedTags.ContainsKey(tag)) {
						_usedTags.Add(tag, _availableTags[tag]);
					}
				}
			}

			foreach (OrganizerTagModel tag in availableTags) {
				if (!_usedTags.ContainsKey(tag.name)) {
					tag.selectedForFiltering = false;
				}
			}
		}

		public OrganizerTagModel getTag(string tag) {
			return _availableTags[tag];
		}

		public bool doesTagExist(string tag) {
			return _availableTags.ContainsKey(tag);
		}

		public OrganizerTagModel addAvailableTag(string newTag) {
			if (!_availableTags.ContainsKey(newTag)) {
				_availableTags.Add(newTag, new OrganizerTagModel(parent, newTag));
				parent.markProfileSettingsAsDirty("New available tag");
			}
			return _availableTags[newTag];
		}

		public void removeTag(string tag) {
			if (_availableTags.ContainsKey(tag)) {
				foreach (OrganizerCraftModel craft in parent.getCraftsOfType(CraftType.SPH)) {
					craft.removeTag(tag);
				}
				foreach (OrganizerCraftModel craft in parent.getCraftsOfType(CraftType.VAB)) {
					craft.removeTag(tag);
				}
				_availableTags.Remove(tag);
				parent.markProfileSettingsAsDirty("Tag removed");
			}
		}

		public void renameTag(string oldName, string newName) {
			foreach (OrganizerCraftModel craft in parent.getCraftsOfType(CraftType.SPH)) {
				if (craft.containsTag(oldName)) {
					craft.addTag(newName);
					craft.removeTag(oldName);
				}
			}
			foreach (OrganizerCraftModel craft in parent.getCraftsOfType(CraftType.VAB)) {
				if (craft.containsTag(oldName)) {
					craft.addTag(newName);
					craft.removeTag(oldName);
				}
			}
			bool selectForFilterAfterInsertion = false;
			if (_availableTags.ContainsKey(oldName)) {
				selectForFilterAfterInsertion = _availableTags[oldName].selectedForFiltering;
				_availableTags.Remove(oldName);
			}
			if (!_availableTags.ContainsKey(newName)) {
				OrganizerTagModel newTag = new OrganizerTagModel(parent, newName);
				newTag.selectedForFiltering = selectForFilterAfterInsertion;
				_availableTags.Add(newName, newTag);
			}
			parent.markProfileSettingsAsDirty("Tag renamed");
		}

	}
}

