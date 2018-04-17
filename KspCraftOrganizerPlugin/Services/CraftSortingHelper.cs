using System;
using System.Collections.Generic;
using KspNalCommon;
using System.Text.RegularExpressions;

namespace KspCraftOrganizer {


	public interface ICraftSortFunction {
		int apply(OrganizerCraftEntity c1, OrganizerCraftEntity c2);
		bool isSame(ICraftSortFunction other);

		string functionTypeId { get; }
		string functionData { get; }
		bool isReversed { get; }
	}

	public class CraftSortFunctionFactory{

		public delegate CraftSortFunction CraftSortFunctionProvider(string data);
		private static Dictionary<String, CraftSortFunctionProvider> sortFunctionDictionary = new Dictionary<String, CraftSortFunctionProvider>();

		static CraftSortFunctionFactory() {
			addSingletonFunction(CraftSortFunction.SORT_CRAFTS_BY_NAME);
			addSingletonFunction(CraftSortFunction.SORT_CRAFTS_BY_MASS);
			addSingletonFunction(CraftSortFunction.SORT_CRAFTS_BY_COST);
			addSingletonFunction(CraftSortFunction.SORT_CRAFTS_BY_STAGES);
			addSingletonFunction(CraftSortFunction.SORT_CRAFTS_BY_PARTS_COUNT);
			addSingletonFunction(CraftSortFunction.SORT_CRAFTS_BY_DATE);

			sortFunctionDictionary.Add(CraftSortFunction.SORT_ID_BY_TAG, (data) => { return CraftSortFunction.createByTagSorting(data);});
		}

		static void addSingletonFunction(CraftSortFunction function) {
			sortFunctionDictionary.Add(function.functionTypeId, (data) => { return function; });	
		}

		public static ICraftSortFunction createFunction(CraftSortingEntry sortingEntry) {
			ICraftSortFunction function = sortFunctionDictionary[sortingEntry.sortingId](sortingEntry.sortingData);
			if (sortingEntry.isReversed) {
				return new ReversedCraftSortingFunction(function);
			} else {
				return function;
			}
		}
	}

	public class CraftSortFunction : ICraftSortFunction {

		public static readonly string SORT_ID_BY_NAME = "byName";
		public static readonly string SORT_ID_BY_PARTS_COUNT = "byPartsCount";
		public static readonly string SORT_ID_BY_MASS = "byMass";
		public static readonly string SORT_ID_BY_STAGES_COUNT = "byStagesCount";
		public static readonly string SORT_ID_BY_COST = "byCost";
		public static readonly string SORT_ID_BY_DATE = "byDate";

		public static readonly string SORT_ID_BY_TAG = "ByTag";

