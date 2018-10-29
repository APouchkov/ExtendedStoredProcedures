using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using System.Text;

//using System.Xml;
//using System.Xml.Schema;
//using System.Xml.Serialization;

[Serializable]
[SqlUserDefinedType(Microsoft.SqlServer.Server.Format.UserDefined, IsByteOrdered = true, MaxByteSize = -1, Name = "TListVariant")]
public class TListVariant: IBinarySerialize/*, IXmlSerializable*/, INullable
{
  public List<Object> FList = new List<Object>();

  public override string ToString()
  {
    int LCount = FList.Count;
    if(LCount == 0)
      return "";

    StringBuilder LResult = new StringBuilder(LCount);
    for(int LIndex = 0; LIndex < LCount; LIndex++)
    { 
      if(LIndex > 0)
        LResult.Append(',');
      Object LValue = FList[LIndex];
      SqlDbType LSqlDbType = Sql.GetSqlType(LValue);
      LResult.Append(LSqlDbType == SqlDbType.NVarChar ? "" : LSqlDbType.ToString());
      LResult.Append('(');
        LResult.Append(Sql.ValueToString(LValue, Sql.ValueDbStyle.SQL).Replace(")", "))"));
      LResult.Append(')');
    }

    return LResult.ToString();
  }

  public static TListVariant Null { get { return new TListVariant(); } }
  public static TListVariant New() { return Null; }

  public bool IsNull { get { return false; } }

  public void FromString(String AString)
  {
    if (String.IsNullOrEmpty(AString)) return;

    foreach (Object LObject in Arrays.SplitString(AString, ',', new char[] {'('}))
    {
      String LItem = (String)LObject;
      if(String.IsNullOrEmpty(LItem))
        continue;

      String LType;
      int LIndex = LItem.IndexOf('(');
      if(LIndex >= 0 && LItem[LItem.Length - 1] == ')')
      {
        LType = LItem.Substring(0, LIndex);
        LItem = LItem.Substring(LIndex + 1, LItem.Length - LIndex - 2);
      }
      else
        LType = "NVarChar";

      SqlDbType LSqlDbType = Sql.TypeFromString(LType);
      FList.Add(Sql.ValueFromString(LItem, LSqlDbType, Sql.ValueDbStyle.SQL));

      //if (LSqlDbType == SqlDbType.Udt)
      //  return new SqlUdt(AType, AValue);
      //else
      //  return Sql.ValueFromString(AValue, LSqlDbType, Sql.ValueDbStyle.SQL);
    }
  }

  [
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Src"),
    SqlMethod(Name = "Parse", OnNullCall = false, DataAccess = DataAccessKind.None, IsDeterministic = true)
  ]
  public static TListVariant Parse(SqlString AString)
  {
    if (AString.IsNull) return null;

    TListVariant LResult = new TListVariant();
    LResult.FromString(AString.Value);

    return LResult;
  }

  [SqlMethod(Name = "Add", OnNullCall = false, DataAccess = DataAccessKind.None, IsMutator = true)]
  public void Add(Object AValue) { FList.Add(AValue); }

  public int Length { get { return FList.Count; } }

  [SqlMethod(Name = "Values", OnNullCall = false, DataAccess = DataAccessKind.None, IsDeterministic = true)]
  public Object Values(int AIndex) { return FList[AIndex - 1]; }

  [SqlMethod(Name = "Contains", OnNullCall = false, DataAccess = DataAccessKind.None, IsDeterministic = true)]
  public Boolean Contains(Object AValue) { return FList.Contains(AValue); }

  [SqlMethod(Name = "Contains All", OnNullCall = false, DataAccess = DataAccessKind.None, IsDeterministic = true)]
  public Boolean ContainsAll(TListVariant AValue)
  {
    if(AValue == null || AValue.FList.Count == 0)
      return true;
    for(int i = AValue.FList.Count - 1; i >= 0; i--)
      if(!FList.Contains(AValue.FList[i]))
        return false;
    return true;
  }

