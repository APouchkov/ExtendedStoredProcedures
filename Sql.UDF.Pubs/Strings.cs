using System;
using System.Linq;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using System.Text;
using System.IO;
using System.Collections;

/// <summary>
/// Собирает строку из строк с разделителем
/// </summary>
[Serializable]
[SqlUserDefinedAggregate(Format.UserDefined, IsInvariantToNulls = true, IsInvariantToDuplicates = false, IsInvariantToOrder = false, MaxByteSize = -1)]
public class Concat : IBinarySerialize
{
  private StringBuilder intermediateResult;
  private String separatorResult;

  public void Init()
  {
    intermediateResult = new StringBuilder();
    separatorResult = "";
  }

  public void Accumulate(SqlString value, SqlString separator)
  {
    if (value.IsNull || separator.IsNull) return;
    if (intermediateResult.Length == 0)
      intermediateResult.Append(value.Value);
    else
      intermediateResult.Append(value.ToString().Insert(0, separator.Value));
    separatorResult = separator.Value;
  }

  public void Merge(Concat other)
  {
    separatorResult = other.separatorResult;
    if (intermediateResult.Length == 0 || separatorResult == String.Empty)
      intermediateResult.Append(other.intermediateResult);
    else
      intermediateResult.Append(other.intermediateResult.Insert(0, separatorResult));
  }

  public SqlString Terminate()
  {
    if (intermediateResult != null && intermediateResult.Length > 0)
      return (SqlString)(intermediateResult.ToString(0, intermediateResult.Length));
    else
      return null;
  }

  public void Read(BinaryReader r)
  {
    if (r == null) throw new ArgumentNullException("r");
    intermediateResult = new StringBuilder(r.ReadString());
    separatorResult = r.ReadString();
  }

  public void Write(BinaryWriter w)
  {
    if (w == null) throw new ArgumentNullException("w");
    w.Write(intermediateResult.ToString());
    w.Write(separatorResult);
  }
}

public struct TSQLParamParseItem
{
  public String Gap;
  public Char Quote;
  public String Value;
  public Boolean Eof;
}

public class SQLParamsParser
{
  private String FString;
  private Char FPrefix;
  private TCommentMethods FComments; // TCommentMethods
  private Char[] FQuotes;
  private Char[] FLiterals;

  private Char FCurrChar;
  private Char FNextChar;

  private int FLength;
  private int FPosition;

  private TSQLParamParseItem FCurrent;
  public TSQLParamParseItem Current { get { return FCurrent; } }

  public SQLParamsParser(String ACommandText, Char APrefix, TCommentMethods AComments, Char[] AQuotes, Char[] ALiterals)
  {
    FString = ACommandText;
    FPrefix = APrefix;
    FComments = AComments;
    FQuotes = AQuotes;
    FLiterals = ALiterals;

    FPosition = -1;
    FLength = FString.Length;
    MoveToNextChar();
    FCurrent.Gap = "";
    FCurrent.Quote = (Char)0;
    FCurrent.Value = "";
  }

  private void MoveToNextChar(Boolean AIncPosition = true)
  {
    if(AIncPosition)
      FPosition++;

    if (FPosition < FLength)
      FCurrChar = FString[FPosition];
    else
    {
      FCurrChar = '\0';
      FCurrent.Eof = true;
    }

    if (FPosition < FLength - 1)
      FNextChar = FString[FPosition + 1];
    else
      FNextChar = '\0';
  }

  private char[] NameDelimeters = new char[] { ' ', '!', '$', '?', ')', '<', '>', '=', '+', '-', '*', '/', '\\', '%', '^', '&', '|', ',', ';', '\'', '"', '`', (char)13, (char)10, (char)0 };
  private Boolean NameDelimiter(Char AChar)
  {
    return NameDelimeters.Contains(AChar);
  }

  private void SkipText(ref int LPosition, Boolean AIncludeCurrent)
  {
    int LWidth = FPosition - LPosition;
    if (AIncludeCurrent) LWidth++;
    FCurrent.Gap = FCurrent.Gap + FString.Substring(LPosition, LWidth);
    LPosition = FPosition + 1;
  }

