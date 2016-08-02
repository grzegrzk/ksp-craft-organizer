using UnityEngine;
using System.Collections.Generic;
using System;

namespace KspCraftOrganizer {
	public class OrganizerWindow : BaseWindow {

		private static readonly string[] SPH_VAB = { "VAB", "SPH" };
		private static readonly CraftType[] SPH_VAB_STATES = { CraftType.VAB, CraftType.SPH };



		private static readonly string TEXT_FILTER_CONTROL_NAME = "OrganizerWindow_TEXT_FILTER_CONTROL_NAME";

		public static readonly int FILTER_TOOLBAR_WIDTH = 220;
		public static readonly int MANAGE_TAGS_TOOLBAR_WIDTH = 240;
		public static readonly int NO_MANAGE_TAGS_TOOLBAR_WIDTH = 0;
		public static readonly int MIN_WINDOW_WIDTH = 870;

		private Vector2 tagScrollPosition;
		public bool showManageTagsToolbar { get; set; }
		public string selectedCraftName = "";
		public bool selectAllFiltered;
		private int selectedGuiSkin = 1;
		private Rect skinToolbarRect;
		private bool extendedCancel;
		private bool displayQuestionAboutExtendedQuestion;

		private GUIStyle _toggleButtonStyleFalse;
		private GUIStyle _toggleButtonStyleTrue;
		private GUIStyle _warningLabelStyle;
		private GUIStyle tooltipBackgroundStyle;
		private OrganizerService _modelLazy;

		public OrganizerService model { get {
				if (_modelLazy == null) { 
					_modelLazy = new OrganizerService();
				} 
				return _modelLazy; 
			} }

		public GUIStyle warningLabelStyle { get { return _warningLabelStyle; } }
		public GUIStyle toggleButtonStyleFalse { get { return _toggleButtonStyleFalse; } }
		public GUIStyle toggleButtonStyleTrue { get { return _toggleButtonStyleTrue; } }

		private OrganizerWindowTagsManagementBar tagsManagementBar;
		private OrganizerWindowCraftList craftList;

		private EditorListenerService editorListenerService = EditorListenerService.instance;

		private ShouldCurrentCraftBeSavedQuestionWindow shouldCurrentCraftBeSavedWindow;

		public OrganizerWindow(ShouldCurrentCraftBeSavedQuestionWindow shouldCurrentCraftBeSavedWindow) : base("Load craft"){			
			tagsManagementBar = new OrganizerWindowTagsManagementBar(this);
			craftList = new OrganizerWindowCraftList(this);
			this.shouldCurrentCraftBeSavedWindow = shouldCurrentCraftBeSavedWindow;
		}

		protected override float getWindowWidthOnScreen(Rect pos) {
			return Math.Min(Math.Min(Math.Max(Screen.width * 9/10, MIN_WINDOW_WIDTH), Screen.height * 1.5f), 1300 * guiRawScale);
		}

		override protected float getMinWindowInnerWidth(Rect pos) {
			return MIN_WINDOW_WIDTH;
		}

		override public void displayWindow() {
			base.displayWindow();

			EditorListenerService.instance.fireEventIfShipHasBeenSaved();
			selectedGuiSkin = Array.IndexOf(GuiStyleOption.SKIN_STATES, model.selectedGuiStyle);
			_modelLazy = null;
		}

		override public void update() {
			base.update();
			if (windowDisplayed) {
				model.update(selectAllFiltered);
				this.selectAllFiltered = model.selectAllFiltered;

				//model.updateFilteredCrafts();
				if (model.primaryCraft == null) {
					this.selectedCraftName = "";
				}
				model.selectedGuiStyle = GuiStyleOption.SKIN_STATES[selectedGuiSkin];
				//selectAllFiltered = model.updateSelectedCrafts(selectAllFiltered);
				//model.updateUsedTags();
				guiStyleOption = GuiStyleOption.SKIN_STATES[selectedGuiSkin];
			}
		}

