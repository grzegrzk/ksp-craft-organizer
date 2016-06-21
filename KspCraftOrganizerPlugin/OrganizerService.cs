using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace KspCraftOrganizer
{

	public class OrganizerService
	{
		private IKspAl ksp = IKspAlProvider.instance;
		private List<OrganizerCraftModel> cachedAvailableCrafts;
		private OrganizerCraftModel[] cachedFilteredCrafts;
		private SortedList<string, OrganizerTagModel> _availableTags;
		private OrganizerCraftModel _primaryCraft;
		private bool _selectAllFiltered;
		private bool _forceUncheckSelectAllFiltered;
		private string _craftNameFilter;
		private bool filterChanged;
		private int cachedSelectedCraftsCount;
		private CraftType _craftType;
		private Dictionary<CraftType, List<OrganizerCraftModel>> craftTypeToAvailableCraftsLazy = new Dictionary<CraftType, List<OrganizerCraftModel>> ();
		private bool _profileSettingsFileIsDirtyDoNotEditDirectly;
		private GuiStyleOption _selectedGuiStyle;
		private SortedList<string, OrganizerTagModel> _usedTags = new SortedList<string, OrganizerTagModel>();

		private OrganizerTagGroupsModel groupTags;

		private SettingsService settingsService = SettingsService.instance;
		private FileLocationService fileLocationService = FileLocationService.instance;

		public OrganizerService(){
			_craftType = ksp.getCurrentCraftType();
			groupTags = new OrganizerTagGroupsModel(this);
			ProfileSettingsDto profileSettigngs = settingsService.readProfileSettings ();
			_availableTags = new SortedList<string, OrganizerTagModel>();
			_selectedGuiStyle = profileSettigngs.selectedGuiStyle;
			if (_selectedGuiStyle == null) {
				_selectedGuiStyle = GuiStyleOption.Ksp;
			}
			foreach(string tagName in profileSettigngs.availableTags){
				addAvailableTag (tagName);
			}
			foreach (string tagName in profileSettigngs.selectedFilterTags)
			{
				if (!_availableTags.ContainsKey(tagName))
				{
					addAvailableTag(tagName);
				}
				_availableTags[tagName].selectedForFiltering = true;
			}
			groupTags.setInitialGroupsWithSelectedNone(profileSettigngs.filterGroupsWithSelectedNoneOption);
			craftNameFilter = profileSettigngs.selectedTextFilter;
			markProfileSettingsAsNotDirty("Constructor - fresh settings were just read");
		}


		public ICollection<OrganizerTagModel> availableTags{
			get{
				return new ReadOnlyCollection<OrganizerTagModel>(new List<OrganizerTagModel>(_availableTags.Values));
			}
		}

		public string craftNameFilter { 
			get { 
				return _craftNameFilter;
			}  
			set{ 
				if (_craftNameFilter != value) {
					_craftNameFilter = value;
					markFilterAsChanged ();
				}
			} 
		}

		public void markFilterAsChanged(){
			filterChanged = true;
			markProfileSettingsAsDirty("Filter changed");
		}

		private bool profileSettingsFileIsDirty { get { return _profileSettingsFileIsDirtyDoNotEditDirectly; }}

		private void markProfileSettingsAsNotDirty(string reason) {
			COLogger.logTrace("marking profile settings as NOT dirty, reason: " + reason);
			_profileSettingsFileIsDirtyDoNotEditDirectly = false;
		}

		private void markProfileSettingsAsDirty(string reason) {
			COLogger.logTrace("marking profile settings as dirty, reason: " + reason);
			_profileSettingsFileIsDirtyDoNotEditDirectly = true;
		}

		public void updateFilteredCrafts(){
			if (filterChanged) {
				this.cachedFilteredCrafts = null;
			}
		}


		public bool craftListModifiedDueToFilter { get; private set; }

		public OrganizerCraftModel[] filteredCrafts{
			get {
				if(cachedFilteredCrafts == null){
					cachedFilteredCrafts = createFilteredCrafts();
				}
				return cachedFilteredCrafts;
			}
		}

		private OrganizerCraftModel[] createFilteredCrafts(){
			List<OrganizerCraftModel> filtered = new List<OrganizerCraftModel> ();
			string upperFilter = craftNameFilter.ToUpper();
			craftListModifiedDueToFilter = true;
			foreach(OrganizerCraftModel craft in availableCrafts){
				bool shouldBeVisibleByDefault;
				if (doesCraftPassFilter (upperFilter, craft, out shouldBeVisibleByDefault)) {
					filtered.Add (craft);
					if (!shouldBeVisibleByDefault) {
						craftListModifiedDueToFilter = false;
					}
				} else {
					if (shouldBeVisibleByDefault) {
						craftListModifiedDueToFilter = false;
					}
					if (craft.isSelectedPrimary) {
						primaryCraft = null;
					}
					craft.setSelectedInternal (false);
				}
			}
			return filtered.ToArray();
		}

		internal GuiStyleOption selectedGuiStyle {
			get {
				return _selectedGuiStyle;
			}
			set {
				if (_selectedGuiStyle != value) {
					_selectedGuiStyle = value;
					markProfileSettingsAsDirty("gui style changed");
				}
			}
		}

		bool doesCraftPassFilter (string upperFilter, OrganizerCraftModel craft, out bool shouldBeVisibleByDefault)
		{
			shouldBeVisibleByDefault = true;
			bool pass = true;
			pass = pass && ( craft.nameToDisplay.ToUpper ().Contains (upperFilter) || craftNameFilter == "");
			foreach (OrganizerTagModel tag in _availableTags.Values) {
				if (tag.selectedForFiltering) {
					pass = pass && (craft.containsTag(tag.name));
				}
				if (YesNoTag.isByDefaultNegativeTag(tag.name) && craft.containsTag(tag.name)){
					shouldBeVisibleByDefault = false;
				}
				if (YesNoTag.isByDefaultPositiveTag(tag.name) && !craft.containsTag(tag.name)) {
					shouldBeVisibleByDefault = false;
				}
			}
			pass = pass && groupTags.doesCraftPassFilter(craft);
			return pass;
		}

		public List<OrganizerCraftModel> availableCrafts{
			get {
				if(cachedAvailableCrafts == null){
					cachedAvailableCrafts = getCraftsOfType (_craftType);
				}
				return cachedAvailableCrafts;
			}
		}
		private List<OrganizerCraftModel> getCraftsOfType(CraftType type){
			if (!craftTypeToAvailableCraftsLazy.ContainsKey(type)) {
				craftTypeToAvailableCraftsLazy.Add(type, fetchAvailableCrafts(type));
			}
			return craftTypeToAvailableCraftsLazy[type];
		}

		private List<OrganizerCraftModel> fetchAvailableCrafts(CraftType type)
		{
			COLogger.logDebug("fetching '" + type + "' crafts from disk");

			string craftDirectory = fileLocationService.getCraftDirectoryForCraftType(type);
			List<OrganizerCraftModel> toRetList = new List<OrganizerCraftModel>();
			toRetList.AddRange(fetchAvailableCrafts(craftDirectory, type, false));
			if (ksp.isShowStockCrafts())
			{
				craftDirectory = fileLocationService.getStockCraftDirectoryForCraftType(type);
				toRetList.AddRange(fetchAvailableCrafts(craftDirectory, type, true));
			}


			toRetList.Sort(delegate (OrganizerCraftModel c1, OrganizerCraftModel c2)
			{
				int craftComparisonResult = -c1.isAutosaved.CompareTo(c2.isAutosaved);
				if (craftComparisonResult == 0)
				{
					craftComparisonResult = c1.name.CompareTo(c2.name);
				}
				return craftComparisonResult;
			});
			return toRetList;
		}

		internal Texture2D getThumbnailForFile(string craftFile) {
			return ksp.getThumbnail(fileLocationService.getThumbUrl(craftFile));
		}

		public bool doNotWriteTagSettingsToDisk { get; internal set; }

		private OrganizerCraftModel[] fetchAvailableCrafts(String craftDirectory, CraftType type, bool isStock){
			COLogger.logDebug ("fetching '" + type + "' crafts from disk from " + craftDirectory);
			float startLoadingTime = Time.realtimeSinceStartup;
			string[] craftFiles = fileLocationService.getAllCraftFilesInDirectory(craftDirectory);
			OrganizerCraftModel[] toRet = new OrganizerCraftModel[craftFiles.Length];

			for (int i = 0; i < craftFiles.Length; ++i) {
				toRet[i] = new OrganizerCraftModel(this, craftFiles[i]);
				toRet[i].isAutosaved = ksp.getAutoSaveCraftName() == Path.GetFileNameWithoutExtension(craftFiles[i]);
				toRet[i].isStock = isStock;

				CraftSettingsDto craftSettings = settingsService.readCraftSettingsForCraftFile (toRet[i].craftFile);
				foreach (string craftTag in craftSettings.tags) {
					addAvailableTag (craftTag);
					toRet[i].addTag (craftTag);
				}
				toRet[i].nameFromSettingsFile = craftSettings.craftName;
				toRet[i].craftSettingsFileIsDirty = false;

				toRet[i].finishCreationMode();
			}
			float endLoadingTime = Time.realtimeSinceStartup;
			COLogger.logDebug("Finished fetching " + craftFiles.Length + " crafts, it took " + (endLoadingTime - startLoadingTime) + "s");
			return toRet;
		}

		public CraftDaoDto getCraftInfo(string craftFilePath) {
			return ksp.getCraftInfo(craftFilePath);
		}

		public void clearFilters() {
			craftNameFilter = "";
			groupTags.clearFilters();
			foreach (OrganizerTagModel tag in availableTags) {
				tag.selectedForFiltering = false;
				if (YesNoTag.isByDefaultNegativeTag(tag.name)) {
					List<string> singleList = new List<string>();
					singleList.Add(tag.name);
					groupTags.setGroupHasSelectedNoneFilter(tag.name, singleList);
				}
				if (YesNoTag.isByDefaultPositiveTag(tag.name)) {
					tag.selectedForFiltering = true;
				}
			}
		}


		public ICollection<string> beginNewTagGroupsFilterSpecification() {
			return groupTags.beginNewFilterSpecification();
		}

		public void endFilterSpecification() {
			groupTags.endFilterSpecification();
		}

		public void setGroupHasSelectedNoneFilter(string groupName, ICollection<string> tagsInGroup) {
			groupTags.setGroupHasSelectedNoneFilter(groupName, tagsInGroup);
		}

		public OrganizerCraftModel primaryCraft {
			set {
				if (_primaryCraft != null) {
					_primaryCraft.setSelectedPrimaryInternal(false);
				}
				_primaryCraft = value;
				if (_primaryCraft != null) {
					_primaryCraft.setSelectedPrimaryInternal(true);
				}
			}
			get {
				return _primaryCraft;
			}
		}

		public void renameCraft(OrganizerCraftModel model, string newName){
			string newFile = fileLocationService.renameCraft(model.craftFile, newName);
			model.setCraftFileInternal(newFile);
			model.craftDto.name = newName;
		}


		internal void deleteCraft(OrganizerCraftModel model) {
			fileLocationService.deleteCraft(model.craftFile);
			availableCrafts.Remove(model);
		}

		public void unselectAllCrafts(){
			foreach (OrganizerCraftModel craft in filteredCrafts) {
				craft.isSelected = false;
			}
		}

		public bool updateSelectedCrafts (bool selectAll){
			if (_forceUncheckSelectAllFiltered) {
				_selectAllFiltered = false;
				_forceUncheckSelectAllFiltered = false;
			} else {
				if (_selectAllFiltered != selectAll || selectAll) {
					foreach (OrganizerCraftModel craft in filteredCrafts) {
						craft.isSelected = selectAll;
					}
					_selectAllFiltered = selectAll;
				}
			}
			cachedSelectedCraftsCount = 0;
			foreach (OrganizerTagModel tag in _availableTags.Values) {
				tag.countOfSelectedCraftsWithThisTag = 0;
			}
			foreach(OrganizerCraftModel craft in filteredCrafts){
				if (craft.isSelected) {
					cachedSelectedCraftsCount += 1;
					foreach (string tag in craft.tags) {
						++_availableTags [tag].countOfSelectedCraftsWithThisTag;
					}
				}
			}
			foreach (OrganizerTagModel tag in _availableTags.Values) {
				tag.updateTagState ();
			}

			return _selectAllFiltered;
		}

		public ICollection<OrganizerTagModel> usedTags {
			get {
				return new ReadOnlyCollection<OrganizerTagModel>(_usedTags.Values);
			}
		}

		public void updateUsedTags(){
			_usedTags.Clear();
			foreach (OrganizerCraftModel craft in availableCrafts) {
				foreach(string tag in craft.tags) {
					if (!_usedTags.ContainsKey(tag)) {
						_usedTags.Add(tag, _availableTags[tag]);
					}
				}
			}

			foreach (OrganizerTagModel tag in availableTags) {
				if (!_usedTags.ContainsKey(tag.name)) {
					tag.selectedForFiltering = false;
				}
			}
		}

		public void onOneCraftUnselected(){
			_forceUncheckSelectAllFiltered = true;
		}

		public int selectedCraftsCount { 
			get {
				return cachedSelectedCraftsCount;
			} 
		}


		public OrganizerTagModel getTag(string tag) {
			return _availableTags[tag];
		}

		public void setTagToAllSelectedCrafts(OrganizerTagModel tag){
			foreach (OrganizerCraftModel craft in filteredCrafts) {
				if (craft.isSelected) {
					craft.addTag (tag.name);
				}
			}
		}

		public void removeTagFromAllSelectedCrafts(OrganizerTagModel tag){
			foreach (OrganizerCraftModel craft in filteredCrafts) {
				if (craft.isSelected) {
					craft.removeTag (tag.name);
				}
			}
		}

		public bool doesTagExist(string tag){
			return _availableTags.ContainsKey (tag);
		}

		public OrganizerTagModel addAvailableTag(string newTag){
			if (!_availableTags.ContainsKey (newTag)) {
				_availableTags.Add (newTag, new OrganizerTagModel (this, newTag));
				markProfileSettingsAsDirty("New available tag");
			}
			return _availableTags [newTag];
		}

		public void removeTag(string tag){
			if (_availableTags.ContainsKey (tag)) {
				foreach (OrganizerCraftModel craft in getCraftsOfType(CraftType.SPH)) {
					craft.removeTag (tag);
				}
				foreach (OrganizerCraftModel craft in getCraftsOfType(CraftType.VAB)) {
					craft.removeTag (tag);
				}
				_availableTags.Remove (tag);
				markProfileSettingsAsDirty("Tag removed");
			}
		}

		public void renameTag(string oldName, string newName){
			foreach (OrganizerCraftModel craft in getCraftsOfType(CraftType.SPH)) {
				if (craft.containsTag (oldName)) {
					craft.addTag (newName);
					craft.removeTag (oldName);
				}
			}
			foreach (OrganizerCraftModel craft in getCraftsOfType(CraftType.VAB)) {
				if (craft.containsTag (oldName)) {
					craft.addTag (newName);
					craft.removeTag (oldName);
				}
			}
			bool selectForFilterAfterInsertion = false;
			if (_availableTags.ContainsKey (oldName)) {
				selectForFilterAfterInsertion = _availableTags [oldName].selectedForFiltering;
				_availableTags.Remove (oldName);
			}
			if (!_availableTags.ContainsKey (newName)) {
				OrganizerTagModel newTag = new OrganizerTagModel (this, newName);
				newTag.selectedForFiltering = selectForFilterAfterInsertion;
				_availableTags.Add (newName, newTag);
			}
			markProfileSettingsAsDirty("Tag renamed");
		}


		public CraftType craftType {
			get {
				return _craftType;
			}

			set {
				if (value != _craftType) {
					_craftType = value;
					clearCaches ();
					if (this._primaryCraft != null)
					{
						this._primaryCraft.setSelectedPrimaryInternal(false);
					}
					this._primaryCraft = null;
				}
			}
		}

		private void clearCaches(){
			this.cachedAvailableCrafts = null;
			this.cachedFilteredCrafts = null;
			this.cachedSelectedCraftsCount = 0;
		}

		public bool isCraftAlreadyLoadedInWorkspace(){
			return ksp.isCraftAlreadyLoadedInWorkspace ();
		}

		public void mergeCraftToWorkspace(OrganizerCraftModel craft){
			ksp.mergeCraftToWorkspace (craft.craftFile);
		}
		public void loadCraftToWorkspace(OrganizerCraftModel craft){
			ksp.loadCraftToWorkspace (craft.craftFile);
		}

		public void writeAllDirtySettings(){
			if (profileSettingsFileIsDirty) {
				ProfileSettingsDto dto = new ProfileSettingsDto ();
				if (doNotWriteTagSettingsToDisk) {
					dto.availableTags = settingsService.readProfileSettings().availableTags;
				} else {
					dto.availableTags = _availableTags.Keys;
				}

				List<string> selectedTags = new List<string> ();
				foreach(OrganizerTagModel tag in _availableTags.Values) {
					if (tag.selectedForFiltering) {
						selectedTags.Add (tag.name);
					}
				}
				dto.filterGroupsWithSelectedNoneOption = groupTags.groupsWithSelectedNoneOption;
				dto.selectedFilterTags = selectedTags.ToArray();
				dto.selectedTextFilter = craftNameFilter;
				dto.selectedGuiStyle = _selectedGuiStyle;
				settingsService.writeProfileSettings(dto);

				markProfileSettingsAsNotDirty("Settings were just written to the disk");
			}
			if (!doNotWriteTagSettingsToDisk) {
				foreach (List<OrganizerCraftModel> crafts in craftTypeToAvailableCraftsLazy.Values) {
					foreach (OrganizerCraftModel craft in crafts) {
						if (craft.craftSettingsFileIsDirty) {
							CraftSettingsDto dto = new CraftSettingsDto();
							dto.tags = craft.tags;
							dto.craftName = craft.name;
							settingsService.writeCraftSettingsForCraftFile(craft.craftFile, dto);

							craft.craftSettingsFileIsDirty = false;
						}
					}
				}
			
			}
		}

	}
}

