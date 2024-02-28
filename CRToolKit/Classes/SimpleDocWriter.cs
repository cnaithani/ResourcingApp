using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Reflection.Metadata;


namespace ResourcingToolKit.Classes
{
    public class SimpleDocWriter 
    {

        public void AddBulletListInPara(WordprocessingDocument document, Paragraph para, List<string> sentences, string searchtext)
        {
            Body body = document.MainDocumentPart.Document.Body;
            Paragraph inserAfter = null;

            foreach (var sentence in sentences)
            {
                if (sentence.Contains("I'm sorry"))
                    continue;

                Paragraph newPara = new Paragraph();
                if (inserAfter == null)
                {
                    inserAfter = para;
                }
                newPara.InnerXml = para.InnerXml.Replace(searchtext, sentence);

                var parent = inserAfter.Parent;
                parent.InsertAfter(newPara, inserAfter);
                inserAfter = newPara;
            }
            body.RemoveChild(para);

            var textRuns = para.Descendants<Text>().ToList();
            foreach (var textRun in textRuns)
            {
                textRun.Space = SpaceProcessingModeValues.Preserve;
            }
            document.Save();
        }

        public void ReplacePara(Paragraph para, string searchtext, string replacetext)
        {
            if (string.IsNullOrEmpty(replacetext))
                return;
            if (replacetext.Contains("I'm sorry"))
                return;

            para.InnerXml = para.InnerXml.Replace(searchtext, replacetext);
        }

        public void ReplacePara(WordprocessingDocument document, Paragraph para, string searchtext, string replacetext, string previosText, int space)
        {
            if (string.IsNullOrEmpty(replacetext))
                return;
            if (replacetext.Contains("I'm sorry"))
                return;

            space = space - previosText.Length;
            if (space < 1) space = 1;
            replacetext =  new string(' ', space) + replacetext;
            para.InnerXml = para.InnerXml.Replace(searchtext, replacetext);

            var textRuns = para.Descendants<Text>().ToList();
            foreach (var textRun in textRuns)
            {
                textRun.Space = SpaceProcessingModeValues.Preserve;
            }
            document.Save();

        }

        public void RemovePara(WordprocessingDocument document, string searchtext)
        {
            MainDocumentPart mainpart = document.MainDocumentPart;
            IEnumerable<OpenXmlElement> elems = mainpart.Document.Body.Descendants().ToList();

            foreach (OpenXmlElement elem in elems)
            {
                if (elem is Text && elem.InnerText.Contains(searchtext))
                {
                    Run run = (Run)elem.Parent;
                    Paragraph p = (Paragraph)run.Parent;
                    p.RemoveAllChildren();
                    p.Remove();
                }
            }
        }

        public void RemoveEmptyLines(WordprocessingDocument document)
        {
            var body = document.MainDocumentPart.Document.Body;
            var emptyParagraphs = body.Elements<Paragraph>();
            bool previousWasEmpty = false;

            foreach (var paragraph in emptyParagraphs.ToList())
            {
                if (IsEmptyParagraph(paragraph))
                {
                    if (previousWasEmpty)
                    {
                        paragraph.Remove();
                    }
                    else
                    {
                        previousWasEmpty = true;
                    }
                }
                else
                {
                    previousWasEmpty = false;
                }
            }
            document.MainDocumentPart.Document.Save();
        }

        internal void RemoveParagraphsContainingText(WordprocessingDocument doc, string text1, string text2)
        {
            var body = doc.MainDocumentPart.Document.Body;
            var paragraphs = body.Descendants<Run>().ToList();
            Run previousParagraph = null;

            foreach (var paragraph in paragraphs)
            {
                if (IsParagraphContainingText(paragraph, text2))
                {
                    if (previousParagraph != null && IsParagraphContainingChar(previousParagraph, text1))
                    {
                        paragraph.Remove();
                        previousParagraph.Remove();
                        break;
                    }

                }
                previousParagraph = paragraph;
            }

            doc.MainDocumentPart.Document.Save();
        }

        bool IsParagraphContainingText(Run paragraph, string searchText)
        {
            return paragraph.InnerText.Contains(searchText);
        }

        bool IsParagraphContainingChar(Run paragraph, string searchText)
        {
            return paragraph.InnerXml.Contains(searchText);
        }

        bool IsEmptyParagraph(Paragraph paragraph)
        {
            return paragraph != null && string.IsNullOrEmpty(paragraph.InnerText.Trim());
        }

    }
}
