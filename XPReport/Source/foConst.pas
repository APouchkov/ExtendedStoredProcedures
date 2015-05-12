(******************************************************************************)
(*  xpReport - foGlobal                                                       *)
(*  Автор: Турянский Александр                                                *)
(*  Версия: 3.5 от 27.03.2008                                                 *)
(******************************************************************************)
unit foConst;

interface

uses
  Classes, DB, MSAccess, VarList;

procedure OpenReportConnection(AConnection: TMSConnection; const AStoredProc: String; AQuery: TCustomMSDataSet; AIndepended: Boolean = False);
procedure CloseReportConnection(AQuery: TCustomMSDataSet);

function  Param(const Name: String; const Value: Variant): TVarItem;
function  ExecSQLString(const ASQL: String; AParams: Array of Variant; AExecuteOnly: Boolean = False; AConnection: TMSConnection = nil): Variant;
function  OpenSQLQuery(const ASQL: String; AParams: Array of Variant; AConnection: TMSConnection = nil): TMSQuery;
procedure CloseSQLQuery(ADataSet: TDataSet);

function  ArrayMerge(S1Array: String; S2Array: String; SDelimeter: Char = ','): String;

function GetMemUsed: Cardinal;
function isStaticPath(const APath: String): Boolean;
function GetMaxLenStrings(AStrings: TStrings): Integer;

const
  lgWarning  = 0;
  lgError    = 1;
  lgInfo     = 2;

var
  TEMP_DIR: String;

resourcestring
  stringEXEC = 'EXEC ';

implementation

uses
  Variants, FileUtil, SysUtils, foReport;

function Param(const Name: String; const Value: Variant): TVarItem;
begin
  Result := TVarClass.Item(Name, Value);
end;

{ Выбор подключения для конкретной Stored-процедуры }
procedure OpenReportConnection(AConnection: TMSConnection; const AStoredProc: String; AQuery: TCustomMSDataSet; AIndepended: Boolean = False);
var
  FServer, FDataBase: String;
  FResult: Variant;

  procedure AcceptConnection;
  begin
    with AQuery.Connection do begin
      if AIndepended then begin
        Tag := 0;
        if AQuery.Connection is TMSConnection then
          TMSConnection(AQuery.Connection).OnInfoMessage := nil;
      end else begin
        Tag := 1;
        if AQuery.Connection is TMSConnection then
          TMSConnection(AQuery.Connection).OnInfoMessage := nil;
      end;

      if not SameText(DataBase, FDataBase) then
        DataBase := FDataBase;

      if not Connected then Connect;
    end;
  end;
    
begin
  FDataBase := AConnection.DataBase;
  FResult := ExecSQLString
            (
              stringEXEC + ' [LINK].[Remote Procedure Destination] @Stored_procedure = :sp, @Cached_servers = :cs, @Result_mode = :rm',
              [AStoredProc, FServer, 'RECORD'], False, AConnection
            );

  FServer   := VarToStr(FResult[0]);
  if not VarIsNull(FResult[1]) then FDataBase := FResult[1];

    // Создаем подключение
    AQuery.Connection := TMSConnection.Create(nil);
    AQuery.Connection.Assign(AConnection);

    with AQuery.Connection do
    try
      LoginPrompt   := False;
      ConnectDialog := nil;
      if FServer <> '' then Server := FServer;
     // AcceptConnection;
    except
      Free;
      AQuery.Connection := nil;
      raise;
    end;

end;

procedure CloseReportConnection(AQuery: TCustomMSDataSet);
begin
  AQuery.Connection.Disconnect;
  AQuery.Connection.Free;
end;

function ExecSQLString(const ASQL: String; AParams: Array of Variant; AExecuteOnly: Boolean = False; AConnection: TMSConnection = nil): Variant;
var
  i: Integer;
  ADataSet: TMSQuery;
begin
  Result := Null;
  ADataSet := TMSQuery.Create(nil);
  with ADataSet do
  try
    if Assigned(AConnection) then
      Connection := AConnection
    else
      raise Exception.Create('Нет соединения 1');
    SQL.Text := ASQL;
    for i := Pred(ParamCount) downto 0 do
      if i <= High(AParams) then
        Params[i].Value := AParams[i]
      else
        Params[i].Clear;
    if AExecuteOnly then
      Execute
    else
      begin
        Open;
        if Fields.Count = 1 then
          if isEmpty then
            Result := Null
          else
            Result := Fields[0].AsVariant
        else
          begin
            Result := VarArrayCreate([0, Pred(Fields.Count)], varVariant);
            for i := Pred(Fields.Count) downto 0 do
            with Fields[i] do
              if isEmpty then
                Result[i] := Null
              else
                case DataType of
                  ftVarBytes: Result[i] := AsString
                  else Result[i] := AsVariant
                end;
          end;
      end;
  finally
    Free;
  end
end;

function OpenSQLQuery(const ASQL: String; AParams: Array of Variant; AConnection: TMSConnection = nil): TMSQuery;
var
  id: Integer;
begin
  Result := nil;
  with TMSQuery.Create(nil) do
  begin
    Assert(Assigned(AConnection));
    SQL.Text := ASQL;
    for id := 0 to ParamCount - 1 do
      if id <= High(AParams) then
        Params[id].Value := AParams[id]
      else
        Params[id].Clear;
    Execute;
  end;
end;

procedure CloseSQLQuery(ADataSet: TDataSet);
begin
  ADataSet.Close;
  ADataSet.Free;
end;

function ArrayMerge(S1Array: String; S2Array: String; SDelimeter: Char = ','): String;
var
  i, l: Integer;
  SElement: String;
begin
  if (S1Array = '*') or (S2Array = '*') then
    Result := '*'
  else begin
    S1Array := Trim(S1Array, SDelimeter);
    S2Array := Trim(S2Array, SDelimeter);
    if (S2Array = '') or (S2Array = SDelimeter) then
      Result := S1Array
    else if (S1Array = '') or (S1Array = SDelimeter) then
      Result := S2Array
    else begin
      Result := S1Array;
      S1Array := SDelimeter + S1Array + SDelimeter;
      l := Length(S2Array);
      while l > 0 do begin
        i := Pos(SDelimeter, S2Array);
        if i>0 then begin
          SElement := Copy(S2Array, 1, Pred(i));
          l := l - i;
          S2Array := Copy(S2Array, Succ(i), l);
        end else begin
          SElement := S2Array;
          l := 0;
        end;
        if Pos(SDelimeter + SElement + SDelimeter, S1Array) = 0 then Result := Result + SDelimeter + SElement;
      end;
    end;
  end;
end;

function GetMemUsed: Cardinal;
begin
  Result := getHeapStatus.TotalAllocated;
end;

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

end.
