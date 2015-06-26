using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using System.Text;

[Serializable]
[SqlUserDefinedType(Microsoft.SqlServer.Server.Format.UserDefined, IsByteOrdered = true, MaxByteSize = -1, Name = "TPrivileges")]
public struct TPrivileges: INullable, IBinarySerialize
{
  public SqlBytes Select;
  public SqlBytes Insert;
  public SqlBytes Update;
  public SqlBytes Initialize;
  public SqlBytes Delete;

  private static void Write7BitEncodedInt(System.IO.BinaryWriter w, Int32 IValue)  
  {  
    UInt32 num = (UInt32)IValue;  
  
    while (num >= 128U)  
    {  
      w.Write((byte) (num | 128U));  
      num >>= 7;  
    }  
  
    w.Write((byte) num);  
  }
  
  private static Int32 Read7BitEncodedInt(System.IO.BinaryReader r)  
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

  public void ItemToString(StringBuilder AResult,  SqlBytes AItem)
  {
    if(AResult.Length > 0)
      AResult.Append(';');

    if(!AItem.IsNull && AItem.Length > 0)
    { 
      System.IO.BinaryReader r = new System.IO.BinaryReader(new System.IO.MemoryStream(AItem.Buffer));
      int LCount = Read7BitEncodedInt(r);

      for(int LIndex = 0; LIndex < LCount; LIndex++)
      { 
        if(LIndex > 0)
          AResult.Append(',');
        AResult.Append(Read7BitEncodedInt(r));
      }
    }
  }

  public override string ToString()
  {
    StringBuilder LResult = new StringBuilder();

    ItemToString(LResult, Select);
    ItemToString(LResult, Insert);
    ItemToString(LResult, Update);
    ItemToString(LResult, Initialize);
    ItemToString(LResult, Delete);

    return LResult.ToString();
  }

  [
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Src"),
    SqlMethod(Name = "Parse", OnNullCall = false, IsDeterministic = true)
  ]
  public static TPrivileges Parse(SqlString AString)
  {
    //if (AString.IsNull) return null;

    //TPrivileges LResult = new TPrivileges();
    //LResult.FromString(AString.Value);

    return TPrivileges.Null; //LResult;
  }

  public bool IsNull
  {
    get
    {
      // Поместите здесь свой код
      return false;
    }
  }

  public static TPrivileges New()
  {
      return Null;
  }

  public static TPrivileges Create(SqlBytes ASelect, SqlBytes AInsert, SqlBytes AUpdate, SqlBytes AInitialize, SqlBytes ADelete)
  {
    TPrivileges h = new TPrivileges();
    h.Select      = ASelect;
    h.Insert      = AInsert;
    h.Update      = AUpdate;
    h.Initialize  = AInitialize;
    h.Delete      = ADelete;

    return h;
  }

  public static TPrivileges Null
  {
    get
    {
      TPrivileges h = new TPrivileges();

      h.Select      = SqlBytes.Null;
      h.Insert      = SqlBytes.Null;
      h.Update      = SqlBytes.Null;
      h.Initialize  = SqlBytes.Null;
      h.Delete      = SqlBytes.Null;

      return h;
    }
  }


  public bool ItemsEqual(SqlBytes AItem1, SqlBytes AItem2)
  {
    if(AItem1.IsNull && AItem2.IsNull)
      return true;

    if(AItem1.IsNull || AItem2.IsNull)
      return false;

    //return AItem1.Equals(AItem2);

    if(AItem1.Length != AItem2.Length)
      return false;

    for(int i = (int)(AItem1.Length) - 1; i >= 0; i--)
      if(AItem1.Buffer[i] != AItem2.Buffer[i])
        return false;

    return true;
  }

  public bool Equals(TPrivileges arg)
  {
    return ItemsEqual(Select, arg.Select) && ItemsEqual(Insert, arg.Insert) && ItemsEqual(Update, arg.Update) && ItemsEqual(Initialize, arg.Initialize) && ItemsEqual(Delete, arg.Delete);
  }

  
  [SqlFunction(Name = "TPrivileges.Is Equal", DataAccess = DataAccessKind.None, IsDeterministic = true)]
  public static bool IsEqual(TPrivileges Item1, TPrivileges Item2)
  {
    return Item1.Equals(Item2);
  }

  public void Write(System.IO.BinaryWriter w)
  {
    if(Select.IsNull)
      w.Write((Byte)0);
    else
    { 
      Write7BitEncodedInt(w, (Int32)Select.Length);
      if(Select.Length > 0)
        w.Write(Select.Buffer, 0, (Int32)(Select.Length));
    }

    if(Insert.IsNull)
      w.Write((Byte)0);
    else
    { 
      Write7BitEncodedInt(w, (Int32)Insert.Length);
      if(Insert.Length > 0)
        w.Write(Insert.Buffer, 0, (Int32)(Insert.Length));
    }

    if(Update.IsNull)
      w.Write((Byte)0);
    else
    { 
      Write7BitEncodedInt(w, (Int32)Update.Length);
      if(Update.Length > 0)
        w.Write(Update.Buffer, 0, (Int32)(Update.Length));
    }

    if(Initialize.IsNull)
      w.Write((Byte)0);
    else
    { 
      Write7BitEncodedInt(w, (Int32)Initialize.Length);
      if(Initialize.Length > 0)
        w.Write(Initialize.Buffer, 0, (Int32)(Initialize.Length));
    }

    if(Delete.IsNull)
      w.Write((Byte)0);
    else
    { 
      Write7BitEncodedInt(w, (Int32)Delete.Length);
      if(Delete.Length > 0)
        w.Write(Delete.Buffer, 0, (Int32)(Delete.Length));
    }
  }

  public void Read(System.IO.BinaryReader r)
  {
    Int32 Len;

    Len = Read7BitEncodedInt(r);
    if(Len == 0)
      Select = new SqlBytes();
    else
      Select = new SqlBytes(r.ReadBytes(Len));

    Len = Read7BitEncodedInt(r);
    if(Len == 0)
      Insert = new SqlBytes();
    else
      Insert = new SqlBytes(r.ReadBytes(Len));

    Len = Read7BitEncodedInt(r);
    if(Len == 0)
      Update = new SqlBytes();
    else
      Update = new SqlBytes(r.ReadBytes(Len));

    Len = Read7BitEncodedInt(r);
    if(Len == 0)
      Initialize = new SqlBytes();
    else
      Initialize = new SqlBytes(r.ReadBytes(Len));

    Len = Read7BitEncodedInt(r);
    if(Len == 0)
      Delete = new SqlBytes();
    else
      Delete = new SqlBytes(r.ReadBytes(Len));
  }

  [SqlMethod(Name = "Contains", OnNullCall = false, IsDeterministic = true)]
  public Boolean Contains(Byte APrivilege, Int32 AValue)
  {
    SqlBytes LData;
    switch(APrivilege)
    {
      case 1 : LData = Select; break;
      case 2 : LData = Insert; break; 
      case 4 : LData = Update; break; 
      case 16: LData = Initialize; break; 
      case 8 : LData = Delete; break; 
      default:
        return false;
    }

    if(LData.IsNull || LData.Length == 0)
      return false;

    System.IO.BinaryReader r = new System.IO.BinaryReader(new System.IO.MemoryStream(LData.Buffer));

    int LCount = Read7BitEncodedInt(r);
    for(; LCount > 0; LCount--)
      if(Read7BitEncodedInt(r) == AValue) return true;

    return false;
 }
}
