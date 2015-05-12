(******************************************************************************)
(*  xpReport - Main                                                           *)
(*  Автор: Турянский Александр                                                *)
(*  Версия: 3.6 от 28.04.2008                                                 *)
(******************************************************************************)
unit Main;
{$I config.inc}

interface

uses
  Windows, MSODSAPI;

const
  VersionBuild = 1002; 

  // Export
  function xp_Report_Help(pSrvProc: SRV_PROC): SRVRETCODE; cdecl; export;
  function xp_Report_System(pSrvProc: SRV_PROC): SRVRETCODE; cdecl; export;
  function xp_Report_Show(pSrvProc: SRV_PROC): SRVRETCODE; cdecl; export;
  function xp_Report_Save(pSrvProc: SRV_PROC): SRVRETCODE; cdecl; export;
  function xp_Report_Print(pSrvProc: SRV_PROC): SRVRETCODE; cdecl; export;
  function xp_Report_Mail(pSrvProc: SRV_PROC): SRVRETCODE; cdecl; export;

  // Global
  procedure GlobalInit;
  procedure GlobalFree;

implementation

uses
  Classes, SysUtils, DateUtils, StrUtils, Variants, VarList, IniFiles, DB,
  SBaseVariantFunctions, ActiveX, XProc, MSAccess, foGlobal, foConst,
  foUtils, foReport, HelpStrs;

type
  TSession = class;

  TSessionState = (ssInit, ssConnect, ssProcess, ssSuspend, ssFree);
  TSessionProcess = procedure of object;

  TSession = class
  private
    FID: Integer;
    FProc: TXProc;
    FState: TSessionState;
    FTimeStart: TDateTime;
    FParams: TVarList;
    FServerInfo: TXServerInfo;
  protected
    function GetParam(const Name: String; Index: Integer; DataType: TFieldType; Required: Boolean = False): Variant;
    procedure ReadParams;
    procedure InternalCreate; virtual;
    procedure InternalDestroy; virtual;
    procedure DoProcess(Process: TSessionProcess);
  public
    constructor Create(pSrvProc: SRV_PROC); virtual;
    destructor Destroy; override;
    procedure Wait;
    procedure Help(const Text: String);
    procedure Error(const Message: String);
    procedure Print(const Message: String);
    procedure PrintFmt(const Message: String; const Args: array of const);
    property ID: Integer read FID;
    property State: TSessionState read FState;
    property TimeStart: TDateTime read FTimeStart;
    property Params: TVarList read FParams;
    property ServerInfo: TXServerInfo read FServerInfo;
  end;

  TConnection = class
  private
    FSPID: Integer;
    FTimeStart: TDateTime;
    FTimeCreate: TDateTime;
    FDBConnection: TMSConnection;
    function GetTimeLive: Integer;
  public
    constructor Create; virtual;
    destructor Destroy; override;
    procedure Connect(Session: TSession);
    property SPID: Integer read FSPID;
    property TimeLive: Integer read GetTimeLive;
    property TimeStart: TDateTime read FTimeStart;
    property TimeCreate: TDateTime read FTimeCreate;
  end;

  TSystemSession = class(TSession)
  private
    FHelp: Boolean;
  public
    constructor Create(pSrvProc: Pointer); override;
    procedure DoAction;
    procedure ProcessInfo;
    procedure ProcessSetFileTime;
    procedure ProcessSetSystemTime;
  end;

  TReportSession = class(TSession)
  private
    FReport: TReport;
    FVariables: TVarList;
    FConnection: TConnection;
    procedure ReadVariables;
  protected
    procedure InternalCreate; override;
    procedure InternalDestroy; override;
    procedure DoMessage(const Msg: String; LogType: SmallInt);
    procedure ProcessShow;
    procedure ProcessSave;
    procedure ProcessPrint;
    procedure ProcessMail;
    property Variables: TVarList read FVariables;
    property Report: TReport read FReport;
  end;

  TRepBuilder = class
  private
    FLogFile: TFileStream;
    FLogFileName: String;
    FSessionList: TThreadList;
    FConnectionList: TThreadList;
    FConnectionPool: TThreadList;
    FLoadINI: Boolean;
    FLock: TRTLCriticalSection;
    FPathTemplate: String;
    FPathDefault: String;
    FPathTemp: String;
    FPathLog: String;
    FMaxConnections: Integer;
    FTimeConnection: Integer;
    FServer: String;
    FDatabase: String;
    FUserName: String;
    FPassword: String;
    FSessionMaxID: Integer;
    function GetSessionCount: Integer;
    function GetSessions(Index: Integer): TSession;
    function GetConnectionCount: Integer;
    function GetConnections(Index: Integer): TConnection;
    procedure CheckConnectionPool;
  protected
    procedure Lock;
    procedure UnLock;
    procedure Error(const Message: String; SessionID: Integer = 0);
    procedure LogAdd(MessType: Char; const Message: String; SessionID: Integer = 0; Params: TObject = nil);
    function CreateConnection(Session: TSession): TConnection;
    procedure FreeConnection(Connection: TConnection);
  public
    constructor Create; virtual;
    destructor Destroy; override;
    function GetSessionID: Integer;
    procedure ReadINI(Reload: Boolean = False);
    property PathTemplate: String read FPathTemplate;
    property PathDefault: String read FPathDefault;
    property PathTemp: String read FPathTemp;
    property PathLog: String read FPathLog;
    property MaxConnections: Integer read FMaxConnections;
    property TimeConnection: Integer read FTimeConnection;
    property Server: String read FServer;
    property Database: String read FDatabase;
    property UserName: String read FUserName;
    property SessionCount: Integer read GetSessionCount;
    property Sessions[Index: Integer]: TSession read GetSessions;
    property ConnectionCount: Integer read GetConnectionCount;
    property Connection[Index: Integer]: TConnection read GetConnections;
  end;

