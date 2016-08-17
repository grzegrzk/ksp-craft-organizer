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

	}
}

