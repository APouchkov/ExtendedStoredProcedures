unit formMain;
{$I config.inc}

interface

uses
  {$IFNDEF DEBUG}
  MSODSAPI,
  {$ENDIF}
  MSAccess, VarList;

type
  TRepConnection = record
    Mutex: THandle;
    Connection: TMSConnection;
  end;

var
 Connections: Array of TRepConnection;

 function Init: Integer;
 procedure Free;
 {$IFNDEF DEBUG}
 function xp_Report_Test(pSrvProc: SRV_PROC): SRVRETCODE; cdecl; export;
 function xp_Report_Show(pSrvProc: SRV_PROC): SRVRETCODE; cdecl; export;
 function xp_Report_Save(pSrvProc: SRV_PROC): SRVRETCODE; cdecl; export;
 function xp_Report_Mail(pSrvProc: SRV_PROC): SRVRETCODE; cdecl; export;
 {$ELSE}
 function ReadINI: Boolean;
 function SaveReport(AExternalObject: TObject; AConnection: TMSConnection; AGetMailConfig: Boolean; AFileName: String; AParams: TVarList; AVariables: TVarList): Boolean;
 {$ENDIF}

implementation

uses
  Windows, Classes, SysUtils, Variants, ActiveX, DB,
  {$IFDEF DEBUG}
  Dialogs,
  {$ELSE}
  XProc,
  {$ENDIF}
  foConst, foReport, IniFiles;

type
  TParam = record
    Name: String;
    Value: Variant;
  end;

resourcestring
  // Help
  SRepHelpTitle = 'Процедура формирования отчетов FoReport ';
  SRepHelpParams = 'Обязательные параметры: @Object_id int.';
  SRepHelpSaveParams = 'Обязательные параметры: @Object_id int, @FileName varchar(8000).';
  SRepHelpMailParams = 'Обязательные параметры: @Object_id int, @EMail varchar(8000).';
  SRepHelpParamsEx = 'Дополнительные: @Proc_name sysname, @Attribute int, @Object_type tinyint.';
  SRepHelpMailParamsEx = 'Дополнительные: @Proc_name sysname, Subject varchar(8000), Textbody varchar(8000), HTMLBody varchar(8000), @Attribute int, @Object_type tinyint.';
  SRepHelpVariables = 'Переменные передаются через параметры: "Variable_<имя переменной>"';

  // Test
  STestParamCount = 'Количество переданных параметров: %d';
  STestVariable = 'Переменная: "%s": %s';
  STestParam = 'Параметр "%s": %s';

  // Report
  SVariablePrefix = 'Variable_';

  // Params
  SRepFileName = 'FileName';
  SRepMailEMail = 'EMail';
  SRepMailSubject = 'Subject';
  SRepMailTextBody = 'TextBody';
  SRepMailHTMLBody = 'HTMLBody';

  // Debug
  SRepDebugExt = '.log';
  SRepDebugMessageFmt = '%s(%s) %s: %s';
  SRepDebugError = 'ERROR: %s';
  SRepDebugSuccess = ' ...';
  SRepDebugShow = 'Show_Report';
  SRepDebugSave = 'Save_Report: %s';
  SRepDebugMail = 'Mail_Report';
  SRepDebugInitialization = 'Инициализация (%s)';
  SRepDebugFinalization = 'Выгрузка (%s)';
  SRepDebugConnect = 'Подключение к основному серверу(%d): Server=''%s'', Database=''%s'', Username=''%s''';
  SRepDebugParams = 'Получение параметров отчета';
  SRepDebugMailConfig = 'Получение конфигурации отправки почты';
  SRepDebugBuild = 'Получение данных для отчета';
  SRepDebugFileSave = 'Построение отчета(ов): ''%s''';
  SRepDebugFree = 'Завершение построения отчета';
  SRepDebugParam = 'Параметры:';
  SRepDebugVariables = 'Переменные:';
  SRepDebugEmpty = '(нет)';
  SRepDebugLineBreak = '=============================================================================================================';

  // Fields
  SRepFieldFileName = 'FileName';
  SRepFieldData = 'Data';

  // Errors
  SRepErrorNoParam = 'Не найден или пуст обязательный параметр "%s"';
  SRepErrorNoFileINI = 'Не найден файл настроек "%s"';

  // INI
  SExt = '.ini';
  SMaxConnections = 'MaxConnections';
  STemplatesPath = 'Templates';
  SDefaultPath = 'Default';
  STempPath = 'TempDir';
  SReportPath = 'Report';
  SPathSection = 'Path';
  SDBSection = 'Connection';
  SServerName = 'ServerName';
  SDataBaseName = 'DatabaseName';
  SLoginName = 'UserName';
  SPassword = 'Password';
  SDebugSection = 'Debug';
  SDebugLogFilePath = 'LogFilePath';

