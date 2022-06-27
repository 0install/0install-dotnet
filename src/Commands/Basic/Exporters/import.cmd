@echo off

FOR %%A IN ("%~dp0content\*.xml") DO 0install import --batch "%%A"

FOR %%A IN ("%~dp0content\*.tgz") DO 0install store add --batch %%~nA "%%A"

mkdir %appdata%\0install.net\desktop-integration\icons
copy /y *.ico %appdata%\0install.net\desktop-integration\icons\
copy /y *.png %appdata%\0install.net\desktop-integration\icons\
