using System;
using UnityEngine;
using KspNalCommon;

namespace KspCraftOrganizer {
	public class OrganizerWindowCraftList {

		private readonly float DOUBLE_CLICK_THRESHOLD = 0.5f;

		private readonly OrganizerWindow parent;
		private Vector2 shipsScrollPosition;
		private Rect shipsRect;

		private OrganizerCraftEntity lastSelectedCraft;
		private float lastClickTime;



		public OrganizerWindowCraftList(OrganizerWindow parent) {
			this.parent = parent;
		}

		private OrganizerController model { get { return parent.model; } }

		public void drawCraftsList() {
			int manageTagsWidth = parent.showManageTagsToolbar ? OrganizerWindow.MANAGE_TAGS_TOOLBAR_WIDTH : OrganizerWindow.NO_MANAGE_TAGS_TOOLBAR_WIDTH;
			using (new GUILayout.VerticalScope(GUILayout.Width(parent.windowWidth - manageTagsWidth - 30 - OrganizerWindow.FILTER_TOOLBAR_WIDTH))) {
				
				using (LayoutScrollScope shipsScroll = new LayoutScrollScope(shipsRect, shipsScrollPosition)) {
					shipsScrollPosition = shipsScroll.scroll;
					using (new GUILayout.VerticalScope(GUILayout.ExpandWidth(false))) {
						if (model.filteredCrafts.Length == 0) {
							if (model.availableCrafts.Count > 0) {
								GUILayout.Label("<Nothing to display - change filter>");
							} else {
								GUILayout.Label("<You have no ships to load>");
							}
						}
						int index = 0;
						foreach (OrganizerCraftEntity craft in model.filteredCrafts) {
							drawSingleCraft(index, craft);
							if (!parent.isKspSkin()) {
								GUILayout.Space(5);
							}
							++index;
						}
						GUILayout.Space(20);
					}
				}
				if (Event.current.type == EventType.repaint) {
					shipsRect = GUILayoutUtility.GetLastRect();
				}


				if (parent.showManageTagsToolbar) {
					using (new GUILayout.HorizontalScope()) {
						parent.selectAllFiltered = parent.guiLayout_Toggle_OrigSkin(parent.selectAllFiltered, "Select all", GUILayout.ExpandWidth(false));
						GUILayout.FlexibleSpace();
						GUILayout.Label("Filtered: " + model.filteredCrafts.Length + "/" + model.availableCrafts.Count + ", selected: " + model.selectedCraftsCount, GUILayout.ExpandWidth(false));
					}
				}
			}
		}

