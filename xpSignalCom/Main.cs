using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Microsoft.SqlServer.Server;
using System.Data.SqlTypes;
using System.Runtime.InteropServices;
using System.Reflection;
using System.IO;
using System.Data;
using System.Data.SqlClient;
using Ini;

public unsafe class T
{
    private struct DISTINGUISHED_NAME
    {
        public string Country;
        public string StateOrProvince;
        public string Locality;
        public string Organization;
        public string OrganizationalUnit;
        public string Title;
        public string CommonName;
        public string EmailAddress;
    }

    private struct CERTIFICATE_INFO
    {
        public string Version;
        public string SerialNumber;
        public string NotBefore;
        public string NotAfter;
        public DISTINGUISHED_NAME Issuer;
        public DISTINGUISHED_NAME Subject;
        public string PublicKey;
        public string X509v3Extensions;
        public string Signature;
        public string Text;
    }

    private struct CERTIFICATE_REQ_INFO
    {
        public string Version;
        public DISTINGUISHED_NAME Subject;
        public string PublicKey;
        public string Signature;
        public string Text;
    }        

    [DllImport("Mespro.dll")]
    private static extern int SetLocality(string Locality);
    [DllImport("Mespro.dll")]
    private static extern int SetCountry(string Country);
    [DllImport("Mespro.dll")]
    private static extern int SetStateOrProvince(string StateOrProvince);
    [DllImport("Mespro.dll")]
    private static extern int SetOrganization(string Organization);
    [DllImport("Mespro.dll")]
    private static extern int SetOrganizationalUnit(string OrganizationalUnit);
    [DllImport("Mespro.dll")]
    private static extern int SetTitle(string Title);
    [DllImport("Mespro.dll")]
    private static extern int SetCommonName(string CommonName);
    [DllImport("Mespro.dll")]
    private static extern int SetEmailAddress(string EmailAddress);
    [DllImport("Mespro.dll")]
    private static extern int PKCS7Final();
    [DllImport("Mespro.dll")]
    private static extern int PKCS7Init(string Pse_path, int Reserved);
    [DllImport("Mespro.dll")]
    private static extern int PSE31_Generation(string Pse_path, int Reserv1, string reserv2, int Flags);
    [DllImport("Mespro.dll")]
    private static extern int SetNewKeysAlgorithm(string Algor);
    [DllImport("Mespro.dll")]
    private static extern void ClearBuffer(string Inp);
    [DllImport("Mespro.dll")]
    private static extern void FreeBuffer(void* Ptr);
    [DllImport("Mespro.dll")]
    private static extern int NewKeysGenerationEx(string Pse_Path, string Reserv, string KeyFile, 
                                                  string Password, string ReqFile);
    [DllImport("Mespro.dll")]
    private static extern int GetCertificateInfoBufferEx(byte[] Buf, int Len, ref CERTIFICATE_INFO Info);
    [DllImport("Mespro.dll")]
    private static extern string GetCertPublicKeyAlgorithmBuffer(byte[] Buf, int Len);
    [DllImport("Mespro.dll")]
    private static extern void FreeCertificateInfo(ref CERTIFICATE_INFO Info);
    [DllImport("Mespro.dll")]
    private static extern int GetRequestInfoBuffer(string Buf, int Len, ref CERTIFICATE_REQ_INFO Info);
    [DllImport("Mespro.dll")]
    private static extern void FreeRequestInfo(ref CERTIFICATE_REQ_INFO Info);
    [DllImport("Mespro.dll")]
    private static extern int AddPSEPrivateKeyFromBufferEx(string Pse_Path, string Rezerv, byte[] Buf, int Len,
                                                           string Password);
    [DllImport("Mespro.dll")]
    private static extern int AddCAs(string CAdir);
    [DllImport("Mespro.dll")]
    private static extern void* GetSignCTX();
    [DllImport("Mespro.dll")]
    private static extern int AddSigner(void* Ctx, int XType, byte[] Param1, ref int Param2);
    [DllImport("Mespro.dll")]
    private static extern int AddSigner(void* Ctx, int XType, string Param1);
    [DllImport("Mespro.dll")]
    private static extern void FreeSignCTX(void* Ctx);
    [DllImport("Mespro.dll")]
    private static extern int InsertCertificateToSign(int Insert);
    [DllImport("Mespro.dll")]
    private static extern int InsertSigningTimeToSign(int Insert);
    [DllImport("Mespro.dll")]
    private static extern int SignBufferEx(void* Ctx, byte[] InBuf, int InLen, ref IntPtr OutBuf, ref int OutLen, int Detach);
    [DllImport("Mespro.dll")]
    private static extern int CheckBufferSignEx(void* Ctx, byte[] InBuf, int InLen, void** OutBuf, int* OutLen,
                                                int SignDel, byte[] DetachedData, int DetachedLen);
    [DllImport("Mespro.dll")]
    private static extern int GetSignatureCount(void* Ctx);
    [DllImport("Mespro.dll")]
    private static extern int GetSignatureStatus(void* Ctx, int Ind);
    [DllImport("Mespro.dll")]
    private static extern string GetSignatureSubject(void* Ctx, int Ind);
    [DllImport("Mespro.dll")]
    private static extern int GetSignatureCertInBuffer(void* Ctx, int Ind, ref IntPtr Buf, ref int Len);
    [DllImport("Mespro.dll")]
    private static extern String GetSignatureTime(void* Ctx, int Ind);
    [DllImport("Mespro.dll")]
    private static extern int SetDigestAlgorithm(string Digest);
    [DllImport("Mespro.dll")]
    private static extern int SignFileEx(void* Ctk, string InFile, string out_file, int Detach);
    [DllImport("Mespro.dll")]
    private static extern int SetCertificateVerifyFlags(UInt32 Flags);
    [DllImport("Mespro.dll")]
    private static extern int CheckFileSignEx(void* Ctx, string InFile, string OutFile, int SignDel, string DetFile);    
    
