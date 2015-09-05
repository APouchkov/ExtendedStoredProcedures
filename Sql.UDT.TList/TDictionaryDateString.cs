using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using System.Text;
using System.Xml;

//using System.Xml.Schema;
//using System.Xml.Serialization;

[Serializable]
[SqlUserDefinedType(Microsoft.SqlServer.Server.Format.UserDefined, IsByteOrdered = true, MaxByteSize = -1, Name = "TDictionaryDateString")]
public class TDictionaryDateString: IBinarySerialize/*, IXmlSerializable*/, INullable
{
  public Dictionary<DateTime,String> FList = new Dictionary<DateTime,String>();

  public override string ToString()
  {
    if(FList.Count == 0)
      return "";

    StringBuilder LResult = new StringBuilder(FList.Count * 11);
    foreach(KeyValuePair<DateTime,String> LKeyValuePair in FList)
    {
      if(LResult.Length > 0)
        LResult.Append(';');
      LResult.Append(XmlConvert.ToString(LKeyValuePair.Key, Sql.XMLDatePattern));
      if (LKeyValuePair.Value != null)
      {
        LResult.Append('=');
        LResult.Append(LKeyValuePair.Value);
      }
    }

    return LResult.ToString();
  }

  public static TDictionaryDateString Null { get { return new TDictionaryDateString(); } }
  public static TDictionaryDateString New() { return Null; }

  public bool IsNull { get { return false; } }

  public void FromString(String AString)
  {
    if (String.IsNullOrEmpty(AString)) return;

    foreach (String LItem in AString.Split(';'))
    {
      int LIndex = LItem.IndexOf('=');
      DateTime  LName;
      String    LValue;
      if(LIndex != -1)
      {
        LName  = XmlConvert.ToDateTime(LItem.Substring(0, LIndex), XmlDateTimeSerializationMode.RoundtripKind);
        LValue = LItem.Substring(LIndex + 1);
      }
      else
      {
        LName  = XmlConvert.ToDateTime(LItem, XmlDateTimeSerializationMode.RoundtripKind);
        LValue = null;
      }
      FList.Add(LName, LValue);
    }
  }

  [
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Src"),
    SqlMethod(Name = "Parse", OnNullCall = false, DataAccess = DataAccessKind.None, IsDeterministic = true)
  ]
  public static TDictionaryDateString Parse(SqlString AString)
  {
    if (AString.IsNull) return null;

    TDictionaryDateString LResult = new TDictionaryDateString();
    LResult.FromString(AString.Value);

    return LResult;
 }

  [SqlMethod(Name = "Add", OnNullCall = false, DataAccess = DataAccessKind.None, IsMutator = true)]
  public void Add(DateTime AName, String AValue) { FList.Add(AName.Date, AValue); }

  public int Length { get { return FList.Count; } }

  [SqlMethod(Name = "Values", OnNullCall = false, DataAccess = DataAccessKind.None, IsDeterministic = true)]
  public SqlString Values(DateTime AName) { return FList[AName]; }

  [SqlMethod(Name = "Contains", OnNullCall = false, DataAccess = DataAccessKind.None, IsDeterministic = true)]
  public Boolean Contains(DateTime AName) { return FList.ContainsKey(AName); }

  [SqlMethod(Name = "Contains All", OnNullCall = false, DataAccess = DataAccessKind.None, IsDeterministic = true)]
  public Boolean ContainsAll(TListDateTime AValue)
  {
    if(AValue == null || AValue.FList.Count == 0)
      return true;

    for(int i = AValue.FList.Count - 1; i >= 0; i--)
      if(!FList.ContainsKey(AValue.FList[i]))
        return false;
    return true;
  }

  public bool Equals(TDictionaryDateString AList)
  {
    if(AList == null || FList.Count != AList.FList.Count)
      return false;

    foreach(KeyValuePair<DateTime,String> LKeyPair in AList.FList)
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
      Int32  LName  = r.ReadInt32();

      String LValue = r.ReadString();
      if(LValue.Length == 1 && LValue[0] == '\0')
        LValue = null;
      FList.Add(new DateTime(LName / 10000, LName / 100 % 100, LName % 100), LValue);
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

    foreach(KeyValuePair<DateTime,String> LKeyPair in FList)
    {
      w.Write((Int32)(LKeyPair.Key.Year * 10000 + LKeyPair.Key.Month * 100 + LKeyPair.Key.Day));
      w.Write(LKeyPair.Value == null ? '\0'.ToString() : LKeyPair.Value);
    }

  }

  [SqlFunction(Name = "Enum(DateTime,String)", FillRowMethodName = "EnumRow", DataAccess = DataAccessKind.None, TableDefinition = "[Name] Int, [Value] NVarChar(4000), [Index] Int", IsDeterministic = true)]
  public static IEnumerable Enum(TDictionaryDateString ADictionary)
  {
    if(ADictionary == null) yield break;
      
    Int32 LIndex = 0;
    foreach(KeyValuePair<DateTime,String> LKeyPair in ADictionary.FList)
    { 
      yield return new KeyValueIndexPair<DateTime,String,Int32>(LKeyPair.Key, LKeyPair.Value, ++LIndex);
    }
  }
  public static void EnumRow(object ARow, out DateTime AName, out String AValue, out Int32 AIndex)
  {
    AName  = ((KeyValueIndexPair<DateTime,String,Int32>)ARow).Key;
    AValue = ((KeyValueIndexPair<DateTime,String,Int32>)ARow).Value;
    AIndex = ((KeyValueIndexPair<DateTime,String,Int32>)ARow).Index;
  }
}

[Serializable]
[SqlUserDefinedAggregate(Format.UserDefined, IsInvariantToNulls = false, IsInvariantToDuplicates = false, IsInvariantToOrder = false, MaxByteSize = -1)]
public class TDictionaryDateStringAggregate: IBinarySerialize
{
  private TDictionaryDateString OResult;

  public void Init()
  {
    OResult = new TDictionaryDateString();
  }

  public void Accumulate(DateTime? AName, String AValue)
  {
    if(AName != null)
		  OResult.Add((DateTime)AName, AValue);
  }

  public void Merge(TDictionaryDateStringAggregate AOther)
  {
    foreach(KeyValuePair<DateTime,String> LKeyPair in AOther.OResult.FList)
      OResult.FList.Add(LKeyPair.Key, LKeyPair.Value);
  }

  public TDictionaryDateString Terminate()
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
      OResult = new TDictionaryDateString();
    OResult.Read(r);
  }

  public void Write(System.IO.BinaryWriter w)
  {
    //if (w == null) throw new ArgumentNullException("w");
    if(OResult != null)
      OResult.Write(w);
  }
};