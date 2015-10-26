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

  [SqlFunction(Name = "Quote String", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
  // RETURNS NULL ON NULL INPUT
  public static String QuoteString(String AValue)
  {
    return Quote(AValue, '\'');
  }

  [SqlFunction(Name = "Deep Quote String", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
  // RETURNS NULL ON NULL INPUT
  public static String DeepQuoteString(String AValue, Byte ADepth)
  {
    return DeepQuote(AValue, '\'', ADepth);
  }

  [SqlFunction(Name = "UnQuote", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
  // RETURNS NULL ON NULL INPUT
  public static String UnQuote(String AValue, Char[] AQuotes)
  {
    if (String.IsNullOrEmpty(AValue) || AQuotes == null || AQuotes.Length == 0) return AValue;
    Char LQuote = AValue[0];

    if (!AQuotes.Contains(LQuote))
      return AValue;
    Char RQuote = InternalGetRightQuote(LQuote);
    return AValue.Substring(1, AValue.Length - 2).Replace(new string(RQuote, 2), RQuote.ToString());
  }
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
