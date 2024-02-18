
using System.Threading.Tasks;
using System.Threading;
using CommunityToolkit.Maui.Storage;
using System.Xml;
using OpenAI_API.Chat;
using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using CRToolKit.Models;
using CRToolKit.DTO;
using System.Runtime.ConstrainedExecution;
using Microsoft.Extensions.Configuration;
using Microsoft.Maui.Controls;
using Newtonsoft.Json;
using DocumentFormat.OpenXml.Drawing;
using CRToolKit.Classes;

namespace CRToolKit;

public partial class MainPage : ContentPage
{
    int count = 0;
    string folderPath = null;
    int totalFile = 0;
    int processedFile = 0;
    int errorFile = 0;
    int waitMillisecond = 45000;
    Conversation conversation;
    List<Candidate> Candidates = new List<Candidate>();
    IConfiguration Config;
    string seprator1 = "|";
    string seprator2 = "#-#";
    string seprator3 = "#Ø#";
    string seprator4 = "#,#";
    string seprator5 = "#:#";
    
    public MainPage(IConfiguration config)
    {
        InitializeComponent();
        Config = config;
    }
    void Theme_Toggled(System.Object sender, Microsoft.Maui.Controls.ToggledEventArgs e)
    {
        if (Application.Current.UserAppTheme == AppTheme.Dark)
        {
            Application.Current.UserAppTheme = AppTheme.Light;
            Preferences.Set("APPTHEME", "Light");

        }
        else
        {
            Application.Current.UserAppTheme = AppTheme.Dark;
            Preferences.Set("APPTHEME", "Dark");
        }
    }

    private async void btnBrowse_Clicked(object sender, EventArgs e)
    {
        var result = await FolderPicker.Default.PickAsync(CancellationToken.None);
        if (result.IsSuccessful)
        {
            lblPath.Text = result.Folder.Path;
            folderPath = result.Folder.Path;
            totalFile = Directory.GetFiles(folderPath).Where(x => x.ToLower().EndsWith(".docx")).Count();
            lblStatus.Text = String.Concat(totalFile.ToString() + " files found!");
        }
    }

