@echo Off
set config=%1
if "%config%" == "" (
   set config=release
)

%WINDIR%\Microsoft.NET\Framework\v4.0.30319\msbuild CssInliner.sln /p:Configuration="%config%";BuildPackage=true;PackageOutputDir=..\artefacts /m /v:M /fl /flp:LogFile=msbuild.log;Verbosity=Normal /nr:false