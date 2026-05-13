@echo off
set MSBUILD_PATH="D:\Program Files (x86)\Microsoft Visual Studio\18\BuildTools\MSBuild\Current\Bin\amd64\MSBuild.exe"
if not exist %MSBUILD_PATH% (
    set MSBUILD_PATH="C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\amd64\MSBuild.exe"
)
if not exist %MSBUILD_PATH% (
    echo MSBuild not found at %MSBUILD_PATH%
    exit /b 1
)
%MSBUILD_PATH% "%~dp0Tractor.sln" /p:Configuration=Debug /v:minimal
pause
