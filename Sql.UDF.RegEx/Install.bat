ECHO OFF

REM Копируем сборку
copy "bin\Release\UDF.RegEx.dll" "\\BD-VM-DBATEST\d$\Programms\Extended Stored Procedures\" /y
IF ERRORLEVEL 0 GOTO NEXT1
GOTO ERROR

:NEXT1
REM Заливаем сборку на сервер
REM Sqlcmd -S "BD-VM-DBATEST" -d "BackOffice" -E -b -i "Install.sql"
if not ERRORLEVEL 1 goto :OK

:ERROR 
pause
GOTO EXIT

:OK

:EXIT