  public bool Equals(TListVariant AList)
  {
    if(AList == null || FList.Count != AList.FList.Count)
      return false;

    for(int i = FList.Count - 1; i >= 0; i--)
      if(FList[i] != AList.FList[i])
        return false;

    return true;
  }

  //[SqlMethod(Name = "ToCompressedBinary", OnNullCall = false, DataAccess = DataAccessKind.None, IsDeterministic = true)]
  //public SqlBytes ToCompressedBinary()
  //{
  //  if(FList == null || FList.Count == 0)
  //    return null;

  //  System.IO.MemoryStream s = new System.IO.MemoryStream();
  //  System.IO.BinaryWriter w = new System.IO.BinaryWriter(s);

  //  int LCount = FList.Count;
  //  Sql.Write7BitEncodedInt(w, LCount);

  //  for(int LIndex = 0; LIndex < LCount; LIndex++)
  //    Sql.Write7BitEncodedInt64(w, FList[LIndex]);

  //  return new SqlBytes(s);
  //}

  //[
  //  System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Src"),
  //  SqlMethod(Name = "FromCompressedBinary", OnNullCall = false, DataAccess = DataAccessKind.None, IsDeterministic = true)
  //]
  //public static TListVariant FromCompressedBinary(SqlBytes AData)
  //{
  //  if (AData.IsNull) return TListVariant.Null;

  //  TListVariant LResult = new TListVariant();
  //  System.IO.BinaryReader r = new System.IO.BinaryReader(AData.Stream);

  //  int LCount = Sql.Read7BitEncodedInt(r);

  //  LResult.FList.Capacity = LCount;
  //  for(; LCount > 0; LCount--)
  //    LResult.FList.Add(Sql.Read7BitEncodedInt64(r));

  //  return LResult;
  //}


  public void Read(System.IO.BinaryReader r)
  {
#if DEBUG
    int LCount = r.ReadInt32();
#else    
    int LCount = Sql.Read7BitEncodedInt(r);
#endif

    FList.Capacity = LCount;
    for(; LCount > 0; LCount--)
    {
      //String    name  = r.ReadString();
      SqlDbType LType = (SqlDbType)r.ReadUInt16();
      Object    value = null;
      Int32 len;
      //Int32 lcid;
      //SqlCompareOptions co;

      switch (LType)
      {
        case SqlDbType.Bit      : value = new SqlBoolean(r.ReadBoolean()); break;
        case SqlDbType.TinyInt  : value = new SqlByte(r.ReadByte()); break;
        case SqlDbType.SmallInt : value = new SqlInt16((Int16)r.ReadInt16()); break;
        case SqlDbType.Int      : value = new SqlInt32((Int32)r.ReadInt32()); break;
        case SqlDbType.BigInt   : value = new SqlInt64(r.ReadInt64()); break;

        case SqlDbType.Binary   :
        case SqlDbType.VarBinary: len = r.ReadUInt16(); value = new SqlBytes(r.ReadBytes(len)); break;

        case SqlDbType.Char     :
        case SqlDbType.VarChar  : //value = new Sql.SqlAnsiString(r); break;
        case SqlDbType.NChar:
        case SqlDbType.NVarChar:
          //co = (SqlCompareOptions)r.ReadUInt16();
          //lcid = r.ReadInt32();
          //value = new SqlString(r.ReadString(), lcid, co);
          value = new SqlString(r.ReadString());
          break;

        case SqlDbType.DateTime     : value = new SqlDateTime(DateTime.FromBinary(r.ReadInt64())); break;
        case SqlDbType.SmallDateTime:
        case SqlDbType.Date         :
        case SqlDbType.DateTime2    : value = DateTime.FromBinary(r.ReadInt64()); break;
        case SqlDbType.Time         : value = TimeSpan.FromTicks(r.ReadInt64()); break;
        case SqlDbType.DateTimeOffset:
          DateTime LDateTime = DateTime.FromBinary(r.ReadInt64());
          value = new DateTimeOffset(LDateTime, TimeSpan.FromTicks(r.ReadInt64()));
          break;

        case SqlDbType.Decimal: value = new SqlDecimal(r.ReadDecimal()); break;
        case SqlDbType.Float  : value = new SqlDouble(r.ReadDouble()); break;
        // Not support SqlDbType.Image
        case SqlDbType.Money  : value = new SqlMoney(r.ReadDecimal()); break;
        case SqlDbType.Real   : value = new SqlSingle(r.ReadDouble()); break;
        case SqlDbType.SmallMoney: value = new SqlMoney(r.ReadDecimal()); break;
        // Not support SqlDbType.Structured
        // Not support SqlDbType.Text
        // Not support SqlDbType.Timestamp
        case SqlDbType.UniqueIdentifier: value = new SqlGuid(r.ReadString()); break;
        // Not support SqlDbType.Variant
        //case SqlDbType.Xml:
        //  XmlReader rXml = XmlReader.Create(new System.IO.StringReader(r.ReadString()));
        //  value = new SqlXml(rXml);
        //  break;

        //case SqlDbType.Udt:
          // TODO: Пока поддержа только TParams
          //String LTypeName = r.ReadString();
          //value = CreateUdtObject(LTypeName);
          //if (value is IBinarySerialize)
          //  (value as IBinarySerialize).Read(r);
          //else
          //  throw new Exception(String.Format("Невозможно прочитать данные типа UDT '{0}' - не поддерживается IBinarySerialize", LTypeName));
          //value = new SqlUdt(r);
          //break;

        default:
          throw new Exception(String.Format("Невозможно прочитать данные, тип '{0}' не поддерживается текущей версией {1}", LType.ToString(), this.GetType().Name));
        // Not support SqlDbType.NText
      }
      if (value != null)
        FList.Add(value);
    }
  }

