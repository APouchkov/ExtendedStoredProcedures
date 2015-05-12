--ALTER DATABASE BackOffice_OLD SET trustworthy ON


DROP PROCEDURE [crypt].[xp_SignalCom_CreateKeys]
GO
DROP PROC [crypt].[xp_SignalCom_SignCheck]
GO
DROP PROC [crypt].[xp_SignalCom_Sign]
GO
DROP PROC [crypt].[xp_SignalCom_ReadCertificate]
GO
DROP PROC [crypt].[xp_SignalCom_SignFile]
GO
DROP PROC [crypt].[xp_SignalCom_CheckSignFile]
GO
DROP PROC [crypt].[xp_SignalCom_ReadRequest]
GO
DROP PROC [crypt].[xp_SignalCom_CreateIni]
GO
DROP ASSEMBLY xp_SignalCom
GO
CREATE ASSEMBLY xp_SignalCom FROM 'D:\Test\xp_SignalCom.dll' WITH PERMISSION_SET = UNSAFE
GO
CREATE Procedure  [crypt].[xp_SignalCom_CreateKeys]
(
@OrgUnit NVarChar(4000),
@CommonName NVarChar(4000),
@Title NVarChar(4000),
@Email NVarChar(4000)
) 
AS EXTERNAL NAME xp_SignalCom.T.CreateKeys
GO
CREATE Procedure  [crypt].[xp_SignalCom_SignCheck]
(
@Data varBinary(8000),
@Sign VarBinary(8000),
@Cert NVarChar(4000) OUT,
@SignDate NVarChar(4000) OUT
) 
AS EXTERNAL NAME xp_SignalCom.T.SignCheck
GO
CREATE PROCEDURE [crypt].[xp_SignalCom_Sign]
(
	@Data VARBINARY(8000),
	@Sign VARBINARY(8000) OUT,
	@Key VARBINARY(8000),
	@Cert VARBINARY(8000),
	@Kek VARBINARY(8000),
	@Masks VARBINARY(8000),
	@mk VARBINARY(8000),
	@rand VARBINARY(8000)	
) AS EXTERNAL NAME xp_SignalCom.T.Sign
GO
CREATE PROCEDURE [crypt].[xp_SignalCom_ReadCertificate]
(
	@Cert VARBINARY(8000),
	@KeysAlgorithm NVARCHAR(4000) OUT,
	@Serial NVARCHAR(4000) OUT,
	@DateBegin NVARCHAR(4000) OUT,
	@DateEnd NVARCHAR(4000) OUT,
	@OrganizationUnit NVARCHAR(4000) OUT,
	@CommonName NVARCHAR(4000) OUT,
	@Title NVARCHAR(4000) OUT	,
	@Country NVARCHAR(4000) = NULL OUT,
	@StateOrProvince NVARCHAR(4000) = NULL OUT,
	@Locality NVARCHAR(4000) = NULL OUT,
	@Organization NVARCHAR(4000) = NULL OUT,
	@EmailAddress NVARCHAR(4000) = NULL OUT
)
	AS EXTERNAL NAME xp_SignalCom.T.ReadCertificate
GO
CREATE PROC [crypt].[xp_SignalCom_SignFile]
(
	@KeyFile NVARCHAR(4000),
	@CertFile NVARCHAR(4000),
	@CAPath NVARCHAR(4000),
	@SignFile NVARCHAR(4000),
	@isInt BIT
)
AS EXTERNAL NAME xp_SignalCom.T.SignFile
GO
CREATE PROC [crypt].[xp_SignalCom_CheckSignFile]
(
	@CAPAth NVARCHAR(4000),
	@SignFile NVARCHAR(4000),
	@OrigFile NVARCHAR(4000),
	@IgnoreVerify BIT = 0
)
AS EXTERNAL NAME xp_SignalCom.T.CheckSignFile
GO
CREATE PROC [crypt].[xp_SignalCom_ReadRequest]
(
  @Request nVARCHAR(4000),
 	@Country NVARCHAR(4000)  OUT,
 	@StateOrProvince NVARCHAR(4000) OUT,
 	@Locality NVARCHAR(4000) OUT,
 	@Organization NVARCHAR(4000) OUT,
 	@OrganizationUnit NVARCHAR(4000) OUT,
	@Title NVARCHAR(4000) OUT	,
	@CommonName NVARCHAR(4000) OUT,
	@EmailAddress NVARCHAR(4000) OUT 
)
AS
EXTERNAL NAME xp_SignalCom.T.ReadRequest
GO
CREATE PROC [crypt].[xp_SignalCom_CreateIni]
(
	@OrganizationUnit NVARCHAR(4000),
	@CommonName NVARCHAR(4000),
	@Title NVARCHAR(4000)
)
AS
EXTERNAL NAME xp_SignalCom.T.CreateIni
--------------------------------------------------------
/*exec [crypt].xp_SignalCom_CreateKeys
  @OrgUnit='FINAM',
  @CommonName='Common', 
  @Title='Vaizer', 
  @Email = 'vaizer@finam.ru'
*/
/*GO
DECLARE @data VARBINARY(4000), @sign VARBINARY(4000)
declare @cert NVARCHAR(4000), @signDate VARCHAR(12);
SELECT @data = [File].[Read]('C:\Temp\~Test\Debug.fp3')
SELECT @sign = [File].[Read]('C:\Temp\~Test\Debug.fp3.sgn')
exec [crypt].xp_SignalCom_SignCheck
  @Data = @data,
  @Sign = @sign,
  @Cert = @Cert out,
  @SignDate = @SignDate OUT
  
  SELECT @cert, @signDate*/
