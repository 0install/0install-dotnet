@echo off
"%windir%\Microsoft.NET\Framework\v4.0.30319\csc.exe" /nologo /win32manifest:"%~dp0..\..\Commands\app.manifest" /out:"%~dp0runenv.exe.template" "%~dp0runenv.cs"
