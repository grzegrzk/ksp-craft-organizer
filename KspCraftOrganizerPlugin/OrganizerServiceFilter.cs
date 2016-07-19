using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace KspCraftOrganizer {
	
	public class OrganizerServiceFilter {

		private SortedList<string, OrganizerTagModel> _availableTags;
		private string _craftNameFilter;
		//private bool filterChanged;
		private SortedList<string, OrganizerTagModel> _usedTags = new SortedList<string, OrganizerTagModel>();
		private OrganizerTagGroupsModel groupTags;
		private OrganizerService parent;

		public OrganizerServiceFilter(OrganizerService parent, ProfileSettingsDto profileSettigngs) {
			this.parent = parent;
			groupTags = new OrganizerTagGroupsModel(parent);

			//ProfileSettingsDto profileSettigngs = settingsService.readProfileSettings();
			_availableTags = new SortedList<string, OrganizerTagModel>();
			//_selectedGuiStyle = profileSettigngs.selectedGuiStyle;
			//if (_selectedGuiStyle == null) {
			//	_selectedGuiStyle = GuiStyleOption.Ksp;
			//}
			foreach (string tagName in profileSettigngs.availableTags) {
				addAvailableTag(tagName);
			}
			foreach (string tagName in profileSettigngs.selectedFilterTags) {
				if (!_availableTags.ContainsKey(tagName)) {
					addAvailableTag(tagName);
				}
				_availableTags[tagName].selectedForFiltering = true;
			}
			groupTags.setInitialGroupsWithSelectedNone(profileSettigngs.filterGroupsWithSelectedNoneOption);
			craftNameFilter = profileSettigngs.selectedTextFilter;
		}

		public ICollection<string> groupsWithSelectedNoneOption {
			get {
				return groupTags.groupsWithSelectedNoneOption;
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
			foreach (OrganizerTagModel tag in _availableTags.Values) {
				if (tag.selectedForFiltering) {
					pass = pass && (craft.containsTag(tag.name));
				}
				if (YesNoTag.isByDefaultNegativeTag(tag.name) && craft.containsTag(tag.name)) {
					shouldBeVisibleByDefault = false;
				}
				if (YesNoTag.isByDefaultPositiveTag(tag.name) && !craft.containsTag(tag.name)) {
					shouldBeVisibleByDefault = false;
				}
			}
			pass = pass && groupTags.doesCraftPassFilter(craft);
			return pass;
		}


		public void clearFilters() {
			craftNameFilter = "";
			groupTags.clearFilters();
			foreach (OrganizerTagModel tag in availableTags) {
				tag.selectedForFiltering = false;
				if (YesNoTag.isByDefaultNegativeTag(tag.name)) {
					List<string> singleList = new List<string>();
					singleList.Add(tag.name);
					groupTags.setGroupHasSelectedNoneFilter(tag.name, singleList);
				}
				if (YesNoTag.isByDefaultPositiveTag(tag.name)) {
					tag.selectedForFiltering = true;
				}
			}
		}

		public ICollection<string> beginNewTagGroupsFilterSpecification() {
			return groupTags.beginNewFilterSpecification();
		}

		public void endFilterSpecification() {
			groupTags.endFilterSpecification();
		}

		public void setGroupHasSelectedNoneFilter(string groupName, ICollection<string> tagsInGroup) {
			groupTags.setGroupHasSelectedNoneFilter(groupName, tagsInGroup);
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