var
  RepBuilder: TRepBuilder;

const
  SSessionStates: Array [TSessionState] of String = ('Init', 'Connect', 'Runnable', 'Suspend', 'Free');

resourcestring
  // Procedures
  SProcNameHelp = 'xp_Report_Help';
  SProcNameSystem = 'xp_Report_System';
  SProcNameReportShow = 'xp_Report_Show';
  SProcNameReportSave = 'xp_Report_Save';
  SProcNameReportPrint = 'xp_Report_Print';
  SProcNameReportMail = 'xp_Report_Mail';

  // Errors
  SErrorININoFile = 'Не найден файл настроек "%s"';
  SErrorINIPrefix = 'Ошибка в файле настроек: ';
  SErrorINIParamServer = 'Не задано имя сервера (%s)';
  SErrorINIParamDataBase = 'Не задано имя базы данных (%s)';
  SErrorINIParamUserName = 'Не задано имя пользователя (%s)';
  SErrorINIParamPassword = 'Не задан пароль (%s)';
  SErrorFileNotFound = 'Не найден файл ''%s''';
  SErrorProcedureUnknown = 'Процедура ''%s'' не поддерживается текущей версией библиотеки';
  SErrorActionUnknown = 'Операция ''%s'' не поддерживается процедурой ''%s'' текущей версией библиотеки';
  SErrorParamName = 'Неизвестный параметр ''%s''';
  SErrorParamTypeDateTime = 'Задан некорректный формат даты для параметра ''%s''';
  SErrorParamRequired = 'Не указан обязательный параметр ''%s''';

  // Ini
  SINIExt = '.ini';

  // SQL
  SSQLTest = 'select @@spid';

  // Path
  SPathSection = 'Path';
  SPathTemplates = 'Templates';
  SPathDefault = 'Default';
  SPathTemp = 'Temp';
  SPathLog = 'Log';

  // DB
  SDBSection = 'Connection';
  SMaxConnections = 'MaxConnections';
  STimeConnection = 'TimeConnection';
  SServer = 'Server';
  SDatabase = 'Database';
  SUserName = 'UserName';
  SPassword = 'Password';

  // Report
  SReportVariablePrefix = 'Variable_';

  // Fields
  SFieldFileName = 'FileName';
  SFieldData = 'Data';

  // Actions
  SSystemActionSetFileTime = 'SetFileTime';
  SSystemActionSetSystemTime = 'SetSystemTime';

  // Params
  SParamAction = 'Action';
  SParamProcName = 'ProcName';
  SParamActionHelp = 'ActionHelp';
  SParamActionInfo = 'Info';
  SParamFileName = 'FileName';
  SParamCreationTime = 'CreationTime';
  SParamLastAccessTime = 'LastAccessTime';
  SParamLastWriteTime = 'LastWriteTime';
  SParamSystemTime = 'SystemTime';  
  SParamMailEMail = 'EMail';
  SParamMailFrom = 'From';
  SParamMailSubject = 'Subject';
  SParamMailTextBody = 'TextBody';
  SParamMailHTMLBody = 'HTMLBody';

  // Test
  STestInfo = 'Текущее соединение: KPID=%d USER=%s; HOST=%s; APPNAME=%s; CODEPAGE=%d';
  STestMemSize = 'Используемая память: %d (%d) байт.';
  STestINIFileName = 'Имя файла настроек: ''%s''';
  STestINIPathTemplate = 'Директория для шаблонов (Templates): ''%s''';
  STestINIPathDefault = 'Директория для сохранения файлов (Default): ''%s''';
  STestINIPathTemp = 'Директория для временных файлов (Temp): ''%s''';
  STestINIPathLog = 'Директория для логов (Log): ''%s''';
  STestINIMaxConnections = 'Максимальное количество подключений к серверу (MaxConnections): %d';
  STestINITimeConnection = 'Время (в минутах) жизни подключения к серверу (TimeConnection): %d';
  STestINIServer = 'Сервер (Server): %s';
  STestINIDatabase = 'База данных (Database): %s';
  STestINIUserName = 'Логин (UserName): %s';
  STestSessionCount = 'Текущих сейсий: %d';
  STestSession = '  %d. %s; STATUS=%s; SID=%d; KPID=%d; USER=%s; HOST=%s; APPNAME=%s';
  STestConnectionCount = 'Подключений к серверу: %d';
  STestConnection = '  %d. %s (%s); SPID=%d; TIMELIVE=%d';
  STestParamCount = 'Количество переданных параметров: %d';
  STestVariable = 'Переменная: ''%s'': %s';
  STestParam = 'Параметр ''%s'': %s';

  // Debug
  SDebugFileExt = '.log';
  SDebugMessage = '%s %s: %s';
  SDebugEmpty = '(нет)';
  SDebugGlobalInit = 'Init DLL M=%d(%d) INI=%s; Templates=%s; Default=%s; Temp=%s; Log=%s; Server=%s; Database=%s; Username=%s';
  SDebugGlobalFree = 'Free DLL M=%d(%d)';
  SDebugInit = 'Init USER=%s; HOST=%s; APPNAME=%s; M=%d(%d)';
  SDebugFree = 'Free M=%d(%d)';
  SDebugConnect = 'Connect (%d)';
  SDebugReConnect = 'ReConnect (%d)';
  SDebugDisconnect ='Disconnect (%d)';
  SDebugParamsEmpty = 'No params';
  SDebugReportTest = 'ReportTest';
  SDebugReportShow = 'ReportShow DS=%d';
  SDebugReportSave = 'ReportSave DS=%d';
  SDebugReportMail = 'ReportMail DS=%d';

