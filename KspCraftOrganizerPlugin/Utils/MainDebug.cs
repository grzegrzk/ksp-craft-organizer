#if DEBUG

using KspCraftOrganizer;
using KramaxReloadExtensions;

/**
 * Only one of the two can be compiled: MainDebug or MainRelease. To compile debug version KramaxPluginReload must be added conditionally.
 * To do so the .csproj file has to be edited by hand to have something like that:
 * 
   <ItemGroup Condition=" '$(Configuration)' == 'Debug' ">
    <Reference Include="KramaxReloadExtensions">
      <HintPath>..\..\..\Library\Application Support\Steam\steamapps\common\Kerbal Space Program\GameData\KramaxPluginReload\Plugins\KramaxReloadExtensions.dll</HintPath>
    </Reference>
  </ItemGroup>
 *
 */
[KSPAddon(KSPAddon.Startup.EditorAny, false)]
	public class KspCraftOrganizerMainDebug : ReloadableMonoBehaviour{

		MainImpl impl = new MainImpl();

		public void Start () {
			COLogger.logDebug("Start in Debug mode");
			impl.Start();
		}

		public void Update () {
			impl.Update();
		}

		public void OnGUI(){
			impl.OnGUI();
		}

		public void OnDestroy() {
			impl.OnDestroy();
		}

		public void OnDisable(){
			impl.OnDisable();
		}
	}
#endif