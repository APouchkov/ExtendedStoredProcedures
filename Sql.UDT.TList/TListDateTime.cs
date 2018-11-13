using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using System.Text;
using System.Xml;

[Serializable]
[SqlUserDefinedType(Microsoft.SqlServer.Server.Format.UserDefined, IsByteOrdered = true, MaxByteSize = -1, Name = "TListDateTime")]
public class TListDateTime: IBinarySerialize/*, IXmlSerializable*/, INullable
{
  public List<DateTime> FList = new List<DateTime>();

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
      DateTime LValue = FList[LIndex]; 
      LResult.Append
      (
        LValue.Date == LValue ?
          XmlConvert.ToString(LValue, Sql.XMLDatePattern)
          :
          XmlConvert.ToString(LValue, XmlDateTimeSerializationMode.RoundtripKind)
      );
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
        DateTime LValue = FList[LIndex]; 
        LResult.Append
        (
          LValue.Date == LValue ?
            XmlConvert.ToString(LValue, Sql.XMLDatePattern)
            :
            XmlConvert.ToString(LValue, XmlDateTimeSerializationMode.RoundtripKind)
        );
      LResult.Append('\'');
    }

    return LResult.ToString();
  }


  public static TListDateTime Null { get { return new TListDateTime(); } }
  public static TListDateTime New() { return Null; }

  public bool IsNull { get { return false; } }

  public void FromString(String AString)
  {
    if (String.IsNullOrEmpty(AString)) return;

    foreach (String LItem in AString.Split(','))
      FList.Add
      (
        XmlConvert.ToDateTime(LItem, XmlDateTimeSerializationMode.RoundtripKind)
        //DateTime.Parse(LItem)
      );
  }

  [
    System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Src"),
    SqlMethod(Name = "Parse", OnNullCall = false, DataAccess = DataAccessKind.None, IsDeterministic = true)
  ]
  public static TListDateTime Parse(SqlString AString)
  {
    if (AString.IsNull) return null;

    TListDateTime LResult = new TListDateTime();
    LResult.FromString(AString.Value);

    return LResult;
  }

  [SqlMethod(Name = "Add", OnNullCall = false, DataAccess = DataAccessKind.None, IsMutator = true)]
  public void Add(DateTime AValue) { FList.Add(AValue); }

  public int Length { get { return FList.Count; } }

  [SqlMethod(Name = "Values", OnNullCall = false, DataAccess = DataAccessKind.None, IsDeterministic = true)]
  public DateTime Values(int AIndex) { return FList[AIndex - 1]; }

  [SqlMethod(Name = "Contains", OnNullCall = false, DataAccess = DataAccessKind.None, IsDeterministic = true)]
  public Boolean Contains(DateTime AValue) { return FList.Contains(AValue); }

  [SqlFunction(Name = "TList.<DateTime>::BinaryContains", DataAccess = DataAccessKind.None, IsDeterministic = true)]
  public static Boolean BinaryContains(SqlBytes AData, DateTime AValue)
  {
    if(AData.IsNull) return false;

    System.IO.BinaryReader r = new System.IO.BinaryReader(new System.IO.MemoryStream(AData.Buffer));

#if DEBUG
    int LCount = r.ReadInt32();
#else    
    int LCount = Sql.Read7BitEncodedInt(r);
#endif
    
    Int64 LValue = (Int64)AValue.ToBinary();
    for(; LCount > 0; LCount--)
      if(r.ReadInt64() == LValue) return true;

    return false;
 }

  [SqlMethod(Name = "Contains All", OnNullCall = false, DataAccess = DataAccessKind.None, IsDeterministic = true)]
  public Boolean ContainsAll(TListDateTime AValue)
  {
    if(AValue == null || AValue.FList.Count == 0)
      return true;
    for(int i = AValue.FList.Count - 1; i >= 0; i--)
      if(!FList.Contains(AValue.FList[i]))
        return false;
    return true;
  }

  public bool Equals(TListDateTime AList)
  {
    if(AList == null || FList.Count != AList.FList.Count)
      return false;

    for(int i = FList.Count - 1; i >= 0; i--)
      if(FList[i] != AList.FList[i])
        return false;

    return true;
  }

  [SqlFunction(Name = "TList.Is Equal<DateTime>", DataAccess = DataAccessKind.None, IsDeterministic = true)]
  public static bool IsEqual(TListDateTime AList1, TListDateTime AList2)
  {
    if (AList1 == null && AList2 == null) return true;
    if (AList1 == null || AList2 == null) return false;

    return AList1.Equals(AList2);
  }

  public void Read(System.IO.BinaryReader r)
  {
    //BinaryFormatter LFormatter = new BinaryFormatter();
    //FList = (List<DateTime>)LFormatter.Deserialize(r.BaseStream);

#if DEBUG
    int LCount = r.ReadInt32();
#else    
    int LCount = Sql.Read7BitEncodedInt(r);
#endif

    FList.Capacity = LCount;
    for(; LCount > 0; LCount--)
      FList.Add(DateTime.FromBinary(r.ReadInt64()));
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
      w.Write((Int64)FList[LIndex].ToBinary());
  }

  [SqlFunction(Name = "Enum(DateTime)", FillRowMethodName = "EnumRow", DataAccess = DataAccessKind.None, TableDefinition = "[Value] DateTime, [Index] Int", IsDeterministic = true)]
  public static IEnumerable Enum(TListDateTime AList)
  {
    if(AList == null) yield break;
      
    Int32 LIndex = 0;
    foreach(DateTime LKey in AList.FList)
    { 
      yield return new KeyValuePair<DateTime,Int32>(LKey, ++LIndex);
    }
  }
  public static void EnumRow(object ARow, out DateTime AValue, out Int32 AIndex)
  {
    AValue = ((KeyValuePair<DateTime,Int32>)ARow).Key;
    AIndex = ((KeyValuePair<DateTime,Int32>)ARow).Value;
  }
}

[Serializable]
[SqlUserDefinedAggregate(Format.UserDefined, IsInvariantToNulls = true, IsInvariantToDuplicates = false, IsInvariantToOrder = false, MaxByteSize = -1)]
public class TListDateTimeAggregate: IBinarySerialize
{
  private TListDateTime OResult;

  public void Init()
  {
    OResult = new TListDateTime();
  }

  public void Accumulate(DateTime? AValue)
  {
    if (AValue != null) 
		  OResult.Add((DateTime)AValue);
  }

  public void Merge(TListDateTimeAggregate AOther)
  {
    OResult.FList.AddRange(AOther.OResult.FList);
  }

  public TListDateTime Terminate()
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
      OResult = new TListDateTime();
    OResult.Read(r);
  }

  public void Write(System.IO.BinaryWriter w)
  {
    //if (w == null) throw new ArgumentNullException("w");
    if(OResult != null)
      OResult.Write(w);
  }
};