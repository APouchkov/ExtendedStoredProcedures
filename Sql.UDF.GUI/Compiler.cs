//------------------------------------------------------------------------------
// <copyright file="CSSqlFunction.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
using System;
using System.Text;
using System.Linq;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;

public partial class Compiler
{
  [SqlFunction(Name = "Compile", DataAccess = DataAccessKind.Read, SystemDataAccess = SystemDataAccessKind.Read, IsDeterministic = false)]
  public static String Compile(String AText, UDT.TParams AParams, String AComments, Char ALiteral, String ATranslateScalar)
  {
    if(String.IsNullOrWhiteSpace(AText))
      return AText;

    if(ATranslateScalar != null && ATranslateScalar.IndexOf("@Text") == -1)
      throw new Exception("Скалярный запрос на межязыковой перевод должен содержать параметр \"@Text\"");
    return (new ScriptParser(AText, AParams, Pub.CommentMethodsParser(AComments), ALiteral, ATranslateScalar)).Text.ToString();
  }
}

/*
{$IF <CONDITION>}
{$ELSE}
{$END}

{$INCLUDE <NAME>}
{$TRANSLATE <TEXT>}
{$EVALUATE <EXPRESSION>}
*/

public class ScriptParser
{
  private const String ContextConnection = "context connection=true";

  private String FString;
  private UDT.TParams FParams;
  private TCommentMethods FComments; // TCommentMethods
  private Char FLiteral;
  private String FTranslateScalar;

  private Char FPriorChar;
  private Char FCurrChar;
  private Char FNextChar;

  private int FLength;
  private int FPosition;

  //private TScriptParseItem FCurrent;
  //public TScriptParseItem Current { get { return FCurrent; } }

  private String  FGap;
  private int     FLine;
  private String  FCommand;
  private String  FValue;
  private Boolean FEof;
  private Boolean FBol;
  private SqlConnection FSqlConnection;
  private SqlCommand    FSqlCommandTranslateScalar;
  private SqlCommand    FSqlCommandOther;

  public StringBuilder Text;

  public void Exception(String AInstruction)
  {
    throw new Exception("Ошибка в строке " + FLine.ToString() + ": " + AInstruction);
  }

  private enum TScriptParserWaitFor: byte { None = 0, Else = 1, End = 2 };

  public ScriptParser(String AText, UDT.TParams AParams, TCommentMethods AComments, Char ALiteral, String ATranslateScalar)
  {
    FString           = AText;
    FParams           = AParams;
    FComments         = AComments;
    FLiteral          = ALiteral;
    FTranslateScalar  = ATranslateScalar;

    FPosition = -1;
    FLength   = FString.Length;
    FCurrChar = '\0';
    MoveToNextChar();

    // FLine
    FGap     = "";
    FLine    = 1;
    FCommand = "";
    FValue   = "";
    //FBol     = false;

    Text = new StringBuilder(1024);

    if(FParams != null)
    {
      FParams.InitContextConnection();
      FSqlConnection = FParams.ContextConnection;
    }
    else
    {
      FSqlConnection = new SqlConnection(ContextConnection);
      FSqlConnection.Open();
    }

    CallIF(TScriptParserWaitFor.None, false);
  }

  private static void InternalDeepQuoteLevel(ref String AString, out byte ADepth)
  {
    if(AString == null || AString.Length < 3 || AString[0] != '<' || AString[2] != '>')
      ADepth = 0;
    else
    {
      ADepth = Convert.ToByte(AString[1].ToString());
      AString = AString.Substring(3, AString.Length - 3).TrimStart();
    }
  }

  private String InternalDeepQuote(String AString, byte ADepth)
  {
    if(ADepth == 0 || String.IsNullOrWhiteSpace(AString))
      return AString;
    else
    {
      int Shl = 1 << ADepth;
      return AString.Replace(new String(FLiteral, 1), new String(FLiteral, Shl));
    }
  }

  private void InternalSkipReturns()
  {
    if(FCurrChar == (Char)13 || FCurrChar == (Char)10)
    {
      MoveToNextChar();
      if((FCurrChar == (Char)13 || FCurrChar == (Char)10) && (FPriorChar != FCurrChar))
        MoveToNextChar();
    }
  }

