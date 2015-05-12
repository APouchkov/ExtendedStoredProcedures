unit MesProDll;

interface

uses
  SysUtils, Classes, Dialogs;

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

  type
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

  type
    CERTIFICATE_REQ_INFO = record
      Version: PChar;
      Subject: DISTINGUISHED_NAME;
      PublicKey: PChar;
      Signature: PChar;
      Text: PChar;
    end;

  PCERTIFICATE_REQ_INFO = ^CERTIFICATE_REQ_INFO;

procedure ClearBuffer(inp: PChar); cdecl; external 'mespro.dll';
procedure FreeBuffer(ptr: Pointer); stdcall; external 'mespro.dll';

function PKCS7Init(pse_path: PChar; reserved: Integer): Integer; stdcall; external 'mespro.dll';
function PKCS7Final(): Integer; stdcall; external 'mespro.dll';

function PSE31_Generation(pse_path: PChar; reserv1: Integer; reserv2: PChar; flags: Integer): Integer; stdcall; external 'mespro.dll';
function SetNewKeysAlgorithm(algor: PChar): Integer; stdcall; external 'mespro.dll';
function NewKeysGenerationEx(pse_path: PChar; reserv: PChar; keyfile: PChar; password: PChar; reqfile: PChar): Integer; stdcall; external 'mespro.dll';
function SetKeyGenerationCallbackFun(Func: Pointer): Integer; stdcall; external 'mespro.dll';

function SetCountry(Country: PChar): Integer; stdcall; external 'mespro.dll';
function SetStateOrProvince(StateOrProvince: PChar): Integer; stdcall; external 'mespro.dll';
function SetLocality(Locality: PChar): Integer; stdcall; external 'mespro.dll';
function SetOrganization(Organization: PChar): Integer; stdcall; external 'mespro.dll';
function SetOrganizationalUnit(OrganizationalUnit: PChar): Integer; stdcall; external 'mespro.dll';
function SetTitle(Title: PChar): Integer; stdcall; external 'mespro.dll';
function SetCommonName(CommonName: PChar): Integer; stdcall; external 'mespro.dll';
function SetEmailAddress(EmailAddress: PChar): Integer; stdcall; external 'mespro.dll';


function AddCAs(CAdir: PChar): Integer; stdcall; external 'mespro.dll';
function AddCRLs(CRLdir: PChar): Integer; stdcall; external 'mespro.dll';

function GetCipherCTX(): Pointer; stdcall; external 'mespro.dll'
procedure FreeCipherCTX(ctx: Pointer); stdcall; external 'mespro.dll';

function AddRecipient(ctx: Pointer; xtype: Integer; param1, param2: PChar): Integer; stdcall; external 'mespro.dll';
function EncryptBuffer(ctx: Pointer; in_buf: PChar; in_len: Integer; out_buf: PPChar; out_len: PInteger): Integer; stdcall; external 'mespro.dll';
function EncryptOneFile(ctx: Pointer; in_file: PChar; out_file: PChar): Integer; stdcall; external 'mespro.dll';
function AddPSEPrivateKeys(psedir: PChar; keydir: PChar): Integer; stdcall; external 'mespro.dll';
function AddPSEPrivateKeyFromBufferEx(pse_path, reserv, buf: PChar; len: Integer; pass: PChar): Integer; stdcall; external 'mespro.dll';

function AddCertificates(certdir: PChar): Integer; stdcall; external 'mespro.dll';
function ClearCertificates(): Integer; stdcall; external 'mespro.dll';
function DecryptBuffer(in_buf: Pointer; in_len: Integer; out_buf: PPointer; out_len: PInteger): Integer; stdcall; external 'mespro.dll';
function DecryptOneFile(in_file: PChar; out_file: PChar): Integer; stdcall; external 'mespro.dll';


function GetSignCTX: Pointer; stdcall; external 'mespro.dll';
function AddSigner(ctx: Pointer; xtype: Integer; param1,param2: PChar): Integer; stdcall; external 'mespro.dll';
procedure FreeSignCTX(ctx: Pointer); stdcall; external 'mespro.dll';

function AddPSEPrivateKey(psedir: PChar; keyfile: PChar): Integer; stdcall; external 'mespro.dll';
function SignBufferEx(sign_ctx: Pointer; in_buf: Pointer; in_len: Integer;
                             out_buf: PPointer; out_len: PInteger; detach: Integer): Integer; stdcall; external 'mespro.dll';
