unit Sirius;

interface
  uses WinSock,SysUtils,Classes;

const
  iBufferDefault  = 2000;
  iSendDefault    = 100;
  esInvalidSocket = 'invalid socket;';
  esDisconnectSocket = 'Disconnect socket. Code=%S';
  esWindowsSocketError = 'Windows socket error: %s (%d), on API ''%s''';
  prTCP  = 'TCP_';

const
 // Shutdown Options
  Id_SD_Recv = 0;
  Id_SD_Send = 1;
  Id_SD_Both = 2;
 // SO_REUSEADDR = $4;

const // имена параметров
  pnHOST   = 'HOST';
  pnPORT   = 'PORT';
  pnSEND   = 'SEND';
  pnRECV   = 'RECV';
  pnLSTTO  = 'LITTO';
  pnBLOCING= 'BLOC';
  pnSocket = 'SOCKET';
  cFS = #$1c;

const
  fnVersion       : String = 'VERSION';
  fnFLAGS         : String = 'FLAG';
  fnSirius        : String = 'SIRIUS';
  fnSign          : String = 'SIGNATURE';
  fnMultiCode     : String = 'MULTICODE';
  fnDublicate     : String = 'DUBLICATE';
  fnNameSeparator : String = ':';
  cSingleDef      : String = 'MBDP';

  PROTOCOL_VERSION : String = '200';

  fnSvrTransaction    : String = '000';
  fnDataDate          : String = '001';
  fnSenderName        : String = '002';
  fnRecipientName     : String = '003';
  fnTransactionSum    : String = '004';
  fnTransactionValuta : String = '005';
  fnCommercCardNumber : String = '006';
  fnClientCardNumber  : String = '007';
  fnTransactionCode   : String = '009';
  fnReturnCode        : String = '010';

  GET_VERSION         = 104;
  SET_CREDIT_LIMIT    = 153;
  GET_CREDIT_LIMIT    = 154;

  //ERRORS
  ceInvalidHeader = 'Invalid header mtssage.';
  ceInvalidMessage= 'Invalid message.';
  ceDublicateField= 'Dublicate field ''%S''.';
  ceInvalidFieldName= 'Invalid field name <%S>.';
  ceInvalidFieldData= 'Invalid data field <%S>.';


const
  INVALID_SOCKET = WinSock.INVALID_SOCKET;
  SOCKET_MODE_BLOCKING =0;
  SOCKET_MODE_NONBLOCKING =1;

  ctFieldData =1;
  ctFieldName =2;

