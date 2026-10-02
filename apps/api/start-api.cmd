@echo off
setlocal
pushd "%~dp0" || exit /b 1
dotnet run --project Ida.Start.csproj --configuration Release --no-launch-profile -- %*
set "ida_exit_code=%ERRORLEVEL%"
popd
exit /b %ida_exit_code%
