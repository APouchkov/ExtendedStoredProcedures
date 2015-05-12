IF NOT EXISTS (SELECT * FROM sys.assemblies asms WHERE asms.name = N'UDF.GUI' and is_user_defined = 1) 
  CREATE ASSEMBLY [UDF.GUI] 
  FROM 'E:\Programms\Extended Stored Procedures\UDF.GUI.dll' 
  WITH PERMISSION_SET = SAFE 
ELSE
  ALTER ASSEMBLY [UDF.GUI] 
  FROM 'E:\Programms\Extended Stored Procedures\UDF.GUI.dll' 
  WITH PERMISSION_SET = SAFE, UNCHECKED DATA 