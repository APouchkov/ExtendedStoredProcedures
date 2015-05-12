library xp_MasterBank_Limits;

uses
  Windows, SysInit,
  Classes,
  db,
  Variants,
  SysUtils,
  MSODSAPI,
  IniFiles,
  XProc,
  ActiveX,
  Sirius in 'Sirius.pas';

const
  iniServer : String = 'server';
  iniOptions: String = 'options';
  iniOptions_Sender: String = 'Sender';

var
  hMono          : TRTLCriticalSection;
  SaveDllProc    : procedure (Reason: Integer) = nil;

  Sender         : String  = '';
  ConnectOptions : String  = '';
  TranCounter    : Integer = 0;

function GetCurrentModule: String;
var
  Buf : AnsiString;
  Len : Integer;
begin
  Len := 2047;
  SetLength(Buf, Len);
  Len := GetModuleFileName(SysInit.HInstance, PChar(Buf), Len);
  SetLength(Buf, Len);
  Result := Buf;
end;

procedure LibExit(Reason: Integer);
begin
  if Reason = DLL_PROCESS_DETACH then
  begin
    DeleteCriticalSection(hMono);
  end;
  if Assigned(SaveDllProc) then SaveDllProc(Reason);  // call saved entry point procedure
end;

function ReadIni: Boolean;
var
  iniFile: String;
  fValues: TStringList;
begin
  Result := (Length(ConnectOptions) > 0) and (Length(Sender) > 0);
  if not Result then begin
    IniFile := ChangeFileExt(GetCurrentModule, '.ini');

    fValues := TStringList.Create;
    if FileExists(iniFile) then with TIniFile.Create(iniFile) do try
      ReadSectionValues(iniServer,fValues);
      ConnectOptions := fValues.Text;

      ReadSectionValues(iniOptions,fValues);
      Sender := fValues.Values[iniOptions_Sender];
      Result := (Length(ConnectOptions)>0) and (Length(Sender)>0);
    finally
      Free;
      fValues.Free
    end;
  end
end;

function __GetXpVersion : ULONG; cdecl; export;
begin
  Result:=ODS_VERSION;
end;

// xp_MasterBank_SendCmd @XXX, ...
function xp_MasterBank_SendCmd(pSrvProc : SRV_PROC) : SRVRETCODE; cdecl; export;
var
  i: Integer;
  field_name: String;
  fValues: TStringList;
  fTransport : TDXSirius;
begin
  result := 1;
  EnterCriticalSection(hMono);

  try // Ловим ошибку создания объекта XProc
    with TXProc.Create(pSrvProc) do try // Ловим любые ошибки внутри обработчика XProc для его корректного удаления
      CoInitialize(nil);
      try // Любые ошибки XProc должны отразиться в окне сообщений ошибок.
        if ReadIni then begin
          if (ParamCount = 0) then
            Raise Exception.Create('Usage: xp_MasterBank_SendCmd @XXX, @YYY, @ZZZ, ...');

          fValues := TStringList.Create;
          try // Ловим ошибку для корректного удаления объекта fValues
            fValues.Values['002'] := Sender; // Может быть переопределена из параметров
            for i := ParamCount downto 1 do begin
              field_name := GetParamName(i);
              if (Length(field_name) > 0) and (GetParamType(i) = ftString) then
                fValues.Values[field_name] := GetParam(i);
            end;

            CreateField(1, 'ID', ftString, 10);
            CreateField(2, 'VALUE', ftString, 255);

            fTransport := TDXSirius.Create;
            try // Ловим ошибку обработки результата
              fTransport.SetOptions('', ConnectOptions);
              fTransport.Open;
              if fTransport.WriteAlgorithm(fValues) then begin
                fValues.Clear;
                fTransport.ReadAlgorithm(fValues);
                for i := Pred(fValues.Count) downto 0 do begin
                  SetField(1, fValues.Names[i]);
                  SetField(2, fValues.ValueFromIndex[i]);
                  Next
                end
              end;
            finally
              fTransport.Close;
              fTransport.Free;
            end;
          finally
            fValues.Free
          end;
        end
      except on E:Exception do begin
        RaisError(E.Message)
      end end
    finally
      if isSuccess then result := 0;
      Free;
    end;
  except
  end;

  LeaveCriticalSection(hMono);
end;


exports
    xp_MasterBank_SendCmd,
    __GetXpVersion;

begin
  SaveDllProc := DllProc;  // save exit procedure chain
  DllProc     := @LibExit;  // install LibExit exit procedure

  InitializeCriticalSection(hMono);
end.

