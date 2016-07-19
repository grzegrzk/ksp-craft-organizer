using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace KspCraftOrganizer
{

	public class OrganizerService {
		private IKspAl ksp = IKspAlProvider.instance;
		private SortedList<string, OrganizerTagModel> _availableTags;
		private string _craftNameFilter;
		private bool filterChanged;
		private bool _profileSettingsFileIsDirtyDoNotEditDirectly;
		private GuiStyleOption _selectedGuiStyle;
		private SortedList<string, OrganizerTagModel> _usedTags = new SortedList<string, OrganizerTagModel>();

		private OrganizerTagGroupsModel groupTags;

		private SettingsService settingsService = SettingsService.instance;
		private FileLocationService fileLocationService = FileLocationService.instance;
		private OrganizerServiceCraftList craftList;

		public OrganizerService() {
			this.craftList = new OrganizerServiceCraftList(this);
			//_craftType = ksp.getCurrentCraftType();
			groupTags = new OrganizerTagGroupsModel(this);
			ProfileSettingsDto profileSettigngs = settingsService.readProfileSettings();
			_availableTags = new SortedList<string, OrganizerTagModel>();
			_selectedGuiStyle = profileSettigngs.selectedGuiStyle;
			if (_selectedGuiStyle == null) {
				_selectedGuiStyle = GuiStyleOption.Ksp;
			}
			foreach (string tagName in profileSettigngs.availableTags) {
				addAvailableTag(tagName);
			}
			foreach (string tagName in profileSettigngs.selectedFilterTags) {
				if (!_availableTags.ContainsKey(tagName)) {
					addAvailableTag(tagName);
				}
				_availableTags[tagName].selectedForFiltering = true;
			}
			groupTags.setInitialGroupsWithSelectedNone(profileSettigngs.filterGroupsWithSelectedNoneOption);
			craftNameFilter = profileSettigngs.selectedTextFilter;
			markProfileSettingsAsNotDirty("Constructor - fresh settings were just read");
		}

		public bool selectAllFiltered {
			get {
				return craftList.selectAllFiltered;
			}
		}
		public ICollection<OrganizerTagModel> availableTags {
			get {
				return new ReadOnlyCollection<OrganizerTagModel>(new List<OrganizerTagModel>(_availableTags.Values));
			}
		}

		public string craftNameFilter {
			get {
				return _craftNameFilter;
			}
			set {
				if (_craftNameFilter != value) {
					_craftNameFilter = value;
					markFilterAsChanged();
				}
			}
		}

		public void markFilterAsChanged() {
			filterChanged = true;
			markProfileSettingsAsDirty("Filter changed");
		}

		private bool profileSettingsFileIsDirty { get { return _profileSettingsFileIsDirtyDoNotEditDirectly; } }

		private void markProfileSettingsAsNotDirty(string reason) {
			COLogger.logTrace("marking profile settings as NOT dirty, reason: " + reason);
			_profileSettingsFileIsDirtyDoNotEditDirectly = false;
		}

		private void markProfileSettingsAsDirty(string reason) {
			COLogger.logTrace("marking profile settings as dirty, reason: " + reason);
			_profileSettingsFileIsDirtyDoNotEditDirectly = true;
		}

		public bool craftsAreFiltered { get { return craftList.craftsAreFiltered; } }

		public OrganizerCraftModel[] filteredCrafts {
			get {
				return craftList.filteredCrafts;
			}
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

		public List<OrganizerCraftModel> availableCrafts {
			get {
				return craftList.availableCrafts;
			}
		}

		internal Texture2D getThumbnailForFile(string craftFile) {
			return ksp.getThumbnail(fileLocationService.getThumbUrl(craftFile));
		}

		public bool doNotWriteTagSettingsToDisk { get; internal set; }

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

		public void unselectAllCrafts() {
			craftList.unselectAllCrafts();
		}
		public void update(bool selectAll) {
			string upperFilter = craftNameFilter.ToUpper();
			craftList.update(delegate (OrganizerCraftModel craft, out bool shouldBeVisibleByDefault){ 
				return doesCraftPassFilter(upperFilter, craft, out shouldBeVisibleByDefault); 
			}, selectAll, filterChanged);

			foreach (OrganizerTagModel tag in _availableTags.Values) {
				tag.countOfSelectedCraftsWithThisTag = 0;
			}
			foreach (OrganizerCraftModel craft in filteredCrafts) {
				if (craft.isSelected) {
					foreach (string tag in craft.tags) {
						++_availableTags[tag].countOfSelectedCraftsWithThisTag;
					}
				}
			}
			foreach (OrganizerTagModel tag in _availableTags.Values) {
				tag.updateTagState();
			}

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
			craftList.forceUncheckSelectAllFiltered = true;
		}

		public int selectedCraftsCount { 
			get {
				return craftList.selectedCraftsCount;
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
				foreach (OrganizerCraftModel craft in craftList.getCraftsOfType(CraftType.SPH)) {
					craft.removeTag (tag);
				}
				foreach (OrganizerCraftModel craft in craftList.getCraftsOfType(CraftType.VAB)) {
					craft.removeTag (tag);
				}
				_availableTags.Remove (tag);
				markProfileSettingsAsDirty("Tag removed");
			}
		}

		public void renameTag(string oldName, string newName){
			foreach (OrganizerCraftModel craft in craftList.getCraftsOfType(CraftType.SPH)) {
				if (craft.containsTag (oldName)) {
					craft.addTag (newName);
					craft.removeTag (oldName);
				}
			}
			foreach (OrganizerCraftModel craft in craftList.getCraftsOfType(CraftType.VAB)) {
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

		public OrganizerCraftModel primaryCraft {
			get {
				return craftList.primaryCraft;
			}
			set {
				craftList.primaryCraft = value;
			}
		}

		public void renameCraft(OrganizerCraftModel craft, string newName) {
			craftList.renameCraft(craft, newName);
		}

		public void deleteCraft(OrganizerCraftModel craft) {
			craftList.deleteCraft(craft);
		}

		public CraftType craftType {
			get {
				return craftList.craftType;
			}
			set {
				craftList.craftType = value;
			}
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
				foreach (List<OrganizerCraftModel> crafts in craftList.alreadyLoadedCrafts) {
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

