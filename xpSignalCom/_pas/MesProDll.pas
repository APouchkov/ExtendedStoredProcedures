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

// Функции для настройки параметров подписи
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

  ERR_2: String 	= 'невозможно получить сертификат авторитета';
  ERR_3: String 	= 'невозможно получить список отмены';
  ERR_4: String 	= 'невозможно расшифровать подпись сертификата';
  ERR_5: String 	= 'невозможно расшифровать подпись списка отмены';
  ERR_6: String 	= 'невозможно декодировать открытый ключ авторитета';
  ERR_7: String 	= 'плохая подпись в сертификате';
  ERR_8: String 	= 'плохая подпись в списке отмены';
  ERR_9: String	  = 'срок действия сертификата еще не настал';
  ERR_10: String 	= 'срок действия сертификата уже истек';
  ERR_11: String 	= 'срок действия списка отмены еще не настал';
  ERR_12: String	= 'срок действия списка отмены уже истек';
  ERR_13: String 	= 'ошибка в формате поля сертификата "NotBefore"';
  ERR_14: String 	= 'ошибка в формате поля сертификата "NotAfter"';
  ERR_15: String 	= 'ошибка в формате поля списка отмены "LastUpdate"';
  ERR_16: String 	= 'ошибка в формате поля списка отмены "NextUpdate"';
  ERR_17: String 	= 'ошибка при выделении памяти';
  ERR_18: String 	= 'самоподписанный сертификат';
  ERR_19: String 	= 'самоподписанный сертификат в цепочке сертификатов';
  ERR_20: String 	= 'невозможно локально получить сертификат авторитета';
  ERR_21: String 	= 'невозможно проверить первый сертификат';
  ERR_22: String 	= 'цепочка сертификатов слишком большая';
  ERR_23: String 	= 'сертификат отменен';
  ERR_24: String 	= 'последний сертификат в цепочке не является доверенным';
  ERR_103: String	= 'ошибка при выделении памяти';
  ERR_104: String	= 'ошибка при чтении сертификата автора подписи';
  ERR_105: String	= 'ошибка при чтении сертификата получателя зашифрованных данных';
  ERR_106: String	= 'файл секретного ключа не задан';
  ERR_107: String	= 'ошибка при чтении файла секретного ключа';
  ERR_108: String	= 'ошибка при задании алгоритма хэш-функции';
  ERR_109: String	= 'ошибка при задании алгоритма шифрования';
  ERR_112: String = 'ошибка при чтении входных данных';
  ERR_114: String	= 'ошибка при записи выходных данных';
  ERR_115: String	= 'ошибка при задании информации автора подписи';
  ERR_116: String	= 'ошибка при установлении заданного алгоритма шифрования';
  ERR_117: String	= 'ошибка при задании информации получателя зашифрованных данных';
  ERR_118: String	= 'ошибка при инициализации процедуры подписи или шифрования';
  ERR_119: String	= 'ошибка при завершении процедуры подписи или шифрования';
  ERR_122: String	= 'ошибка при ASN.1-кодировании';
  ERR_123: String	= 'ошибка при ASN.1-декодировании';
  ERR_124: String	= 'неизвестный тип PKCS';
  ERR_125: String	= 'ошибка при расшифровке данных';
  ERR_127: String	= 'подписи не обнаружены';
  ERR_128: String	= 'обнаружены неподтвержденные подписи';
  ERR_129: String	= 'сертификат автора подписи не найден';
  ERR_132: String	= 'обязательные подписи не обнаружены';
  ERR_134: String	= 'сертификат получателя зашифрованных данных не найден';
  ERR_135: String = 'ошибка при чтении вектора инициализации генератора сл. чисел';
  ERR_136: String = 'файл вектора инициализации генератора сл. чисел не задан';
  ERR_138: String	= 'попытка расшифровать незашифрованные данные';
  ERR_143: String	= 'ошибка при записи ключа';
  ERR_144: String	= 'ошибка при генерации ключей';
  ERR_145: String	= 'ошибка в формате составного имени';
  ERR_147: String	= 'ошибка при подписи запроса';
  ERR_148: String	= 'файл запроса не задан';
  ERR_149: String	= 'ошибка при записи в файл запроса';
  ERR_150: String	= 'ошибка при задании номера версии запроса';
  ERR_153: String	= 'ошибка при чтении списка отмены';
  ERR_154: String	= 'ошибка при задании списка отмены';
  ERR_155: String	= 'плохой сертификат получателя';
  ERR_158: String	= 'ошибка при инициализации процедуры проверки подписи';
  ERR_159: String	= 'в параметре задан нулевой указатель';
  ERR_160: String	= 'размер заданного буфера меньше требуемого';
  ERR_161: String	= 'библиотека не инициализирована';
  ERR_162: String	= 'данные не подписаны';
  ERR_163: String	= 'подпись не подтверждена';
  ERR_164: String	= 'ошибка при чтении файла ключа СА';
  ERR_165: String	= 'ошибка при записи вектора инициализации генератора случайных чисел';
  ERR_166: String	= 'ошибка при чтении сертификата';
  ERR_167: String	= 'неверно задан алгоритм генерируемых ключей';
  ERR_168: String	= 'ошибка при считывании секретного ключа из ключевого носителя СКЗИ';
  ERR_169: String	= 'ошибка при инициализации СКЗИ';
  ERR_170: String	= 'ошибка при копировании секретного ключа СКЗИ';
  ERR_171: String	= 'файл ключа для формирования запроса не задан';
  ERR_172: String	= 'ошибка при чтении файла ключа для формирования запроса';
  ERR_173: String	= 'ошибка при чтении файла ключа для подписи запроса';
  ERR_174: String	= 'файл ключа для подписи запроса не задан';
  ERR_175: String	= 'ошибка при чтении ключевого носителя СКЗИ с ключом для формирования запроса';
  ERR_176: String	= 'ошибка при чтении ключевого носителя СКЗИ с ключом для подписи запроса';
  ERR_177: String	= 'ошибка при создании резервной копии ключевого носителя СКЗИ';
  ERR_178: String	= 'ошибка при инициализации криптобиблиотеки';
  ERR_179: String	= 'ошибка при создании ключевого носителя СКЗИ 3.1';
  ERR_180: String	= 'в заданном каталоге уже существуют ключевые объекты СКЗИ 3.1';
  ERR_181: String	= 'заданный двухключевой алгоритм не поддерживается данной функцией';
  ERR_182: String	= 'ошибка при создании запроса сертификата';
  ERR_183: String	= 'ошибка при добавлении сертификата CA';
  ERR_184: String	= 'процесс был прерван';
  ERR_185: String	= 'ошибка при декодировании запроса сертификата';
  ERR_186: String	= 'ошибка при декодировании сертификата';
  ERR_187: String	= 'ошибка при декодировании списка отмены сертификатов';
  ERR_188: String	= 'ошибка при кодировании запроса сертификата';
  ERR_189: String	= 'ошибка при кодировании сертификата';
  ERR_190: String	= 'ошибка при кодировании списка отмены сертификатов';
  ERR_191: String	= 'ошибка при декодировании секретного ключа';
  ERR_192: String	= 'ошибка при кодировании секретного ключа';
  ERR_193: String	= 'ошибка при удалении файла';
  ERR_194: String	= 'ошибка при копировании файла';
  ERR_195: String	= 'файл с заданным именем уже существует';
  ERR_196: String	= 'файл с сертификатом СА не найден';
  ERR_197: String	= 'ошибка при чтении файла запроса сертификата';
  ERR_198: String	= 'ошибка при формировании свертки запроса сертификата';
  ERR_199: String	= 'ошибка при формировании свертки секретного ключа';
  ERR_200: String	= 'ошибка при формировании свертки сертификата';
  ERR_201: String	= 'ошибка при выводе параметров сертификата';

