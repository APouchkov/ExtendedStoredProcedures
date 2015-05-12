ECHO OFF

REM Копируем сборку
copy "bin\Release\UDP.FileIO.dll" "\\SRV-DEV01\d$\Programms\Extended Stored Procedures\" /y
IF ERRORLEVEL 0 GOTO NEXT1
GOTO ERROR

:NEXT1
REM Заливаем сборку на сервер
REM Sqlcmd -S "SRV-DEV01" -d "Test.BackOffice" -E -b -i "Install.sql"
REM if not ERRORLEVEL 1 goto :OK

:ERROR 
pause
GOTO EXIT

:OK

:EXIT