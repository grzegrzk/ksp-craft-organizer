using System.Collections.Generic;

namespace KspCraftOrganizer {

	public class FilterTagGroup : TagGroup<OrganizerTagEntity> {
		
		private OrganizerController parent;

		public FilterTagGroup(OrganizerController parent, string name) : base(name) {
			this.parent = parent;
		}


		public bool isCollapsedInFilterView {
			get {
				return parent.stateManager.isGroupCollapsedInFilters(name);
			}
			set {
				parent.stateManager.setGroupCollapsedInFilters(name, value);
			}
		}


		public bool hasSelectedNoneFilter {
			get {
				return parent.stateManager.isGroupHasSelectedNoneFilter(name);
			}
			set {
				parent.stateManager.setGroupHasSelectedNoneFilter(name, value);
			}
		}
	}

	public class FilterTagsGrouper : TagsGrouper<OrganizerTagEntity, FilterTagGroup> {

		//private Dictionary<string, string> groupsWithSelectedNone = new Dictionary<string, string>();
		private OrganizerController parent;

		public FilterTagsGrouper(OrganizerController parent) : base(t => t.name, s => new FilterTagGroup(parent, s)) {
			this.parent = parent;
		}


		public override void update(ICollection<OrganizerTagEntity> currentTags) {
			base.update(currentTags);
		}

		public bool restGroupCollapsed { 
			get{
				return parent.stateManager.isRestTagsCollapsedInFilter();
			}
			set { 
				parent.stateManager.setRestTagsCollapsedInFilter(value);
			} 
		}

		public ICollection<string> collapsedFilterGroups {
			get {
				List<string> toRet = new List<string>();
				foreach (FilterTagGroup tagGroup in this.groups) {
					if (tagGroup.isCollapsedInFilterView) {
						toRet.Add(tagGroup.name);
					}
				}
				return toRet;
			}
		}

		public void setInitialGroupsWithSelectedNone(ICollection<string> filterGroupsWithSelectedNoneOption) {

			foreach (FilterTagGroup g in groups) {
				g.hasSelectedNoneFilter = filterGroupsWithSelectedNoneOption.Contains(g.name);
			}
		}

		public void setCollapsedGroups(ICollection<string> collapsedGroups) {
			foreach (FilterTagGroup tagGroup in this.groups) {
				tagGroup.isCollapsedInFilterView = collapsedGroups.Contains(tagGroup.name);
			}
		}

		public ICollection<string> groupsWithSelectedNoneOption {
			get {
				List<string> toRet = new List<string>();
				foreach (FilterTagGroup tagGroup in this.groups) {
					if (tagGroup.hasSelectedNoneFilter) {
						toRet.Add(tagGroup.name);
					}
				}
				return toRet;
			}
		}

		public void clearFilters() {
			foreach (FilterTagGroup g in groups) {
				g.hasSelectedNoneFilter = false;
			}
		}

		public void setGroupHasSelectedNoneFilter(string groupName, bool isNoneFilterSelected) {
			if (groupExists(groupName)) {
				if (getGroup(groupName).hasSelectedNoneFilter != isNoneFilterSelected) {
					getGroup(groupName).hasSelectedNoneFilter = isNoneFilterSelected;
					parent.markFilterAsChanged();
				}
			}
		}

		public bool hasGroupSelectedNoneFilter(string groupName) {
			if (groupExists(groupName)) {
				return getGroup(groupName).hasSelectedNoneFilter;
			} else {
				return false;
			}
		}

		public bool canGroupBeCleared(string groupName) {
			bool canBeCleared = false;
			if (groupExists(groupName)) {
				FilterTagGroup tagGroup = getGroup(groupName);
				bool byDefaultNegative = false;
				foreach (TagInGroup<OrganizerTagEntity> tag in tagGroup.tags) {
					if (YesNoTag.isByDefaultNegativeTag(tag.originalTag.name)) {
						canBeCleared = canBeCleared || tag.originalTag.selectedForFiltering;
						byDefaultNegative = true;
					} else if (YesNoTag.isByDefaultPositiveTag(tag.originalTag.name)) {
						canBeCleared = canBeCleared || !tag.originalTag.selectedForFiltering;
					} else {
						if (tag.originalTag.selectedForFiltering) {
							canBeCleared = true;
							break;
						}
					}

				}
				if (byDefaultNegative) {
					canBeCleared = canBeCleared || !hasGroupSelectedNoneFilter(tagGroup.name);
				} else {
					canBeCleared = canBeCleared || hasGroupSelectedNoneFilter(tagGroup.name);
				}
			}
			return canBeCleared;
		}

		public void clearGroup(string groupName) {
			if (groupExists(groupName)) {
				FilterTagGroup tagGroup = getGroup(groupName);
				bool byDefaultNegative = false;
				foreach (TagInGroup<OrganizerTagEntity> tag in tagGroup.tags) {
					if (YesNoTag.isByDefaultNegativeTag(tag.originalTag.name)) {
						tag.originalTag.selectedForFiltering = false;
						byDefaultNegative = true;
					} else if (YesNoTag.isByDefaultPositiveTag(tag.originalTag.name)) {
						tag.originalTag.selectedForFiltering = true;
					} else {

						tag.originalTag.selectedForFiltering = false;
					}

				}
				if (byDefaultNegative) {
					setGroupHasSelectedNoneFilter(groupName, true);
				} else {
					setGroupHasSelectedNoneFilter(groupName, false);
				}
			}
		}

		public bool doesCraftPassFilter(OrganizerCraftEntity craft, out bool shouldBeVisibleByDefault) {
			bool pass = true;
			shouldBeVisibleByDefault = true;
			foreach (TagGroup<OrganizerTagEntity> tagGroup in this.groups) {
				bool anythingSelectedInThisGroup = false;
				bool craftPassesAnythingInThisGroup = false;
				bool craftContainsAnyTagFromThisGroup = false;
				foreach (TagInGroup<OrganizerTagEntity> tag in tagGroup.tags) {
					bool craftHasThisTag = craft.containsTag(tag.originalTag.name);
					craftContainsAnyTagFromThisGroup = craftContainsAnyTagFromThisGroup || craftHasThisTag;
					if (tag.originalTag.selectedForFiltering) {
						anythingSelectedInThisGroup = true;
						craftPassesAnythingInThisGroup = craftPassesAnythingInThisGroup || craftHasThisTag;
					}

					if (YesNoTag.isByDefaultNegativeTag(tag.originalTag.name) && craftHasThisTag) {
						shouldBeVisibleByDefault = false;
					}
					if (YesNoTag.isByDefaultPositiveTag(tag.originalTag.name) && !craftHasThisTag) {
						shouldBeVisibleByDefault = false;
					}

				}
				if (hasGroupSelectedNoneFilter(tagGroup.name)) {
					anythingSelectedInThisGroup = true;
					craftPassesAnythingInThisGroup = craftPassesAnythingInThisGroup || !craftContainsAnyTagFromThisGroup;
				}
				if (anythingSelectedInThisGroup && !craftPassesAnythingInThisGroup) {
					pass = false;
					break;
				}

			}

			foreach (OrganizerTagEntity tag in this.restTags) {
				if (tag.selectedForFiltering) {
					if (!craft.containsTag(tag.name)) {
						pass = false;
					}
				}
			}

			return pass;
		}

	}
}