function CheckBufferSignEx(sign_ctx: Pointer; in_buf: Pointer; in_len: Integer;
                                  out_buf: PPointer; out_len: PInteger; sign_del: Integer;
								  detach: Pointer; detach_ln: Integer): Integer; stdcall; external 'mespro.dll';
function SignFileEx(ctx: Pointer; in_file: PChar; out_file: PChar; detach: Integer): Integer; stdcall; external 'mespro.dll';
function CheckFileSignEx(sign_ctx: Pointer; in_file: PChar; out_file: PChar;
                                sign_del: Integer; det_file: PChar): Integer; stdcall; external 'mespro.dll';

function GetCertificateInfo(certfile: PChar): PChar; stdcall; external 'mespro.dll';
function GetCertificateInfoEx(certfile: PChar; info: PCERTIFICATE_INFO): Integer; stdcall; external 'mespro.dll';
function GetCertificateInfoBufferEx(buf: PChar; ln: Integer; info: PCERTIFICATE_INFO): Integer; stdcall; external 'mespro.dll';
function GetCertPublicKeyAlgorithmBuffer(buf: PChar; ln: Integer): PChar; stdcall; external 'mespro.dll';
procedure FreeCertificateInfo(info: PCERTIFICATE_INFO); stdcall; external 'mespro.dll';

function  GetRequestInfoBuffer(buf: PChar; ln: Integer; info: PCERTIFICATE_REQ_INFO): Integer; stdcall; external 'mespro.dll';
procedure FreeRequestInfo(info: PCERTIFICATE_REQ_INFO); stdcall; external 'mespro.dll';

function  GetSignatureTime(ctx: Pointer; ind: Integer): PChar; stdcall; external 'mespro.dll';
function  GetSignatureCount(ctx: Pointer): Integer; stdcall; external 'mespro.dll';
function  GetSignatureSubject(ctx: Pointer; ind: Integer): PChar; stdcall; external 'mespro.dll';
function  GetSignatureIssuer(ctx: Pointer; ind: Integer): PChar; stdcall; external 'mespro.dll';
function  GetSignatureStatus(ctx: Pointer; ind: Integer): Integer; stdcall; external 'mespro.dll';

// ������� ��� ��������� ���������� �������
function SetDigestAlgorithm(Digest: PChar): Integer; stdcall; external 'mespro.dll';
function InsertCertificateToSign(Insert: Integer): Integer; stdcall; external 'mespro.dll';
function InsertSigningTimeToSign(Insert: Integer): Integer; stdcall; external 'mespro.dll';
function SetCertificateVerifyFlags(Flags: Cardinal): Integer; stdcall; external 'mespro.dll';

function SignaturesInfo(sgn_ctx: Pointer): TStrings;
function ParseSignInfo(SignInfo: TStrings): TStrings;
function LoadFile(const FileName: String; var Data: String): Boolean;
function SignFile(KeyFile, CertFile, CAPath, SignFile: String; isInt: Boolean = False): Boolean;
function CheckSignFile(CAPath, SignFile: String; out SignInfo: TStringList): Boolean;

