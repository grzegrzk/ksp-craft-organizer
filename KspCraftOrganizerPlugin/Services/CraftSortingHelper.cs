using System;
using System.Collections.Generic;
using KspNalCommon;
using System.Text.RegularExpressions;

namespace KspCraftOrganizer {


	public interface ICraftSortFunction {
		int apply(OrganizerCraftEntity c1, OrganizerCraftEntity c2);
		bool isSame(ICraftSortFunction other);
	}

	public class CraftSortFunction : ICraftSortFunction {


		public static CraftSortFunction SORT_CRAFTS_BY_NAME = new CraftSortFunction((c1, c2) => {
			int craftComparisonResult = -c1.isAutosaved.CompareTo(c2.isAutosaved);
			if (craftComparisonResult == 0) {
				craftComparisonResult = stringsNaturalCompare(c1.name, c2.name);
			}
			return craftComparisonResult;
		});

		static int stringsNaturalCompare(string s1, string s2) {
			int maxDigits1 = findLongestDigits(s1);
			int maxDigits2 = findLongestDigits(s2);
			int maxDigits = Math.Max(maxDigits1, maxDigits2);

			string expandedS1 = Regex.Replace(s1, "\\d+", (match) => { return match.Value.PadLeft(maxDigits, '0'); });
			string expandedS2 = Regex.Replace(s2, "\\d+", (match) => { return match.Value.PadLeft(maxDigits, '0'); });

			return expandedS1.CompareTo(expandedS2);
		}

		static int findLongestDigits(string s) {
			String digitsPattern = "\\d+";
			Match digitsMatch = Regex.Match(s, digitsPattern);

			int maxDigits = 0;
			while (digitsMatch.Success) {
				maxDigits = Math.Max(maxDigits, digitsMatch.Length);
				digitsMatch = digitsMatch.NextMatch();
			}
			return maxDigits;
		}

		public static CraftSortFunction SORT_CRAFTS_BY_PARTS_COUNT = new CraftSortFunction((c1, c2) => {
			int craftComparisonResult = c1.partCount.CompareTo(c2.partCount);
			return craftComparisonResult;
		});
		public static CraftSortFunction SORT_CRAFTS_BY_MASS = new CraftSortFunction((c1, c2) => {
			int craftComparisonResult = c1.mass.CompareTo(c2.mass);
			return craftComparisonResult;
		});
		public static CraftSortFunction SORT_CRAFTS_BY_STAGES = new CraftSortFunction((c1, c2) => {
			int craftComparisonResult = c1.stagesCount.CompareTo(c2.stagesCount);
			return craftComparisonResult;
		});
		public static CraftSortFunction SORT_CRAFTS_BY_COST = new CraftSortFunction((c1, c2) => {
			int craftComparisonResult = c1.cost.CompareTo(c2.cost);
			return craftComparisonResult;
		});

		public static CraftSortFunction createByTagSorting(String tagGroup) {
			return new CraftSortFunctionByTag(tagGroup, (c1, c2) => {
				int craftComparisonResult;
				bool g1Exists = c1.groupedTags.groupExists(tagGroup);
				bool g2Exists = c2.groupedTags.groupExists(tagGroup);
				if (!g1Exists || !g2Exists) {
					if (g1Exists == g2Exists) {
						craftComparisonResult = 0;
					} else {
						if (!g1Exists) {
							craftComparisonResult = 1;
						} else {
							craftComparisonResult = -1;
						}
					}
				} else {
					String g1 = c1.groupedTags.getGroup(tagGroup).firstTag.originalTag;
					String g2 = c2.groupedTags.getGroup(tagGroup).firstTag.originalTag;
					craftComparisonResult = stringsNaturalCompare(g1, g2);
				}
				return craftComparisonResult;
			});
		}

		private delegate int CraftSortDelegateDoNotUseDirectly(OrganizerCraftEntity c1, OrganizerCraftEntity c2);
		private CraftSortDelegateDoNotUseDirectly function;

		private CraftSortFunction(CraftSortDelegateDoNotUseDirectly function) {
			this.function = function;
		}

		public int apply(OrganizerCraftEntity c1, OrganizerCraftEntity c2) {
			return function(c1, c2);
		}