    private const string iniOptions = "options";
    private const string iniCertificate = "certificate";

    public static string UserDir = "";
    public static string KeyDir = "";
    public static string RndDir = "";
    public static string CAsDir = "";
    public static string LibKey = "";

    public static string Country = "";
    public static string StateOrProvince = "";
    public static string Locality = "";
    public static string Organization = "";
    public static string Title = "";
    public static string EmailAddress = "";

    public static string SecretKeyFile = "";
    public static string KeysAlgorithm = "";
    public static string PublicKeyFile = "";
    public static string RequestFile = "";
    public static string CertificateFile = "";
    public static string IniFile = "";         
    
    private const string ERR_LOAD_INI = "ERR_LOAD_INI: Ini-файл не может быть загружен.";
    private const string ERR_CRYPT_LIB = "ERR_CRYPT_LIB: Нераспознанная ошибка криптографической библиотеки.";
    
    private const int BY_FILE = 0;
    private const int BY_BUFFER = 4;
    private const int CERT_HAS_EXPIRED_IGNORE = 0x2;

    private const string FileName_kek_opq = "kek.opq";
    private const string FileName_masks_db3 = "masks.db3";
    private const string FileName_mk_db3    = "mk.db3";
    private const string FileName_rand_opq  = "rand.opq";
    
    private static void ExecSQL(string SQL)
    {
        SqlConnection conn = new SqlConnection();
        conn.ConnectionString = "Context Connection=true";
        SqlCommand cmd = new SqlCommand();
        cmd.Connection = conn;
        cmd.CommandText = SQL;
        conn.Open();
        SqlDataReader rdr = cmd.ExecuteReader();
        SqlContext.Pipe.Send(rdr);
        rdr.Close();
        conn.Close();
    }

    private static string FormatError(string Name, int Code)
    {
        return String.Format("{0} ({1}, Code = {2})", ERR_CRYPT_LIB, Name, Code);
    }

    private static void CheckPath (ref string Val)
    {           
        if ((Val.Length > 1) && !(Val[1].Equals('\\')) && !(Val[1].Equals(':')))
        {
            Val = Environment.SystemDirectory + @"\" + Val;
        }
    }

