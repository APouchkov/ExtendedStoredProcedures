IF NOT EXISTS (SELECT * FROM sys.assemblies asms WHERE asms.name = N'INT.TParams' and is_user_defined = 1) 
  CREATE ASSEMBLY [INT.TParams] 
  FROM 'E:\Programms\Extended Stored Procedures\INT.TParams.dll' 
  WITH PERMISSION_SET = SAFE 
ELSE
  ALTER ASSEMBLY [INT.TParams] 
  FROM 'E:\Programms\Extended Stored Procedures\INT.TParams.dll' 
  WITH PERMISSION_SET = SAFE, UNCHECKED DATA 