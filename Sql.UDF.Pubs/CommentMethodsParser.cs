using System;
using System.Data.SqlTypes;
using System.Text;

public enum TCommentMethod : byte { None = 0, Lattice = 1, DoubleMinus = 2, DoubleSlash = 4, Braces = 8, SlashRange = 16, BracketRange = 32 };
[Flags]
public enum TCommentMethods : byte { None = TCommentMethod.None, Lattice = TCommentMethod.Lattice, DoubleMinus = TCommentMethod.DoubleMinus, DoubleSlash = TCommentMethod.DoubleSlash, Braces = TCommentMethod.Braces, SlashRange = TCommentMethod.SlashRange, BracketRange = TCommentMethod.BracketRange };

public partial class Pub
{
  public static TCommentMethods CommentMethodsParser(String AOptions)
  {
    TCommentMethods LResult = TCommentMethods.None;

    // Если параметры не переданы, то ничего не парсим
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
