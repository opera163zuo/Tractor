@echo off
set MSBUILD_PATH="D:\Program Files (x86)\Microsoft Visual Studio\18\BuildTools\MSBuild\Current\Bin\amd64\MSBuild.exe"
if not exist %MSBUILD_PATH% (
    set MSBUILD_PATH="C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\amd64\MSBuild.exe"
)
if not exist "%~dp0scripts\normalize_dotnet_encoding.py" echo Missing encoding normalization script & exit /b 1
where py >nul 2>nul
if %errorlevel%==0 (
    py -3 "%~dp0scripts\normalize_dotnet_encoding.py" "%~dp0"
) else (
    where python >nul 2>nul
    if %errorlevel%==0 (
        python "%~dp0scripts\normalize_dotnet_encoding.py" "%~dp0"
    ) else (
        echo Python launcher not found
        exit /b 1
    )
)
if errorlevel 1 exit /b %errorlevel%
if not exist %MSBUILD_PATH% echo MSBuild not found & exit /b 1
%MSBUILD_PATH% "%~dp0Tractor.sln" /p:Configuration=Debug /v:minimal
pause