// Export
function xp_Report_Help(pSrvProc: SRV_PROC): SRVRETCODE; cdecl; export;
begin
  try
    with TSystemSession.Create(pSrvProc) do
    try
      FHelp := True;
      DoProcess(DoAction);
    finally
      Free;
    end;
    Result := SUCCEED;
  except
    on E: Exception do
    begin
      RepBuilder.Error(E.Message);
      Result := FAIL;
    end;
  end;
end;

function xp_Report_System(pSrvProc: SRV_PROC): SRVRETCODE; cdecl; export;
begin
  try
    with TSystemSession.Create(pSrvProc) do
    try
      DoProcess(DoAction);
    finally
      Free;
    end;
    Result := SUCCEED;
  except
    on E: Exception do
    begin
      RepBuilder.Error(E.Message);
      Result := FAIL;
    end;
  end;
end;

function xp_Report_Show(pSrvProc: SRV_PROC): SRVRETCODE;
begin
  Result := FAIL;
  try
    with TReportSession.Create(pSrvProc) do
    try
      DoProcess(ProcessShow);
    finally
      Free;
    end;
    Result := SUCCEED;
  except
    on E: EAbort do exit;
    on E: Exception do RepBuilder.Error(E.Message);
  end;
end;

function xp_Report_Save(pSrvProc: SRV_PROC): SRVRETCODE;
begin
  Result := FAIL;
  try
    with TReportSession.Create(pSrvProc) do
    try
      DoProcess(ProcessSave);
    finally
      Free;
    end;
    Result := SUCCEED;
  except
    on E: EAbort do exit;
    on E: Exception do RepBuilder.Error(E.Message);
  end;
end;

function xp_Report_Print(pSrvProc: SRV_PROC): SRVRETCODE; cdecl; export;
begin
  Result := FAIL;
  try
    with TReportSession.Create(pSrvProc) do
    try
      DoProcess(ProcessPrint);
    finally
      Free;
    end;
    Result := SUCCEED;
  except
    on E: EAbort do exit;
    on E: Exception do RepBuilder.Error(E.Message);
  end;
end;

function xp_Report_Mail(pSrvProc: SRV_PROC): SRVRETCODE;
begin
  Result := FAIL;
  try
    with TReportSession.Create(pSrvProc) do
    try
      DoProcess(ProcessMail);
    finally
      Free;
    end;
    Result := SUCCEED;
  except
    on E: EAbort do exit;
    on E: Exception do RepBuilder.Error(E.Message);
  end;
end;

// Global
procedure GlobalInit;
begin
  RepBuilder := TRepBuilder.Create;
end;

procedure GlobalFree;
begin
  RepBuilder.Free;
end;

{ TSession }
constructor TSession.Create(pSrvProc: SRV_PROC);
begin
  inherited Create;
  CoInitializeEx(nil, COINIT_APARTMENTTHREADED);
  FProc := TXProc.Create(pSrvProc);
  FServerInfo := FProc.ServerInfo;
  FState := ssInit;
  FTimeStart := Now;
  with RepBuilder do
  begin
    FID := GetSessionID;
    FSessionList.Add(Self);
  end;
  FParams := TVarList.Create(True);
end;

destructor TSession.Destroy;
begin
  FState := ssFree;
  RepBuilder.LogAdd('F', Format(SDebugFree, [GetMemSize, GetMemUsed]), ID);
  RepBuilder.FSessionList.Remove(Self);
  FParams.Free;
  FProc.Free;
  CoUninitialize;
  inherited;
end;

function TSession.GetParam(const Name: String; Index: Integer; DataType: TFieldType; Required: Boolean): Variant;
var
  AID: Integer;
begin
  Result := Null;
  AID := FProc.GetParamIDByName(Name);
  if AID <= 0 then
  begin
    AID := Index;
    if (AID > 0) and (AID <= FProc.ParamCount) and (FProc.GetParamName(AID) <> '') and not SameText(FProc.GetParamName(AID), Name) then
      raise Exception.CreateFmt(SErrorParamName, [FProc.GetParamName(AID)])
  end;
  if Required and ((AID <= 0) or (AID > FProc.ParamCount)) then
    raise Exception.CreateFmt(SErrorParamRequired, [Name])
  else
    begin
      case DataType of
        ftDate, ftTime, ftDateTime: if not TryVariantToDateTime(FProc.GetParam(AID), Result) then
                                      raise Exception.CreateFmt(SErrorParamTypeDateTime, [Name]);
      else Result := FProc.GetParam(AID);
    end;
  end;
