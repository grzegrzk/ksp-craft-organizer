$msBuildDir="C:\Windows\Microsoft.NET\Framework64\v4.0.30319"
$basePluginDir=".\dist\KspCraftOrganizer"

Write-Host "Removing old distribution"
Remove-Item -Path ".\dist" -Recurse
Remove-Item -Path ".\KspCraftOrganizerPlugin\bin" -Recurse

Write-Host "Creating empty folders for new distribution"
mkdir "$basePluginDir\Plugins"
mkdir "$basePluginDir\icons"


Write-Host "Compiling project"
Invoke-Expression "$msBuildDir\msbuild.exe KspCraftOrganizerPlugin.sln '/target:Clean,Build' /p:Configuration=ReleaseWindows '/l:FileLogger,Microsoft.Build.Engine;logfile=Manual_MSBuild_ReleaseVersion_LOG.log'"

Write-Host "Copying files"
Get-ChildItem .\icons\ -Include *.png -Recurse | % {Copy-Item $_ "$basePluginDir\icons"}
Copy-Item .\KspCraftOrganizerPlugin\bin\ReleaseWindows\KspCraftOrganizerPlugin.dll $basePluginDir\Plugins

Copy-Item .\LICENSE $basePluginDir\LICENSE.txt

Write-Host "Creating distribution archive"
#Compress-Archive creates archive incompatible with osx :/
#Compress-Archive -DestinationPath ".\dist\KspCraftOrganizer.zip" -Path ".\dist\KspCraftOrganizer"

7z a ".\dist\KspCraftOrganizer.zip" ".\dist\KspCraftOrganizer"

Write-Host "DONE"
