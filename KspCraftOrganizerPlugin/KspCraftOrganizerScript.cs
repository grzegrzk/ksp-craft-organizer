using UnityEngine;
using System.Collections;
using KspCraftOrganizer;
using System.Collections.Generic;
using System;

//namespace KspCraftOrganizer{
	

[KSPAddon(KSPAddon.Startup.EditorAny, false)]
public class KspCraftOrganizerScript : MonoBehaviour {

	private static readonly string GUI_ID_ADD_NEW_TAG_TEXT = "GUI_ID_ADD_NEW_TAG_TEXT";

	private static readonly Rect ZERO_RECT = new Rect (0, 0, 0, 0);
	private static readonly string[] SPH_VAB = {"VAB", "SPH"};
	private static readonly CraftType[] SPH_VAB_STATES = {CraftType.VAB, CraftType.SPH};

	private static readonly int FILTER_TOOLBAR_WIDTH = 220;
	private static readonly int MANAGE_TAGS_TOOLBAR_WIDTH = 240;
	private static readonly int NO_MANAGE_TAGS_TOOLBAR_WIDTH = 0;
	private static readonly int WINDOW_WIDTH = 870;

	private static readonly int WRITE_TIME_THRESHOLD = 5;

	private Rect windowPos;
	private string textFilter = "";
	private string newTagText = "";
	private Vector2 tagScrollPosition;
	private Vector2 assingTagsScrollPosition;
	private Vector2 shipsScrollPosition;
	private bool showManageTagsToolbar = false;
	private string selectedCraftName = "";
	private bool selectAllFiltered;
	private float lastWriteTime = 0;

	private GUIStyle toggleButtonStyleFalse;
	private GUIStyle toggleButtonStyleTrue;
	private GUIStyle warningLabelStyle;
	private GUISkin skin;
	private GUISkin originalSkin;

	private bool newTagWasJustAdded;
	private bool needsToUpdateSelectionInNewTag;

	private KspCraftOrganizerService model = new KspCraftOrganizerService();

	public void Start () {
		COLogger.Log("Start");
		lastWriteTime = Time.realtimeSinceStartup;
		windowPos = new Rect(Mathf.Max(Screen.width / 10, 300), Screen.height / 10, WINDOW_WIDTH, Mathf.Max(Screen.height*8/10, 400));
	}
	
	void Update () {
		model.craftNameFilter = textFilter;
		model.updateFilteredCrafts ();
		if (model.primaryCraft == null) {
			this.selectedCraftName = "";
		}
		selectAllFiltered = model.updateSelectedCrafts(selectAllFiltered);
		if ((Time.realtimeSinceStartup - lastWriteTime) > WRITE_TIME_THRESHOLD) {
			model.writeAllDirtySettings ();
			this.lastWriteTime = Time.realtimeSinceStartup;
		}
	}

	private void WindowGUI(int WindowID){
		if (Event.current.type != EventType.Repaint && Event.current.type != EventType.Layout) {
			COLogger.Log (Event.current);
		}


		GUIStyle buttonStyle = skin.button;

		toggleButtonStyleFalse = new GUIStyle(buttonStyle);
		toggleButtonStyleFalse.hover = toggleButtonStyleFalse.normal;
		toggleButtonStyleFalse.active = toggleButtonStyleFalse.normal;

		toggleButtonStyleTrue = new GUIStyle(buttonStyle);
		toggleButtonStyleTrue.normal = toggleButtonStyleTrue.active;
		toggleButtonStyleTrue.hover = toggleButtonStyleTrue.active;

		warningLabelStyle = new GUIStyle (skin.label);
		warningLabelStyle.normal.textColor = Color.red;

		using (new GUILayout.VerticalScope ()) {

			DrawTopToolbar ();

			GUILayout.Space (10);

			using (new GUILayout.HorizontalScope ()) {
				
				DrawFilterColumn ();

					
				DrawCraftsList ();

				GUILayout.Space (10);

				if (showManageTagsToolbar) {
					DrawManageTagsColumn (); 
				} 
			}
//			if (!showManageTagsToolbar) {
				GUILayout.Space (10);
//			}

			DrawBottomBar ();
		}
		GUI.DragWindow();
	}

