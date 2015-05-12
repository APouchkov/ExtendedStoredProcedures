library xp_SignalCom;

uses
  Windows, SysInit,
  Classes,
  db,
  Variants,
  SysUtils,
  FileUtil,
  MSODSAPI,
  IniFiles,
  XProc;

type
  DISTINGUISHED_NAME = record
    Country: PChar;
    StateOrProvince: PChar;
    Locality: PChar;
    Organization: PChar;
    OrganizationalUnit: PChar;
    Title: PChar;
    CommonName: PChar;
    EmailAddress: PChar;
  end;

  CERTIFICATE_INFO = record
    Version: PChar;
    SerialNumber: PChar;
    NotBefore: PChar;
    NotAfter: PChar;
    Issuer: DISTINGUISHED_NAME;
    Subject: DISTINGUISHED_NAME;
    PublicKey: PChar;
    X509v3Extensions: PChar;
    Signature: PChar;
    Text: PChar;
  end;

  PCERTIFICATE_INFO = ^CERTIFICATE_INFO;

  CERTIFICATE_REQ_INFO = record
    Version: PChar;
    Subject: DISTINGUISHED_NAME;
    PublicKey: PChar;
    Signature: PChar;
    Text: PChar;
  end;

  PCERTIFICATE_REQ_INFO = ^CERTIFICATE_REQ_INFO;

procedure ClearBuffer(inp: PChar); stdcall; external 'mespro.dll';
procedure FreeBuffer(ptr: Pointer); stdcall; external 'mespro.dll';

function PKCS7Init(pse_path: PChar; reserved: Integer): Integer; stdcall; external 'mespro.dll';
function PKCS7Final(): Integer; stdcall; external 'mespro.dll';

function PSE31_Generation(pse_path: PChar; reserv1: Integer; reserv2: PChar; flags: Integer): Integer; stdcall; external 'mespro.dll';
function SetNewKeysAlgorithm(algor: PChar): Integer; stdcall; external 'mespro.dll';

function SetCountry(Country: PChar): Integer; stdcall; external 'mespro.dll';
function SetStateOrProvince(StateOrProvince: PChar): Integer; stdcall; external 'mespro.dll';
function SetLocality(Locality: PChar): Integer; stdcall; external 'mespro.dll';
function SetOrganization(Organization: PChar): Integer; stdcall; external 'mespro.dll';
function SetOrganizationalUnit(OrganizationalUnit: PChar): Integer; stdcall; external 'mespro.dll';
function SetTitle(Title: PChar): Integer; stdcall; external 'mespro.dll';
function SetCommonName(CommonName: PChar): Integer; stdcall; external 'mespro.dll';
function SetEmailAddress(EmailAddress: PChar): Integer; stdcall; external 'mespro.dll';

function NewKeysGenerationEx(pse_path: PChar; reserv: PChar; keyfile: PChar; password: PChar; reqfile: PChar): Integer; stdcall; external 'mespro.dll';

function GetCertificateInfoBufferEx(buf: PChar; ln: Integer; info: PCERTIFICATE_INFO): Integer; stdcall; external 'mespro.dll';
function GetCertPublicKeyAlgorithmBuffer(buf: PChar; ln: Integer): PChar; stdcall; external 'mespro.dll';
procedure FreeCertificateInfo(info: PCERTIFICATE_INFO); stdcall; external 'mespro.dll';

function  GetRequestInfoBuffer(buf: PChar; ln: Integer; info: PCERTIFICATE_REQ_INFO): Integer; stdcall; external 'mespro.dll';
procedure FreeRequestInfo(info: PCERTIFICATE_REQ_INFO); stdcall; external 'mespro.dll';

function AddPSEPrivateKeyFromBufferEx(pse_path, reserv, buf: PChar; len: Integer; pass: PChar): Integer; stdcall; external 'mespro.dll';
function AddCAs(CAdir: PChar): Integer; stdcall; external 'mespro.dll';
function GetSignCTX: Pointer; stdcall; external 'mespro.dll';
function AddSigner(ctx: Pointer; xtype: Integer; param1,param2: PChar): Integer; stdcall; external 'mespro.dll';
procedure FreeSignCTX(ctx: Pointer); stdcall; external 'mespro.dll';