  public Boolean MoveNext()
  {
    int LPosition;
    TCommentMethod LCurrComment;

    if (FCurrent.Eof)
      return false;

    FCurrent.Gap = "";
    LPosition = FPosition;
    LCurrComment = TCommentMethod.None;

    for (;;) /*(FPosition <= FLength)*/
    {
      switch (LCurrComment)
      {
        case TCommentMethod.Lattice:
        case TCommentMethod.DoubleMinus:
        case TCommentMethod.DoubleSlash:
          if (FCurrChar == (char)10 || FCurrChar == (char)13 || FCurrChar == (char)0)
            LCurrComment = TCommentMethod.None;
          MoveToNextChar();
          continue;
        case TCommentMethod.SlashRange:
          {
            if ((FCurrChar == '*') && (FNextChar == '/'))
            {
              LCurrComment = TCommentMethod.None;
              FPosition++;
            }
            MoveToNextChar();
            continue;
          }
        case TCommentMethod.BracketRange:
          {
            if ((FCurrChar == '*') && (FNextChar == ')'))
            {
              LCurrComment = TCommentMethod.None;
              FPosition++;
            }
            MoveToNextChar();
            continue;
          }
        case TCommentMethod.Braces:
          {
            if (FCurrChar == '}')
              LCurrComment = TCommentMethod.None;
            MoveToNextChar();
            continue;
          }
      }

      switch (FCurrChar)
      {
        case '#':
          if ((TCommentMethods.Lattice & FComments) != 0)
          {
            LCurrComment = TCommentMethod.Lattice;
            MoveToNextChar();
            continue;
          }
          else break;
        case '{':
          if ((TCommentMethods.Braces & FComments) != 0)
          {
            LCurrComment = TCommentMethod.Braces;
            MoveToNextChar();
            continue;
          }
          else break;
        case '-':
          if (((TCommentMethods.DoubleMinus & FComments) != 0) && (FNextChar == '-'))
          {
            LCurrComment = TCommentMethod.DoubleMinus;
            FPosition++;
            MoveToNextChar();
            continue;
          }
          else break;
        case '/':
          if (((TCommentMethods.DoubleSlash & FComments) != 0) && (FNextChar == '/'))
          {
            LCurrComment = TCommentMethod.DoubleSlash;
            FPosition++;
            MoveToNextChar();
            continue;
          }
          else if (((TCommentMethods.SlashRange & FComments) != 0) && (FNextChar == '*'))
          {
            LCurrComment = TCommentMethod.SlashRange;
            FPosition++;
            MoveToNextChar();
            continue;
          }
          else break;
        case '(':
          if (((TCommentMethods.BracketRange & FComments) != 0) && (FNextChar == '*'))
          {
            LCurrComment = TCommentMethod.BracketRange;
            FPosition++;
            MoveToNextChar();
            continue;
          }
          else break;
        case (char)0:
          SkipText(ref LPosition, false);
          FCurrent.Quote = (Char)0;
          FCurrent.Value = "";
          FCurrent.Eof = true;
          return true;
        default:
          if (FCurrChar == FPrefix)
            if (FNextChar == FPrefix)
            {
              FPosition++;
              SkipText(ref LPosition, false);
              MoveToNextChar();
              continue;
            }
            else
            {
              SkipText(ref LPosition, false);
              MoveToNextChar();
              FCurrent.Quote = Pub.InternalGetRightQuote(FCurrChar, FQuotes);
              if (FCurrent.Quote != (char)0)
              {
                FPosition++;
                if (Pub.InternalParseEOQ(FCurrent.Quote, FString, ref FPosition, out FCurrent.Value, new char[0]))
                {
                  if (String.IsNullOrEmpty(FCurrent.Value))
                  {
                    LPosition--;
                    SkipText(ref LPosition, false);
                  }
                  else
                  {
                    LPosition = FPosition;
                    FCurrent.Eof = (FPosition >= FLength);
                    if(!FCurrent.Eof) MoveToNextChar(false);
                    return true;
                  }
                }
                else
                {
                  LPosition--;
                  FPosition = FLength;
                  SkipText(ref LPosition, true);
                  FCurrent.Quote = (Char)0;
                  FCurrent.Value = "";
                  FCurrent.Eof = true;
                  return true;
                }
              }
              else
              {
                while (!NameDelimiter(FCurrChar))
                  MoveToNextChar();
                if (LPosition == FPosition)
                  LPosition--;
                else
                {
                  FCurrent.Value = FString.Substring(LPosition, FPosition - LPosition);
                  FCurrent.Eof = (FPosition >= FLength);
                  return true;
                }
              }
              break;
            }
          else
            break;
      }

      if (FLiterals.Contains(FCurrChar))
      {
        Char LEndChar = Pub.InternalGetRightQuote(FCurrChar);
        do
        {
          MoveToNextChar();
          if (FCurrChar == LEndChar)
            if (FNextChar == LEndChar)
            {
              MoveToNextChar();
              //SkipText(ref LPosition, false);
              //FPosition++;
            }
            else
            {
              MoveToNextChar();
              break;
            }
        } while (FPosition < FLength);
      }
      else
        MoveToNextChar();
    }

  }
}

