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
[SqlUserDefinedType(Microsoft.SqlServer.Server.Format.UserDefined, IsByteOrdered = true, MaxByteSize = -1, Name = "TListString")]
public class TListString: IBinarySerialize/*, IXmlSerializable*/, INullable
{
  public List<String> FList = new List<String>();

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

  public static TListString Null { get { return new TListString(); } }
  public static TListString New() { return Null; }

  public bool IsNull { get { return false; } }

  public void FromString(String AString)
  {
    if (String.IsNullOrEmpty(AString)) return;

    foreach (string LItem in AString.Split(','))
      FList.Add(LItem);
  }

  [
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Src"),
    SqlMethod(Name = "Parse", OnNullCall = false, DataAccess = DataAccessKind.None, IsDeterministic = true)
  ]
  public static TListString Parse(SqlString AString)
  {
    if (AString.IsNull) return null;

    TListString LResult = new TListString();
    LResult.FromString(AString.Value);

    return LResult;
 }

  [SqlMethod(Name = "Add", OnNullCall = false, DataAccess = DataAccessKind.None, IsMutator = true)]
  public void Add(String AValue) { FList.Add(AValue); }

  public int Length { get { return FList.Count; } }

  [SqlMethod(Name = "Values", OnNullCall = false, DataAccess = DataAccessKind.None, IsDeterministic = true)]
  public String Values(int AIndex) { return FList[AIndex - 1]; }

  [SqlMethod(Name = "Contains", OnNullCall = false, DataAccess = DataAccessKind.None, IsDeterministic = true)]
  public Boolean Contains(String AValue) { return FList.Contains(AValue); }

  [SqlMethod(Name = "Contains All", OnNullCall = false, DataAccess = DataAccessKind.None, IsDeterministic = true)]
  public Boolean ContainsAll(TListString AValue)
  {
    if(AValue == null || AValue.FList.Count == 0)
      return true;
    for(int i = AValue.FList.Count - 1; i >= 0; i--)
      if(!FList.Contains(AValue.FList[i]))
        return false;
    return true;
  }

  public bool Equals(TListString AList)
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

    for(; LCount > 0; LCount--)
      FList.Add(r.ReadString());
  }

  public void Write(System.IO.BinaryWriter w)
  {
    int LCount = FList.Count;
#if DEBUG
    w.Write(LCount);
#else    
    Sql.Write7BitEncodedInt(w, LCount);
#endif

    FList.Capacity = LCount;
    for(int LIndex = 0; LIndex < LCount; LIndex++)
      w.Write(FList[LIndex]);
  }

  [SqlFunction(Name = "Enum(String)", FillRowMethodName = "EnumRow", DataAccess = DataAccessKind.None, TableDefinition = "[Value] NVarChar(4000), [Index] Int", IsDeterministic = true)]
  public static IEnumerable Enum(TListString AList)
  {
    if(AList == null) yield break;
      
    Int32 LIndex = 0;
    foreach(String LKey in AList.FList)
    { 
      yield return new KeyValuePair<String,Int32>(LKey, ++LIndex);
    }
  }
  public static void EnumRow(object ARow, out SqlString AValue, out Int32 AIndex)
  {
    AValue = ((KeyValuePair<String,Int32>)ARow).Key;
    AIndex = ((KeyValuePair<String,Int32>)ARow).Value;
  }
}

[Serializable]
[SqlUserDefinedAggregate(Format.UserDefined, IsInvariantToNulls = true, IsInvariantToDuplicates = false, IsInvariantToOrder = false, MaxByteSize = -1)]
public class TListStringAggregate: IBinarySerialize
{
  private TListString OResult;

  public void Init()
  {
    OResult = new TListString();
  }

  public void Accumulate(SqlString AValue)
  {
    if (AValue.IsNull) 
      return;

		OResult.Add(AValue.Value);
  }

  public void Merge(TListStringAggregate AOther)
  {
    OResult.FList.AddRange(AOther.OResult.FList);
  }

  public TListString Terminate()
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
      OResult = new TListString();
    OResult.Read(r);
  }

  public void Write(System.IO.BinaryWriter w)
  {
    //if (w == null) throw new ArgumentNullException("w");
    if(OResult != null)
      OResult.Write(w);
  }
};