  // Возвращает TRUE, если найден ELSE
  private Boolean CallIF(TScriptParserWaitFor AWaitFor, Boolean ASkipText)
  {
    while (MoveNext())
    {
      if(!ASkipText) Text.Append(FGap);

      if(!String.IsNullOrEmpty(FCommand))
        if(FCommand == "IF")
        {
          if(FBol) InternalSkipReturns();

          Boolean LIfTrue = INT.TParams.EvaluateBoolean(FParams, FValue, false);
          Boolean LElseFound = CallIF(TScriptParserWaitFor.Else | TScriptParserWaitFor.End, ASkipText | (!LIfTrue));
          if(LElseFound) CallIF(TScriptParserWaitFor.End, ASkipText | LIfTrue);
        }
        else if(FCommand == "END" || FCommand == "ENDIF" || FCommand == "IFEND")
        {
          if(FBol) InternalSkipReturns();

          if((AWaitFor & TScriptParserWaitFor.End) == 0)
            Exception("Найдена инструкция {$END} без предшествующей ей инструкции {$IF}");
          return false;
        }
        else if(ASkipText)
          continue;

        else if(FCommand == "ELSE")
        {
          if(FBol) InternalSkipReturns();

          if((AWaitFor & TScriptParserWaitFor.Else) == 0)
            Exception("Найдена инструкция {$ELSE} без предшествующей ей инструкции {$IF}");
          return true;
        }
        else if(FCommand == "I" || FCommand == "INCLUDE")
        {
          byte LDepth;
          String LStringFValue = FValue.Trim();
          InternalDeepQuoteLevel(ref LStringFValue, out LDepth);

          SqlString LValue = INT.TParams.AsNVarChar(FParams, LStringFValue);
          if(!LValue.IsNull)
          {
            Text.Append(InternalDeepQuote(LValue.Value, LDepth));
          }
        }
        else if(FTranslateScalar != null && (FCommand == "T" || FCommand == "TRANSLATE"))
        {
          byte LDepth;
          String LStringFValue = FValue.Trim();
          InternalDeepQuoteLevel(ref LStringFValue, out LDepth);

          if(FSqlCommandTranslateScalar == null)
          {
            FSqlCommandTranslateScalar = FSqlConnection.CreateCommand();
            FSqlCommandTranslateScalar.CommandType = CommandType.Text;
            FSqlCommandTranslateScalar.CommandText = FTranslateScalar;
            FSqlCommandTranslateScalar.Parameters.Add("@Text", SqlDbType.NVarChar, 1000).Direction = ParameterDirection.Input;
          }

          FSqlCommandTranslateScalar.Parameters[0].Value = LStringFValue;
          Object LValue = FSqlCommandTranslateScalar.ExecuteScalar();
          Text.Append(InternalDeepQuote(LValue.ToString(), LDepth));
        }
        else if(FCommand == "SELECT" || FCommand == "EXEC")
        {
          byte LDepth;
          String LStringFValue = FValue;
          InternalDeepQuoteLevel(ref LStringFValue, out LDepth);

          if(FSqlCommandOther == null)
          {
            FSqlCommandOther = FSqlConnection.CreateCommand();
            FSqlCommandOther.CommandType = CommandType.Text;
          }

          //Object LValue = INT.TParams.InternalEvaluate(FParams, FCommand + ' ' + LStringFValue);
          FSqlCommandOther.CommandText = DynamicSQL.FinalSQL(FCommand + ' ' + LStringFValue, FParams);
          try
          { 
            Object LValue = FSqlCommandOther.ExecuteScalar();
            if(LValue != null && LValue != DBNull.Value)
              Text.Append(InternalDeepQuote(Convert.ToString(LValue), LDepth));
          }
          catch(Exception E)
          {
            Exception("Ошибка исполнения запроса {" + FSqlCommandOther.CommandText + "}: " + E.Message);
          }
        }
        else if(FCommand == "SET")
        {
          int LEqual = FValue.IndexOf('=');
          if(LEqual < 0)
            Exception("Пропущен символ присвоения '=': " + FCommand + ' ' + FValue);

          String LName = FValue.Substring(0, LEqual).Trim();
          if(LName.Length == 0)
            Exception("Имя макроса должно быть непустым: " + FCommand + ' ' + FValue);
          
          FParams.AddParam(LName, new SqlString(FValue.Substring(LEqual + 1, FValue.Length - LEqual - 1)));
          InternalSkipReturns();
        }
        else if(FCommand == "C" || FCommand == "COMMENT")
        {
          InternalSkipReturns();
        }
        else
          Exception("Найдена неизвестная инструкция {$" + FCommand + "}");

    }

    if((AWaitFor & TScriptParserWaitFor.End) != 0)
      Exception("Конец сценария достигнут, хотя ожидается инструкция {$END}");

    return false;
  }

