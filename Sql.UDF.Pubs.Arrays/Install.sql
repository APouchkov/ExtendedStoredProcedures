IF NOT EXISTS (SELECT * FROM sys.assemblies asms WHERE asms.name = N'UDF.Pubs.Arrays' and is_user_defined = 1) 
  CREATE ASSEMBLY [UDF.Pubs.Arrays] 
  FROM 'E:\Programms\Extended Stored Procedures\UDF.Pubs.Arrays.dll' 
  WITH PERMISSION_SET = SAFE 
ELSE
  ALTER ASSEMBLY [UDF.Pubs.Arrays] 
  FROM 'E:\Programms\Extended Stored Procedures\UDF.Pubs.Arrays.dll' 
  WITH PERMISSION_SET = SAFE, UNCHECKED DATA 