var
  FCount: Integer = 0;
  FLock: TRTLCriticalSection;
  FInitialization: Boolean = False;
  FDateTimeInit: TDateTime = 0;
  MailConfig: TRepMailConfig;
  ReportTitle: String = '';
  // Report ini
  FMaxConnections: Integer = 0;
  FTemplatePath: String = '';
  FDefaultPath: String = '';
  FTempPath: String = '';
  FServerName: String = '';
  FDatabaseName: String = '';
  FLoginName: String = '';
  FPassword: String = '';
  FDebugLogFilePath: String = '';

function GetCurrentModule: String;
var
  Buf: AnsiString;
  Len: Integer;
begin
  Len := 2000;
  SetLength(Buf, Len);
  Len := GetModuleFileName(SysInit.HInstance, PChar(Buf), Len);
  SetLength(Buf, Len);
  Result := Buf;
end;

function ReadINI: Boolean;

  function PreparePath(const Path: String): String;
  begin
    Result := Path;
    if Result[Length(Result)] <> '\' then Result := Result + '\';
  end;

var
  AFileName: String;
begin
  AFileName := ChangeFileExt(GetCurrentModule, SExt);
  Result := FileExists(AFileName);
  if not Result then exit;
  with TIniFile.Create(AFileName) do
  try
    FTemplatePath := PreparePath(ReadString(SPathSection, STemplatesPath, EmptyStr));
    FDefaultPath := PreparePath(ReadString(SPathSection, SDefaultPath, EmptyStr));
    FTempPath := PreparePath(ReadString(SPathSection, STempPath, TEMP_DIR));
    FMaxConnections := ReadInteger(SDBSection, SMaxConnections, 1);
    FServerName := ReadString(SDBSection, SServerName, EmptyStr);
    FDataBaseName := ReadString(SDBSection, SDataBaseName, EmptyStr);
    FLoginName := ReadString(SDBSection, SLoginName, EmptyStr);
    FPassword := ReadString(SDBSection, SPassword, EmptyStr);
    FDebugLogFilePath := ReadString(SDebugSection, SDebugLogFilePath, EmptyStr);
  finally
    Free;
  end;
end;

{$IFNDEF DEBUG}
procedure PrintMessage(XProc: TXProc; const AMessage: String);
begin
  XProc.Print(AMessage);
end;
{$ENDIF}

procedure DebugMessage(const AMessage: String; Success: Boolean = True; const Prefix: String = '');
var
  ALogFile: TextFile;
  AFileName, APrefix: String;
begin
  EnterCriticalSection(FLock);
  try
    if FDebugLogFilePath = '' then exit;
    try
      APrefix := Prefix;
      DateTimeToString(AFileName, 'yyyy.mm.dd', Now);
      AFileName := FDebugLogFilePath + '\' + ChangeFileExt(ExtractFileName(GetCurrentModule), '')+ '[' + AFileName + ']' + SRepDebugExt;
      AssignFile(ALogFile, AFileName);
      if FileExists(AFileName) then
        Append(ALogFile)
      else
      begin
        ForceDirectories(ExtractFileDir(AFileName));
        Rewrite(ALogFile);
      end;
      if APrefix = '' then
      begin
        if Success then
          APrefix := 'F'
        else
          APrefix := 'S';
      end;
      if APrefix = 'L' then
        Writeln(ALogFile, '')
      else
      if APrefix = 'LN' then
        Writeln(ALogFile, SRepDebugLineBreak)
      else
        Writeln(ALogFile, Format(SRepDebugMessageFmt, [TimeToStr(Now), TimeToStr(FDateTimeInit), APrefix, AMessage]));
      Flush(ALogFile);
      CloseFile(ALogFile);
    except
    end;
  finally
    LeaveCriticalSection(FLock);
  end;    
