using System;
using UnityEngine;

namespace KspCraftOrganizer {
	
	public class ConfirmCraftReplaceWindow : BaseWindow {
		
		public ConfirmCraftReplaceWindow() : base("Confirm File Overwrite", 500) {
			
		}

		public string craftName { get; set; }

		public Globals.Procedure OnReplace { get; set; }

		override protected void windowGUI(int WindowID) {
			using (new GUILayout.VerticalScope()) {
				GUILayout.Label("'" + craftName + "' already exists. Do you want to overwrite it?");
				if (GUILayout.Button("Overwrite")) {
					OnReplace();
					hideWindow();
				}
				if (GUILayout.Button("Cancel")) {
					hideWindow();
				}
			}
		}

		override protected float getWindowHeight(Rect pos) {
			return pos.height;
		}

	}
}

