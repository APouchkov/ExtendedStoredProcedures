using System;
using System.IO;
using System.Data;
using System.Data.SqlClient;
using Microsoft.SqlServer.Server;
using System.Xml;
using System.Data.SqlTypes;
using System.Collections;
using System.Collections.Generic;

public partial class XmlRecords
{
  [SqlFunction(Name = "XML Records::Update", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
  public static String XMLRecordsUpdate(SqlXml Xml, SqlXml UpdateXml, String ATag, String AIdentity, Boolean ARaiseIfNotFound)
  {
    if(String.IsNullOrWhiteSpace(ATag))
      throw new System.Exception("Parameter @Tag can't be null or empty");

    if(String.IsNullOrWhiteSpace(AIdentity))
      throw new System.Exception("Parameter @Identity can't be null or empty");

    if (UpdateXml.IsNull) return (Xml.IsNull ? null : Xml.Value);

    XmlDocument UpdateDoc = new XmlDocument();
    XmlNode UpdateRoot = UpdateDoc.CreateElement("RECORDS");
    UpdateRoot.InnerXml = UpdateXml.Value;

    XmlDocument Doc = new XmlDocument();
    XmlNode Root = Doc.CreateElement("RECORDS");
    if (Xml != null && !Xml.IsNull)
      Root.InnerXml = Xml.Value;

    for(int I = 0; I < UpdateRoot.ChildNodes.Count; I++)
    {
      XmlNode UpdateNode = UpdateRoot.ChildNodes[I];
      if(UpdateNode.LocalName != ATag) continue;

      XmlAttribute Attribute = UpdateNode.Attributes["ACTION"];
      if (Attribute == null) continue;

      String LAction = Attribute.Value;
      if (String.IsNullOrEmpty(LAction)) continue;

      if (LAction != "I" && LAction != "U" && LAction != "D")
        throw new System.Exception("Incorrect attribute @ACTION = " + LAction + " can't be null");

      Attribute = UpdateNode.Attributes[AIdentity];
      if (Attribute == null)
        throw new System.Exception("Attribute @" + AIdentity + " can't be null");

      String LIdentityValue = Attribute.Value;
      if (String.IsNullOrEmpty(LIdentityValue))
        throw new System.Exception("Attribute @" + AIdentity + " can't be empty");

      if(LAction == "D")
      {
        XmlNode Node = Root.SelectSingleNode(ATag + "[@" + AIdentity + "=\"" + LIdentityValue + "\"]");
        if (Node == null)
        {
          if(ARaiseIfNotFound)
            throw new System.Exception("Record " + ATag + "[@" + AIdentity + "=\"" + LIdentityValue + "\"] not found");
        }
        else
          Root.RemoveChild(Node);
      }
      else
      {
        Attribute = UpdateNode.Attributes["FIELDS"];
        if (Attribute == null) continue;
        String LFields = Attribute.Value;
        if (String.IsNullOrEmpty(LFields)) continue;

        if(LAction == "I")
        {
          XmlNode Node = Root.SelectSingleNode(ATag + "[@" + AIdentity + "=\"" + LIdentityValue + "\"]");
          if (Node != null)
          {
            if(ARaiseIfNotFound)
              throw new System.Exception("Duplicate record " + ATag + "[@" + AIdentity + "=\"" + LIdentityValue + "\"]");
          }
          else
          {
            Node = Doc.CreateElement(ATag);
            Root.AppendChild(Node);
            Boolean LIdentityPresent = false;
            foreach(String LName in LFields.Split(','))
            {
              Attribute = UpdateNode.Attributes[LName];
              if (Attribute != null)
              {
                if(LName == AIdentity) LIdentityPresent = true;
                XmlAttribute SAttribute = Doc.CreateAttribute(LName);
                SAttribute.Value = Attribute.Value;
                Node.Attributes.Append(SAttribute);
              }
            }
            if(!LIdentityPresent)
              throw new System.Exception("Cannot insert record if '" + AIdentity + "' field not in FIELDS attribute");
          }
        } else //if(LAction == "U")
        {
          XmlNode Node = Root.SelectSingleNode(ATag + "[@" + AIdentity + "=\"" + LIdentityValue + "\"]");
          if (Node == null)
          {
            if(ARaiseIfNotFound)
              throw new System.Exception("Record " + ATag + "[@" + AIdentity + "=\"" + LIdentityValue + "\"] not found");
          }
          else
          {
            foreach(String LName in LFields.Split(','))
            {
              Attribute = UpdateNode.Attributes[LName];
              if (Attribute != null)
              {
                XmlAttribute SAttribute = Node.Attributes[LName];
                if(SAttribute == null)
                {
                  SAttribute = Doc.CreateAttribute(LName);
                  SAttribute.Value = Attribute.Value;
                  Node.Attributes.Append(SAttribute);
                }
                else
                  SAttribute.Value = Attribute.Value;
              }
              else
              {
                XmlAttribute SAttribute = Node.Attributes[LName];
                if(SAttribute != null)
                  Node.Attributes.Remove(SAttribute);
              }
            }
          }
        }
      }
    }

    return Root.ChildNodes.Count == 0 ? null : Root.InnerXml;
  }

  [SqlFunction(Name = "XML Records::Cleanup", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
  public static String XMLRecordsCleanup(SqlXml UpdateXml, String ATag, String AIdentity)
  {
    if (UpdateXml.IsNull) return null;

    if(String.IsNullOrWhiteSpace(ATag))
      throw new System.Exception("Parameter @Tag can't be null or empty");

    if(String.IsNullOrWhiteSpace(AIdentity))
      throw new System.Exception("Parameter @Identity can't be null or empty");

    List<String> LIdentities = new List<String>();

    XmlDocument UpdateDoc = new XmlDocument();
    XmlNode UpdateRoot = UpdateDoc.CreateElement("RECORDS");
    UpdateRoot.InnerXml = UpdateXml.Value;

    for(int I = 0; I < UpdateRoot.ChildNodes.Count;)
    {
      XmlNode UpdateNode = UpdateRoot.ChildNodes[I];
      if(UpdateNode.LocalName != ATag)
      {
        UpdateRoot.RemoveChild(UpdateNode);
        continue;
      }

      XmlAttribute FIELDS = UpdateNode.Attributes["FIELDS"];
      if (FIELDS == null)
      {
        UpdateRoot.RemoveChild(UpdateNode);
        continue;
      }

      String LFields = FIELDS.Value;
      if (String.IsNullOrEmpty(LFields)) 
      {
        UpdateRoot.RemoveChild(UpdateNode);
        continue;
      }

      UpdateNode.Attributes.Remove(FIELDS);
      Boolean LFieldsUnlimited = (LFields == "*");
      if(!LFieldsUnlimited) LFields = ',' + LFields + ',';
      Boolean LIdentityFound = false;

      for(int J = 0; J < UpdateNode.Attributes.Count;)
      {
        XmlAttribute Attribute = UpdateNode.Attributes[J];
        if(Attribute.Name == AIdentity)
        {
          if (String.IsNullOrEmpty(Attribute.Value))
            throw new System.Exception("Attribute @" + AIdentity + " can't be empty");
          if(LIdentities.IndexOf(Attribute.Value) >= 0)
            throw new System.Exception("Duplicate record [@" + AIdentity + "=\"" + Attribute.Value + "\"]");
          LIdentities.Add(Attribute.Value);
          LIdentityFound = true;
          J++;
        }
        else if(!LFieldsUnlimited && LFields.IndexOf(',' + Attribute.Name + ',') < 0)
          UpdateNode.Attributes.Remove(Attribute);
        else
          J++;
      }

      if(!LIdentityFound)
        throw new System.Exception("Attribute @" + AIdentity + " can't be null");
      if(UpdateNode.Attributes.Count == 0)
        UpdateRoot.RemoveChild(UpdateNode);
      else
      {
        while(UpdateNode.HasChildNodes) UpdateNode.RemoveChild(UpdateNode.ChildNodes[0]);
        I++;
      }
    }

    return UpdateRoot.ChildNodes.Count == 0 ? null : UpdateRoot.InnerXml;
  }

  [SqlFunction(Name = "XML Records::Prepare", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
  public static String XMLRecordsPrepare(SqlXml UpdateXml, String ATag, String AIdentity, String AFilter)
  {
    if (UpdateXml == null || UpdateXml.IsNull) return null;
    if(String.IsNullOrWhiteSpace(AFilter)) return null;

    if(String.IsNullOrWhiteSpace(ATag))
      throw new System.Exception("Parameter @Tag can't be null or empty");

    if(String.IsNullOrWhiteSpace(AIdentity))
      throw new System.Exception("Parameter @Identity can't be null or empty");

    if(AFilter == "*") return UpdateXml.Value;
    AFilter = "," + AFilter + ",";

    XmlDocument UpdateDoc = new XmlDocument();
    XmlNode UpdateRoot = UpdateDoc.CreateElement("RECORDS");
    UpdateRoot.InnerXml = UpdateXml.Value;

    for(int I = 0; I < UpdateRoot.ChildNodes.Count;)
    {
      XmlNode UpdateNode = UpdateRoot.ChildNodes[I];
      if(UpdateNode.LocalName != ATag)
      {
        UpdateRoot.RemoveChild(UpdateNode);
        continue;
      }
/*
      XmlAttribute FIELDS = UpdateNode.Attributes["FIELDS"];
      if (FIELDS == null)
      {
        UpdateRoot.RemoveChild(UpdateNode);
        continue;
      }

      String LFields = FIELDS.Value;
      if (String.IsNullOrEmpty(LFields)) 
      {
        UpdateRoot.RemoveChild(UpdateNode);
        continue;
      }
*/
      if(UpdateNode.Attributes.Count == 0)
        UpdateRoot.RemoveChild(UpdateNode);
      else
      {
        XmlAttribute IdentityAttribute = UpdateNode.Attributes[AIdentity];
        if (IdentityAttribute == null || AFilter.IndexOf("," + IdentityAttribute.Value + ",") < 0)
          UpdateRoot.RemoveChild(UpdateNode);
        else
          I++;
      }
    }

    return UpdateRoot.ChildNodes.Count == 0 ? null : UpdateRoot.InnerXml;
  }

  [SqlFunction(Name = "XML Record::New Attribute", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
  public static String XMLRecordNewAttribute(SqlXml DataXml, String ATag, String AAttributeName, String AAttributeValue)
  {
    if (DataXml.IsNull || AAttributeValue == null) return null;

    if(String.IsNullOrWhiteSpace(ATag))
      throw new System.Exception("Parameter @Tag can't be null or empty");

    if(String.IsNullOrWhiteSpace(AAttributeName))
      throw new System.Exception("Parameter @AttributeName can't be null or empty");

    XmlDocument UpdateDoc = new XmlDocument();
    XmlNode UpdateRoot = UpdateDoc.CreateElement("RECORDS");
    UpdateRoot.InnerXml = DataXml.Value;

    for(int I = 0; I < UpdateRoot.ChildNodes.Count; I++)
    {
      XmlNode UpdateNode = UpdateRoot.ChildNodes[I];
      if(UpdateNode.LocalName == ATag)
      {
        XmlAttribute Attribute = UpdateDoc.CreateAttribute(AAttributeName);
        Attribute.Value = AAttributeValue;
        UpdateNode.Attributes.Append(Attribute);
      }
    }

    return UpdateRoot.ChildNodes.Count == 0 ? null : UpdateRoot.InnerXml;
  }

  [SqlFunction(Name = "XML Record::InnerXml", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, IsDeterministic = true)]
  public static String XMLRecordInnerXml(SqlXml AXml, String ATag)
  {
    if(String.IsNullOrWhiteSpace(ATag))
      throw new System.Exception("Parameter @Tag can't be null or empty");

    if (AXml.IsNull)
      return null;

    XmlReader LReader = AXml.CreateReader();
    while(LReader.Read())
    {
      if(LReader.NodeType == XmlNodeType.Element)
      {
        if (LReader.Name == ATag)
        {
          String LInnerXml = LReader.ReadInnerXml();
          if (String.IsNullOrWhiteSpace(LInnerXml))
            return null;
          else
            return LInnerXml;
        }
        else
          LReader.Skip();
      }
    }

    return null;
  }

  private struct XMLRecordAttribute
  {
    public String  Name;
    public String  Value;
    public Int16   Index;
  }

  [SqlFunction(FillRowMethodName = "XMLRecordAttributesRow", DataAccess = DataAccessKind.None, SystemDataAccess = SystemDataAccessKind.None, TableDefinition = "[Index] SmallInt, [Name] SysName, [Value] NVarChar(Max)", IsDeterministic = true)]
  public static IEnumerable XMLRecordAttributes(SqlXml AXml)
  {
    if (AXml.IsNull) yield break;
    XmlReader LReader = AXml.CreateReader();
    XMLRecordAttribute LAttribute;
    if (LReader.Read() && LReader.NodeType == XmlNodeType.Element)
    {
      LAttribute.Index = 0;
      while (LReader.MoveToNextAttribute())
      {
        LAttribute.Index++;
        LAttribute.Name  = LReader.Name;
        LAttribute.Value = LReader.Value;
        yield return LAttribute;
      }
    }
  }

  public static void XMLRecordAttributesRow(Object ARow, out Int16 AIndex, out String AName, out String AValue)
  {
    AIndex  = ((XMLRecordAttribute)ARow).Index;
    AName   = ((XMLRecordAttribute)ARow).Name;
    AValue  = ((XMLRecordAttribute)ARow).Value;
  }
};