function InsertCertificateToSign(insert: Integer): Integer; stdcall; external 'mespro.dll';
function InsertSigningTimeToSign(Insert: Integer): Integer; stdcall; external 'mespro.dll';

function SignBufferEx(sign_ctx: Pointer; in_buf: Pointer; in_len: Integer;
                             out_buf: PPointer; out_len: PInteger; detach: Integer): Integer; stdcall; external 'mespro.dll';
function CheckBufferSignEx(sign_ctx: Pointer; in_buf: Pointer; in_len: Integer;
                                  out_buf: PPointer; out_len: PInteger; sign_del: Integer;
								  detach: Pointer; detach_ln: Integer): Integer; stdcall; external 'mespro.dll';

function  GetSignatureCount(ctx: Pointer): Integer; stdcall; external 'mespro.dll';
function  GetSignatureSubject(ctx: Pointer; ind: Integer): PChar; stdcall; external 'mespro.dll';
function  GetSignatureIssuer(ctx: Pointer; ind: Integer): PChar; stdcall; external 'mespro.dll';
function  GetSignatureStatus(ctx: Pointer; ind: Integer): Integer; stdcall; external 'mespro.dll';
function  GetSignatureCertInBuffer(ctx: Pointer; ind: Integer; buf: Pointer; ln: PInteger): Integer; stdcall; external 'mespro.dll';
function  GetSignatureTime(ctx: Pointer; ind: Integer): PChar; stdcall; external 'mespro.dll';


const
  iniOptions: String = 'options';
  iniCertificate: String = 'certificate';

  ERR_INVALID_PARAMS: String = 'ERR_INVALID_PARAMS: Неверные параметры процедуры';
  ERR_LOAD_INI: String = 'ERR_LOAD_INI: Ini-файл не может быть загружен.';
  ERR_CRYPT_LIB: String = 'ERR_CRYPT_LIB: Нераспознанная ошибка криптографической библиотеки.';

  MAX_FILE_SIZE  = 5120;

  Field_KeysAlgorithm   : String = 'KeysAlgorithm';
  Field_Country         : String = 'Country';
  Field_Locality        : String = 'Locality';
  Field_StateOrProvince : String = 'StateOrProvince';
  Field_Organization    : String = 'Organization';
  Field_OrganizationUnit: String = 'OrganizationUnit';
  Field_Title           : String = 'Title';
  Field_CommonName      : String = 'CommonName';
  Field_EmailAddress    : String = 'EmailAddress';

  Field_Serial          : String = 'Serial';
  Field_DateBegin       : String = 'DateBegin';
  Field_DateEnd         : String = 'DateEnd';
//  Field_Issuer          : String = 'Issuer';
  Field_Certificate     : String = 'Certificate';
  Field_Result          : String = 'Result';
  Field_SignDate        : String = 'SignDate';

// MesPro DLL Const
  BY_FILE			  = 0;
  BY_SUBJECT	  = 1;
  BY_SERIAL		  = 2;
  BY_COMPONENTS	=	3;
  BY_BUFFER			= 4;

  FileName_kek_opq   : String = 'kek.opq';
  FileName_masks_db3 : String = 'masks.db3';
  FileName_mk_db3    : String = 'mk.db3';
  FileName_rand_opq  : String = 'rand.opq';

var
  hMono         : TRTLCriticalSection;
  SaveDllProc   : procedure (Reason: Integer) = nil;

  Country          : String = '';
  StateOrProvince  : String = '';
  Locality         : String = '';
  Organization     : String = '';
  Title            : String = '';
  EmailAddress     : String = '';

  UserDir  : String = '';
  KeyDir   : String = '';
  RndDir   : String = '';
  CAsDir   : String = '';
  LibKey   : String = '';

  KeysAlgorithm: String = '';

  SecretKeyFile    : String = '';
  PublicKeyFile    : String = '';
  RequestFile      : String = '';
  CertificateFile  : String = '';
  IniFile          : String = '';

