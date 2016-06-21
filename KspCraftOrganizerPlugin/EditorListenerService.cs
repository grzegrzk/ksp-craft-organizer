using System;
using System.IO;
using KSP.UI.Screens;

using KSP.UI;

namespace KspCraftOrganizer {
	/**
	 * This class can generate events related to Editor: new/load/save. It can also answer if ship has been modified.
	 * 
	 * About new/load/save events:
	 * 
	 * This class can generate new/load/save events for Editor facitily. Originally Save events are not generated, so this class
	 * is a huge hack to simulate it. Basically it checks if file was updated on disk when some new ship is going to be loaded to editor
	 * or if editor is exited or if new ship is going to be made etc. This is not very reliable, but I have not found any other solution
	 * 
	 * It would be good to have following events supported nativly by KSP:
	 *  - Save event fired every time ship is saved (including autosaves), with the path to the file where it is going to be saved
	 *  - "Is ship modified" query event or similar mechanism that would allow plugin to inform the KSP that ship is modified, so
	 * 		clicking on "open" button or "new" could show warning message "your ship is not saved" even if original ship is unmodified,
	 * 		but for example tags are modified
	 * 
	 * About checking if ship has been modified:
	 * 
	 * It does not take into account that user could undo some action. It seems to be a bug in KSP that Undo events are sent 
	 * in both cases: Undo and redo, but redo are never sent so we cannot detect it reliably.
	 * 
	 */
	public class EditorListenerService {

		public static EditorListenerService instance = new EditorListenerService();

		//
		//the ship is autosaved on launch, so we should generate onSave events for save only during launch. Right now they are generated each time editor is exited.
		//
		//

		public EditorListenerService() {
		}


		private string _lastSavedShipName;
		private string _lastShipNameInEditor;
		private string originalShipRealFileOrNull;
		private bool newEditor;
		private DateTime lastSaveDate;
		private FileLocationService fileLocattion = FileLocationService.instance;

		public delegate void OnShipSaved(string fileName, bool craftSavedToNewFile);
		public delegate void OnShipLoaded(string fileName);
		public delegate void OnEditorStarted();

		public OnShipSaved onShipSaved { get; set; }
		public OnShipLoaded onShipLoaded { get; set; }
		public OnEditorStarted onEditorStarted { get; set; }

		public void start() {
			GameEvents.onEditorStarted.Add(this.processOnEditorStarted);
			GameEvents.onEditorLoad.Add(this.processOnEditorLoaded);
			EditorLogic.fetch.shipNameField.onValueChange.AddListener(this.processOnShipNameChanged);

			GameEvents.onEditorShipModified.Add(this.onEditorShipModified);
			GameEvents.onEditorUndo.Add(this.onEditorUndo);

			onShipSaved += delegate (string path, bool craftSavedToNewFile) {
				COLogger.logDebug("On Saved to " + path + ", is new file: " + craftSavedToNewFile);
			};

			onShipLoaded += delegate (string path) {
				COLogger.logDebug("On Loaded from " + path);
			};
			onEditorStarted += delegate () {
				COLogger.logDebug("On Editor started");
			};

			if (EditorLogic.fetch.ship == null) {
				COLogger.logDebug("Ship in editor is null");
				_lastSavedShipName = "";
				_lastShipNameInEditor = "";
				originalShipRealFileOrNull = null;
				newEditor = true;
			} else {
				COLogger.logDebug("Ship name in editor: " + EditorLogic.fetch.ship.shipName + ", number of parts: " + EditorLogic.fetch.ship.Count + ", file:" + EditorDriver.filePathToLoad);
				_lastSavedShipName = EditorLogic.fetch.ship.shipName;
				_lastShipNameInEditor = EditorLogic.fetch.ship.shipName;
				string file = EditorDriver.filePathToLoad;
				originalShipRealFileOrNull = file;
				newEditor = EditorLogic.fetch.ship.Count == 0;
				if (!newEditor && onShipLoaded != null) {
					onShipLoaded(file);
				}
				updateLastSaveDate();
				//UpdateLastSaveTimeFromOriginalShipFile();

			}
		}

		public bool isModifiedSinceSave { get; private set; }

		private void onEditorShipModified(ShipConstruct s) {
			isModifiedSinceSave = true;
		}

		private void onEditorUndo(ShipConstruct s) {
			//we cannot track undo step - it seems they are buggy and this event is called in both Undo & Redo events. 
			//On the contrary, Redo events are never called :(
		}