----------------------------------------------------------
 
/* GO
 DECLARE @sign VARBINARY(8000)
 DECLARE @key VARBINARY(8000), @cert VARBINARY(8000),
 @kek VARBINARY(8000), @masks VARBINARY(8000), @mk VARBINARY(8000), @rand VARBINARY(8000)
 
 SET @key = [File].[Read]('C:\Temp\~Test\Тестов\secret.key')
 SET @Cert =[File].[Read]('C:\Temp\~Test\Тестов\Key#80235#.pem')
 SET @kek = [File].[Read]('C:\Temp\~Test\Тестов\kek.opq')
 SET @masks = [File].[Read]('C:\Temp\~Test\Тестов\masks.db3')
 SET @mk =[File].[Read]('C:\Temp\~Test\Тестов\mk.db3')
 SET @rand= [File].[Read]('C:\Temp\~Test\Тестов\rand.opq')
   
 
 EXEC [crypt].xp_SignalCom_Sign
   @Data = 0x56234875223095287324572345908273409587234095872344,
   @sign = @sign out,
   @Key = @key,
   @Cert = @cert,
   @kek =  @kek,
   @masks = @masks,
   @mk = @mk,
   @rand= @rand  

SELECT @sign   
*/
----------------------------------------------------------------------------------
GO
/*
DECLARE @Cert VARBINARY(8000),
@KeysAlgorithm NVARCHAR(4000), @Serial NVARCHAR(4000),
@DateBegin NVARCHAR(4000), @DateEnd NVARCHAR(4000), 
@OrganizationUnit NVARCHAR(4000),@CommonName NVARCHAR(4000), @Title NVARCHAR(4000),
@Country VARCHAR(4000), @StateOrProvince NVARCHAR(4000), @Locality NVARCHAR(4000),
@Organization NVARCHAR(4000), @EmailAddress NVARCHAR(4000)

SET @Cert = [File].[Read]('C:\Temp\~Test\Тестов\Key#80235#.pem')
EXEC [crypt].xp_SignalCom_ReadCertificate
  @Cert = @Cert,
  @KeysAlgorithm = @KeysAlgorithm OUT,
  @Serial = @Serial OUT,
  @DateBegin  = @DateBegin OUT,
  @DateEnd = @DateEnd OUT,
  @OrganizationUnit = @OrganizationUnit OUT,
  @CommonName = @CommonName OUT,
  @Title = @Title OUT,
  @Country = @Country OUT,
  @StateOrProvince = @StateOrProvince OUT,
  @Locality = @Locality OUT,
  @Organization = @Organization OUT,
  @EmailAddress = @EmailAddress OUT
  
  
SELECT  @KeysAlgorithm, @Serial, @DateBegin, @DateEnd, @OrganizationUnit, @CommonName, 
        @Title,@Country, @StateOrProvince, @Locality, @Organization, @EmailAddress
*/        

/*EXEC [crypt].[xp_SignalCom_SignFile]
@KeyFile = 'C:\Temp\~Test\Тестов\secret.key',
@CertFile = 'C:\Temp\~Test\Тестов\Key#80235#.pem',
@CAPath = 'C:\Temp\~Test\CAs',
@SignFile = 'C:\Temp\~Test\ini.ini',
@isInt = 0
*/
/*
EXEC [crypt].[xp_SignalCom_CheckSignFile]
  @CAPath = 'C:\Temp\~Test\CAs',
  @SignFile = 'C:\Temp\~Test\Debug.fp3.sgn',
  @OrigFile = 'C:\Temp\~Test\Debug.fp3',
  @IgnoreVerify = 0
*/  

