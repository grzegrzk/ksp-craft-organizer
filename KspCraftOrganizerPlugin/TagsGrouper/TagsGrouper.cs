using System.Collections.Generic;

namespace KspCraftOrganizer {

	public static class YesNoTag {
		public static bool isYesNoTag(string tag) {
			return tag.EndsWith("?");
		}
		public static string getGroupDisplayName(string name) {
			if (name.EndsWith("?")) {
				int startIndex = 0;
				int toOmitLength = 1;
				if (name.StartsWith("+") || name.StartsWith("-")) {
					startIndex = 1;
					toOmitLength = 2;
				}
				return name.Substring(startIndex, name.Length - toOmitLength);
			} else {
				return name;
			}
		}

		public static bool isByDefaultNegativeTag(string tag) {
			return tag.StartsWith("-");
		}

		public static bool isByDefaultPositiveTag(string tag) {
			return tag.StartsWith("+");
		}
	}

	public class TagInGroup<T> {
		private static readonly char[] PATH_SEPARATORS = { '/', '\\' };

		public string groupName { get; private set; }
		public string tagDisplayName { get; private set; }

		public T originalTag { get; private set; }
		public string originalTagString { get; private set; }


		public TagInGroup(T originalTag, Globals.Function<string, T> stringizer) {
			this.originalTag = originalTag;
			this.originalTagString = stringizer(originalTag);
			int separatorIndex = originalTagString.IndexOfAny(PATH_SEPARATORS);
			if (separatorIndex >= 0) {
				this.groupName = originalTagString.Substring(0, separatorIndex);
				this.tagDisplayName = originalTagString.Substring(separatorIndex + 1);
			} else {
				if (YesNoTag.isYesNoTag(originalTagString)) {
					this.groupName = originalTagString;
					this.tagDisplayName = "yes";
				} else {
					this.groupName = "";
					this.tagDisplayName = originalTagString;
				}
			}
		}

		public bool hasGroupName {
			get {
				return groupName != "";
			}
		}
	}


	public class TagGroup<T> {

		public string name { get; private set; }
		public string displayName {
			get {
				if (YesNoTag.isYesNoTag(name)) {
					return YesNoTag.getGroupDisplayName(name);
				} else {
					return name;
				}
			}
		}

		public bool isYesNoGroup { get { return YesNoTag.isYesNoTag(name); } }
		private SortedDictionary<string, TagInGroup<T>> _tags = new SortedDictionary<string, TagInGroup<T>>();

		public TagGroup(string name) {
			this.name = name;
		}

		public void addTagIfNotExist(TagInGroup<T> tag) {
			if (!_tags.ContainsKey(tag.tagDisplayName)) {
				_tags.Add(tag.tagDisplayName, tag);
			}
		}

		public void removeTagIfExists(TagInGroup<T> tag) {
			if (_tags.ContainsKey(tag.tagDisplayName)) {
				_tags.Remove(tag.tagDisplayName);
			}
		}

		public ICollection<TagInGroup<T>> tags {
			get {
				return _tags.Values;
			}
		}
		public string tagsAsString {
			get {
				return Globals.join(_tags.Keys, tag => tag, ", ");
			}
		}

		public ICollection<string> tagsAsArrayOfStrings {
			get {
				List<string> toRet = new List<string>();
				foreach (TagInGroup<T> t in _tags.Values) {
					toRet.Add(t.originalTagString);
				}
				return toRet;
			}
		}

		public TagInGroup<T> firstTag {
			get {
				foreach (TagInGroup<T> toRet in _tags.Values) {
					return toRet;
				}
				return null;
			}
		}
	}

	public class TagsGrouper<T, G> where G : TagGroup<T> {
		private SortedDictionary<string, G> _tagGroups = new SortedDictionary<string, G>();
		private SortedList<string, T> _restTags = new SortedList<string, T>();
		private Dictionary<string, TagInGroup<T>> allTags = new Dictionary<string, TagInGroup<T>>();

		private Globals.Function<string, T> stringizer;
		private Globals.Function<G, string> createGroup;


		public TagsGrouper(Globals.Function<string, T> stringizer, Globals.Function<G, string> createGroup) {
			this.stringizer = stringizer;
			this.createGroup = createGroup;
		}


		public virtual void update(ICollection<T> currentTags) {
			Dictionary<string, TagInGroup<T>> tagsToRemove = allTags;
			allTags = new Dictionary<string, TagInGroup<T>>();

			foreach (T tag in currentTags) {
				TagInGroup<T> tagInGroup = new TagInGroup<T>(tag, stringizer);
				string stringizedTag = stringizer(tag);
				if (tagInGroup.hasGroupName) {
					if (!_tagGroups.ContainsKey(tagInGroup.groupName)) {
						_tagGroups.Add(tagInGroup.groupName, createGroup(tagInGroup.groupName));
					}
					_tagGroups[tagInGroup.groupName].addTagIfNotExist(tagInGroup);
				} else {
					if (!_restTags.ContainsKey(stringizedTag)){
						_restTags.Add(stringizedTag, tag);
					}
				}

				allTags.Add(stringizedTag, tagInGroup);
				if (tagsToRemove.ContainsKey(stringizedTag)) {
					tagsToRemove.Remove(stringizedTag);
				}
			}

			foreach (var tag in tagsToRemove) {
				if (_tagGroups.ContainsKey(tag.Value.groupName)) {
					_tagGroups[tag.Value.groupName].removeTagIfExists(tag.Value);
				}
				if (_restTags.ContainsKey(tag.Key)) {
					_restTags.Remove(tag.Key);
				}
			}

			List<string> groupsToRemove = new List<string>();
			foreach (var tagGroup in _tagGroups) {
				if (tagGroup.Value.tags.Count == 0) {
					groupsToRemove.Add(tagGroup.Key);
				}
			}
			foreach (string groupToRemove in groupsToRemove) {
				if (_tagGroups.ContainsKey(groupToRemove)){
					_tagGroups.Remove(groupToRemove);
				}
			}
		}

		public ICollection<G> groups {
			get {
				return _tagGroups.Values;
			}
		}

		public ICollection<T> restTags {
			get {
				return _restTags.Values;
			}
		}


		public string restTagsAsString {
			get {
				return Globals.join(_restTags.Keys, tag => tag, ", ");
			}
		}
		public float restTagsGuiHeight { get; set; }

		internal bool groupExists(string g) {
			return _tagGroups.ContainsKey(g);
		}

		internal G getGroup(string groupName) {
			return _tagGroups[groupName];
		}
	}

}

