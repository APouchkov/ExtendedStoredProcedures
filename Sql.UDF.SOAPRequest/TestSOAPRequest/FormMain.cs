using System;
using System.Data.SqlTypes;
using System.IO;
using System.Windows.Forms;
using System.Xml;

namespace TestSOAPRequest
{
    public partial class FormMain : Form
    {
        public FormMain()
        {
            InitializeComponent();
        }

        private void buttonSend_Click(object sender, EventArgs e)
        {
            richTextBoxResponseHeader.Text = "";
            richTextBoxResponseBody.Text = "";
            var header = richTextBoxHeader.Text.Length > 0 ? new SqlXml(XmlReader.Create(new StringReader(richTextBoxHeader.Text))) : SqlXml.Null;
            var body = new SqlXml(XmlReader.Create(new StringReader(richTextBoxBody.Text)));

            Sql.UDF.SOAPRequest.SOAP.SOAPRequest((SqlString)textBoxUri.Text, ref header, ref body);

            richTextBoxResponseHeader.Text = header.IsNull == false ? header.Value : "" ;
            richTextBoxResponseBody.Text = body.IsNull == false ? body.Value : "";
        }
    }
}
