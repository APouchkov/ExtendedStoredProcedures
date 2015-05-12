USE [master]
GO
/****** Object:  StoredProcedure [SignalCom].[File::Check::Signature]    Script Date: 04/16/2009 12:17:17 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[SignalCom].[File::Check::Signature]') AND type in (N'P', N'PC'))
DROP PROCEDURE [SignalCom].[File::Check::Signature]
GO

/****** Object:  StoredProcedure [SignalCom].[File::Create::Config]    Script Date: 04/16/2009 12:17:17 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[SignalCom].[File::Create::Config]') AND type in (N'P', N'PC'))
DROP PROCEDURE [SignalCom].[File::Create::Config]
GO

/****** Object:  StoredProcedure [SignalCom].[File::Create::Keys]    Script Date: 04/16/2009 12:17:17 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[SignalCom].[File::Create::Keys]') AND type in (N'P', N'PC'))
DROP PROCEDURE [SignalCom].[File::Create::Keys]
GO

/****** Object:  StoredProcedure [SignalCom].[File::Read::Certificate]    Script Date: 04/16/2009 12:17:17 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[SignalCom].[File::Read::Certificate]') AND type in (N'P', N'PC'))
DROP PROCEDURE [SignalCom].[File::Read::Certificate]
GO

/****** Object:  StoredProcedure [SignalCom].[File::Read::Request]    Script Date: 04/16/2009 12:17:17 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[SignalCom].[File::Read::Request]') AND type in (N'P', N'PC'))
DROP PROCEDURE [SignalCom].[File::Read::Request]
GO

/****** Object:  StoredProcedure [SignalCom].[Data::Sign]    Script Date: 04/16/2009 12:17:17 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[SignalCom].[Data::Sign]') AND type in (N'P', N'PC'))
DROP PROCEDURE [SignalCom].[Data::Sign]
GO

/****** Object:  StoredProcedure [SignalCom].[Data::Check::Signature]    Script Date: 04/16/2009 12:17:17 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[SignalCom].[Data::Check::Signature]') AND type in (N'P', N'PC'))
DROP PROCEDURE [SignalCom].[Data::Check::Signature]
GO

/****** Object:  StoredProcedure [SignalCom].[File::Sign]    Script Date: 04/16/2009 12:17:17 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[SignalCom].[File::Sign]') AND type in (N'P', N'PC'))
DROP PROCEDURE [SignalCom].[File::Sign]
GO
/****** Object:  SqlAssembly [xp_SignalCom]    Script Date: 04/16/2009 12:10:40 ******/
IF  EXISTS (SELECT * FROM sys.assemblies asms WHERE asms.name = N'xpSignalCom')
DROP ASSEMBLY [xpSignalCom]
GO

/****** Object:  SqlAssembly [xp_SignalCom]    Script Date: 04/16/2009 12:10:40 ******/
CREATE ASSEMBLY [xpSignalCom]
AUTHORIZATION [dbo]
FROM 'c:\Программы\Assemblies\xpSignalCom.dll'
WITH PERMISSION_SET = UNSAFE
GO

IF  EXISTS (SELECT * FROM sys.schemas WHERE name = N'SignalCom')
DROP SCHEMA [SignalCom]
GO
CREATE SCHEMA [SignalCom] AUTHORIZATION [dbo]
GO
GRANT EXECUTE ON SCHEMA::[SignalCom] TO [BackOffice] 
GO
GRANT SELECT ON SCHEMA::[SignalCom] TO [BackOffice] 
GO

USE [master]
GO

/****** Object:  StoredProcedure [SignalCom].[Data::Check::Signature]    Script Date: 08/17/2009 12:35:44 ******/
CREATE PROCEDURE [SignalCom].[Data::Check::Signature]
	@Data [varbinary](max),
	@Sign [varbinary](4000),
	@Cert [varbinary](4000) OUTPUT,
	@SignDate [nvarchar](4000) OUTPUT,
	@IgnoreExpired [bit] = False
WITH EXECUTE AS CALLER
AS
EXTERNAL NAME [xpSignalCom].[T].[SignCheck]
GO

/****** Object:  StoredProcedure [SignalCom].[Data::Sign]    Script Date: 08/17/2009 12:35:44 ******/
CREATE PROCEDURE [SignalCom].[Data::Sign]
	@Data [varbinary](max),
	@Sign [varbinary](4000) OUTPUT,
	@Key [varbinary](4000),
	@Cert [varbinary](4000),
	@Kek [varbinary](4000),
	@Masks [varbinary](4000),
	@mk [varbinary](4000),
	@rand [varbinary](4000)