public partial class Pub
{
  /// <summary>
  /// Возвращает символ закрывающей квоты, во символу открывающей квоты
  /// </summary>
  public static char InternalGetRightQuote(char ALeftQuote)
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

  public static char InternalGetRightQuote(char ALeftQuote, char[] AAllowedQuotes)
  {
    if (AAllowedQuotes.Contains(ALeftQuote))
      return InternalGetRightQuote(ALeftQuote);
    return (char)0;
  }

  public static Boolean InternalParseEOQ(char ARightQuote, String AString, ref int AOffset, out String AValue, char[] ANextChars)
  {
    int LPos, LNextPos;
    char LNextChar;

    AValue = "";
    LPos = AOffset;
    for (; ; )
    {
      LNextPos = AString.IndexOf(ARightQuote, LPos);
      if (LNextPos == 0)
        return false;

      AValue += AString.Substring(LPos, LNextPos - LPos);
      LPos = LNextPos + 1;
      if (LPos >= AString.Length)
        LNextChar = (char)0;
      else
        LNextChar = AString[LPos];

      if (LNextChar == ARightQuote)
      {
        LPos++;
        AValue += ARightQuote;
      }
      else if ((ANextChars.Length == 0) || ANextChars.Contains(LNextChar))
        break;
      else
        return false;
    }

    AOffset = LPos;
    return true;
  }


  /// <summary>
  /// Разбивает строку на подстроки по делителю
  /// </summary>
  [SqlFunction(FillRowMethodName = "SplitRow", DataAccess = DataAccessKind.None, TableDefinition = "value nvarchar(max)", IsDeterministic = true)]
  public static IEnumerable Split(SqlString AText, Char ASeparator)
  {
    if (AText.IsNull || (AText.Value.Length == 0)) return null; //yield break;
    //if (String.IsNullOrEmpty(ASeparator))
    //  return AText.Split();

    return AText.Value.Split(new Char[1]{ASeparator});
  }

  public static void SplitRow(Object ARow, out SqlString AValue)
  {
    AValue = (String)ARow;
  }

  [SqlFunction(Name = "Convert Binary::To Base64", DataAccess = DataAccessKind.None, IsDeterministic = true)]
  public static String ConvertBinaryToBase64(SqlBinary ABinary)
  {
    //if (ABinary.IsNull) return null;
    return Convert.ToBase64String(ABinary.Value);
  }

  [SqlFunction(Name = "Trim Left", DataAccess = DataAccessKind.None, IsDeterministic = true)]
  public static String TrimLeft(String AValue, SqlChars ASymbols)
  {
    //if (AValue.IsNull) return AValue;
    return AValue.TrimStart(ASymbols.Value);
  }

