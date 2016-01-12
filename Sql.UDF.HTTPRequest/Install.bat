ECHO OFF

REM Копируем сборку
copy "bin\Release\UDF.HTTPRequest.dll" "\\BD-VM-DV-CIS\Programms\Extended Stored Procedures" /y
IF ERRORLEVEL 0 GOTO NEXT1
GOTO ERROR

:NEXT1
REM Заливаем сборку на сервер
rem Sqlcmd -S "BD-VM-DBATEST\SQLSERVER2014" -d "BackOffice" -E -b -i "Install.sql"
rem if not ERRORLEVEL 1 goto :OK

:ERROR 
pause
GOTO EXIT

:OK

:EXIT