    private static void SaveFile(string FileName, SqlBinary Data)
    {
        string DirName = Path.GetDirectoryName(FileName);
        if (!Directory.Exists(DirName))
        {
            Directory.CreateDirectory(DirName);
        }

        BinaryWriter binaryWriter = new BinaryWriter(File.Create(FileName));
        try
        {
            binaryWriter.Write(Data.Value);
        }
        finally
        {
            binaryWriter.Close();
        }
    }

    private static bool ReadIni(bool isShortKey)
    {
        String iniFileName = "xp_SignalCom.ini";
        IniFile Ini = new IniFile(iniFileName);        
        UserDir = Ini.ReadValue(iniOptions, "UserDir");
        CheckPath(ref UserDir);
        KeyDir = Ini.ReadValue(iniOptions, "KeyDir");
        CheckPath(ref KeyDir);
        RndDir = Ini.ReadValue(iniOptions, "RndDir");
        CheckPath(ref RndDir);
        CAsDir = Ini.ReadValue(iniOptions, "CAsDir");
        CheckPath(ref CAsDir);                        
        IniFile = Ini.ReadValue(iniOptions, "IniFile");
        CheckPath(ref IniFile);
        
        LibKey = Ini.ReadValue(iniOptions, "LibKey");
        SecretKeyFile = Ini.ReadValue(iniOptions, "SecretKeyFile");
        KeysAlgorithm = Ini.ReadValue(iniOptions, "KeysAlgorithm");
        PublicKeyFile = Ini.ReadValue(iniOptions, "PublicKeyFile");
        RequestFile = Ini.ReadValue(iniOptions, "RequestFile");
        CertificateFile = Ini.ReadValue(iniOptions, "CertificateFile");

        Country = Ini.ReadValue(iniCertificate, "Country");
        Locality = Ini.ReadValue(iniCertificate, "Locality");
        Organization = Ini.ReadValue(iniCertificate, "Organization");
        Title = Ini.ReadValue(iniCertificate, "Title");
        EmailAddress = Ini.ReadValue(iniCertificate, "EmailAddress");
        if (isShortKey == true)
        {
            StateOrProvince = "RUTOKEN";
        }
        else
        {
            StateOrProvince = Ini.ReadValue(iniCertificate, "StateOrProvince");
        }
        return (((KeysAlgorithm.Length > 0) && (SecretKeyFile.Length > 0) && (PublicKeyFile.Length > 0) && (CertificateFile.Length > 0)));
    }    
    
