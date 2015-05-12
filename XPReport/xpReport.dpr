(******************************************************************************)
(*  xpReport                                                                  *)
(*  Автор: Турянский Александр                                                *)
(*  Версия: 3.6 от 28.04.2008                                                 *)
(******************************************************************************)
library xpReport;
{$I config.inc}

uses
  Windows,
  MSODSAPI,
  foGlobal in 'Source\foGlobal.pas',
  foConst in 'Source\foConst.pas',
  foUtils in '..\..\BackOffice\Common\foUtils.pas',
  foReport in '..\..\BackOffice\Common\foReport.pas',
  foReportDBF in '..\..\BackOffice\Common\foReportDBF.pas',
  foReportExcel in '..\..\BackOffice\Common\foReportExcel.pas',
  foReportFR3 in '..\..\BackOffice\Common\foReportFR3.pas',
  foReportWord in '..\..\BackOffice\Common\foReportWord.pas',
  foReportXML in '..\..\BackOffice\Common\foReportXML.pas',
  foReportTXT in '..\..\BackOffice\Common\foReportTXT.pas',
  foReportIntf in '..\..\BackOffice\Common\foReportIntf.pas',
  Main in 'Source\Main.pas',
  HelpStrs in 'Source\HelpStrs.pas',
  SBaseVariantFunctions in '..\..\BackOffice\Common\SBaseVariantFunctions.pas',
  SBaseStringFunctions in '..\..\BackOffice\Common\SBaseStringFunctions.pas',
  SBaseConstants in '..\..\BackOffice\Common\SBaseConstants.pas',
  fsFunction_TDataSet in '..\..\BackOffice\Common\fsFunction_TDataSet.pas',
  fsFunctions_Ru in '..\..\BackOffice\Common\fsFunctions_Ru.pas';

var
  SaveDllProc: procedure (Reason: Integer) = nil;

procedure LibProc(Reason: Integer);
begin
  case Reason of
    DLL_PROCESS_ATTACH: GlobalInit;
    DLL_PROCESS_DETACH: GlobalFree;
  end;
  if Assigned(SaveDllProc) then SaveDllProc(Reason);
end;

function __GetXpVersion: ULONG; cdecl; export;
begin
  Result := ODS_VERSION;
end;

exports
    __GetXpVersion,
    xp_Report_Help,
    xp_Report_System,
    xp_Report_Show,
    xp_Report_Save,
    xp_Report_Print,    
    xp_Report_Mail;

begin
  IsMultiThread := True;
  SaveDllProc := DllProc;
  DllProc := @LibProc;
  LibProc(DLL_PROCESS_ATTACH);
end.

