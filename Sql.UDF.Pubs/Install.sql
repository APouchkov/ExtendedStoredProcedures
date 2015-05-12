IF NOT EXISTS (SELECT * FROM sys.assemblies asms WHERE asms.name = N'UDF.Pubs' and is_user_defined = 1) 
  CREATE ASSEMBLY [UDF.Pubs] 
  FROM 'E:\Programms\Extended Stored Procedures\UDF.Pubs.dll' 
  WITH PERMISSION_SET = SAFE 
ELSE
  ALTER ASSEMBLY [UDF.Pubs] 
  FROM 'E:\Programms\Extended Stored Procedures\UDF.Pubs.dll' 
  WITH PERMISSION_SET = SAFE, UNCHECKED DATA 