end;

procedure DebugLineBreak(Line: Boolean = False);
begin
  if Line then
    DebugMessage('', True, 'LN')
  else
    DebugMessage('', True, 'L');
end;

procedure DebugParams(const ACaption: String; AParams: TVarList);
var
  i: Integer;
begin
  DebugMessage(ACaption, True, 'P');
  if AParams.Count = 0 then
    DebugMessage('    ' + SRepDebugEmpty, True, 'P')
  else
  for i := 0 to Pred(AParams.Count) do
  with AParams.ClassItems[i] do
    DebugMessage('    ' + Name + '=' + VarToStr(Value), True, 'P');
end;

function Init: Integer;
begin
  Result := -1;
  EnterCriticalSection(FLock);
  try
    if not FInitialization then
    begin
      // Read INI File
      if not ReadINI then
        raise ERepError.CreateFmt(SRepErrorNoFileINI, [ExtractFileName(ChangeFileExt(GetCurrentModule, SExt))]);
      FDateTimeInit := Now;
      if FMaxConnections <= 0 then FMaxConnections := 1;
      DebugLineBreak(True);
      DebugMessage(Format(SRepDebugInitialization, [TimeToStr(FDateTimeInit)]));
      SetLength(Connections, FMaxConnections);
      FInitialization := True;
    end;
    {
    repeat
      // White empty connection
      for i := 0 to Pred(FMaxConnections) do
      begin
      //  if WaitForSingleObject(Connections[i].Mutex, 100) <> WAIT_TIMEOUT then
        begin
          Result := i;
          break;
        end;
      end;
    until Result >= 0;
    }
  finally
    LeaveCriticalSection(FLock);
  end;
end;

procedure InitDB(AIndex: Integer);
var
  i: Integer;
begin
  EnterCriticalSection(FLock);
  try
    // Connections[i].Mutex := CreateMutex(nil, False, nil);
    if not Assigned(Connections[AIndex].Connection) then
    begin
      Connections[AIndex].Connection := TMSConnection.Create(nil);
      with Connections[AIndex].Connection do
      begin
        Server := FServerName;
        Database := FDataBaseName;
        Username := FLoginName;
        Password := FPassword;
      end;
    end;
    // TODO: ??? Test connection ???
    if not Connections[AIndex].Connection.Connected then
    begin
      DebugMessage(Format(SRepDebugConnect, [AIndex, FServerName, FDataBaseName, FLoginName]), False);
      Connections[AIndex].Connection.Connect;
      DebugMessage(Format(SRepDebugConnect, [AIndex, FServerName, FDataBaseName, FLoginName]));
    end;
    FInitialization := True;
  finally
    LeaveCriticalSection(FLock);
  end;
end;

procedure Free;
var
  i: Integer;
begin
  EnterCriticalSection(FLock);
  try
    if not FInitialization then exit;
    DebugLineBreak;    
    DebugMessage(Format(SRepDebugFinalization, [TimeToStr(FDateTimeInit)]), False);
    for i := 0 to Pred(FMaxConnections) do
    begin
      if Assigned(Connections[i].Connection) then
      begin
        Connections[i].Connection.Disconnect;
        Connections[i].Connection.Free;
      end;
    end;
    SetLength(Connections, 0);
    FInitialization := False;
    DebugMessage(Format(SRepDebugFinalization, [TimeToStr(FDateTimeInit)]));
  finally
    LeaveCriticalSection(FLock);
  end;
end;

procedure DoMessage(Sender: TObject; const Msg: String; MessageType: TRepMessageType);
{$IFNDEF DEBUG}
var
  XProc: TXProc;
{$ENDIF}
begin
  {$IFNDEF DEBUG}
  XProc := Sender as TXProc;
  {$ENDIF}
  if MessageType in [rmtError, rmtLogError] then
  begin
    DebugMessage(Msg, True, 'E');
    {$IFDEF DEBUG}
    MessageDlg(Msg, mtError, [mbOk], 0);
    {$ELSE}
    XProc.RaisError(Msg)
    {$ENDIF}
  end
  else
  begin
    DebugMessage(Msg, True, 'I');
    {$IFDEF DEBUG}
    MessageDlg(Msg, mtInformation, [mbOk], 0);
    {$ELSE}
    XProc.Print(Msg);
    {$ENDIF}
  end;