  // Разбор текста
  private void MoveToNextChar()
  {
    FPriorChar = FCurrChar;

    FPosition++;
    if (FPosition < FLength)
      FCurrChar = FString[FPosition];
    else
    {
      FCurrChar = '\0';
      FEof = true;
    }

    if (FPosition < FLength - 1)
      FNextChar = FString[FPosition + 1];
    else
      FNextChar = '\0';
  }

  private void SkipText(ref int LPosition, Boolean AIncludeCurrent)
  {
    if(LPosition >= FString.Length)
      return;

    int LWidth;
    if(FPosition >= FString.Length)
      LWidth = FString.Length - LPosition;
    else 
      LWidth = FPosition - LPosition;

    if (AIncludeCurrent) LWidth++;
    if(LWidth > 0)
      FGap = FGap + FString.Substring(LPosition, LWidth);
    LPosition = FPosition + 1;
  }

  private Char[] LSpaces = new Char[3] {' ', '\r', '\n'};
  public Boolean MoveNext()
  {

    int LPosition;
    int LLine = FLine;
    Boolean LWaitForCommand = false;
    Boolean LInLiteral = false;
    TCommentMethod LCurrComment;

    if (FEof)
      return false;

    FGap = "";
    LPosition = FPosition;
    LCurrComment = TCommentMethod.None;

    for (;;) /*(FPosition <= FLength)*/
    {
      if(FCurrChar == (char)13) LLine++;

      if(LWaitForCommand && FCurrChar == '}')
      {
        LWaitForCommand = false;
        FCommand = FString.Substring(LPosition, FPosition - LPosition).TrimEnd();
        int LIndex = 0;
        int LLength = FCommand.Length;
        for(; LIndex < LLength && !LSpaces.Contains(FCommand[LIndex]); LIndex++);

        if(LIndex < LLength)
        {
          FValue   = FCommand.Substring(LIndex + 1);
          FCommand = FCommand.Substring(0, LIndex);
        }
        else
          FValue = "";
        FEof   = (FPosition >= FLength);
        MoveToNextChar();
        return true;
      }
      else if(!LInLiteral && !LWaitForCommand && LCurrComment != TCommentMethod.None)
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

      if(FCurrChar == (char)0)
      { 
        SkipText(ref LPosition, false);
        FCommand = "";
        FValue   = "";
        FEof   = true;
        FLine  = LLine;
        return true;
      }
      else if (!LWaitForCommand && FCurrChar == '{' && FNextChar == '$')
      {
        LCurrComment = TCommentMethod.Braces;
        LWaitForCommand = true;
        SkipText(ref LPosition, false);
        FBol = (FPriorChar == (Char)13 || FPriorChar == (Char)10);
        MoveToNextChar(); MoveToNextChar(); LPosition++;
        FLine = LLine;
        continue;
      }
      else if(!LInLiteral && !LWaitForCommand && LCurrComment == TCommentMethod.None)
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
        }

      if(!LInLiteral && FCurrChar == FLiteral)
        LInLiteral = true;
      else if(LInLiteral && FCurrChar == FLiteral)
        if(FNextChar == FLiteral)
          MoveToNextChar();
        else
          LInLiteral = false;

      MoveToNextChar();
    }
  }
}
