using System;
using System.Collections.Generic;

namespace KspCraftOrganizer {
	
	public class OrganizerTagGroupsModel {
		private OrganizerService parent;
		private Dictionary<string, string> groupsWithSelectedNone = new Dictionary<string, string>();
		private Dictionary<string, string> negativeTagsNow = new Dictionary<string, string>();
		private Dictionary<string, string> previousNegativeTags = new Dictionary<string, string>();
		private bool someNewTagInGroup;

		public OrganizerTagGroupsModel(OrganizerService parent) {
			this.parent = parent;
		}

		public ICollection<string> beginNewFilterSpecification() {
			previousNegativeTags = negativeTagsNow;
			negativeTagsNow = new Dictionary<string, string>();

			Dictionary<string, string> oldGroups = groupsWithSelectedNone;
			groupsWithSelectedNone = new Dictionary<string, string>();
			someNewTagInGroup = false;
			return oldGroups.Keys;
		}

		public void setInitialGroupsWithSelectedNone(ICollection<string> filterGroupsWithSelectedNoneOption) {

			foreach (string groupName in filterGroupsWithSelectedNoneOption) {

				if (!groupsWithSelectedNone.ContainsKey(groupName)) {
					groupsWithSelectedNone.Add(groupName, groupName);
				}
			}
		}

		public bool doesCraftPassFilter(OrganizerCraftModel craft) {
			bool pass = true;
			foreach (string tag in negativeTagsNow.Values) {
				pass = pass && !craft.containsTag(tag);
				if (pass == false) {
					break;
				}
			}
			return pass;
		}

		public ICollection<string> groupsWithSelectedNoneOption { 
			get {
				return groupsWithSelectedNone.Keys;
			} 
		}

		public void setGroupHasSelectedNoneFilter(string groupName, ICollection<string> tagsInGroup) {
			if (!groupsWithSelectedNone.ContainsKey(groupName)){
				groupsWithSelectedNone.Add(groupName, groupName);
			}
			foreach(string tag in tagsInGroup){
				negativeTagsNow.Add(tag, tag);
				if (!previousNegativeTags.ContainsKey(tag)) {
					COLogger.logDebug("someNewTagInGroup");
					someNewTagInGroup = true;
					parent.getTag(tag).selectedForFiltering = false;
				}
			}
		}

		public void endFilterSpecification() {
			if (someNewTagInGroup || someTagRemoved()) {
				parent.markFilterAsChanged();
			}
		}

		private bool someTagRemoved() {
			bool toRet = false;

			foreach (string previousTag in previousNegativeTags.Keys) {
				if (!negativeTagsNow.ContainsKey(previousTag)) {
					toRet = true;
					COLogger.logDebug("someTagRemoved");
					break;
				}
			}
			return toRet;
		}

		public void clearFilters() {
			beginNewFilterSpecification();
		}

}
}

