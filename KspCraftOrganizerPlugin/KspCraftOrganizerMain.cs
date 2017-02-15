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
			return "CraftOrganizer 1.4.1";
		}

		public bool isDebug() {
			return SettingsService.instance.getPluginSettings().debug;
		}

		public GUISkin kspSkin() {
			return IKspAlProvider.instance.kspSkin();
		}
	}

	[KSPAddon(KSPAddon.Startup.EditorAny, false)]
	public class KspCraftOrganizerMain: MonoBehaviour2 {
		private List<BaseWindow> windows = new List<BaseWindow>();

		private OrganizerWindow craftOrganizerWindow;
		private CurrentCraftTagsWindow manageThisCraftWindow;

		private List<ApplicationLauncherButton> appLauncherButtons = new List<ApplicationLauncherButton>();

		private bool alreadyAfterCleanup = false;

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

			GameEvents.onGameSceneLoadRequested.Add(OnSceneLoadRequested);

		}

		public void OnSceneLoadRequested(GameScenes gs) {
			PluginLogger.logDebug("OnSceneLoadRequested");
			CleanUp();
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

			GameEvents.onGameSceneLoadRequested.Remove(OnSceneLoadRequested);
			EditorListenerService.instance.processOnEditorExit();

			foreach (ApplicationLauncherButton button in appLauncherButtons) {
				ApplicationLauncher.Instance.RemoveModApplication(button);
			}
			EditorListenerService.instance.destroy();
			IKspAlProvider.instance.destroy();

			alreadyAfterCleanup = true;

		}

		//
		//Making cleanup in OnDestroy in not a good idea since ksp 1.2 because some global data no longer exist 
		//when this event is fired, so we make cleanup in onGameSceneLoadRequested. We still need to handle OnDestroy
		//in case plugin is reloaded using Kramax Plugin Reload.
		//
		public void OnDestroy() {
			PluginLogger.logDebug("OnDestroy");
			if (!alreadyAfterCleanup) {
				CleanUp();
			}
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

		public void OnDisable() {
			PluginLogger.logDebug("Craft organizer plugin - OnDisable in " + EditorDriver.editorFacility);
		}
	}
}

