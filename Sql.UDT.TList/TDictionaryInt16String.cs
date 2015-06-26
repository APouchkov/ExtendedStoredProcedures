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
[SqlUserDefinedType(Microsoft.SqlServer.Server.Format.UserDefined, IsByteOrdered = true, MaxByteSize = -1, Name = "TDictionaryInt16String")]
public class TDictionaryInt16String: IBinarySerialize/*, IXmlSerializable*/, INullable
{
  public Dictionary<Int16,String> FList = new Dictionary<Int16,String>();

  public override string ToString()
  {
    if(FList.Count == 0)
      return "";

    StringBuilder LResult = new StringBuilder(FList.Count * 2);
    foreach(KeyValuePair<Int16,String> LKeyValuePair in FList)
    {
      if(LResult.Length > 0)
        LResult.Append(';');
      LResult.Append(LKeyValuePair.Key);
      LResult.Append('=');
      LResult.Append(LKeyValuePair.Value);
    }

    return LResult.ToString();
  }

  public static TDictionaryInt16String Null { get { return new TDictionaryInt16String(); } }
  public static TDictionaryInt16String New() { return Null; }

  public bool IsNull { get { return false; } }

  public void FromString(String AString)
  {
    if (String.IsNullOrEmpty(AString)) return;

    foreach (String LItem in AString.Split(';'))
    {
      int LIndex = LItem.IndexOf('=');
      Int16   LName;
      String  LValue;
      if(LIndex != -1)
      {
        LName  = Int16.Parse(LItem.Substring(0, LIndex));
        LValue = LItem.Substring(LIndex + 1);
      }
      else
      {
        LName  = Int16.Parse(LItem);
        LValue = null;
      }
      FList.Add(LName, LValue);
    }
  }

  [
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Src"),
    SqlMethod(Name = "Parse", OnNullCall = false, DataAccess = DataAccessKind.None, IsDeterministic = true)
  ]
  public static TDictionaryInt16String Parse(SqlString AString)
  {
    if (AString.IsNull) return null;

    TDictionaryInt16String LResult = new TDictionaryInt16String();
    LResult.FromString(AString.Value);

    return LResult;
 }

  [SqlMethod(Name = "Add", OnNullCall = false, DataAccess = DataAccessKind.None, IsMutator = true)]
  public void Add(Int16 AName, String AValue) { FList.Add(AName, AValue); }

  public int Length { get { return FList.Count; } }

  [SqlMethod(Name = "Values", OnNullCall = false, DataAccess = DataAccessKind.None, IsDeterministic = true)]
  public SqlString Values(Int16 AName) { return FList[AName]; }

  [SqlMethod(Name = "Contains", OnNullCall = false, DataAccess = DataAccessKind.None, IsDeterministic = true)]
  public Boolean Contains(Int16 AName) { return FList.ContainsKey(AName); }

  [SqlMethod(Name = "Contains All", OnNullCall = false, DataAccess = DataAccessKind.None, IsDeterministic = true)]
  public Boolean ContainsAll(TListInt16 AValue)
  {
    if(AValue == null || AValue.FList.Count == 0)
      return true;

    for(int i = AValue.FList.Count - 1; i >= 0; i--)
      if(!FList.ContainsKey(AValue.FList[i]))
        return false;
    return true;
  }

  public bool Equals(TDictionaryInt16String AList)
  {
    if(AList == null || FList.Count != AList.FList.Count)
      return false;

    foreach(KeyValuePair<Int16,String> LKeyPair in AList.FList)
    {
      if(FList[LKeyPair.Key] != LKeyPair.Value)
        return false;
    }

    return true;
  }

  public void Read(System.IO.BinaryReader r)
  {
#if DEBUG
    int LCount = r.ReadInt16();
#else    
    int LCount = Sql.Read7BitEncodedInt(r);
#endif

    for(; LCount > 0; LCount--)
    {
      Int16  LName  = r.ReadInt16();
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

    foreach(KeyValuePair<Int16,String> LKeyPair in FList)
    {
      w.Write(LKeyPair.Key);
      w.Write(LKeyPair.Value == null ? '\0'.ToString() : LKeyPair.Value);
    }

  }

  [SqlFunction(Name = "Enum(Int16,String)", FillRowMethodName = "EnumRow", DataAccess = DataAccessKind.None, TableDefinition = "[Name] Int, [Value] NVarChar(4000), [Index] Int", IsDeterministic = true)]
  public static IEnumerable Enum(TDictionaryInt16String ADictionary)
  {
    if(ADictionary == null) yield break;
      
    Int16 LIndex = 0;
    foreach(KeyValuePair<Int16,String> LKeyPair in ADictionary.FList)
    { 
      yield return new KeyValueIndexPair<Int16,String,Int32>(LKeyPair.Key, LKeyPair.Value, ++LIndex);
    }
  }
  public static void EnumRow(object ARow, out Int16 AName, out String AValue, out Int32 AIndex)
  {
    AName  = ((KeyValueIndexPair<Int16,String,Int32>)ARow).Key;
    AValue = ((KeyValueIndexPair<Int16,String,Int32>)ARow).Value;
    AIndex = ((KeyValueIndexPair<Int16,String,Int32>)ARow).Index;
  }
}

[Serializable]
[SqlUserDefinedAggregate(Format.UserDefined, IsInvariantToNulls = false, IsInvariantToDuplicates = false, IsInvariantToOrder = false, MaxByteSize = -1)]
public class TDictionaryInt16StringAggregate: IBinarySerialize
{
  private TDictionaryInt16String OResult;

  public void Init()
  {
    OResult = new TDictionaryInt16String();
  }

  public void Accumulate(Int16 AName, String AValue)
  {
		OResult.Add(AName, AValue);
  }

  public void Merge(TDictionaryInt16StringAggregate AOther)
  {
    foreach(KeyValuePair<Int16,String> LKeyPair in AOther.OResult.FList)
      OResult.FList.Add(LKeyPair.Key, LKeyPair.Value);
  }

  public TDictionaryInt16String Terminate()
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
      OResult = new TDictionaryInt16String();
    OResult.Read(r);
  }

  public void Write(System.IO.BinaryWriter w)
  {
    //if (w == null) throw new ArgumentNullException("w");
    if(OResult != null)
      OResult.Write(w);
  }
};