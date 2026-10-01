@echo off
setlocal
set "ida_environment=%~1"
if not defined ida_environment set "ida_environment=Local"
set "ida_mode=%~2"
if not defined ida_mode set "ida_mode=dev"
set "DOTNET_ENVIRONMENT=%ida_environment%"
set "ASPNETCORE_ENVIRONMENT=%ida_environment%"
pushd "%~dp0"
dotnet run --project Ida.Start.csproj --no-launch-profile -- --mode "%ida_mode%"
set "ida_exit_code=%errorlevel%"
popd
exit /b %ida_exit_code%
