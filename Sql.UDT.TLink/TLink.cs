using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;

[Serializable]
[Microsoft.SqlServer.Server.SqlUserDefinedType(Format.Native)]
public struct TLink : INullable
{
  private Byte  _Type;
  private Int32 _Value;

  public override string ToString()
  {
    // Замените следующий код своим собственным
    return (IsNull ? "" : _Type + ":" + _Value);
  }

  public bool IsNull
  {
    get
    {
      // Поместите здесь свой код
      return false;
    }
  }

  public static TLink New()
  {
      return Null;
  }

  public static TLink CreateByType(SqlByte AType, SqlInt32 AValue)
  {
    if (AType.IsNull)
      return Null;

    TLink h = new TLink();
    h._Type = AType.Value;

    if (AValue.IsNull || AValue.Value <= 0)
      h._Value = 0;
    else
      h._Value = AValue.Value;

    return h;
  }

  public static TLink CreateByCode(SqlString ACode, SqlInt32 AValue)
  {
    if (ACode.IsNull)
      return Null;

    String CodeString = ACode.Value;
    if (CodeString.Length != 1) 
      throw new Exception("Invalid length for parameter 'Code'");
    TLink h = new TLink();
    h._Type = (byte)Char.ConvertToUtf32(CodeString.Substring(0, 1), 0);

    if (AValue.IsNull || AValue.Value <= 0)
      h._Value = 0;
    else
      h._Value = AValue.Value;

    return h;
  }

  public static TLink Null
  {
    get
    {
      TLink h = new TLink();
      return h;
    }
  }

  public static TLink Parse(SqlString s)
  {
    if (s.IsNull)
      return Null;

    String value = s.Value;
    if (value == "")
      return Null;

    int Pos = value.IndexOf(":");
    if (Pos < 0)
      return Null;

    TLink u = new TLink();
    u._Type = Byte.Parse(value.Substring(0, Pos));

    if (Pos < value.Length - 1)
      u._Value = Int32.Parse(value.Substring(Pos + 1));
    else
      u._Value = 0;

    return u;
  }

  public SqlByte Type
  {
    get
    {
      return this._Type;
    }
    set
    {
      this._Type = value.Value;
    }
  }

  public SqlString Code
  {
    get
    {
      return Char.ConvertFromUtf32(this._Type);
    }
    set
    {
      this._Type = (byte)Char.ConvertToUtf32(value.Value, 0);
    }
  }

  public SqlInt32 Value
  {
    get
    {
      return this._Value > 0 ? (SqlInt32)this._Value : SqlInt32.Null;
    }
    set
    {
      if (value.IsNull || value.Value <= 0)
        this._Value = 0;
      else
        this._Value = value.Value;
    }
  }

  public bool Equals(TLink arg)
  {
//    if (arg == null) return false;
    return (this._Type == arg._Type) && (this._Value == arg._Value);
  }
}
