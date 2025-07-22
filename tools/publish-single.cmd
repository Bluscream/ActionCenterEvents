@echo off
echo Building as single executable...

set "ARCH=net6.0-windows10.0.17763.0"
set "EXE_NAME=ActionCenterEvents"

echo.
echo Option 1: Framework-dependent single file (requires .NET runtime)
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
if %ERRORLEVEL% equ 0 (
    echo.
    echo Success! Single file created at: bin\Release\%ARCH%\win-x64\publish\%EXE_NAME%.exe
    echo Size: ~25MB (requires .NET runtime)
    if exist "bin\Release\%ARCH%\win-x64\publish\%EXE_NAME%.exe" (
        copy /Y "bin\Release\%ARCH%\win-x64\publish\%EXE_NAME%.exe" "bin\%EXE_NAME%.exe" >nul 2>&1
        echo Copied to: bin\%EXE_NAME%.exe
    ) else (
        echo Warning: Expected file not found at bin\Release\%ARCH%\win-x64\publish\%EXE_NAME%.exe
    )
) else (
    echo Build failed! Error code: %ERRORLEVEL%
)

echo.
echo Option 2: Self-contained single file (larger, no runtime required)
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
if %ERRORLEVEL% equ 0 (
    echo.
    echo Success! Self-contained single file created at: bin\Release\%ARCH%\win-x64\publish\%EXE_NAME%.exe
    echo Size: ~171MB (includes .NET runtime)
    if exist "bin\Release\%ARCH%\win-x64\publish\%EXE_NAME%.exe" (
        copy /Y "bin\Release\%ARCH%\win-x64\publish\%EXE_NAME%.exe" "bin\%EXE_NAME%.standalone.exe" >nul 2>&1
        echo Copied to: bin\%EXE_NAME%.standalone.exe
    ) else (
        echo Warning: Expected file not found at bin\Release\%ARCH%\win-x64\publish\%EXE_NAME%.exe
    )
) else (
    echo Self-contained build failed! Error code: %ERRORLEVEL%
)

echo.
echo Build complete! Check the bin directory for your executables.
if exist "bin\%EXE_NAME%.exe" (
    echo - bin\%EXE_NAME%.exe (framework-dependent)
)
if exist "bin\%EXE_NAME%.standalone.exe" (
    echo - bin\%EXE_NAME%.standalone.exe (self-contained)
)
@REM pause