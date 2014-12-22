 param (
    [string]$config = "release",
    [string]$version = ""
 )

 $dir = (Split-Path $MyInvocation.InvocationName)  + "\.."
 
& ($dir + "scripts\SetAssemblyVersion.ps1") $version -path $dir