using UnityEngine;
using System.Collections.Generic;
using System;
using KspNalCommon;

namespace KspCraftOrganizer {
	/**
	 * This class is extremely tricky because there is no easy way in KSP to detect when craft in VAB/SPH is saved + it is not possible
	 * to prevent loading new craft in editor only if user confirms that. It is possible to do things like that only if we replaced standard
	 * "save"/"load" actions in VAB/SPH. I do not want to replace such important functionalities with custom, own functions so
	 * everything is a little hacky to workaround it. See EditorListenerService for details.
	 * <p>
	 * Test scenarios:
	 *
	 * - Create new craft, save it, assign tags. The tags should be written on disk.
	 * - Create new craft, assign tags, save it. The tags should be written on disk.
	 * - Load existing craft, assign tags. Tags should be written.
	 * - Load existing craft, modify it, assign tags. Tags should be written.
	 * - Load existing craft, rename it, assign tags, save. The tags should be written only to new craft name.
	 * - Load existing craft, rename it to the name of some other existing craft, assign tags, rename again to old name, save. The tags should be written only to old craft.
	 * - Load existing craft, rename it to the name of some other existing craft, try to save but after question if craft should be overwritten click "cancel". Tags should NOT be written.
	 * - Load existing craft, rename it to the name of some other existing craft, try to save and after question if craft should be overwritten click "Overwrite". Tags should be written.
	 * - Load existing craft, renamie it, assign tags, launch, revert to VAB, save. Tags should be written.
	 */
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
						model.userAddAvailableTag(newTagText);
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