    [SqlProcedure]
    public static int CreateKeys(SqlString OrganizationUnit, SqlString CommonName, SqlString TitleName, SqlString Email, bool isShortKey)
    {
        int Code = -1;
        if (ReadIni(isShortKey))
        {
            string Dir = UserDir + @"\#" + String.Format("{0:yyyyMMddHHmmssfff}", DateTime.Now) + @"\";
            ClearBuffer(LibKey);
            try
            {                                
                Code = PKCS7Init(RndDir, 0);
                if (Code != 0) { throw new Exception(FormatError("PKCS7Init", Code)); };
                Code = PSE31_Generation(Dir, 0, "", 0);
                if (Code != 0) { throw new Exception(FormatError("PSE31_Generation", Code)); }
                Code = SetCountry(Country);
                if (Code != 0) { throw new Exception(FormatError("SetCountry", Code)); }
                Code = SetStateOrProvince(StateOrProvince);
                if (Code != 0) { throw new Exception(FormatError("SetStateOrProvince", Code)); }
                Code = SetLocality(Locality);
                if (Code != 0) { throw new Exception(FormatError("SetLocality", Code)); }
                Code = SetOrganization(Organization);
                if (Code != 0) { throw new Exception(FormatError("SetOrganization", Code)); }
                Code = SetOrganizationalUnit(OrganizationUnit.ToString());
                if (Code != 0) { throw new Exception(FormatError("SetOrganizationalUnit", Code)); }
                Code = SetCommonName(CommonName.ToString());
                if (Code != 0) { throw new Exception(FormatError("SetCommonName", Code)); }
                string sTmp = TitleName.IsNull ? Title : TitleName.ToString();
                Code = SetTitle(sTmp);
                if (Code != 0) { throw new Exception(FormatError("SetTitle", Code)); }
                sTmp = Email.IsNull ? EmailAddress : Email.ToString();
                Code = SetEmailAddress(sTmp);
                if (Code != 0) { throw new Exception(FormatError("SetEmailAddress", Code)); }
                Code = SetNewKeysAlgorithm(KeysAlgorithm);
                if (Code != 0) { throw new Exception(FormatError("SetNewKeysAlgotithm", Code)); }
                Code = NewKeysGenerationEx(Dir, null, Dir + SecretKeyFile, null, Dir + RequestFile);                              
                if (Code != 0) { throw new Exception(FormatError("NewKeysGenerationEx", Code)); }
            }
            finally
            {
                PKCS7Final();
                string SQL = "";
                string[] files = Directory.GetFiles(Dir);
                for (int i = 0; i < files.Length; i++)
                {                    
                    if (i > 0) { SQL += " UNION ALL "; }
                    SQL += String.Format("SELECT FileName='{0}',FileSource=[File].[Read]('{1}')", 
                        Path.GetFileName(files[i]), files[i]);
                }
                ExecSQL(SQL);
                for (int i = 0; i < files.Length; i++)
                {
                    File.Delete(Dir + Path.GetFileName(files[i]));
                }
                Directory.Delete(Dir);                
            };            
        }
        else
        {
              throw new Exception(ERR_LOAD_INI);            
        }
        return Code;         
    }


    public static int SignCheck(SqlBinary Data, SqlBinary Sign, out SqlBinary Cert, out SqlString SignDate, SqlBoolean IgnoreExpired)
    {
        Cert = null;
        SignDate = null;
        int Code = -1;
        void* Ptr = null;
        if (ReadIni(false))
        {
            ClearBuffer(LibKey);
            try
            {                
                Code = PKCS7Init(RndDir, 0);
                if (Code != 0) { throw new Exception(FormatError("PKCS7Init", Code)); }
                Code = AddCAs(CAsDir);
                if (Code != 0) { throw new Exception(FormatError("AddCAs", Code)); }
                Ptr = GetSignCTX();
                if (Ptr == null) { throw new Exception(ERR_CRYPT_LIB + " (GetSignCTX)"); }                
                if (!Cert.IsNull)
                {                    
                    Code = AddSigner(Ptr, BY_BUFFER, Cert.ToString()); 
                    if (Code != 0) { throw new Exception(FormatError("AddSigner", Code)); }
                };
                if (IgnoreExpired == true)
                    Code = SetCertificateVerifyFlags(CERT_HAS_EXPIRED_IGNORE);
                Code = CheckBufferSignEx(Ptr, Sign.Value, Sign.Length, null, null, 0, Data.Value, Data.Length);
                if (Code != 0) { throw new Exception(FormatError("CheckBufferSignEx", Code)); }
                int Count = GetSignatureCount(Ptr);
                if (Count == 1)
                {
                    Code = GetSignatureStatus(Ptr, 0);
                    if (Code != 0) { throw new Exception(FormatError("GetSignatureStatus", Code)); }
                    IntPtr sCert = IntPtr.Zero;
                    int LenCert = 0;
                    Code = GetSignatureCertInBuffer(Ptr, 0, ref sCert, ref LenCert);
                    if (Code != 0) { throw new Exception(FormatError("GetSignatureCertInBuffer", Code)); }
                    byte[] bCert = new byte[LenCert];
                    Marshal.Copy(sCert, bCert, 0, LenCert);
                    Cert = bCert;
                }
                SignDate = GetSignatureTime(Ptr, 0);
            }
            finally
            {
                if (Ptr != null)
                {
                    FreeSignCTX(Ptr);
                }
                PKCS7Final();
            }
        }
        else
        {
            throw new Exception(ERR_LOAD_INI);
        }
        return Code;        
    }