const
  BY_FILE			  = 0;
  BY_SUBJECT	  = 1;
  BY_SERIAL		  = 2;
  BY_COMPONENTS	=	3;
  BY_BUFFER			= 4;
  // Flags definitions for SetCertificateVerifyFlags, VerifyCertificate and VerifyCRL
  STRICT_CERT_VERIFICATION      = $00000000;
  CERT_NOT_YET_VALID_IGNORE	    = $00000001;
  CERT_HAS_EXPIRED_IGNORE	      = $00000002;
  CERT_REVOKED_IGNORE	          =	$00000004;
  ISSUER_CERT_NOT_FOUND_IGNORE  =	$00000008;
  DEPTH_ZERO_ROOT_IGNORE        = $00000010;
  CRL_MUST_EXIST                = $00000020;
  FAIL_IF_CRL_NOT_YET_VALID	    =	$00000040;
  FAIL_IF_CRL_HAS_EXPIRED	      = $00000080;
  CRL_NOT_YET_VALID_IGNORE      =	$00000100;
  CRL_HAS_EXPIRED_IGNORE	      =	$00000200;

  MAX_FILE_SIZE  = 5120;

  LibKey   : String = 'F752AA41';

  strTime: String = 'Time: ';

  ERR_2: String 	= '���������� �������� ���������� ����������';
  ERR_3: String 	= '���������� �������� ������ ������';
  ERR_4: String 	= '���������� ������������ ������� �����������';
  ERR_5: String 	= '���������� ������������ ������� ������ ������';
  ERR_6: String 	= '���������� ������������ �������� ���� ����������';
  ERR_7: String 	= '������ ������� � �����������';
  ERR_8: String 	= '������ ������� � ������ ������';
  ERR_9: String	  = '���� �������� ����������� ��� �� ������';
  ERR_10: String 	= '���� �������� ����������� ��� �����';
  ERR_11: String 	= '���� �������� ������ ������ ��� �� ������';
  ERR_12: String	= '���� �������� ������ ������ ��� �����';
  ERR_13: String 	= '������ � ������� ���� ����������� "NotBefore"';
  ERR_14: String 	= '������ � ������� ���� ����������� "NotAfter"';
  ERR_15: String 	= '������ � ������� ���� ������ ������ "LastUpdate"';
  ERR_16: String 	= '������ � ������� ���� ������ ������ "NextUpdate"';
  ERR_17: String 	= '������ ��� ��������� ������';
  ERR_18: String 	= '��������������� ����������';
  ERR_19: String 	= '��������������� ���������� � ������� ������������';
  ERR_20: String 	= '���������� �������� �������� ���������� ����������';
  ERR_21: String 	= '���������� ��������� ������ ����������';
  ERR_22: String 	= '������� ������������ ������� �������';
  ERR_23: String 	= '���������� �������';
  ERR_24: String 	= '��������� ���������� � ������� �� �������� ����������';
  ERR_103: String	= '������ ��� ��������� ������';
  ERR_104: String	= '������ ��� ������ ����������� ������ �������';
  ERR_105: String	= '������ ��� ������ ����������� ���������� ������������� ������';
  ERR_106: String	= '���� ���������� ����� �� �����';
  ERR_107: String	= '������ ��� ������ ����� ���������� �����';
  ERR_108: String	= '������ ��� ������� ��������� ���-�������';
  ERR_109: String	= '������ ��� ������� ��������� ����������';
  ERR_112: String = '������ ��� ������ ������� ������';
  ERR_114: String	= '������ ��� ������ �������� ������';
  ERR_115: String	= '������ ��� ������� ���������� ������ �������';
  ERR_116: String	= '������ ��� ������������ ��������� ��������� ����������';
  ERR_117: String	= '������ ��� ������� ���������� ���������� ������������� ������';
  ERR_118: String	= '������ ��� ������������� ��������� ������� ��� ����������';
  ERR_119: String	= '������ ��� ���������� ��������� ������� ��� ����������';
  ERR_122: String	= '������ ��� ASN.1-�����������';
  ERR_123: String	= '������ ��� ASN.1-�������������';
  ERR_124: String	= '����������� ��� PKCS';
  ERR_125: String	= '������ ��� ����������� ������';
  ERR_127: String	= '������� �� ����������';
  ERR_128: String	= '���������� ���������������� �������';
  ERR_129: String	= '���������� ������ ������� �� ������';
  ERR_132: String	= '������������ ������� �� ����������';
  ERR_134: String	= '���������� ���������� ������������� ������ �� ������';
  ERR_135: String = '������ ��� ������ ������� ������������� ���������� ��. �����';
  ERR_136: String = '���� ������� ������������� ���������� ��. ����� �� �����';
  ERR_138: String	= '������� ������������ ��������������� ������';
  ERR_143: String	= '������ ��� ������ �����';
  ERR_144: String	= '������ ��� ��������� ������';
  ERR_145: String	= '������ � ������� ���������� �����';
  ERR_147: String	= '������ ��� ������� �������';
  ERR_148: String	= '���� ������� �� �����';
  ERR_149: String	= '������ ��� ������ � ���� �������';
  ERR_150: String	= '������ ��� ������� ������ ������ �������';
  ERR_153: String	= '������ ��� ������ ������ ������';
  ERR_154: String	= '������ ��� ������� ������ ������';
  ERR_155: String	= '������ ���������� ����������';
  ERR_158: String	= '������ ��� ������������� ��������� �������� �������';
  ERR_159: String	= '� ��������� ����� ������� ���������';
  ERR_160: String	= '������ ��������� ������ ������ ����������';
  ERR_161: String	= '���������� �� ����������������';
  ERR_162: String	= '������ �� ���������';
  ERR_163: String	= '������� �� ������������';
  ERR_164: String	= '������ ��� ������ ����� ����� ��';
  ERR_165: String	= '������ ��� ������ ������� ������������� ���������� ��������� �����';
  ERR_166: String	= '������ ��� ������ �����������';
  ERR_167: String	= '������� ����� �������� ������������ ������';
  ERR_168: String	= '������ ��� ���������� ���������� ����� �� ��������� �������� ����';
  ERR_169: String	= '������ ��� ������������� ����';
  ERR_170: String	= '������ ��� ����������� ���������� ����� ����';
  ERR_171: String	= '���� ����� ��� ������������ ������� �� �����';
  ERR_172: String	= '������ ��� ������ ����� ����� ��� ������������ �������';
  ERR_173: String	= '������ ��� ������ ����� ����� ��� ������� �������';
  ERR_174: String	= '���� ����� ��� ������� ������� �� �����';
  ERR_175: String	= '������ ��� ������ ��������� �������� ���� � ������ ��� ������������ �������';
  ERR_176: String	= '������ ��� ������ ��������� �������� ���� � ������ ��� ������� �������';
  ERR_177: String	= '������ ��� �������� ��������� ����� ��������� �������� ����';
  ERR_178: String	= '������ ��� ������������� ����������������';
  ERR_179: String	= '������ ��� �������� ��������� �������� ���� 3.1';
  ERR_180: String	= '� �������� �������� ��� ���������� �������� ������� ���� 3.1';
  ERR_181: String	= '�������� ������������ �������� �� �������������� ������ ��������';
  ERR_182: String	= '������ ��� �������� ������� �����������';
  ERR_183: String	= '������ ��� ���������� ����������� CA';
  ERR_184: String	= '������� ��� �������';
  ERR_185: String	= '������ ��� ������������� ������� �����������';
  ERR_186: String	= '������ ��� ������������� �����������';
  ERR_187: String	= '������ ��� ������������� ������ ������ ������������';
  ERR_188: String	= '������ ��� ����������� ������� �����������';
  ERR_189: String	= '������ ��� ����������� �����������';
  ERR_190: String	= '������ ��� ����������� ������ ������ ������������';
  ERR_191: String	= '������ ��� ������������� ���������� �����';
  ERR_192: String	= '������ ��� ����������� ���������� �����';
  ERR_193: String	= '������ ��� �������� �����';
  ERR_194: String	= '������ ��� ����������� �����';
  ERR_195: String	= '���� � �������� ������ ��� ����������';
  ERR_196: String	= '���� � ������������ �� �� ������';
  ERR_197: String	= '������ ��� ������ ����� ������� �����������';
  ERR_198: String	= '������ ��� ������������ ������� ������� �����������';
  ERR_199: String	= '������ ��� ������������ ������� ���������� �����';
  ERR_200: String	= '������ ��� ������������ ������� �����������';
  ERR_201: String	= '������ ��� ������ ���������� �����������';