		private void drawSingleCraft(int index, OrganizerCraftEntity craft) {
			using (new GUILayout.HorizontalScope()) {
				if (parent.showManageTagsToolbar) {
					using (new GUILayout.VerticalScope(GUILayout.ExpandWidth(false), GUILayout.MaxWidth(20), GUILayout.Height(60))) {
						GUILayout.FlexibleSpace();
						craft.isSelected = GUILayout.Toggle(craft.isSelected, "", GUILayout.ExpandWidth(false));
						GUILayout.FlexibleSpace();
					}
				}
				using (new GUILayout.VerticalScope(GUILayout.ExpandWidth(true))) {
					GUIStyle thisCraftButtonStyle = craft.isSelectedPrimary ? parent.toggleButtonStyleTrue : parent.toggleButtonStyleFalse;
					CraftTagsGrouper tagsGrouper = craft.groupedTags;

					using (new GUI.ClipScope(Globals.ZERO_RECT)) {
						GUILayout.Button("", thisCraftButtonStyle, GUILayout.Height(0));

						if (Event.current.type == EventType.Repaint) {
							craft.guiWidth = GUILayoutUtility.GetLastRect().width;
						}
					}
					float thisShipWidth = craft.guiWidth;
					bool smallThumb = parent.showManageTagsToolbar && parent.windowWidth < 1000;
					float tagsWidth = thisShipWidth - 20.0f;
					if (!smallThumb) {
						tagsWidth -= 52;
					}


					string restOfTagsLabel = "Rest of tags:";
					if (tagsGrouper.groups.Count == 0) {
						restOfTagsLabel = "Tags:";
					}
					float tagValuesWidth = tagsWidth - craft.tagGroupNameWidth - 10;
					if (Event.current.type == EventType.Repaint) {
						craft.tagGroupNameWidth = 0;
						foreach (TagGroup<string> tagGroup in tagsGrouper.groups) {
							craft.tagGroupNameWidth = Math.Max(craft.tagGroupNameWidth, calcLabelWidth(tagGroup.displayName + ":"));
						}
						if (tagsGrouper.restTags.Count > 0) {
							craft.tagGroupNameWidth = Math.Max(craft.tagGroupNameWidth, calcLabelWidth(restOfTagsLabel));
						}

						craft.tagsHeight = 0;
						tagValuesWidth = tagsWidth - craft.tagGroupNameWidth - 10;
						foreach (CraftTagGroup tagGroup in tagsGrouper.groups) {
							tagGroup.guiHeight = calcMultilineLabelHeight(tagValuesWidth, tagGroup.tagsAsString) - (parent.isKspSkin() ? 7 : 2);
							craft.tagsHeight += tagGroup.guiHeight;
						}
						if (tagsGrouper.restTags.Count > 0) {
							tagsGrouper.restTagsGuiHeight = calcMultilineLabelHeight(tagValuesWidth, tagsGrouper.restTagsAsString) - (parent.isKspSkin() ? 7 : 2);

							craft.tagsHeight += tagsGrouper.restTagsGuiHeight;
						}

						craft.guiHeight = 60 + craft.tagsHeight;
						if (!craft.containsMissedParts || craft.notEnoughScience) {
							craft.guiHeight += 20;
						}
					}

					if (GUILayout.Button("", thisCraftButtonStyle, GUILayout.Height(craft.guiHeight), GUILayout.ExpandHeight(false))) {
						if ((Event.current.modifiers & EventModifiers.Control) == 0 || !parent.showManageTagsToolbar) {
							model.unselectAllCrafts();
							parent.selectedCraftName = craft.name;
							craft.isSelected = true;
							model.primaryCraft = craft;
							if (lastSelectedCraft != null 
							    && lastSelectedCraft.craftFile == craft.craftFile 
							    && (Time.realtimeSinceStartup - lastClickTime) < DOUBLE_CLICK_THRESHOLD) {
								parent.load();
							}
							lastSelectedCraft = craft;
							lastClickTime = Time.realtimeSinceStartup;
						} else {
							if (craft.isSelected) {
								craft.isSelected = false;
							} else {
								parent.selectedCraftName = craft.name;
								craft.isSelected = true;
								model.primaryCraft = craft;
							}
						}
					}
					Rect thisShipRect = GUILayoutUtility.GetLastRect();
					Color tagsColor = parent.originalSkin.label.normal.textColor;
					GUIStyle tagsStyle = new GUIStyle(parent.skin.label);
					tagsStyle.normal.textColor = tagsColor;
					GUIStyle craftNameStyle = new GUIStyle(parent.skin.label);
					craftNameStyle.normal.textColor = Color.yellow;
					GUIStyle goodLabelStyle = new GUIStyle(parent.skin.label);
					goodLabelStyle.normal.textColor = Color.green;

					int leftOffset = (int)thisShipRect.x + 10;
					int firstRowsLeftOffset = leftOffset;
					int nextTop = (int)thisShipRect.y + 10;
					int costPosX = Math.Min((int)(thisShipWidth * 2.0f / 3), 300);//(int)thisShipRect.x + (int)(parent.showManageTagsToolbar ? (160 + firstRowsLeftOffset) : 360);
					int thumbSize = smallThumb ? 38 : 52;
					int thumbPosY = (int)thisShipRect.y + (int)(smallThumb ? 10 : (craft.guiHeight - 52) / 2);
					int thumbPosX = (int)thisShipRect.x + (int)(smallThumb ? thisShipWidth - 48 : thisShipWidth - 62);

					int costMaxWidth = thumbPosX - costPosX - 10;
					int extraInfoMaxWidth = costPosX - firstRowsLeftOffset - 10;
					drawLabel(craftNameStyle, firstRowsLeftOffset, nextTop, craft.nameToDisplay, "!!craft" + index);
					nextTop += 20;
					if (parent.showManageTagsToolbar) {
						drawScaledLabel(parent.skin.label, firstRowsLeftOffset, nextTop, extraInfoMaxWidth, thisShipRect, "Parts: " + craft.partCount + ", Stages: " + craft.stagesCount);
					} else {
						drawScaledLabel(parent.skin.label, firstRowsLeftOffset, nextTop, extraInfoMaxWidth, thisShipRect, "Parts: " + craft.partCount + ", Mass: " + craft.massToDisplay + ", Stages: " + craft.stagesCount);
					}
					drawScaledLabel(craft.cost > model.availableFunds && model.availableFunds >= 0 ? parent.warningLabelStyle : goodLabelStyle, costPosX, nextTop, costMaxWidth, thisShipRect, "Cost: " + craft.costToDisplay);
					nextTop += 20;
					if (!craft.containsMissedParts) {
						drawLabel(parent.warningLabelStyle, leftOffset, nextTop, "*The craft contains missed or invalid parts*");
						nextTop += 20;
					} else if (craft.notEnoughScience) {
						drawLabel(parent.warningLabelStyle, leftOffset, nextTop, "*Unavailable due to science level*");
						nextTop += 20;
					}

					foreach (CraftTagGroup tagGroup in tagsGrouper.groups) {
						drawLabel(tagsStyle, leftOffset, nextTop, tagGroup.displayName + ":");
						drawMultilineLabel(tagsColor,
										   tagValuesWidth,
										   (int)(leftOffset + 10 + craft.tagGroupNameWidth),
										   nextTop,
										   tagGroup.tagsAsString);
						nextTop += (int)tagGroup.guiHeight;
					}
					if (tagsGrouper.restTags.Count > 0) {
						drawLabel(tagsStyle, leftOffset, nextTop, restOfTagsLabel);
						drawMultilineLabel(tagsColor,
										   tagValuesWidth,
										   (int)(leftOffset + 10 + craft.tagGroupNameWidth),
										   nextTop,
										   tagsGrouper.restTagsAsString);
						nextTop += (int)tagsGrouper.restTagsGuiHeight;
					}
					//
					//Starting from KSP 1.2 the textures for labels are no longer stretched to the whole label. So we draw the label first
					//to enable tooltip and then we draw texture by hand over the whole area:
					//
					GUI.Label(new Rect(thumbPosX, thumbPosY, thumbSize, thumbSize), new GUIContent("", "!!craft" + index));
					GUI.DrawTexture(new Rect(thumbPosX, thumbPosY, thumbSize, thumbSize), craft.thumbTexture);
				}
			}
		}