	void DrawTopToolbar ()
	{
		using (new GUILayout.HorizontalScope ()) {
			int sphOrVab = GUILayout.Toolbar (Array.IndexOf(SPH_VAB_STATES, model.craftType), SPH_VAB, GUILayout.Width (150), GUILayout.ExpandWidth (false));
			model.craftType =  SPH_VAB_STATES [sphOrVab];

			bool displayCraftsFilteredWarning = model.availableCrafts.Length > model.filteredCrafts.Length;
			if (!displayCraftsFilteredWarning) {
				GUI.BeginClip (ZERO_RECT);
			}
			GUILayout.Space (10);
			GUILayout.Label ("Not all crafts are displayed because of the filter", warningLabelStyle, GUILayout.ExpandWidth (false));
			if (GUILayout.Button ("Clear filter", GUILayout.ExpandWidth (false))) {
				ClearFilters ();
			}

			if (!displayCraftsFilteredWarning) {
				GUI.EndClip ();
			}

			GUILayout.FlexibleSpace ();
			string toggleManageTagsButtonLabel = showManageTagsToolbar ? "Manage Tags->" : "<-Manage Tags";
			if (GUILayout.Button (toggleManageTagsButtonLabel, GUILayout.ExpandWidth (false))) {
				showManageTagsToolbar = !showManageTagsToolbar;
			}
		}
	}

	void DrawFilterColumn ()
	{
		using (new GUILayout.VerticalScope (GUILayout.Width (FILTER_TOOLBAR_WIDTH))) {
			using (new GUILayout.VerticalScope (GUILayout.ExpandWidth (false))) {
				GUILayout.Label ("Filter crafts by name:", GUILayout.ExpandWidth (false));
				textFilter = GUILayout.TextField (textFilter, GUILayout.Width (FILTER_TOOLBAR_WIDTH - 10 - skin.verticalScrollbar.CalcScreenSize (skin.verticalScrollbar.CalcSize (new GUIContent (""))).x));
			}
			ICollection<TagModel> availableTags = model.availableTags;
			if (availableTags.Count > 0) {
				GUILayout.Space (15);
				GUILayout.Label ("Filter crafts by tag:");
				using (GUILayout.ScrollViewScope tagScrollScope = new GUILayout.ScrollViewScope (tagScrollPosition, false, false, GUILayout.MaxWidth (FILTER_TOOLBAR_WIDTH - 10))) {
					tagScrollPosition = tagScrollScope.scrollPosition;
					using (new GUILayout.VerticalScope (GUILayout.ExpandWidth (false))) {
						foreach (TagModel tag in availableTags) {
							tag.selectedForFiltering = GuiLayoutToggle (tag.selectedForFiltering, tag.name);
							GUILayout.Space (5);
						}
					}
				}
			}
		}
	}

	private bool GuiLayoutToggle(bool value, string name, params GUILayoutOption[] options){
		return GUILayout.Toggle(value, name, originalSkin.toggle, options);
	}

	void DrawCraftsList ()
	{

		int manageTagsWidth = showManageTagsToolbar ? MANAGE_TAGS_TOOLBAR_WIDTH : NO_MANAGE_TAGS_TOOLBAR_WIDTH;
		using (new GUILayout.VerticalScope (GUILayout.Width (WINDOW_WIDTH - manageTagsWidth - 30 - FILTER_TOOLBAR_WIDTH))) {
			shipsScrollPosition = GUILayout.BeginScrollView (shipsScrollPosition, GUILayout.MaxHeight (500));
			using (new GUILayout.VerticalScope (GUILayout.ExpandWidth (false))) {
				if (model.filteredCrafts.Length == 0) {
					if (model.availableCrafts.Length > 0) {
						GUILayout.Label ("<Nothing to display - change filter>");
					} else {
						GUILayout.Label ("<You have no ships to load>");
					}
				}
				foreach (CraftModel craft in model.filteredCrafts) {
					DrawSingleCraft (craft);
					GUILayout.Space (5);
				}
			}
			GUILayout.EndScrollView ();
		

//			if (showManageTagsToolbar) {
//				using (new GUILayout.HorizontalScope ()) {
			//					selectAllFiltered = GuiLayoutToggle (selectAllFiltered, "Select all", GUILayout.ExpandWidth (false));
//					GUILayout.FlexibleSpace ();
//					GUILayout.Label ("Filtered: " + model.filteredCrafts.Length + "/" + model.availableCrafts.Length + ", selected: " + model.selectedCraftsCount, GUILayout.ExpandWidth (false));	
//				}
//			}
		}
	}

