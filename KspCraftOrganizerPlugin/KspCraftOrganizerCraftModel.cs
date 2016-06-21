using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace KspCraftOrganizer
{

	public class CraftType{

		public static readonly CraftType SPH = new CraftType("SPH");
		public static readonly CraftType VAB = new CraftType("VAB");

		private string _directoryName;

		public CraftType(String directoryName){
			this._directoryName = directoryName;
		}
		public string directoryName { get { return _directoryName; } }	

		public string id { get { return _directoryName; } }	

		public override string ToString(){
			return id;
		}
	};



	public class CraftModel{
		private string _name;
		private bool _selectedPrimary;
		private bool _selected;
		private string _craftFile;
		private SortedList<string, string> _tags = new SortedList<string, string>();
		private readonly KspCraftOrganizerService service;

		public CraftModel(KspCraftOrganizerService service, string craftFile){
			this.service = service;
			this._craftFile = craftFile;
		}

		public string name { get { return _name; }}
		public void setNameInternal(string name){
			this._name = name;
		}
		public int cost { get ; set; }
		public string costToDisplay { get { 
				int cost = this.cost;
				if (cost > 10000000) {
					return roundDiv (mass, 1000000) + "M";
				}
				if (cost > 10000) {
					return roundDiv (mass, 1000) + "k";
				}
				return cost.ToString();
			}}
		public int partCount { get ; set; }
		public int mass { get ; set; }
		public int stagesCount {get ;set; }
		public string massToDisplay {
			get {
				int mass = this.mass;
				if (mass > 1000000000) {
					return roundDiv (mass, 1000000000) + "Mt";
				}
				if (mass > 1000000) {
					return roundDiv (mass, 1000000) + "kt";
				}
				if (mass > 10000) {
					return roundDiv (mass, 1000) + "t";
				}
				return mass + "kg";
			}
		}
		private string roundDiv(int toDiv, int  divisor){
			return Math.Round (((double)toDiv) / divisor, 2).ToString();
		}
		public void setSelectedPrimaryInternal(bool selectedPrimary){
			if (_selectedPrimary != selectedPrimary) {
				inRenameState = false;
			}
			_selectedPrimary = selectedPrimary;
		}
		public void setSelectedInternal(bool selected){
			_selected = selected;
		}
		public bool inRenameState { set ; get; }
		public bool isSelectedPrimary{ get {return _selectedPrimary; }}
		public bool isSelected { 
			get { 
				return _selected;
			} 
			set{
				if (_selected != value) {
					_selected = value;
					if (!value) {
						service.onOneCraftUnselected ();
					}
				}
			}
		}

		public ICollection<string> tags{
			get{
				return new ReadOnlyCollection<string>(_tags.Keys);
			}
		}

		public string tagsString{
			get{
				StringBuilder sb = new StringBuilder ();
				bool first = true;
				foreach (string tag in _tags.Values) {
					if (!first) {
						sb.Append (", ");
					}
					sb.Append (tag);
					first = false;
				}
				return sb.ToString ();
			}
		}

		public void addTag(string tag){
			if (!_tags.ContainsKey (tag)) {
				_tags.Add (tag, tag);
				craftSettingsFileIsDirty = true;
				service.markFilterAsChanged ();
			}
		}

		public void removeTag(string tag){
			if (_tags.ContainsKey (tag)) {
				_tags.Remove (tag);
				craftSettingsFileIsDirty = true;
				service.markFilterAsChanged ();
			}
		}

		public bool craftSettingsFileIsDirty { get ; set ;} 
		public bool containsTag(string tag){
			return _tags.ContainsKey (tag);
		}

		public float guiHeight { get; set ; }

		public string craftFile { 
			get{ 
				return _craftFile;
			} 
		}
		public void setCraftFileInternal(string craftFile){
			this._craftFile = craftFile;
		}
		public string craftSettingsFile{
			get{
				return Path.Combine(Path.GetDirectoryName(_craftFile), Path.GetFileNameWithoutExtension(_craftFile)) + ".crmgr";
			}
		}
	}
}

