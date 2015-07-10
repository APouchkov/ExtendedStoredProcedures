using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class Pub
{
  public static void Write7BitEncodedInt(System.IO.BinaryWriter w, Int32 IValue)  
  {  
    UInt32 num    = (UInt32)IValue;  
  
    while (num >= 128U)  
    {  
      w.Write((byte) (num | 128U));  
      num >>= 7;
    }  
  
    w.Write((byte) num);
  }
  
  public static Int32 Read7BitEncodedInt(System.IO.BinaryReader r)  
  {  
    // some names have been changed to protect the readability  
    Int32 returnValue = 0;  
    Int32 bitIndex    = 0;  
  
    while (bitIndex != 35)  
    {  
      byte currentByte = r.ReadByte();  
      returnValue |= ((Int32) currentByte & (Int32) sbyte.MaxValue) << bitIndex;  
      bitIndex += 7;  
  
      if (((Int32) currentByte & 128) == 0)  
        return returnValue;  
    }  
  
    throw new Exception("Wrong System.IO.BinaryReader.Read7BitEncodedInt");
  }
}

// <TYPE>(1) <SIZE>{1-5} <DATA>

[Serializable]
[SqlUserDefinedAggregate(Format.UserDefined, IsInvariantToNulls = false, IsInvariantToDuplicates = false, IsInvariantToOrder = false, MaxByteSize = -1)]
public class TPrivilegesObjectAggregate: IBinarySerialize
{
  private Dictionary<Char, System.IO.BinaryWriter> OResult;

  public void Init()
  {
    OResult = new Dictionary<Char, System.IO.BinaryWriter>();
  }

  public void Accumulate(Char AType, Int32 AValue)
  {
    System.IO.BinaryWriter w;
    if(!OResult.TryGetValue(AType, out w))
    {
      System.IO.MemoryStream s = new System.IO.MemoryStream();
      w = new System.IO.BinaryWriter(s);
		  OResult.Add(AType, w);
    }
    Pub.Write7BitEncodedInt(w, AValue);
  }

  public void Merge(TPrivilegesObjectAggregate AOther)
  {
    foreach(KeyValuePair<Char, System.IO.BinaryWriter> LKeyPair in AOther.OResult)
    { 
      System.IO.BinaryReader r = new System.IO.BinaryReader(LKeyPair.Value.BaseStream);
      System.IO.BinaryWriter w;

      if(!OResult.TryGetValue(LKeyPair.Key, out w))
      {
        System.IO.MemoryStream s = new System.IO.MemoryStream();
        w = new System.IO.BinaryWriter(s);
		    OResult.Add(LKeyPair.Key, w);
      }
      w.Write(((System.IO.MemoryStream)LKeyPair.Value.BaseStream).ToArray());
    }
  }

  public SqlBytes Terminate()
  {
    if (OResult == null || OResult.Count == 0)
      return SqlBytes.Null;

    System.IO.MemoryStream s = new System.IO.MemoryStream();
    System.IO.BinaryWriter w = new System.IO.BinaryWriter(s);
    Write(w);
    return new SqlBytes(s);
  }

  public void Write(System.IO.BinaryWriter w)
  {
    foreach(KeyValuePair<Char, System.IO.BinaryWriter> LKeyPair in OResult)
    {
      System.IO.MemoryStream s = (System.IO.MemoryStream)(LKeyPair.Value.BaseStream);

      w.Write(LKeyPair.Key);
      Pub.Write7BitEncodedInt(w, (Int32)(s.Length));
      w.Write(s.ToArray());
    }
  }

  public void Read(System.IO.BinaryReader r)
  {
    Init();

    Char  LType;
    Int32 LLength;

    while(r.BaseStream.Position < r.BaseStream.Length)
    {
      System.IO.MemoryStream s = new System.IO.MemoryStream();
      System.IO.BinaryWriter w = new System.IO.BinaryWriter(s);

      LType   = r.ReadChar();  
      LLength = Pub.Read7BitEncodedInt(r);
      w.Write(r.ReadBytes(LLength));

      OResult.Add(LType, w);
    }
  }
}