		override protected void windowGUI(int WindowID) {
			GUIStyle buttonStyle = skin.button;

			_toggleButtonStyleFalse = new GUIStyle(buttonStyle);
			toggleButtonStyleFalse.hover = toggleButtonStyleFalse.normal;
			toggleButtonStyleFalse.active = toggleButtonStyleFalse.normal;

			_toggleButtonStyleTrue = new GUIStyle(buttonStyle);
			toggleButtonStyleTrue.normal = toggleButtonStyleTrue.active;
			toggleButtonStyleTrue.hover = toggleButtonStyleTrue.active;

			_warningLabelStyle = new GUIStyle(skin.label);
			_warningLabelStyle.normal.textColor = Color.red;

			using (new GUILayout.VerticalScope()) {

				drawTopToolbar();

				GUILayout.Space(10);

				using (new GUILayout.HorizontalScope()) {

					drawFilterColumn();


					craftList.drawCraftsList();

					GUILayout.Space(10);

					if (showManageTagsToolbar) {
						tagsManagementBar.drawManageTagsColumn();
					}
				}
				//			if (!showManageTagsToolbar) {
				GUILayout.Space(10);
				//			}

				drawBottomBar();
			}

			drawCraftTooltip();

			GUI.DragWindow();
		}

		private void drawTopToolbar() {
			using (new GUILayout.HorizontalScope()) {
				int sphOrVab = GUILayout.Toolbar(Array.IndexOf(SPH_VAB_STATES, model.craftType), SPH_VAB, GUILayout.Width(150), GUILayout.ExpandWidth(false));
				model.craftType = SPH_VAB_STATES[sphOrVab];

				bool displayCraftsFilteredWarning = model.craftsAreFiltered;
				if (!displayCraftsFilteredWarning) {
					GUI.BeginClip(Globals.ZERO_RECT);
				}
				GUILayout.Space(10);
				GUILayout.Label("The filter modifies craft list", warningLabelStyle, GUILayout.ExpandWidth(false));
				if (GUILayout.Button("Clear filter", GUILayout.ExpandWidth(false))) {
					clearFilters();
				}

				if (!displayCraftsFilteredWarning) {
					GUI.EndClip();
				}

				GUILayout.FlexibleSpace();
				string toggleManageTagsButtonLabel = showManageTagsToolbar ? "Manage Tags->" : "<-Manage Tags";
				if (GUILayout.Button(toggleManageTagsButtonLabel, GUILayout.ExpandWidth(false))) {
					showManageTagsToolbar = !showManageTagsToolbar;
				}
			}
		}

