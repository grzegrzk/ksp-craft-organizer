using System;
using UnityEngine;

#if DEBUG
using KramaxReloadExtensions;
#endif

namespace KspNalCommon {
	/**
	 * To compile debug version KramaxPluginReload must be added conditionally.
	 * To do so the .csproj file has to be edited by hand to have something like that:
	 * 
	   <ItemGroup Condition=" '$(Configuration)' == 'Debug' ">
		<Reference Include="KramaxReloadExtensions">
		  <HintPath>..\..\..\Library\Application Support\Steam\steamapps\common\Kerbal Space Program\GameData\KramaxPluginReload\Plugins\KramaxReloadExtensions.dll</HintPath>
		</Reference>
	  </ItemGroup>
	 *
	 */
#if DEBUG
	public class MonoBehaviour2: ReloadableMonoBehaviour {
#else
	public class MonoBehaviour2: MonoBehaviour {
#endif

	}
}