	void DrawSingleCraft (CraftModel craft)
	{
		using (new GUILayout.HorizontalScope ()) {
			if (showManageTagsToolbar) {
				using (new GUILayout.VerticalScope (GUILayout.ExpandWidth (false), GUILayout.MaxWidth (20), GUILayout.Height (60))) {
					GUILayout.FlexibleSpace ();
					craft.isSelected = GuiLayoutToggle(craft.isSelected, "", GUILayout.ExpandWidth (false));
					GUILayout.FlexibleSpace ();
				}
			}
			using (new GUILayout.VerticalScope (GUILayout.ExpandWidth (true))) {
				GUIStyle thisCraftButtonStyle = craft.isSelectedPrimary ? toggleButtonStyleTrue : toggleButtonStyleFalse;
				float thisShipWidth;
				using (new GUI.ClipScope (ZERO_RECT)) {
					GUILayout.Button ("", thisCraftButtonStyle, GUILayout.Height(0));
					thisShipWidth = GUILayoutUtility.GetLastRect ().width;
				}
				float tagsWidth = thisShipWidth - 20.0f;
				string tagsString = "Tags: " + craft.tagsString;
				if (Event.current.type == EventType.Repaint) {
					craft.guiHeight = 60 + (craft.tags.Count > 0 ? CalcMultilineLabelHeight(tagsWidth, tagsString) : 0);
				}

				if (GUILayout.Button ("", thisCraftButtonStyle, GUILayout.Height (craft.guiHeight), GUILayout.ExpandHeight(false))) {
					
					if ((Event.current.modifiers & EventModifiers.Control) == 0 || !showManageTagsToolbar) {
						model.unselectAllCrafts ();
						this.selectedCraftName = craft.name;
						craft.isSelected = true;
						model.primaryCraft = craft;
					} else {
						if (craft.isSelected) {
							craft.isSelected = false;
						} else {
							this.selectedCraftName = craft.name;
							craft.isSelected = true;
							model.primaryCraft = craft;
						}
					}
				}
				Rect thisShipRect = GUILayoutUtility.GetLastRect ();
				using (new GUI.GroupScope (thisShipRect)) {
					DrawLabel (Color.yellow, 10, 10, craft.name);
					DrawLabel (skin.label.normal.textColor, 10, 30, "Parts: " + craft.partCount + ", Mass: " + craft.massToDisplay + ", Stages: " + craft.stagesCount);
					DrawLabel (Color.green, 250, 30, "Cost: " + craft.costToDisplay);
					if (craft.tags.Count > 0) {
						DrawMultilineLabel (skin.label.normal.textColor, tagsWidth, 10, 50, tagsString);
					}
				}
			}
		}
	}


	private float CalcMultilineLabelHeight(float maxWidth, string text){
		GUIStyle style = new GUIStyle (skin.label);
		return style.CalcHeight (new GUIContent (text), maxWidth);
	}

	private float DrawMultilineLabel(Color color, float maxWidth, int x, int y, string text){
		GUIStyle style = new GUIStyle (skin.label);
		style.normal.textColor = color;
		float height = style.CalcHeight (new GUIContent (text), maxWidth);
		Rect position = new Rect ();
		position.x = x;
		position.y = y;
		position.width = maxWidth;
		position.height = height;
		GUI.Label (position, text, style);
		return height;
	}
	private void DrawLabel(Color color, int x, int y, string text){
		GUIStyle style = new GUIStyle (skin.label);
		style.normal.textColor = color;
		Vector2 size = style.CalcSize(new GUIContent(text));
		Rect position = new Rect ();
		position.x = x;
		position.y = y;
		position.width = size.x;
		position.height = size.y;
		GUI.Label (position, text, style);
	}

	private void ClearFilters(){
		this.textFilter = "";
		foreach (TagModel tag in model.availableTags) {
			tag.selectedForFiltering = false;
		}
	}

