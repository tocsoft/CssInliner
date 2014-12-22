 param (
    [string]$config = "Release",
    [string]$version = ""
 )

$dir = [System.IO.Path]::GetFullPath((Split-Path $MyInvocation.InvocationName)  + "\..")
$solution = "$dir\src\CssInliner.sln"

if ($version -ne "") {
    & "$dir\scripts\SetAssemblyVersion.ps1" $version -path $dir
}

#download nuget.exe if missing

$nugetExe="$dir\scripts\nuget.exe"
If (Test-Path $nugetExe){
    & "$nugetExe" update -self
}else{
    Invoke-WebRequest https://www.nuget.org/nuget.exe -OutFile $nugetExe
}

& "$nugetExe" restore $solution

& "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\msbuild" $solution /t:Rebuild "/p:Configuration=`"$config`""


$nugetVersion = (Get-Item "$dir\src\CssInliner\bin\$config\Tocsoft.CssInliner.dll" | Select-Object -ExpandProperty VersionInfo)[0].ProductVersion

#$nugetVersion 
New-Item -ItemType Directory -Force -Path "$dir\artefacts"

& "$nugetExe" pack "$dir\src\CssInliner.nuspec" -OutputDirectory "$dir\artefacts" -BasePath "$dir\src" -Version $nugetVersion -Properties "Configuration=$config"


if ($version -ne "") {
    & "$dir\scripts\RevertAssemblyVersion.ps1" -path $dir
}