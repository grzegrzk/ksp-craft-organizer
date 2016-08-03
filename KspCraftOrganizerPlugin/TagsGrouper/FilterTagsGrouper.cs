using System.Collections.Generic;

namespace KspCraftOrganizer {

	public class FilterTagGroup : TagGroup<OrganizerTagEntity> {

		private bool _collapsedInFilterView;
		private OrganizerController parent;

		public FilterTagGroup(OrganizerController parent, string name) : base(name) {
			this.parent = parent;
		}


		public bool collapsedInFilterView {
			get {
				return _collapsedInFilterView;
			}
			set {
				if (_collapsedInFilterView != value) {
					_collapsedInFilterView = value;
					parent.markProfileSettingsAsDirty("Filter tag group collapsed state changed");
				}
			}
		}

	}

	public class FilterTagsGrouper : TagsGrouper<OrganizerTagEntity, FilterTagGroup> {

		private Dictionary<string, string> groupsWithSelectedNone = new Dictionary<string, string>();
		private OrganizerController parent;

		public FilterTagsGrouper(OrganizerController parent) : base(t => t.name, s => new FilterTagGroup(parent, s)) {
			this.parent = parent;
		}


		public override void update(ICollection<OrganizerTagEntity> currentTags) {
			base.update(currentTags);

			List<string> groupsToRemove = new List<string>();
			foreach (string g in groupsWithSelectedNone.Keys) {
				if (!this.groupExists(g)) {
					groupsToRemove.Add(g);
				}
			}

			foreach (string g in groupsToRemove) {
				groupsWithSelectedNone.Remove(g);
			}
		}

		public bool restGroupCollapsed { get; set; }

		public ICollection<string> collapsedFilterGroups {
			get {
				List<string> toRet = new List<string>();
				foreach (FilterTagGroup tagGroup in this.groups) {
					if (tagGroup.collapsedInFilterView) {
						toRet.Add(tagGroup.name);
					}
				}
				return toRet;
			}
		}

		public void setInitialGroupsWithSelectedNone(ICollection<string> filterGroupsWithSelectedNoneOption) {
			groupsWithSelectedNone.Clear();
			foreach (string groupName in filterGroupsWithSelectedNoneOption) {
				if (!groupsWithSelectedNone.ContainsKey(groupName)) {
					groupsWithSelectedNone.Add(groupName, groupName);
				}
			}
		}

		public void setCollapsedGroups(ICollection<string> collapsedGroups) {
			foreach (FilterTagGroup tagGroup in this.groups) {
				tagGroup.collapsedInFilterView = collapsedGroups.Contains(tagGroup.name);
			}
		}

		public ICollection<string> groupsWithSelectedNoneOption {
			get {
				return groupsWithSelectedNone.Keys;
			}
		}

		public void clearFilters() {
			groupsWithSelectedNone.Clear();
		}

		public void setGroupHasSelectedNoneFilter(string groupName, bool isNoneFilterSelected) {
			if (isNoneFilterSelected) {
				if (!groupsWithSelectedNone.ContainsKey(groupName)) {
					groupsWithSelectedNone.Add(groupName, groupName);
					parent.markFilterAsChanged();
				}
			} else {
				if (groupsWithSelectedNone.ContainsKey(groupName)) {
					groupsWithSelectedNone.Remove(groupName);
					parent.markFilterAsChanged();
				}
			}
		}

		public bool hasGroupSelectedNoneFilter(string groupName) {
			return groupsWithSelectedNone.ContainsKey(groupName);
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

					if (YesNoTag.isByDefaultNegativeTag(tag.originalTag.name) && craft.containsTag(tag.originalTag.name)) {
						shouldBeVisibleByDefault = false;
					}
					if (YesNoTag.isByDefaultPositiveTag(tag.originalTag.name) && !craft.containsTag(tag.originalTag.name)) {
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