	void DrawManageTagsColumn ()
	{
		using (new GUILayout.VerticalScope (GUILayout.Width (MANAGE_TAGS_TOOLBAR_WIDTH))) {
			using (new GUILayout.HorizontalScope (GUILayout.ExpandWidth (false))) {
				if (model.selectedCraftsCount == 0) {
					GUILayout.Label ("No craft selected, cannot assign tags", warningLabelStyle);
				} else if (model.selectedCraftsCount == 1) {
					GUILayout.Label ("Assign tags to selected craft:");
				} else {
					GUILayout.Label ("Assign tags to " + model.selectedCraftsCount + " selected crafts:");
				}
				GUILayout.Space (20);
			}
			assingTagsScrollPosition = GUILayout.BeginScrollView (assingTagsScrollPosition, GUILayout.MinHeight (70), GUILayout.MaxHeight (500));
			if (model.availableTags.Count > 0) {
				foreach (TagModel tag in model.availableTags) {

					DrawSingleTag (tag);

					GUILayout.Space (5);
				}
			}
			else {
				GUILayout.Label ("<No tags exist yet>");
			}
			GUILayout.Label ("Add new tag:");
			using (new GUILayout.HorizontalScope (GUILayout.ExpandWidth (false))) {
				GUI.SetNextControlName (GUI_ID_ADD_NEW_TAG_TEXT);
				newTagText = GUILayout.TextField (newTagText, GUILayout.Width (100));

				FocusNewTagTextFieldIfNeeded ();

				bool addButtonClicked = GUILayout.Button ("Add", GUILayout.ExpandWidth (false));
				string nameOfFocusedControl = GUI.GetNameOfFocusedControl ();
				Event currentEven = Event.current;
				bool enterPressedOnInput = 
					nameOfFocusedControl == GUI_ID_ADD_NEW_TAG_TEXT 
					&& currentEven.type == EventType.KeyUp 
					&& currentEven.keyCode == KeyCode.Return;
				if ((addButtonClicked || enterPressedOnInput) && newTagText.Trim() != "") {
					model.addAvailableTag (newTagText.Trim());
					newTagWasJustAdded = true;
				}
			}
			GUILayout.EndScrollView ();
		}
	}

	void DrawSingleTag (TagModel tag)
	{
		using (new GUILayout.HorizontalScope ()) {
			if (tag.inRenameMode) {
				tag.inNameEditMode = GUILayout.TextField (tag.inNameEditMode, GUILayout.ExpandWidth (false), GUILayout.Width (MANAGE_TAGS_TOOLBAR_WIDTH - 10 - 20));
			}
			else {
				bool prevState = tag.tagState == TagState.SET_IN_ALL;
				bool newState = GuiLayoutToggle(prevState, tag.name);
				if (prevState != newState) {
					tag.tagState = newState ? TagState.SET_IN_ALL : TagState.UNSET_IN_ALL;
				}
			}
			if (!tag.inDeleteMode && !tag.inRenameMode && !tag.inHideUnhideMode) {
				if (GUILayout.Button (tag.inOptionsMode ? "<" : ">", originalSkin.button, GUILayout.ExpandWidth (false))) {
					tag.inOptionsMode = !tag.inOptionsMode;
				}
			}
		}
		if (tag.inOptionsMode) {
			using (new GUILayout.HorizontalScope (GUILayout.ExpandWidth (false))) {
				if (tag.inRenameMode) {
					GUILayout.Label ("Rename:");
					if (GUILayout.Button ("Ok")) {
						tag.inRenameMode = false;
						string nameInEdit = tag.inNameEditMode.Trim ();
						if (nameInEdit != tag.name && nameInEdit != "") {
							string newTagName = nameInEdit;
							int tagCorrectionSuffixIndex = 2;
							while (model.doesTagExist (newTagName)) {
								newTagName = nameInEdit + "#" + tagCorrectionSuffixIndex;
								++tagCorrectionSuffixIndex;
							}
							model.renameTag (tag.name, newTagName);
						}
					}
					if (GUILayout.Button ("Cancel")) {
						tag.inRenameMode = false;
					}
				}
				else
					if (tag.inDeleteMode) {
						GUILayout.Label ("Delete tag?");
						if (GUILayout.Button ("Yes")) {
							tag.inDeleteMode = false;
							model.removeTag (tag.name);
						}
						if (GUILayout.Button ("No")) {
							tag.inDeleteMode = false;
						}
					}
					else
						if (tag.inHideUnhideMode) {
							if (tag.hidden) {
								GUILayout.Label ("Unhide tag?");
								if (GUILayout.Button ("Yes")) {
									tag.inHideUnhideMode = false;
									tag.hidden = false;
								}
								if (GUILayout.Button ("No")) {
									tag.inHideUnhideMode = false;
								}
							}
							else {
								GUILayout.Label ("Hide tag?");
								if (GUILayout.Button ("Yes")) {
									tag.inHideUnhideMode = false;
									tag.hidden = true;
								}
								if (GUILayout.Button ("No")) {
									tag.inHideUnhideMode = false;
								}
							}
						}
						else {
							using (new GUILayout.VerticalScope (GUILayout.ExpandWidth (false))) {
								using (new GUILayout.HorizontalScope (GUILayout.ExpandWidth (false))) {
									if (GUILayout.Button ("Delete")) {
										tag.inDeleteMode = true;
									}
									if (GUILayout.Button ("Rename")) {
										tag.inRenameMode = true;
										tag.inNameEditMode = tag.name;
									}
									//										if (GUILayout.Button (tag.hidden ? "Unhide" : "Hide")) {
									//											tag.inHideUnhideMode = true;
									//										}
								}
							}
						}
			}
		}
	}

