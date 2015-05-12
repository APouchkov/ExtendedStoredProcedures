
USE [BackOffice]
DROP FUNCTION [System].[SOAP::Request]
DROP ASSEMBLY [UDF.SOAPRequest]

CREATE ASSEMBLY [UDF.SOAPRequest]
FROM '\\wasp\C$\Assembly\UDF.SOAPRequest.dll'
WITH PERMISSION_SET = EXTERNAL_ACCESS;

CREATE FUNCTION [System].[SOAP::Request](@Uri NVARCHAR(1000), @Body XML)
RETURNS XML WITH EXECUTE AS CALLER
AS
EXTERNAL NAME [UDF.SOAPRequest].[Sql.UDF.SOAPRequest.SOAP].[SOAPRequest]

grant exec on [System].[SOAP::Request]  to [Sys_BackOffice]
grant exec on [System].[SOAP::Request]  to [Sys_FrontOffice]
grant exec on [System].[SOAP::Request]  to [Sys_Gate]


-- new!  SET @xml = [System].[SOAP::Request] ('http://will/WS_SMS', @xml)



--exec sp_rename '[System].[SOAP::Request]', 'SOAP::Request_OLD'
  
/*
DECLARE 
  @uri nvarchar(1000) = 'http://will/WS_SMS',
  @r xml = NULL,
  @body xml = '
    <SendSMS>
    <SenderCode>1</SenderCode>
    <retries>2</retries>
    <SMS><retries>2</retries>
    <phones><phone>79851962045</phone></phones>
    <message>Привет SQL Server assembly!</message>
    <rus>1</rus>
    </SMS></SendSMS>  
  '
SET @r = [System].[SOAP::Request](@uri, @body)
select @r
*/