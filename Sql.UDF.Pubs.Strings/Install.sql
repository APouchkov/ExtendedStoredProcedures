IF NOT EXISTS (SELECT * FROM sys.assemblies asms WHERE asms.name = N'UDF.Pubs.Strings' and is_user_defined = 1) 
  CREATE ASSEMBLY [UDF.Pubs.Strings] 
  FROM 'E:\Programms\Extended Stored Procedures\UDF.Pubs.Strings.dll' 
  WITH PERMISSION_SET = SAFE 
ELSE
  ALTER ASSEMBLY [UDF.Pubs.Strings] 
  FROM 'E:\Programms\Extended Stored Procedures\UDF.Pubs.Strings.dll' 
  WITH PERMISSION_SET = SAFE, UNCHECKED DATA 