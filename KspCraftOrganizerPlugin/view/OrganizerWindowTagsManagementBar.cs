using UnityEngine;

namespace KspCraftOrganizer {
	
	public class OrganizerWindowTagsManagementBar {
		
		private static readonly string GUI_ID_ADD_NEW_TAG_TEXT = "GUI_ID_ADD_NEW_TAG_TEXT";

		private readonly OrganizerWindow parent;

		private Vector2 assingTagsScrollPosition;
		private string newTagText = "";
		private bool newTagWasJustAdded;
		private bool needsToUpdateSelectionInNewTag;

		public OrganizerWindowTagsManagementBar(OrganizerWindow parent) {
			this.parent = parent;
		}

		private OrganizerController model { get { return parent.model; } }

		public void drawManageTagsColumn() {
			using (new GUILayout.VerticalScope(GUILayout.Width(OrganizerWindow.MANAGE_TAGS_TOOLBAR_WIDTH))) {
				using (new GUILayout.HorizontalScope(GUILayout.ExpandWidth(false))) {
					if (model.selectedCraftsCount == 0) {
						GUILayout.Label("No craft selected, cannot assign tags", parent.warningLabelStyle);
					} else if (model.selectedCraftsCount == 1) {
						GUILayout.Label("Assign tags to selected craft. The changes will be applied immediately:");
					} else {
						GUILayout.Label("Assign tags to " + model.selectedCraftsCount + " selected crafts. The changes will be applied immediately:");
					}
					GUILayout.Space(20);
				}
				assingTagsScrollPosition = GUILayout.BeginScrollView(assingTagsScrollPosition);
				if (model.availableTags.Count > 0) {

					foreach (ManagementTagGroup tagGroup in model.managementTagsGroups.groups) {
						bool collapsed = tagGroup.collapsedInManagementView;
						if (collapsed) {
							if (GUILayout.Button("+ " + tagGroup.displayName, parent.skin.label)) {
								tagGroup.collapsedInManagementView = !collapsed;
							}
						} else {
							if (GUILayout.Button("- " + tagGroup.displayName + ":", parent.skin.label)) {
								tagGroup.collapsedInManagementView = !collapsed;
							}
							foreach (TagInGroup<OrganizerTagEntity> tag in tagGroup.tags) {
								drawSingleTag(tag.originalTag);

								GUILayout.Space(5);
							}
						}
					}

					if (model.managementTagsGroups.groups.Count > 0) {
						if (model.restTagsInManagementCollapsed) {
							if (GUILayout.Button("+ Rest tags", parent.skin.label)) {
								model.restTagsInManagementCollapsed = !model.restTagsInManagementCollapsed;
							}
						} else {
							if (GUILayout.Button("- Rest tags:", parent.skin.label)) {
								model.restTagsInManagementCollapsed = !model.restTagsInManagementCollapsed;
							}
						}
					} else {
						model.restTagsInManagementCollapsed = false;
					}
					if (!model.restTagsInManagementCollapsed) {
						foreach (OrganizerTagEntity tag in model.managementTagsGroups.restTags) {
							drawSingleTag(tag);

							GUILayout.Space(5);
						}
					}
				} else {
					GUILayout.Label("<No tags exist yet>");
				}
				GUILayout.Label("Add new tag:");
				using (new GUILayout.HorizontalScope(GUILayout.ExpandWidth(false))) {
					GUI.SetNextControlName(GUI_ID_ADD_NEW_TAG_TEXT);
					newTagText = GUILayout.TextField(newTagText, GUILayout.Width(160));

					focusNewTagTextFieldIfNeeded();

					bool addButtonClicked = GUILayout.Button("Add", GUILayout.ExpandWidth(false));
					string nameOfFocusedControl = GUI.GetNameOfFocusedControl();
					Event currentEven = Event.current;
					bool enterPressedOnInput =
						nameOfFocusedControl == GUI_ID_ADD_NEW_TAG_TEXT
						&& currentEven.type == EventType.KeyUp
						&& currentEven.keyCode == KeyCode.Return;
					if ((addButtonClicked || enterPressedOnInput) && newTagText.Trim() != "") {
						model.addAvailableTag(newTagText.Trim());
						newTagWasJustAdded = true;
					}
				}
				GUILayout.EndScrollView();
			}
		}

