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
[SqlUserDefinedType(Microsoft.SqlServer.Server.Format.UserDefined, IsByteOrdered = true, MaxByteSize = -1, Name = "TListInt8")]
public class TListInt8: IBinarySerialize/*, IXmlSerializable*/, INullable
{
  public List<Byte> FList = new List<Byte>();

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
      LResult.Append(FList[LIndex].ToString());
    }

    return LResult.ToString();
  }

  public string ToSQLText()
  {
    int LCount = FList.Count;
    if(LCount == 0)
      return "";

    StringBuilder LResult = new StringBuilder(LCount);
    for(int LIndex = 0; LIndex < LCount; LIndex++)
    { 
      if(LIndex > 0)
        LResult.Append(", ");
      LResult.Append('\'');
        LResult.Append(FList[LIndex].ToString().Replace("'", "''"));
      LResult.Append('\'');
    }

    return LResult.ToString();
  }

  public static TListInt8 Null { get { return new TListInt8(); } }
  public static TListInt8 New() { return Null; }

  public bool IsNull { get { return false; } }

  public void FromString(String AString)
  {
    if (String.IsNullOrEmpty(AString)) return;

    foreach (string LItem in AString.Split(','))
      FList.Add(Byte.Parse(LItem));
  }

  [
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Src"),
    SqlMethod(Name = "Parse", OnNullCall = false, DataAccess = DataAccessKind.None, IsDeterministic = true)
  ]
  public static TListInt8 Parse(SqlString AString)
  {
    if (AString.IsNull) return null;

    TListInt8 LResult = new TListInt8();
    LResult.FromString(AString.Value);

    return LResult;
  }

  [SqlMethod(Name = "Add", OnNullCall = false, DataAccess = DataAccessKind.None, IsMutator = true)]
  public void Add(Byte AValue) { FList.Add(AValue); }

  public int Length { get { return FList.Count; } }

  [SqlMethod(Name = "Values", OnNullCall = false, DataAccess = DataAccessKind.None, IsDeterministic = true)]
  public Byte Values(int AIndex) { return FList[AIndex - 1]; }

  [SqlMethod(Name = "Contains", OnNullCall = false, DataAccess = DataAccessKind.None, IsDeterministic = true)]
  public Boolean Contains(Byte AValue) { return FList.Contains(AValue); }
  [SqlMethod(Name = "Contains All", OnNullCall = false, DataAccess = DataAccessKind.None, IsDeterministic = true)]

  public Boolean ContainsAll(TListInt8 AValue)
  {
    if(AValue == null || AValue.FList.Count == 0)
      return true;
    for(int i = AValue.FList.Count - 1; i >= 0; i--)
      if(!FList.Contains(AValue.FList[i]))
        return false;
    return true;
  }

  public bool Equals(TListInt8 AList)
  {
    if(AList == null || FList.Count != AList.FList.Count)
      return false;

    for(int i = FList.Count - 1; i >= 0; i--)
      if(FList[i] != AList.FList[i])
        return false;

    return true;
  }

  public void Read(System.IO.BinaryReader r)
  {
#if DEBUG
    int LCount = r.ReadInt32();
#else    
    int LCount = Sql.Read7BitEncodedInt(r);
#endif

    FList.Capacity = LCount;
    for(; LCount > 0; LCount--)
      FList.Add(r.ReadByte());
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
      w.Write(FList[LIndex]);
  }

  [SqlFunction(Name = "Enum(Int8)", FillRowMethodName = "EnumRow", DataAccess = DataAccessKind.None, TableDefinition = "[Value] TinyInt, [Index] SmallInt", IsDeterministic = true)]
  public static IEnumerable Enum(TListInt8 AList)
  {
    if(AList == null) yield break;
      
    Int16 LIndex = 0;
    foreach(Byte LKey in AList.FList)
    { 
      yield return new KeyValuePair<Byte,Int16>(LKey, ++LIndex);
    }
  }
  public static void EnumRow(object ARow, out Byte AValue, out Int16 AIndex)
  {
    AValue = ((KeyValuePair<Byte,Int16>)ARow).Key;
    AIndex = ((KeyValuePair<Byte,Int16>)ARow).Value;
  }
}

[Serializable]
[SqlUserDefinedAggregate(Format.UserDefined, IsInvariantToNulls = true, IsInvariantToDuplicates = false, IsInvariantToOrder = false, MaxByteSize = -1)]
public class TListInt8Aggregate: IBinarySerialize
{
  private TListInt8 OResult;

  public void Init()
  {
    OResult = new TListInt8();
  }

  public void Accumulate(Byte AValue)
  {
    //if (AValue.IsNull) 
    //  return;

		OResult.Add(AValue);
  }

  public void Merge(TListInt8Aggregate AOther)
  {
    OResult.FList.AddRange(AOther.OResult.FList);
  }

  public TListInt8 Terminate()
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
      OResult = new TListInt8();
    OResult.Read(r);
  }

  public void Write(System.IO.BinaryWriter w)
  {
    //if (w == null) throw new ArgumentNullException("w");
    if(OResult != null)
      OResult.Write(w);
  }
};