function GetErrorMessage(ErrCode: integer): String;


var
  SimpleText: PChar = '123456789012345678901234567890123456789012345678901234567890'#0;

implementation

const
  errInit: string = '������ ��� ������������� ����������';
  errDigestAlgorithm: string = '������ ��� ��������� ��������� ���-�������';
  errCertificateToSign: string = '������ ��� ��������� ��������� ����������� ����������� � �������';
  errTimeToSign: string = '������ ��� ��������� ��������� ��������� ������� � �������';
  errSigner: string = '������ ��� ���������� ����������� ������ ������� � ���������';
  errSignFile: string = '������ ��� ������� �����';
  errPSEPrivateKey: string = '������ ��� ������ ����� ���������� �����';
  errCA: string = '������ ��� ������ ����������������� ����������';
  errChkSignFile: string = '������ ��� �������� �������';

function GetErrorMessage(ErrCode: integer): String;
begin
  case ErrCode of
    2: Result 	:='���������� �������� ���������� ����������';
    3: Result 	:='���������� �������� ������ ������';
    4: Result 	:='���������� ������������ ������� �����������';
    5: Result 	:='���������� ������������ ������� ������ ������';
    6: Result 	:='���������� ������������ �������� ���� ����������';
    7: Result 	:='������ ������� � �����������';
    8: Result 	:='������ ������� � ������ ������';
    9: Result	  :='���� �������� ����������� ��� �� ������';
    10: Result 	:='���� �������� ����������� ��� �����';
    11: Result 	:='���� �������� ������ ������ ��� �� ������';
    12: Result	:='���� �������� ������ ������ ��� �����';
    13: Result 	:='������ � ������� ���� ����������� "NotBefore"';
    14: Result 	:='������ � ������� ���� ����������� "NotAfter"';
    15: Result 	:='������ � ������� ���� ������ ������ "LastUpdate"';
    16: Result 	:='������ � ������� ���� ������ ������ "NextUpdate"';
    17: Result 	:='������ ��� ��������� ������';
    18: Result 	:='��������������� ����������';
    19: Result 	:='��������������� ���������� � ������� ������������';
    20: Result 	:='���������� �������� �������� ���������� ����������';
    21: Result 	:='���������� ��������� ������ ����������';
    22: Result 	:='������� ������������ ������� �������';
    23: Result 	:='���������� �������';
    24: Result 	:='��������� ���������� � ������� �� �������� ����������';
    103: Result	:='������ ��� ��������� ������';
    104: Result	:='������ ��� ������ ����������� ������ �������';
    105: Result	:='������ ��� ������ ����������� ���������� ������������� ������';
    106: Result	:='���� ���������� ����� �� �����';
    107: Result	:='������ ��� ������ ����� ���������� �����';
    108: Result	:='������ ��� ������� ��������� ���-�������';
    109: Result	:='������ ��� ������� ��������� ����������';
    112: Result :='������ ��� ������ ������� ������';
    114: Result	:='������ ��� ������ �������� ������';
    115: Result	:='������ ��� ������� ���������� ������ �������';
    116: Result	:='������ ��� ������������ ��������� ��������� ����������';
    117: Result	:='������ ��� ������� ���������� ���������� ������������� ������';
    118: Result	:='������ ��� ������������� ��������� ������� ��� ����������';
    119: Result	:='������ ��� ���������� ��������� ������� ��� ����������';
    122: Result	:='������ ��� ASN.1-�����������';
    123: Result	:='������ ��� ASN.1-�������������';
    124: Result	:='����������� ��� PKCS';
    125: Result	:='������ ��� ����������� ������';
    127: Result	:='������� �� ����������';
    128: Result	:='���������� ���������������� �������';
    129: Result	:='���������� ������ ������� �� ������';
    132: Result	:='������������ ������� �� ����������';
    134: Result	:='���������� ���������� ������������� ������ �� ������';
    135: Result :='������ ��� ������ ������� ������������� ���������� ��. �����';
    136: Result :='���� ������� ������������� ���������� ��. ����� �� �����';
    138: Result	:='������� ������������ ��������������� ������';
    143: Result	:='������ ��� ������ �����';
    144: Result	:='������ ��� ��������� ������';
    145: Result	:='������ � ������� ���������� �����';
    147: Result	:='������ ��� ������� �������';
    148: Result	:='���� ������� �� �����';
    149: Result	:='������ ��� ������ � ���� �������';
    150: Result	:='������ ��� ������� ������ ������ �������';
    153: Result	:='������ ��� ������ ������ ������';
    154: Result	:='������ ��� ������� ������ ������';
    155: Result	:='������ ���������� ����������';
    158: Result	:='������ ��� ������������� ��������� �������� �������';
    159: Result	:='� ��������� ����� ������� ���������';
    160: Result	:='������ ��������� ������ ������ ����������';
    161: Result	:='���������� �� ����������������';
    162: Result	:='������ �� ���������';
    163: Result	:='������� �� ������������';
    164: Result	:='������ ��� ������ ����� ����� ��';
    165: Result	:='������ ��� ������ ������� ������������� ���������� ��������� �����';
    166: Result	:='������ ��� ������ �����������';
    167: Result	:='������� ����� �������� ������������ ������';
    168: Result	:='������ ��� ���������� ���������� ����� �� ��������� �������� ����';
    169: Result	:='������ ��� ������������� ����';
    170: Result	:='������ ��� ����������� ���������� ����� ����';
    171: Result	:='���� ����� ��� ������������ ������� �� �����';
    172: Result	:='������ ��� ������ ����� ����� ��� ������������ �������';
    173: Result	:='������ ��� ������ ����� ����� ��� ������� �������';
    174: Result	:='���� ����� ��� ������� ������� �� �����';
    175: Result	:='������ ��� ������ ��������� �������� ���� � ������ ��� ������������ �������';
    176: Result	:='������ ��� ������ ��������� �������� ���� � ������ ��� ������� �������';
    177: Result	:='������ ��� �������� ��������� ����� ��������� �������� ����';
    178: Result	:='������ ��� ������������� ����������������';
    179: Result	:='������ ��� �������� ��������� �������� ���� 3.1';
    180: Result	:='� �������� �������� ��� ���������� �������� ������� ���� 3.1';
    181: Result	:='�������� ������������ �������� �� �������������� ������ ��������';
    182: Result	:='������ ��� �������� ������� �����������';
    183: Result	:='������ ��� ���������� ����������� CA';
    184: Result	:='������� ��� �������';
    185: Result	:='������ ��� ������������� ������� �����������';
    186: Result	:='������ ��� ������������� �����������';
    187: Result	:='������ ��� ������������� ������ ������ ������������';
    188: Result	:='������ ��� ����������� ������� �����������';
    189: Result	:='������ ��� ����������� �����������';
    190: Result	:='������ ��� ����������� ������ ������ ������������';
    191: Result	:='������ ��� ������������� ���������� �����';
    192: Result	:='������ ��� ����������� ���������� �����';
    193: Result	:='������ ��� �������� �����';
    194: Result	:='������ ��� ����������� �����';
    195: Result	:='���� � �������� ������ ��� ����������';
    196: Result	:='���� � ������������ �� �� ������';
    197: Result	:='������ ��� ������ ����� ������� �����������';
    198: Result	:='������ ��� ������������ ������� ������� �����������';
    199: Result	:='������ ��� ������������ ������� ���������� �����';
    200: Result	:='������ ��� ������������ ������� �����������';
    201: Result	:='������ ��� ������ ���������� �����������';
  else Result := '����������� ������';
  end;
