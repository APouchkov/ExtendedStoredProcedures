IF NOT EXISTS (SELECT * FROM sys.assemblies asms WHERE asms.name = N'PublicDomain.ZoneInfo' and is_user_defined = 1) 
  CREATE ASSEMBLY [PublicDomain.ZoneInfo] 
  FROM 'D:\Programms\Extended Stored Procedures\PublicDomain.ZoneInfo.dll' 
  WITH PERMISSION_SET = UNSAFE 
ELSE
  ALTER ASSEMBLY [PublicDomain.ZoneInfo] 
  FROM 'D:\Programms\Extended Stored Procedures\PublicDomain.ZoneInfo.dll' 
  WITH PERMISSION_SET = UNSAFE, UNCHECKED DATA 