end;

procedure TSession.ReadParams;
var
  i: Integer;
  AName: String;
begin
  with FProc do
  for i := 1 to ParamCount do
  begin
    AName := GetParamName(i);
    FParams[AName] := GetParam(i);
  end;
end;

procedure TSession.Help(const Text: String);
var
  i, l, APos: Integer;
begin
  PrintFmt(SHelpTitle, [SRepVersion, VersionBuild, GetCurrentModuleName]);
  APos := 0;
  repeat
    i := APos;
    APos := PosEx(';', Text + ';', Succ(APos));
    l := APos - i;
    Print(Copy(Text, Succ(i), Pred(l)));
  until APos = 0;
end;

procedure TSession.Error(const Message: String);
begin
  RepBuilder.Error(Message, ID);
  FProc.RaisError(Message);
end;

procedure TSession.Print(const Message: String);
begin
  if Message <> '' then FProc.Print(Message);
end;

procedure TSession.PrintFmt(const Message: String; const Args: array of const);
begin
  Print(Format(Message, Args));
end;

procedure TSession.DoProcess(Process: TSessionProcess);
begin
  try
    InternalCreate;
    try
      FState := ssProcess;
      Process;
    finally
      FState := ssFree;    
      InternalDestroy;
    end;
  except
    on E: EAbort do exit;
    on E: Exception do
    begin
      Error(E.Message);
      Abort;
    end;
  end;
end;

procedure TSession.Wait;
var
  AOldState: TSessionState;
begin
  AOldState := State;
  FState := ssSuspend;
  try
    Sleep(0);
  finally
    FState := AOldState;
  end;
end;

procedure TSession.InternalCreate;
begin
  RepBuilder.ReadINI;
  ReadParams;
  with RepBuilder, FProc.ServerInfo do LogAdd('S', Format(SDebugInit, [User, Host, AppName, GetMemSize, GetMemUsed]), ID, Params);
end;

procedure TSession.InternalDestroy;
begin
end;

{ TConnection }
constructor TConnection.Create;
begin
  inherited Create;
  FTimeCreate := Now;
  FTimeStart := TimeCreate;
  FDBConnection := TMSConnection.Create(nil);
  with FDBConnection do
  begin
    Server := RepBuilder.FServer;
    Database := RepBuilder.FDatabase;
    Username := RepBuilder.FUserName;
    Password := RepBuilder.FPassword;
  end;
end;

destructor TConnection.Destroy;
begin
  FDBConnection.Disconnect;
  FDBConnection.Free;
  inherited;
end;

procedure TConnection.Connect(Session: TSession);
begin
  if not FDBConnection.Connected then
  begin
    with RepBuilder do LogAdd('S', Format(SDebugConnect, [ConnectionCount]), Session.ID);
    FDBConnection.Connect;
    with RepBuilder do LogAdd('F', Format(SDebugConnect, [ConnectionCount]), Session.ID);
  end;
  try
    FSPID := isNull(ExecSQLString(SSQLTest, [], False, FDBConnection), -1);
  except
    // Try reconnect
    with RepBuilder do LogAdd('S', Format(SDebugReConnect, [ConnectionCount]), Session.ID);
    FDBConnection.Disconnect;
    FDBConnection.Connect;
    FSPID := isNull(ExecSQLString(SSQLTest, [], False, FDBConnection), -1);
    with RepBuilder do LogAdd('F', Format(SDebugReConnect, [ConnectionCount]), Session.ID);
  end;
  FTimeStart := Now;  
end;

function TConnection.GetTimeLive: Integer;
begin
  Result := MinutesBetween(Now, FTimeStart);
end;

{ TSystemSession }
constructor TSystemSession.Create(pSrvProc: Pointer);
begin
  inherited;
  FHelp := False;
end;

procedure TSystemSession.DoAction;
var
  AAction, AProcName: String;
begin
  if FHelp then
  begin
    AProcName := Trim(isNull(GetParam(SParamProcName, 1, ftString), ''));
    if (AProcName = '') or SameText(AProcName, SProcNameHelp) then begin Help(SHelpMain); exit; end;
  end;
  if SameText(AProcName, SProcNameReportShow) then Help(SHelpReportShow)
  else
  if SameText(AProcName, SProcNameReportSave) then Help(SHelpReportSave)
  else
  if SameText(AProcName, SProcNameReportPrint) then Help(SHelpReportPrint)
  else
  if SameText(AProcName, SProcNameReportMail) then Help(SHelpReportMail)
  else
  begin
    if FHelp and not SameText(AProcName, SProcNameSystem) then
      raise Exception.CreateFmt(SErrorProcedureUnknown, [AProcName]);
    if FHelp then
      AAction := Trim(isNull(GetParam(SParamAction, 2, ftString), ''))
    else
      AAction := Trim(isNull(GetParam(SParamAction, 1, ftString), ''));
    if FHelp and (AAction = '') then Help(SHelpSystem)
    else
    if SameText(AAction, SSystemActionSetFileTime) then ProcessSetFileTime
    else
    if SameText(AAction, SSystemActionSetSystemTime) then ProcessSetSystemTime
    else
    if (AAction = '') or SameText(AAction, SParamActionInfo) then ProcessInfo
    else
      raise Exception.CreateFmt(SErrorActionUnknown, [AAction, SProcNameSystem]);
  end;
