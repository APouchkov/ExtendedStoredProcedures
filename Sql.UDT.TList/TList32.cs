using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using System.Text;

[Serializable]
[SqlUserDefinedType(Microsoft.SqlServer.Server.Format.UserDefined, IsByteOrdered = true, MaxByteSize = -1, Name = "TListInt32")]
public class TListInt32: IBinarySerialize/*, IXmlSerializable*/, INullable
{
  public List<Int32> FList = new List<Int32>();

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

  public static TListInt32 Null { get { return new TListInt32(); } }
  public static TListInt32 New() { return Null; }

  public bool IsNull { get { return false; } }

  public void FromString(String AString)
  {
    if (String.IsNullOrEmpty(AString)) return;

    foreach (String LItem in AString.Split(','))
      FList.Add(Int32.Parse(LItem));
  }

  public static TListInt32 Parse(SqlString AString)
  {
    if (AString.IsNull) return null;

    TListInt32 LResult = new TListInt32();
    LResult.FromString(AString.Value);

    return LResult;
  }

  [SqlMethod(Name = "Add", OnNullCall = false, IsMutator = true)]
  public void Add(Int32 AValue) { FList.Add(AValue); }

  public int Length { get { return FList.Count; } }

  [SqlMethod(Name = "Values", OnNullCall = false, IsDeterministic = true)]
  public Int32 Values(int AIndex) { return FList[AIndex - 1]; }

  [SqlMethod(Name = "Contains", OnNullCall = false, IsDeterministic = true)]
  public Boolean Contains(Int32 AValue) { return FList.Contains(AValue); }

  [SqlMethod(Name = "Contains All", OnNullCall = false, IsDeterministic = true)]
  public Boolean ContainsAll(TListInt32 AValue)
  {
    if(AValue == null || AValue.FList.Count == 0)
      return true;
    for(int i = AValue.FList.Count - 1; i >= 0; i--)
      if(!FList.Contains(AValue.FList[i]))
        return false;
    return true;
  }

  public bool Equals(TListInt32 AList)
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
    //BinaryFormatter LFormatter = new BinaryFormatter();
    //FList = (List<Int32>)LFormatter.Deserialize(r.BaseStream);

#if DEBUG
    int LCount = r.ReadInt32();
#else    
    int LCount = Sql.Read7BitEncodedInt(r);
#endif

    FList.Capacity = LCount;
    for(; LCount > 0; LCount--)
      FList.Add(r.ReadInt32());
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

  [SqlFunction(Name = "Enum(Int32)", FillRowMethodName = "EnumRow", DataAccess = DataAccessKind.None, TableDefinition = "[Value] Int, [Index] Int", IsDeterministic = true)]
  public static IEnumerable Enum(TListInt32 AList)
  {
    if(AList == null) yield break;
      
    Int32 LIndex = 0;
    foreach(Int32 LKey in AList.FList)
    { 
      yield return new KeyValuePair<Int32,Int32>(LKey, ++LIndex);
    }
  }
  public static void EnumRow(object ARow, out Int32 AValue, out Int32 AIndex)
  {
    AValue = ((KeyValuePair<Int32,Int32>)ARow).Key;
    AIndex = ((KeyValuePair<Int32,Int32>)ARow).Value;
  }
}

[Serializable]
[SqlUserDefinedAggregate(Format.UserDefined, IsInvariantToNulls = true, IsInvariantToDuplicates = false, IsInvariantToOrder = false, MaxByteSize = -1)]
public class TListInt32Aggregate: IBinarySerialize
{
  private TListInt32 OResult;

  public void Init()
  {
    OResult = new TListInt32();
  }

  public void Accumulate(Int32 AValue)
  {
    //if (AValue.IsNull) 
    //  return;

		OResult.Add(AValue);
  }

  public void Merge(TListInt32Aggregate AOther)
  {
    OResult.FList.AddRange(AOther.OResult.FList);
  }

  public TListInt32 Terminate()
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
      OResult = new TListInt32();
    OResult.Read(r);
  }

  public void Write(System.IO.BinaryWriter w)
  {
    //if (w == null) throw new ArgumentNullException("w");
    if(OResult != null)
      OResult.Write(w);
  }
};