    public static int Sign(SqlBinary Data, out SqlBinary Sign, SqlBinary Key, SqlBinary Cert,
         SqlBinary kek_opq, SqlBinary masks_db3, SqlBinary mk_db3, SqlBinary rand_opq)    
    {
        int Code = -1;
        Sign = null;
        void* Ptr = null;
        if (ReadIni(false))
        {
            string Dir = KeyDir + @"\#" + String.Format("{0:yyyyMMddHHmmssfff}", DateTime.Now) + @"\";
            SaveFile(Dir + FileName_kek_opq, kek_opq);
            SaveFile(Dir + FileName_masks_db3, masks_db3);
            SaveFile(Dir + FileName_mk_db3, mk_db3);
            SaveFile(Dir + FileName_rand_opq, rand_opq);

            ClearBuffer(LibKey) ;
            try
            {
                Code = PKCS7Init(RndDir, 0);
                if (Code != 0) { throw new Exception(FormatError("PKCS7Init", Code)); }
                Code = AddPSEPrivateKeyFromBufferEx(Dir, null, Key.Value, Key.Length, null);
                if (Code != 0) { throw new Exception(FormatError("AddPSEPrivateKeyFromBufferEx", Code)); }
                Code = AddCAs(CAsDir);
                if (Code != 0) { throw new Exception(FormatError("AddCAs", Code)); }
                Ptr = GetSignCTX();
                if (Ptr == null) { throw new Exception(ERR_CRYPT_LIB + " (GetSignCTX)"); }
                int LenCert = Cert.Length;
                Code = AddSigner(Ptr, BY_BUFFER, Cert.Value, ref LenCert);
                if (Code != 0) { throw new Exception(FormatError("AddSigner", Code)); }
                Code = InsertSigningTimeToSign(1);
                if (Code != 0) { throw new Exception(FormatError("InsertSigningTimeToSign", Code)); }
                Code = InsertCertificateToSign(1);
                if (Code != 0) { throw new Exception(FormatError("InsertCertificateToSign", Code)); }
                int LenSign = 0;
                IntPtr sSign = IntPtr.Zero;
                Code = SignBufferEx(Ptr, Data.Value, Data.Length, ref sSign, ref LenSign, 1);
                if (Code != 0) { throw new Exception(FormatError("SignBufferEx", Code)); }
                byte[] bSign = new byte[LenSign];
                Marshal.Copy(sSign, bSign, 0, LenSign);
                Sign = bSign;
            }
            finally
            {
                if (Ptr != null)
                {
                    FreeSignCTX(Ptr);
                }                 
                PKCS7Final();
                string[] files = Directory.GetFiles(Dir);
                for (int i = 0; i < files.Length; i++)
                {
                    File.Delete(Dir + Path.GetFileName(files[i]));
                }
                Directory.Delete(Dir); 
            }

        }
        else
        {
            throw new Exception(ERR_LOAD_INI);
        }
        return Code;
    }