function GetErrorMessage(ErrCode: integer): String;


var
  SimpleText: PChar = '123456789012345678901234567890123456789012345678901234567890'#0;

implementation

const
  errInit: string = 'Ошибка при инициализации библиотеки';
  errDigestAlgorithm: string = 'Ошибка при установке алгоритма хэш-функции';
  errCertificateToSign: string = 'Ошибка при установке параметра добавлениия сертификата в подпись';
  errTimeToSign: string = 'Ошибка при установке параметра включения времени в подпись';
  errSigner: string = 'Ошибка при добавлении сертификата автора подписи к контексту';
  errSignFile: string = 'Ошибка при подписи файла';
  errPSEPrivateKey: string = 'Ошибка при чтении файла секретного ключа';
  errCA: string = 'Ошибка при чтении сертификационного авторитета';
  errChkSignFile: string = 'Ошибка при проверке подписи';

function GetErrorMessage(ErrCode: integer): String;
begin
  case ErrCode of
    2: Result 	:='невозможно получить сертификат авторитета';
    3: Result 	:='невозможно получить список отмены';
    4: Result 	:='невозможно расшифровать подпись сертификата';
    5: Result 	:='невозможно расшифровать подпись списка отмены';
    6: Result 	:='невозможно декодировать открытый ключ авторитета';
    7: Result 	:='плохая подпись в сертификате';
    8: Result 	:='плохая подпись в списке отмены';
    9: Result	  :='срок действия сертификата еще не настал';
    10: Result 	:='срок действия сертификата уже истек';
    11: Result 	:='срок действия списка отмены еще не настал';
    12: Result	:='срок действия списка отмены уже истек';
    13: Result 	:='ошибка в формате поля сертификата "NotBefore"';
    14: Result 	:='ошибка в формате поля сертификата "NotAfter"';
    15: Result 	:='ошибка в формате поля списка отмены "LastUpdate"';
    16: Result 	:='ошибка в формате поля списка отмены "NextUpdate"';
    17: Result 	:='ошибка при выделении памяти';
    18: Result 	:='самоподписанный сертификат';
    19: Result 	:='самоподписанный сертификат в цепочке сертификатов';
    20: Result 	:='невозможно локально получить сертификат авторитета';
    21: Result 	:='невозможно проверить первый сертификат';
    22: Result 	:='цепочка сертификатов слишком большая';
    23: Result 	:='сертификат отменен';
    24: Result 	:='последний сертификат в цепочке не является доверенным';
    103: Result	:='ошибка при выделении памяти';
    104: Result	:='ошибка при чтении сертификата автора подписи';
    105: Result	:='ошибка при чтении сертификата получателя зашифрованных данных';
    106: Result	:='файл секретного ключа не задан';
    107: Result	:='ошибка при чтении файла секретного ключа';
    108: Result	:='ошибка при задании алгоритма хэш-функции';
    109: Result	:='ошибка при задании алгоритма шифрования';
    112: Result :='ошибка при чтении входных данных';
    114: Result	:='ошибка при записи выходных данных';
    115: Result	:='ошибка при задании информации автора подписи';
    116: Result	:='ошибка при установлении заданного алгоритма шифрования';
    117: Result	:='ошибка при задании информации получателя зашифрованных данных';
    118: Result	:='ошибка при инициализации процедуры подписи или шифрования';
    119: Result	:='ошибка при завершении процедуры подписи или шифрования';
    122: Result	:='ошибка при ASN.1-кодировании';
    123: Result	:='ошибка при ASN.1-декодировании';
    124: Result	:='неизвестный тип PKCS';
    125: Result	:='ошибка при расшифровке данных';
    127: Result	:='подписи не обнаружены';
    128: Result	:='обнаружены неподтвержденные подписи';
    129: Result	:='сертификат автора подписи не найден';
    132: Result	:='обязательные подписи не обнаружены';
    134: Result	:='сертификат получателя зашифрованных данных не найден';
    135: Result :='ошибка при чтении вектора инициализации генератора сл. чисел';
    136: Result :='файл вектора инициализации генератора сл. чисел не задан';
    138: Result	:='попытка расшифровать незашифрованные данные';
    143: Result	:='ошибка при записи ключа';
    144: Result	:='ошибка при генерации ключей';
    145: Result	:='ошибка в формате составного имени';
    147: Result	:='ошибка при подписи запроса';
    148: Result	:='файл запроса не задан';
    149: Result	:='ошибка при записи в файл запроса';
    150: Result	:='ошибка при задании номера версии запроса';
    153: Result	:='ошибка при чтении списка отмены';
    154: Result	:='ошибка при задании списка отмены';
    155: Result	:='плохой сертификат получателя';
    158: Result	:='ошибка при инициализации процедуры проверки подписи';
    159: Result	:='в параметре задан нулевой указатель';
    160: Result	:='размер заданного буфера меньше требуемого';
    161: Result	:='библиотека не инициализирована';
    162: Result	:='данные не подписаны';
    163: Result	:='подпись не подтверждена';
    164: Result	:='ошибка при чтении файла ключа СА';
    165: Result	:='ошибка при записи вектора инициализации генератора случайных чисел';
    166: Result	:='ошибка при чтении сертификата';
    167: Result	:='неверно задан алгоритм генерируемых ключей';
    168: Result	:='ошибка при считывании секретного ключа из ключевого носителя СКЗИ';
    169: Result	:='ошибка при инициализации СКЗИ';
    170: Result	:='ошибка при копировании секретного ключа СКЗИ';
    171: Result	:='файл ключа для формирования запроса не задан';
    172: Result	:='ошибка при чтении файла ключа для формирования запроса';
    173: Result	:='ошибка при чтении файла ключа для подписи запроса';
    174: Result	:='файл ключа для подписи запроса не задан';
    175: Result	:='ошибка при чтении ключевого носителя СКЗИ с ключом для формирования запроса';
    176: Result	:='ошибка при чтении ключевого носителя СКЗИ с ключом для подписи запроса';
    177: Result	:='ошибка при создании резервной копии ключевого носителя СКЗИ';
    178: Result	:='ошибка при инициализации криптобиблиотеки';
    179: Result	:='ошибка при создании ключевого носителя СКЗИ 3.1';
    180: Result	:='в заданном каталоге уже существуют ключевые объекты СКЗИ 3.1';
    181: Result	:='заданный двухключевой алгоритм не поддерживается данной функцией';
    182: Result	:='ошибка при создании запроса сертификата';
    183: Result	:='ошибка при добавлении сертификата CA';
    184: Result	:='процесс был прерван';
    185: Result	:='ошибка при декодировании запроса сертификата';
    186: Result	:='ошибка при декодировании сертификата';
    187: Result	:='ошибка при декодировании списка отмены сертификатов';
    188: Result	:='ошибка при кодировании запроса сертификата';
    189: Result	:='ошибка при кодировании сертификата';
    190: Result	:='ошибка при кодировании списка отмены сертификатов';
    191: Result	:='ошибка при декодировании секретного ключа';
    192: Result	:='ошибка при кодировании секретного ключа';
    193: Result	:='ошибка при удалении файла';
    194: Result	:='ошибка при копировании файла';
    195: Result	:='файл с заданным именем уже существует';
    196: Result	:='файл с сертификатом СА не найден';
    197: Result	:='ошибка при чтении файла запроса сертификата';
    198: Result	:='ошибка при формировании свертки запроса сертификата';
    199: Result	:='ошибка при формировании свертки секретного ключа';
    200: Result	:='ошибка при формировании свертки сертификата';
    201: Result	:='ошибка при выводе параметров сертификата';
  else Result := 'Неизвестная ошибка';
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

  // Данная функция возвращает число подписей.
  count := GetSignatureCount(sgn_ctx);
  if count <> 1 then
    Result.Add('Файл содержит более одной подписи');

  sub := GetSignatureSubject(sgn_ctx, 0);
  ins := GetSignatureIssuer(sgn_ctx, 0);
  dt := GetSignatureTime(sgn_ctx, 0);
  if sub <> nil then
  begin
    Result.Add('Subject: ' + String(sub));  // 0
    Result.Add('Issuer: ' + String(ins));   // 1
    Result.Add(strTime + String(dt));      // 2

  // Данная функция возвращает статус подписи (подтверждена или нет).
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
// Subject: /C=RU/L=Москва/O=FINAM/OU=37789/T=Клиент "КИС "Финам"/CN=Вайнштейн Олег Борисович

  if SignInfo.IndexOf('Status: OK') <> -1 then
    Result.Add('ПОДПИСЬ ПРОВЕРЕНА')
  else
    Result.Add('ПОДПИСЬ НЕ ПРОШЛА ПРОВЕРКУ');

  if SignInfo.Count = 4 then
  begin
    Result.Add('Подписано:');
    // nnPersone
    Result.Add('Код клиента: ' + copy(SignInfo[0], pos('/OU=', SignInfo[0])+4, pos('/T=', SignInfo[0])- 4 - pos('/OU=', SignInfo[0])));
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
  // Инициализация работы с библиотекой
  sgn_ctx := nil;
  try
    try
      RC := PKCS7Init(nil, 0);
      Result := RC = 0;
      if not Result then
        raise Exception.Create(GetErrorMessage(RC));

      // Установка алгоритма хэш-функции
      RC := SetDigestAlgorithm('RUS-HASH');
      Result := RC = 0;
      if not Result then raise Exception.Create(GetErrorMessage(RC));

      // Добавление сертификата в подпись
      RC := InsertCertificateToSign(1);
      Result := RC = 0;
      if not Result then raise Exception.Create(GetErrorMessage(RC));

      // Включение времени в подпись
      RC := InsertSigningTimeToSign(1);
      Result := RC = 0;
      if not Result then raise Exception.Create(GetErrorMessage(RC));

      // Считываем из СКЗИ секретный ключ автора подписи
      LoadFile(KeyFile, SKey);
      RC := AddPSEPrivateKeyFromBufferEx(PChar(ExtractFilePath(KeyFile)), nil, PChar(Skey), Length(SKey), nil);
      Result := RC = 0;
      if not Result then raise Exception.Create(GetErrorMessage(RC));

      // Считываем в память открытые ключи сертификационного авторитета
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
  // Инициализация работы с библиотекой
  sgn_ctx := nil;
  try
    RC := PKCS7Init(nil, 0);
    Result := RC = 0;
    if not Result then
      raise Exception.Create(GetErrorMessage(RC));
    try
      // Считываем в память открытые ключи сертификационного авторитета
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

      // Вывод информации о подписи.
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

  // Функции подписи и проверки подписи с использованием ключей СКЗИ.

  memo.Lines.Add('Signing...');

  // Формирование подписи.

  // Считываем из СКЗИ секретный ключ автора подписи.
  // Ключи СКЗИ могут быть считаны также функциями
  // AddPSEPrivateKeyFromBuffer() и AddPSEPrivateKeys().
