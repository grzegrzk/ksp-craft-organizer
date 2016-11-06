using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace KspCraftOrganizer {

	public class OrganizerControllerFilter {
		
		private SortedList<string, OrganizerTagEntity> _availableTags;
		private SortedList<string, OrganizerTagEntity> _usedTags = new SortedList<string, OrganizerTagEntity>();
		public FilterTagsGrouper usedTagsGrouper { get; private set; }
		private OrganizerController parent;
		bool availableTagsCreated = false;

		public OrganizerControllerFilter(OrganizerController parent) {
			this.parent = parent;
			this.usedTagsGrouper = new FilterTagsGrouper(parent);

			_availableTags = new SortedList<string, OrganizerTagEntity>();

			craftNameFilter = parent.stateManager.getCraftNameFilter();
		}

		public void recreateAvailableTags() {
			_availableTags.Clear();
			foreach (string tagName in parent.stateManager.availableTagsForCurrentSave) {
				addAvailableTag(tagName);
			}

			foreach (OrganizerCraftEntity craft in parent.availableCrafts) {
				foreach (string tag in craft.tags) {
					addAvailableTag(tag);
				}
			}
			parent.refreshDefaultTagsToAdd();
			availableTagsCreated = true;
		}

		public void init() {
			//
		}

		public bool restTagsCollapsed { get { return usedTagsGrouper.restGroupCollapsed; } set { usedTagsGrouper.restGroupCollapsed = value; } }

		public ICollection<string> groupsWithSelectedNoneOption {
			get {
				return usedTagsGrouper.groupsWithSelectedNoneOption;
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
				return parent.stateManager.getCraftNameFilter();
			}
			set {
				if (parent.stateManager.getCraftNameFilter() != value) {
					parent.stateManager.setCraftNameFilter(value);
					markFilterAsChanged();
				}
			}
		}

		public void markFilterAsChanged() {
			filterChanged = true;
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
			pass = usedTagsGrouper.doesCraftPassFilter(craft, out shouldBeVisibleByDefault) && pass;
			return pass;
		}


		public void clearFilters() {
			craftNameFilter = "";
			usedTagsGrouper.clearFilters();
			foreach (OrganizerTagEntity tag in availableTags) {
				tag.selectedForFiltering = false;
				if (YesNoTag.isByDefaultNegativeTag(tag.name)) {
					usedTagsGrouper.setGroupHasSelectedNoneFilter(tag.name, true);
				}
				if (YesNoTag.isByDefaultPositiveTag(tag.name)) {
					tag.selectedForFiltering = true;
				}
			}
		}

		public void setGroupHasSelectedNoneFilter(string groupName, bool selectedNoneFilter) {
			usedTagsGrouper.setGroupHasSelectedNoneFilter(groupName, selectedNoneFilter);
		}

		public void update() {
			if (!availableTagsCreated) {
				recreateAvailableTags();
			}
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
			usedTagsGrouper.update(usedTags);
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
				parent.stateManager.addAvailableTag(newTag);
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
				parent.stateManager.removeTag(tag);
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
			parent.stateManager.renameTag(oldName, newName);
		}

	}
}

