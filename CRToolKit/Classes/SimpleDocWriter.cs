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
                newPara.InnerXml = para.InnerXml.Replace(searchtext, sentence.EscapeXmlCharacters());

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
            {
                replacetext = "NULL";
            }
            if (replacetext.Contains("I'm sorry"))
                return;

            para.InnerXml = para.InnerXml.Replace(searchtext, replacetext.EscapeXmlCharacters());
        }

        public void ReplacePara(WordprocessingDocument document, Paragraph para, string searchtext, string replacetext, string previosText, int space)
        {
            if (string.IsNullOrEmpty(replacetext))
                return;
            if (replacetext.Contains("I'm sorry"))
                return;

            var textRuns = para.Descendants<Text>().ToList();
            foreach (var textRun in textRuns)
            {
                textRun.Space = SpaceProcessingModeValues.Preserve;
            }
            document.Save();

            para.InnerXml = para.InnerXml.Replace(searchtext, replacetext);

            bool isMultiline = false;
            while (isMultiline == false)
            {
                searchtext = replacetext;
                replacetext = new string(' ', 2) + replacetext;
                para.InnerXml = para.InnerXml.Replace(searchtext, replacetext);
                isMultiline = IsParagraphMultiLine(document, para);
            }
        }

        bool IsParagraphMultiLine(WordprocessingDocument doc, Paragraph para)
        {
            bool isMultiLine = false;
            var paragraph = para;

            var text = paragraph.InnerText;

            // Approximate the number of lines based on the text length and average line height
            var numLines = Math.Ceiling((double)text.Length / 155); // Assuming an average line length of 80 characters

            // Check if the paragraph spans more than one line
            if (numLines > 1)
            {
                isMultiLine = true;
            }


            return isMultiLine;
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

        public void RemoveText(WordprocessingDocument doc, string searchtext)
        {
            var body = doc.MainDocumentPart.Document.Body;

            foreach (var paragraph in body.Descendants<Paragraph>())
            {
                foreach (var run in paragraph.Descendants<Run>())
                {
                    foreach (var text in run.Descendants<Text>())
                    {
                        if (text.Text.Contains(searchtext))
                        {
                            // Replace the target word with an empty string
                            text.Text = text.Text.Replace(searchtext, "");
                        }
                    }
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

        internal void RemoveEmptyBulletPoints(WordprocessingDocument doc)
        {

            var body = doc.MainDocumentPart.Document.Body;

            foreach (var paragraph in body.Descendants<Paragraph>())
            {
                // Check if the paragraph has a bullet
                var bullet = paragraph.Descendants<NumberingProperties>().FirstOrDefault();
                if (bullet != null)
                {
                    var run = paragraph.Descendants<Run>().FirstOrDefault();
                    if (run != null && run.Descendants<Text>().All(t => string.IsNullOrWhiteSpace(t.Text)))
                    {
                        // If all text in the paragraph is whitespace or empty, remove the paragraph
                        paragraph.Remove();
                    }
                }
            }

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
