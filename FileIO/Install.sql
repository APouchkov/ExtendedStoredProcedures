IF NOT EXISTS (SELECT * FROM sys.assemblies asms WHERE asms.name = N'UDP.FileIO' and is_user_defined = 1) 
  CREATE ASSEMBLY [UDP.FileIO] 
  FROM 'D:\Programms\Extended Stored Procedures\UDP.FileIO.dll' 
  WITH PERMISSION_SET = EXTERNAL_ACCESS 
ELSE
  ALTER ASSEMBLY [UDP.FileIO] 
  FROM 'D:\Programms\Extended Stored Procedures\UDP.FileIO.dll' 
  WITH PERMISSION_SET = EXTERNAL_ACCESS, UNCHECKED DATA 