const
  //Syntax
  csLatin = ['A'..'Z','a'..'z'];
  csDigit = ['0'..'9'];
  csCiril = ['А'..'П','Р'..'Я','а'..'п','р'..'я','Ё','ё'];
  csSeparator =[' ','_','.',',',';',':','!','?','+','-','*','/','\','|','^','~','(',')','{','}',
                '[',']','<','>','''','"','=','#','%','$','@','&'];


type
  EDSockError  = Exception;
  EDProtocol = class(Exception);

TDWinSock = class(TStream)
private
  fSocket    : TSocket;
  fAddr      : sockaddr_in;
  fSAddr     : sockaddr_in;
  fConnected : Boolean;
  fBlocking  : Boolean;
  fTimOutSND : Integer;
  fTimOutRCV : Integer;
  fTimOutUSE : Integer;
  fHost      : String;
  fPort      : Integer;
  procedure SetOptions;
  procedure CheckError;
  procedure CheckInvalid;
//  procedure SetAddr(aAddr : sockaddr_in);
  procedure Error(MSG : String);
  function  ResolveHost(const AHost: string): string;
  function  FGetHostByName(const AHostName: string): string;
  function  TInAddrToString(var AInAddr): string;
  function  IsIP(AIP: string): boolean;
public
  constructor Create(aSocket : TSocket);
  destructor  Destroy;override;
  procedure   Close; virtual;
  procedure   Disconnect(Code : Integer); virtual;
  function    Connected : Boolean;
  procedure   SetParams(Params : TStrings);
  procedure   GetParams(Params : TStrings);
  procedure   SetTimout(Snd : Integer; Rcv : Integer);
  property    Blocking : Boolean read fBlocking write fBlocking;
end;

TDSockClient = class (TDWinSock)
private
  fInBuffer  : TMemoryStream;
  fOutBuffer : TMemoryStream;
  procedure CheckInBufferSize(Size : Integer);
//  procedure CheckOutBufferSize(Size : Integer);
public
  constructor Create(aSocket : TSocket);
  destructor  Destroy; override;
  procedure   Open(host:String; port:Integer);
  function    Read (var Buf; Len : Integer): Longint; override;
  function    Write(const Buf;Len : Longint): Longint; override;
  function    Seek(Offset: Longint; Origin: Word): Longint; override;
end;


TDXTCPProtocol = class(TObject)
protected
  fStream    : TStream;
  fSocket    : TSocket;
  fBlocking  : Boolean;
  fTimOutSND : Integer;
  fTimOutRCV : Integer;
  fHost      : String;
  fPort      : Integer;
  procedure Error(Msg : String);
public
  constructor Create;
  destructor  Destroy;override;
  procedure Open;virtual;
  procedure Close;virtual;
  function  Read(var Buf; Count : Longint) : Longint;virtual;
  function  Write(const Buf; Count : Longint) : Longint;virtual;
  procedure SetOptions (const context : String; const Params : String);virtual;
  procedure GetOptions (const context : String; var Params : String);virtual;
end;

TDXSirius = class (TObject)
  protected
    fProtocol : TDXTCPProtocol;
    function  Valid(Token : String; fType : Integer): Boolean;
    function  CardinalToStr(x: Cardinal): String;
    procedure Error(Msg : String);
  public
    constructor Create;
    destructor  Destroy;override;
    procedure  Open;
    procedure  Close;
    function  ReadAlgorithm(xFields : TStringList): Boolean; virtual;
    function  WriteAlgorithm(xFields : TStringList): Boolean; virtual;
    procedure SetOptions (const context : String; const Params : String);virtual;
    procedure GetOptions (const context : String; var Params : String);virtual;
    function  Peer: String;
  end;


  function  DateTimeToSirius(Date : TDateTime):String;
  function  SiriusToDateTime(Date : String) : TDateTime;
  function  CardTreckToSirius(PAN:String;Alias_Client : String;EXPDate : String;DiscretionaryData : String;CVC : String):String;
  function  CardTreckToPan(Treck : String):String;
  function  CardTreckToExp(Treck : String):String;
  function  CardToCVC(Treck : String):String;
  function  CardTreckToPin(Treck : String):String;
  function  AmountTOSirius(Amount : String):String;
  function  SiriusToAmount(Amount : String):String;
  function  SiriusToAmountExt(Amount : String; Format : String; DecimalSeparator : Char) : String;
  function  SiriusToCurr(Currency : String):String;
  function  Replace(InStr : String; InSub:String; OutSub:String):String;

implementation
uses Windows,DateUtils;

var
  WSAData: TWSAData;

function iif(ifs : boolean; Thens : Variant; Elses : Variant ): Variant;
begin
  if ifs then Result:=Thens
  else Result:=Elses;
end;

procedure Startup;
var
  ErrorCode: Integer;
begin
  ErrorCode := WSAStartup($0101, WSAData);
  if ErrorCode <> 0 then
    raise EDSockError.CreateFmt(esWindowsSocketError,[SysErrorMessage(ErrorCode), ErrorCode, 'WSAStartup']);
end;

procedure Cleanup;
var
  ErrorCode: Integer;
begin
  ErrorCode := WSACleanup;
  if ErrorCode <> 0 then
    raise EDSockError.CreateFmt(esWindowsSocketError,[SysErrorMessage(ErrorCode), ErrorCode, 'WSACleanup']);
end;

  function  Replace(InStr : String; InSub:String; OutSub:String):String;
begin
  Result:=InStr;
  if InSub<>'' then
    while Pos(InSub,Result)>0 do
      Result:=Copy(Result,1,Pos(InSub,Result)-1)+OutSub+Copy(Result,Pos(InSub,Result)+Length(InSub),Length(Result));
end;

function  AmountToSirius(Amount : String):String;
var
  s: String;
begin
  try
    Result:=IntToStr(Trunc(StrToFloat(Amount)*100));
  except
    begin
      s:=Amount;
      if Pos(',',s)>0 then s:=Copy(s,1,Pos(',',s)-1)+'.'+Copy(s,Pos(',',s)+1,Length(s))
      else if Pos('.',s)>0 then s:=Copy(s,1,Pos('.',s)-1)+','+Copy(s,Pos('.',s)+1,Length(s));
      Result:=IntToStr(Trunc(StrToFloat(S)*100));
    end;
  end;
end;

function SiriusToAmountExt(Amount : String; Format : String; DecimalSeparator : Char) : String;
var
  I : Integer;
  NewFormat : TFormatSettings;
begin
  GetLocaleFormatSettings(0, NewFormat);
  NewFormat.DecimalSeparator :=DecimalSeparator;
  if Trim(Amount)='' then Amount:='0';
  try
    I:=StrToInt(Amount);
  except
    I:=0;
  end;
  if Format='' then Format:='0.00';
  Result:= FormatFloat(Format,I/100,NewFormat);
end;

function SiriusToAmount(Amount : String):String;
begin
  Result:= SiriusToAmountExt(Amount,'0.00','.');
end;

function SiriusToCurr(Currency : String):String;
var
  Curr : String;
begin
  Curr:=Trim(Currency);
  if Curr = '810' then Result:='RUR'
  else if Curr = '840' then Result:='USD'
  else if Curr = '978' then Result:='EUR'
  else Result:=Currency;
end;

function  SiriusToDateTime(Date : String) : TDateTime;
var
  yyyy : word;
  mm   : word;
  dd   : word;
  hh   : word;
  nn   : word;
  ss   : word;
  sA   : String;
begin
  sA:=Copy(Date,1,4);
  if sA <> '' then yyyy:=StrToInt(sA);
  sA:=Copy(Date,5,2);
  if sA <> '' then mm:=StrToInt(sA);
  sA:=Copy(Date,7,2);
  if sA <> '' then dd:=StrToInt(sA);
  sA:=Copy(Date,9,2);
  if sA <> '' then hh:=StrToInt(sA);
  sA:=Copy(Date,11,2);
  if sA <> '' then nn:=StrToInt(sA);
  sA:=Copy(Date,13,2);
  if sA <> '' then ss:=StrToInt(sA);
  Result:=EncodeDateTime(yyyy, mm, dd, hh, nn, ss, 0);
end;

function  DateTimeToSirius(Date : TDateTime):String;
begin
  Result:=FormatDateTime('yyyymmddhhnnss',Date);
end;

function  CardTreckToSirius(PAN:String; Alias_Client : String;EXPDate : String;DiscretionaryData : String; CVC : String):String;
begin
  Result:=Trim(PAN)+'='+TRIM(EXPDate);
  if Trim(DiscretionaryData)<>'' then Result:=';'+Result+Trim(DiscretionaryData) //TREK
  else if Alias_Client<>'' then  Result:='a'+Result+Trim(Alias_Client)//alias
  else Result:='m'+Result;//Ручной ввод
  Result:=Result+'?';
  if Trim(CVC)<>'' then Result:=Result+'c'+Trim(CVC);
end;

function  CardTreckToPan(Treck : String):String;
begin
  Result:=Copy(Trim(Treck),2,Pos('=',Trim(Treck))-2);
end;

function  CardTreckToExp(Treck : String):String;
begin
  Result:=Copy(Trim(Treck),Pos('=',Trim(Treck))+1,4);
end;

function  CardToCVC(Treck : String):String;
begin
  Result:=Copy(Trim(Treck),Pos('c',Trim(Treck))+1,Length(Treck));
  if Pos('m',Result)>0 then Delete(Result,Pos('m',Result),Length(Result));
  if Pos(';',Result)>0 then Delete(Result,Pos(';',Result),Length(Result));
  if Pos('p',Result)>0 then Delete(Result,Pos('p',Result),Length(Result));
  if Pos('e',Result)>0 then Delete(Result,Pos('e',Result),Length(Result));
end;

function CardTreckToPin(Treck : String):String;
begin
  Result:=Copy(Trim(Treck),Pos('p',Trim(Treck))+1,Length(Treck));
  if Pos('m',Result)>0 then Delete(Result,Pos('m',Result),Length(Result));
  if Pos(';',Result)>0 then Delete(Result,Pos(';',Result),Length(Result));
  if Pos('c',Result)>0 then Delete(Result,Pos('c',Result),Length(Result));
  if Pos('e',Result)>0 then Delete(Result,Pos('e',Result),Length(Result));
end;









{ TDWinSock }

procedure TDWinSock.CheckError;
var
  eCode : Integer;
  Text  : String;
begin
  eCode := WSAGetLastError;
  case eCode of
    WSAEINTR:  Text := 'Interrupted system call.';
    WSAEBADF:  Text := 'Bad file number.';
    WSAEACCES: Text := 'Access denied.';
    WSAEFAULT: Text := 'Bad address.';
    WSAEINVAL: Text := 'Invalid argument.';
    WSAEMFILE: Text := 'Too many open files.';

    WSAEWOULDBLOCK:     Text := 'Operation would block. ';
    WSAEINPROGRESS:     Text := 'Operation now in progress.';
    WSAEALREADY:        Text := 'Operation already in progress.';
    WSAENOTSOCK:        Text := 'Socket operation on non-socket.';
    WSAEDESTADDRREQ:    Text := 'Destination address required.';
    WSAEMSGSIZE:        Text := 'Message too long.';
    WSAEPROTOTYPE:      Text := 'Protocol wrong type for socket.';
    WSAENOPROTOOPT:     Text := 'Bad protocol option.';
    WSAEPROTONOSUPPORT: Text := 'Protocol not supported.';
    WSAESOCKTNOSUPPORT: Text := 'Socket type not supported.';
    WSAEOPNOTSUPP:      Text := 'Operation not supported on socket.';
    WSAEPFNOSUPPORT:    Text := 'Protocol family not supported.';
    WSAEAFNOSUPPORT:    Text := 'Address family not supported by protocol family.';
    WSAEADDRINUSE:      Text := 'Address already in use.';
    WSAEADDRNOTAVAIL:   Text := 'Cannot assign requested address.';
    WSAENETDOWN:        Text := 'Network is down.';
    WSAENETUNREACH:     Text := 'Network is unreachable.';
    WSAENETRESET:       Text := 'Net dropped connection or reset.';
    WSAECONNABORTED:    Text := 'Software caused connection abort.';
    WSAECONNRESET:      Text := 'Connection reset by peer.';
    WSAENOBUFS:         Text := 'No buffer space available.';
    WSAEISCONN:         Text := 'Socket is already connected.';
    WSAENOTCONN:        Text := 'Socket is not connected.';
    WSAESHUTDOWN:       Text := 'Cannot send or receive after socket is closed.';
    WSAETOOMANYREFS:    Text := 'Too many references, cannot splice.';
    WSAETIMEDOUT:       Text := 'Connection timed out.';
    WSAECONNREFUSED:    Text := 'Connection refused.';
    WSAELOOP:           Text := 'Too many levels of symbolic links.';
    WSAENAMETOOLONG:    Text := 'File name too long.';
    WSAEHOSTDOWN:       Text := 'Host is down.';
    WSAEHOSTUNREACH:    Text := 'No route to host.';
    WSAENOTEMPTY:       Text := 'Directory not empty';
  else Text:='';
  end;
  ERROR('Socket error #'+IntToStr(eCode)+' '+Text);
end;

procedure TDWinSock.CheckInvalid;
begin
  if fSocket=INVALID_SOCKET then
  begin
    Error(esInvalidSocket);
    exit;
  end;
end;

procedure TDWinSock.Close;
begin
  if fConnected then
  begin
    if shutdown(fSocket,Id_SD_Send)=SOCKET_ERROR then
    begin
      CheckError;
    end;
  end;

  if  fSocket<>INVALID_SOCKET then
  begin
    if closesocket(fSocket)=SOCKET_ERROR then
    begin
      CheckError;
    end;
    fSocket:=INVALID_SOCKET;
    fConnected:=False;
  end;
end;

function TDWinSock.Connected: Boolean;
begin
  Result:=fConnected and (fSocket<>INVALID_SOCKET);
end;

constructor TDWinSock.Create(aSocket: TSocket);
begin
  inherited Create;
  fTimOutSND :=60000;
  fTimOutRCV :=60000;
  fTimOutUSE :=0;

  Startup;
  fSocket:=aSocket;
  fBlocking:=True;
  if fSocket<>INVALID_SOCKET then
  begin
    fConnected:=True
  end
  else fConnected:=False;
  SetOptions;
end;

destructor TDWinSock.Destroy;
begin
  Cleanup;
  inherited;
end;

procedure TDWinSock.Disconnect(Code: Integer);
begin
  Close;
  Error(Format(esDisconnectSocket,[IntToStr(Code)]));
end;

procedure TDWinSock.Error(MSG: String);
begin
  raise EDSockError.Create(MSG);
end;

function TDWinSock.FGetHostByName(const AHostName: string): string;
var
  pa: PChar;
  sa: TInAddr;
  Host: PHostEnt;
begin
  Host := GetHostByName(PChar(AHostName));
  if Host = nil then
  begin
    Error(esInvalidSocket);
  end
  else
  begin
    pa := Host^.h_addr_list^;
    sa.S_un_b.s_b1 := pa[0];
    sa.S_un_b.s_b2 := pa[1];
    sa.S_un_b.s_b3 := pa[2];
    sa.S_un_b.s_b4 := pa[3];
    result := TInAddrToString(sa);
  end;
end;

procedure TDWinSock.GetParams(Params: TStrings);
begin
  if not Assigned(Params) then exit;
 Params.Values[pnSEND]:=IntToStr(fTimOutSND);
 Params.Values[pnRECV]:=IntToStr(fTimOutRCV);
 Params.Values[pnPORT]:=IntToStr(fPort);
 Params.Values[pnHOST]:=fHost;
 if fBlocking then Params.Values[pnBLOCING]:='B'else Params.Values[pnBLOCING]:='N';
end;

function TDWinSock.IsIP(AIP: string): boolean;
var
  s1, s2, s3, s4: string;

  function ByteIsOk(const AByte: string): boolean;
  begin
    result := (StrToIntDef(AByte, -1) > -1) and (StrToIntDef(AByte, 256) < 256);
  end;
  function Fetch(var A :String;B : String):String;
  begin
    if Pos(B,A)>0 then
    begin
      Result:=Copy(A,1,Pos(B,A)-1);
      Delete(A,1,Pos(B,A));
    end
    else
    begin
      Result:=A;
      A:='';
    end;
  end;
begin
  s1 := Fetch(AIP, '.');
  s2 := Fetch(AIP, '.');
  s3 := Fetch(AIP, '.');
  s4 := AIP;
  result := ByteIsOk(s1) and ByteIsOk(s2) and ByteIsOk(s3) and ByteIsOk(s4);
end;


function TDWinSock.ResolveHost(const AHost: string): string;
begin
  if Trim(UpperCase(AHost))= 'LOCALHOST' then
  begin
    result := '127.0.0.1';
  end
  else
    if IsIP(AHost) then
  begin
    result := AHost;
  end
  else
  begin
    result :=FGetHostByName(AHost);
  end;
end;
{
procedure TDWinSock.SetAddr(aAddr: sockaddr_in);
begin
  fSAddr:=aAddr;
end;
}
procedure TDWinSock.SetOptions;
var
  socket_mode : Longint;
begin
  if not fBlocking then socket_mode := SOCKET_MODE_NONBLOCKING
  else socket_mode := SOCKET_MODE_BLOCKING;
  WinSock.ioctlsocket(fSocket, FIONBIO, socket_mode);
  WinSock.setsockopt(fSocket, SOL_SOCKET,SO_REUSEADDR,PChar(@fTimOutUSE),SizeOf(fTimOutUSE));
  if fBlocking then
  begin
    WinSock.setsockopt(fSocket, SOL_SOCKET, SO_SNDTIMEO,PChar(@fTimOutSND),SizeOf(fTimOutSND));
    WinSock.setsockopt(fSocket, SOL_SOCKET, SO_RCVTIMEO,PChar(@fTimOutRCV),SizeOf(fTimOutRCV));
  end;
end;

procedure TDWinSock.SetParams(Params: TStrings);
var
  vA : String;
  vSend : Integer;
  vRecv : Integer;
  vHost : String;
  vPort : Integer;
begin
  if not Assigned(Params) then exit;
  vA:=Params.Values[pnSEND];
  if vA = '' then vA:='1000';
  vSend:= StrToInt(vA);
  vA:=Params.Values[pnRECV];
  if vA='' then vA:='1000';
  vRecv:=StrToInt(vA);
  vA:=Params.Values[pnPORT];
  if vA='' then vA:='3000';
  vPort:=StrToInt(vA);
  vA:=Params.Values[pnHOST];
  if vA='' then vA:='LOCALHOST';
  vHost:=vA;
  vA:=Params.Values[pnBLOCING];
  fBlocking:=UPPERCASE(vA)='B';
  fHost:=vHost;
  fPort:=vPort;
  SetTimout(vSend,vRecv);
end;

procedure TDWinSock.SetTimout(Snd, Rcv: Integer);
begin
  fTimOutSND :=Snd;
  fTimOutRCV :=Rcv;
  SetOptions;
end;

function TDWinSock.TInAddrToString(var AInAddr): string;
begin
  with TInAddr(AInAddr).S_un_b do
  begin
    result := IntToStr(Ord(s_b1)) + '.' + IntToStr(Ord(s_b2)) + '.' +
      IntToStr(Ord(s_b3)) + '.'
      + IntToStr(Ord(s_b4));
  end;
end;

{ TDSockClient }

procedure TDSockClient.CheckInBufferSize(Size: Integer);
begin
  if Size>fInBuffer.Size-fInBuffer.Position then
     fInBuffer.SetSize(Size+fInBuffer.Position);
end;
{
procedure TDSockClient.CheckOutBufferSize(Size: Integer);
begin
  if Size>fOutBuffer.Size-fOutBuffer.Position then
    fOutBuffer.SetSize(fOutBuffer.Position+Size);
end;
}
constructor TDSockClient.Create(aSocket: TSocket);
begin
  //BufferDefault
  inherited;
  fInBuffer  := TMemoryStream.Create;
  fInBuffer.SetSize(iBufferDefault);
  fOutBuffer := TMemoryStream.Create;
  fOutBuffer.SetSize (iBufferDefault);
end;

destructor TDSockClient.Destroy;
begin
  Close;
  if Assigned (fInBuffer) then fInBuffer.Free;
  if Assigned(fOutBuffer) then fOutBuffer.Free;
  inherited;
end;


procedure TDSockClient.Open(host: String; port: Integer);
begin
  try
    if host<>'' then fhost:=host else host:=fhost;
    if port>0   then fport:=port else port:=fport;
    if (port=0) or (host='') then exit;
    if fSocket=INVALID_SOCKET then
    begin
      fConnected:=False;
      fSocket:=WinSock.socket(AF_INET,SOCK_STREAM,IPPROTO_IP);
      fAddr.sin_family:=AF_INET;
      fAddr.sin_port:=0;
      fAddr.sin_addr.S_addr:=htonl(INADDR_ANY);
      if bind(fSocket,fAddr,sizeof(fAddr))=SOCKET_ERROR then
      begin
//        fSocket:=INVALID_SOCKET;
        CheckError;
      end;
    end;
    SetOptions;
    if not fConnected then
    begin
      try host:=ResolveHost(host);  except end;
      fSAddr.sin_family:=AF_INET;
      fSAddr.sin_port:= htons(port);
      fSAddr.sin_addr.S_addr:=inet_addr(PChar(host));
      if connect(fSocket,fSAddr,sizeof(fSAddr))=SOCKET_ERROR then
      begin
        CheckError;
      end;
      fConnected:=True;
    end;
  finally
    if not Connected and (fSocket<>INVALID_SOCKET) then Close;
  end;
end;


function TDSockClient.Read(var Buf;Len: Integer): Longint;
var
  iLen : integer;
begin
  CheckInvalid;
  CheckInBufferSize(Len);
  while fInBuffer.Position < Len do
  begin
    iLen:=Len-fInBuffer.Position;
    iLen:=recv(fSocket,PChar(fInBuffer.Memory)[fInBuffer.Position],iLen,0);
    if iLen=SOCKET_ERROR then
    begin
      CheckError;
    end;
    if iLen=0 then
    begin
      Result:=-1;
      Disconnect(iLen);
      exit;
    end
    else fInBuffer.Seek(fInBuffer.Position+iLen,soFromBeginning);
  end;
  Move(PChar(fInBuffer.Memory)[0],Buf,Len);
  fInBuffer.Seek(0,soFromBeginning);
  Result:=0;
end;

function TDSockClient.Seek(Offset: Integer; Origin: Word): Longint;
begin
  Result:=0;
end;

function TDSockClient.Write(const Buf; Len: Longint):Longint;
var
  Pos : Integer;
  iLen : Integer;
begin
  Result:=0;
  if (Len<=0) then exit;
  CheckInvalid;
  Result:=Len;
  fOutBuffer.WriteBuffer(Buf,Len);
  //Send Memory
  Pos:=0;
  while fOutBuffer.Position-Pos>iSendDefault do
  begin
    iLen:=send(fSocket,PChar(fOutBuffer.Memory)[Pos],iSendDefault,0);
    if iLen=SOCKET_ERROR then
    begin
      CheckError;
    end;
    if Len<>SOCKET_ERROR then
    begin
      if iLen=0 then
      begin
        Result:=-1;
        Disconnect(iLen);
        exit;
      end;
      inc(Pos,iSendDefault);
    end;
  end;
  if fOutBuffer.Position-Pos>0 then
  begin
    if send(fSocket,PChar(fOutBuffer.Memory)[Pos],fOutBuffer.Position-Pos,0)=SOCKET_ERROR then
    begin
      CheckError;
    end;
  end;
  fOutBuffer.Seek(0,soFromBeginning);
end;
{ TDTCPProtocol }

procedure TDXTCPProtocol.Close;
begin
  inherited;
  if Assigned(fStream) then
  begin
    TDSockClient(fStream).Close;
    fStream.Free;
    fStream:=nil;
  end;
end;

constructor TDXTCPProtocol.Create;
begin
  inherited;
  fStream:=nil;
  fSocket:=INVALID_SOCKET;
  fBlocking:=False;
  fTimOutSND:=1000;
  fTimOutRCV:=1000;
  fHost:='LOCALHOST';
  fPort:=1024;
end;

destructor TDXTCPProtocol.Destroy;
begin
  inherited;
  if Assigned(fStream) then fStream.Free;
  fStream:=nil;
end;

procedure TDXTCPProtocol.Error(Msg: String);
begin
  raise EDProtocol.Create(ClassName+'::'+Msg);
end;

procedure TDXTCPProtocol.GetOptions(const context: String;
  var Params: String);
var
  S : TStringList;
begin
  S:=TStringList.Create;
  try
    S.Values[context+pnSocket]:=IntToStr(fSocket);
    S.Values[context+pnBLOCING]:=iif(fBlocking,'1','0');
    S.Values[context+pnSEND]:=IntToStr(fTimOutSND);
    S.Values[context+pnRECV]:=IntToStr(fTimOutRCV);
    S.Values[context+pnHOST]:=fHost;
    S.Values[context+pnPORT]:=IntToStr(fPort);
    Params:=S.Text;
  finally
    S.Free;
  end;
end;

procedure TDXTCPProtocol.Open;
var
  Addr: sockaddr_in;
  Len: Integer;
begin
  if Assigned(fStream) then Close;
  fStream:=TDSockClient.Create(fSocket);
  try
    TDSockClient(fStream).SetTimout(fTimOutSND,fTimOutRCV);
    if fSocket=INVALID_SOCKET then begin
      TDSockClient(fStream).Open(fHost,fPort);
//      fSocket := TDSockClient(fStream).fSocket;
    end else begin
      Len := sizeof(sockaddr_in);
      if getpeername(Integer(fSocket),Addr,Len)=0 then begin
        with Addr.sin_addr.S_un_b do fHost := IntToStr(Ord(s_b1)) + '.' + IntToStr(Ord(s_b2)) + '.' + IntToStr(Ord(s_b3)) + '.' + IntToStr(Ord(s_b4));
        fPort := Addr.sin_port
      end
    end;
  except
   begin
     Close;
     raise;
   end;
  end;
end;

function TDXTCPProtocol.Read(var Buf; Count: Integer): Longint;
begin
  Result:=0;
  if (Assigned(fStream))and(TDSockClient(fStream).Connected) then Result:=fStream.Read(Buf,Count);
end;

procedure TDXTCPProtocol.SetOptions(const context, Params: String);
var
  S  : TStringList;
begin
  S  := TStringList.Create;
  try
    S.Text:=Params;
    if S.Values[context+pnSocket]='' then fSocket:=INVALID_SOCKET
    else
    begin
      fSocket:=StrToInt(S.Values[context+pnSocket]);
    end;

    if S.Values[context+pnBLOCING]='' then fBlocking:=False
    else fBlocking:=Trim(S.Values[context+pnBLOCING])='1';

    if S.Values[context+pnSEND]='' then fTimOutSND:=1000
    else fTimOutSND:=StrToInt(S.Values[context+pnSEND]);

    if S.Values[context+pnRECV]='' then fTimOutRCV:=1000
    else fTimOutRCV:=StrToInt(S.Values[context+pnRECV]);

    if S.Values[context+pnHOST]='' then fHost:='LOCALHOST'
    else fHost:=S.Values[context+pnHOST];

    if S.Values[context+pnPORT]='' then fPort:=1024
    else fPort:=StrToInt(S.Values[context+pnPORT]);
  finally
    S.Free;
  end;
end;

function TDXTCPProtocol.Write(const Buf; Count: Integer): Longint;
begin
  Result:=0;
  if Assigned(fStream) and TDSockClient(fStream).Connected then Result:=fStream.Write(Buf,Count);
end;

{ TDXSirius }

function TDXSirius.CardinalToStr(x: Cardinal): String;
var
  sA : ShortString;
begin
  sA[0]:=#4;
  move(x,sA[1],4);
  Result:=sA;
end;

procedure TDXSirius.Close;
begin
  if Assigned(fProtocol) then fProtocol.Close;
end;

constructor TDXSirius.Create;
begin
  inherited;
  fProtocol := TDXTCPProtocol.Create;
end;

destructor TDXSirius.Destroy;
begin
  fProtocol.Free;
  fProtocol:=nil;
  inherited;
end;

procedure TDXSirius.Error(Msg: String);
begin
  raise EDProtocol.Create(ClassName+'::'+Msg);
end;

procedure TDXSirius.GetOptions(const context: String;
  var Params: String);
begin
end;

function  TDXSirius.Peer: String;
begin
  if Assigned(fProtocol) then Result := fProtocol.fHost
  else Result := ''
end;

procedure TDXSirius.Open;
begin
  if Assigned(fProtocol) then fProtocol.Open;
end;

function TDXSirius.ReadAlgorithm(xFields : TStringList): Boolean;
var
  Ch     : Char;
  fMsg   : String;
  sfName : String;
  sfValue: String;
  xC     : Cardinal;
  xL     : Cardinal;
  xSize  : Cardinal;
begin
  Result := False;

  xC:=0;
  sfValue:='';
  xFields.Clear;
  if not Assigned(fProtocol) or not TDSockClient(fProtocol.fStream).Connected {(fProtocol.fSocket=INVALID_SOCKET)} then exit;
  while xC<Length(cSingleDef) do
  begin
    fProtocol.Read(Ch,1);
    sfValue:=sfValue+Ch;
    inc(xC);
    if sfValue<>Copy(cSingleDef,1,xC) then
    begin
      xC:=1;
      sfValue:='';
    end;
  end;
  try
    xFields.Values[fnSign]:=sfValue;
    fProtocol.Read(xC,4);
    xFields.Values[fnVersion]:=IntToStr(xC);
    fProtocol.Read(xSize,4);
    fProtocol.Read(xC,4);
    xFields.Values[fnFLAGS]:=IntToStr(xC);
    if xSize>0 then
    begin
      SetLength(fMsg,xSize);
      fProtocol.Read(fMSG[1],xSize);
    end;
{Field Pars}
    while Length(fMSG)>0 do
    begin
      xC:=Pos(cFS,fMSG);
      if xC<=0 then xC:=Length(fMSG)+1;
      sfName:=Copy(fMSG,1,xC-1);
      if sfName='' then Error(ceInvalidMessage);
      if xC>Length(fMSG) then xC:=Length(fMSG);
      delete(fMSG,1,xC);
      xL:=Pos(fnNameSeparator,sfName);
      sfValue:=Copy(sfName,xL+1,Length(sfName)-xL);
      delete(sfName,xL,Length(sfName));
      while Length(Trim(sfName))<3 do sfName:='0'+Trim(sfName);
      if not Valid(sfName,ctFieldName)   then Error(Format(ceInvalidFieldName,[sfName]));
      if not Valid(sfValue,ctFieldData)  then Error(Format(ceInvalidFieldData,[sfName]));
      //  if fFields.IndexOfName(Name)<>-1 then Error(Format(ceDublicateField,[Name]));
      xFields.Values[sfName]:=sfValue;
    end;
    Result := True
  finally
  end;
end;

procedure TDXSirius.SetOptions(const context, Params: String);
begin
  fProtocol.SetOptions('TCP_',Params);
end;

function TDXSirius.Valid(Token: String; fType: Integer): Boolean;
var
  i : integer;
  sA : ShortString;
begin
  Result:=False;
  case fType of
   ctFieldData:
       for i:=1 to Length(Token) do
       begin
         sA:=Copy(Token,i,1);
         if not ((sA[1] in csLatin)or(sA[1]in csDigit)or(sA[1]in csCiril)or(sA[1]in csSeparator)) then exit;
       end;
   ctFieldName:
     begin
       if Length(Token)<>3 then exit;
       for i:=1 to Length(Token) do
       begin
         sA:=Copy(Token,i,1);
         if not(sA[1]in csDigit) then exit;
       end;
     end;
  end;
  Result:=True;
end;

function TDXSirius.WriteAlgorithm(xFields : TStringList): Boolean;
var
  i       : Integer;
  sA      : String;
  sMsg    : String;
  sPack   : String;
begin
  Result := False;
  try
    sMsg:=cSingleDef;
    sA:=xFields.Values[fnVersion];
    if sA='' then sA := PROTOCOL_VERSION;
    sMsg:=sMsg+CardinalToStr(StrToInt(sA));
    sPack:='';
    for i:=0 to xFields.Count-1 do
      if Valid(xFields.Names[i],ctFieldName) then
        sPack:=sPack+xFields.Names[i]+fnNameSeparator
               +xFields.Values[xFields.Names[i]]+cFS;
    sA:=CardinalToStr(Length(sPack));
    sMsg:=sMsg+sA;
    sA:=xFields.Values[fnFLAGS];
    if sA='' then sA:='1';
    sMsg:=sMsg+CardinalToStr(StrToInt(sA))+sPack;
    if Assigned(fProtocol) then fProtocol.Write(sMsg[1],Length(sMsg));
    Result := True;
  except
  end;
end;



end.