	void FocusNewTagTextFieldIfNeeded ()
	{
		if (Event.current.type == EventType.Repaint) {
			Rect newTagTextFieldRect = GUILayoutUtility.GetLastRect ();
			if (needsToUpdateSelectionInNewTag) {
				int keyboardControl = GUIUtility.keyboardControl;
				TextEditor te = (TextEditor)GUIUtility.GetStateObject (typeof(TextEditor), GUIUtility.keyboardControl);
				if (te != null) {
					te.cursorIndex = newTagText.Length;
					te.selectIndex = te.cursorIndex;
					needsToUpdateSelectionInNewTag = false;
				}
			}
			if (newTagWasJustAdded) {
				newTagWasJustAdded = false;
				GUI.FocusControl (GUI_ID_ADD_NEW_TAG_TEXT);
				GUI.ScrollTo (newTagTextFieldRect);
				needsToUpdateSelectionInNewTag = true;
			}
		}
	}

	void DrawBottomBar ()
	{
		int bottomButtonsWidth = 70;
		using (new GUILayout.HorizontalScope ()) {
			GUILayout.Space (FILTER_TOOLBAR_WIDTH);
			if (model.primaryCraft != null) {
				if (model.primaryCraft.inRenameState) {

					selectedCraftName = GUILayout.TextField (selectedCraftName, GUILayout.Width(200));

					if (GUILayout.Button ("Rename")) {
						string newName = selectedCraftName.Trim ();
						if (newName != "") {
							model.renameCraft (model.primaryCraft, newName);
						}
						model.primaryCraft.inRenameState = false;
					}
					if (GUILayout.Button ("Cancel")) {
						model.primaryCraft.inRenameState = false;
					}
				} else {
					if (GUILayout.Button ("Rename", GUILayout.ExpandWidth (true), GUILayout.Width (bottomButtonsWidth))) {
						model.primaryCraft.inRenameState = true;
						selectedCraftName = model.primaryCraft.name;
					}
				}

				GUILayout.FlexibleSpace ();

				if(model.isCraftAlreadyLoadedInWorkspace()){
					if (GUILayout.Button ("Merge", GUILayout.ExpandWidth (true), GUILayout.Width (bottomButtonsWidth))) {
						model.mergeCraftToWorkspace (model.primaryCraft);
					}
				}
				if (GUILayout.Button ("Load", GUILayout.ExpandWidth (true), GUILayout.Width (bottomButtonsWidth))) {
					model.loadCraftToWorkspace (model.primaryCraft);
				}
				if (GUILayout.Button ("Cancel", GUILayout.ExpandWidth (true), GUILayout.Width (bottomButtonsWidth))) {
					
				}

			}else {
				using (new GUILayout.HorizontalScope ()) {
					GUILayout.FlexibleSpace ();
					if (GUILayout.Button ("Cancel", GUILayout.ExpandWidth (true), GUILayout.Width (bottomButtonsWidth))) {
						COLogger.Log ("Load2!");
					}
				}
			}
		} 
	}
	public void OnGUI(){

		originalSkin = GUI.skin;
		skin = IKspCraftorganizerDaoProvider.instance.guiSkin();
		GUI.skin = skin;

//		windowPos = GUILayout.Window(1986767, windowPos, WindowGUI, "Load craft", GUILayout.MinWidth(700));	

		windowPos = GUI.ModalWindow(1986767, windowPos, WindowGUI, "Manage Crafts " + windowPos);	

//			windowPos.width = WINDOW_WIDTH + (showTagsToolbar ? TAGS_TOOLBAR_WIDTH : 0);
	}
}
//}