using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;

[Serializable]
[Microsoft.SqlServer.Server.SqlUserDefinedType(Format.Native)]
public struct TWho : INullable
{
  private Int32 _Person;
  private Int32 _Representative;

  public override string ToString()
  {
    return (IsNull ? "" : (_Person > 0 ? _Person.ToString() : "") + ":" + ((_Person > 0) && (_Representative > 0) ? _Representative.ToString() : ""));
  }

  public bool IsNull
  {
    get
    {
      // Поместите здесь свой код
      return false;
    }
  }

  public static TWho New()
  {
      return Null;
  }

  public static TWho Create(SqlInt32 APerson, SqlInt32 ARepresentative)
  {
    if (APerson.IsNull || APerson.Value <= 0 )
      return Null;

    TWho h = new TWho();
    h._Person         = APerson.Value;
    if (ARepresentative.IsNull || ARepresentative.Value <= 0)
      h._Representative = 0;
    else
      h._Representative = ARepresentative.Value;
    return h;
  }

  public static TWho Null
  {
    get
    {
      TWho h = new TWho();
      return h;
    }
  }

  public static TWho Parse(SqlString s)
  {
    if (s.IsNull)
      return Null;

    String value = s.Value;
    if (value == "")
      return Null;

    int Pos = value.IndexOf(":");
    if (Pos < 0)
      return Null;

    TWho u = new TWho();
    if (Pos > 0)
      u._Person         = Int32.Parse(value.Substring(0, Pos));
    else
      u._Person         = 0;

    if (Pos < value.Length - 1)
      u._Representative = Int32.Parse(value.Substring(Pos + 1));
    else
      u._Representative = 0;

    return u;
  }

  public SqlInt32 Person
  {
    get
    {
      return this._Person > 0 ? (SqlInt32)this._Person : SqlInt32.Null;
    }
    set
    {
      if (value.IsNull || value.Value <= 0)
      {
        this._Person = 0;
        this._Representative = 0;
      }
      else
        this._Person = value.Value;
    }
  }

  public SqlInt32 Representative
  {
    get
    {
      return this._Representative > 0 ? (SqlInt32)this._Representative : SqlInt32.Null;
    }
    set
    {
      this._Representative = value.Value;
    }
  }

  public bool Equals(TWho arg)
  {
    return (this._Person == arg._Person) && (this._Representative == arg._Representative);
  }
}