/*
DECLARE @Request VARCHAR(8000)

DECLARE @OrganizationUnit NVARCHAR(4000),@CommonName NVARCHAR(4000), @Title NVARCHAR(4000),
@Country VARCHAR(4000), @StateOrProvince NVARCHAR(4000), @Locality NVARCHAR(4000),
@Organization NVARCHAR(4000), @EmailAddress NVARCHAR(4000)

SET @Request =
'
Certificate Request:
    Data:
        Version: 0 (0x0)
        Subject:                 C=RU, L=Москва, O=FINAM, OU=FINAM, T=Vaizer, CN=Common, Email=vaizer@finam.ru
        Subject Public Key Info:
            Public Key Algorithm: ecr3410 (1.3.6.1.4.1.5849.1.6.2)
            ECGOST Public Key:
                pub: 
                    04:50:28:36:87:17:87:b4:01:f5:a5:09:4a:e2:fb:
                    db:a9:a5:37:9e:03:6d:04:4f:46:5a:b6:a2:a0:8f:
                    ee:78:17:5b:4e:c4:96:8c:91:82:60:aa:80:4c:d5:
                    8b:8f:79:fd:79:42:68:4d:06:48:7d:cb:ae:8a:88:
                    89:3d:85:1f:b2
                prime:  (256 bits)
                    00:ff:ff:ff:ff:00:00:00:01:00:00:00:00:00:00:
                    00:00:00:00:00:00:ff:ff:ff:ff:ff:ff:ff:ff:ff:
                    ff:ff:ff
                a:  (256 bits)
                    00:ff:ff:ff:ff:00:00:00:01:00:00:00:00:00:00:
                    00:00:00:00:00:00:ff:ff:ff:ff:ff:ff:ff:ff:ff:
                    ff:ff:fc
                b:  (255 bits)
                    5a:c6:35:d8:aa:3a:93:e7:b3:eb:bd:55:76:98:86:
                    bc:65:1d:06:b0:cc:53:b0:f6:3b:ce:3c:3e:27:d2:
                    60:4b
                base: 
                    04:6b:17:d1:f2:e1:2c:42:47:f8:bc:e6:e5:63:a4:
                    40:f2:77:03:7d:81:2d:eb:33:a0:f4:a1:39:45:d8:
                    98:c2:96:4f:e3:42:e2:fe:1a:7f:9b:8e:e7:eb:4a:
                    7c:0f:9e:16:2b:ce:33:57:6b:31:5e:ce:cb:b6:40:
                    68:37:bf:51:f5
                order:  (256 bits)
                    00:ff:ff:ff:ff:00:00:00:00:ff:ff:ff:ff:ff:ff:
                    ff:ff:bc:e6:fa:ad:a7:17:9e:84:f3:b9:ca:c2:fc:
                    63:25:51
                cofactor:  (1 bit)
                    01
        Attributes:
            a0:00
    Signature Algorithm: ecr3410WithR3411 (1.3.6.1.4.1.5849.1.3.2)
        30:46:02:21:00:fa:c2:0f:29:a9:f2:52:d3:45:54:c2:ff:b9:
        c7:7b:f5:af:3f:50:36:3b:2b:c4:b1:28:55:8d:f4:21:ff:bc:
        ab:02:21:00:a5:06:9f:47:0f:37:af:55:97:04:ad:a4:bf:8b:
        10:71:a7:7a:9a:93:a5:d5:80:ed:a8:3f:5c:66:b5:13:39:33
-----BEGIN CERTIFICATE REQUEST-----
MIICJDCCAcUCAQAwgYAxCzAJBgNVBAYTAlJVMQ8wDQYDVQQHFAbM7vHq4uAxDjAM
BgNVBAoTBUZJTkFNMQ4wDAYDVQQLEwVGSU5BTTEPMA0GA1UEDBMGVmFpemVyMQ8w
DQYDVQQDEwZDb21tb24xHjAcBgkqhkiG9w0BCQEWD3ZhaXplckBmaW5hbS5ydTCC
ATkwgfAGCisGAQQBrVkBBgIwgeECAQEwLAYHKoZIzj0BAQIhAP////8AAAABAAAA
AAAAAAAAAAAA////////////////MEUCIQD/////AAAAAQAAAAAAAAAAAAAAAP//
/////////////AIgWsY12Ko6k+ez671VdpiGvGUdBrDMU7D2O848PifSYEsEQQRr
F9Hy4SxCR/i85uVjpEDydwN9gS3rM6D0oTlF2JjClk/jQuL+Gn+bjufrSnwPnhYr
zjNXazFezsu2QGg3v1H1AiEA/////wAAAAD//////////7zm+q2nF56E87nKwvxj
JVECAQEDRAAEQQRQKDaHF4e0AfWlCUri+9uppTeeA20ET0ZatqKgj+54F1tOxJaM
kYJgqoBM1YuPef15QmhNBkh9y66KiIk9hR+yoAAwDgYKKwYBBAGtWQEDAgUAA0kA
MEYCIQD6wg8pqfJS00VUwv+5x3v1rz9QNjsrxLEoVY30If+8qwIhAKUGn0cPN69V
lwStpL+LEHGnepqTpdWA7ag/XGa1Ezkz
-----END CERTIFICATE REQUEST-----
'

EXEC [crypt].[xp_SignalCom_ReadRequest]  
  @Request = @Request,
  @Country = @Country OUT,
  @StateOrProvince = @StateOrProvince OUT,
  @Locality = @Locality OUT,
  @Organization = @Organization OUT,
  @OrganizationUnit = @OrganizationUnit OUT,
  @Title = @Title OUT,
  @CommonName = @CommonName OUT,    
  @EmailAddress = @EmailAddress OUT
  
  
SELECT  @Country, @StateOrProvince, @Locality, @Organization, @OrganizationUnit, @Title,
        @CommonName,  @EmailAddress
*/

/*
EXEC [crypt].[xp_SignalCom_CreateIni]
  @OrganizationUnit = 'MyOrgUnit',
  @CommonName = 'MyCommonName',
  @Title = 'MyTitle'
*/

GO