    private async void Process_Clicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(folderPath))
            return;

        btnProcess.IsEnabled = false;
        processedFile = 0;
        errorFile = 0;
        foreach (var file in Directory.GetFiles(folderPath))
        {
            var candidate = await Process(file);
            if (candidate != null)
            {
                Candidates.Add(candidate);
                processedFile++;
            }
            else
            {
                errorFile++;
            }
            lblStatus.Text = String.Concat("Processed ", processedFile.ToString(), " out of ", totalFile.ToString() + " (", errorFile.ToString(), " errors!)");
        }

        lblStatus.Text = "Done! " + lblStatus.Text;

        btnProcess.IsEnabled = true;

    }

    private async Task<Candidate> Process(string file)
    {
        Candidate candidate = new Candidate();
        StringBuilder stringBuilder;
        try
        {
            using (WordprocessingDocument wordprocessingDocument = WordprocessingDocument.Open(file, false))
            {
                NameTable nameTable = new NameTable();
                XmlNamespaceManager xmlNamespaceManager = new XmlNamespaceManager(nameTable);
                xmlNamespaceManager.AddNamespace("w", "http://schemas.openxmlformats.org/wordprocessingml/2006/main");

                string wordprocessingDocumentText;
                using (StreamReader streamReader = new StreamReader(wordprocessingDocument.MainDocumentPart.GetStream()))
                {
                    wordprocessingDocumentText = streamReader.ReadToEnd();
                }

                stringBuilder = new StringBuilder(wordprocessingDocumentText.Length);

                XmlDocument xmlDocument = new XmlDocument(nameTable);
                xmlDocument.LoadXml(wordprocessingDocumentText);

                XmlNodeList paragraphNodes = xmlDocument.SelectNodes("//w:p", xmlNamespaceManager);
                foreach (XmlNode paragraphNode in paragraphNodes)
                {
                    XmlNodeList textNodes = paragraphNode.SelectNodes(".//w:t | .//w:tab | .//w:br", xmlNamespaceManager);
                    foreach (XmlNode textNode in textNodes)
                    {
                        switch (textNode.Name)
                        {
                            case "w:t":
                                stringBuilder.Append(textNode.InnerText);
                                break;

                            case "w:tab":
                                stringBuilder.Append("\t");
                                break;

                            case "w:br":
                                stringBuilder.Append("\v");
                                break;
                        }
                    }

                    stringBuilder.Append(Environment.NewLine);
                }
            }

            var txt = stringBuilder.ToString();

            var sbPrompt = new StringBuilder();
            sbPrompt.AppendLine("You are a text processing agent working with candidates resumes.");
            sbPrompt.AppendLine("Extract specified values from the source text.");
            sbPrompt.AppendLine("Return answer as JSON object with following fields:");
            sbPrompt.AppendLine("\"Name\" <string>");
            sbPrompt.AppendLine("\"Email\" <string>");
            sbPrompt.AppendLine("\"Phone\" <string>");
            sbPrompt.AppendLine("\"Qualification\" [{}]");
            sbPrompt.AppendLine("\"WorkHistory\" [{}]");
            sbPrompt.AppendLine("\"Summary\" <string>");
            sbPrompt.AppendLine("Do not infer any data based on previous training, strictly use only source text given below as input.");
            sbPrompt.AppendLine("========");
            sbPrompt.AppendLine(txt);
            sbPrompt.AppendLine("========");

            conversation = ChatGPT.CreateConversation();
            conversation.AppendUserInput(sbPrompt.ToString());
            var returnChat = await SendRequestAsync();

            var candidateDTO  = JsonConvert.DeserializeObject<CandidateDTO>(returnChat);
            await GetWork(conversation, candidateDTO.WorkHistory);


            //Get local template file path from config
            var templateFilePath = Config.GetRequiredSection("Settings:TemplateFilePath").Value.ToString();
            var redultDirpath = Config.GetRequiredSection("Settings:RedultDirpath").Value.ToString(); ;
            candidate.FilePath = TransformFile(candidateDTO, templateFilePath, redultDirpath);

            //await Task.Delay(waitMillisecond);

        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        return candidate;
    }

    async Task GetWork(Conversation chat, List<WorkHistoryDTO> workList)
    {
        foreach (var work in workList)
        {
            try
            {
               await GetWorkDetails(chat, work);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("TooManyRequests")){
                    await Task.Delay(waitMillisecond);
                    await GetWorkDetails(chat, work);
                }
            }

        }
    }

    async Task GetWorkDetails(Conversation chat, WorkHistoryDTO work)
    {

        var sbPrompt = new StringBuilder();
        var tenure = work.Dates != null ? work.Dates : work.Duration;
        tenure = (tenure == null) ? string.Empty : tenure;
        sbPrompt.AppendLine(string.Concat("Please tell bullet point summary of candidate's work in ", work.Company, " during ", tenure));
        conversation.AppendUserInput(sbPrompt.ToString());
        work.Summary = await SendRequestAsync();
    }


    CandidateDTO GetData(string[] candidateArr )
    {
        var candidateDTO = new CandidateDTO();
        candidateDTO.Name = GetVal(candidateArr, "Name", seprator2);
        candidateDTO.Email = GetVal(candidateArr, "Email", seprator2);
        candidateDTO.Phone = GetVal(candidateArr, "Phone", seprator2);
        candidateDTO.Summary = GetVal(candidateArr, "Summary", seprator2);

        return candidateDTO;
    }

    String TransformFile(CandidateDTO candidate, string templateFilePath, string redultDirpath)
    {
        int ctr = 1;
        var targetFileName = candidate.Name;
        string targetFilePath = null;
        targetFilePath = redultDirpath + "\\" + targetFileName + ".docx";

        if (!Directory.Exists(redultDirpath))
        {
            Directory.CreateDirectory(redultDirpath);
        }

        while (File.Exists(targetFilePath))
        {
            ctr += 1;
            targetFilePath = redultDirpath + "\\" + targetFileName + " - " + ctr.ToString() + ".docx";
        }
        if (!File.Exists(targetFilePath))
        {
            File.Copy(templateFilePath, targetFilePath);
        }

        using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(targetFilePath, true))
        {
            string? docText = null;


            if (wordDoc.MainDocumentPart is null)
            {
                throw new ArgumentNullException("MainDocumentPart and/or Body is null.");
            }

            TransformWorkHistory(candidate, wordDoc.MainDocumentPart.Document.Body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>().Where(p => p.InnerText.Contains("SWUMM")).ToList(),wordDoc);

            using (StreamReader sr = new StreamReader(wordDoc.MainDocumentPart.GetStream()))
            {
                docText = sr.ReadToEnd();
            }

            docText = docText.Replace("NME", candidate.Name);
            docText = docText.Replace("EML", candidate.Email);
            docText = docText.Replace("MOB", candidate.Phone);
            docText = docText.Replace("SUMM", candidate.Summary);
            docText = TransformWorkHistory(candidate, docText);

            using (StreamWriter sw = new StreamWriter(wordDoc.MainDocumentPart.GetStream(FileMode.Create)))
            {
                sw.Write(docText);
            }

            return targetFilePath;
        }
    }

    void TransformWorkHistory(CandidateDTO candidate, List<DocumentFormat.OpenXml.Wordprocessing.Paragraph> paras, WordprocessingDocument doc)
    {

        var desig = "DESG";
        var summ = "SWUMM";
        int ctrWork = candidate.WorkHistory.Count;
        var writer = new SimpleDocWriter();
        for (int ctr = 0; ctr < ctrWork; ctr++)
        {
            var workText = candidate.WorkHistory[ctr].Summary.Split("\n- ");
            if (workText!=null && workText.Length>0 && workText[0].StartsWith("-"))
            {
                workText[0] = workText[0][1..].Trim();
            }
            var para = paras[ctr];
            writer.AddBulletListInPara(doc, para, workText.ToList(), summ + (ctr+1).ToString());
        }


    }
    string TransformWorkHistory(CandidateDTO candidate, string docText)
    {
        int ctr = 1;
        var desig = "DESG";
        var summ = "SWUMM";
        foreach(var work in candidate.WorkHistory)
        {
            var desigCur = desig + ctr.ToString();
            var summCur = summ + ctr.ToString();
            docText = docText.Replace(desigCur, work.Position);
            docText = docText.Replace(summCur, work.Summary);
            ctr += 1;
        }
        return docText;
    }

    private async System.Threading.Tasks.Task<string> SendRequestAsync()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        Task<string> task = conversation.GetResponseFromChatbotAsync();
        await System.Threading.Tasks.Task.WhenAny(task, System.Threading.Tasks.Task.Delay(Timeout.Infinite, cancellationTokenSource.Token));
        cancellationTokenSource.Token.ThrowIfCancellationRequested();
        return await task;
    }

    private String GetVal(string[] vals, string key, string seprator)
    {
        var keyStr = vals.Where(x=>x.Contains(key + seprator)).FirstOrDefault();
        if (String.IsNullOrEmpty(keyStr))
            return string.Empty;

        return keyStr.Split(seprator)[1].Trim();
    }
}