		public void destroy() {
			onShipSaved = null;
			onShipLoaded = null;
			onEditorStarted = null;

			GameEvents.onEditorStarted.Remove(this.processOnEditorStarted);
			GameEvents.onEditorLoad.Remove(this.processOnEditorLoaded);
			GameEvents.onEditorShipModified.Remove(this.onEditorShipModified);
			GameEvents.onEditorUndo.Remove(this.onEditorUndo);

			EditorLogic.fetch.shipNameField.onValueChange.RemoveListener(this.processOnShipNameChanged);
		}

		public void processOnEditorExit() {
			fireEventIfShipHasBeenSaved();
			if (onShipSaved != null) {
				onShipSaved(fileLocattion.getAutoSaveShipPath(), true);
			}
		}


		private void processOnEditorStarted() {
			COLogger.logDebug("processOnEditorStarted");
			fireEventIfShipHasBeenSaved();

			newEditor = true;
			updateLastSaveDate();
			_lastSavedShipName = lastShipNameInEditor;

			if (onEditorStarted != null) {
				onEditorStarted();
			}

		}

		private void processOnEditorLoaded(ShipConstruct c, CraftBrowserDialog.LoadType lt) {
			if (lt == CraftBrowserDialog.LoadType.Normal) {
				COLogger.logDebug("processOnEditorLoaded, file: " + EditorDriver.filePathToLoad);
				string file = EditorDriver.filePathToLoad;
				if (!newEditor) {
					fireEventIfShipHasBeenSaved();
				}

				_lastShipNameInEditor = c.shipName;

				_lastSavedShipName = c.shipName;
				newEditor = false;
				originalShipRealFileOrNull = file;
				isModifiedSinceSave = false;

				updateLastSaveDate();
				//UpdateLastSaveTimeFromOriginalShipFile();

				if (onShipLoaded != null) {
					onShipLoaded(file);
				}
			}
		}

		private void updateLastSaveDate() {
			string fileSavePath = fileLocattion.getCraftSaveFilePathForShipName(lastShipNameInEditor);
			if (File.Exists(fileSavePath)) {
				lastSaveDate = File.GetLastWriteTime(fileSavePath);
			} else {
				COLogger.logDebug("Cannot find file " + fileSavePath);
				lastSaveDate = new DateTime(0);
			}
		}

		/**
		 * This function must be called every time user has possibility to "see" that ship was saved. For example we 
		 * use it when the user opens "load craft" window so we can detect if current ship was saved and if it was, the tags are
		 * written to the disk as well.
		 */
		public void fireEventIfShipHasBeenSaved() {

			string fileSavePath = fileLocattion.getCraftSaveFilePathForShipName(lastShipNameInEditor);
			if (File.Exists(fileSavePath) && File.GetLastWriteTime(fileSavePath) > lastSaveDate) {
				COLogger.logDebug("Craft file for " + lastShipNameInEditor + " changed, previous save date: " + lastSaveDate + ", current save date: " + File.GetLastWriteTime(fileSavePath));
				if (onShipSaved != null) {
					onShipSaved(fileSavePath, 
					            _lastSavedShipName != lastShipNameInEditor || 
					            newEditor || 
					            (originalShipRealFileOrNull != null && originalShipRealFileOrNull != fileSavePath));
				}

				lastSaveDate = File.GetLastWriteTime(fileSavePath);
				_lastSavedShipName = lastShipNameInEditor;
				originalShipRealFileOrNull = null;
				newEditor = false;
				isModifiedSinceSave = false;
			}

		}


		private void processOnShipNameChanged(string newName) {
			COLogger.logDebug("processOnShipNameChanged");

			if (lastShipNameInEditor != newName) {
				fireEventIfShipHasBeenSaved();

				_lastShipNameInEditor = newName;
				updateLastSaveDate();
			}
		}

		public bool canAutoSaveSomethingToDisk() {
			return !newEditor && lastShipNameInEditor == _lastSavedShipName && File.Exists(originalShipFile);
		}

		public string lastShipNameInEditor { get { return _lastShipNameInEditor; } }
		public string lastSavedShipName { get {return _lastSavedShipName; } }


		public string originalShipFile {
			get {
				if (originalShipRealFileOrNull != null) {
					return originalShipRealFileOrNull;
				} else {
					return fileLocattion.getCraftSaveFilePathForShipName(lastSavedShipName);
				}
			}
		}


		public string currentShipFile {
			get {
				if (originalShipRealFileOrNull != null && lastSavedShipName == lastShipNameInEditor) {
					return originalShipRealFileOrNull;
				} else {
					return fileLocattion.getCraftSaveFilePathForShipName(lastShipNameInEditor);
				}
			}
		}

		public bool isNewEditor() {
			return newEditor;
		}
	}
}

