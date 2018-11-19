using System;
using System.Linq;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using System.Text;
using System.IO;
using System.Collections;

public class Strings
{
  /// <summary>
  /// Возвращает символ закрывающей квоты, во символу открывающей квоты
  /// </summary>
  public static char InternalGetRightQuote(Char ALeftQuote)
  {
    switch (ALeftQuote)
    {
      case '[': return ']';
      case '{': return '}';
      case '(': return ')';
      case '<': return '>';
      default: return ALeftQuote; // == !!!!
    }
  }

  public static Char InternalGetRightQuote(Char ALeftQuote, Char[] AAllowedQuotes)
  {
    if (AAllowedQuotes.Contains(ALeftQuote))
      return InternalGetRightQuote(ALeftQuote);
    return (char)0;
  }

  static String NullIfEmpty(String Value)
  {
    return Value == "" ? null : Value;
  }
  static String NullIfEmpty(SqlString Value)
  {
    return Value.IsNull ? null : NullIfEmpty(Value.Value);
  }

  [SqlFunction(Name = "Convert Binary::To Base64", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
  // RETURNS NULL ON NULL INPUT
  public static String ConvertBinaryToBase64(SqlBinary ABinary)
  {
    return Convert.ToBase64String(ABinary.Value);
  }

  public static void BytesToHex(byte[] ABytes, StringBuilder AResult, int AStartIndex = 0, int ACount = -1)
  {
    int LEndIndex;
    if(ACount == -1)
      LEndIndex = ABytes.Length - AStartIndex;
    else
      LEndIndex = AStartIndex + ACount;
    
    int LByteBuffer;
    for(int i = AStartIndex; i < LEndIndex; i++)
    {
      byte LByte = ABytes[i];
      LByteBuffer = LByte >> 4; AResult.Append((char)(55 + LByteBuffer + (((LByteBuffer - 10) >> 31) & -7)));
      LByteBuffer = LByte & 0xF; AResult.Append((char)(55 + LByteBuffer + (((LByteBuffer - 10) >> 31) & -7)));
    }
  }

  [SqlFunction(Name = "Convert Binary::To Hex", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
  // RETURNS NULL ON NULL INPUT
  public static String ConvertBinaryToHex(SqlBinary ABinary)
  {
    StringBuilder LResult = new StringBuilder(ABinary.Length * 2);
    BytesToHex(ABinary.Value, LResult);
    return LResult.ToString();
  }

  [SqlFunction(Name = "Convert Binary::To Hex(Splited)", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
  // RETURNS NULL ON NULL INPUT
  public static String ConvertBinaryToHexSplited(SqlBinary ABinary, Int16 ALineBytes, String ASeparator)
  {
    //String SValue = BitConverter.ToString(ABinary.Value);
    byte[] LBinary = ABinary.Value;
    int LLength = LBinary.Length;
    int LLines  = (LLength + ALineBytes - 1) / ALineBytes;
    StringBuilder LResult = new StringBuilder(LLength * 2 + ASeparator.Length * (LLines - 1));

    for(int i = 0; i < LLines; i++)
    {
      if(i > 0)
        LResult.Append(ASeparator);

      BytesToHex(LBinary, LResult, i * ALineBytes, ALineBytes);
      //LResult.Append(BitConverter.ToString(ABinary.Value, i * ALineBytes, ALineBytes));
    }
    return LResult.ToString();
  }

  [SqlFunction(Name = "Trim Left", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
  // RETURNS NULL ON NULL INPUT
  public static String TrimLeft(String AValue, Char[] ASymbols)
  {
    return AValue.TrimStart(ASymbols);
  }

  [SqlFunction(Name = "Trim Left(Null If Empty)", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
  // RETURNS NULL ON NULL INPUT
  public static String TrimLeftNull(String AValue, Char[] ASymbols)
  {
    AValue = AValue.TrimStart(ASymbols);
    if(AValue.Length == 0)
      return null;
    else
      return AValue;
  }

  [SqlFunction(Name = "Trim Right", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
  // RETURNS NULL ON NULL INPUT
  public static String TrimRight(String AValue, Char[] ASymbols)
  {
    return AValue.TrimEnd(ASymbols);
  }

  [SqlFunction(Name = "Trim Right(Null If Empty)", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
  // RETURNS NULL ON NULL INPUT
  public static String TrimRightNull(String AValue, Char[] ASymbols)
  {
    AValue = AValue.TrimEnd(ASymbols);
    if(AValue.Length == 0)
      return null;
    else
      return AValue;
  }

  [SqlFunction(Name = "Trim Right#Spaces", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
  // RETURNS NULL ON NULL INPUT
  public static String TrimRightSpaces(String AValue)
  {
    return AValue.TrimEnd();
  }

  [SqlFunction(Name = "Trim", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
  // RETURNS NULL ON NULL INPUT
  public static String Trim(String AValue, Char[] ASymbols)
  {
    return AValue.Trim(ASymbols);
  }

  [SqlFunction(Name = "Trim(Null If Empty)", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
  // RETURNS NULL ON NULL INPUT
  public static String TrimNull(String AValue, Char[] ASymbols)
  {
    AValue = AValue.Trim(ASymbols);
    if(AValue.Length == 0)
      return null;
    else
      return AValue;
  }

  [SqlFunction(Name = "Trim#Spaces", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
  // RETURNS NULL ON NULL INPUT
  public static String TrimSpaces(String AValue)
  {
    return AValue.Trim();
  }

  [SqlFunction(Name = "Trim#Spaces(Null If Empty)", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
  // RETURNS NULL ON NULL INPUT
  public static String TrimSpacesNull(String AValue)
  {
    AValue = AValue.Trim();
    if(AValue.Length == 0)
      return null;
    else
      return AValue;
  }

  [SqlFunction(Name = "Remove Characters", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
  // RETURNS NULL ON NULL INPUT
  public static String RemoveCharacters(String AValue, Char[] AChars)
  {
    StringBuilder LResult = new StringBuilder(AValue.Length);

    for (int i = 0; i < AValue.Length; i++)
      if (!AChars.Contains(AValue[i]))
        LResult.Append(AValue[i]);

    //for (int i = LValue.Length - 1; i >= 0; i--)
    //  if (LChars.Contains(LValue[i]))
    //    LValue = LValue.Remove(i, 1);

    if (LResult.Length < AValue.Length)
      return LResult.ToString();
    else
      return AValue;
  }

  [SqlFunction(Name = "Quote", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
  // RETURNS NULL ON NULL INPUT
  public static String Quote(String AValue, Char AQuote)
  {
    Char RQuote = InternalGetRightQuote(AQuote);
    return AQuote + AValue.Replace(new String(RQuote, 1), new String(RQuote, 2)) + RQuote;
  }

  [SqlFunction(Name = "QuoteIfNeeded", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
  // RETURNS NULL ON NULL INPUT
  public static String QuoteIfNeeded(String AValue, Char AQuote)
  {
    Char RQuote = InternalGetRightQuote(AQuote);
    if(AValue.IndexOf(AQuote) >= 0)
      return AQuote + AValue.Replace(new String(RQuote, 1), new String(RQuote, 2)) + RQuote;
    return AValue;
  }

  [SqlFunction(Name = "QuoteIfNeeded(Ex)", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
  // RETURNS NULL ON NULL INPUT
  public static String QuoteIfNeededEx(String AValue, Char AQuote, char[] AExtraChars)
  {
    Char RQuote = InternalGetRightQuote(AQuote);
    if((AValue.IndexOf(AQuote) >= 0) || (AValue.IndexOfAny(AExtraChars) >= 0))
      return AQuote + AValue.Replace(new String(RQuote, 1), new String(RQuote, 2)) + RQuote;
    return AValue;
  }

  [SqlFunction(Name = "Deep Quote", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
  // RETURNS NULL ON NULL INPUT
  public static String DeepQuote(String AValue, Char AQuote, Byte ADepth)
  {
    Char RQuote = InternalGetRightQuote(AQuote);
    int Shl = 1 << (ADepth - 1);
    String RQuotes = new string(RQuote, Shl);

    if (AQuote == RQuote)
    {
      return
        RQuotes
        + AValue.Replace(RQuote.ToString(), RQuotes + RQuote)
        + RQuotes;
    }
    else
    {
      return
        AQuote
        + AValue.Replace(RQuote.ToString(), RQuotes + RQuote)
        + RQuotes;
    }
  }

  [SqlFunction(Name = "String@Quote?SQL", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
  // RETURNS NULL ON NULL INPUT
  public static String QuoteStringSQL(String AValue)
  {
    return Quote(AValue, '\'');
  }

  [SqlFunction(Name = "String@Quote(If Needed)?SQL", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
  // RETURNS NULL ON NULL INPUT
  public static String QuoteStringSQLIfNeeded(String AValue)
  {
    return QuoteIfNeeded(AValue, '\'');
  }

  [SqlFunction(Name = "String@Deep Quote?SQL", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
  // RETURNS NULL ON NULL INPUT
  public static String DeepQuoteStringSQL(String AValue, Byte ADepth)
  {
    return DeepQuote(AValue, '\'', ADepth);
  }

  [SqlFunction(Name = "String@UnQuote", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
  // RETURNS NULL ON NULL INPUT
  public static String UnQuote(String AValue, Char[] AQuotes)
  {
    if (AValue == null || AValue.Length < 2 || AQuotes == null || AQuotes.Length == 0) return AValue;
    Char LQuote = AValue[0];
    Char RQuote = InternalGetRightQuote(LQuote, AQuotes);
    if (RQuote == (Char)0)
      return AValue;

    if(AValue[AValue.Length - 1] != RQuote)
      return AValue;

      //if(AValue.Length < 800)
      //  throw new System.SystemException("Unclosed quotation at string: " + AValue);
      //else
      //  throw new System.SystemException("Unclosed quotation at string");

    return AValue.Substring(1, AValue.Length - 2).Replace(new string(RQuote, 2), RQuote.ToString());
  }

/*
  [SqlFunction(Name = "UnQuoteCustom", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
  // RETURNS NULL ON NULL INPUT
  public static String UnQuoteCustom(String AValue, Char[] AQuotes)
  {
    if (AValue == null || AValue.Length < 2 || AQuotes == null || AQuotes.Length == 0) return AValue;

    int LQuoIndex = AValue.IndexOfAny(AQuotes);
    if (LQuoIndex == -1)
      return AValue;

    Char RQuote = InternalGetRightQuote(LQuote);
    return AValue.Substring(1, AValue.Length - 2).Replace(new string(RQuote, 2), RQuote.ToString());
  }
*/

}

/// <summary>
/// Собирает строку из строк с разделителем
/// </summary>
[Serializable]
[SqlUserDefinedAggregate(Format.UserDefined, IsInvariantToNulls = true, IsInvariantToDuplicates = false, IsInvariantToOrder = false, MaxByteSize = -1)]
public class Concat: IBinarySerialize
{
  private StringBuilder FResult;
  private String        FSeparator;
  private Boolean       FEmpty;

  public void Init()
  {
    FResult     = new StringBuilder();
    FSeparator  = "";
    FEmpty      = true;
  }

  public void Accumulate(String AValue, String ASeparator)
  {
    if (AValue == null || ASeparator == null) return;

    if(FEmpty)
    {
      FEmpty     = false;
      FSeparator = ASeparator;
    }
    else if (ASeparator.Length > 0)
      FResult.Append(ASeparator);

    FResult.Append(AValue);
  }

  public void Merge(Concat AOther)
  {
    if(AOther.FEmpty) return;

    FSeparator = AOther.FSeparator;
    if (!FEmpty && FSeparator.Length > 0)
      FResult.Append(FSeparator);

    FResult.Append(AOther.FResult);
    FEmpty = false;
  }

  public String Terminate()
  {
    if (FResult != null && FResult.Length > 0)
      return FResult.ToString();
    else
      return null;
  }

  public void Read(BinaryReader r)
  {
    //if (r == null) throw new ArgumentNullException("r");
    FResult     = new StringBuilder(r.ReadString());
    FSeparator  = r.ReadString();
    FEmpty      = r.ReadBoolean();
  }

  public void Write(BinaryWriter w)
  {
    //if (w == null) throw new ArgumentNullException("w");
    w.Write(FResult.ToString());
    w.Write(FSeparator);
    w.Write(FEmpty);
  }
}
