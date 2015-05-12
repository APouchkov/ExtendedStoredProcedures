------------------------------------------------------
--	Testing assembly and extended stored procedures	--
--	for xpSignalCom.dll															--
------------------------------------------------------
--	Script created by Oleg Vaynshteyn at 2009-04-16	--
------------------------------------------------------
--	For test using person of Тестов Тест Тестович		--
--	Organization Unit is 23711
------------------------------------------------------
USE [master]
GO
SET NOCOUNT ON
GO
------------------------------------------------------
--	Test for creating Ini-File											--
------------------------------------------------------
--	standard Ini																		--
------------------------------------------------------
DECLARE
	@RC int

PRINT 'Creating Standard Ini-File'
exec @RC = [SignalCom].[File::Create::Config]
	@OrganizationUnit = 23711
, @CommonName = 'Тестов Тест Тестович'
, @Title = 'Клиент "КИС "Финам"'
, @isShortKey = 0

if @RC = 0 PRINT 'Result of creating Standard Ini-File - Succeed'
ELSE PRINT 'Result of creating Standard Ini-File - Failed'
PRINT '-----------------------------------------------------------------------'
GO
------------------------------------------------------
--	Ini for RUTOKEN																	--
------------------------------------------------------
DECLARE
	@RC int

PRINT 'Creating Ini-File for RUTOKEN'
exec @RC =[SignalCom].[File::Create::Config]
	@OrganizationUnit = 23711
, @CommonName = 'Тестов Тест Тестович'
, @Title = 'Клиент "КИС "Финам"'
, @isShortKey = 1

if @RC = 0 PRINT 'Result of creating Ini-File for RUTOKEN - Succeed'
ELSE PRINT 'Result of creating Ini-File for RUTOKEN - Failed'
PRINT '-----------------------------------------------------------------------'
GO
------------------------------------------------------
--	Test for creating Keys													--
------------------------------------------------------
--	standard Keys																		--
------------------------------------------------------
DECLARE
	@RC int
PRINT 'Creating Standard Keys'	
exec @RC = [SignalCom].[File::Create::Keys]
	@OrganizationUnit = 23711
, @CommonName = 'Тестов Тест Тестович'
, @Title = 'Клиент "КИС "Финам"'
, @Email = null
, @isShortKey = 0
if @RC = 0 PRINT 'Result of creating Standard Keys - Succeed'
ELSE PRINT 'Result of creating Standard Keys - Failed'
PRINT '-----------------------------------------------------------------------'
GO
------------------------------------------------------
--	Keys for RUTOKEN																--
------------------------------------------------------
DECLARE
	@RC int
PRINT 'Creating Keys for RUTOKEN'	
exec @RC = [SignalCom].[File::Create::Keys]
	@OrganizationUnit = 23711
, @CommonName = 'Тестов Тест Тестович'
, @Title = 'Клиент "КИС "Финам"'
, @Email = null
, @isShortKey = 1
if @RC = 0 PRINT 'Result of creating Keys for RUTOKEN - Succeed'
ELSE PRINT 'Result of creating Keys for RUTOKEN - Failed'
PRINT '-----------------------------------------------------------------------'
GO
------------------------------------------------------
--	Test for Cheking sign of file										--
------------------------------------------------------
PRINT 'Cheking sign of file'	
DECLARE 
	@Time varchar(50)
,	@ClientID int	
,	@RC int

exec @RC = [SignalCom].[File::Check::Signature]
	@CAPAth = 'C:\Keys\Nkeys'
, @SignFile = 'C:\TEMP\Test\Брокерский отчет за период 01.04.2009 - 01.04.2009 (17799).fp3.sga'
, @OrigFile = 'C:\TEMP\Test\Брокерский отчет за период 01.04.2009 - 01.04.2009 (17799).fp3'
, @IgnoreVerify = 0
,	@Time = @Time out
,	@ClientID = @ClientID out
IF @RC = 0 begin
	PRINT 'Result of cheking sign of file - Succeed'
	PRINT @Time
	PRINT @ClientID
end
PRINT '-----------------------------------------------------------------------'
GO
------------------------------------------------------
--	Test for reading certificate										--
------------------------------------------------------
DECLARE
	@RC int
