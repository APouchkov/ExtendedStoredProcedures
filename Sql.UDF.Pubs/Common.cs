using System;
using System.Data.SqlTypes;
using System.Text;
using System.Linq;

public enum TLoadValueCondition { lvcIfNotPresent = 1 /* E */, lvcIfReceive = 2 /* R */, lvcAlways = 3 /* A */};

public enum TCommentMethod : byte { None = 0, Lattice = 1, DoubleMinus = 2, DoubleSlash = 4, Braces = 8, SlashRange = 16, BracketRange = 32 };
[Flags]
public enum TCommentMethods : byte { None = TCommentMethod.None, Lattice = TCommentMethod.Lattice, DoubleMinus = TCommentMethod.DoubleMinus, DoubleSlash = TCommentMethod.DoubleSlash, Braces = TCommentMethod.Braces, SlashRange = TCommentMethod.SlashRange, BracketRange = TCommentMethod.BracketRange };

public partial class Pub
{
  public static readonly Char[] Spaces = new Char[2] {' ', '\t'};

  public static TLoadValueCondition LoadValueConditionParser(Char AOption)
  {
    switch (AOption)
    {
      case 'E': return TLoadValueCondition.lvcIfNotPresent;
      case 'R': return TLoadValueCondition.lvcIfReceive;
      case 'A': return TLoadValueCondition.lvcAlways;
    }

    throw new Exception("Invalid parameter value TLoadValueCondition = " + AOption);
  }

  public static TCommentMethods CommentMethodsParser(String AOptions)
  {
    TCommentMethods LResult = TCommentMethods.None;

    if (!String.IsNullOrEmpty(AOptions))
      foreach (String LItem in AOptions.Split(','))
      {
        try
        {
          LResult |= (TCommentMethods)Enum.Parse(typeof(TCommentMethods), LItem, true);
        }
        catch (ArgumentException)
        {
          throw new Exception("Неизвестное значение перечисления TCommentMethods: '" + LItem + '\'');
        }
      }

    return LResult;
  }

  public struct TStringNamedItem
  {
    public int     Index;
    public String  Name;
    public String  CastAs;
    public Char    Quote;
    public String  Value;
    public Boolean Eof;
  }

  public class NamedItemsParser
  {
    private String  FString;
    private Char[]  FDelimiters, FDelimitersEx;
    private Boolean FWithCastAs;

    //private Char FCurrChar;
    //private Char FNextChar;

    //private int FLength;
    private int FPosition;

    private TStringNamedItem FCurrent;
    public TStringNamedItem Current { get { return FCurrent; } }

    public NamedItemsParser(String AString, Char[] ADelimiters, Boolean AWithCastAs)
    {
      FString     = AString;
      FDelimiters = ADelimiters;
      
      FDelimitersEx = new Char[FDelimiters.Length + 1];
      FDelimiters.CopyTo(FDelimitersEx, 0);
      FDelimitersEx[FDelimiters.Length] = '\0';

      FWithCastAs = AWithCastAs;

      FPosition       = 0;
      FCurrent.Index  = -1;
      FCurrent.Name   = null;
      FCurrent.CastAs = null;
      FCurrent.Value  = null;
      FCurrent.Eof    = String.IsNullOrEmpty(FString);
    }

    private static readonly Char[] FQuotes = new Char[4] { '"', '[', '\'', '{' };
    private const String SError_InvalidString = "Invalid named variables string: ";
    public Boolean MoveNext()
    {
      while (!FCurrent.Eof && FDelimiters.Contains(FString[FPosition]))
        FCurrent.Eof = (++FPosition == FString.Length);

      if (FCurrent.Eof)
        return false;

      FCurrent.Index++;

      Char LQuote = Strings.InternalGetRightQuote(FString[FPosition], FQuotes);
      if (LQuote == '\0')
      {
        int LEQPosition = FString.IndexOf('=', FPosition);
        if (LEQPosition == -1 || LEQPosition == FPosition)
          throw new Exception(SError_InvalidString + FString);

        FCurrent.Name = FString.Substring(FPosition, LEQPosition - FPosition);
        FPosition = LEQPosition + 1;

        if (FWithCastAs)
        { 
          int LCastAsPosition = FCurrent.Name.LastIndexOf(':');
          if (LCastAsPosition >= 0)
          {
            FCurrent.CastAs = FCurrent.Name.Substring(LCastAsPosition + 1);
            FCurrent.Name = FCurrent.Name.Substring(0, LCastAsPosition);
          }
          else
            FCurrent.CastAs = null;
        }
      }
      else
      {
        Char[] LNextChars;
        if(FWithCastAs)
          LNextChars = new char[2] { ':', '=' };
        else
          LNextChars = new char[1] { '=' };

        FPosition++;
        if(!Sql.InternalParseEOQ(LQuote, FString, ref FPosition, out FCurrent.Name, LNextChars))
          throw new Exception(SError_InvalidString + FString);

        if (FWithCastAs && (FPosition < FString.Length - 1) && (FString[FPosition] == ':'))
        {
          int LEQPosition = FString.IndexOf('=', FPosition + 1);
          if (LEQPosition == -1)
            throw new Exception(SError_InvalidString + FString);
          FCurrent.CastAs = FString.Substring(FPosition + 1, LEQPosition - FPosition - 1);
          FPosition = LEQPosition + 1;
        }
        else
        { 
          FPosition++;
          FCurrent.CastAs = null;
        }
      }

      if (FPosition < FString.Length)
      {
        int LPosition = FPosition;
        while(FPosition < FString.Length && Spaces.Contains(FString[LPosition])) LPosition++;
        if(LPosition == FPosition)
          FCurrent.Quote = Strings.InternalGetRightQuote(FString[FPosition], FQuotes);
        else
        {
          FCurrent.Quote = Strings.InternalGetRightQuote(FString[LPosition], FQuotes);
          if(FCurrent.Quote != '\0')
            FPosition = LPosition;
        }
      }
      else
        FCurrent.Quote = '\0';

      if (FCurrent.Quote == '\0')
      {
        int LDelimiterPosition = FString.IndexOfAny(FDelimiters, FPosition);
        if (LDelimiterPosition == -1)
        {
          LDelimiterPosition = FString.Length;
          FCurrent.Eof = true;
        }
        FCurrent.Value = FString.Substring(FPosition, LDelimiterPosition - FPosition);
        FPosition = LDelimiterPosition + 1;
      }
      else
      {
        FPosition++;
        if(!Sql.InternalParseEOQ(FCurrent.Quote, FString, ref FPosition, out FCurrent.Value, FDelimitersEx))
          throw new Exception(SError_InvalidString + FString);
        FPosition++;
        FCurrent.Eof = (FPosition >= FString.Length);
      }

      return true;
    }
  }

}