end;

procedure TSystemSession.ProcessInfo;
var
  i, AMemSize: Integer;
begin
  if FHelp then begin Help(SHelpSytstemInfo); exit; end;
  PrintFmt(SHelpTitle, [SRepVersion, VersionBuild, GetCurrentModuleName]);
  with ServerInfo do PrintFmt(STestInfo, [KPID, User, Host, AppName, CodePage]);
  AMemSize := GetMemSize;
  if AMemSize >= 0 then PrintFmt(STestMemSize, [GetMemSize, GetMemUsed]);
  PrintFmt(STestINIFileName, [ChangeFileExt(GetCurrentModuleName, SINIExt)]);
  with RepBuilder do
  begin
    PrintFmt(STestINIPathTemplate, [PathTemplate]);
    PrintFmt(STestINIPathDefault, [PathDefault]);
    PrintFmt(STestINIPathTemp, [PathTemp]);
    PrintFmt(STestINIPathLog, [PathLog]);
    PrintFmt(STestINIMaxConnections, [MaxConnections]);
    PrintFmt(STestINITimeConnection, [TimeConnection]);
    PrintFmt(STestINIServer, [Server]);
    PrintFmt(STestINIDatabase, [Database]);
    PrintFmt(STestINIUserName, [UserName]);
    with FSessionList, LockList do
    try
      PrintFmt(STestSessionCount, [Count]);
      for i := 0 to Pred(Count) do
      with TSession(Items[i]), ServerInfo do
        Self.PrintFmt(STestSession + IfThen(Items[i] = Self, '*', ''), [Succ(i), DateTimeToStr(TimeStart), SSessionStates[State], ID, KPID, User, Host, AppName]);
    finally
       UnlockList;
    end;
    with FConnectionList, LockList do
    try
      PrintFmt(STestConnectionCount, [Count]);
      for i := 0 to Pred(Count) do
      with TConnection(Items[i]) do
         PrintFmt(STestConnection, [Succ(i), DateTimeToStr(TimeCreate), DateTimeToStr(TimeStart), SPID, TimeLive]);
    finally
       UnlockList;
    end;
  end;
end;

procedure TSystemSession.ProcessSetFileTime;

  function DateTimeToPFileTime(ADateTime: TDateTime): TFileTime;
  var
    ASystemDateTime: TSystemTime;
  begin
    DateTimeToSystemTime(ADateTime, ASystemDateTime);
    SystemTimeToFileTime(ASystemDateTime, Result);
    LocalFileTimeToFileTime(Result, Result);
  end;

var
  AFile: THandle;
  AFileName: String;
  ACreationTime, ALastAccessTime, ALastWriteTime: TDateTime;
  AFileCreationTime, AFileLastAccessTime, AFileLastWriteTime: TFileTime;
begin
  if FHelp then begin Help(SHelpSystemSetFileTime); exit; end;
  AFileName := GetParam(SParamFileName, 2, ftString, True);
  ACreationTime := isNull(GetParam(SParamCreationTime, 3, ftDateTime), 0);
  ALastAccessTime := isNull(GetParam(SParamLastAccessTime, 4, ftDateTime), 0);
  ALastWriteTime := isNull(GetParam(SParamLastWriteTime, 5, ftDateTime), 0);
  if not FileExists(AFileName) then
    raise Exception.CreateFmt(SErrorFileNotFound, [AFileName]);
  AFile := CreateFile(PChar(AFileName), $0100, 0, nil, OPEN_EXISTING, FILE_FLAG_BACKUP_SEMANTICS, 0);
  try
    if (ACreationTime = 0) or (ACreationTime = 0) or (ACreationTime = 0) then
      GetFileTime(AFile, @AFileCreationTime, @AFileLastAccessTime, @AFileLastWriteTime);
    if ACreationTime <> 0 then AFileCreationTime := DateTimeToPFileTime(ACreationTime);
    if ALastAccessTime <> 0 then AFileLastAccessTime := DateTimeToPFileTime(ALastAccessTime);
    if ALastWriteTime <> 0 then AFileLastWriteTime := DateTimeToPFileTime(ALastWriteTime);
    SetFileTime(AFile, @AFileCreationTime, @AFileLastAccessTime, @AFileLastWriteTime);
  finally
    CloseHandle(AFile);
  end;
end;

procedure TSystemSession.ProcessSetSystemTime;
var
  ST: TSystemTime;
  ADateTime: TDateTime;
  TZI: TTimeZoneInformation;
begin
  if FHelp then begin Help(SHelpSystemSetSystemTime); exit; end;
  ADateTime := GetParam(SParamSystemTime, 2, ftDateTime, True);
  GetTimeZoneInformation(TZI);
  ADateTime := ADateTime + TZI.Bias / 1440;
  with ST do
  begin
    wYear := StrToInt(FormatDateTime('yyyy', ADateTime));
    wMonth := StrToInt(FormatDateTime('mm', ADateTime));
    wDay := StrToInt(FormatDateTime('dd', ADateTime));
    wHour := StrToInt(FormatDateTime('hh', ADateTime));
    wMinute := StrToInt(FormatDateTime('nn', ADateTime));
    wSecond := StrToInt(FormatDateTime('ss', ADateTime));
    wMilliseconds := 0;
  end;
  SetSystemTime(ST);