		private void drawFilterColumn() {
			using (new GUILayout.VerticalScope(GUILayout.Width(FILTER_TOOLBAR_WIDTH))) {
				using (new GUILayout.VerticalScope(GUILayout.ExpandWidth(false))) {
					GUILayout.Label("Filter crafts by name:", GUILayout.ExpandWidth(false));
					GUI.SetNextControlName(TEXT_FILTER_CONTROL_NAME);
					model.craftNameFilter = GUILayout.TextField(model.craftNameFilter, GUILayout.Width(FILTER_TOOLBAR_WIDTH - 10 - skin.verticalScrollbar.CalcScreenSize(skin.verticalScrollbar.CalcSize(new GUIContent(""))).x));
					if (justAfterWindowDisplay) {
						GUI.FocusControl(TEXT_FILTER_CONTROL_NAME);
					}
				}

				if (model.usedTags.Count > 0) {
					OrganizerServiceFilterGroupsOfTagModel filterGroups = model.filterGroups;
					GUILayout.Space(15);
					GUILayout.Label("Filter crafts by tag:");
					using (GUILayout.ScrollViewScope tagScrollScope = new GUILayout.ScrollViewScope(tagScrollPosition, false, false, GUILayout.MaxWidth(FILTER_TOOLBAR_WIDTH - 10))) {
						tagScrollPosition = tagScrollScope.scrollPosition;
						using (new GUILayout.VerticalScope(GUILayout.ExpandWidth(false))) {
							foreach (FilterTagGroup tagGroup in filterGroups.groups) {
								bool collapsed = tagGroup.collapsedInFilterView;
								if (!collapsed) {
									if (GUILayout.Button("- " + tagGroup.displayName + ":", this.skin.label)) {
										tagGroup.collapsedInFilterView = !collapsed;
										model.markProfileSettingsAsDirty("group collapsed state changed");
									}
									if (tagGroup.isYesNoGroup) {
										OrganizerTagModel tag = tagGroup.firstTag.originalTag;
										int thisYesNoTagState;
										if (tag.selectedForFiltering) {
											thisYesNoTagState = 0;
										} else if (filterGroups.hasGroupSelectedNoneFilter(tagGroup.name)) {
											thisYesNoTagState = 1;
										} else {
											thisYesNoTagState = 2;
										}
										thisYesNoTagState = guiLayout_Radio_OrigSkin(thisYesNoTagState, 0, " " + tagGroup.displayName);
										thisYesNoTagState = guiLayout_Radio_OrigSkin(thisYesNoTagState, 1, " not " + tagGroup.displayName);
										thisYesNoTagState = guiLayout_Radio_OrigSkin(thisYesNoTagState, 2, " all");
										tag.selectedForFiltering = thisYesNoTagState == 0;
										model.setGroupHasSelectedNoneFilter(tagGroup.name, thisYesNoTagState == 1);
									} else {
										foreach (TagInGroup<OrganizerTagModel> tagInGroup in tagGroup.tags) {
											OrganizerTagModel tag = tagInGroup.originalTag;
											tag.selectedForFiltering = guiLayout_Toggle_OrigSkin(tag.selectedForFiltering, " " + tagInGroup.tagDisplayName);
											GUILayout.Space(5);
										}
										bool noneSelected = guiLayout_Toggle_OrigSkin(filterGroups.hasGroupSelectedNoneFilter(tagGroup.name), " <with no tag assigned>");

										model.setGroupHasSelectedNoneFilter(tagGroup.name, noneSelected);

									}
								} else {
									if(GUILayout.Button("+ " + tagGroup.displayName, this.skin.label)){
										tagGroup.collapsedInFilterView = !collapsed;
										model.markProfileSettingsAsDirty("group collapsed state changed");
									}
								}
							}
							if (filterGroups.restTags.Count > 0) {
								if (filterGroups.groups.Count > 0) {
									GUILayout.Label("Rest tags:");
								} else {
									GUILayout.Label("Tags:");
								}
								foreach (OrganizerTagModel tag in filterGroups.restTags) {
									tag.selectedForFiltering = guiLayout_Toggle_OrigSkin(tag.selectedForFiltering, " " + tag.name);
									GUILayout.Space(5);
								}
							}
						}
					}
				}
			}
		}

		public int guiLayout_Radio_OrigSkin(int state, int thisIndex, string name, params GUILayoutOption[] options) {
			bool oldIsSelected = state == thisIndex;
			bool newIsSelected = GUILayout.Toggle(oldIsSelected, name, originalSkin.toggle, options);
			if (newIsSelected || oldIsSelected) {
				return thisIndex;
			}
			return state;
		}
		public bool guiLayout_Toggle_OrigSkin(bool value, string name, params GUILayoutOption[] options) {
			return GUILayout.Toggle(value, name, originalSkin.toggle, options);
		}

		private void clearFilters() {
			model.clearFilters();
		}

		public bool isKspSkin() {
			return GuiStyleOption.SKIN_STATES[selectedGuiSkin] == GuiStyleOption.Ksp;
		}