end;

{$IFNDEF DEBUG}
procedure CheckParamType(const Name: String; FieldType, DataType: TFieldType);
begin
  // TODO: !!!
end;

function GetParamByNameEx(XProc: TXProc; const Name: String; FieldType: TFieldType; Required: Boolean = True): Variant;
var
  AIndex: Integer;
begin
  Result := Null;
  AIndex := XProc.GetParamIDByName(Name);
  if (AIndex > 0) and not XProc.GetParamIsNull(AIndex) then
  begin
    CheckParamType(Name, FieldType, XProc.GetParamType(AIndex));
    Result := XProc.GetParam(AIndex);
  end;
  if Required and VarIsNull(Result) then
    raise ERepError.CreateFmt(SRepErrorNoParam, [Name]);
end;

function xp_Report_Test(pSrvProc: SRV_PROC): SRVRETCODE; cdecl; export;
type
  TBuffer = Array [0..8000] of Char;
  PBuffer = ^TBuffer;
var
  XProc: TXProc;
  AName: String;
  i, ACount: Integer;
begin
  Result := SUCCEED;
  XProc := TXProc.Create(pSrvProc);
  with XProc do
  try
    try
      Print(SRepHelpTitle + SRepVersion);
      ACount := Length(SVariablePrefix);
      Print(Format(STestParamCount, [ParamCount]));
      for i := ParamCount downto 1 do
      begin
        AName := GetParamName(i);
        if SameText(SVariablePrefix, Copy(AName, 1, ACount)) then
          Print(Format(STestVariable, [Copy(AName, Succ(ACount), Length(AName) - Pred(ACount)), VarToStr(GetParam(i))]))
        else
          Print(Format(STestParam, [AName, VarToStr(GetParam(i))]));
      end;
    except
      on E: Exception do begin RaisError(E.Message); Result := FAIL; end;
    end;
  finally
    Free;
  end;
end;
{$ENDIF}

function SaveReport(AExternalObject: TObject; AConnection: TMSConnection; AGetMailConfig: Boolean; AFileName: String; AParams: TVarList; AVariables: TVarList; AFileNames: TStrings): Boolean;
var
  ARep: TRep;
begin
  Result := True;
  TemplateDir := FTemplatePath;
  ARep := TRep.Create(nil);
  try
    ARep.ExternalObject := AExternalObject;
    ARep.MainConnection := AConnection;
    ARep.OnLogMessageExternal := DoMessage;
    DebugMessage(SRepDebugParams, False);
    ARep.OpenReport(AParams);
    DebugMessage(SRepDebugParams);
    if AGetMailConfig then
    begin
      DebugMessage(SRepDebugMailConfig, False);
      MailConfig := ARep.GetMailConfig([]);
      DebugMessage(SRepDebugMailConfig);
    end;
    with ARep do
    begin
      AssignVariables(AVariables);
      DebugMessage(SRepDebugBuild, False);
      if BuildReport(AParams) then
      begin
        DebugMessage(SRepDebugBuild);
        DebugMessage(Format(SRepDebugFileSave, [ExtractFilePath(AFileName) + PrepareFileName(AFileName)]), False);
        SaveReport(ExtractFilePath(AFileName) + PrepareFileName(AFileName));
        DebugMessage(Format(SRepDebugFileSave, [ExtractFilePath(AFileName) + PrepareFileName(AFileName)]));
        DebugMessage(SRepDebugBuild, False);
        while BuildReportNext do
        begin
          DebugMessage(SRepDebugBuild);
          DebugMessage(Format(SRepDebugFileSave, [ExtractFilePath(AFileName) + PrepareFileName(AFileName)]), False);
          SaveReport(ExtractFilePath(AFileName) + PrepareFileName(AFileName));
          DebugMessage(Format(SRepDebugFileSave, [ExtractFilePath(AFileName) + PrepareFileName(AFileName)]));
          DebugMessage(SRepDebugBuild, False);
        end;
        DebugMessage(SRepDebugBuild);
      end;
    end;
    if Assigned(AFileNames) then AFileNames.Assign(ARep.FileNames);
    ReportTitle := ARep.Caption;
  finally
    DebugMessage(SRepDebugFree, False);
    ARep.Free;
    DebugMessage(SRepDebugFree);
  end;