		public static CraftSortFunction SORT_CRAFTS_BY_NAME = new CraftSortFunction(SORT_ID_BY_NAME, (c1, c2) => {
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

		public static CraftSortFunction SORT_CRAFTS_BY_PARTS_COUNT = new CraftSortFunction(SORT_ID_BY_PARTS_COUNT, (c1, c2) => {
			int craftComparisonResult = c1.partCount.CompareTo(c2.partCount);
			return craftComparisonResult;
		});
		public static CraftSortFunction SORT_CRAFTS_BY_MASS = new CraftSortFunction(SORT_ID_BY_MASS, (c1, c2) => {
			int craftComparisonResult = c1.mass.CompareTo(c2.mass);
			return craftComparisonResult;
		});
		public static CraftSortFunction SORT_CRAFTS_BY_STAGES = new CraftSortFunction(SORT_ID_BY_STAGES_COUNT, (c1, c2) => {
			int craftComparisonResult = c1.stagesCount.CompareTo(c2.stagesCount);
			return craftComparisonResult;
		});
		public static CraftSortFunction SORT_CRAFTS_BY_COST = new CraftSortFunction(SORT_ID_BY_COST, (c1, c2) => {
			int craftComparisonResult = c1.cost.CompareTo(c2.cost);
			return craftComparisonResult;
		});
		public static CraftSortFunction SORT_CRAFTS_BY_DATE = new CraftSortFunction(SORT_ID_BY_DATE, (c1, c2) => {
			int craftComparisonResult = c1.lastWriteTime.CompareTo(c2.lastWriteTime);
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
		private readonly string _functionTypeId;
		private CraftSortDelegateDoNotUseDirectly function;

		private CraftSortFunction(String _functionTypeId, CraftSortDelegateDoNotUseDirectly function) {
			this._functionTypeId = _functionTypeId;
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
			while(true){
				ReversedCraftSortingFunction c2Reversed = c2Objs as ReversedCraftSortingFunction;
				if (!object.ReferenceEquals(c2Reversed, null)) {
					c2Objs = c2Reversed.inner;
				}
				else
				{
					break;
				}
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


		public override int GetHashCode() {
			return this.function.GetHashCode();
		}

		public bool isSame(ICraftSortFunction other) {
			return this.Equals(other);
		}


		public string functionTypeId { get { return _functionTypeId; } }

		public virtual string functionData { get { return ""; } }

		public bool isReversed { get { return false; } }

		class CraftSortFunctionByTag : CraftSortFunction {

			private string tagGroupName;

			public CraftSortFunctionByTag(string tagGroupName, CraftSortDelegateDoNotUseDirectly function) : base(SORT_ID_BY_TAG, function) {
				this.tagGroupName = tagGroupName;
			}

			public override bool Equals(object c2Objs) {
				if (c2Objs == null) {
					return false;
				}
				while(true){
					ReversedCraftSortingFunction c2Reversed = c2Objs as ReversedCraftSortingFunction;
					if (!object.ReferenceEquals(c2Reversed, null)) {
						c2Objs = c2Reversed.inner;
					}
					else
					{
						break;
					}
				}
				CraftSortFunctionByTag c2 = c2Objs as CraftSortFunctionByTag;
				if (c2 == null) {
					return false;
				}
				return this.tagGroupName == c2.tagGroupName;
			}

			public override int GetHashCode() {
				return this.tagGroupName.GetHashCode();
			}

			public override string functionData { get { return tagGroupName; } }

		}
	}


	class ReversedCraftSortingFunction : ICraftSortFunction {

		public ICraftSortFunction inner;

		public ReversedCraftSortingFunction(ICraftSortFunction _inner) {
			this.inner = _inner;
		}

		public int apply(OrganizerCraftEntity c1, OrganizerCraftEntity c2) {
			return -inner.apply(c1, c2);
		}

		public bool isSame(ICraftSortFunction other) {
			return inner.isSame(other);
		}

		public string functionTypeId { get { return inner.functionTypeId; } }

		public string functionData { get { return inner.functionData; } }

		public bool isReversed { get { return true; } }
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
		
		private OrganizerControllerStateManager stateManager;

		public CraftSortingHelper(OrganizerControllerStateManager stateManager) {
			this.stateManager = stateManager;
		}

		public void sortCrafts(List<OrganizerCraftEntity> crafts) {
			PluginLogger.logDebug("Sorting crafts");
			List<ICraftSortFunction> craftSortingFunctions = new List<ICraftSortFunction>(stateManager.getCraftSortFunctions());
			crafts.Sort((c1, c2) => {
				int toRet = 0;
				try {
					for (int i = craftSortingFunctions.Count - 1; i >= 0; --i) {
						toRet = craftSortingFunctions[i].apply(c1, c2);
						if (toRet != 0) {
							break;
						}
					}
					if (toRet == 0) {
						toRet = CraftSortFunction.SORT_CRAFTS_BY_NAME.apply(c1, c2);
					}
				} catch (Exception ex) {
					PluginLogger.logError("Error while comparing craft '" + c1.craftFile + "' witch '" + c2.craftFile, ex);
				}
				return toRet;
			});
		}


		public bool addCraftSortingFunction(CraftSortFunction function) {
			PluginLogger.logDebug("Setting sorting function");
			ICraftSortFunction lastSortFunction = stateManager.getLastSortFunction();
			if (lastSortFunction == null || !lastSortFunction.isSame(function)) {
				stateManager.addCraftSortingFunction(function);
				return true;
			} else {
				stateManager.removeLastSortingFunction();
				if (lastSortFunction.isReversed)
				{
					stateManager.addCraftSortingFunction(function);
				}
				else
				{
					stateManager.addCraftSortingFunction(new ReversedCraftSortingFunction(function));	
				}
				return true;
			}
		}

		public ICraftSortFunction getLastSortFunction() {
			return stateManager.getLastSortFunction();
		}

	}
}