end;

function LoadFile(const FileName: String; var Data: String): Boolean;
var
  FileHandle: Integer;
  FileLength: Integer;
begin
  Result := False; Data := EmptyStr;
  if FileExists(FileName) then
  begin
    FileHandle := FileOpen(FileName, fmOpenRead or fmShareDenyNone);
    if FileHandle<>-1 then
    try
      FileLength := FileSeek(FileHandle,0,2);
      if FileLength<=MAX_FILE_SIZE then
      begin
        FileSeek(FileHandle,0,0);
        SetLength(Data,FileLength);
        Result := FileRead(FileHandle,Data[1],FileLength) > 0;
      end;
    finally
      FileClose(FileHandle);
    end;
  end
end;

function SignaturesInfo(sgn_ctx: Pointer): TStrings;
var
  sub,ins,dt: PChar;
  i, count: Integer;
begin
  Result := TStringList.Create;

  // ������ ������� ���������� ����� ��������.
  count := GetSignatureCount(sgn_ctx);
  if count <> 1 then
    Result.Add('���� �������� ����� ����� �������');

  sub := GetSignatureSubject(sgn_ctx, 0);
  ins := GetSignatureIssuer(sgn_ctx, 0);
  dt := GetSignatureTime(sgn_ctx, 0);
  if sub <> nil then
  begin
    Result.Add('Subject: ' + String(sub));  // 0
    Result.Add('Issuer: ' + String(ins));   // 1
    Result.Add(strTime + String(dt));      // 2

  // ������ ������� ���������� ������ ������� (������������ ��� ���).
    i := GetSignatureStatus(sgn_ctx, 0);
    if(i = 0) then
      Result.Add('Status: OK')
    else
      Result.Add('Error: ' + IntToStr(i));

    FreeBuffer(sub);
  end;
