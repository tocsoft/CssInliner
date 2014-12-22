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

& "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\msbuild" $solution "/p:Configuration=`"$config`""


$nugetVersion = Get-Item "$dir\src\CssInliner\bin\$config\Tocsoft.CssInliner.dll" | Select-Object -ExpandProperty VersionInfo

& "$nugetExe" pack "$dir\src\CssInliner.nuspec" -OutputDirectory "$dir\artefacts" -BasePath "$dir\src" -Version $nugetVersion


if ($version -ne "") {
    & "$dir\scripts\RevertAssemblyVersion.ps1" -path $dir
}