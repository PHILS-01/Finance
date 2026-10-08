@echo off
rem Builds FinLite.exe with the C# compiler that ships with Windows (no installs needed)
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe
"%CSC%" /nologo /target:winexe /optimize /out:FinLite.exe /reference:System.Windows.Forms.dll /reference:System.Drawing.dll FinLite.cs
if exist FinLite.exe (echo Built FinLite.exe) else (echo Build failed)
pause