, @KeysAlgorithm varchar(max)
, @Serial varchar(max)
, @DateBegin varchar(max)
, @DateEnd varchar(max)
, @OrganizationUnit varchar(max)
, @CommonName varchar(max)
, @Title varchar(max)
, @Country varchar(max)
, @StateOrProvince varchar(max)
, @Locality varchar(max)
, @Organization varchar(max)
, @EmailAddress varchar(max)
PRINT 'Reading certificate'
EXEC @RC = [SignalCom].[File::Read::Certificate]
	@Cert = 0x2D2D2D2D2D424547494E2043455254494649434154452D2D2D2D2D0D0A4D4949435354434341657967417749424167494B41625943434B774242414A437744414F42676F72426745454161315A41514D4342514177474445574D4251470D0A413155454178514E5157527461573566513046664D6A41784D544165467730774F4445794D6A49784E444D324D7A5661467730774F5445794D6A49784E444D320D0A4D7A56614D4947384D517377435159445651514745774A53565445564D424D47413155454278344D424277455067524242446F454D6751774D513477444159440D0A5651514B45775647535535425454454F4D4177474131554543784D464D544D774E6A6B784C7A417442674E56424177654A675161424473454F415131424430450D0A5167416741434945476751594243454149414169424351454F41513942444145504141694D5555775177594456515144486A774548775177424541455251512B0D0A42447745506751794143414545675137424441454E415134424545454F7751774244494149415151424430454D415243424434454F77524D424455454D6751340D0A42456377586A415742676F72426745454161315A4151594342676771686B6A4F50514D4242774E4541415242424965726E4772714F624A74613349326B5853460D0A70427773554B78545A44726E506E4B345A79744C3968684657564948327971596E4F343854366B656D7634467A4B55516843454367344A73427669336E53346E0D0A5646576A634442754D43414741315564446745422F77515742425450786D6D432B5762524842546F312F79475731665A655332785254424B42674E5648534D420D0A416638455144412B6742544676706F6B784A7048364571744459596976794E4E625451713061456370426F77474445574D425147413155454178514E515752740D0A61573566513046664D6A41784D5949494162594241514545415145774467594B4B775942424147745751454441675541413063414D45514349417538343573530D0A4B723244595934575439385275484B6773355866344739787951452F72426647336838574169415666457862474F7232467931726744624D41536935652F52320D0A494262314C324B64693866686D73632B6F413D3D0D0A2D2D2D2D2D454E442043455254494649434154452D2D2D2D2D0D0A
, @KeysAlgorithm = @KeysAlgorithm out
, @Serial = @Serial out
, @DateBegin = @DateBegin out
, @DateEnd = @DateEnd out
, @OrganizationUnit = @OrganizationUnit out
, @CommonName = @CommonName out
, @Title = @Title out
, @Country = @Country out
, @StateOrProvince = @StateOrProvince out
, @Locality = @Locality out
, @Organization = @Organization out
, @EmailAddress = @EmailAddress out
if @RC = 0 BEGIN
	PRINT 'Result of reading certificate - Succeed'
	PRINT	'KeysAlgorithm = ' + @KeysAlgorithm 
	PRINT	'Serial = ' + @Serial 
	PRINT	'DateBegin = ' + @DateBegin 
	PRINT	'DateEnd = ' + @DateEnd 
	PRINT	'OrganizationUnit = ' + @OrganizationUnit 
	PRINT	'CommonName = ' + @CommonName 
	PRINT	'Title = ' + @Title 
	PRINT	'Country = ' + @Country 
	PRINT	'StateOrProvince = ' + @StateOrProvince 
	PRINT	'Locality = ' + @Locality 
	PRINT	'Organization = ' + @Organization 
	PRINT	'EmailAddress = ' + @EmailAddress 
END	
ELSE PRINT 'Result of reading certificate - Failed'
PRINT '-----------------------------------------------------------------------'
GO
------------------------------------------------------
--	Test for reading request												--
------------------------------------------------------
DECLARE
	@RC int
,	@Request varchar(max)	
, @Country varchar(max)
, @StateOrProvince varchar(max)
, @Locality varchar(max)
, @Organization varchar(max)
, @OrganizationUnit varchar(max)
, @Title varchar(max)
, @CommonName varchar(max)
, @EmailAddress varchar(max)
PRINT 'Reading Request'
set @Request = [File].[Read] ('C:\TEMP\Test\request.req')
EXEC @RC = [SignalCom].[File::Read::Request]
	@Request = @Request