////////////////////////////////////////////////////////////////
// <TYPE>(1) <SIZE>{1-5} <ROLES>
// <SIZE>{1-5} <OBJECT_ID>{1-5} <PRIVILEGES> | <SIZE>{1-5} <OBJECT_ID>{1-5} <PRIVILEGES>


[Serializable]
[SqlUserDefinedType(Microsoft.SqlServer.Server.Format.UserDefined, IsByteOrdered = true, MaxByteSize = -1, Name = "TPrivileges")]
public struct TPrivileges: INullable, IBinarySerialize
{
  public Dictionary<Int32, Byte[]> FData;

  public TPrivileges(Boolean AInit = false)
  {
    if(AInit)
      FData = new Dictionary<Int32, Byte[]>();
    else
      FData = null;
  }

  public static TPrivileges Null
  {
    get
    {
      return new TPrivileges(false);
    }
  }
  public static TPrivileges New()
  {
    return new TPrivileges(true);
  }

  public override string ToString()
  {
    StringBuilder LResult = new StringBuilder();

    foreach(KeyValuePair<Int32, Byte[]> LKeyPair in FData)
    {
      if(LResult.Length > 0)
        LResult.Append(';');  

      System.IO.MemoryStream s = new System.IO.MemoryStream(LKeyPair.Value);
      System.IO.BinaryReader r = new System.IO.BinaryReader(s);

      while(s.Position < s.Length)
      {
        if(s.Position > 0)
          LResult.Append('|');

        Char  LPrivilege = r.ReadChar();
        Int32 LEndPos    = Pub.Read7BitEncodedInt(r) + (Int32)s.Position;

        for(Boolean LNeedDelim = false; s.Position < LEndPos;)
        { 
          if(LNeedDelim)
            LResult.Append(',');
          else
            LNeedDelim = true;
          LResult.Append(Pub.Read7BitEncodedInt(r));
        }
      }
    }

    return LResult.ToString();
  }

  [
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Src"),
    SqlMethod(Name = "Parse", OnNullCall = false, DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)
  ]
  public static TPrivileges Parse(SqlString AString)
  {
    // НЕ РЕАЛИЗОВАННО !!!!!!
    return new TPrivileges(false);
  }

  public bool IsNull
  {
    get
    {
      // НЕ РЕАЛИЗОВАННО !!!!!!
      return (FData == null);
    }
  }

  public void Write(System.IO.BinaryWriter w)
  {
    System.IO.Stream  LStream = w.BaseStream;

    foreach(KeyValuePair<Int32, Byte[]> LKeyPair in FData)
    {
      Pub.Write7BitEncodedInt(w, LKeyPair.Key);           // ObjectId
      Pub.Write7BitEncodedInt(w, LKeyPair.Value.Length);  // ItemLength
      w.Write(LKeyPair.Value);                            // Privileges
    }
  }

  public void Read(System.IO.BinaryReader r)
  {
    Int32   LObjectId;
    Byte[]  LPrivileges;

    System.IO.Stream  LStream = r.BaseStream;
    Int32             LLength = (Int32)(LStream.Length);
    Int16             LItemLength;

    FData = new Dictionary<Int32, Byte[]>();
    while(LStream.Position < LLength)
    {
      LObjectId   = Pub.Read7BitEncodedInt(r);
      LItemLength = (Int16)Pub.Read7BitEncodedInt(r);
      LPrivileges = r.ReadBytes(LItemLength); 

      FData.Add(LObjectId, LPrivileges);
    }
  }