		private void drawBottomBar() {
			int bottomButtonsWidth = 70;
			using (new GUILayout.VerticalScope()) {
				using (new GUILayout.HorizontalScope()) {
					GUILayout.Label("Skin:");
					selectedGuiSkin = GUILayout.Toolbar(selectedGuiSkin, GuiStyleOption.SKIN_DISPLAY_OPTIONS);
					if (Event.current.type == EventType.Repaint) {
						skinToolbarRect = GUILayoutUtility.GetLastRect();
					}
					GUILayout.Space(Math.Max(FILTER_TOOLBAR_WIDTH - (skinToolbarRect.x + skinToolbarRect.width), 0));
					if (model.primaryCraft != null) {
						if (!model.primaryCraft.isStock && !model.primaryCraft.isAutosaved) {
							if (model.primaryCraft.inRenameState) {

								selectedCraftName = GUILayout.TextField(selectedCraftName, GUILayout.Width(200));

								if (GUILayout.Button("Rename")) {
									string newName = selectedCraftName.Trim();
									if (newName != "") {
										model.renameCraft(model.primaryCraft, newName);
									}
									model.primaryCraft.inRenameState = false;
								}
								if (GUILayout.Button("Cancel")) {
									model.primaryCraft.inRenameState = false;
								}
							} else if (model.primaryCraft.inDeleteState) {
								GUILayout.Label("Delete " + model.primaryCraft.name + "?");
								if (GUILayout.Button("Delete")) {
									model.deleteCraft(model.primaryCraft);
									model.primaryCraft.inDeleteState = false;
								}
								if (GUILayout.Button("Cancel")) {
									model.primaryCraft.inDeleteState = false;
								}
							} else {
								if (GUILayout.Button("Rename", GUILayout.ExpandWidth(true), GUILayout.Width(bottomButtonsWidth))) {
									model.primaryCraft.inRenameState = true;
									selectedCraftName = model.primaryCraft.name;
								}
								if (GUILayout.Button("Delete", GUILayout.ExpandWidth(true), GUILayout.Width(bottomButtonsWidth))) {
									model.primaryCraft.inDeleteState = true;
									selectedCraftName = model.primaryCraft.name;
								}
							}
						}
						GUILayout.FlexibleSpace();


						if (model.isCraftAlreadyLoadedInWorkspace()) {
							if (GUILayout.Button("Merge", GUILayout.ExpandWidth(true), GUILayout.Width(bottomButtonsWidth))) {
								model.mergeCraftToWorkspace(model.primaryCraft);
								hideWindow();
							}
						}
						if (GUILayout.Button("Load", GUILayout.ExpandWidth(true), GUILayout.Width(bottomButtonsWidth))) {
							load();
						}
						drawCancelInBottomBar(bottomButtonsWidth);

					} else {
						using (new GUILayout.HorizontalScope()) {
							GUILayout.FlexibleSpace();
							drawCancelInBottomBar(bottomButtonsWidth);
						}
					}
				}
				if (extendedCancel) {
					using (new GUILayout.HorizontalScope()) {
						GUILayout.FlexibleSpace();
						if (displayQuestionAboutExtendedQuestion) {
							GUILayout.Label("Are you sure?");
							if (GUILayout.Button("Discard")) {
								extendedCancel = false;
								model.doNotWriteTagSettingsToDisk = true;
								hideWindow();
							}
							if (GUILayout.Button("Cancel")) {
								displayQuestionAboutExtendedQuestion = false;
								extendedCancel = false;
							}
						} else {
							if (GUILayout.Button("Close & Discard all changes made in tags", GUILayout.ExpandWidth(false))) {
								displayQuestionAboutExtendedQuestion = true;
							}
						}
					}
				}
			}
		}

		public void load() {
			if (editorListenerService.isModifiedSinceSave) {
				shouldCurrentCraftBeSavedWindow.fileToLoad = model.primaryCraft.craftFile;
				shouldCurrentCraftBeSavedWindow.displayWindow();
			} else {
				model.loadCraftToWorkspace(model.primaryCraft);
			}
			hideWindow();
		}

