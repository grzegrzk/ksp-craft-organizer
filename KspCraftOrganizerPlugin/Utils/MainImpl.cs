using UnityEngine;
using System.Collections.Generic;
using System.IO;
using KSP.UI.Screens;
using KspNalCommon;
using System;

namespace KspCraftOrganizer {

	public class KspCraftOrganizerProperties : CommonPluginProperties {
		public bool canGetIsDebug() {
			return SettingsService.instance != null;
		}

		public int getInitialWindowId() {
			return 4430924;
		}

		public string getPluginDirectory() {
			return FileLocationService.instance.getThisPluginDirectory();
		}

		public string getPluginLogName() {
			return "CraftOrganizer";
		}

		public bool isDebug() {
			return SettingsService.instance.getPluginSettings().debug;
		}

		public GUISkin kspSkin() {
			return IKspAlProvider.instance.kspSkin();
		}
	}

	/**
	 * This class does not inherit from MonoBehaviour to make it easy to use Kramax reloader in debug builds and do not use it in normal builds.
	 */
	public class MainImpl {
		private List<BaseWindow> windows = new List<BaseWindow>();

		private OrganizerWindow craftOrganizerWindow;
		private CurrentCraftTagsWindow manageThisCraftWindow;

		private List<ApplicationLauncherButton> appLauncherButtons = new List<ApplicationLauncherButton>();

		public void Start() {
			PluginCommons.init(new KspCraftOrganizerProperties());

			PluginLogger.logDebug("Craft organizer plugin - start");

			IKspAlProvider.instance.start();

			CraftAlreadyExistsQuestionWindow craftAlreadyExistsQuestionWindow = addWindow(new CraftAlreadyExistsQuestionWindow());
			ShouldCurrentCraftBeSavedQuestionWindow shouldCraftBeSavedQuestionWindow = addWindow(new ShouldCurrentCraftBeSavedQuestionWindow());
			craftOrganizerWindow = addWindow(new OrganizerWindow(shouldCraftBeSavedQuestionWindow, craftAlreadyExistsQuestionWindow));
			manageThisCraftWindow = addWindow(new CurrentCraftTagsWindow());

			addLauncherButtonInAllEditors(craftOrganizerWindow.displayWindow, "manage.png");
			addLauncherButtonInAllEditors(manageThisCraftWindow.displayWindow, "tags.png");

			foreach (BaseWindow window in windows) {
				window.start();
			}

			EditorListenerService.instance.start();

		}

		private void addLauncherButtonInAllEditors(Globals.Procedure callback, string textureFile) {
			ApplicationLauncherButton button = null;

			Texture2D texture = UiUtils.loadIcon(textureFile);

			button = ApplicationLauncher.Instance.AddModApplication(
				delegate () {
					button.SetFalse(false);
					callback();
				}, null, null, null, null, null,
				ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH, texture);
			appLauncherButtons.Add(button);
			
		}

		private T addWindow<T>(T newWindow) where T : BaseWindow  {
			windows.Add(newWindow);
			return newWindow;
		}



		private void CleanUp() {
			PluginLogger.logDebug("Craft organizer plugin - CleanUp in " + EditorDriver.editorFacility);
			EditorListenerService.instance.processOnEditorExit();

			foreach (ApplicationLauncherButton button in appLauncherButtons) {
				ApplicationLauncher.Instance.RemoveModApplication(button);
			}
			EditorListenerService.instance.destroy();
			IKspAlProvider.instance.destroy();

		}

		public void Update() {
			foreach (BaseWindow window in windows) {
				window.update();
			}
		}

		public void OnGUI() {
			//if (Event.current.type != EventType.Repaint && Event.current.type != EventType.Layout) {
			//	COLogger.Log(Event.current);
			//}
			foreach (BaseWindow window in windows) {
				window.onGUI();
			}
		}

		public void OnDestroy() {
			PluginLogger.logDebug("Craft organizer plugin - OnDestroy in " + EditorDriver.editorFacility);
			CleanUp();
		}

		public void OnDisable() {
			PluginLogger.logDebug("Craft organizer plugin - OnDisable in " + EditorDriver.editorFacility);
		}
	}
}

