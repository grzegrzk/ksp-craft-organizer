using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace KspCraftOrganizer
{

	public class OrganizerController {
		private IKspAl ksp = IKspAlProvider.instance;
		private bool _profileSettingsFileIsDirtyDoNotEditDirectly;
		private GuiStyleOption _selectedGuiStyle;

		private SettingsService settingsService = SettingsService.instance;
		private FileLocationService fileLocationService = FileLocationService.instance;
		private OrganizerControllerCraftList craftList;
		private OrganizerControllerFilter filter;
		public ManagementTagsGrouper managementTagsGroups;
		public bool restTagsInManagementCollapsed { get; set; }
		private bool afterSettingsChanged = true;
		private ProfileAllFilterSettingsDto allFiltersDto;

		public bool restTagsInFilterCollapsed { get { return filter.restTagsCollapsed; } set { filter.restTagsCollapsed = value;}  }

		public Dictionary<string, bool> defaultTagsToAdd { get; private set; }
		public List<string> defaultTagsNotToAdd { get; private set; }


		public OrganizerController() {
			this.managementTagsGroups = new ManagementTagsGrouper(this);
			this.craftList = new OrganizerControllerCraftList(this);
			ProfileSettingsDto profileSettings = settingsService.readProfileSettings();
			this.allFiltersDto = profileSettings.allFilter;
			this.filter = new OrganizerControllerFilter(this, profileSettings);
			this.defaultTagsToAdd = new Dictionary<string, bool>();
			refreshDefaultTagsToAdd();

			_selectedGuiStyle = profileSettings.selectedGuiStyle;
			if (_selectedGuiStyle == null) {
				_selectedGuiStyle = GuiStyleOption.Ksp;
			}
			markProfileSettingsAsNotDirty("Constructor - fresh settings were just read");

			this.filter.init();
		}

		private void refreshDefaultTagsToAdd() {
			defaultTagsNotToAdd = new List<string>();
			foreach (string tag in settingsService.getPluginSettings().defaultAvailableTags) {
				if (!filter.doesTagExist(tag)) {
					if (!this.defaultTagsToAdd.ContainsKey(tag)) {
						this.defaultTagsToAdd.Add(tag, true);
					}
				} else {
					if (this.defaultTagsToAdd.ContainsKey(tag)) {
						this.defaultTagsToAdd.Remove(tag);
					}
					defaultTagsNotToAdd.Add(tag);
				}
			}
		}

		internal void addSelectedDefaultTags() {
			foreach (KeyValuePair<string, bool> tag in new Dictionary<string, bool>(defaultTagsToAdd)) {
				if (tag.Value) {
					addAvailableTag(tag.Key);
				}
			}
		}

		private ProfileFilterSettingsDto getFilterDtoFor(CraftType craftType) {
			return allFiltersDto.getFilterDtoFor(ksp.getCurrentEditorFacilityType(), craftType);
		}


		public bool selectAllFiltered {
			get {
				return craftList.selectAllFiltered;
			}
		}

		public ICollection<OrganizerTagEntity> availableTags {
			get {
				return filter.availableTags;
			}
		}

		public string craftNameFilter {
			get {
				return filter.craftNameFilter;
			}
			set {
				filter.craftNameFilter = value;
			}
		}

		public void markFilterAsChanged() {
			if (filter != null) {//may happen in constructor
				filter.markFilterAsChanged();
			}
		}

		public void markFilterAsUpToDate() {
			filter.markFilterAsUpToDate();
		}

		public List<OrganizerCraftEntity> getCraftsOfType(CraftType type) {
			return craftList.getCraftsOfType(type);
		}

		private bool profileSettingsFileIsDirty { get { return _profileSettingsFileIsDirtyDoNotEditDirectly; } }

		private void markProfileSettingsAsNotDirty(string reason) {
			COLogger.logTrace("marking profile settings as NOT dirty, reason: " + reason);
			_profileSettingsFileIsDirtyDoNotEditDirectly = false;
		}

		public void markProfileSettingsAsDirty(string reason) {
			COLogger.logTrace("marking profile settings as dirty, reason: " + reason);
			_profileSettingsFileIsDirtyDoNotEditDirectly = true;
		}

		public bool craftsAreFiltered { get { return craftList.craftsAreFiltered; } }

		public OrganizerCraftEntity[] filteredCrafts {
			get {
				return craftList.filteredCrafts;
			}
		}

		public ICollection<string> availableSaveNames {
			get {
				return fileLocationService.getAvailableSaveNames();
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

		public List<OrganizerCraftEntity> availableCrafts {
			get {
				return craftList.availableCrafts;
			}
		}

		internal Texture2D getThumbnailForFile(string craftFile) {
			return ksp.getThumbnail(fileLocationService.getThumbUrl(craftFile));
		}

		public bool doNotWriteTagSettingsToDisk { get; set; }

		public CraftDaoDto getCraftInfo(string craftFilePath) {
			return ksp.getCraftInfo(craftFilePath);
		}


		public void clearFilters() {
			filter.clearFilters();
		}

		public void setGroupHasSelectedNoneFilter(string groupName, bool selectedNone) {
			filter.setGroupHasSelectedNoneFilter(groupName, selectedNone);
		}

		public FilterTagsGrouper filterTagsGrouper {
			get {
				return filter.tagsGrouper;
			}
		}

		public void unselectAllCrafts() {
			craftList.unselectAllCrafts();
		}

		public void update(bool selectAll) {
			//
			//Filter & crafts affect each other:
			//
			// - The only tags that affect filtering are those assigned to the crafts currently on the list
			// - The only displayed crafts are those that pass filter
			//
			//
			//So at first we need to update filter and then update craft list.
			//
			filter.update();
			ProfileFilterSettingsDto filterDto = getFilterDtoFor(craftType);
			if (afterSettingsChanged) {
				filter.applyFilterSettings(filterDto);
			}
			craftList.update(selectAll, filter.filterChanged);
			managementTagsGroups.update(availableTags);
			if (afterSettingsChanged) {
				managementTagsGroups.applyFilterSettings(filterDto);
				restTagsInManagementCollapsed = filterDto.restManagementTagsCollapsed;
			}

			afterSettingsChanged = false;
		}

		public OrganizerControllerCraftList.CraftFilterPredicate craftFilterPredicate {
			get {
				return filter.createCraftFilterPredicate();
			}
		}


		public ICollection<OrganizerTagEntity> usedTags {
			get {
				return filter.usedTags;
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

		public OrganizerTagEntity getTag(string tag) {
			return filter.getTag(tag);
		}

		public void setTagToAllSelectedCrafts(OrganizerTagEntity tag){
			foreach (OrganizerCraftEntity craft in filteredCrafts) {
				if (craft.isSelected) {
					craft.addTag (tag.name);
				}
			}
		}

		public void removeTagFromAllSelectedCrafts(OrganizerTagEntity tag){
			foreach (OrganizerCraftEntity craft in filteredCrafts) {
				if (craft.isSelected) {
					craft.removeTag (tag.name);
				}
			}
		}

		public bool doesTagExist(string tag) {
			return filter.doesTagExist(tag);
		}

		public OrganizerTagEntity addAvailableTag(string newTag) {
			OrganizerTagEntity toRet = filter.addAvailableTag(newTag);
			refreshDefaultTagsToAdd();
			return toRet;
		}

		public void removeTag(string tag) {
			filter.removeTag(tag);
			refreshDefaultTagsToAdd();
		}

		public void renameTag(string oldName, string newName) {
			filter.renameTag(oldName, newName);
			refreshDefaultTagsToAdd();
		}

		public OrganizerCraftEntity primaryCraft {
			get {
				return craftList.primaryCraft;
			}
			set {
				craftList.primaryCraft = value;
			}
		}

		public void renameCraft(OrganizerCraftEntity craft, string newName) {
			craftList.renameCraft(craft, newName);
		}

		public void deleteCraft(OrganizerCraftEntity craft) {
			craftList.deleteCraft(craft);
		}

		public CraftType craftType {
			get {
				return craftList.craftType;
			}
			set {
				if (craftList.craftType != value) {
					assignStateToFiltersDto();

					afterSettingsChanged = true;

					craftList.craftType = value;
				}
			}
		}

		private void assignStateToFiltersDto() {
			ProfileFilterSettingsDto filterDto = getFilterDtoFor(craftList.craftType);
			filter.assignCurrentFilterSettingsToDto(filterDto);
			managementTagsGroups.assignCurrentFilterSettingsToDto(filterDto);
			filterDto.restManagementTagsCollapsed = this.restTagsInManagementCollapsed;
		}

		public bool isCraftAlreadyLoadedInWorkspace(){
			return ksp.isCraftAlreadyLoadedInWorkspace ();
		}

		public void mergeCraftToWorkspace(OrganizerCraftEntity craft){
			ksp.mergeCraftToWorkspace (craft.craftFile);
		}

		public void loadCraftToWorkspace(OrganizerCraftEntity craft){
			ksp.loadCraftToWorkspace (craft.craftFile);
		}

		public void writeAllDirtySettings(){
			if (profileSettingsFileIsDirty) {
				ProfileSettingsDto dto = new ProfileSettingsDto ();
				if (doNotWriteTagSettingsToDisk) {
					dto.availableTags = settingsService.readProfileSettings().availableTags;
				} else {
					dto.availableTags = filter.availableTagsNames;
				}

				assignStateToFiltersDto();

				dto.allFilter = allFiltersDto;
				dto.selectedGuiStyle = _selectedGuiStyle;
				settingsService.writeProfileSettings(dto);

				markProfileSettingsAsNotDirty("Settings were just written to the disk");
			}
			if (!doNotWriteTagSettingsToDisk) {
				foreach (List<OrganizerCraftEntity> crafts in craftList.alreadyLoadedCrafts) {
					foreach (OrganizerCraftEntity craft in crafts) {
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

