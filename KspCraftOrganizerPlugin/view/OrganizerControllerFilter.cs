using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace KspCraftOrganizer {

	public class OrganizerControllerFilter {
		
		private SortedList<string, OrganizerTagEntity> _availableTags;
		private string _craftNameFilter;
		private SortedList<string, OrganizerTagEntity> _usedTags = new SortedList<string, OrganizerTagEntity>();
		public FilterTagsGrouper tagsGrouper { get; private set; }
		private OrganizerController parent;


		public OrganizerControllerFilter(OrganizerController parent, ProfileSettingsDto profileSettigngs) {
			this.parent = parent;
			this.tagsGrouper = new FilterTagsGrouper(parent);

			_availableTags = new SortedList<string, OrganizerTagEntity>();
			foreach (string tagName in profileSettigngs.availableTags) {
				addAvailableTag(tagName);
			}

			craftNameFilter = "";
		}


		public void init() {
			//
		}


		public void applyFilterSettings(ProfileFilterSettingsDto dto) {
			foreach (OrganizerTagEntity tag in _availableTags.Values) {
				tag.selectedForFiltering = false;
			}

			foreach (string tagName in dto.selectedFilterTags) {
				if (_availableTags.ContainsKey(tagName)) {
					_availableTags[tagName].selectedForFiltering = true;
				}
			}

			tagsGrouper.setInitialGroupsWithSelectedNone(dto.filterGroupsWithSelectedNoneOption);
			craftNameFilter = dto.selectedTextFilter;
			if (craftNameFilter == null) {
				craftNameFilter = "";
			}
			tagsGrouper.setCollapsedGroups(dto.collapsedFilterGroups);
			tagsGrouper.restGroupCollapsed = dto.restFilterTagsCollapsed;


			COLogger.logDebug("applyFilterSettings, selected tags: " + Globals.join(dto.selectedFilterTags, ", "));
			COLogger.logDebug("applyFilterSettings, groupsWithSelectedNoneOption: " + Globals.join(dto.filterGroupsWithSelectedNoneOption, ", "));
			COLogger.logDebug("applyFilterSettings, collapsed groups: " + Globals.join(dto.collapsedFilterGroups, ", "));
			COLogger.logDebug("applyFilterSettings, rest tags collapsed: " + dto.restFilterTagsCollapsed);
		}


		public bool restTagsCollapsed { get { return tagsGrouper.restGroupCollapsed; } set { tagsGrouper.restGroupCollapsed = value; } }

		public void assignCurrentFilterSettingsToDto(ProfileFilterSettingsDto dto) {
			List<string> selectedTags = new List<string>();
			foreach (OrganizerTagEntity tag in availableTags) {
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
			dto.collapsedFilterGroups = new List<string>(tagsGrouper.collapsedFilterGroups);
			dto.restFilterTagsCollapsed = tagsGrouper.restGroupCollapsed;
			COLogger.logDebug("assignCurrentFilterSettingsToDto, selected tags: " + Globals.join(selectedTags, ", "));
			COLogger.logDebug("assignCurrentFilterSettingsToDto, groupsWithSelectedNoneOption: " + Globals.join(dto.filterGroupsWithSelectedNoneOption, ", "));
			COLogger.logDebug("assignCurrentFilterSettingsToDto, collapsed groups: " + Globals.join(dto.collapsedFilterGroups, ", "));
			COLogger.logDebug("assignCurrentFilterSettingsToDto, rest group collapsed: " + dto.restFilterTagsCollapsed);
		}

		public ICollection<string> groupsWithSelectedNoneOption {
			get {
				return tagsGrouper.groupsWithSelectedNoneOption;
			}
		}

		public bool filterChanged { get; private set; }

		public ICollection<OrganizerTagEntity> availableTags {
			get {
				return new ReadOnlyCollection<OrganizerTagEntity>(new List<OrganizerTagEntity>(_availableTags.Values));
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

		public OrganizerControllerCraftList.CraftFilterPredicate createCraftFilterPredicate() {
			string upperFilter = craftNameFilter.ToUpper();
			return delegate (OrganizerCraftEntity craft, out bool shouldBeVisibleByDefault) {
				return doesCraftPassFilter(upperFilter, craft, out shouldBeVisibleByDefault);
			};
		}

		private bool doesCraftPassFilter(string upperFilter, OrganizerCraftEntity craft, out bool shouldBeVisibleByDefault) {
			shouldBeVisibleByDefault = true;
			bool pass = true;
			pass = pass && (craft.nameToDisplay.ToUpper().Contains(upperFilter) || craftNameFilter == "");
			pass = tagsGrouper.doesCraftPassFilter(craft, out shouldBeVisibleByDefault) && pass;
			return pass;
		}


		public void clearFilters() {
			craftNameFilter = "";
			tagsGrouper.clearFilters();
			foreach (OrganizerTagEntity tag in availableTags) {
				tag.selectedForFiltering = false;
				if (YesNoTag.isByDefaultNegativeTag(tag.name)) {
					tagsGrouper.setGroupHasSelectedNoneFilter(tag.name, true);
				}
				if (YesNoTag.isByDefaultPositiveTag(tag.name)) {
					tag.selectedForFiltering = true;
				}
			}
		}

		public void setGroupHasSelectedNoneFilter(string groupName, bool selectedNoneFilter) {
			tagsGrouper.setGroupHasSelectedNoneFilter(groupName, selectedNoneFilter);
		}

		public void update() {
			foreach (OrganizerTagEntity tag in _availableTags.Values) {
				tag.countOfSelectedCraftsWithThisTag = 0;
			}
			foreach (OrganizerCraftEntity craft in parent.filteredCrafts) {
				if (craft.isSelected) {
					foreach (string tag in craft.tags) {
						++_availableTags[tag].countOfSelectedCraftsWithThisTag;
					}
				}
			}
			foreach (OrganizerTagEntity tag in _availableTags.Values) {
				tag.updateTagState();
			}
			updateUsedTags();
			tagsGrouper.update(usedTags);
		}

		public ICollection<OrganizerTagEntity> usedTags {
			get {
				return new ReadOnlyCollection<OrganizerTagEntity>(_usedTags.Values);
			}
		}

		public void updateUsedTags() {
			_usedTags.Clear();
			foreach (OrganizerCraftEntity craft in parent.availableCrafts) {
				foreach (string tag in craft.tags) {
					if (!_usedTags.ContainsKey(tag)) {
						_usedTags.Add(tag, _availableTags[tag]);
					}
				}
			}

			foreach (OrganizerTagEntity tag in availableTags) {
				if (!_usedTags.ContainsKey(tag.name)) {
					tag.selectedForFiltering = false;
				}
			}
		}

		public OrganizerTagEntity getTag(string tag) {
			return _availableTags[tag];
		}

		public bool doesTagExist(string tag) {
			return _availableTags.ContainsKey(tag);
		}

		public OrganizerTagEntity addAvailableTag(string newTag) {
			if (!_availableTags.ContainsKey(newTag)) {
				_availableTags.Add(newTag, new OrganizerTagEntity(parent, newTag));
				parent.markProfileSettingsAsDirty("New available tag");
			}
			return _availableTags[newTag];
		}

		public void removeTag(string tag) {
			if (_availableTags.ContainsKey(tag)) {
				foreach (OrganizerCraftEntity craft in parent.getCraftsOfType(CraftType.SPH)) {
					craft.removeTag(tag);
				}
				foreach (OrganizerCraftEntity craft in parent.getCraftsOfType(CraftType.VAB)) {
					craft.removeTag(tag);
				}
				_availableTags.Remove(tag);
				parent.markProfileSettingsAsDirty("Tag removed");
			}
		}

		public void renameTag(string oldName, string newName) {
			foreach (OrganizerCraftEntity craft in parent.getCraftsOfType(CraftType.SPH)) {
				if (craft.containsTag(oldName)) {
					craft.addTag(newName);
					craft.removeTag(oldName);
				}
			}
			foreach (OrganizerCraftEntity craft in parent.getCraftsOfType(CraftType.VAB)) {
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
				OrganizerTagEntity newTag = new OrganizerTagEntity(parent, newName);
				newTag.selectedForFiltering = selectForFilterAfterInsertion;
				_availableTags.Add(newName, newTag);
			}
			parent.markProfileSettingsAsDirty("Tag renamed");
		}

	}
}