		private void drawCancelInBottomBar(int bottomButtonsWidth) {
			if (GUILayout.Button("Cancel", GUILayout.ExpandWidth(true), GUILayout.Width(bottomButtonsWidth))) {
				hideWindow();
			}
			if (extendedCancel) {
				if (GUILayout.Button("<<", GUILayout.ExpandWidth(true))) {
					extendedCancel = false;
					displayQuestionAboutExtendedQuestion = false;
				}
			} else {
				if (GUILayout.Button(">>", GUILayout.ExpandWidth(true))) {
					extendedCancel = true;
					displayQuestionAboutExtendedQuestion = false;
				}
			}
			
		}

		override public void hideWindow() {
			base.hideWindow();
			model.writeAllDirtySettings();
		}



		private void drawCraftTooltip() {
			if (GUI.tooltip != null && GUI.tooltip != "" && GUI.tooltip.StartsWith("!!craft")) {
				int craftIndex = int.Parse(GUI.tooltip.Substring("!!craft".Length));
				OrganizerCraftModel tooltipCraft = model.filteredCrafts[craftIndex];

				GUIStyle nameStyle = new GUIStyle(skin.label);
				GUIStyle descStyle = new GUIStyle(skin.label);
				float tooltipTextWidth = 300;

				ParagraphBoxDrawer drawer = new ParagraphBoxDrawer(tooltipTextWidth);
				drawer.addParagraph(tooltipCraft.name, nameStyle);
				string[] paragraphs = tooltipCraft.description.Split(new string[] { "\n", "¨¨" }, StringSplitOptions.None);
				foreach (string p in paragraphs) {
					drawer.addParagraph(p.Substring(0, p.Length), descStyle);
				}

				if (tooltipBackgroundStyle == null) {
					tooltipBackgroundStyle = new GUIStyle(skin.box);
					Texture2D backgroundTexture = new Texture2D(1, 1);
					backgroundTexture.SetPixel(0, 0, new Color(0, 0, 0, 0.8f));
					backgroundTexture.wrapMode = TextureWrapMode.Repeat;
					backgroundTexture.Apply();
					tooltipBackgroundStyle.normal.background = backgroundTexture;
				}
				float thumbSize = Math.Min(windowHeight/3, 560);
				float thumbMargin = 10;
				Vector2 tooltipPos = Event.current.mousePosition + new Vector2(10, 10);
				Vector2 tooltipSize = new Vector2(tooltipTextWidth + thumbSize + thumbMargin*2, Math.Max(drawer.contentSize.y, thumbSize + thumbMargin*2));
				clampTooltipPosToWindow(ref tooltipPos, tooltipSize);

				if (new Rect(tooltipPos, tooltipSize).Contains(Event.current.mousePosition)) {
					tooltipPos = Event.current.mousePosition - tooltipSize - new Vector2(10, 10);
					clampTooltipPosToWindow(ref tooltipPos, tooltipSize);
				}

				GUI.Box(new Rect(tooltipPos, tooltipSize), "", tooltipBackgroundStyle);
				drawer.drawAt(tooltipPos);

				GUI.DrawTexture(new Rect(tooltipPos.x + tooltipTextWidth + thumbMargin, tooltipPos.y + thumbMargin, thumbSize, thumbSize), tooltipCraft.thumbTexture);
			}
		}

		private void clampTooltipPosToWindow(ref Vector2 tooltipPos, Vector2 tooltipSize) {
			if (tooltipPos.y + tooltipSize.y > windowHeight) {
				tooltipPos.y = windowHeight - tooltipSize.y - 10;
				if (tooltipPos.y < 10) {
					tooltipPos.y = 10;
				}
			}
			if (tooltipPos.x + tooltipSize.x > windowWidth) {
				tooltipPos.x = windowWidth - tooltipSize.x - 10;
				if (tooltipPos.x < 10) {
					tooltipPos.x = 10;
				}
			}
		}

	}
}

