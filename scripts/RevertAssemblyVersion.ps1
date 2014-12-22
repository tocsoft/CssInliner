# RevertAssemblyVersion.ps1
#
# http://www.luisrocha.net/2009/11/setting-assembly-version-with-windows.html
# http://blogs.msdn.com/b/dotnetinterop/archive/2008/04/21/powershell-script-to-batch-update-assemblyinfo-cs-with-new-version.aspx
# http://jake.murzy.com/post/3099699807/how-to-update-assembly-version-numbers-with-teamcity
# https://github.com/ferventcoder/this.Log/blob/master/build.ps1#L6-L19
 
Param(
    [string]$path=$pwd
)
function Revert-SourceVersion
{
    foreach ($o in $input) 
    {
        $backupFile = ($o.FullName + "")

        $target = "$backupFile".Substring(0, "$backupFile".Length - 4)

        Write-Host "Reverting Updating  '$backupFile' -> $target"
        Copy-Item $backupFile $target    
        Remove-Item $backupFile 
    }
}
function Revert-AllAssemblyInfoFiles ( )
{
    Write-Host "Searching '$path'"
   foreach ($file in "AssemblyInfo.cs.bak", "AssemblyInfo.vb.bak" ) 
   {
        get-childitem $path -recurse |? {$_.Name -eq $file} | Revert-SourceVersion ;
   }
}
 
Revert-AllAssemblyInfoFiles $version