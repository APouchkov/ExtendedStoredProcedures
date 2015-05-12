using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Microsoft.SqlServer.Server;

public struct SqlUdt : IBinarySerialize
{
  private String FTypeName;
  private byte[] FBuffer;

  public String TypeName { get { return FTypeName; } }
  public byte[] Buffer { get { return FBuffer; } }

  public Object CreateUdtObject(Boolean ALoadBuffer = false)
  {
    Object LResult;
    String AssemblyName;

    //if(FTypeName.Substring(0, 5).Equals("TList") || FTypeName.Substring(0, 11).Equals("TDictionary"))
    //{
    AssemblyName = typeof(TListString).Assembly.FullName;
    //}
    //else
    //  throw new Exception(String.Format("Невозможно прочитать данные, тип UDT '{0}' не поддерживается текущей версией CLR", FTypeName));

    LResult = System.Activator.CreateInstance(AssemblyName, FTypeName).Unwrap();
      
    if(ALoadBuffer)
    {
      System.IO.BinaryReader r = new System.IO.BinaryReader(new System.IO.MemoryStream(FBuffer));
      (LResult as IBinarySerialize).Read(r);
    }

    return LResult;
  }

  public SqlUdt(String ATypeName, byte[] ABuffer)
  {
    FTypeName = ATypeName;
    FBuffer   = ABuffer;
  }

  public SqlUdt(String ATypeName, String AString)
  {
    FTypeName = ATypeName;
    if(AString == null)
      FBuffer = null;
    else
    {
      FBuffer = null;
      Object LObject = CreateUdtObject();
      LObject.GetType().InvokeMember("FromString", BindingFlags.InvokeMethod, null, LObject, new Object[]{AString});

      System.IO.MemoryStream s = new System.IO.MemoryStream();
      System.IO.BinaryWriter w = new System.IO.BinaryWriter(s);
      (LObject as IBinarySerialize).Write(w);
      FBuffer = s.ToArray();
    }
  }

  public override String ToString()
  {
    if(FBuffer == null) return null;

    Object LObject = CreateUdtObject(true);

    return LObject.ToString();
    // "FBuffer=" + FBuffer.Length.ToString() + ", TListString=" + ((TListString)LObject).Length.ToString();
  }

  public SqlUdt(System.IO.BinaryReader r)
  {
    FTypeName = null;
    FBuffer   = null;
    Read(r);
  }

  public void Read(System.IO.BinaryReader r)
  {
    FTypeName = r.ReadString();
    Int32 Len = Sql.Read7BitEncodedInt(r);
    FBuffer   = r.ReadBytes(Len);
  }

  public void Write(System.IO.BinaryWriter w)
  {
    w.Write(FTypeName);
    Sql.Write7BitEncodedInt(w, (Int32)FBuffer.Length);
    w.Write(FBuffer);
  }

  public SqlUdt(Object o)
  {
    FTypeName = o.GetType().FullName;
    System.IO.MemoryStream s = new System.IO.MemoryStream();
    System.IO.BinaryWriter w = new System.IO.BinaryWriter(s);
    (o as IBinarySerialize).Write(w);
    FBuffer   = s.ToArray();
    //throw new Exception("System = " + FTypeName + ", FBuffer.Length = " + FBuffer.Length.ToString());
  }
}
