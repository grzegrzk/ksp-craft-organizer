using System.Collections.Generic;

namespace KspCraftOrganizer {

	public class ManagementTagGroup : TagGroup<OrganizerTagEntity> {
		
		private OrganizerController parent;

		public ManagementTagGroup(OrganizerController parent, string name) : base(name) {
			this.parent = parent;
		}

		public bool collapsedInManagementView {
			get {
				return parent.stateManager.isGroupCollapsedInManagement(name);
			}
			set {
				parent.stateManager.setGroupCollapsedInManagement(name, value);
			}
		}

	}

	public class ManagementTagsGrouper : TagsGrouper<OrganizerTagEntity, ManagementTagGroup> {

		public ManagementTagsGrouper(OrganizerController parent) : base(t => t.name, s => new ManagementTagGroup(parent, s)) {

		}

		//public void assignCurrentFilterSettingsToDto(ProfileFilterSettingsDto filterDto) {
		//	List<string> collapsedGroups = new List<string>();
		//	foreach (ManagementTagGroup tagGroup in groups) {
		//		if (tagGroup.collapsedInManagementView) {
		//			collapsedGroups.Add(tagGroup.displayName);
		//		}
		//	}
		//	filterDto.collapsedManagementGroups = collapsedGroups;
		//}

		//public void applyFilterSettings(ProfileFilterSettingsDto filterDto) {
		//	foreach (ManagementTagGroup tagGroup in groups) {
		//		tagGroup.collapsedInManagementView = filterDto.collapsedManagementGroups.Contains(tagGroup.displayName);
		//	}
		//}

	}
}

