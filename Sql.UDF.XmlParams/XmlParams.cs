using System;
using System.IO;
using System.Data;
using System.Data.SqlClient;
using Microsoft.SqlServer.Server;
using System.Xml;
using System.Data.SqlTypes;

public partial class XmlParams
{
    private static void SetParamAttribute(XmlDocument Doc, XmlNode Node, String Name, String Value, Boolean Required)
    {
        XmlAttribute NodeAttribute = Node.Attributes[Name];
        if (Value == "" && !Required)
        {
            if (NodeAttribute != null) Node.Attributes.Remove(NodeAttribute);
            return;
        }
        if (NodeAttribute == null)
        {
            NodeAttribute = Doc.CreateAttribute(Name);
            Node.Attributes.Append(NodeAttribute);
        }
        NodeAttribute.Value = Value;
    }

    private static void SetParamNode(XmlDocument Doc, XmlNode Node, String Name, String Value, SqlString Type)
    {
        SetParamAttribute(Doc, Node, "VALUE", Value, true);
        if (Type.IsNull)
            SetParamAttribute(Doc, Node, "TYPE", "", false);
        else
            SetParamAttribute(Doc, Node, "TYPE", Type.Value, false);
    }

    private static bool IsNull(System.Object Value)
    {
        if (Value is DateTime || Value is TimeSpan)
            return false;
        else
            return (Value is System.DBNull || Value == null || ((INullable)Value).IsNull);
    }

    private static SqlString InternalParamAdd(String Xml, SqlString Name, System.Object Value, SqlString Type)
    {
      String ParamName = XmlConvert.DecodeName(Name.Value); //.ToUpper()
      XmlDocument Doc = new XmlDocument();

      XmlNode Root = Doc.CreateElement("PARAMS");
//      Doc.AppendChild(Root);

//      if (Xml == null || Xml.IsNull)
//        Doc.AppendChild(Doc.CreateElement("PARAMS"));
//      else
//        Doc.LoadXml(Xml.Value);

//        XmlNode Root = Doc.DocumentElement;
//        XmlNode Node = Doc.SelectSingleNode("PARAM[@NAME=\"" + ParamName + "\"]");

      if (Xml != null)
        Root.InnerXml = Xml;

      XmlNode Node = Root.SelectSingleNode("PARAM[@NAME=\"" + ParamName + "\"]");

     
      if (IsNull(Value))
      {
        if (Node != null) Root.RemoveChild(Node);
      }
      else
      {
        if (Node == null && Value != null)
        {
          Node = Doc.CreateElement("PARAM");
          Root.AppendChild(Node);
          XmlAttribute NodeAttribute = Doc.CreateAttribute("NAME");
          NodeAttribute.Value = XmlConvert.DecodeName(Name.ToString()); //.ToUpper()
          Node.Attributes.Append(NodeAttribute);
        }
        if (Value is SqlDateTime) SetParamNode(Doc, Node, ParamName, XmlConvert.ToString((DateTime)(SqlDateTime)Value, XmlDateTimeSerializationMode.RoundtripKind), Type.Value == "" ? "DateTime" : Type);
        else if (Value is DateTime) SetParamNode(Doc, Node, ParamName, XmlConvert.ToString((DateTime)Value, XmlDateTimeSerializationMode.RoundtripKind), Type.Value == "" ? "Date" : Type);
        else if (Value is SqlByte) SetParamNode(Doc, Node, ParamName, XmlConvert.ToString((Byte)(SqlByte)Value), Type.Value == "" ? "TinyInt" : Type);
        else if (Value is SqlInt64) SetParamNode(Doc, Node, ParamName, ((SqlInt64)Value).ToString(), Type.Value == "" ? "BigInt" : Type);
        else if (Value is SqlInt32) SetParamNode(Doc, Node, ParamName, XmlConvert.ToString((Int32)(SqlInt32)Value), Type.Value == "" ? "Int" : Type);
        else if (Value is SqlInt16) SetParamNode(Doc, Node, ParamName, XmlConvert.ToString((Int16)(SqlInt16)Value), Type.Value == "" ? "SmallInt" : Type);
        else if (Value is SqlMoney) SetParamNode(Doc, Node, ParamName, XmlConvert.ToString((Decimal)(SqlMoney)Value), Type.Value == "" ? "Money" : Type);
        else if (Value is SqlDecimal) SetParamNode(Doc, Node, ParamName, XmlConvert.ToString((Decimal)(SqlDecimal)Value), Type.Value == "" ? "Decimal" : Type);
        else if (Value is SqlDouble) SetParamNode(Doc, Node, ParamName, XmlConvert.ToString((Double)(SqlDouble)Value), Type.Value == "" ? "Double" : Type);
        else if (Value is SqlSingle) SetParamNode(Doc, Node, ParamName, XmlConvert.ToString((Single)(SqlSingle)Value), Type.Value == "" ? "Single" : Type);
        else if (Value is SqlString) SetParamNode(Doc, Node, ParamName, (String)(SqlString)Value, Type.Value == "" ? "String" : Type);
        else if (Value is SqlBoolean) SetParamNode(Doc, Node, ParamName, (Boolean)(SqlBoolean)Value ? "1" : "0", Type.Value == "" ? "Boolean" : Type);
        else if (Value is TimeSpan) SetParamNode(Doc, Node, ParamName, XmlConvert.ToString((TimeSpan)Value), Type.Value == "" ? "Time" : Type);
        else if (Value is SqlGuid) SetParamNode(Doc, Node, ParamName, XmlConvert.ToString((Guid)(SqlGuid)Value), Type.Value == "" ? "GUId" : Type);
        // SqlBytes TODO: ??? Save to CDATA ???            
        else
          throw new System.Exception("Not supported type : " + Value.GetType().ToString() + " for add parameter " + Name.ToString());
      }

//      MemoryStream MS = new MemoryStream();
//      Doc.Save(MS);
//      SqlXml Result = new SqlXml(MR);
//      return Result; 

      return new SqlString(Root.InnerXml);
    }