//  Result := AddPSEPrivateKey('xxx', 'xxx\secret.key');
//  Result := AddPSEPrivateKeys('randomize', 'xxx\keys');
  LoadFile('xxx\secret.key',PK);
  Result := AddPSEPrivateKeyFromBufferEx('d:\Certificate', nil, @PK[1], Length(PK), nil);

  if(Result<>0) then begin memo.Lines.Add('Error: ' + IntToStr(Result)); Exit end else memo.Lines.Add('PSE Loaded...');

  // Считываем в память открытые ключи сертификационного авторитета
  // для проверки сертификата автора подписи и списков отмены.
  Result := AddCAs('d:\Certificate');
  if(Result<>0) then begin memo.Lines.Add('Error: ' + IntToStr(Result)); Exit end else memo.Lines.Add('CA Loaded...');

  // Создание контекста подписи.
  sgn_ctx := GetSignCTX();
  if sgn_ctx=nil then Exit else memo.Lines.Add('CTS Created...');

  try
  // Добавление автора подписи в контекст (задаем файл его сертификата).
  Result := AddSigner(sgn_ctx, BY_FILE, 'd:\Certificate\Key#16291#5.pem', nil);
  if(Result<>0) then begin memo.Lines.Add('Error: ' + IntToStr(Result)); Exit end else memo.Lines.Add('Buffer signing...');

  // Подпись блока данных

  // В данном примере подпись формируется отдельно от данных (шестой параметр)
  // Буфер для подписи будет выделен внутри функции "SignBufferEx",
  // т.к. указатель на буфер buf равен нулю.

  Result := SignBufferEx(sgn_ctx, SimpleText, strlen(SimpleText), @buf, @ln, 1);
  if(Result<>0) then begin memo.Lines.Add('Error: ' + IntToStr(Result)); Exit end;
  PK := ''; for i := 0 to pred(ln) do PK := PK + dec2hex(PByte(Integer(buf)+i)^); memo.Lines.Add('0x'+PK+'=');


