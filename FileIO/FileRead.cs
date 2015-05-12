using Microsoft.SqlServer.Server;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.IO;

public class T
{
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static SqlBinary FileRead(string FileName)
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
        
        return (new SqlBinary(Data));
    }
    public static void FileWrite(string FileName, SqlBinary Text)
    {
        BinaryWriter binaryWriter = new BinaryWriter(File.Create(FileName));
        try
        {
            binaryWriter.Write(Text.Value);
        }
        finally
        {
            binaryWriter.Close();
        }
    }
}
