set PluginRoot=..
set KeePassExe=%PluginRoot%\bin\Debug\KeePass.exe
Set BuildPath=%~dp0

xcopy /Y %PluginRoot%\Properties\AssemblyInfo.cs plgx\Properties\
xcopy /Y %PluginRoot%\HIBPOfflineCheck.csproj plgx\
xcopy /Y %PluginRoot%\HIBPOfflineCheck.sln plgx\
xcopy /Y %PluginRoot%\HIBPOfflineCheckExt.cs plgx\
xcopy /Y %PluginRoot%\HIBPOfflineCheckOptions.cs plgx\
xcopy /Y %PluginRoot%\HIBPOfflineCheckOptions.Designer.cs plgx\
xcopy /Y %PluginRoot%\HIBPOfflineCheckOptions.resx plgx\
xcopy /Y %PluginRoot%\HIBPOfflineColumnProv.cs plgx\
xcopy /Y %PluginRoot%\ProgressDisplay.cs plgx\
xcopy /Y %PluginRoot%\ProgressDisplay.Designer.cs plgx\
xcopy /Y %PluginRoot%\ProgressDisplay.resx plgx\
xcopy /Y %PluginRoot%\Options.cs plgx\
xcopy /Y %PluginRoot%\Resources\Nuvola\B48x48_KOrganizer.png plgx\Resources\Nuvola\
xcopy /Y %PluginRoot%\Properties\Resources.Designer.cs plgx\Properties\
xcopy /Y %PluginRoot%\Properties\Resources.resx plgx\Properties\

%KeePassExe% --plgx-create "%BuildPath%plgx" --plgx-prereq-net:4.5

move /Y "%BuildPath%plgx.plgx" %PluginRoot%\HIBPOfflineCheck.plgx

rmdir /S /Q "%BuildPath%plgx