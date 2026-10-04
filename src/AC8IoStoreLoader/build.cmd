@echo off
setlocal
call "C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\VC\Auxiliary\Build\vcvars64.bat" >nul
if errorlevel 1 exit /b 1

lib /nologo /def:UE4SS.def /machine:x64 /out:UE4SS.lib
if errorlevel 1 exit /b 1

cl /nologo /std:c++17 /EHsc /O2 /MD /LD loader.cpp ^
  vendor\IoStoreLoaderMod\minhook\src\buffer.c ^
  vendor\IoStoreLoaderMod\minhook\src\hook.c ^
  vendor\IoStoreLoaderMod\minhook\src\trampoline.c ^
  vendor\IoStoreLoaderMod\minhook\src\hde\hde32.c ^
  vendor\IoStoreLoaderMod\minhook\src\hde\hde64.c ^
  /Ivendor\IoStoreLoaderMod\minhook\include ^
  /Ivendor\IoStoreLoaderMod\minhook\src ^
  /Ivendor\IoStoreLoaderMod\minhook\src\hde ^
  /link /OUT:main.dll /MAP:main.map UE4SS.lib
if errorlevel 1 exit /b 1

if exist "..\AC8AssetPatcher\AC8AssetPatcher.csproj" (
  if not exist "..\AC8AssetPatcher\assets\AC8IoStoreLoader" mkdir "..\AC8AssetPatcher\assets\AC8IoStoreLoader"
  copy /Y main.dll "..\AC8AssetPatcher\assets\AC8IoStoreLoader\main.dll" >nul
  if errorlevel 1 exit /b 1
)
exit /b 0