/* Old Code
     private async Task<Candidate> Process(string file)
    {
        Candidate candidate = new Candidate();
        StringBuilder stringBuilder;
        try
        {
            using (WordprocessingDocument wordprocessingDocument = WordprocessingDocument.Open(file, false))
            {
                NameTable nameTable = new NameTable();
                XmlNamespaceManager xmlNamespaceManager = new XmlNamespaceManager(nameTable);
                xmlNamespaceManager.AddNamespace("w", "http://schemas.openxmlformats.org/wordprocessingml/2006/main");

                string wordprocessingDocumentText;
                using (StreamReader streamReader = new StreamReader(wordprocessingDocument.MainDocumentPart.GetStream()))
                {
                    wordprocessingDocumentText = streamReader.ReadToEnd();
                }

                stringBuilder = new StringBuilder(wordprocessingDocumentText.Length);

                XmlDocument xmlDocument = new XmlDocument(nameTable);
                xmlDocument.LoadXml(wordprocessingDocumentText);

                XmlNodeList paragraphNodes = xmlDocument.SelectNodes("//w:p", xmlNamespaceManager);
                foreach (XmlNode paragraphNode in paragraphNodes)
                {
                    XmlNodeList textNodes = paragraphNode.SelectNodes(".//w:t | .//w:tab | .//w:br", xmlNamespaceManager);
                    foreach (XmlNode textNode in textNodes)
                    {
                        switch (textNode.Name)
                        {
                            case "w:t":
                                stringBuilder.Append(textNode.InnerText);
                                break;

                            case "w:tab":
                                stringBuilder.Append("\t");
                                break;

                            case "w:br":
                                stringBuilder.Append("\v");
                                break;
                        }
                    }

                    stringBuilder.Append(Environment.NewLine);
                }
            }

            var txt = stringBuilder.ToString();

            var sbPrompt = new StringBuilder();
            var sbSystemMsg = new StringBuilder();
            sbPrompt.AppendLine("text:Following is text retrived from a resume - ");
            sbPrompt.AppendLine(txt);
            sbPrompt.AppendLine("question_to_ask: Provide following information - ");
            sbPrompt.AppendLine("Name");
            sbPrompt.AppendLine("Email");
            sbPrompt.AppendLine("Phone");
            sbPrompt.AppendLine("Qualification");
            sbPrompt.AppendLine("Work History");
            sbPrompt.AppendLine("Summary");

            sbSystemMsg.AppendLine("Desired format:");
            sbSystemMsg.AppendLine(string.Concat("Name", seprator2, "James Collingwood"));
            sbSystemMsg.Append(seprator1);
            sbSystemMsg.AppendLine(string.Concat("Email", seprator2, "james.collingwood@gmail.com"));
            sbSystemMsg.Append(seprator1);
            sbSystemMsg.AppendLine(string.Concat("Phone", seprator2, "703-561-2213"));
            sbSystemMsg.Append(seprator1);
            sbSystemMsg.AppendLine(string.Concat("Qualification", seprator2, "Degree", seprator5, "B.A", seprator4, " Percentage", seprator5, " 59%", seprator4, " University", seprator5, "Osaka State University", seprator3));
            sbSystemMsg.Append(seprator1);
            sbSystemMsg.AppendLine(string.Concat("Work History", seprator2, "Company", seprator5, " ABC Ltd.", seprator4, " Designation", seprator5, " Sr. Software Engineer", seprator4, " Period", seprator5, " May, 2005 - June, 2008", seprator4));
            sbSystemMsg.AppendLine(string.Concat("Work History", seprator2, "Company", seprator5, " Microsoft INC Ltd.", seprator4, " Designation", seprator5, " Sr. Engineering Manager", seprator4, " Period", seprator5, " June, 2008 - August, 2009", seprator4));
            sbSystemMsg.Append(seprator1);
            sbSystemMsg.AppendLine(string.Concat("Summary" , seprator2, "17+ years of Experience in designing and developing applications using Microsoft Technology Stack (C#, VB.Net,.Net, .Net Core, ASP.Net MVC, ASP.Net, SQL Server) , Javascript Frameworks(Angular, D3JS) and Cloud (Azure)"));

            conversation = ChatGPT.CreateConversation();
            conversation.AppendUserInput(sbPrompt.ToString());
            conversation.AppendSystemMessage(sbSystemMsg.ToString());
            var returnChat = (await SendRequestAsync()).Split(seprator1);

            var candidateDTO = GetData(returnChat);

            //Get local template file path from config
            var templateFilePath = Config.GetRequiredSection("Settings:TemplateFilePath").Value.ToString();
            var redultDirpath = Config.GetRequiredSection("Settings:RedultDirpath").Value.ToString(); ;

            candidate.FilePath = TransformFile(candidateDTO, templateFilePath, redultDirpath);

            await Task.Delay(waitMillisecond);

        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        return candidate;
    }
 */