end;

{ TReportSession }
procedure TReportSession.DoMessage(const Msg: String; LogType: SmallInt);
begin
  Print(Msg);
  RepBuilder.LogAdd('I', Msg, ID);
end;

procedure TReportSession.ReadVariables;
var
  i, APrefixLength: Integer;
begin
  inherited;
  APrefixLength := Length(SReportVariablePrefix);
  with FParams do
  for i := 0 to Pred(Count) do
  with ClassItems[i] do
  if SameText(SReportVariablePrefix, Copy(Name, 1, APrefixLength)) then
    FVariables.Values[Name] := Value;
end;

procedure TReportSession.InternalCreate;
begin
  inherited;
  CoInitializeEx(nil, COINIT_APARTMENTTHREADED);
  FVariables := TVarList.Create(True);
  ReadVariables;
  FReport := TReport.Create(nil);
  FReport.OnLogMessage := DoMessage;
  FConnection := RepBuilder.CreateConnection(Self);
  FReport.MainConnection := FConnection.FDBConnection;
end;

procedure TReportSession.InternalDestroy;
begin
  FReport.MainConnection := nil;
  RepBuilder.FreeConnection(FConnection);
  FReport.Free;
  FVariables.Free;
  CoUninitialize;
  inherited;
end;

procedure TReportSession.ProcessShow;
var
  i, ASize: Integer;
  Buffer: PChar;
  AFileName: String;
  AFile: TFileStream;
begin
  with RepBuilder do LogAdd('S', Format(SDebugReportShow, [Report.DataSetCount]), ID, nil);
  with Report do
  begin
    if not VarIsNull(Self.Params[SRepSysParam_ProcName]) then Report.ProcNamePriority := pnpClient;
    OpenReport(Self.Params);
    AssignVariables(Self.Variables);
    if BuildReport(Self.Params) then
    repeat
      RepBuilder.Lock;
      try
        SaveReport('', rrtInternal);
      finally
         RepBuilder.UnLock;
      end;
    until not BuildReportNext;
  end;
  with RepBuilder do LogAdd('F', Format(SDebugReportShow, [Report.DataSetCount]), ID, Report.OriginalFileNames);
  Buffer := nil;
  try
    FProc.CreateField(1, SFieldFileName, ftString, GetMaxLenStrings(Report.OriginalFileNames));
    FProc.CreateField(2, SFieldData, ftBCD);
    for i := 0 to Pred(Report.OriginalFileNames.Count) do
    try
      AFileName := Report.FileNames[i]; // OriginalFileNames[i];
      AFile := TFileStream.Create(AFileName, fmOpenRead + fmShareDenyWrite);
      try
        ASize := AFile.Size;
        Buffer := StrAlloc(ASize);
        AFile.Read(Buffer[0], StrBufSize(Buffer));
      finally
        AFile.Free;
      end;
      FProc.SetField(1, Report.OriginalFileNames[i]);
      FProc.SetField(2, Buffer, ASize);
      FPRoc.Next;
    finally
      StrDispose(Buffer);
    end;
  finally
    with Report.FileNames do
    for i := 0 to Pred(Count) do
      DeleteFile(Strings[i]);
  end;
end;

procedure TReportSession.ProcessSave;
var
  i: Integer;
  AFileName: String;
begin
  with RepBuilder do LogAdd('S', Format(SDebugReportSave, [Report.DataSetCount]), ID, nil);
  AFileName := isNull(Self.Params[SParamFileName], '');
  if not VarIsNull(Self.Params[SRepSysParam_ProcName]) then Report.ProcNamePriority := pnpClient;
  with Report do
  begin
    OpenReport(Self.Params);
    AssignVariables(Self.Variables);
    if BuildReport(Self.Params) then
    repeat
      RepBuilder.Lock;
      try
        SaveReport(AFileName);
      finally
         RepBuilder.UnLock;
      end;
    until not BuildReportNext;
  end;
  with RepBuilder do LogAdd('F', Format(SDebugReportSave, [Report.DataSetCount]), ID, Report.FileNames);
  for i := 0 to Pred(Report.FileNames.Count) do
    Report.FileNames[i] := ExpandFileName(Report.FileNames[i]);
  FProc.CreateField(1, SFieldFileName, ftString, GetMaxLenStrings(Report.FileNames));
  for i := 0 to Pred(Report.FileNames.Count) do
  begin
    FProc.SetField(1, Report.FileNames[i]);
    FPRoc.Next;
  end;
end;

procedure TReportSession.ProcessPrint;
begin
  raise Exception.CreateFmt(SErrorProcedureUnknown, [SProcNameReportPrint]);
end;

procedure TReportSession.ProcessMail;
var
  i: Integer;
  AFileName: String;
  AMailConfig: TRepMailConfig;
  AEMailTo, ASubject, ATextBody, AHTMLBody: String;