		private float calcLabelWidth(string text) {
			GUIStyle style = new GUIStyle(parent.skin.label);
			return style.CalcSize(new GUIContent(text)).x;
		}

		private float calcMultilineLabelHeight(float maxWidth, string text) {
			GUIStyle style = new GUIStyle(parent.skin.label);
			return style.CalcHeight(new GUIContent(text), maxWidth);
		}

		private float drawMultilineLabel(Color color, float maxWidth, int x, int y, string text) {
			GUIStyle style = new GUIStyle(parent.skin.label);
			style.normal.textColor = color;
			float height = style.CalcHeight(new GUIContent(text), maxWidth);
			Rect position = new Rect();
			position.x = x;
			position.y = y;
			position.width = maxWidth;
			position.height = height;
			GUI.Label(position, text, style);
			return height;
		}

		private void drawScaledLabel(GUIStyle style, int x, int y, int maxWidth, Rect scopeRect, string text) {

			Vector2 size = style.CalcSize(new GUIContent(text));
			float scale = 1;

			if (size.x > maxWidth) {
				scale = ((float)maxWidth) / size.x;
			}
			using (new ScaledGuiScope(scale, x, y + size.y / 2)) {
				Rect position = new Rect();
				position.x = x;
				position.y = y;
				position.width = size.x;
				position.height = size.y;
				GUI.Label(position, text, style);
			}
		}

		private void drawLabel(GUIStyle style, int x, int y, string text) {
			drawLabel(style, x, y, text, "");
		}

		private void drawLabel(GUIStyle style, int x, int y, string text, string tooltip) {
			Vector2 size = style.CalcSize(new GUIContent(text));
			Rect position = new Rect();
			position.x = x;
			position.y = y;
			position.width = size.x;
			position.height = size.y;
			GUI.Label(position, new GUIContent(text, tooltip), style);
		}
	}
}

