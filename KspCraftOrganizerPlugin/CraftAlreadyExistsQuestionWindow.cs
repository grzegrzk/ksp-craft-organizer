using System;
using System.IO;
using UnityEngine;

namespace KspCraftOrganizer {

	public class CraftAlreadyExistsQuestionWindow : BaseWindow {
		

		public CraftAlreadyExistsQuestionWindow() : base("Load craft?") {

		}

		public Globals.Procedure okContinuation { get; set; }
		public string craftName { get; set; }

		override protected void windowGUI(int WindowID) {
			using (new GUILayout.VerticalScope()) {
				GUILayout.Label("The craft '" + craftName + "' already exists. If you load this craft and save it without renaming the existing one will be overwritten.");
				if (GUILayout.Button("Load")) {
					okContinuation();
					hideWindow();
				}
				if (GUILayout.Button("Cancel")) {
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