end;

{$IFNDEF DEBUG}
function isStaticPath(const APath: String): Boolean;
begin
  if (Length(APath) >= 2) and (Copy(APath, 1, 2) = '\\') then
    Result := True
  else
    if Pos(':', APath) > 0 then Result := True
  else
    Result := False;
end;

function GetMaxLenStrings(AStrings: TStrings): Integer;
var
  i: Integer;
begin
  Result := 0;
  for i := 0 to Pred(AStrings.Count) do
    if Result < Length(AStrings[i]) then
      Result := Length(AStrings[i]);
  if Result = 0 then Result := 1;
end;

function xp_Report_Show(pSrvProc: SRV_PROC): SRVRETCODE; cdecl; export;
var
  F: File;
  XProc: TXProc;
  Buffer: PChar;
  AFileNames: TStrings;
  AName, AFileName, AThreadID: String;
  AParams, AVariables: TVarList;
  i, ACount, ALen, Size, AIndex: Integer;
begin
//    Buffer := nil;
  EnterCriticalSection(FLock);
  try
    Inc(FCount);
    AThreadID := '(' + IntToStr(FCount) + ')';
  finally
    LeaveCriticalSection(FLock);
  end;
  AIndex := Init;
  Result := SUCCEED;
  DebugMessage(AThreadID + SRepDebugShow, False);
  Sleep(100);
  DebugMessage(AThreadID + SRepDebugShow);
{
  try
    EnterCriticalSection(FLock);
    try
      DebugMessage(AThreadID + 'Sleep(100)', False);
      Sleep(100);
    finally
      LeaveCriticalSection(FLock);
    end;

    EnterCriticalSection(FLock);
    try
      DebugMessage(AThreadID + 'XProc := TXProc.Create(pSrvProc);', False);
   //   XProc := TXProc.Create(pSrvProc);
    finally
      LeaveCriticalSection(FLock);
    end;

    EnterCriticalSection(FLock);
    try
      DebugMessage(AThreadID + 'CreateField(1, SRepFieldFileName, ftString, 255);', False);
    //  XProc.CreateField(1, SRepFieldFileName, ftString, 255);
    //  XProc.CreateField(2, SRepFieldData, ftBCD);
    finally
      LeaveCriticalSection(FLock);
    end;

    EnterCriticalSection(FLock);
    try
      DebugMessage(AThreadID + 'XProc.Free', False);
    //  XProc.Free;
    finally
      LeaveCriticalSection(FLock);
    end;
   }

   {
        EnterCriticalSection(FLock);
      try
//        XProc := TXProc.Create(pSrvProc);
      finally
        LeaveCriticalSection(FLock);
      end;
      with XProc do
      try
        EnterCriticalSection(FLock);
        try
          CreateField(1, SRepFieldFileName, ftString, 255);
          CreateField(2, SRepFieldData, ftBCD);
          Sleep(100);
        finally
          LeaveCriticalSection(FLock);
        end;

      finally
        EnterCriticalSection(FLock);
        try
          XProc.Free;
        finally
          LeaveCriticalSection(FLock);
        end;

      end;   }
      {
    except
      on E: Exception do DebugMessage(E.Message, True, 'E');
    end;
    DebugMessage(AThreadID + SRepDebugShow);
    }
{  try
    AFileNames := TStringList.Create;
    AFileNames := TStringList.Create;
    AParams := TVarList.Create(True);
    AVariables := TVarList.Create(True);
    try
      XProc := TXProc.Create(pSrvProc);
      with XProc do
      try
        try
         DebugMessage('Флаг запуска = ' + IntToStr(FCount));
        // If without params then HELP
        if XProc.ParamCount = 0 then
        begin
          Print(SRepHelpTitle + SRepVersion);
          Print(SRepHelpParams);
          Print(SRepHelpParamsEx);
          Print(SRepHelpVariables);
          exit;
        end;
        // Prepare report params
        if not isStaticPath(FTempPath) then
          begin
            if FTempPath <> '\' then
              AFileName := RepTempFile(FDefaultPath + FTempPath)
            else
              AFileName := RepTempFile(FDefaultPath);
          end
        else
          AFileName := FTempPath;
        ALen := Length(ExtractFilePath(AFileName));
        // Prepare params & variables
        ACount := Length(SVariablePrefix);
        for i := ParamCount downto 1 do
        begin
          AName := GetParamName(i);
          if SameText(SVariablePrefix, Copy(AName, 1, ACount)) then
            AVariables[Copy(AName, Succ(ACount), Length(AName) - Pred(ACount))] := GetParam(i)
          else
            AParams[AName] := GetParam(i);
        end;
        DebugParams(SRepDebugParam, AParams);
        DebugParams(SRepDebugVariables, AVariables);
       // if not SaveReport(XProc, Connections[AIndex].Connection, False, AFileName, AParams, AVariables, AFileNames) then exit;
        // Select files
        CreateField(1, SRepFieldFileName, ftString, GetMaxLenStrings(AFileNames) - ALen);
        CreateField(2, SRepFieldData, ftBCD);
        for i := 0 to Pred(AFileNames.Count) do
        begin
          try
            AssignFile(F, AFileNames[i]);
            Reset(F, 1);
            Size := FileSize(F);
            GetMem(Buffer, Size);
            BlockRead(F, Buffer^, Size);
            SetField(1, Copy(AFileNames[i], Succ(ALen), Length(AFileNames[i]) - Pred(ALen)));
            SetField(2, Buffer, Size);
          finally
            CloseFile(F);
            DeleteFile(AFileNames[i]);
          end;
          Next;
        end;
       except
        on E: Exception do
        begin
          DebugMessage(E.Message, True, 'E');
          RaisError(E.Message);
          Result := FAIL;
        end;
       end;
      finally
        Free;
      end;
    finally
      AParams.Free;
      AVariables.Free;
      FreeMem(Buffer);
    end;
  except
    on E: Exception do DebugMessage(E.Message, True, 'E');
  end;
  AFileNames.Free;}

