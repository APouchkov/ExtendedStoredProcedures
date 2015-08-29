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
[SqlUserDefinedType(Microsoft.SqlServer.Server.Format.UserDefined, IsByteOrdered = true, MaxByteSize = -1, Name = "TListInt64")]
public class TListInt64: IBinarySerialize/*, IXmlSerializable*/, INullable
{
  public List<Int64> FList = new List<Int64>();

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

  public static TListInt64 Null { get { return new TListInt64(); } }
  public static TListInt64 New() { return Null; }

  public bool IsNull { get { return false; } }

  public void FromString(String AString)
  {
    if (String.IsNullOrEmpty(AString)) return;

    foreach (string LItem in AString.Split(','))
      FList.Add(Int64.Parse(LItem));
  }

  [
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Src"),
    SqlMethod(Name = "Parse", OnNullCall = false, DataAccess = DataAccessKind.None, IsDeterministic = true)
  ]
  public static TListInt64 Parse(SqlString AString)
  {
    if (AString.IsNull) return null;

    TListInt64 LResult = new TListInt64();
    LResult.FromString(AString.Value);

    return LResult;
  }

  [SqlMethod(Name = "Add", OnNullCall = false, DataAccess = DataAccessKind.None, IsMutator = true)]
  public void Add(Int64 AValue) { FList.Add(AValue); }

  public int Length { get { return FList.Count; } }

  [SqlMethod(Name = "Values", OnNullCall = false, DataAccess = DataAccessKind.None, IsDeterministic = true)]
  public Int64 Values(int AIndex) { return FList[AIndex - 1]; }

  [SqlMethod(Name = "Contains", OnNullCall = false, DataAccess = DataAccessKind.None, IsDeterministic = true)]
  public Boolean Contains(Int64 AValue) { return FList.Contains(AValue); }

  [SqlMethod(Name = "Contains All", OnNullCall = false, DataAccess = DataAccessKind.None, IsDeterministic = true)]
  public Boolean ContainsAll(TListInt64 AValue)
  {
    if(AValue == null || AValue.FList.Count == 0)
      return true;
    for(int i = AValue.FList.Count - 1; i >= 0; i--)
      if(!FList.Contains(AValue.FList[i]))
        return false;
    return true;
  }

  public bool Equals(TListInt64 AList)
  {
    if(AList == null || FList.Count != AList.FList.Count)
      return false;

    for(int i = FList.Count - 1; i >= 0; i--)
      if(FList[i] != AList.FList[i])
        return false;

    return true;
  }

  [SqlMethod(Name = "ToCompressedBinary", OnNullCall = false, DataAccess = DataAccessKind.None, IsDeterministic = true)]
  public SqlBytes ToCompressedBinary()
  {
    if(FList == null || FList.Count == 0)
      return null;

    System.IO.MemoryStream s = new System.IO.MemoryStream();
    System.IO.BinaryWriter w = new System.IO.BinaryWriter(s);

    int LCount = FList.Count;
    Sql.Write7BitEncodedInt(w, LCount);

    for(int LIndex = 0; LIndex < LCount; LIndex++)
      Sql.Write7BitEncodedInt64(w, FList[LIndex]);

    return new SqlBytes(s);
  }

  [
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Src"),
    SqlMethod(Name = "FromCompressedBinary", OnNullCall = false, DataAccess = DataAccessKind.None, IsDeterministic = true)
  ]
  public static TListInt64 FromCompressedBinary(SqlBytes AData)
  {
    if (AData.IsNull) return TListInt64.Null;

    TListInt64 LResult = new TListInt64();
    System.IO.BinaryReader r = new System.IO.BinaryReader(AData.Stream);

    int LCount = Sql.Read7BitEncodedInt(r);

    LResult.FList.Capacity = LCount;
    for(; LCount > 0; LCount--)
      LResult.FList.Add(Sql.Read7BitEncodedInt64(r));

    return LResult;
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
      FList.Add(r.ReadInt64());
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

  [SqlFunction(Name = "Enum(Int64)", FillRowMethodName = "EnumRow", DataAccess = DataAccessKind.None, TableDefinition = "[Value] BigInt, [Index] Int", IsDeterministic = true)]
  public static IEnumerable Enum(TListInt64 AList)
  {
    if(AList == null) yield break;
      
    Int32 LIndex = 0;
    foreach(Int64 LKey in AList.FList)
    { 
      yield return new KeyValuePair<Int64, Int32>(LKey, ++LIndex);
    }
  }
  public static void EnumRow(object ARow, out Int64 AValue, out Int32 AIndex)
  {
    AValue = ((KeyValuePair<Int64,Int32>)ARow).Key;
    AIndex = ((KeyValuePair<Int64,Int32>)ARow).Value;
  }
}

[Serializable]
[SqlUserDefinedAggregate(Format.UserDefined, IsInvariantToNulls = true, IsInvariantToDuplicates = false, IsInvariantToOrder = false, MaxByteSize = -1)]
public class TListInt64Aggregate: IBinarySerialize
{
  private TListInt64 OResult;

  public void Init()
  {
    OResult = new TListInt64();
  }

  public void Accumulate(Int64 AValue)
  {
    //if (AValue.IsNull) 
    //  return;

		OResult.Add(AValue);
  }

  public void Merge(TListInt64Aggregate AOther)
  {
    OResult.FList.AddRange(AOther.OResult.FList);
  }

  public TListInt64 Terminate()
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
      OResult = new TListInt64();
    OResult.Read(r);
  }

  public void Write(System.IO.BinaryWriter w)
  {
    //if (w == null) throw new ArgumentNullException("w");
    if(OResult != null)
      OResult.Write(w);
  }
};