end;

function ParseSignInfo(SignInfo: TStrings): TStrings;
begin
  Result := TStringList.Create;
// Subject: /C=RU/L=������/O=FINAM/OU=37789/T=������ "��� "�����"/CN=��������� ���� ���������

  if SignInfo.IndexOf('Status: OK') <> -1 then
    Result.Add('������� ���������')
  else
    Result.Add('������� �� ������ ��������');

  if SignInfo.Count = 4 then
  begin
    Result.Add('���������:');
    // nnPersone
    Result.Add('��� �������: ' + copy(SignInfo[0], pos('/OU=', SignInfo[0])+4, pos('/T=', SignInfo[0])- 4 - pos('/OU=', SignInfo[0])));
    // time
    Result.Add(StringReplace(SignInfo[2], strTime, EmptyStr, [rfReplaceAll, rfIgnoreCase]));
    // subject
    Result.Add(copy(SignInfo[0], pos('/T=', SignInfo[0])+3, pos('/CN=', SignInfo[0])- 3 - pos('/T=', SignInfo[0])));
    Result.Add(copy(SignInfo[0], pos('/CN=', SignInfo[0])+4, Length(SignInfo[0])));
  end;
end;

function SignFile(KeyFile, CertFile, CAPath, SignFile: String; isInt: Boolean): Boolean;
var
  RC: integer;
  sgn_ctx: Pointer;
  SKey: string;
  ext: string;
