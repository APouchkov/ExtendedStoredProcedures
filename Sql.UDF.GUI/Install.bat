ECHO OFF

REM �������� ������
copy "obj\Release\UDF.GUI.dll" "\\BD-VM-DV-CIS\Programms\Extended Stored Procedures\" /y
IF ERRORLEVEL 0 GOTO NEXT1
GOTO ERROR

:NEXT1
REM �������� ������ �� ������
REM Sqlcmd -S "BD-VM-DBATEST" -d "BackOffice.Test" -E -b -i "Install.sql"
REM if not ERRORLEVEL 1 goto :OK

:ERROR 
pause
GOTO EXIT

:OK

:EXIT