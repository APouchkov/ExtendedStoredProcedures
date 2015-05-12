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
    [SqlFunction(Name = "XML Records::Update", DataAccess = DataAccessKind.None, IsDeterministic = true)]
    public static SqlString XMLRecordsUpdate(SqlXml Xml, SqlXml UpdateXml, SqlString Tag, SqlString Identity, SqlBoolean RaiseIfNotFound)
    {
      if(Tag == null || Tag.IsNull)
        throw new System.Exception("Parameter @Tag can't be null");
      string LTag = Tag.ToString();
      if(String.IsNullOrEmpty(LTag))
        throw new System.Exception("Parameter @Tag can't be empty");

      if(Identity == null || Identity.IsNull)
        throw new System.Exception("Parameter @Identity can't be null");
      string LIdentity = Identity.ToString();
      if(String.IsNullOrEmpty(LIdentity))
        throw new System.Exception("Parameter @Identity can't be empty");

      Boolean LRaiseIfNotFound = RaiseIfNotFound.IsTrue;

      if (UpdateXml == null || UpdateXml.IsNull) return (Xml == null || Xml.IsNull ? null : Xml.Value);

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
        if(UpdateNode.LocalName != LTag) continue;

        XmlAttribute Attribute = UpdateNode.Attributes["ACTION"];
        if (Attribute == null) continue;
        string LAction = Attribute.Value;
        if (String.IsNullOrEmpty(LAction)) continue;
        if (LAction != "I" && LAction != "U" && LAction != "D")
          throw new System.Exception("Incorrect attribute @ACTION = " + LAction + " can't be null");

        Attribute = UpdateNode.Attributes[LIdentity];
        if (Attribute == null)
          throw new System.Exception("Attribute @" + LIdentity + " can't be null");
        string LIdentityValue = Attribute.Value;
        if (String.IsNullOrEmpty(LIdentityValue))
          throw new System.Exception("Attribute @" + LIdentity + " can't be empty");

        if(LAction == "D")
        {
          XmlNode Node = Root.SelectSingleNode(LTag + "[@" + LIdentity + "=\"" + LIdentityValue + "\"]");
          if (Node == null)
          {
            if(LRaiseIfNotFound)
              throw new System.Exception("Record " + LTag + "[@" + LIdentity + "=\"" + LIdentityValue + "\"] not found");
          }
          else
            Root.RemoveChild(Node);
        } else
        {
          Attribute = UpdateNode.Attributes["FIELDS"];
          if (Attribute == null) continue;
          string LFields = Attribute.Value;
          if (String.IsNullOrEmpty(LFields)) continue;

          if(LAction == "I")
          {
            XmlNode Node = Root.SelectSingleNode(LTag + "[@" + LIdentity + "=\"" + LIdentityValue + "\"]");
            if (Node != null)
            {
              if(LRaiseIfNotFound)
                throw new System.Exception("Duplicate record " + LTag + "[@" + LIdentity + "=\"" + LIdentityValue + "\"]");
            }
            else
            {
              Node = Doc.CreateElement(LTag);
              Root.AppendChild(Node);
              Boolean LIdentityPresent = false;
              foreach(string LName in LFields.Split(','))
              {
                Attribute = UpdateNode.Attributes[LName];
                if (Attribute != null)
                {
                  if(LName == LIdentity) LIdentityPresent = true;
                  XmlAttribute SAttribute = Doc.CreateAttribute(LName);
                  SAttribute.Value = Attribute.Value;
                  Node.Attributes.Append(SAttribute);
                }
              }
              if(!LIdentityPresent)
                throw new System.Exception("Cannot insert record if '" + LIdentity + "' field not in FIELDS attribute");
            }
          } else //if(LAction == "U")
          {
            XmlNode Node = Root.SelectSingleNode(LTag + "[@" + LIdentity + "=\"" + LIdentityValue + "\"]");
            if (Node == null)
            {
              if(LRaiseIfNotFound)
                throw new System.Exception("Record " + LTag + "[@" + LIdentity + "=\"" + LIdentityValue + "\"] not found");
            }
            else
            {
              foreach(string LName in LFields.Split(','))
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

      return Root.ChildNodes.Count == 0 ? null : new SqlString(Root.InnerXml);
    }

    [SqlFunction(Name = "XML Records::Cleanup", DataAccess = DataAccessKind.None, IsDeterministic = true)]
    public static SqlString XMLRecordsCleanup(SqlXml UpdateXml, SqlString Tag, SqlString Identity)
    {
      if (UpdateXml == null || UpdateXml.IsNull) return null;

      if(Tag == null || Tag.IsNull)
        throw new System.Exception("Parameter @Tag can't be null");
      string LTag = Tag.ToString();
      if(String.IsNullOrEmpty(LTag))
        throw new System.Exception("Parameter @Tag can't be empty");

      if(Identity == null || Identity.IsNull)
        throw new System.Exception("Parameter @Identity can't be null");
      string LIdentity = Identity.ToString();
      if(String.IsNullOrEmpty(LIdentity))
        throw new System.Exception("Parameter @Identity can't be empty");

      List<string> LIdentities = new List<string>();

      XmlDocument UpdateDoc = new XmlDocument();
      XmlNode UpdateRoot = UpdateDoc.CreateElement("RECORDS");
      UpdateRoot.InnerXml = UpdateXml.Value;

      for(int I = 0; I < UpdateRoot.ChildNodes.Count;)
      {
        XmlNode UpdateNode = UpdateRoot.ChildNodes[I];
        if(UpdateNode.LocalName != LTag)
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

        string LFields = FIELDS.Value;
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
          if(Attribute.Name == LIdentity)
          {
            if (String.IsNullOrEmpty(Attribute.Value))
              throw new System.Exception("Attribute @" + LIdentity + " can't be empty");
            if(LIdentities.IndexOf(Attribute.Value) >= 0)
              throw new System.Exception("Duplicate record [@" + LIdentity + "=\"" + Attribute.Value + "\"]");
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
          throw new System.Exception("Attribute @" + LIdentity + " can't be null");
        if(UpdateNode.Attributes.Count == 0)
          UpdateRoot.RemoveChild(UpdateNode);
        else
        {
          while(UpdateNode.HasChildNodes) UpdateNode.RemoveChild(UpdateNode.ChildNodes[0]);
          I++;
        }
      }

      return UpdateRoot.ChildNodes.Count == 0 ? null : new SqlString(UpdateRoot.InnerXml);
    }

    [SqlFunction(Name = "XML Records::Prepare", DataAccess = DataAccessKind.None, IsDeterministic = true)]
    public static SqlString XMLRecordsPrepare(SqlXml UpdateXml, SqlString Tag, SqlString Identity, SqlString Filter)
    {
      if (UpdateXml == null || UpdateXml.IsNull) return null;
      if(Filter == null || Filter.IsNull) return null;

      if(Tag == null || Tag.IsNull)
        throw new System.Exception("Parameter @Tag can't be null");
      string LTag = Tag.ToString();
      if(String.IsNullOrEmpty(LTag))
        throw new System.Exception("Parameter @Tag can't be empty");

      if(Identity == null || Identity.IsNull)
        throw new System.Exception("Parameter @Identity can't be null");
      string LIdentity = Identity.ToString();
      if(String.IsNullOrEmpty(LIdentity))
        throw new System.Exception("Parameter @Identity can't be empty");

      string LFilter = Filter.ToString();
      if(LFilter == "") return null;
      if(LFilter == "*") return UpdateXml.Value;
      LFilter = "," + LFilter + ",";

      XmlDocument UpdateDoc = new XmlDocument();
      XmlNode UpdateRoot = UpdateDoc.CreateElement("RECORDS");
      UpdateRoot.InnerXml = UpdateXml.Value;

      for(int I = 0; I < UpdateRoot.ChildNodes.Count;)
      {
        XmlNode UpdateNode = UpdateRoot.ChildNodes[I];
        if(UpdateNode.LocalName != LTag)
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

        string LFields = FIELDS.Value;
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
          XmlAttribute IdentityAttribute = UpdateNode.Attributes[LIdentity];
          if (IdentityAttribute == null || LFilter.IndexOf("," + IdentityAttribute.Value + ",") < 0)
            UpdateRoot.RemoveChild(UpdateNode);
          else
            I++;
        }
      }

      return UpdateRoot.ChildNodes.Count == 0 ? null : new SqlString(UpdateRoot.InnerXml);
    }

    [SqlFunction(Name = "XML Records::New Attribute", DataAccess = DataAccessKind.None, IsDeterministic = true)]
    public static SqlString XMLRecordsNewAttribute(SqlXml DataXml, SqlString Tag, SqlString AttributeName, SqlString AttributeValue)
    {
      if (DataXml == null || DataXml.IsNull || AttributeValue == null || AttributeValue.IsNull) return null;

      if(Tag == null || Tag.IsNull)
        throw new System.Exception("Parameter @Tag can't be null");
      string LTag = Tag.ToString();
      if(String.IsNullOrEmpty(LTag))
        throw new System.Exception("Parameter @Tag can't be empty");

      if(AttributeName == null || AttributeName.IsNull)
        throw new System.Exception("Parameter @AttributeName can't be null");
      string LAttributeName = AttributeName.ToString();
      if(String.IsNullOrEmpty(LAttributeName))
        throw new System.Exception("Parameter LAttributeName can't be empty");
      string LAttributeValue = AttributeValue.ToString();

      XmlDocument UpdateDoc = new XmlDocument();
      XmlNode UpdateRoot = UpdateDoc.CreateElement("RECORDS");
      UpdateRoot.InnerXml = DataXml.Value;

      for(int I = 0; I < UpdateRoot.ChildNodes.Count; I++)
      {
        XmlNode UpdateNode = UpdateRoot.ChildNodes[I];
        if(UpdateNode.LocalName == LTag)
        {
          XmlAttribute Attribute = UpdateDoc.CreateAttribute(LAttributeName);
          Attribute.Value = LAttributeValue;
          UpdateNode.Attributes.Append(Attribute);
        }
      }

      return UpdateRoot.ChildNodes.Count == 0 ? null : new SqlString(UpdateRoot.InnerXml);
    }

    [SqlFunction(Name = "XML Records::InnerXml", DataAccess = DataAccessKind.None, IsDeterministic = true)]
    public static SqlString XMLRecordsInnerXml(SqlXml Xml, SqlString Tag)
    {
      if(Tag == null || Tag.IsNull)
        throw new System.Exception("Parameter @Tag can't be null");

      if (Xml == null || Xml.IsNull)
        return null;

      XmlDocument Doc = new XmlDocument();
      Doc.LoadXml(Xml.Value);

      if(Doc.ChildNodes[0].LocalName == Tag.Value)
      {
        string InnerXml = Doc.ChildNodes[0].InnerXml; //.TrimEnd(' ');
        if(string.IsNullOrEmpty(InnerXml))
          return null;
        else
          return new SqlString();
      }
      else
        return null;
        // new SqlString(Xml.Value);
        //throw new System.Exception("Invalid @Tag = " + Doc.ChildNodes[0].LocalName);
    }
};