procedure LibExit(Reason: Integer);
begin
  if Reason = DLL_PROCESS_DETACH then
  begin
    DeleteCriticalSection(hMono);
  end;
  if Assigned(SaveDllProc) then SaveDllProc(Reason);  // call saved entry point procedure
end;

function  isNull(v1,v2: Variant): Variant;
begin
  if v1 = Null then Result := v2
   else Result := v1
end;

function GetCurrentModule: String;
var
  Buf : AnsiString;
  Len : Integer;
begin
  Len:=2000;
  SetLength(Buf,Len);
  Len:=GetModuleFileName(SysInit.HInstance,PChar(Buf),Len);
  SetLength(Buf,Len);
  Result:=Buf;
end;

function ReadIni: Boolean;
var
  iniFileName: String;
  fValues: TStringList;
  DLLPath: String;
  function isConfigLoaded: Boolean;
  begin
    Result :=
    (Length(KeysAlgorithm)>0) and (Length(SecretKeyFile)>0) and (Length(PublicKeyFile)>0) and (Length(RequestFile)>0) and (Length(CertificateFile)>0);
  end;
begin
  Result := isConfigLoaded;
  if not Result then begin
    iniFileName:=ChangeFileExt(GetCurrentModule,'.ini');

    fValues := TStringList.Create;
    if FileExists(iniFileName) then with TIniFile.Create(iniFileName) do try

      ReadSectionValues(iniOptions,fValues);

      DLLPath := ExtractFilePath(GetCurrentModule);

      UserDir := fValues.Values['UserDir'];
      if (Length(UserDir)>1) and (UserDir[1]<>'\') and (UserDir[2]<>':') then
        UserDir := DLLPath + UserDir;

      KeyDir := fValues.Values['KeyDir'];
      if (Length(KeyDir)>1) and (KeyDir[1]<>'\') and (KeyDir[2]<>':') then
        KeyDir := DLLPath + KeyDir;

      CAsDir := fValues.Values['CAsDir'];
      if (Length(CAsDir)>1) and (CAsDir[1]<>'\') and (CAsDir[2]<>':') then
        CAsDir := DLLPath + CAsDir;

      RndDir := fValues.Values['RndDir'];
      if (Length(RndDir)>1) and (RndDir[1]<>'\') and (RndDir[2]<>':') then
        RndDir := DLLPath + RndDir;

      LibKey := fValues.Values['LibKey'];
      KeysAlgorithm := fValues.Values[Field_KeysAlgorithm];
      SecretKeyFile := fValues.Values['SecretKeyFile'];
      PublicKeyFile := fValues.Values['PublicKeyFile'];
      RequestFile := fValues.Values['RequestFile'];
      CertificateFile := fValues.Values['CertificateFile'];

      IniFile := fValues.Values['IniFile'];
      if (Length(IniFile)>1) and (IniFile[1]<>'\') and (IniFile[2]<>':') then
        IniFile := DLLPath + IniFile;

      ReadSectionValues(iniCertificate,fValues);
      Country      := fValues.Values[Field_Country];
      Locality     := fValues.Values[Field_Locality];
      Organization := fValues.Values[Field_Organization];
      Title        := fValues.Values[Field_Title];
      EmailAddress := fValues.Values[Field_EmailAddress];

      Result := isConfigLoaded;
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

function LoadFile(const FileName: String; var Data: String): Boolean;
var
  FileHandle: Integer;
  FileLength: Integer;
begin
  Result := False;
  if FileExists(FileName) then begin
    FileHandle := FileOpen(FileName, fmOpenRead or fmShareDenyNone);
    if FileHandle<>-1 then try
      FileLength := FileSeek(FileHandle,0,2);
      if FileLength<=MAX_FILE_SIZE then begin
        FileSeek(FileHandle,0,0);
        SetLength(Data,FileLength);
        Result := FileRead(FileHandle,Data[1],FileLength) > 0;
      end;
    finally
      FileClose(FileHandle);
    end;
  end
end;

function SaveFile(const FileName: String; const Data: PByte; const DataLen: Integer): Boolean; overload;
var
  FileHandle: Integer;
begin
  Result := False;
  if FileExists(FileName) then
    FileHandle := FileOpen(FileName, fmOpenWrite or fmShareDenyNone)
  else
    FileHandle := FileCreate(FileName);
  if FileHandle=-1 then
    Raise Exception.Create('ERR_OPEN_FILE: Невозможно открыть файл для записи.')
  else try
    Result := FileWrite(FileHandle,Data^,DataLen)>0;
    SetEndOfFile(FileHandle);
  finally
    FileClose(FileHandle);
  end;
end;

function SaveFile(const FileName: String; const Data: String): Boolean; overload;
begin
  Result := SaveFile(FileName, PByte(@Data[1]), Length(Data))
end;

function xp_SignalCom_CreateIni(pSrvProc : SRV_PROC) : SRVRETCODE; cdecl; export;
var
  XProc: TXProc;
  i: Integer;
  Data: String;
  procedure ReplaceValue(Field_Name, Field_Value: String);
  var
    i,j: Integer;
  begin
    if Length(Field_Name)>0 then begin
      i := Pos(Field_Name+'=',Data);
      if (i>0) and ((i=1) or (Data[i-1] in [#10,#13])) then begin
        i := i + Length(Field_Name); j := i+1;
        while (j<=Length(Data)) and not (Data[j] in [#10,#13]) do Inc(j);
        if j<=Length(Data) then Data := Copy(Data,1,i) + Field_Value + Copy(Data,j,Length(Data))
        else Data := Copy(Data,1,i) + Field_Value;
      end;
    end;
  end;
begin
  result := 1;
  XProc := TXProc.Create(pSrvProc);
  with XProc do try try
    if (ParamCount<1) then Raise Exception.Create(ERR_INVALID_PARAMS);
    if ReadIni then begin
      if LoadFile(IniFile,Data) then begin
        Data := Data;
        ReplaceValue(Field_KeysAlgorithm,KeysAlgorithm);
        ReplaceValue('KeyFile',SecretKeyFile);
        ReplaceValue(Field_Country,Country);
        ReplaceValue(Field_Locality,Locality);
        ReplaceValue(Field_Organization,Organization);
        ReplaceValue(Field_Title,Title);
        ReplaceValue(Field_EmailAddress,EmailAddress);

        for i := ParamCount downto 1 do
          ReplaceValue(GetParamName(i),VarToStr(GetParam(i)));

        if GetParamName(1) = Field_Result then
          SetParam(1,Data)
        else begin
          CreateField(1,'INI',ftString,MAX_FILE_SIZE);
          SetField(1,Data);
          Next;
        end;
      end else
        RaisError(ERR_LOAD_INI + ' [IniFile: ' + IniFile + ']')
    end else
      RaisError(ERR_LOAD_INI)
  except on E:Exception do begin
      RaisError(E.Message)
  end end finally
    if isSuccess then result := 0;
    Free;
  end;
end;

function xp_SignalCom_CreateKeys(pSrvProc : SRV_PROC) : SRVRETCODE; cdecl; export;
var
  XProc: TXProc;
  KeyDir: String;
  Txt: String;
  Code: Integer;
  sr: TSearchRec;
begin
  result := 1;
  XProc := TXProc.Create(pSrvProc);

  with XProc do try try
    if (ParamCount<1) then Raise Exception.Create(ERR_INVALID_PARAMS);
    if ReadIni then begin
      KeyDir := UserDir + '\#' + FormatDateTime('yyyymmddhhnnsszzz',now());

      EnterCriticalSection(hMono);

      try
        ClearBuffer(PChar(LibKey));
        Code := PKCS7Init(PChar(RndDir), 0);
        if Code=0 then try

          Code := PSE31_Generation(PChar(KeyDir), 0, nil, 0); if Code<>0 then Exception.Create(ERR_CRYPT_LIB + ' (PSE31_Generation)');

          Code := SetCountry(PChar(Country));
          if Code<>0 then Exception.Create(ERR_CRYPT_LIB + ' (SetCountry)');
          Code := SetStateOrProvince(PChar(StateOrProvince));
          if Code<>0 then Exception.Create(ERR_CRYPT_LIB + ' (SetStateOrProvince)');
          Code := SetLocality(PChar(Locality));
          if Code<>0 then Exception.Create(ERR_CRYPT_LIB + ' (SetLocality)');
          Code := SetOrganization(PChar(Organization));
          if Code<>0 then Exception.Create(ERR_CRYPT_LIB + ' (SetOrganization)');

          Txt  := VarToStr(GetParamByName(Field_OrganizationUnit));
          Code := SetOrganizationalUnit(PChar(Txt));
          if Code<>0 then Exception.Create(ERR_CRYPT_LIB + ' (SetOrganizationalUnit)');

          Txt  := VarToStr(GetParamByName(Field_CommonName));
          Code := SetCommonName(PChar(Txt));
          if Code<>0 then Exception.Create(ERR_CRYPT_LIB + ' (SetCommonName)');

          Txt := isNull(GetParamByName(Field_Title),Title);
          Code := SetTitle(PChar(Txt));
          if Code<>0 then Exception.Create(ERR_CRYPT_LIB + ' (SetTitle)');

          Txt := isNull(GetParamByName(Field_EmailAddress),EmailAddress);
          Code := SetEmailAddress(PChar(Txt));
          if Code<>0 then RaisError(ERR_CRYPT_LIB + ' (SetEmailAddress)');

          Code := SetNewKeysAlgorithm(PChar(KeysAlgorithm));
          if Code<>0 then Exception.Create(ERR_CRYPT_LIB + ' (SetNewKeysAlgorithm)');

          Code := NewKeysGenerationEx(PChar(KeyDir), nil, PChar(KeyDir + '\' + SecretKeyFile),
		 						nil, PChar(KeyDir + '\' + RequestFile));
          if Code<>0 then Exception.Create(ERR_CRYPT_LIB + ' (NewKeysGenerationEx)');

        finally
          PKCS7Final;
        end else begin
          RaisError(ERR_CRYPT_LIB + ' (PKCS7Init)');
          Exit;
        end
      finally
        LeaveCriticalSection(hMono);
      end;

      CreateField(1,'FileName',ftString,255);
      CreateField(2,'FileSource',ftVarBytes,MAX_FILE_SIZE);

      if FindFirst(KeyDir + '\*.*', faReadOnly or faHidden or faSysFile or faArchive, sr) = 0 then begin
      repeat
        if stricomp(PChar(sr.Name),PChar(PublicKeyFile))<>0 then begin
          if LoadFile(KeyDir + '\' + sr.Name,Txt) then begin
             SetField(1,sr.Name);
             SetField(2,Txt);
             Next;
          end;
        end;
      until FindNext(sr) <> 0;
      FindClose(sr);
      end;
      FileUtil.DeleteFiles(KeyDir + '\*.*');
      RemoveDir(KeyDir);

    end else
      RaisError(ERR_LOAD_INI)
  except on E:Exception do begin
      RaisError(E.Message)
  end end finally
    if isSuccess then result := 0;
    Free;
  end;

end;


function xp_SignalCom_ReadRequest(pSrvProc : SRV_PROC) : SRVRETCODE; cdecl; export;
var
  XProc: TXProc;
  i: Integer;
  Info: CERTIFICATE_REQ_INFO;
begin
  result := 1;
  XProc := TXProc.Create(pSrvProc);
  with XProc do try try
    if (ParamCount<2) or (GetParamType(1)<>ftString) then Raise Exception.Create(ERR_INVALID_PARAMS);
    if (GetParamLen(1)>0) and ReadIni then try
      EnterCriticalSection(hMono);
      ClearBuffer(PChar(LibKey));
      i := GetRequestInfoBuffer(GetParamData(1), GetParamLen(1), @Info);
      if i > 0 then
        Raise Exception.Create('DLL_ERR: Code=' + IntToStr(i))
      else begin
        SetParamByName(Field_Country,String(Info.Subject.Country));
        SetParamByName(Field_StateOrProvince,String(Info.Subject.StateOrProvince));
        SetParamByName(Field_Locality,String(Info.Subject.Locality));
        SetParamByName(Field_Organization,String(Info.Subject.Organization));
        SetParamByName(Field_OrganizationUnit,String(Info.Subject.OrganizationalUnit));
        SetParamByName(Field_Title,String(Info.Subject.Title));
        SetParamByName(Field_CommonName,String(Info.Subject.CommonName));
        SetParamByName(Field_EmailAddress,String(Info.Subject.EmailAddress));
        FreeRequestInfo(@Info);
      end;
    finally
      LeaveCriticalSection(hMono);
    end else
      RaisError(ERR_LOAD_INI)
  except on E:Exception do begin
    RaisError(E.Message)
  end end finally
    if isSuccess then result := 0;
    Free;
  end;
end;

function xp_SignalCom_ReadCertificate(pSrvProc : SRV_PROC) : SRVRETCODE; cdecl; export;
var
  XProc: TXProc;
  Info: CERTIFICATE_INFO;
begin
  result := 1;
  XProc := TXProc.Create(pSrvProc,False);
  with XProc do try try
    if (ParamCount<1) or (not (GetParamType(1) in [ftString,ftVarBytes])) or (GetParamLen(1)=0) then
      Raise Exception.Create(ERR_INVALID_PARAMS);
    if ReadIni then
    try
      EnterCriticalSection(hMono);
      ClearBuffer(PChar(LibKey));
      result := GetCertificateInfoBufferEx(GetParamData(1), GetParamLen(1), @Info);
      if result > 0 then Raise Exception.Create(ERR_CRYPT_LIB + ' (GetCertificateInfoBufferEx: ' + IntToStr(result) + ')');
      try
        SetParamByName(Field_KeysAlgorithm,String(GetCertPublicKeyAlgorithmBuffer(GetParamData(1), GetParamLen(1))));
        SetParamByName(Field_Serial,String(Info.SerialNumber));
        SetParamByName(Field_DateBegin,String(Info.NotBefore));
        SetParamByName(Field_DateEnd,String(Info.NotAfter));

        SetParamByName(Field_Country,String(Info.Subject.Country));
        SetParamByName(Field_StateOrProvince,String(Info.Subject.StateOrProvince));
        SetParamByName(Field_Locality,String(Info.Subject.Locality));
        SetParamByName(Field_Organization,String(Info.Subject.Organization));
        SetParamByName(Field_OrganizationUnit,String(Info.Subject.OrganizationalUnit));
        SetParamByName(Field_Title,String(Info.Subject.Title));
        SetParamByName(Field_CommonName,String(Info.Subject.CommonName));
        SetParamByName(Field_EmailAddress,String(Info.Subject.EmailAddress));
      finally
        FreeCertificateInfo(@Info);
      end;

    finally
      LeaveCriticalSection(hMono);
    end else
      RaisError(ERR_LOAD_INI)
  except on E:Exception do
    begin
      if Result = 0 then Result := 1;
      RaisError(E.Message) ;
    end
  end finally
    if isSuccess then result := 0;
    Free;
  end;
end;


function xp_SignalCom_Sign(pSrvProc : SRV_PROC) : SRVRETCODE; cdecl; export;
var    // @Data, @Sign out, @Key, @Cert, @kek.opq, @masks.db3, @mk.db3, @rand.opq, @OutWithCert, @OutWithTime
  XProc: TXProc;
  Dir : String;
  sgn_ctx: Pointer;
  buf: Pointer;
  ln : Integer;
  Data: String;
begin
  result := 1;
  XProc := TXProc.Create(pSrvProc,False);
  with XProc do
  try
    try
      if (ParamCount<8) or (not (GetParamType(1) in [ftString,ftVarBytes]))
      or (GetParamType(2) <> ftString) or (GetParamType(3) <> ftString)
      or (GetParamType(4) <> ftString) then Raise Exception.Create(ERR_INVALID_PARAMS);

      if ReadIni then begin
        EnterCriticalSection(hMono);
        try
          Dir := KeyDir + '\#' + FormatDateTime('yyyymmddhhnnsszzz',now());
          ForceDirectories(Dir);
          if not SaveFile(Dir + '\' + FileName_kek_opq, PByte(GetParamData(5)), GetParamLen(5))
          or not SaveFile(Dir + '\' + FileName_masks_db3, PByte(GetParamData(6)), GetParamLen(6))
          or not SaveFile(Dir + '\' + FileName_mk_db3, PByte(GetParamData(7)), GetParamLen(7))
          or not SaveFile(Dir + '\' + FileName_rand_opq, PByte(GetParamData(8)), GetParamLen(8)) then
            raise Exception.Create(ERR_CRYPT_LIB + ' Ошибка при копировании прочих файлов.');

          ClearBuffer(PChar(LibKey));
          result := PKCS7Init(PChar(RndDir), 0);
          if result<>0 then
            raise Exception.Create(ERR_CRYPT_LIB + ' (PKCS7Init (' + IntToStr(result) + ')');
          sgn_ctx := nil; buf := nil;
          try
            result := AddPSEPrivateKeyFromBufferEx(PChar(Dir), nil, GetParamData(3), GetParamLen(3), nil);
            if result<>0 then Raise Exception.Create(ERR_CRYPT_LIB + ' (AddPSEPrivateKeyFromBufferEx: ' + IntToStr(result) + ')');
            result := AddCAs(PChar(CAsDir)); if result<>0 then Exception.Create(ERR_CRYPT_LIB + ' (AddCAs)');
            sgn_ctx := GetSignCTX(); if sgn_ctx=nil then Exception.Create(ERR_CRYPT_LIB + ' (GetSignCTX)');
            ln := GetParamLen(4);
            result := AddSigner(sgn_ctx, BY_BUFFER, GetParamData(4), @ln);
            if result<>0 then Raise Exception.Create(ERR_CRYPT_LIB + ' (AddSigner: ' + IntToStr(result) + ')');
            result := InsertSigningTimeToSign(1);
            if result<>0 then Raise Exception.Create(ERR_CRYPT_LIB + ' (InsertSigningTimeToSign: ' + IntToStr(result) + ')');
            if (ParamCount>=9) and (Integer(GetParam(9))<>0) then begin
              result := InsertCertificateToSign(1);
              if result<>0 then Raise Exception.Create(ERR_CRYPT_LIB + ' (InsertCertificateToSign: ' + IntToStr(result) + ')');
            end;
            result := SignBufferEx(sgn_ctx, GetParamData(1), GetParamLen(1), @buf, @ln, 1);
            if result<>0 then Raise Exception.Create(ERR_CRYPT_LIB + ' (SignBufferEx: ' + IntToStr(result) + ')');
            Setlength(Data,ln); MoveMemory(Pointer(Data), buf, ln);
            SetParam(2,Data);
          finally
            if buf <> nil then try FreeBuffer(buf) except end;
            if sgn_ctx <> nil then try FreeSignCTX(sgn_ctx) except end;
            try PKCS7Final except end;
          end;
        finally
          try
            FileUtil.DeleteFiles(Dir + '\*.*');
            RemoveDir(Dir);
          except end;
          LeaveCriticalSection(hMono);
        end;
      end else
        RaisError(ERR_LOAD_INI)
    except
      on E:Exception do
      begin
        if Result = 0 then Result := 1;
        RaisError(E.Message);
      end
    end
  finally
//    if isSuccess and (Result = 0) then result := 0;
    Free;
  end;
end;

function xp_SignalCom_SignCheck(pSrvProc : SRV_PROC) : SRVRETCODE; cdecl; export;
var    // @Data, @Sign[, @Cert][, @Subject out][, @Insuer out][, @SignDate out]
  XProc: TXProc;
  sgn_ctx: Pointer;
  ln : Integer;
  buf: PChar;
  dt: string;

  Count: Integer;
begin
  result := 1;
  XProc := TXProc.Create(pSrvProc,False);
  with XProc do try try
    if (ParamCount<2) or (not (GetParamType(1) in [ftString,ftVarBytes])) or (GetParamType(2) <> ftString) then
      Raise Exception.Create(ERR_INVALID_PARAMS);
    if ReadIni then begin
      EnterCriticalSection(hMono);
      try
        ClearBuffer(PChar(LibKey));
        result := PKCS7Init(PChar(RndDir), 0);
        if result<>0 then raise Exception.Create(ERR_CRYPT_LIB + ' (PKCS7Init)');
        sgn_ctx := nil;
        try
          result := AddCAs(PChar(CAsDir));
          if result<>0 then raise Exception.Create(ERR_CRYPT_LIB + ' (AddCAs)');

          sgn_ctx := GetSignCTX();
          if sgn_ctx=nil
            then raise Exception.Create(ERR_CRYPT_LIB + ' (GetSignCTX)');

          if (GetParamType(3)=ftString) and not GetParamIsNull(3) then begin
            ln := GetParamLen(3); result := AddSigner(sgn_ctx, BY_BUFFER, GetParamData(3), @ln);
            if result<>0 then raise Exception.Create(ERR_CRYPT_LIB + ' (AddSigner: ' + IntToStr(result) + ')');
          end;

          result := CheckBufferSignEx(sgn_ctx, GetParamData(2), GetParamLen(2), nil, nil, 0, GetParamData(1), GetParamLen(1));
          if result<>0 then raise Exception.Create(ERR_CRYPT_LIB + ' (CheckBufferSignEx: ' + IntToStr(result) + ')');

          Count := GetSignatureCount(sgn_ctx);
          if Count=1 then begin
              isSuccess := GetSignatureStatus(sgn_ctx, 0)=0;
              if ParamCount>3 then begin
                buf := nil;
                Result := GetSignatureCertInBuffer(sgn_ctx,0,@buf,@ln) ;
                if Result <> 0 then
                  raise Exception.Create(ERR_CRYPT_LIB + ' (GetSignatureCertInBuffer: ' + IntToStr(result) + ')');
                try
                  SetParamByName(Field_Certificate,Copy(buf,1,ln));
                finally
                  if buf <> nil then try FreeBuffer(buf) except end;
                end;
              end;
              // ======= Vaynshteyn =========
              if ParamCount>4 then
              begin
                dt :=  GetSignatureTime(sgn_ctx, 0);
                SetParamByName(Field_SignDate, dt);
              end;
              // ============================
          end else
            isSuccess := False;
        finally
          if sgn_ctx <> nil then try FreeSignCTX(sgn_ctx) except end;
          try PKCS7Final except end;
        end;
      finally
        LeaveCriticalSection(hMono);
      end
    end else
      RaisError(ERR_LOAD_INI)
  except
    on E:Exception do
    begin
      if Result = 0 then Result := 1;
      RaisError(E.Message);
    end
  end finally
    if isSuccess and (Result = 0) then result := 0;
    Free;
  end;
end;


exports
    xp_SignalCom_CreateIni,
    xp_SignalCom_CreateKeys,
    xp_SignalCom_ReadRequest,
    xp_SignalCom_ReadCertificate,
    xp_SignalCom_Sign,
    xp_SignalCom_SignCheck,
    __GetXpVersion;

begin
// library initialization code
  SaveDllProc := DllProc;  // save exit procedure chain
  DllProc     := @LibExit;  // install LibExit exit procedure
  
  InitializeCriticalSection(hMono);
end.

