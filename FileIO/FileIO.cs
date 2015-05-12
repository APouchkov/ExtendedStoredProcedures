using Microsoft.SqlServer.Server;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.IO;
using System.Data;
using System;
using System.Collections;
using System.Collections.Generic;

public class FileIO
{
    [Microsoft.SqlServer.Server.SqlProcedure]  
    public static SqlBytes FileRead(string FileName)
    {
        FileStream fs;
        fs = File.OpenRead(FileName);
        BinaryReader binaryReader = new BinaryReader(fs);
        byte[] Data;
        try
        {            
           Data = binaryReader.ReadBytes((int)fs.Length);           
        }
        finally
        {
            binaryReader.Close();
        }
        
        return (new SqlBytes(Data));
    }
    
    public static void FileWrite(string FileName, SqlBytes Data, bool ForceDirectory)
    {      
        string DirName = Path.GetDirectoryName(FileName);
        if (ForceDirectory && !Directory.Exists(DirName))
        {
            Directory.CreateDirectory(DirName);
        }

        BinaryWriter binaryWriter = new BinaryWriter(File.Create(FileName));
        try
        {
            binaryWriter.Write(Data.Value);
        }
        finally
        {
            binaryWriter.Close();
        }
    }

    public static void FileMove(string FileName, string NewFileName)
    {
        File.Move(FileName, NewFileName);
    }

    public static void FileCopy(string SourceFileName, string DestFileName, bool Overwrite)
    {
        File.Copy(SourceFileName, DestFileName, Overwrite);
    }

    public static void FileDelete(string FileName)
    {
        File.Delete(FileName);
    }

    public static SqlBoolean FileExists(string FileName)
    {
        return (File.Exists(FileName));
    }
    
    public static void CreateDirectory(string DirName)
    {
        Directory.CreateDirectory(DirName);
    }

    [SqlFunction(FillRowMethodName = "FillFileInfoRow")]
    public static IEnumerable FileInfo(String FileName)
    {
        List<string> names = new List<string>();
        names.Add(FileName);
        return names;
    }

    public static void FillFileInfoRow(object FileName, out long Length, out DateTime CreateTime, 
        out DateTime LastAccessTime, out DateTime LastWriteTime, out string Attributes)
    {
        FileInfo fi = new FileInfo((string) FileName);
        Length = fi.Length;
        CreateTime = fi.CreationTime;
        LastAccessTime = fi.LastAccessTime;
        LastWriteTime = fi.LastWriteTime;
        Attributes = fi.Attributes.ToString().Replace(" ", "");
    }

    public struct FileList
    {
        public string FileName;
        public bool IsDirectory;
    };

    [SqlFunction(FillRowMethodName = "FileListRow")]
    public static IEnumerable GetFileList(string DirName, string Pattern)
    {        
        string[] dirs = Directory.GetDirectories(DirName, Pattern);
        string[] files = Directory.GetFiles(DirName, Pattern);
        FileList[] fl = new FileList[dirs.Length + files.Length];
        for (int i = 0; i < dirs.Length; i++)
        {            
            fl[i].FileName = dirs[i];
            fl[i].IsDirectory = true;
        };
        for (int i = dirs.Length; i < dirs.Length + files.Length; i++)
        {
            fl[i].FileName = files[i - dirs.Length];
            fl[i].IsDirectory = false;
        };
        return fl;
    }

    public static void FileListRow(object FList, out string FileName, out long Length, out bool IsDirectory,
        out DateTime CreateTime, out DateTime LastAccessTime, out DateTime LastWriteTime, out string Attributes)
    {
        FileList FL = (FileList)FList;
        FileName = FL.FileName;
        IsDirectory = FL.IsDirectory;
        if (IsDirectory)
        {
            DirectoryInfo di = new DirectoryInfo(FileName);
            CreateTime = di.CreationTime;
            LastAccessTime = di.LastAccessTime;
            LastWriteTime = di.LastWriteTime;
            Attributes = di.Attributes.ToString().Replace(" ", "");
            Length = 0;
        }
        else
        {
            FillFileInfoRow(FileName, out Length, out CreateTime, out LastAccessTime, 
                out LastWriteTime, out Attributes);
        }
    }

   
}
