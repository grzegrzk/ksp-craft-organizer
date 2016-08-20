using UnityEngine;
using System.Collections.Generic;
using System;

namespace KspNalCommon
{
	public abstract class BaseWindow
	{
		private static readonly float UNLOCK_WAIT_THRESHOLD = 0.5f;
		private static int WINDOW_ID = PluginCommons.instance.getInitialWindowId();

		private string _name;
		protected Rect windowPos { get; private set;} 
		private bool _windowDisplayed;
		private GUISkin _originalSkin;
		private GUISkin _skin;
		private int windowId = WINDOW_ID++;
		private bool centered = false;
		private GUIStyle fadeStyle;
		private bool locked;
		private bool wasLockedOnMouseOver;
		private bool waitingForUnlockEditor;//to solve the problem with accidental clicks when user clicks "cancel"
		private float waitingForUnlockEditorStartTime;

		public Globals.Procedure OnHide { get; set; }

		public bool justAfterWindowDisplay { get; private set; }

		public BaseWindow(string name)
		{
			this._name = name;
			this.guiStyleOption = GuiStyleOption.Ksp;
		}

		virtual public void start()
		{
			PluginLogger.logDebug("Start in window: " + _name);
		}

		virtual public void displayWindow()
		{
			PluginLogger.logDebug("DisplayWindow: " + _name);
			locked = false;
			//float height = getWindowHeight(windowPos);
			//float windowWidth = getWindowWidth(windowPos);
			windowPos = new Rect((Screen.width - windowWidthOnScreen) / 2, (Screen.height - windowHeightOnScreen) / 2, windowWidth, windowHeight);
			_windowDisplayed = true;
			centered = false;
			justAfterWindowDisplay = true;
			waitingForUnlockEditor = false;
		}

		virtual protected float getWindowHeightOnScreen(Rect pos) {
			return Screen.height * 8 / 10;
		}

		abstract protected float getWindowWidthOnScreen(Rect pos);

		abstract protected float getMinWindowInnerWidth(Rect pos);

		public float windowHeight {
			get {
				return windowHeightOnScreen / getGuiScale();
			}
		}

		private float windowHeightOnScreen {
			get {
				return getWindowHeightOnScreen(windowPos);
			}
		}

		public float windowWidth {
			get {
				return windowWidthOnScreen / getGuiScale();
			}
		}

		private float windowWidthOnScreen {
			get {
				return getWindowWidthOnScreen(windowPos);
			}
		}


		virtual public void update()
		{
		}

		abstract protected void windowGUI(int WindowID);
		private void windowGUIPriv(int WindowID) {
			using (new InsideWindowScope(windowPos.position)) {
				windowGUI(WindowID);
			}
		}

		public GUISkin skin { get { return _skin; } }

		protected float guiRawScale {
			get {
				//float scale = OrganizerWindowCraftList.debugScale;
				//float scale = GameSettings.UI_SCALE;
				return GameSettings.UI_SCALE;
			}
		}

		private float getGuiScale() {
			float scale = guiRawScale;
			float minWidth = getMinWindowInnerWidth(windowPos);
			if (windowWidthOnScreen / scale < minWidth) {
				scale = windowWidthOnScreen /minWidth;
				//COLogger.logTrace("True scale: " + scale);
			}
			return scale;
		}

		virtual public void onGUI()
		{
			if (_windowDisplayed) {

				_originalSkin = GUI.skin;
				if (guiStyleOption == GuiStyleOption.Ksp) {
					_skin = PluginCommons.instance.kspSkin();
				} else {
					_skin = GUI.skin;
				}
				GUI.skin = skin;

				if (!locked) {
					if (isPopupWindow()) {
						KSPBasics.instance.lockEditor();
					}
					locked = true;
				}
				if (!isPopupWindow()) {
					if (shouldLockEditor()) {
						wasLockedOnMouseOver = true;
						KSPBasics.instance.lockEditor();
					} else if(wasLockedOnMouseOver){
						KSPBasics.instance.unlockEditor();
						wasLockedOnMouseOver = false;
					}
				}
				if (fadeStyle == null) {
					fadeStyle = new GUIStyle(_skin.box);

					Texture2D backgroundTexture = new Texture2D(1, 1);
					backgroundTexture.SetPixel(0, 0, new Color(0, 0, 0, 0.8f));
					backgroundTexture.wrapMode = TextureWrapMode.Repeat;
					backgroundTexture.Apply();
					fadeStyle.normal.background = backgroundTexture;
				}
				if (isPopupWindow()) {
					GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "", fadeStyle);
				}

				using (new ScaledGuiScope(getGuiScale(), windowPos.x, windowPos.y)) {
					//GUIUtility.ScaleAroundPivot(new Vector2(getGuiScale(), getGuiScale()), new Vector2(windowPos.x, windowPos.y));


					windowPos = GUILayout.Window(windowId, windowPos, windowGUIPriv, _name);
					if (!centered && Event.current.type == EventType.Repaint) {
						windowPos = new Rect((Screen.width - windowWidthOnScreen) / 2, (Screen.height - windowHeightOnScreen) / 2, windowWidth, windowHeight);
						centered = true;
					}

					GUI.skin = null;
					if (Event.current.type == EventType.Repaint) {
						justAfterWindowDisplay = false;
					}
				}
			} else if (waitingForUnlockEditor && (Time.realtimeSinceStartup - waitingForUnlockEditorStartTime) > UNLOCK_WAIT_THRESHOLD) {
				waitingForUnlockEditor = false;
				if (isPopupWindow()) {
					KSPBasics.instance.unlockEditor();
				}
			}
		}

		private bool shouldLockEditor() {
			return isPopupWindow() || windowPos.Contains(Event.current.mousePosition);
		}

		virtual protected bool isPopupWindow() {
			return true;
		}

		public GuiStyleOption guiStyleOption { get; set; }

		public bool windowDisplayed {get {return _windowDisplayed;}}

		public GUISkin originalSkin { get { return _originalSkin; } }

		virtual public void hideWindow()
		{
			PluginLogger.logDebug("hideWindow: " + _name);
			waitingForUnlockEditor = true;
			waitingForUnlockEditorStartTime = Time.realtimeSinceStartup;
			this._windowDisplayed = false;
			if (OnHide != null) {
				OnHide();
			}
		}
	}
}