    private static string GetValueFromXMLAttribute(SqlXml Xml, SqlString Name, out String ValueType)
    {
      ValueType = null;
      if (Xml == null || Xml.IsNull) return null;

      XmlDocument Doc = new XmlDocument();
//      Doc.LoadXml(Xml.Value);
      XmlNode Root = Doc.CreateElement("PARAMS");
      Root.InnerXml = Xml.Value;

      XmlNode Node = null;
      XmlNode Attribute = null;
      String[] Names = Name.ToString().Split(','); //.ToUpper()
      int i = 0;
      while (Node == null && i < Names.Length)
      {
//            Node = Doc.SelectSingleNode("PARAMS/PARAM[@NAME=\"" + Names[i] + "\"]");
        Node = Root.SelectSingleNode("PARAM[@NAME=\"" + Names[i] + "\"]");
        i++;
      }
      if (Node == null)
          return null;
      else
      {
        Attribute = Node.Attributes["TYPE"];
        if (Attribute != null) ValueType = Attribute.Value.ToString(); else ValueType = null;
        Attribute = Node.Attributes["VALUE"];
        if (Attribute != null) return Attribute.Value.ToString(); else return null;
      }
    }

    [SqlFunction(Name = "XML Params::Add", DataAccess = DataAccessKind.None, IsDeterministic = true)]
    public static SqlString ParamAdd(SqlXml Xml, SqlString Name, System.Object Value)
    {
        return InternalParamAdd((Xml == null || Xml.IsNull ? null : Xml.Value), Name, Value, "");
    }

    [SqlFunction(Name = "XML Params::Add::Date", DataAccess = DataAccessKind.None, IsDeterministic = true)]
    public static SqlString ParamAddDate(SqlXml Xml, SqlString Name, SqlDateTime Value, SqlString Type)
    {
        if ((Value.IsNull) && (Type.IsNull || Type.Value == ""))
          return InternalParamAdd((Xml == null || Xml.IsNull ? null : Xml.Value), Name, Value.IsNull ? Value : Value.Value.Date, "date");
        return InternalParamAdd((Xml == null || Xml.IsNull ? null : Xml.Value), Name, Value.IsNull ? Value : Value.Value.Date, Type);
    }

    [SqlFunction(Name = "XML Params::Add::VarChar", DataAccess = DataAccessKind.None, IsDeterministic = true)]
    public static SqlString ParamAddVarchar(SqlXml Xml, SqlString Name, SqlString Value)
    {
        return InternalParamAdd((Xml == null || Xml.IsNull ? null : Xml.Value), Name, Value, "");
    }

    [SqlFunction(Name = "XML Params::Delete", DataAccess = DataAccessKind.None, IsDeterministic = true)]
    public static SqlString ParamDelete(SqlXml Xml, SqlString Name)
    {
      String ParamName = XmlConvert.DecodeName(Name.ToString()); //.ToUpper()
      XmlDocument Doc = new XmlDocument();

      if (Xml == null || Xml.IsNull)
        return null;
//      else
//        Doc.LoadXml(Xml.Value);

      XmlNode Root = Doc.CreateElement("PARAMS");
      Root.InnerXml = Xml.Value;
//      Doc.AppendChild(Root);

//      XmlNode Root = Doc.DocumentElement;
      XmlNode Node = Root.SelectSingleNode("PARAM[@NAME=\"" + ParamName + "\"]");
      if (Node != null) Root.RemoveChild(Node);

//      MemoryStream MS = new MemoryStream();
//      Doc.Save(MS);
//      SqlXml Result = new SqlXml(MS);
//      return Result;
      return Root.InnerXml;
    }

