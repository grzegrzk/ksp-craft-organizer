param (
    [Parameter(Mandatory=$true)][string]$version
)
$versionElements = $version -split '\.'
$major=$versionElements[0]
$minor=$versionElements[1]
$patch=$versionElements[2]

$versionFile = Get-Content 'KspCraftOrganizer.version' -raw | ConvertFrom-Json

Write-Host "Old Version: $($versionFile.VERSION.MAJOR) $($versionFile.VERSION.MINOR) $($versionFile.VERSION.PATCH)"
Write-Host "New Version: $($major).$($minor).$patch"

$continue = $( Read-Host "Type 'y' to continue" )
If($continue -ne "y"){
	Write-Host "Exiting"
	exit 
}

$msBuildDir="C:\Windows\Microsoft.NET\Framework64\v4.0.30319"
$basePluginDir=".\dist\KspCraftOrganizer"

Write-Host "Updating version file"
$versionFile.VERSION.MAJOR = $major
$versionFile.VERSION.MINOR = $minor
$versionFile.VERSION.PATCH = $patch
$versionFile | ConvertTo-Json -Depth 20  | set-content 'KspCraftOrganizer.version'

$KspCraftOrganizerVersionCsPath='.\KspCraftOrganizerPlugin\KspCraftOrganizerVersion.cs'
(Get-Content $KspCraftOrganizerVersionCsPath) -replace 'public const string Version = ".*"', "public const string Version = ""$major.$minor.$patch""" | Set-Content $KspCraftOrganizerVersionCsPath

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
Copy-Item .\KspCraftOrganizer.version $basePluginDir\KspCraftOrganizer.version

Write-Host "Creating distribution archive"
#Compress-Archive creates archive incompatible with osx :/
#Compress-Archive -DestinationPath ".\dist\KspCraftOrganizer.zip" -Path ".\dist\KspCraftOrganizer"

7z a ".\dist\KspCraftOrganizer.zip" ".\dist\KspCraftOrganizer"

Write-Host "DONE"
