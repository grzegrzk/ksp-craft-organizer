#if !DEBUG

using KspCraftOrganizer;
using UnityEngine;


[KSPAddon(KSPAddon.Startup.EditorAny, false)]
public class KspCraftOrganizerMainRelease : MonoBehaviour {

	MainImpl impl = new MainImpl();

	public void Start() {
		COLogger.logDebug("Start in Release mode");
		impl.Start();
	}

	public void Update() {
		impl.Update();
	}

	public void OnGUI() {
		impl.OnGUI();
	}

	public void OnDestroy() {
		impl.OnDestroy();
	}

	public void OnDisable() {
		impl.OnDisable();
	}
}

#endif