, @OrganizationUnit = @OrganizationUnit out
, @CommonName = @CommonName out
, @Title = @Title out
, @Country = @Country out
, @StateOrProvince = @StateOrProvince out
, @Locality = @Locality out
, @Organization = @Organization out
, @EmailAddress = @EmailAddress out
if @RC = 0 BEGIN
	PRINT 'Result of reading Request - Succeed'
	PRINT	'OrganizationUnit = ' + @OrganizationUnit 
	PRINT	'CommonName = ' + @CommonName 
	PRINT	'Title = ' + @Title 
	PRINT	'Country = ' + @Country 
	PRINT	'StateOrProvince = ' + @StateOrProvince 
	PRINT	'Locality = ' + @Locality 
	PRINT	'Organization = ' + @Organization 
	PRINT	'EmailAddress = ' + @EmailAddress 
END	
ELSE PRINT 'Result of reading Request - Failed. Code = ' + cast(@RC as varchar)
PRINT '-----------------------------------------------------------------------'
GO
------------------------------------------------------
--	Test for sign data															--
------------------------------------------------------
DECLARE
	@RC int
,	@Data varbinary(8000)
, @Sign varbinary(8000)
, @Key varbinary(8000)
, @Cert varbinary(8000)
, @Kek varbinary(8000)
, @Masks varbinary(8000)
, @mk varbinary(8000)
, @rand varbinary(8000)
PRINT 'Signing data'
SELECT
	@Data  = [File].[Read] ('C:\Temp\Test\Debug.fp3')
, @Key	 = [File].[Read] ('C:\Keys\Nkeys\secret.key')
, @Cert  = [File].[Read] ('C:\Keys\Nkeys\Key#13069#.pem')
, @Kek	 = [File].[Read] ('C:\Keys\Nkeys\kek.opq')
, @Masks = [File].[Read] ('C:\Keys\Nkeys\masks.db3')
, @mk		 = [File].[Read] ('C:\Keys\Nkeys\mk.db3')
, @rand	 = [File].[Read] ('C:\Keys\Nkeys\rand.opq')
EXEC @RC = [SignalCom].[Data::Sign]
	@Data = @Data
, @Sign = @Sign out
, @Key	= @Key
, @Cert	= @Cert
, @Kek	= @Kek
, @Masks= @Masks 
, @mk		= @mk
, @rand	= @rand
if @RC = 0 BEGIN
	exec [File].[Write] 
		@FileName = 'C:\Temp\Test\Debug.fp3.sga'
	, @Text = @Sign
	, @CheckPath = 1
	PRINT 'Result of signing data - Succeed'
	select [Data] = @Data
	select [Sign] = @Sign
END	
ELSE PRINT 'Result of signing data - Failed. Code = ' + cast(@RC as varchar)
PRINT '-----------------------------------------------------------------------'
GO
------------------------------------------------------
--	Test for check sign of data										  --
------------------------------------------------------
DECLARE
	@RC int
,	@Data varbinary(8000)
, @Sign varbinary(8000)
, @Cert varchar(4000)
,	@SignDate varchar(4000)

SELECT
	@Data = [File].[Read] ('C:\Temp\Test\Debug.fp3')
, @Sign = [File].[Read] ('C:\Temp\Test\Debug.fp3.sga')

PRINT 'Checking sign of data'
EXEC @RC = [SignalCom].[Data::Check::Signature]
	@Data = @Data
, @Sign = @Sign
, @Cert = @Cert out
, @SignDate = @SignDate out
if @RC = 0 BEGIN
	PRINT 'Result of checking sign of data - Succeed'
	PRINT @Cert
	PRINT @SignDate
	select @Sign
END	
ELSE PRINT 'Result of checking sign of data - Failed. Code = ' + cast(@RC as varchar)
PRINT '-----------------------------------------------------------------------'
GO
------------------------------------------------------
--	Test for sign file															--
------------------------------------------------------
DECLARE
	@RC int
,	@KeyFile varchar(400)
, @CertFile varchar(400)
, @CAPath varchar(400)
, @SignFile varchar(400)
, @isInt bit

SELECT
	@KeyFile = 'C:\Keys\Nkeys\secret.key'
, @CertFile = 'C:\Keys\Nkeys\Key#13069#.pem'
, @CAPath = 'C:\Keys\Nkeys\'
, @SignFile = 'C:\TEMP\Test\Брокерский отчет за период 01.04.2009 - 01.04.2009 (17799).fp3'
, @isInt = 1

PRINT 'Signing file'
EXEC @RC = [SignalCom].[File::Sign]
	@KeyFile = @KeyFile
, @CertFile = @CertFile
, @CAPath = @CAPath
, @SignFile = @SignFile
, @isInt = @isInt
IF @RC = 0 PRINT 'Result of signing file - Succeed'
ELSE PRINT 'Result of signing file - Failed. Code = ' + cast(@RC as varchar)	
PRINT '-----------------------------------------------------------------------'
GO