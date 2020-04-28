using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using System.Xml;
using System.Windows;
using HtmlAgilityPack;

namespace SIMAIL.Classes.Email
{
    public class c_EmailBody
    {
        public string Text { get; set; }
        public cEmail_Signature Signature { get; set; }
        public string TextReponse { get; set; }


        public c_EmailBody()
        {
            this.Signature = new cEmail_Signature();
        }

        /// <summary>
        ///     ''' Renvoie La chaine de caractère devant composer le paramètre Body de la classe MailMessage sans Template
        ///     ''' </summary>
        ///     ''' <returns></returns>
        public string Generer()
        {
            try
            {
                this.Text = "";

                return this.Text;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return null;
            }
        }

        /// <summary>
        ///     ''' Renvoie la template
        ///     ''' </summary>
        ///     ''' <returns></returns>
        public string ToHTML()
        {
            string vHTMLDoc;

            vHTMLDoc = "<html>" + "\r\n";
            vHTMLDoc = vHTMLDoc + "<head>" + "\r\n";
            vHTMLDoc = vHTMLDoc + "<meta http-equiv=\"content-type\" content=\"text/html; charset=UTF-8\">";
            vHTMLDoc = vHTMLDoc + "</head>" + "\r\n";
            vHTMLDoc = vHTMLDoc + "<body>" + "\r\n";
            vHTMLDoc = vHTMLDoc + this.Text.Replace("\r\n", "\r\n" + "</br>") + "\r\n";
            vHTMLDoc = vHTMLDoc + this.Signature.html;
            vHTMLDoc = vHTMLDoc + "</body>" + "\r\n";
            vHTMLDoc = vHTMLDoc + "</html>" + "\r\n";

            return vHTMLDoc;
        }
        public string ToHTMLReponse()
        {
            String vHTMLDoc;
            vHTMLDoc = "<html>" + "\r\n";
            vHTMLDoc = vHTMLDoc + "<head>" + "\r\n";
            vHTMLDoc = vHTMLDoc + "<meta http-equiv=\"content-type\" content=\"text/html; charset=UTF-8\">";
            vHTMLDoc = vHTMLDoc + "</head>" + "\r\n";
            vHTMLDoc = vHTMLDoc + "<body>" + "\r\n";
            vHTMLDoc = vHTMLDoc + this.Text.Replace("\r\n", "\r\n" + "</br>") + "\r\n";
            vHTMLDoc = vHTMLDoc + this.TextReponse.Replace("\r\n", "\r\n" + "</br>") + "\r\n";
            vHTMLDoc = vHTMLDoc + "</body>" + "\r\n";
            vHTMLDoc = vHTMLDoc + "</html>" + "\r\n";

            return vHTMLDoc;
        }

        public string convertTextFromHtml(String pHtml)
        {
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(pHtml);

                StringWriter sw = new StringWriter();
                ConvertTo(doc.DocumentNode, sw);
                sw.Flush();
                return sw.ToString();
        }

        public void ConvertTo(HtmlNode node, TextWriter outText)
        {
            string html;
            switch (node.NodeType)
            {
                case HtmlNodeType.Comment:
                    // don't output comments
                    break;

                case HtmlNodeType.Document:
                    ConvertContentTo(node, outText);
                    break;

                case HtmlNodeType.Text:
                    // script and style must not be output
                    string parentName = node.ParentNode.Name;
                    if ((parentName == "script") || (parentName == "style"))
                        break;

                    // get text
                    html = ((HtmlTextNode)node).Text;

                    // is it in fact a special closing node output as text?
                    if (HtmlNode.IsOverlappedClosingElement(html))
                        break;

                    // check the text is meaningful and not a bunch of whitespaces
                    if (html.Trim().Length > 0)
                    {
                        outText.Write(HtmlEntity.DeEntitize(html));
                    }
                    break;

                case HtmlNodeType.Element:
                    switch (node.Name)
                    {
                        case "p":
                            // treat paragraphs as crlf
                            outText.Write("\r\n");
                            break;
                    }

                    if (node.HasChildNodes)
                    {
                        ConvertContentTo(node, outText);
                    }
                    break;

            }
        }

        private void ConvertContentTo(HtmlNode node, TextWriter outText)
        {
            foreach (HtmlNode subnode in node.ChildNodes)
            {
                ConvertTo(subnode, outText);
            }
        }

    }
}