end;

function xp_Report_Save(pSrvProc: SRV_PROC): SRVRETCODE; cdecl; export;
type
  TBuffer = Array [0..8000] of Char;
  PBuffer = ^TBuffer;
var
  XProc: TXProc;
  AFileNames: TStrings;
  i, ACount, AIndex: Integer;
  AName, AFileName: String;
  AParams, AVariables: TVarList;
begin
{
  Result := SUCCEED;
  AIndex := Init;
  DebugLineBreak;
  DebugMessage(Format(SRepDebugShow, [AFileName]), False);
  CoInitializeEx(nil, COINIT_APARTMENTTHREADED);
  try
    try
      FileNames := TStringList.Create;
      AParams := TVarList.Create(True);
      AVariables := TVarList.Create(True);
      try
        XProc := TXProc.Create(pSrvProc);
        with XProc do
        try
          try
            // If without params then HELP
            if XProc.ParamCount = 0 then
            begin
              Print(SRepHelpTitle + SRepVersion);
              Print(SRepHelpSaveParams);
              Print(SRepHelpParamsEx);
              Print(SRepHelpVariables);
              exit;
            end;
            // Prepare report params
            if not isStaticPath(ExtractFilePath(AFileName)) then
            begin
              if AFileName <> '\' then
                AFileName := FDefaultPath + AFileName
              else
                AFileName := FDefaultPath;
            end;
             // Prepare params & variables
            ACount := Length(SVariablePrefix);
            for i := ParamCount downto 1 do
            begin
              AName := GetParamName(i);
              if SameText(SVariablePrefix, Copy(AName, 1, ACount)) then
                AVariables[Copy(AName, Succ(ACount), Length(AName) - Pred(ACount))] := GetParam(i)
              else
                AParams[AName] := GetParam(i);
            end;
            DebugParams(SRepDebugParam, AParams);
            DebugParams(SRepDebugVariables, AVariables);
            if not SaveReport(XProc, Connections[AIndex].Connection, False, AFileName, AParams, AVariables) then exit;
            // Select files
            CreateField(1, SRepFieldFileName, ftString, GetLenFileNames);
            for i := 0 to Pred(FileNames.Count) do
            begin
              SetField(1, FileNames[i]);
              Next;
            end;
          except
            on E: Exception do
            begin
              DebugMessage(E.Message, True, 'E');
              RaisError(E.Message);
              Result := FAIL;
            end;
          end;
        finally
          Free;
        end;
      finally
        AParams.Free;
        AVariables.Free;
        FileNames.Free;
      end;
    except
      on E: Exception do DebugMessage(E.Message, True, 'E');
    end;
  finally
    CoUninitialize;
  end;
  DebugMessage(Format(SRepDebugShow, [AFileName]));
  }
