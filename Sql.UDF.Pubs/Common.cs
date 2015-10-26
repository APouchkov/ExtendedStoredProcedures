using System;
using System.Data.SqlTypes;
using System.Text;

public enum TLoadValueCondition { lvcIfNotPresent = 1 /* E */, lvcIfReceive = 2 /* R */, lvcAlways = 3 /* A */};

public enum TCommentMethod : byte { None = 0, Lattice = 1, DoubleMinus = 2, DoubleSlash = 4, Braces = 8, SlashRange = 16, BracketRange = 32 };
[Flags]
public enum TCommentMethods : byte { None = TCommentMethod.None, Lattice = TCommentMethod.Lattice, DoubleMinus = TCommentMethod.DoubleMinus, DoubleSlash = TCommentMethod.DoubleSlash, Braces = TCommentMethod.Braces, SlashRange = TCommentMethod.SlashRange, BracketRange = TCommentMethod.BracketRange };

public partial class Pub
{
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
}
