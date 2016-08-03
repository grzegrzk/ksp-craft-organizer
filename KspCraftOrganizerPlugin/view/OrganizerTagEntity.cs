using System;

namespace KspCraftOrganizer
{

	public enum TagState{
		SET_IN_ALL,
		UNSET_IN_ALL,
		MIXED
	}

	public class OrganizerTagEntity{
		private string _name;
		private TagState _tagState = TagState.UNSET_IN_ALL;
		private OrganizerController service;
		private bool _selectedForFiltering = false;

		public OrganizerTagEntity(OrganizerController service, string name){
			this._name = name;
			this.service = service;
			this.inRenameMode = false;
		}

		public bool selectedForFiltering { 
			get {
				return _selectedForFiltering;
			}
			set{
				if (_selectedForFiltering != value) {
					_selectedForFiltering = value;
					service.markFilterAsChanged();
				}
			}
		}

		public int countOfSelectedCraftsWithThisTag { get ; set; }

		public string name{
			get {
				return _name;
			}
		}


		public TagState tagState{
			get {
				return _tagState;
			}
			set{
				if (_tagState != value) {
					_tagState = value;
					if (_tagState == TagState.SET_IN_ALL) {
						service.setTagToAllSelectedCrafts (this);
					}
					if (_tagState == TagState.UNSET_IN_ALL) {
						service.removeTagFromAllSelectedCrafts (this);
					}
				}
			}
		}

		public void updateTagState(){
			if (countOfSelectedCraftsWithThisTag == 0) {
				_tagState = TagState.UNSET_IN_ALL;
			} else if (countOfSelectedCraftsWithThisTag == service.selectedCraftsCount) {
				_tagState = TagState.SET_IN_ALL;
			} else {
				_tagState = TagState.MIXED;
			}
		}


		public bool hidden { get ; set ;}

		public string inNameEditMode { get ; set; }
		public bool inRenameMode { get ; set ;}
		public bool inDeleteMode { get ; set ;}
		public bool inOptionsMode { get ; set ;}
		public bool inHideUnhideMode { get ; set ;}


	}
}