begin
  if isInt then
    ext := '.sga'
  else
    ext := '.sgn';
  Result := False;
  ClearBuffer(PChar(LibKey));
  // ������������� ������ � �����������
  sgn_ctx := nil;
  try
    try
      RC := PKCS7Init(nil, 0);
      Result := RC = 0;
      if not Result then
        raise Exception.Create(GetErrorMessage(RC));

      // ��������� ��������� ���-�������
      RC := SetDigestAlgorithm('RUS-HASH');
      Result := RC = 0;
      if not Result then raise Exception.Create(GetErrorMessage(RC));

      // ���������� ����������� � �������
      RC := InsertCertificateToSign(1);
      Result := RC = 0;
      if not Result then raise Exception.Create(GetErrorMessage(RC));

      // ��������� ������� � �������
      RC := InsertSigningTimeToSign(1);
      Result := RC = 0;
      if not Result then raise Exception.Create(GetErrorMessage(RC));

      // ��������� �� ���� ��������� ���� ������ �������
      LoadFile(KeyFile, SKey);
      RC := AddPSEPrivateKeyFromBufferEx(PChar(ExtractFilePath(KeyFile)), nil, PChar(Skey), Length(SKey), nil);
      Result := RC = 0;
      if not Result then raise Exception.Create(GetErrorMessage(RC));

      // ��������� � ������ �������� ����� ����������������� ����������
      RC := AddCAs(PChar(CAPath));
      Result := RC = 0;
      if not Result then raise Exception.Create(GetErrorMessage(RC));

      sgn_ctx := GetSignCTX;
      RC := AddSigner(sgn_ctx, BY_FILE, PChar(CertFile), nil);
      Result := RC = 0;
      if not Result then raise Exception.Create(GetErrorMessage(RC));

      RC := SignFileEx(sgn_ctx, PChar(SignFile), PChar(SignFile + ext), 1);
      Result := RC = 0;
      if not Result then raise Exception.Create(GetErrorMessage(RC));
    except
      on E: Exception do
        MessageDlg(E.Message, mtError, [mbOk], 0);
    end;
  finally
    FreeSignCTX(sgn_ctx);
    sgn_ctx := nil;
    PKCS7Final;
  end;
end;

function CheckSignFile(CAPath, SignFile: String; out SignInfo: TStringList): Boolean;
var
  RC: integer;
  sgn_ctx: Pointer;
begin
  ClearBuffer(PChar(LibKey));
  // ������������� ������ � �����������
  sgn_ctx := nil;
  try
    RC := PKCS7Init(nil, 0);
    Result := RC = 0;
    if not Result then
      raise Exception.Create(GetErrorMessage(RC));
    try
      // ��������� � ������ �������� ����� ����������������� ����������
      RC := AddCAs(PChar(CAPath));
      Result := RC = 0;
      if not Result then
        raise Exception.Create(GetErrorMessage(RC));

      sgn_ctx := GetSignCTX;
      RC := SetCertificateVerifyFlags(CERT_HAS_EXPIRED_IGNORE);
      if not Result then
        raise Exception.Create(GetErrorMessage(RC));

      RC := CheckFileSignEx(sgn_ctx, PChar(SignFile), nil, 1, PChar(ChangeFileExt(SignFile, EmptyStr)));
      Result := RC = 0;
      if not Result then
        raise Exception.Create(GetErrorMessage(RC));

      // ����� ���������� � �������.
      SignInfo.AddStrings(ParseSignInfo(SignaturesInfo(sgn_ctx)));
    except
      on E: Exception do
        MessageDlg(E.Message, mtError, [mbOk], 0);
    end;
  finally
    FreeSignCTX(sgn_ctx);
    sgn_ctx := nil;
    PKCS7Final;
  end;
end;

