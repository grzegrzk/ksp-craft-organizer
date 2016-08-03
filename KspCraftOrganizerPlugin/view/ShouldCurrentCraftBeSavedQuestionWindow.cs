using System;
using UnityEngine;
namespace KspCraftOrganizer {
	public class ShouldCurrentCraftBeSavedQuestionWindow: BaseWindow {
		
		private IKspAl ksp = IKspAlProvider.instance;

		public ShouldCurrentCraftBeSavedQuestionWindow() : base("Save current craft?") {
			
		}

		public string fileToLoad { get; set; }

		override protected void windowGUI(int WindowID) {
			using (new GUILayout.VerticalScope()) {
				GUILayout.Label("Do you want to save the current craft '" + ksp.getCurrentCraftName() + "' before loading new one?");
				if (GUILayout.Button("Save and load new craft")) {
					ksp.saveCurrentCraft();
					ksp.loadCraftToWorkspace(fileToLoad);
					hideWindow();
				}
				if (GUILayout.Button("Cancel")) {
					hideWindow();
				}
				if (GUILayout.Button("Don't Save and load new craft")) {
					ksp.loadCraftToWorkspace(fileToLoad);
					hideWindow();
				}
			}
		}

		override protected float getWindowHeightOnScreen(Rect pos) {
			return pos.height;
		}

		protected override float getWindowWidthOnScreen(Rect pos) {
			return 500;
		}

		override protected float getMinWindowInnerWidth(Rect pos) {
			return 500;
		}
	}
}