		private void drawSingleTag(OrganizerTagEntity tag) {
			using (new GUILayout.HorizontalScope()) {
				if (tag.inRenameMode) {
					tag.inNameEditMode = GUILayout.TextField(tag.inNameEditMode, GUILayout.ExpandWidth(false), GUILayout.Width(OrganizerWindow.MANAGE_TAGS_TOOLBAR_WIDTH - (parent.isKspSkin() ? 20 : 10) - 20));
				} else {
					bool prevState = tag.tagState == TagState.SET_IN_ALL;
					bool newState = parent.guiLayout_Toggle_OrigSkin(prevState, tag.name);
					if (prevState != newState) {
						tag.tagState = newState ? TagState.SET_IN_ALL : TagState.UNSET_IN_ALL;
					}
				}
				if (!tag.inDeleteMode && !tag.inRenameMode && !tag.inHideUnhideMode) {
					if (GUILayout.Button(tag.inOptionsMode ? "<" : ">", parent.originalSkin.button, GUILayout.ExpandWidth(false))) {
						tag.inOptionsMode = !tag.inOptionsMode;
					}
				}
			}
			if (tag.inOptionsMode) {
				using (new GUILayout.HorizontalScope(GUILayout.ExpandWidth(false))) {
					if (tag.inRenameMode) {
						GUILayout.Label("Rename:");
						if (GUILayout.Button("Ok")) {
							tag.inRenameMode = false;
							string nameInEdit = tag.inNameEditMode.Trim();
							if (nameInEdit != tag.name && nameInEdit != "") {
								string newTagName = nameInEdit;
								int tagCorrectionSuffixIndex = 2;
								while (model.doesTagExist(newTagName)) {
									newTagName = nameInEdit + "#" + tagCorrectionSuffixIndex;
									++tagCorrectionSuffixIndex;
								}
								model.renameTag(tag.name, newTagName);
							}
						}
						if (GUILayout.Button("Cancel")) {
							tag.inRenameMode = false;
						}
					} else if (tag.inDeleteMode) {
						GUILayout.Label("Delete tag?");
						if (GUILayout.Button("Yes")) {
							tag.inDeleteMode = false;
							model.removeTag(tag.name);
						}
						if (GUILayout.Button("No")) {
							tag.inDeleteMode = false;
						}
					} else {
						if (tag.inHideUnhideMode) {
							if (tag.hidden) {
								GUILayout.Label("Unhide tag?");
								if (GUILayout.Button("Yes")) {
									tag.inHideUnhideMode = false;
									tag.hidden = false;
								}
								if (GUILayout.Button("No")) {
									tag.inHideUnhideMode = false;
								}
							} else {
								GUILayout.Label("Hide tag?");
								if (GUILayout.Button("Yes")) {
									tag.inHideUnhideMode = false;
									tag.hidden = true;
								}
								if (GUILayout.Button("No")) {
									tag.inHideUnhideMode = false;
								}
							}
						} else {
							using (new GUILayout.VerticalScope(GUILayout.ExpandWidth(false))) {
								using (new GUILayout.HorizontalScope(GUILayout.ExpandWidth(false))) {
									if (GUILayout.Button("Delete")) {
										tag.inDeleteMode = true;
									}
									if (GUILayout.Button("Rename")) {
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
		}

		private void focusNewTagTextFieldIfNeeded() {
			if (Event.current.type == EventType.Repaint) {
				Rect newTagTextFieldRect = GUILayoutUtility.GetLastRect();
				if (needsToUpdateSelectionInNewTag) {
					int keyboardControl = GUIUtility.keyboardControl;
					TextEditor te = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
					if (te != null) {
						te.cursorIndex = newTagText.Length;
						te.selectIndex = te.cursorIndex;
						needsToUpdateSelectionInNewTag = false;
					}
				}
				if (newTagWasJustAdded) {
					newTagWasJustAdded = false;
					GUI.FocusControl(GUI_ID_ADD_NEW_TAG_TEXT);
					GUI.ScrollTo(newTagTextFieldRect);
					needsToUpdateSelectionInNewTag = true;
					newTagText = "";
				}
			}
		}
	}

}

