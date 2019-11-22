@echo off

set SolutionName=GrobExp.Mutators
set PackagedProjectName=Mutators

rem reset current directory to the location of this script
pushd "%~dp0"

if exist "./%PackagedProjectName%/bin" (
    rd "./%PackagedProjectName%/bin" /Q /S || exit /b 1
)

dotnet build --force --no-incremental --configuration Release "./%SolutionName%.sln" || exit /b 1

dotnet pack --no-build --configuration Release "./%SolutionName%.sln" || exit /b 1

pause