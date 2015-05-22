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
[SqlUserDefinedType(Microsoft.SqlServer.Server.Format.UserDefined, IsByteOrdered = true, MaxByteSize = -1, Name = "TDictionaryStringString")]
public class TDictionaryStringString: IBinarySerialize/*, IXmlSerializable*/, INullable
{
  public Dictionary<String, String> FList = new Dictionary<String, String>();

  public override string ToString()
  {
    if(FList.Count == 0)
      return "";

    StringBuilder LResult = new StringBuilder(FList.Count * 2);
    foreach(KeyValuePair<String,String> LKeyValuePair in FList)
    {
      if(LResult.Length > 0)
        LResult.Append(';');
      LResult.Append(LKeyValuePair.Key);
      if(LKeyValuePair.Value != null)
      { 
        LResult.Append('=');
        LResult.Append(LKeyValuePair.Value);
      }
    }

    return LResult.ToString();
  }

  public static TDictionaryStringString Null { get { return new TDictionaryStringString(); } }
  public static TDictionaryStringString New() { return Null; }

  public bool IsNull { get { return false; } }

  public void FromString(String AString)
  {
    if (String.IsNullOrEmpty(AString)) return;

    foreach (String LItem in AString.Split(';'))
    {
      int LIndex = LItem.IndexOf('=');
      String LName, LValue;
      if(LIndex != -1)
      {
        LName  = LItem.Substring(0, LIndex);
        LValue = LItem.Substring(LIndex + 1);
      }
      else
      {
        LName  = LItem;
        LValue = null;
      }
      FList.Add(LName, LValue);
    }
  }

  public static TDictionaryStringString Parse(SqlString AString)
  {
    if (AString.IsNull) return null;

    TDictionaryStringString LResult = new TDictionaryStringString();
    LResult.FromString(AString.Value);

    return LResult;
 }

  [SqlMethod(Name = "Add", OnNullCall = false, IsMutator = true)]
  public void Add(String AName, String AValue) { FList.Add(AName, AValue); }

  public int Length { get { return FList.Count; } }

  [SqlMethod(Name = "Values", OnNullCall = false, IsDeterministic = true)]
  public String Values(String AName) { return FList[AName]; }

  [SqlMethod(Name = "Contains", OnNullCall = false, IsDeterministic = true)]
  public Boolean Contains(String AName) { return FList.ContainsKey(AName); }

  [SqlMethod(Name = "Contains All", OnNullCall = false, IsDeterministic = true)]
  public Boolean ContainsAll(TListString AValue)
  {
    if(AValue == null || AValue.FList.Count == 0)
      return true;

    for(int i = AValue.FList.Count - 1; i >= 0; i--)
      if(!FList.ContainsKey(AValue.FList[i]))
        return false;
    return true;
  }

  public bool Equals(TDictionaryStringString AList)
  {
    if(AList == null || FList.Count != AList.FList.Count)
      return false;

    foreach(KeyValuePair<String,String> LKeyPair in AList.FList)
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
    {
      String LName = r.ReadString();
      String LValue = r.ReadString();
      if(LValue.Length == 1 && LValue[0] == '\0')
        LValue = null;
      FList.Add(LName, LValue);
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

    foreach(KeyValuePair<String,String> LKeyPair in FList)
    {
      w.Write(LKeyPair.Key);
      w.Write(LKeyPair.Value == null ? '\0'.ToString() : LKeyPair.Value);
    }

  }

  [SqlFunction(Name = "Enum(String,String)", FillRowMethodName = "EnumRow", DataAccess = DataAccessKind.None, TableDefinition = "[Name] NVarChar(4000), [Value] NVarChar(4000), [Index] Int", IsDeterministic = true)]
  public static IEnumerable Enum(TDictionaryStringString ADictionary)
  {
    if(ADictionary == null) yield break;
      
    Int32 LIndex = 0;
    foreach(KeyValuePair<String,String> LKeyPair in ADictionary.FList)
    { 
      yield return new KeyValueIndexPair<String,String,Int32>(LKeyPair.Key, LKeyPair.Value, ++LIndex);
    }
  }
  public static void EnumRow(object ARow, out String AName, out SqlString AValue, out Int32 AIndex)
  {
    AName  = ((KeyValueIndexPair<String,String,Int32>)ARow).Key;
    AValue = ((KeyValueIndexPair<String,String,Int32>)ARow).Value;
    AIndex = ((KeyValueIndexPair<String,String,Int32>)ARow).Index;
  }
}

[Serializable]
[SqlUserDefinedAggregate(Format.UserDefined, IsInvariantToNulls = false, IsInvariantToDuplicates = false, IsInvariantToOrder = false, MaxByteSize = -1)]
public class TDictionaryStringStringAggregate: IBinarySerialize
{
  private TDictionaryStringString OResult;

  public void Init()
  {
    OResult = new TDictionaryStringString();
  }

  public void Accumulate(String AName, String AValue)
  {
		//OResult.Add(AName, AValue.IsNull ? "" : AValue.Value);
		OResult.Add(AName, AValue);
  }

  public void Merge(TDictionaryStringStringAggregate AOther)
  {
    foreach(KeyValuePair<String,String> LKeyPair in AOther.OResult.FList)
      OResult.FList.Add(LKeyPair.Key, LKeyPair.Value);
  }

  public TDictionaryStringString Terminate()
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
      OResult = new TDictionaryStringString();
    OResult.Read(r);
  }

  public void Write(System.IO.BinaryWriter w)
  {
    //if (w == null) throw new ArgumentNullException("w");
    if(OResult != null)
      OResult.Write(w);
  }
};