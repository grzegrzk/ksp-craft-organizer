using UnityEngine;
using System.Collections.Generic;
using System;

namespace KspCraftOrganizer {
	public class CurrentCraftTagsWindow : BaseWindow {
		private static readonly int WINDOW_WIDTH = 500;

		private Vector2 tagScrollPos;

		private CurrentCraftTagsController model = new CurrentCraftTagsController();
		private string newTagText = "";

		public CurrentCraftTagsWindow() : base("Current Craft Settings") {

		}

		override public void displayWindow() {
			base.displayWindow();
			model.resetToLastlyEditied();
		}

		protected override float getWindowWidthOnScreen(Rect pos) {
			return WINDOW_WIDTH;
		}

		override protected float getMinWindowInnerWidth(Rect pos) {
			return WINDOW_WIDTH;
		}

		override protected float getWindowHeightOnScreen(Rect pos) {
			return Math.Min(Screen.height * 8 / 10, 800);
		}

		override protected void windowGUI(int WindowID) {
			using (new GUILayout.VerticalScope()) {
				GUILayout.Label("Select tags for this craft:");
				tagScrollPos = GUILayout.BeginScrollView(tagScrollPos);
				using (new GUILayout.VerticalScope()) {
					foreach (CurrentCraftTagEntity tag in model.availableTags) {
						tag.selected = GUILayout.Toggle(tag.selected, tag.name, originalSkin.toggle);
					}
				}
				GUILayout.EndScrollView();

				GUILayout.Space(10);
				using (new GUILayout.HorizontalScope()) {
					GUILayout.Label("Add new tag:");
					newTagText = GUILayout.TextField(newTagText, GUILayout.Width(200));
					if (GUILayout.Button("Add", GUILayout.Width(100), GUILayout.ExpandWidth(false))) {
						model.addAvailableTag(newTagText);
					}
				}
				GUILayout.Space(20);

				if (GUILayout.Button("Ok", GUILayout.ExpandWidth(true))) {
					model.saveIfPossible();
					hideWindow();
				}
				if (GUILayout.Button("Cancel", GUILayout.ExpandWidth(true))) {
					hideWindow();
				}

			}
			GUI.DragWindow();

		}

	}
}