begin
  with RepBuilder do LogAdd('S', Format(SDebugReportMail, [Report.DataSetCount]), ID, nil);
  AFileName := isNull(Self.Params[SParamFileName], '');
  if not VarIsNull(Self.Params[SRepSysParam_ProcName]) then Report.ProcNamePriority := pnpClient;
  with Report do
  begin
    AMailConfig := GetMailConfig(Self.Params);
    OpenReport(Self.Params);
    AssignVariables(Self.Variables);
    if BuildReport(Self.Params) then
    repeat
      RepBuilder.Lock;
      try
        SaveReport(AFileName);
      finally
         RepBuilder.UnLock;
      end;
    until not BuildReportNext;
  end;
  with RepBuilder do LogAdd('F', Format(SDebugReportMail, [Report.DataSetCount]), ID, Report.FileNames);
  // Send Mail
  ASubject := Report.Caption;
  AEMailTo := isNull(Params[SParamMailEMail], '');
  ASubject := isNull(Params[SParamMailSubject], '');
  ATextBody := isNull(Params[SParamMailTextBody], '');
  AHTMLBody := isNull(Params[SParamMailHTMLBody], '');
  with AMailConfig do
    EMail := isNull(Params[SParamMailFrom], EMail);
  SendMail(AMailConfig, AEMailTo, ASubject, ATextBody, AHTMLBody, Report.FileNames.DelimitedText);
  for i := 0 to Pred(Report.OriginalFileNames.Count) do
    Report.FileNames[i] := ExpandFileName(Report.OriginalFileNames[i]);
  FProc.CreateField(1, SFieldFileName, ftString, GetMaxLenStrings(Report.OriginalFileNames));
  for i := 0 to Pred(Report.OriginalFileNames.Count) do
  begin
    FProc.SetField(1, Report.OriginalFileNames[i]);
    FPRoc.Next;
  end;
end;

{ TRepBuilder }
constructor TRepBuilder.Create;
begin
  inherited;
  InitializeCriticalSection(FLock);
  FSessionList := TThreadList.Create;
  FConnectionList := TThreadList.Create;
  FConnectionPool := TThreadList.Create;
  FLoadINI := False;
end;

destructor TRepBuilder.Destroy;
var
  i: Integer;
begin
  try
    LogAdd('S', Format(SDebugGlobalFree, [GetMemSize, GetMemUsed]));
    with FConnectionPool, LockList do
    try
      for i := 0 to Pred(Count) do
        TMSConnection(Items[i]).Free;
    finally
      UnLockList;
    end;
    FConnectionPool.Free;
    FConnectionList.Free;
    FSessionList.Free;
    LogAdd('F', Format(SDebugGlobalFree, [GetMemSize, GetMemUsed]));
  except
    on E: Exception do Error(E.Message);
  end;
  try
    if Assigned(FLogFile) then FLogFile.Free;
    DeleteCriticalSection(FLock);
    inherited;
  except
  end;
end;

procedure TRepBuilder.Lock;
begin
  EnterCriticalSection(FLock);
end;

procedure TRepBuilder.UnLock;
begin
  LeaveCriticalSection(FLock);
end;

procedure TRepBuilder.ReadINI(Reload: Boolean = False);

   function UpdatePath(const APath: String): String;
   begin
     if Trim(APath) <> '' then
       Result := IncludeTrailingPathDelimiter(APath)
     else
       Result := '';
   end;

   function UpdateRequared(const AStr, AName, AError: String): String;
   begin
     if Trim(AStr) = '' then
       raise Exception.CreateFmt(SErrorINIPrefix + AError, [AName]);
     Result := AStr;
   end;

var
  AFileName: String;
begin
  Lock;
  try
    if FLoadINI and not Reload then exit;
    AFileName := ChangeFileExt(GetCurrentModuleName, SINIExt);
    if not FileExists(AFileName) then
      raise Exception.CreateFmt(SErrorININoFile, [AFileName]);
    with TIniFile.Create(AFileName) do
    try
      // Path
      FPathTemplate := UpdatePath(ReadString(SPathSection, SPathTemplates, FPathTemplate));
      FPathDefault := UpdatePath(ReadString(SPathSection, SPathDefault, FPathDefault));
      FPathTemp := UpdatePath(ReadString(SPathSection, SPathTemp, GetTempDir));
      FPathLog := UpdatePath(ReadString(SPathSection, SPathLog, FPathLog));
      // Connection
      FMaxConnections := ReadInteger(SDBSection, SMaxConnections, FMaxConnections);
      FTimeConnection := ReadInteger(SDBSection, STimeConnection, FTimeConnection);
      FServer := UpdateRequared(ReadString(SDBSection, SServer, FServer), SServer, SErrorINIParamServer);
      FDatabase := UpdateRequared(ReadString(SDBSection, SDataBase, FDatabase), SDataBase, SErrorINIParamDataBase);
      FUserName := UpdateRequared(ReadString(SDBSection, SUserName, FUserName), SUserName, SErrorINIParamUserName);
      FPassword := UpdateRequared(ReadString(SDBSection, SPassword, FPassword), SPassword, SErrorINIParamPassword);
      FLoadINI := True;
      LogAdd('S', Format(SDebugGlobalInit, [GetMemSize, GetMemUsed, AFileName, PathTemplate, PathDefault, PathTemp, PathLog, Server, Database, Username]));
    finally
      Free;
    end;
    foReport.TemplatesDir := FPathTemplate;
    foConst.TEMP_DIR := FPathTemp;
    SetCurrentDir(FPathDefault);
  finally
    UnLock;
  end;
