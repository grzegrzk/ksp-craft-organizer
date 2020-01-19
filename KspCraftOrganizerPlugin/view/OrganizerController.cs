using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using KspNalCommon;

namespace KspCraftOrganizer {
	

	public class OrganizerController {
		private IKspAl ksp = IKspAlProvider.instance;

		private SettingsService settingsService = SettingsService.instance;
		private FileLocationService fileLocationService = FileLocationService.instance;
		private OrganizerControllerCraftList craftList;
		private OrganizerControllerFilter filter;
		public ManagementTagsGrouper managementTagsGroups;
		public OrganizerControllerStateManager stateManager { get; private set; }

		public bool restTagsInFilterCollapsed { get { return filter.restTagsCollapsed; } set { filter.restTagsCollapsed = value;}  }
		public bool restTagsInManagementCollapsed { get { return stateManager.isRestTagsCollapsedInManagement(); } set { stateManager.setRestTagsCollapsedInManagement(value); } }

		public Dictionary<string, bool> defaultTagsToAdd { get; private set; }
		public List<string> defaultTagsNotToAdd { get; private set; }

		public string currentSave { get; private set; }

		private CraftDataCacheContext craftCacheContext;


		public OrganizerController() {
			currentSave = ksp.NameOfSaveFolder;

			this.stateManager = new OrganizerControllerStateManager(this);
			this.managementTagsGroups = new ManagementTagsGrouper(this);
			this.craftList = new OrganizerControllerCraftList(this);
			this.filter = new OrganizerControllerFilter(this);
			this.defaultTagsToAdd = new Dictionary<string, bool>();
			this.craftCacheContext = new CraftDataCacheContext();

			this.filter.init();
		}

		public void refreshDefaultTagsToAdd() {
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
				return stateManager.selectedGuiStyle;
			}
			set {
				stateManager.selectedGuiStyle = value;
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
			return ksp.getCraftInfo(craftCacheContext, craftFilePath, fileLocationService.getCraftSettingsFileForCraftFile(craftFilePath));
		}


		public void clearFilters() {
			filter.clearFilters();
		}

		public void setGroupHasSelectedNoneFilter(string groupName, bool selectedNone) {
			filter.setGroupHasSelectedNoneFilter(groupName, selectedNone);
		}

		public FilterTagsGrouper filterTagsGrouper {
			get {
				return filter.usedTagsGrouper;
			}
		}

		public void unselectAllCrafts() {
			craftList.unselectAllCrafts();
		}

		public void update(string selectedSave, bool selectAll) {
			//
			//Filter & crafts affect each other:
			//
			// - The only tags that affect filtering are those assigned to the crafts currently on the list
			// - The only displayed crafts are those that pass filter
			//
			//
			//So at first we need to update filter and then update craft list.
			//
			if (this.currentSave != selectedSave) {
				this.currentSave = selectedSave;
				managementTagsGroups = new ManagementTagsGrouper(this);
				craftList.clearCaches("save folder changed");
				craftList.primaryCraft = null;
				filter.recreateAvailableTags();
				managementTagsGroups.update(availableTags);
				refreshDefaultTagsToAdd();
			}

			filter.update();
			craftList.update(selectAll, filter.filterChanged);
			managementTagsGroups.update(availableTags);
		}

		internal void onCraftSortingFunctionSelected(CraftSortFunction function) {
			craftList.addCraftSortingFunction(function);
		}

		internal ICraftSortFunction getCraftSortingFunction() {
			return craftList.getCraftSortingFunction();
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
				craftList.craftType = value;
			}
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

		public void writeAllDirtySettings() {
			stateManager.writeAllDirtySettings(doNotWriteTagSettingsToDisk);
		}

		public ICollection<List<OrganizerCraftEntity>> alreadyLoadedCrafts {
			get {
				return craftList.alreadyLoadedCrafts;
			}
		}

		public double availableFunds {
			get {
				return ksp.getAvailableFunds();
			}
		}

		public bool isCraftAlreadyExists(OrganizerCraftEntity craft) {
			string fileAfterSave = fileLocationService.getCraftSaveFilePathForShipName(craft.name);
			return File.Exists(fileAfterSave) && fileAfterSave != craft.craftFile;
		}


		public bool thisIsPrimarySave {
			get {
				return currentSave == ksp.NameOfSaveFolder;
			}
		}
	}
}

