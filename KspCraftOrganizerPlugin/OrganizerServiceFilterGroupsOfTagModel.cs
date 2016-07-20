using System;
using System.Collections.Generic;

namespace KspCraftOrganizer {

	public class OrganizerServiceFilterGroupsOfTagModel {

		private OrganizerService parent;
		private TagsGrouper<OrganizerTagModel> tagsGrouper;
		private Dictionary<string, string> groupsWithSelectedNone = new Dictionary<string, string>();

		public OrganizerServiceFilterGroupsOfTagModel(OrganizerService parent) {
			this.parent = parent;

		}

		public void update() {
			ICollection<OrganizerTagModel> usedTags = parent.usedTags;
			this.tagsGrouper = new TagsGrouper<OrganizerTagModel>(usedTags, t => t.name);
		}

		public ICollection<TagGroup<OrganizerTagModel>> groups {
			get {
				return this.tagsGrouper.groups;
			}
		}

		public ICollection<OrganizerTagModel> restTags {
			get {
				return this.tagsGrouper.restTags;
			}
		}

		public void setInitialGroupsWithSelectedNone(ICollection<string> filterGroupsWithSelectedNoneOption) {
			foreach (string groupName in filterGroupsWithSelectedNoneOption) {
				if (!groupsWithSelectedNone.ContainsKey(groupName)) {
					groupsWithSelectedNone.Add(groupName, groupName);
				}
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

		public bool doesCraftPassFilter(OrganizerCraftModel craft, out bool shouldBeVisibleByDefault) {
			if (tagsGrouper == null) {
				shouldBeVisibleByDefault = false;
				return false;
			}
			bool pass = true;
			shouldBeVisibleByDefault = true;
			foreach (TagGroup<OrganizerTagModel> tagGroup in tagsGrouper.groups) {
				bool anythingSelectedInThisGroup = false;
				bool craftPassesAnythingInThisGroup = false;
				bool craftContainsAnyTagFromThisGroup = false;
				foreach (TagInGroup<OrganizerTagModel> tag in tagGroup.tags) {
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

			foreach (OrganizerTagModel tag in tagsGrouper.restTags) {
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