  [SqlFunction(Name = "Trim Right", DataAccess = DataAccessKind.None, IsDeterministic = true)]
  public static String TrimRight(String AValue, SqlChars ASymbols)
  {
    //if (AValue.IsNull) return AValue;
    return AValue.TrimEnd(ASymbols.Value);
  }

  [SqlFunction(Name = "Trim Right(Spaces)", DataAccess = DataAccessKind.None, IsDeterministic = true)]
  public static String TrimRightSpaces(String AValue)
  {
    //if (AValue.IsNull) return AValue;
    return AValue.TrimEnd();
  }

  [SqlFunction(Name = "Trim", DataAccess = DataAccessKind.None, IsDeterministic = true)]
  public static String Trim(String AValue, SqlChars ASymbols)
  {
    //if (AValue.IsNull) return AValue;
    return AValue.Trim(ASymbols.Value);
  }

  [SqlFunction(Name = "Trim(Spaces)", DataAccess = DataAccessKind.None, IsDeterministic = true)]
  public static String TrimSpaces(String AValue)
  {
    //if (AValue.IsNull) return AValue;
    return AValue.Trim();
  }

  [SqlFunction(Name = "Remove Characters", DataAccess = DataAccessKind.None, IsDeterministic = true)]
  public static String RemoveCharacters(String AValue, Char[] AChars)
  {
    //if (AValue.IsNull) return AValue;
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

  [SqlFunction(Name = "Quote", DataAccess = DataAccessKind.None, IsDeterministic = true)]
  public static String Quote(String AValue, Char AQuote)
  {
    //if (AValue.IsNull) return AValue;
    Char RQuote = InternalGetRightQuote(AQuote);
    return AQuote + AValue.Replace(new String(RQuote, 1), new String(RQuote, 2)) + RQuote;
  }

  [SqlFunction(Name = "Deep Quote", DataAccess = DataAccessKind.None, IsDeterministic = true)]
  public static String DeepQuote(String AValue, Char AQuote, Byte ADepth)
  {
    //if (AValue.IsNull) return AValue;
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

  [SqlFunction(Name = "Quote String", DataAccess = DataAccessKind.None, IsDeterministic = true)]
  public static String QuoteString(String AValue)
  {
    //if (AValue.IsNull) return AValue;
    return Quote(AValue, '\'');
  }

  [SqlFunction(Name = "Deep Quote String", DataAccess = DataAccessKind.None, IsDeterministic = true)]
  public static String DeepQuoteString(String AValue, Byte ADepth)
  {
    //if (AValue.IsNull) return AValue;
    return DeepQuote(AValue, '\'', ADepth);
  }

  [SqlFunction(Name = "UnQuote", DataAccess = DataAccessKind.None, IsDeterministic = true)]
  public static String UnQuote(String AValue, Char[] AQuotes)
  {
    if (String.IsNullOrEmpty(AValue) || AQuotes == null || AQuotes.Length == 0) return AValue;
    char LQuote = AValue[0];

    if (!AQuotes.Contains(LQuote))
      return AValue;
    char RQuote = InternalGetRightQuote(LQuote);
    return AValue.Substring(1, AValue.Length - 2).Replace(new string(RQuote, 2), RQuote.ToString());
  }

  [SqlFunction(Name = "Trim VarBinary", DataAccess = DataAccessKind.None, IsDeterministic = true)]
  public static SqlBinary TrimVarBinary(SqlBinary AValue)
  {
    if ((AValue.Value.Length == 0) || (AValue.Value[0] > 0))
      return AValue;
    for (int I = 0; I < AValue.Value.Length; I++)
    {
      if (AValue.Value[I] > 0)
      {
        byte[] result = new byte[AValue.Value.Length - I];
        Array.Copy(AValue.Value, I, result, 0, result.Length);
        return new SqlBinary(result);
      }
    }
    return new SqlBinary(new byte[] { 0 });
  }
}