end;

procedure TRepBuilder.Error(const Message: String; SessionID: Integer = 0);
begin
  try
    LogAdd('E', Message, SessionID, nil);
  except
  end;
end;

procedure TRepBuilder.LogAdd(MessType: Char; const Message: String; SessionID: Integer = 0; Params: TObject = nil);
var
  PStr: PChar;
  AFileMode: Word;
  i, ALength: Integer;
  AStr, AFileName: String;
begin
  if FPathLog = '' then exit;
  Lock;
  try
    DateTimeToString(AFileName, 'yyyy.mm.dd', Now);
    AFileName := FPathLog + ChangeFileExt(ExtractFileName(GetCurrentModuleName), '')+ '[' + AFileName + ']' + SDebugFileExt;
    if not Assigned(FLogFile) or (FLogFileName <> AFileName) then
    begin
      if Assigned(FLogFile) then FLogFile.Free;
      if FileExists(AFileName) then
        AFileMode := fmOpenWrite or fmShareDenyWrite
      else
        begin
          ForceDirectories(ExtractFileDir(AFileName));
          AFileMode := fmCreate or fmShareDenyWrite;
        end;
      try
        FLogFile := TFileStream.Create(AFileName, AFileMode);
        FLogFile.Position := FLogFile.Size;        
      except
        FLogFile := nil;
        exit;
      end;
    end;
    AStr := Format(SDebugMessage, [TimeToStr(Now), MessType, Message, '']);
    if Assigned(Params) then
    begin
      AStr := AStr + '(';
      if Params is TVarList then
      with TVarList(Params) do
      begin
        if Count = 0 then
          AStr := AStr + SDebugParamsEmpty
        else
          for i := 0 to Pred(Count) do
          with ClassItems[i] do
            AStr := AStr + Name + '=' + VarToStr(Value) + IfThen(i < Pred(Count), ',', '');
      end
      else
      if Params is TStrings then
      with TStrings(Params) do
      begin
        if Count = 0 then
          AStr := AStr + SDebugParamsEmpty
        else
          for i := 0 to Pred(Count) do
            AStr := AStr + Strings[i] + IfThen(i < Pred(Count), ',', '');
      end;
      AStr := AStr + ')';
    end;
    if SessionID > 0 then
      AStr := AStr + '; SI=' + IntToStr(SessionID);
    AStr := AStr + #13#10;
    ALength := Length(AStr);
    PStr := StrNew(PChar(AStr));
    try
      FLogFile.Write(PStr^, ALength);
    finally
      StrDispose(PStr);
    end;
  finally
    UnLock;
  end;
end;

function TRepBuilder.GetSessionCount: Integer;
begin
  with FSessionList, LockList do
  try
    Result := Count;
  finally
    UnlockList;
  end;
end;

function TRepBuilder.GetSessions(Index: Integer): TSession;
begin
  with FSessionList, LockList do
  try
    Result := Items[Index];
  finally
    UnlockList;
  end;
end;

function TRepBuilder.GetConnectionCount: Integer;
begin
  with FConnectionList, LockList do
  try
    Result := Count;
  finally
    UnlockList;
  end;
end;

function TRepBuilder.GetConnections(Index: Integer): TConnection;
begin
  with FConnectionList, LockList do
  try
    Result := Items[Index];
  finally
    UnlockList;
  end;
end;

function TRepBuilder.GetSessionID: Integer;
begin
  InterlockedIncrement(FSessionMaxID);
  Result := FSessionMaxID;
end;


procedure TRepBuilder.CheckConnectionPool;
var
  i: Integer;
  AConnection: TConnection;
begin
  with FConnectionPool, LockList do
  try
    for i := Pred(Count) downto 0 do
    if TConnection(Items[i]).TimeLive >= FTimeConnection then
    begin
      AConnection :=  TConnection(Items[i]);
      FConnectionList.Remove(AConnection);
      Remove(AConnection);
      AConnection.Free;
    end;
  finally
    UnLockList;
  end;
end;

function TRepBuilder.CreateConnection(Session: TSession): TConnection;
begin
  Result := nil;
  Session.FState := ssSuspend;
  if FTimeConnection > 0 then
  begin
    CheckConnectionPool;
    repeat
      with FConnectionPool, LockList do
      try
        if Count > 0 then
          Result := Extract(Items[Pred(Count)]);
      finally
        UnLockList;
      end;
      if not Assigned(Result) then Session.Wait;
    until Assigned(Result) or (ConnectionCount < FMaxConnections)
  end
  else
    while ConnectionCount >= FMaxConnections do Session.Wait;
  if not Assigned(Result) then
  begin
    Result := TConnection.Create;
    FConnectionList.Add(Result);
  end;
  Session.FState := ssConnect;
  Result.Connect(Session);
end;

procedure TRepBuilder.FreeConnection(Connection: TConnection);
begin
  if FTimeConnection > 0 then
  begin
    CheckConnectionPool;
    Connection.FTimeStart := Now;
    FConnectionPool.Add(Connection)
  end
  else
  begin
    FConnectionList.Remove(Connection);
    Connection.Free;
  end;
end;

end.
