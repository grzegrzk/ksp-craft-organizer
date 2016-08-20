using System;
namespace KspNalCommon {
	public class KSPBasics {

		private static readonly String LOCK_NAME = PluginCommons.instance.getPluginDirectory() + "_LOCK";

		public static readonly KSPBasics instance = new KSPBasics();

		public KSPBasics() {
		}


		public void lockEditor() {
			EditorLogic.fetch.toolsUI.enabled = false;
			EditorLogic.fetch.enabled = false;


			bool lockExit = true;
#if DEBUG
			lockExit = false;
#endif
			EditorLogic.fetch.Lock(true, lockExit, true, LOCK_NAME);
		}

		public void unlockEditor() {
			if (EditorLogic.fetch != null) {
				if (EditorLogic.fetch.toolsUI != null) {
					EditorLogic.fetch.toolsUI.enabled = true;
				}
				EditorLogic.fetch.enabled = true;
				EditorLogic.fetch.Unlock(LOCK_NAME);
			}
		}
	}
}