  public void Write(System.IO.BinaryWriter w)
  {
    int LCount = FList.Count;
#if DEBUG
    w.Write(LCount);
#else    
    Sql.Write7BitEncodedInt(w, LCount);
#endif

    for(int LIndex = 0; LIndex < LCount; LIndex++)
    {
      Object LValue = FList[LIndex];
      SqlDbType type = Sql.GetSqlType(LValue);
      w.Write((UInt16)type);

      switch (type)
      {
        case SqlDbType.Bit      : w.Write((bool)((SqlBoolean)LValue)); break;
        case SqlDbType.TinyInt  : w.Write((Byte)((SqlByte)LValue)); break;
        case SqlDbType.SmallInt : w.Write((Int16)((SqlInt16)LValue)); break;
        case SqlDbType.Int      : w.Write((Int32)((SqlInt32)LValue).Value); break;
        case SqlDbType.BigInt   : w.Write((Int64)((SqlInt64)LValue)); break;

        case SqlDbType.Binary   :
        case SqlDbType.VarBinary: 
          if(LValue is SqlBinary)
          {
            w.Write((UInt16)((SqlBinary)LValue).Length);
            w.Write(((SqlBinary)LValue).Value, 0, (Int32)((SqlBinary)LValue).Length);
          }
          else
          {
            w.Write((UInt16)((SqlBytes)LValue).Length);
            w.Write(((SqlBytes)LValue).Buffer, 0, (Int32)((SqlBytes)LValue).Length);
          }
          break;

        case SqlDbType.Char:
        case SqlDbType.VarChar: //((Sql.SqlAnsiString)LValue).Write(w); break;
        case SqlDbType.NChar:
        case SqlDbType.NVarChar:
          SqlString NVarChar;
          if(LValue is SqlChars)
            NVarChar = ((SqlChars)LValue).ToSqlString().Value;
          else
            NVarChar = (SqlString)LValue;
          //w.Write((UInt16)NVarChar.SqlCompareOptions);
          //w.Write(NVarChar.LCID);
          w.Write(NVarChar.Value);
          break;

        case SqlDbType.DateTime     : w.Write((Int64)((DateTime)((SqlDateTime)LValue).Value).ToBinary()); break;
        case SqlDbType.SmallDateTime:
        case SqlDbType.Date         :
        case SqlDbType.DateTime2    : w.Write((Int64)((DateTime)LValue).ToBinary()); break;
        case SqlDbType.Time         : w.Write((Int64)((TimeSpan)LValue).Ticks); break;
        case SqlDbType.DateTimeOffset: 
          DateTimeOffset LDateTimeOffset = (DateTimeOffset)LValue;
          w.Write((Int64)LDateTimeOffset.DateTime.ToBinary());
          w.Write((Int64)LDateTimeOffset.Offset.Ticks);
          break;

        case SqlDbType.Decimal    : w.Write(((SqlDecimal)LValue).Value); break;
        case SqlDbType.Float      : w.Write(((SqlDouble)LValue).Value); break;
        case SqlDbType.Real       : w.Write((Double)((SqlSingle)LValue).Value); break;
        case SqlDbType.SmallMoney :
        case SqlDbType.Money      : w.Write(((SqlMoney)LValue).Value); break;

        //case SqlDbType.Udt:
        //  ((SqlUdt)LValue).Write(w);
          // TODO: Пока поддержа только TParams
          //if (LValue is IBinarySerialize)
          //{
          // // w.Write(LValue.GetType().Assembly.FullName);
          //  w.Write(LValue.GetType().FullName);
          //  (LValue as IBinarySerialize).Write(w);
          //}
          //else
          //  throw new Exception(String.Format("Невозможно записать данные, тип UDT '{0}' не поддерживается текущей версией {1}", LValue.GetType().Name, this.GetType().Name));

          //(LValue as IBinarySerialize).Write(w);
        //  break;

        case SqlDbType.UniqueIdentifier: w.Write(((Guid)((SqlGuid)LValue).Value).ToString()); break;

        case SqlDbType.Xml: w.Write(((SqlXml)LValue).Value); break;
        default:
          throw new Exception(String.Format("Невозможно записать данные, тип '{0}' не поддерживается текущей версией {1}", type.ToString(), this.GetType().Name));

        // Not support SqlDbType.Image
        // Not support SqlDbType.NText
        // Not support SqlDbType.Structured
        // Not support SqlDbType.Text
        // Not support SqlDbType.Timestamp
        // Not support SqlDbType.Variant
      }
    }
  }