  [SqlMethod(Name = "Prepare", OnNullCall = false, DataAccess = DataAccessKind.Read, SystemDataAccess = SystemDataAccessKind.Read, IsMutator = true)]
  public void Prepare(String AObject, String AKinds)
  {
    SqlConnection LContextConnection = new SqlConnection("context connection=true");
    LContextConnection.Open();

    SqlCommand LCommand = LContextConnection.CreateCommand();
    LCommand.CommandType = CommandType.Text;
    //LCommand.CommandText = "SELECT [TPrivileges].[Prepare]('" + AObject.Replace("'", "''") + "', '" + AKinds.Replace("'", "''") + "')";
    LCommand.CommandText = "SELECT [TPrivileges].[Prepare]('" + AObject.Replace("'", "''") + "')";

    object LBytes = LCommand.ExecuteScalar();

    if(LBytes is Byte[])
    {
      System.IO.MemoryStream s = new System.IO.MemoryStream((Byte[])LBytes);
      System.IO.BinaryReader r = new System.IO.BinaryReader(s);

      System.IO.MemoryStream t = new System.IO.MemoryStream();
      System.IO.BinaryWriter w = new System.IO.BinaryWriter(t);

      Int16 LObjectId = (Int16)Pub.Read7BitEncodedInt(r);
      FData.Remove(LObjectId);

      // <TYPE>(1) <LENGTH>{1-5} <ROLES>
      while(s.Position < s.Length)
      {
        Char    LType   = r.ReadChar();
        Boolean LNeeded = (AKinds.IndexOf(LType) >= 0);
        Int32   LLength = Pub.Read7BitEncodedInt(r);
        if(LNeeded) 
        {
          w.Write(LType);
          Pub.Write7BitEncodedInt(w, LLength);
          w.Write(r.ReadBytes(LLength));
        }
        else
          s.Seek(LLength, System.IO.SeekOrigin.Current);

        //for(Int32 LCount = Pub.Read7BitEncodedInt(r); LCount > 0; LCount--)
        //{
        //  Int32 LValue = Pub.Read7BitEncodedInt(r);
        //  if(LNeeded) 
        //    Pub.Write7BitEncodedInt(w, LValue);
        //}
      }

      //FData.Add(LObjectId, r.ReadBytes((Int32)(r.BaseStream.Length - 2)));
      if(t.Length > 0)
        FData.Add(LObjectId, t.ToArray());
    }
  }

  public Boolean FindPrivileges(Byte[] APrivileges, Char APrivilege, System.IO.BinaryReader r, out Int32 ALength)
  {
    System.IO.Stream s = r.BaseStream;

    ALength = 0;
    while(s.Position < s.Length)
    {
      Char LPrivilege = r.ReadChar();
      ALength = Pub.Read7BitEncodedInt(r);
      if(LPrivilege == APrivilege)
        return true;

      s.Seek(ALength, System.IO.SeekOrigin.Current);
    }

    return false;
  }

  [SqlMethod(Name = "Exists", OnNullCall = false, DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
  public Boolean Exists(Int16 AObjectId, Char APrivilege)
  {
    Byte[] LPrivileges;

    if(!FData.TryGetValue(AObjectId, out LPrivileges))
      return false;

    System.IO.MemoryStream s = new System.IO.MemoryStream(LPrivileges);
    System.IO.BinaryReader r = new System.IO.BinaryReader(s);
    Int32 LRolesLength;

    return FindPrivileges(LPrivileges, APrivilege, r, out LRolesLength);
  }

  [SqlMethod(Name = "Contains", OnNullCall = false, DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
  public Boolean Contains(Int16 AObjectId, Char APrivilege, Int32 AValue)
  {
    Byte[] LPrivileges;
    if (!FData.TryGetValue(AObjectId, out LPrivileges))
      return false;

    System.IO.MemoryStream s = new System.IO.MemoryStream(LPrivileges);
    System.IO.BinaryReader r = new System.IO.BinaryReader(s);
    Int32 LEndPos;

    if(!FindPrivileges(LPrivileges, APrivilege, r, out LEndPos))
      return false;

    LEndPos += (Int32)s.Position;
    while(s.Position < LEndPos)
      if(Pub.Read7BitEncodedInt(r) == AValue) return true;

    return false;
  }
}
