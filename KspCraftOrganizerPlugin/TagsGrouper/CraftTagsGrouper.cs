using System.Collections.Generic;

namespace KspCraftOrganizer {

	public class CraftTagGroup : TagGroup<string> {

		public CraftTagGroup(string name) : base(name) {
			//
		}

		public float guiHeight { get; set; }
	}

	public class CraftTagsGrouper : TagsGrouper<string, CraftTagGroup> {
		public CraftTagsGrouper(ICollection<string> tags) : base(t => t, s => new CraftTagGroup(s)) {
			update(tags);
		}
	}
}

