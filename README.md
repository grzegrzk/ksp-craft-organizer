# KSP craft organizer
Plugin for Kerbal Space Program (KSP) that helps searching crafts by name and allows to tag them. It also allows to load crafts from different saves.

# What is it exactly?
You can see features in this gallery: http://imgur.com/a/MfQlk

# Forum thread

For more information/discussion you can visti this forum thread:
http://forum.kerbalspaceprogram.com/index.php?/topic/145176-release113-ksp-craft-organizer-vabsph-tags-craft-searching

# How to install?
Get newest release from https://github.com/grzegrzk/ksp-craft-organizer/releases, unzip it and put in "Kerbal Space Program/GameData" folder.

# Development of plugin

- Build locally Kramax Plugin Reload from https://github.com/grzegrzk/KramaxPluginReload
- Copy built .dlls from Kramax Plugin Reload to `$(KspDirectory)/GameData/KramaxPluginReload`
- In `$(KspDirectory)/GameData/KramaxPluginReload` create file `Settings.cfg` (If necessary change the `path` part):
```
windowsSdkBinPath=C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8 Tools\
dotFrameworkBinPath=C:\Windows\Microsoft.NET\Framework\v4.0.30319\
PluginSetting
{
	name = KspCraftOrganizer
	path = C:\projects\ksp-craft-organizer\KspCraftOrganizerPlugin\bin\DebugWindows\KspCraftOrganizerPlugin.dll
	loadOnce = false
	methodsAllowedToFail = false
}
```
- If necessary define environment variable KspPath, by default it will be `C:\Program Files (x86)\Steam\steamapps\common\Kerbal Space Program-mod-dev`
- Build `Ksp Craft Organizer` in Debug mode
- Create `$(KspDirectory)/GameData/KspCraftOrganizer` and copy there `icons` directory
- Threre CANNOT be plugin .dll in `$(KspDirectory)/GameData/KspCraftOrganizer/Plugins`
- Run KSP
- Click "Reload Plugins" button when you want to reload plugin after project is rebuilt in IDE
- If necessary edit created file `C:\Program Files (x86)\Steam\steamapps\common\Kerbal Space Program-mod-dev\GameData\KspCraftOrganizer\settings.conf` and modify `debug` to `true`. It will make plugin to log more things into console.