WITH EXECUTE AS CALLER
AS
EXTERNAL NAME [xpSignalCom].[T].[Sign]
GO

/****** Object:  StoredProcedure [SignalCom].[File::Check::Signature]    Script Date: 08/17/2009 12:35:44 ******/
CREATE PROCEDURE [SignalCom].[File::Check::Signature]
	@CAPAth [nvarchar](4000),
	@SignFile [nvarchar](4000),
	@OrigFile [nvarchar](4000),
	@IgnoreVerify [bit] = False,
	@Time [nvarchar](50) OUTPUT,
	@ClientId [int] OUTPUT
WITH EXECUTE AS CALLER
AS
EXTERNAL NAME [xpSignalCom].[T].[CheckSignFile]
GO

/****** Object:  StoredProcedure [SignalCom].[File::Create::Config]    Script Date: 08/17/2009 12:35:44 ******/
CREATE PROCEDURE [SignalCom].[File::Create::Config]
	@OrganizationUnit [nvarchar](4000),
	@CommonName [nvarchar](4000),
	@Title [nvarchar](4000),
	@isShortKey [bit]
WITH EXECUTE AS CALLER
AS
EXTERNAL NAME [xpSignalCom].[T].[CreateIni]
GO

/****** Object:  StoredProcedure [SignalCom].[File::Create::Keys]    Script Date: 08/17/2009 12:35:44 ******/
CREATE PROCEDURE [SignalCom].[File::Create::Keys]
	@OrganizationUnit [nvarchar](4000),
	@CommonName [nvarchar](4000),
	@Title [nvarchar](4000),
	@Email [nvarchar](4000),
	@isShortKey [bit]
WITH EXECUTE AS CALLER
AS
EXTERNAL NAME [xpSignalCom].[T].[CreateKeys]
GO

/****** Object:  StoredProcedure [SignalCom].[File::Read::Certificate]    Script Date: 08/17/2009 12:35:44 ******/
CREATE PROCEDURE [SignalCom].[File::Read::Certificate]
	@Cert [varbinary](4000),
	@KeysAlgorithm [nvarchar](4000) OUTPUT,
	@Serial [nvarchar](4000) OUTPUT,
	@DateBegin [nvarchar](4000) OUTPUT,
	@DateEnd [nvarchar](4000) OUTPUT,
	@OrganizationUnit [nvarchar](4000) OUTPUT,
	@CommonName [nvarchar](4000) OUTPUT,
	@Title [nvarchar](4000) OUTPUT,
	@Country [nvarchar](4000) OUTPUT,
	@StateOrProvince [nvarchar](4000) OUTPUT,
	@Locality [nvarchar](4000) OUTPUT,
	@Organization [nvarchar](4000) OUTPUT,
	@EmailAddress [nvarchar](4000) OUTPUT
WITH EXECUTE AS CALLER
AS
EXTERNAL NAME [xpSignalCom].[T].[ReadCertificate]
GO

/****** Object:  StoredProcedure [SignalCom].[File::Read::Request]    Script Date: 08/17/2009 12:35:45 ******/
CREATE PROCEDURE [SignalCom].[File::Read::Request]
	@Request [nvarchar](4000),
	@Country [nvarchar](4000) OUTPUT,
	@StateOrProvince [nvarchar](4000) OUTPUT,
	@Locality [nvarchar](4000) OUTPUT,
	@Organization [nvarchar](4000) OUTPUT,
	@OrganizationUnit [nvarchar](4000) OUTPUT,
	@Title [nvarchar](4000) OUTPUT,
	@CommonName [nvarchar](4000) OUTPUT,
	@EmailAddress [nvarchar](4000) OUTPUT
WITH EXECUTE AS CALLER
AS
EXTERNAL NAME [xpSignalCom].[T].[ReadRequest]
GO

/****** Object:  StoredProcedure [SignalCom].[File::Sign]    Script Date: 08/17/2009 12:35:45 ******/
CREATE PROCEDURE [SignalCom].[File::Sign]
	@KeyFile [nvarchar](4000),
	@CertFile [nvarchar](4000),
	@CAPath [nvarchar](4000),
	@SignFile [nvarchar](4000),
	@isInt [bit]
WITH EXECUTE AS CALLER
AS
EXTERNAL NAME [xpSignalCom].[T].[SignFile]
GO

