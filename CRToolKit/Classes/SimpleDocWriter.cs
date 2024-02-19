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
using Microsoft.Maui.Graphics;
using DocumentFormat.OpenXml.EMMA;

namespace CRToolKit.Classes
{
    public class SimpleDocWriter : IDisposable
    {
        private MemoryStream _ms;
        private WordprocessingDocument _wordprocessingDocument;

        public SimpleDocWriter()
        {
            _ms = new MemoryStream();
            _wordprocessingDocument = WordprocessingDocument.Create(_ms, WordprocessingDocumentType.Document);
            var mainDocumentPart = _wordprocessingDocument.AddMainDocumentPart();
            Body body = new Body();
            mainDocumentPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document(body);
        }

        public void AddParagraph(string sentence)
        {
            List<Run> runList = ListOfStringToRunList(new List<string> { sentence });
            AddParagraph(runList);
        }
        public void AddParagraph(List<string> sentences)
        {
            List<Run> runList = ListOfStringToRunList(sentences);
            AddParagraph(runList);
        }

        public void AddParagraph(List<Run> runList)
        {
            var para = new Paragraph();
            foreach (Run runItem in runList)
            {
                para.AppendChild(runItem);
            }

            Body body = _wordprocessingDocument.MainDocumentPart.Document.Body;
            body.AppendChild(para);
        }

        public void AddBulletList(List<string> sentences)
        {
            var runList = ListOfStringToRunList(sentences);

            AddBulletList(runList);
        }


        public void AddBulletList(List<Run> runList)
        {
            // Introduce bulleted numbering in case it will be needed at some point
            NumberingDefinitionsPart numberingPart = _wordprocessingDocument.MainDocumentPart.NumberingDefinitionsPart;
            if (numberingPart == null)
            {
                numberingPart = _wordprocessingDocument.MainDocumentPart.AddNewPart<NumberingDefinitionsPart>("NumberingDefinitionsPart001");
                Numbering element = new Numbering();
                element.Save(numberingPart);
            }

            // Insert an AbstractNum into the numbering part numbering list.  The order seems to matter or it will not pass the 
            // Open XML SDK Productity Tools validation test.  AbstractNum comes first and then NumberingInstance and we want to
            // insert this AFTER the last AbstractNum and BEFORE the first NumberingInstance or we will get a validation error.
            var abstractNumberId = numberingPart.Numbering.Elements<AbstractNum>().Count() + 1;
            var abstractLevel = new Level(new NumberingFormat() { Val = NumberFormatValues.Bullet }, new LevelText() { Val = "·" }) { LevelIndex = 0 };
            var abstractNum1 = new AbstractNum(abstractLevel) { AbstractNumberId = abstractNumberId };

            if (abstractNumberId == 1)
            {
                numberingPart.Numbering.Append(abstractNum1);
            }
            else
            {
                AbstractNum lastAbstractNum = numberingPart.Numbering.Elements<AbstractNum>().Last();
                numberingPart.Numbering.InsertAfter(abstractNum1, lastAbstractNum);
            }

            // Insert an NumberingInstance into the numbering part numbering list.  The order seems to matter or it will not pass the 
            // Open XML SDK Productity Tools validation test.  AbstractNum comes first and then NumberingInstance and we want to
            // insert this AFTER the last NumberingInstance and AFTER all the AbstractNum entries or we will get a validation error.
            var numberId = numberingPart.Numbering.Elements<NumberingInstance>().Count() + 1;
            NumberingInstance numberingInstance1 = new NumberingInstance() { NumberID = numberId };
            AbstractNumId abstractNumId1 = new AbstractNumId() { Val = abstractNumberId };
            numberingInstance1.Append(abstractNumId1);

            if (numberId == 1)
            {
                numberingPart.Numbering.Append(numberingInstance1);
            }
            else
            {
                var lastNumberingInstance = numberingPart.Numbering.Elements<NumberingInstance>().Last();
                numberingPart.Numbering.InsertAfter(numberingInstance1, lastNumberingInstance);
            }

            Body body = _wordprocessingDocument.MainDocumentPart.Document.Body;

            foreach (Run runItem in runList)
            {
                // Create items for paragraph properties
                var numberingProperties = new NumberingProperties(new NumberingLevelReference() { Val = 0 }, new NumberingId() { Val = numberId });
                var spacingBetweenLines1 = new SpacingBetweenLines() { After = "0" };  // Get rid of space between bullets
                var indentation = new Indentation() { Left = "720", Hanging = "360" };  // correct indentation 

                ParagraphMarkRunProperties paragraphMarkRunProperties1 = new ParagraphMarkRunProperties();
                RunFonts runFonts1 = new RunFonts() { Ascii = "Symbol", HighAnsi = "Symbol" };
                paragraphMarkRunProperties1.Append(runFonts1);

                // create paragraph properties
                var paragraphProperties = new ParagraphProperties(numberingProperties, spacingBetweenLines1, indentation, paragraphMarkRunProperties1);

                // Create paragraph 
                var newPara = new Paragraph(paragraphProperties);

                // Add run to the paragraph
                newPara.AppendChild(runItem);

                // Add one bullet item to the body
                body.AppendChild(newPara);
            }
        }

        public void AddBulletListInPara(WordprocessingDocument document,Paragraph para, List<string> sentences, string searchtext)
        {
            Body body = document.MainDocumentPart.Document.Body;
            Paragraph inserAfter = null;

            foreach(var sentence in sentences)
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
        }

        public void ReplacePara(Paragraph para, string searchtext,string replacetext)
        {
            if (replacetext.Contains("I'm sorry"))
                return;

            para.InnerXml = para.InnerXml.Replace(searchtext, replacetext);
        }

        public void Dispose()
        {
            CloseAndDisposeOfDocument();
            if (_ms != null)
            {
                _ms.Dispose();
                _ms = null;
            }
        }

        public MemoryStream SaveToStream()
        {
            _ms.Position = 0;
            return _ms;
        }

        public void SaveToFile(string fileName)
        {
            if (_wordprocessingDocument != null)
            {
                CloseAndDisposeOfDocument();
            }

            if (_ms == null)
                throw new ArgumentException("This object has already been disposed of so you cannot save it!");

            using (var fs = File.Create(fileName))
            {
                _ms.WriteTo(fs);
            }
        }

        private void CloseAndDisposeOfDocument()
        {
            if (_wordprocessingDocument != null)
            {
                //_wordprocessingDocument.;
                _wordprocessingDocument.Dispose();
                _wordprocessingDocument = null;
            }
        }

        private static List<Run> ListOfStringToRunList(List<string> sentences)
        {
            var runList = new List<Run>();
            foreach (string item in sentences)
            {
                //var newRun = new Run(new RunProperties(
                //    new RunStyle() { Val = "ListParagraph" },
                //    new NoProof()), new Text(item));
                var newRun = new Run();
               newRun.AppendChild(new Text(item));
                runList.Add(newRun);
            }

            return runList;
        }
    }
}