    public static int SignFile(SqlString KeyFile, SqlString CertFile, SqlString CAPath,
                                SqlString SignFile, bool isInt)
    {
        int Code = -1;
        if (ReadIni(false))
        {

            string ext;
            if (isInt)
            {
                ext = ".sga";
            }
            else
            {
                ext = ".sgn";
            }

            void* Ptr = null;
            ClearBuffer(LibKey);
            try
            {
                Code = PKCS7Init(null, 0);
                if (Code != 0) { throw new Exception(FormatError("PKCS7Init", Code)); }
                Code = SetDigestAlgorithm("RUS-HASH");
                if (Code != 0) { throw new Exception(FormatError("SetDigestAlgorithm", Code)); }
                Code = InsertCertificateToSign(1);
                if (Code != 0) { throw new Exception(FormatError("InsertCertificateToSign", Code)); }
                Code = InsertSigningTimeToSign(1);
                if (Code != 0) { throw new Exception(FormatError("InsertSigningTimeToSign", Code)); }
                byte[] bKey = File.ReadAllBytes(KeyFile.ToString());
                Code = AddPSEPrivateKeyFromBufferEx(Path.GetDirectoryName(KeyFile.ToString()), null, bKey, bKey.Length, null);
                if (Code != 0) { throw new Exception(FormatError("AddPSEPrivateKeyFromBufferEx", Code)); }
                Code = AddCAs(CAPath.ToString());
                if (Code != 0) { throw new Exception(FormatError("AddCAs", Code)); }
                Ptr = GetSignCTX();
                if (Ptr == null) { throw new Exception(ERR_CRYPT_LIB + " (GetSignCTX)"); }
                Code = AddSigner(Ptr, BY_FILE, CertFile.ToString());
                if (Code != 0) { throw new Exception(FormatError("AddSigner", Code)); }
                Code = SignFileEx(Ptr, SignFile.ToString(), SignFile.ToString() + ext, 1);
                if (Code != 0) { throw new Exception(FormatError("SignFileEx", Code)); }
            }
            finally
            {
                if (Ptr != null)
                {
                    FreeSignCTX(Ptr);
                }
                PKCS7Final();
            }
        }
        else
        {
            throw new Exception(ERR_LOAD_INI);
        }
        return Code;
    }

    public static int CheckSignFile(SqlString CAPath, SqlString SignFile, SqlString OrigFile, bool IgnoreVerify,
                                    out SqlString Time, out int ClientId)
    {
        int Code = -1;
        if (ReadIni(false))
        {            
            void* Ptr = null;
            ClearBuffer(LibKey);
            try
            {
                Code = PKCS7Init(null, 0);
                if (Code != 0) { throw new Exception(FormatError("PKCS7Init", Code)); }
                Code = AddCAs(CAPath.ToString());
                if (Code != 0) { throw new Exception(FormatError("AddCAs", Code)); }
                Ptr = GetSignCTX();
                if (Ptr == null) { throw new Exception(ERR_CRYPT_LIB + " (GetSignCTX)"); }
                if (!IgnoreVerify)
                {
                    Code = SetCertificateVerifyFlags(CERT_HAS_EXPIRED_IGNORE);
                    if (Code != 0) { throw new Exception(FormatError("SetCertificateVerifyFlags", Code)); }
                }                
                Code = CheckFileSignEx(Ptr, SignFile.ToString(), null, 1, OrigFile.ToString());
                if (Code != 0) { throw new Exception(FormatError("CheckFileSignEx", Code)); }
                
                int Status = GetSignatureStatus(Ptr, 0);
                Time = GetSignatureTime(Ptr, 0);
                string Subj = GetSignatureSubject(Ptr, 0);                
                int IdStart = Subj.IndexOf(@"/OU=", 0);
                int IdEnd = Subj.IndexOf(@"/", IdStart + 2);
                
                ClientId = int.Parse(Subj.Substring(IdStart + 4, IdEnd - IdStart - 4));
            }
            finally
            {
                if (Ptr != null)
                {
                    FreeSignCTX(Ptr);
                }
                PKCS7Final();
            }
        }
        else
        {
            throw new Exception(ERR_LOAD_INI);
        }
        return Code;
    }

    private static object _lockObkect = new object();

    public static int ReadCertificate(SqlBinary Cert, out SqlString KeysAlgorithm, out SqlString Serial,
                                      out SqlString DateBegin, out SqlString DateEnd, 
                                      out SqlString OrganizationUnit, out SqlString CommonName, 
                                      out SqlString Title, out SqlString Country,
                                      out SqlString StateOrProvince, out SqlString Locality,
                                      out SqlString Organization, out SqlString EmailAddress)
    {
        int Code = -1;
        if (ReadIni(false))
        {
            lock (_lockObkect)
            {
                ClearBuffer(LibKey);
                CERTIFICATE_INFO Info = new CERTIFICATE_INFO();
                Code = GetCertificateInfoBufferEx(Cert.Value, Cert.Length, ref Info);
                if (Code != 0) { throw new Exception(FormatError("GetCertificateInfoBufferEx", Code)); }
                KeysAlgorithm = GetCertPublicKeyAlgorithmBuffer(Cert.Value, Cert.Length);
                Serial = Info.SerialNumber;
                DateBegin = Info.NotBefore;
                DateEnd = Info.NotAfter;
                OrganizationUnit = Info.Subject.OrganizationalUnit;
                CommonName = Info.Subject.CommonName;
                Title = Info.Subject.Title;
                Country = Info.Subject.Country;
                StateOrProvince = Info.Subject.StateOrProvince;
                Locality = Info.Subject.Locality;
                Organization = Info.Subject.Organization;
                EmailAddress = Info.Subject.EmailAddress;
            }
        }
        else
        {
            throw new Exception(ERR_LOAD_INI);
        }
        return Code;
    }