    [SqlFunction(Name = "XML Params::Exists", DataAccess = DataAccessKind.None, IsDeterministic = true)]
    public static SqlBoolean ParamExists(SqlXml Xml, SqlString Name)
    {
      if (Xml == null || Xml.IsNull) return new SqlBoolean(false);
      XmlDocument Doc = new XmlDocument();

//      Doc.LoadXml(Xml.Value);
      XmlNode Root = Doc.CreateElement("PARAMS");
      Root.InnerXml = Xml.Value;

      XmlNode Node = null;
      String[] Names = Name.ToString().Split(','); //.ToUpper()
      int i = 0;
      while (Node == null && i < Names.Length)
      {
//            Node = Doc.SelectSingleNode("PARAMS/PARAM[@NAME=\"" + Names[i] + "\"]");
        Node = Root.SelectSingleNode("PARAM[@NAME=\"" + Names[i] + "\"]");
        i++;
      }
      if (Node == null)
        return new SqlBoolean(false);
      else
        return new SqlBoolean(true);
    }

    private static String GetValueFromXML(SqlXml Xml, SqlString Name)
    {
      if (Xml == null || Xml.IsNull) return null;
      XmlDocument Doc = new XmlDocument();

//      Doc.LoadXml(Xml.Value);
      XmlNode Root = Doc.CreateElement("PARAMS");
      Root.InnerXml = Xml.Value;

      XmlNode Node = null;
      XmlNode Attribute = null;
      String[] Names = Name.ToString().Split(','); //.ToUpper()
      int i = 0;
      while (Node == null && i < Names.Length)
      {
//            Node = Doc.SelectSingleNode("PARAMS/PARAM[@NAME=\"" + Names[i] + "\"]");
        Node = Root.SelectSingleNode("PARAM[@NAME=\"" + Names[i] + "\"]");
        i++;
      }
      if (Node == null)
        return null;
      else
      {
        Attribute = Node.Attributes["VALUE"];
        if (Attribute != null) return Attribute.Value.ToString(); else return null;
      }
    }

    [SqlFunction(Name = "XML Param::VarChar", DataAccess = DataAccessKind.None, IsDeterministic = true)]
    public static SqlString ParamVarchar(SqlXml Xml, SqlString Name)
    {
      return new SqlString(GetValueFromXML(Xml, Name));
    }

    [SqlFunction(Name = "XML Param::Bit", DataAccess = DataAccessKind.None, IsDeterministic = true)]
    public static SqlBoolean ParamBit(SqlXml Xml, SqlString Name)
    {
      String Value = GetValueFromXML(Xml, Name);
      if (Value == null)
        return new SqlBoolean();
      else
        try
        {
          return new SqlBoolean(XmlConvert.ToBoolean(Value));
        }
        catch (Exception Error)
        {
          throw new System.Exception("Error convert Param = \"" + Name.Value.ToString() + "\" Value = \"" + Value.ToString() + "\" to Boolean: " + Error.Message);
        }
    }

    [SqlFunction(Name = "XML Param::Int", DataAccess = DataAccessKind.None, IsDeterministic = true)]
    public static SqlInt32 ParamInt(SqlXml Xml, SqlString Name)
    {
        String Value = GetValueFromXML(Xml, Name);
        if (Value == null)
          return new SqlInt32();
        else
          try
          {
            return new SqlInt32(XmlConvert.ToInt32(Value));
          }
          catch (Exception Error)
          {
            throw new System.Exception("Error convert Param = \"" + Name.Value.ToString() + "\" Value = \"" + Value.ToString() + "\" to Int: " + Error.Message);
          }

    }

    [SqlFunction(Name = "XML Param::Date", DataAccess = DataAccessKind.None, IsDeterministic = true)]
    public static DateTime? ParamDateTime(SqlXml Xml, SqlString Name)
    {
      String Value = GetValueFromXML(Xml, Name);
      if (Value == null)
        return null;
      else
        try
        {
          return XmlConvert.ToDateTime(Value, XmlDateTimeSerializationMode.RoundtripKind);
        }
        catch (Exception Error)
        {
          throw new System.Exception("Error convert Param = \"" + Name.Value + "\" Value = \"" + Value + "\" to DateTime: " + Error.Message);
        }
    }

