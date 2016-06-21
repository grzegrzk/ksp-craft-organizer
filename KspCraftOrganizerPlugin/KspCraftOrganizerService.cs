using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace KspCraftOrganizer
{

	public class KspCraftOrganizerService
	{
		private IKspCraftorganizerDao dao = IKspCraftorganizerDaoProvider.instance;
		private CraftModel[] cachedAvailableCrafts;
		private CraftModel[] cachedFilteredCrafts;
		private SortedList<string, TagModel> _availableTags;
		private CraftModel _primaryCraft;
		private bool _selectAllFiltered;
		private bool _forceUncheckSelectAllFiltered;
		private string _craftNameFilter;
		private bool filterChanged;
		private int cachedSelectedCraftsCount;
		private CraftType _craftType;
		private Dictionary<CraftType, CraftModel[]> craftTypeToAvailableCraftsLazy = new Dictionary<CraftType, CraftModel[]> ();
		private bool profileSettingsFileIsDirty;

		public KspCraftOrganizerService(){
			_craftType = dao.getCurrentCraftType();
			ProfileSettingsDto profileSettigngs = dao.readProfileSettings (profileSettingsFile);
			_availableTags = new SortedList<string, TagModel>();
			foreach(string tagName in profileSettigngs.availableTags){
				addAvailableTag (tagName);
			}
			craftNameFilter = "";
			profileSettingsFileIsDirty = false;
		}

		private string profileSettingsFile { get { return Path.Combine (dao.getBaseCraftDirectory (), "profile_settings.pcrmgr"); } }

		public ICollection<TagModel> availableTags{
			get{
				return new ReadOnlyCollection<TagModel>(new List<TagModel>(_availableTags.Values));
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
			profileSettingsFileIsDirty = true;
		}

		public void updateFilteredCrafts(){
			if (filterChanged) {
				this.cachedFilteredCrafts = null;
			}
		}

		public CraftModel[] filteredCrafts{
			get {
				if(cachedFilteredCrafts == null){
					cachedFilteredCrafts = createFilteredCrafts();
				}
				return cachedFilteredCrafts;
			}
		}

		private CraftModel[] createFilteredCrafts(){
			List<CraftModel> filtered = new List<CraftModel> ();
			string upperFilter = craftNameFilter.ToUpper();
			foreach(CraftModel craft in availableCrafts){
				if (doesCraftPassFilter (upperFilter, craft)) {
					filtered.Add (craft);
				} else {
					if (craft.isSelectedPrimary) {
						primaryCraft = null;
					}
					craft.setSelectedInternal (false);
				}
			}
			return filtered.ToArray();
		}

		bool doesCraftPassFilter (string upperFilter, CraftModel craft)
		{
			bool pass = true;
			pass = pass && ( craft.name.ToUpper ().Contains (upperFilter) || craftNameFilter == "");
			foreach (TagModel tag in _availableTags.Values) {
				if (tag.selectedForFiltering) {
					pass = pass && (craft.containsTag(tag.name));
				}
			}
			return pass;
		}

		public CraftModel[] availableCrafts{
			get {
				if(cachedAvailableCrafts == null){
					cachedAvailableCrafts = getCraftsOfType (_craftType);
				}
				return cachedAvailableCrafts;
			}
		}
		private CraftModel[] getCraftsOfType(CraftType type){
			if (!craftTypeToAvailableCraftsLazy.ContainsKey(type)) {
				craftTypeToAvailableCraftsLazy.Add(type, fetchAvailableCrafts(type));
			}
			return craftTypeToAvailableCraftsLazy[type];
		}

		private CraftModel[] fetchAvailableCrafts(CraftType type){
			COLogger.Log ("fetching '" + type + "' crafts from disk");
			string craftDirectory = Path.Combine(dao.getBaseCraftDirectory(), type.directoryName);
			string[] craftFiles = Directory.GetFiles (craftDirectory, "*.craft");
			CraftModel[] toRet = new CraftModel[craftFiles.Length];

			for (int i = 0; i < craftFiles.Length; ++i) {
				toRet[i] = new CraftModel(this, craftFiles[i]);
				toRet[i].setNameInternal(Path.GetFileNameWithoutExtension (craftFiles [i]));
				CraftDaoDto craft = dao.getCraftInfo (craftFiles [i]);

				toRet [i].setNameInternal(craft.name);
				toRet [i].cost = craft.cost;
				toRet [i].mass = craft.mass;
				toRet [i].partCount = craft.partCount;
				toRet [i].stagesCount = craft.stagesCount;

				PerCraftSettingsDto craftSettings = dao.readCraftSettings (toRet[i].craftSettingsFile);
				foreach (string craftTag in craftSettings.selectedTags) {
					addAvailableTag (craftTag);
					toRet [i].addTag (craftTag);
				}
				toRet [i].craftSettingsFileIsDirty = false;
			}
			return toRet;
		}

		public CraftModel primaryCraft {
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

		public void renameCraft(CraftModel model, string newName){
			string oldOrganizerFile = model.craftSettingsFile;
			string newFile = Path.Combine (Path.GetDirectoryName (model.craftFile), newName + ".craft");
			File.Move (model.craftFile, newFile);
			model.setCraftFileInternal (newFile);
			dao.renameCraft (model.craftFile, newName);
			model.setNameInternal (newName);
			if (File.Exists (oldOrganizerFile)) {
				File.Move (oldOrganizerFile, model.craftSettingsFile);
			}
		}

		public void unselectAllCrafts(){
			foreach (CraftModel craft in filteredCrafts) {
				craft.isSelected = false;
			}
		}

		public bool updateSelectedCrafts (bool selectAll){
			if (_forceUncheckSelectAllFiltered) {
				_selectAllFiltered = false;
				_forceUncheckSelectAllFiltered = false;
			} else {
				if (_selectAllFiltered != selectAll || selectAll) {
					foreach (CraftModel craft in filteredCrafts) {
						craft.isSelected = selectAll;
					}
					_selectAllFiltered = selectAll;
				}
			}
			cachedSelectedCraftsCount = 0;
			foreach (TagModel tag in _availableTags.Values) {
				tag.countOfSelectedCraftsWithThisTag = 0;
			}
			foreach(CraftModel craft in filteredCrafts){
				if (craft.isSelected) {
					cachedSelectedCraftsCount += 1;
					foreach (string tag in craft.tags) {
						++_availableTags [tag].countOfSelectedCraftsWithThisTag;
					}
				}
			}
			foreach (TagModel tag in _availableTags.Values) {
				tag.updateTagState ();
			}

			return _selectAllFiltered;
		}


		public void onOneCraftUnselected(){
			_forceUncheckSelectAllFiltered = true;
		}

		public int selectedCraftsCount { 
			get {
				return cachedSelectedCraftsCount;
			} 
		}

		public void setTagToAllSelectedCrafts(TagModel tag){
			foreach (CraftModel craft in filteredCrafts) {
				if (craft.isSelected) {
					craft.addTag (tag.name);
				}
			}
		}

		public void removeTagFromAllSelectedCrafts(TagModel tag){
			foreach (CraftModel craft in filteredCrafts) {
				if (craft.isSelected) {
					craft.removeTag (tag.name);
				}
			}
		}

		public bool doesTagExist(string tag){
			return _availableTags.ContainsKey (tag);
		}

		public TagModel addAvailableTag(string newTag){
			if (!_availableTags.ContainsKey (newTag)) {
				_availableTags.Add (newTag, new TagModel (this, newTag));
				profileSettingsFileIsDirty = true;
			}
			return _availableTags [newTag];
		}

		public void removeTag(string tag){
			if (_availableTags.ContainsKey (tag)) {
				foreach (CraftModel craft in getCraftsOfType(CraftType.SPH)) {
					craft.removeTag (tag);
				}
				foreach (CraftModel craft in getCraftsOfType(CraftType.VAB)) {
					craft.removeTag (tag);
				}
				_availableTags.Remove (tag);
			}
		}

		public void renameTag(string oldName, string newName){
			foreach (CraftModel craft in getCraftsOfType(CraftType.SPH)) {
				if (craft.containsTag (oldName)) {
					craft.addTag (newName);
					craft.removeTag (oldName);
				}
			}
			foreach (CraftModel craft in getCraftsOfType(CraftType.VAB)) {
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
				TagModel newTag = new TagModel (this, newName);
				newTag.selectedForFiltering = selectForFilterAfterInsertion;
				_availableTags.Add (newName, newTag);
			}
		}


		public CraftType craftType {
			get {
				return _craftType;
			}

			set {
				if (value != _craftType) {
					_craftType = value;
					clearCaches ();
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
			return dao.isCraftAlreadyLoadedInWorkspace ();
		}

		public void mergeCraftToWorkspace(CraftModel craft){
			dao.mergeCraftToWorkspace (craft.craftFile);
		}
		public void loadCraftToWorkspace(CraftModel craft){
			dao.loadCraftToWorkspace (craft.craftFile);
		}

		public void writeAllDirtySettings(){
			if (profileSettingsFileIsDirty) {
				ProfileSettingsDto dto = new ProfileSettingsDto ();
				dto.availableTags = new string[_availableTags.Count];
				int i = 0;
				List<string> selectedTags = new List<string> ();
				foreach(TagModel tag in _availableTags.Values){
					dto.availableTags [i] = tag.name;
					if (tag.selectedForFiltering) {
						selectedTags.Add (tag.name);
					}
					++i;
				}
				dto.selectedFilterTags = selectedTags.ToArray();
				dao.writeProfileSettings (profileSettingsFile, dto);


				profileSettingsFileIsDirty = false;
			}
			foreach (CraftModel[] crafts in craftTypeToAvailableCraftsLazy.Values) {
				foreach (CraftModel craft in crafts) {
					if (craft.craftSettingsFileIsDirty) {
						PerCraftSettingsDto dto = new PerCraftSettingsDto ();
						dto.selectedTags = craft.tags;
						dao.writeCraftSettings (craft.craftSettingsFile, dto);
						craft.craftSettingsFileIsDirty = false;
					}
				}
			}
		}

	}
}