    public static int ReadRequest(SqlString Request, out SqlString Country, out SqlString StateOrProvince, 
        out SqlString Locality, out SqlString Organization, out SqlString OrganizationUnit, out SqlString Title,
        out SqlString CommonName, out SqlString EmailAddress)
    {
        int Code = -1;
        if (ReadIni(false))
        {            
            ClearBuffer(LibKey);
            CERTIFICATE_REQ_INFO Info = new CERTIFICATE_REQ_INFO();
            try
            {                
                Code = GetRequestInfoBuffer(Request.ToString(), Request.ToString().Length, ref Info);
                Country = Info.Subject.Country;
                StateOrProvince = Info.Subject.StateOrProvince;
                Locality = Info.Subject.Locality;
                Organization = Info.Subject.Organization;
                OrganizationUnit = Info.Subject.OrganizationalUnit;
                Title = Info.Subject.Title;
                CommonName = Info.Subject.CommonName;
                EmailAddress = Info.Subject.EmailAddress;
            }
            finally
            {
                FreeRequestInfo(ref Info);
            }
        }
        else
        {
            throw new Exception(ERR_LOAD_INI);
        }
        return Code;
    }


    private static bool IsLineBreak(char ch)
    {
        return (ch == '\n') || (ch == '\r');
    }
    
    private static void ReplaceValue(ref string Data, string FieldName, string FieldValue)
    {
        int i = Data.IndexOf(FieldName + '=');

        if ((i > 0) && ((i == 1) || (IsLineBreak(Data[i - 1]))))
        {
            i += FieldName.Length + 1;
            int j = i + 1;

            while ((j <= Data.Length) && (!IsLineBreak(Data[j])))
            {
                j++;
            }

            if (j <= Data.Length)
            {
                Data = Data.Substring(0, i) + FieldValue + Data.Substring(j);
            }
            else
            {
                Data = Data.Substring(0, i) + FieldValue;
            }
        }   
    }


    public static void CreateIni(SqlString OrganizationUnit, SqlString CommonName, SqlString ATitle, bool isShortKey)
    {
        if (ReadIni(isShortKey))
        {
            string Data = File.ReadAllText(IniFile);            

            ReplaceValue(ref Data, "KeysAlgorithm", KeysAlgorithm);
            ReplaceValue(ref Data, "KeyFile", SecretKeyFile);
            ReplaceValue(ref Data, "Country", Country);

            if (isShortKey == true)
            {
                ReplaceValue(ref Data, "StateOrProvince", "RUTOKEN");
            }
            else
            {
                ReplaceValue(ref Data, "StateOrProvince", StateOrProvince);
            }
            ReplaceValue(ref Data, "Locality", Locality);
            ReplaceValue(ref Data, "Organization", Organization);
            ReplaceValue(ref Data, "EmailAddress", EmailAddress);            
            ReplaceValue(ref Data, "Title", ATitle.IsNull ? Title : ATitle.ToString());
            ReplaceValue(ref Data, "OrganizationUnit", OrganizationUnit.ToString());
            ReplaceValue(ref Data, "CommonName", CommonName.ToString());
            
            ExecSQL(String.Format("SELECT INI='{0}'", Data));
        }
        else
        {
            throw new Exception(ERR_LOAD_INI);
        };
    }
}