    [SqlFunction(Name = "XML Param::DateTime", DataAccess = DataAccessKind.None, IsDeterministic = true)]
    public static SqlDateTime ParamDateTimeTemp(SqlXml Xml, SqlString Name)
    {
      String ValueType;
      String Value = GetValueFromXMLAttribute(Xml, Name, out ValueType);
      if (Value == null) return new SqlDateTime();
      SqlDateTime Result;
      try
      {
        Result = new SqlDateTime(XmlConvert.ToDateTime(Value, XmlDateTimeSerializationMode.RoundtripKind));
      }
      catch (Exception Error)
      {
        throw new System.Exception("Error convert Param = \"" + Name.Value.ToString() + "\" Value = \"" + Value.ToString() + "\" to DateTime: " + Error.Message);
      }
      if (ValueType != null && ValueType != "")
      {
        // year, month, day, hour, minute, second
        switch (ValueType)
        {
          case "1": return Result.Value.Date.AddDays(1);
          default: return Result;
        }
      }
      return Result;
    }

    [SqlFunction(Name = "XML Param::Time", DataAccess = DataAccessKind.None, IsDeterministic = true)]
    public static TimeSpan? ParamTime(SqlXml Xml, SqlString Name)
    {
      String Value = GetValueFromXML(Xml, Name);
      if (Value == null)
        return null;
      else
        try
        {
            DateTime DateTimeValue = XmlConvert.ToDateTime(Value, XmlDateTimeSerializationMode.RoundtripKind);
            return new TimeSpan(0, DateTimeValue.Hour, DateTimeValue.Minute, DateTimeValue.Second, DateTimeValue.Millisecond);
        }
        catch (Exception Error)
        {
            throw new System.Exception("Error convert Param = \"" + Name.Value.ToString() + "\" Value = \"" + Value.ToString() + "\" to Time: " + Error.Message);
        }
    }

    //[SqlFunction(Name = "XML Param::Period Begin", DataAccess = DataAccessKind.None, IsDeterministic = true)]
    //public static SqlDateTime ParamPeriodBegin(SqlXml Xml, SqlString Name)
    //{
    //    String ValueType;
    //    String Value = GetValueFromXMLAttribute(Xml, Name, out ValueType);
    //    if (Value == null) return new SqlDateTime();
    //    SqlDateTime Result;
    //    try
    //    {
    //        Result = new SqlDateTime(XmlConvert.ToDateTime(Value, XmlDateTimeSerializationMode.RoundtripKind));
    //    }
    //    catch (Exception Error)
    //    {
    //        throw new System.Exception("Error convert Param = \"" + Name.Value.ToString() + "\" Value = \"" + Value.ToString() + "\" to DateTime: " + Error.Message);
    //    }
    //    if (ValueType != null && ValueType != "")
    //    {
    //        // year, month, day, hour, minute, second
    //        switch (ValueType)
    //        {
    //            case "0": return Result.Value.Date;
    //            case "1": return Result.Value.Date;
    //            case "date": return Result.Value.Date;
    //            case "datetime": return Result.Value.Date;
    //            // case "datetime": return Result.Value.Date.AddHours(Result.Value.Hour).AddMinutes(Result.Value.Minute).AddSeconds(Result.Value.Second);
    //            default: throw new System.Exception("Unknown type for parameter " + Name.ToString() + " : \"" + ValueType + "\"");
    //        }
    //    }
    //    return Result;
    //}

    //[SqlFunction(Name = "XML Param::Period End", DataAccess = DataAccessKind.None, IsDeterministic = true)]
    //public static SqlDateTime ParamPeriodEnd(SqlXml Xml, SqlString Name)
    //{
    //    String ValueType;
    //    String Value = GetValueFromXMLAttribute(Xml, Name, out ValueType);
    //    if (Value == null) return new SqlDateTime();
    //    SqlDateTime Result;
    //    try
    //    {
    //        Result = new SqlDateTime(XmlConvert.ToDateTime(Value, XmlDateTimeSerializationMode.RoundtripKind));
    //    }
    //    catch (Exception Error)
    //    {
    //        throw new System.Exception("Error convert Param = \"" + Name.Value.ToString() + "\" Value = \"" + Value.ToString() + "\" to DateTime: " + Error.Message);
    //    }
    //    if (ValueType != null && ValueType != "")
    //    {
    //        // year, month, day, hour, minute, second
    //        switch (ValueType)
    //        {
    //            case "0": return Result.Value.Date;
    //            case "1": return Result.Value.Date.AddDays(1);
    //            case "date": return Result.Value.Date.AddDays(1);
    //            case "datetime": return Result.Value.Date.AddDays(1);
    //            // case "datetime": return Result.Value.Date.AddHours(Result.Value.Hour).AddMinutes(Result.Value.Minute).AddSeconds(Result.Value.Second + 1);
    //            default: throw new System.Exception("Unknown type for parameter " + Name.ToString() + " : \"" + ValueType + "\"");
    //        }
    //    }
    //    return Result;
    //}
};

