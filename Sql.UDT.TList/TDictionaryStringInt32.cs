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
[SqlUserDefinedType(Microsoft.SqlServer.Server.Format.UserDefined, IsByteOrdered = true, MaxByteSize = -1, Name = "TDictionaryStringInt32")]
public class TDictionaryStringInt32: IBinarySerialize/*, IXmlSerializable*/, INullable
{
  public Dictionary<String,Int32> FList = new Dictionary<String,Int32>();

  public override string ToString()
  {
    if(FList.Count == 0)
      return "";

    StringBuilder LResult = new StringBuilder(FList.Count * 2);
    foreach(KeyValuePair<String,Int32> LKeyValuePair in FList)
    {
      if(LResult.Length > 0)
        LResult.Append(';');
      LResult.Append(LKeyValuePair.Key);
      LResult.Append('=');
      LResult.Append(LKeyValuePair.Value);
    }

    return LResult.ToString();
  }

  public static TDictionaryStringInt32 Null { get { return new TDictionaryStringInt32(); } }
  public static TDictionaryStringInt32 New() { return Null; }

  public bool IsNull { get { return false; } }

  public void FromString(String AString)
  {
    if (String.IsNullOrEmpty(AString)) return;

    foreach (String LItem in AString.Split(';'))
    {
      int LIndex = LItem.IndexOf('=');
      FList.Add(LItem.Substring(0, LIndex), Int32.Parse(LItem.Substring(LIndex + 1)));
    }
  }

  [
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Src"),
    SqlMethod(Name = "Parse", OnNullCall = false, DataAccess = DataAccessKind.None, IsDeterministic = true)
  ]
  public static TDictionaryStringInt32 Parse(SqlString AString)
  {
    if (AString.IsNull) return null;

    TDictionaryStringInt32 LResult = new TDictionaryStringInt32();
    LResult.FromString(AString.Value);

    return LResult;
 }

  [SqlMethod(Name = "Add", OnNullCall = false, DataAccess = DataAccessKind.None, IsMutator = true)]
  public void Add(String AName, Int32 AValue) { FList.Add(AName, AValue); }

  public int Length { get { return FList.Count; } }

  [SqlMethod(Name = "Values", OnNullCall = false, DataAccess = DataAccessKind.None, IsDeterministic = true)]
  public SqlInt32 Values(String AName) { return FList[AName]; }

  [SqlMethod(Name = "Contains", OnNullCall = false, DataAccess = DataAccessKind.None, IsDeterministic = true)]
  public Boolean Contains(String AName) { return FList.ContainsKey(AName); }

  [SqlMethod(Name = "Contains All", OnNullCall = false, DataAccess = DataAccessKind.None, IsDeterministic = true)]
  public Boolean ContainsAll(TListString AValue)
  {
    if(AValue == null || AValue.FList.Count == 0)
      return true;

    for(int i = AValue.FList.Count - 1; i >= 0; i--)
      if(!FList.ContainsKey(AValue.FList[i]))
        return false;
    return true;
  }

  public bool Equals(TDictionaryStringInt32 AList)
  {
    if(AList == null || FList.Count != AList.FList.Count)
      return false;

    foreach(KeyValuePair<String,Int32> LKeyPair in AList.FList)
    {
      if(FList[LKeyPair.Key] != LKeyPair.Value)
        return false;
    }

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
      FList.Add(r.ReadString(), r.ReadInt32());
  }

  public void Write(System.IO.BinaryWriter w)
  {
    int LCount = FList.Count;
#if DEBUG
    w.Write(LCount);
#else    
    Sql.Write7BitEncodedInt(w, LCount);
#endif

    foreach(KeyValuePair<String,Int32> LKeyPair in FList)
    {
      w.Write(LKeyPair.Key);
      w.Write(LKeyPair.Value);
    }

  }

  [SqlFunction(Name = "Enum(String,Int32)", FillRowMethodName = "EnumRow", DataAccess = DataAccessKind.None, TableDefinition = "[Name] NVarChar(4000), [Value] Int, [Index] Int", IsDeterministic = true)]
  public static IEnumerable Enum(TDictionaryStringInt32 ADictionary)
  {
    if(ADictionary == null) yield break;
      
    Int32 LIndex = 0;
    foreach(KeyValuePair<String,Int32> LKeyPair in ADictionary.FList)
    { 
      yield return new KeyValueIndexPair<String,Int32,Int32>(LKeyPair.Key, LKeyPair.Value, ++LIndex);
    }
  }
  public static void EnumRow(object ARow, out String AName, out Int32 AValue, out Int32 AIndex)
  {
    AName  = ((KeyValueIndexPair<String,Int32,Int32>)ARow).Key;
    AValue = ((KeyValueIndexPair<String,Int32,Int32>)ARow).Value;
    AIndex = ((KeyValueIndexPair<String,Int32,Int32>)ARow).Index;
  }
}

[Serializable]
[SqlUserDefinedAggregate(Format.UserDefined, IsInvariantToNulls = false, IsInvariantToDuplicates = false, IsInvariantToOrder = false, MaxByteSize = -1)]
public class TDictionaryStringInt32Aggregate: IBinarySerialize
{
  private TDictionaryStringInt32 OResult;

  public void Init()
  {
    OResult = new TDictionaryStringInt32();
  }

  public void Accumulate(String AName, Int32 AValue)
  {
		OResult.Add(AName, AValue);
  }

  public void Merge(TDictionaryStringInt32Aggregate AOther)
  {
    foreach(KeyValuePair<String,Int32> LKeyPair in AOther.OResult.FList)
      OResult.FList.Add(LKeyPair.Key, LKeyPair.Value);
  }

  public TDictionaryStringInt32 Terminate()
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
      OResult = new TDictionaryStringInt32();
    OResult.Read(r);
  }

  public void Write(System.IO.BinaryWriter w)
  {
    //if (w == null) throw new ArgumentNullException("w");
    if(OResult != null)
      OResult.Write(w);
  }
};