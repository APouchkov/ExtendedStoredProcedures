IF NOT EXISTS (SELECT * FROM sys.assemblies asms WHERE asms.name = N'UDF.TimeZones' and is_user_defined = 1) 
  CREATE ASSEMBLY [UDF.TimeZones] 
  FROM 'D:\Programms\Extended Stored Procedures\UDF.TimeZones.dll' 
  WITH PERMISSION_SET = UNSAFE 
ELSE
  ALTER ASSEMBLY [UDF.TimeZones] 
  FROM 'D:\Programms\Extended Stored Procedures\UDF.TimeZones.dll' 
  WITH PERMISSION_SET = UNSAFE, UNCHECKED DATA 