end;

function xp_Report_Mail(pSrvProc: SRV_PROC): SRVRETCODE; cdecl; export;
var
  XProc: TXProc;
  Buffer: PChar;
  AFileNames: TStrings;
  i, ACount, ALen, AIndex: Integer;
  AName, AFileName: String;
  AParams, AVariables: TVarList;
  AEMailTo, ASubject, ATextBody, AHTMLBody: String;
begin
{
  Buffer := nil;
  Result := SUCCEED;
  AIndex := Init;
  DebugLineBreak;
  DebugMessage(SRepDebugMail, False);
  CoInitializeEx(nil, COINIT_APARTMENTTHREADED);
  try
    try
      FileNames := TStringList.Create;
      AParams := TVarList.Create(True);
      AVariables := TVarList.Create(True);
      try
        XProc := TXProc.Create(pSrvProc);
        with XProc do
        try
          try
            // If without params then HELP
            if XProc.ParamCount = 0 then
            begin
              Print(SRepHelpTitle + SRepVersion);
              Print(SRepHelpMailParams);
              Print(SRepHelpMailParamsEx);
              Print(SRepHelpVariables);
              exit;
            end;
            // Read INI File
            if not ReadINI then
              raise ERepError.CreateFmt(SRepErrorNoFileINI, [ExtractFileName(ChangeFileExt(GetCurrentModule, SExt))]);
            // Prepare report params
            if not isStaticPath(FTempPath) then
              begin
                if FTempPath <> '\' then
                  AFileName := RepTempFile(FDefaultPath + FTempPath)
                else
                  AFileName := RepTempFile(FDefaultPath);
              end
            else
              AFileName := FTempPath;
            ALen := Length(ExtractFilePath(AFileName));
            // Prepare params & variables
            ASubject := isNull(GetParamByNameEx(XProc, SRepMailSubject, ftString, False), '');
            ATextBody := isNull(GetParamByNameEx(XProc, SRepMailTextBody, ftString, False), '');
            AHTMLBody := isNull(GetParamByNameEx(XProc, SRepMailHTMLBody, ftString, False), '');
            ACount := Length(SVariablePrefix);
            for i := ParamCount downto 1 do
            begin
              AName := GetParamName(i);
              if SameText(SVariablePrefix, Copy(AName, 1, ACount)) then
                AVariables[Copy(AName, Succ(ACount), Length(AName) - Pred(ACount))] := GetParam(i)
              else
                AParams[AName] := GetParam(i);
            end;
            DebugParams(SRepDebugParam, AParams);
            DebugParams(SRepDebugVariables, AVariables);
            // Save Report
            if not SaveReport(XProc, Connections[AIndex].Connection, True, AFileName, AParams, AVariables) then exit;
            // Send Mail
            if Trim(ASubject) = '' then ASubject := ReportTitle;
            SendMail(MailConfig, AEMailTo, ASubject, ATextBody, AHTMLBody, FileNames.DelimitedText);
            // Select files
            CreateField(1, SRepFieldFileName, ftString, GetLenFileNames - ALen);
            for i := 0 to Pred(FileNames.Count) do
            begin
              SetField(1, Copy(FileNames[i], Succ(ALen), Length(FileNames[i]) - Pred(ALen)));
              Next;
            end;
           except
            on E: Exception do
            begin
              DebugMessage(E.Message, True, 'E');
              RaisError(E.Message);
              Result := FAIL;
            end;
          end;
        finally
          Free;
        end;
      finally
        AParams.Free;
        AVariables.Free;
        FileNames.Free;
        FreeMem(Buffer);
      end;
    except
      on E: Exception do DebugMessage(E.Message, True, 'E');
    end;
  finally
    CoUninitialize;
  end;
  DebugMessage(SRepDebugMail);
  }
end;
{$ENDIF}

initialization
  InitializeCriticalSection(FLock);
  CoInitializeEx(nil, COINIT_APARTMENTTHREADED);

finalization
  Free;
  CoUninitialize;
  DeleteCriticalSection(FLock);

end.