  [SqlFunction(Name = "Enum(Variant)", FillRowMethodName = "EnumRow", DataAccess = DataAccessKind.None, TableDefinition = "[Value] SQL_Variant, [Index] Int", IsDeterministic = true)]
  public static IEnumerable Enum(TListVariant AList)
  {
    if(AList == null) yield break;
      
    Int32 LIndex = 0;
    foreach(Object LKey in AList.FList)
    { 
      yield return new KeyValuePair<Object, Int32>(LKey, ++LIndex);
    }
  }
  public static void EnumRow(object ARow, out Object AValue, out Int32 AIndex)
  {
    AValue = ((KeyValuePair<Object,Int32>)ARow).Key;
    AIndex = ((KeyValuePair<Object,Int32>)ARow).Value;
  }
}

[Serializable]
[SqlUserDefinedAggregate(Format.UserDefined, IsInvariantToNulls = true, IsInvariantToDuplicates = false, IsInvariantToOrder = false, MaxByteSize = -1)]
public class TListVariantAggregate: IBinarySerialize
{
  private TListVariant OResult;

  public void Init()
  {
    OResult = new TListVariant();
  }

  public void Accumulate(Object AValue)
  {
    //if (AValue.IsNull) 
    //  return;

		OResult.Add(AValue);
  }

  public void Merge(TListVariantAggregate AOther)
  {
    OResult.FList.AddRange(AOther.OResult.FList);
  }

  public TListVariant Terminate()
  {
    if (OResult != null && OResult.FList.Count > 0)
      return OResult;
    else
      return null;
  }

  public void Read(System.IO.BinaryReader r)
  {
    //if (r == null) throw new ArgumentNullException("r");
    if(OResult == null)
      OResult = new TListVariant();
    OResult.Read(r);
  }

  public void Write(System.IO.BinaryWriter w)
  {
    //if (w == null) throw new ArgumentNullException("w");
    if(OResult != null)
      OResult.Write(w);
  }
};