{
function TForm1.SignatureExamples: Integer;
//*****************************************************************************/
var
  sgn_ctx: Pointer;
  buf: Pointer;
  i,ln: Integer;
  str: String;
  PK: String;
  function Dec2Hex(v: Byte): String;
  var
    x: Byte;
  begin
    x := v div 16; v := v mod 16;
    if x<10 then Result := chr(Ord('0')+x) else Result := chr(Ord('A')+x-10);
    if v<10 then Result := Result + chr(Ord('0')+v) else Result := Result + chr(Ord('A')+v-10);
  end;
begin
  sgn_ctx := nil;
  buf := nil;
  i := 0;

  // ������� ������� � �������� ������� � �������������� ������ ����.

  memo.Lines.Add('Signing...');

  // ������������ �������.

  // ��������� �� ���� ��������� ���� ������ �������.
  // ����� ���� ����� ���� ������� ����� ���������
  // AddPSEPrivateKeyFromBuffer() � AddPSEPrivateKeys().
//  Result := AddPSEPrivateKey('xxx', 'xxx\secret.key');
//  Result := AddPSEPrivateKeys('randomize', 'xxx\keys');
  LoadFile('xxx\secret.key',PK);
  Result := AddPSEPrivateKeyFromBufferEx('d:\Certificate', nil, @PK[1], Length(PK), nil);

  if(Result<>0) then begin memo.Lines.Add('Error: ' + IntToStr(Result)); Exit end else memo.Lines.Add('PSE Loaded...');

  // ��������� � ������ �������� ����� ����������������� ����������
  // ��� �������� ����������� ������ ������� � ������� ������.
  Result := AddCAs('d:\Certificate');
  if(Result<>0) then begin memo.Lines.Add('Error: ' + IntToStr(Result)); Exit end else memo.Lines.Add('CA Loaded...');

  // �������� ��������� �������.
  sgn_ctx := GetSignCTX();
  if sgn_ctx=nil then Exit else memo.Lines.Add('CTS Created...');

  try
  // ���������� ������ ������� � �������� (������ ���� ��� �����������).
  Result := AddSigner(sgn_ctx, BY_FILE, 'd:\Certificate\Key#16291#5.pem', nil);
  if(Result<>0) then begin memo.Lines.Add('Error: ' + IntToStr(Result)); Exit end else memo.Lines.Add('Buffer signing...');

  // ������� ����� ������

  // � ������ ������� ������� ����������� �������� �� ������ (������ ��������)
  // ����� ��� ������� ����� ������� ������ ������� "SignBufferEx",
  // �.�. ��������� �� ����� buf ����� ����.

  Result := SignBufferEx(sgn_ctx, SimpleText, strlen(SimpleText), @buf, @ln, 1);
  if(Result<>0) then begin memo.Lines.Add('Error: ' + IntToStr(Result)); Exit end;
  PK := ''; for i := 0 to pred(ln) do PK := PK + dec2hex(PByte(Integer(buf)+i)^); memo.Lines.Add('0x'+PK+'=');


//  SetLength(str,ln);
//  MoveMemory(@str[1],buf,ln);
  memo.Lines.Add('OK');

  memo.Lines.Add('File signing');

  // ������� �����.
  // ������� ����������� � ��������� �����.

  Result := SignFileEx(sgn_ctx, 'test.txt', 'r3410.sgn', 1);
  if(Result<>0) then begin memo.Lines.Add('Error: ' + IntToStr(Result)); Exit end;

  memo.Lines.Add('OK');

  // ����������� �������� �������.
  FreeSignCTX(sgn_ctx);
  sgn_ctx := nil;

  memo.Lines.Add('Signature checking...');

  // �������� �������.

  // ��������� � ������ �������� ����� ����������������� ����������
  // ��� �������� ����������� ������ ������� � ������� ������.
  Result := AddCAs('d:\Certificate');
  if(Result<>0) then Exit;

  // ��������� � ������ ������ ���������� ������������.
//  Result := AddCRLs('pse31/u1/crl');
//  if(Result<>0) then Exit;

  // �������� ��������� �������� �������.
  sgn_ctx := GetSignCTX();
  if sgn_ctx=nil then Exit;

  // ���������� ������ ������� � �������� (������ ���� ��� �����������).
  Result := AddSigner(sgn_ctx, BY_FILE, 'd:\Certificate\Key#16291#5.pem', nil);
  if(Result<>0) then Exit;

  memo.Lines.Add('Buffer signature checking...');

  // �������� ������� ����� ������.

  Result := CheckBufferSignEx(sgn_ctx, buf, ln, nil, nil, 0, SimpleText, strlen(SimpleText));
  if(Result<>0) then Exit;

  // ����� ���������� � �������.
  SignaturesInfo(sgn_ctx);

  // ����������� ���������� ��� ������� �����.
  FreeBuffer(buf);
  buf := nil;

  memo.Lines.Add('OK');

  memo.Lines.Add('File signature checking...');

  // �������� ������� �����.

  Result := CheckFileSignEx(sgn_ctx, 'r3410.sgn', nil, 0, 'test.txt');
  if(Result<>0) then Exit;

  // ����� ���������� � �������.
  SignaturesInfo(sgn_ctx);

  // ����������� �������� �������� �������.
  FreeSignCTX(sgn_ctx);
  sgn_ctx := nil;

  memo.Lines.Add('OK');

 finally
   if(buf <> nil) then FreeBuffer(buf);
   if(sgn_ctx <> nil) then FreeBuffer(sgn_ctx);
 end;

end;
}

end.