		public static bool operator ==(CraftSortFunction o1, CraftSortFunction o2) {
			if (object.ReferenceEquals(o1, null) || object.ReferenceEquals(o2, null)) {
				return object.ReferenceEquals(o1, o2);
			}
			CraftSortFunctionByTag o1Tag = o1 as CraftSortFunctionByTag;
			if (!object.ReferenceEquals(o1Tag, null)) {
				return o1Tag.Equals(o2);
			}
			return o1.Equals(o2);
		}

		public static bool operator !=(CraftSortFunction o1, CraftSortFunction o2) {
			return !(o1 == o2);
		}

		public override bool Equals(object c2Objs) {
			if (object.ReferenceEquals(c2Objs, null)) {
				return false;
			}
			CraftSortFunction c2 = c2Objs as CraftSortFunction;
			CraftSortFunction c2Tag = c2Objs as CraftSortFunctionByTag;
			if (!object.ReferenceEquals(c2Tag, null)) {
				return c2Tag.Equals(this);
			}
			if (object.ReferenceEquals(c2, null)) {
				return false;
			}
			return this.function == c2.function;
		}

		public bool isSame(ICraftSortFunction other) {
			return this.Equals(other);
		}


		class CraftSortFunctionByTag : CraftSortFunction {

			private string tagGroupName;

			public CraftSortFunctionByTag(string tagGroupName, CraftSortDelegateDoNotUseDirectly function) : base(function) {
				this.tagGroupName = tagGroupName;
			}

			public override bool Equals(object c2Objs) {
				if (c2Objs == null) {
					return false;
				}
				CraftSortFunctionByTag c2 = c2Objs as CraftSortFunctionByTag;
				if (c2 == null) {
					return false;
				}
				return this.tagGroupName == c2.tagGroupName;
			}
		}
	}


	class ReversedCraftSortingFunction : ICraftSortFunction {

		private ICraftSortFunction inner;

		public ReversedCraftSortingFunction(ICraftSortFunction _inner) {
			this.inner = _inner;
		}

		public int apply(OrganizerCraftEntity c1, OrganizerCraftEntity c2) {
			return -inner.apply(c1, c2);
		}

		public bool isSame(ICraftSortFunction other) {
			return inner.isSame(other);
		}

	}

	public class CraftSortData {
		private string _name;
		private CraftSortFunction _function;


		public CraftSortData(string name, CraftSortFunction function) {
			this._name = name;
			this._function = function;
		}

		public string name { get { return _name; } }
		public CraftSortFunction function { get { return _function; } }

		public override bool Equals(System.Object c2Objs) {
			if (object.ReferenceEquals(c2Objs, null)) {
				return false;
			}
			CraftSortData c2 = c2Objs as CraftSortData;
			if (object.ReferenceEquals(c2, null)) {
				return false;
			}
			return this._name == c2._name;
		}

		public override int GetHashCode() {
			return this._name.GetHashCode();
		}
	}


	public class CraftSortingHelper {
		
		private List<ICraftSortFunction> craftSortingFunctions = new List<ICraftSortFunction>();

		public void sortCrafts(List<OrganizerCraftEntity> crafts) {
			crafts.Sort((c1, c2) => {
				int toRet = 0;
				for (int i = craftSortingFunctions.Count - 1; i >= 0; --i) {
					toRet = craftSortingFunctions[i].apply(c1, c2);
					if (toRet != 0) {
						break;
					}
				}
				if (toRet == 0) {
					toRet = CraftSortFunction.SORT_CRAFTS_BY_NAME.apply(c1, c2);
				}
				return toRet;
			});
		}

		public bool addCraftSortingFunction(CraftSortFunction function) {
			PluginLogger.logDebug("Setting sorting function");
			if (craftSortingFunctions.Count == 0 || !craftSortingFunctions[craftSortingFunctions.Count - 1].isSame(function)) {
				craftSortingFunctions.Add(function);
				if (craftSortingFunctions.Count > 10) {
					craftSortingFunctions.RemoveAt(0);
				}
				return true;
			} else {
				ICraftSortFunction oldSortFunction = craftSortingFunctions[craftSortingFunctions.Count - 1];
				craftSortingFunctions.RemoveAt(craftSortingFunctions.Count - 1);
				craftSortingFunctions.Add(new ReversedCraftSortingFunction(oldSortFunction));
				return true;
			}
		}

	}
}