//  SetLength(str,ln);
//  MoveMemory(@str[1],buf,ln);
  memo.Lines.Add('OK');

  memo.Lines.Add('File signing');

  // Подпись файла.
  // Подпись формируется в отдельном файле.

  Result := SignFileEx(sgn_ctx, 'test.txt', 'r3410.sgn', 1);
  if(Result<>0) then begin memo.Lines.Add('Error: ' + IntToStr(Result)); Exit end;

  memo.Lines.Add('OK');

  // Освобождаем контекст подписи.
  FreeSignCTX(sgn_ctx);
  sgn_ctx := nil;

  memo.Lines.Add('Signature checking...');

  // Проверка подписи.

  // Считываем в память открытые ключи сертификационного авторитета
  // для проверки сертификата автора подписи и списков отмены.
  Result := AddCAs('d:\Certificate');
  if(Result<>0) then Exit;

  // Считываем в память списки отмененных сертификатов.
//  Result := AddCRLs('pse31/u1/crl');
//  if(Result<>0) then Exit;

  // Создание контекста проверки подписи.
  sgn_ctx := GetSignCTX();
  if sgn_ctx=nil then Exit;

  // Добавление автора подписи в контекст (задаем файл его сертификата).
  Result := AddSigner(sgn_ctx, BY_FILE, 'd:\Certificate\Key#16291#5.pem', nil);
  if(Result<>0) then Exit;

  memo.Lines.Add('Buffer signature checking...');

  // Проверка подписи блока данных.

  Result := CheckBufferSignEx(sgn_ctx, buf, ln, nil, nil, 0, SimpleText, strlen(SimpleText));
  if(Result<>0) then Exit;

  // Вывод информации о подписи.
  SignaturesInfo(sgn_ctx);

  // Освобождаем выделенный для подписи буфер.
  FreeBuffer(buf);
  buf := nil;

  memo.Lines.Add('OK');

  memo.Lines.Add('File signature checking...');

  // Проверка подписи файла.

  Result := CheckFileSignEx(sgn_ctx, 'r3410.sgn', nil, 0, 'test.txt');
  if(Result<>0) then Exit;

  // Вывод информации о подписи.
  SignaturesInfo(sgn_ctx);

  // Освобождаем контекст проверки подписи.
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
