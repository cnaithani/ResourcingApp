
using System.Threading.Tasks;
using System.Threading;
using CommunityToolkit.Maui.Storage;
using System.Xml;
using OpenAI_API.Chat;
using System.Text;
using DocumentFormat.OpenXml.Packaging;
using CRToolKit.Models;
using CRToolKit.DTO;
using System.Runtime.ConstrainedExecution;
using Microsoft.Extensions.Configuration;
using Microsoft.Maui.Controls;

namespace CRToolKit;

public partial class MainPage : ContentPage
{
    int count = 0;
    string folderPath = null;
    int totalFile = 0;
    int processedFile = 0;
    int errorFile = 0;
    int waitMillisecond = 1000;
    Conversation conversation;
    List<Candidate> Candidates = new List<Candidate>();
    IConfiguration Config;
    string seprator1 = "|";
    string seprator2 = "#-#";
    string seprator3 = "#Ø#";
    string seprator4 = "#:#";


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
            sbSystemMsg.AppendLine(string.Concat("Name", seprator2, "James Collingwood", seprator1) );
            sbSystemMsg.AppendLine(string.Concat("Email", seprator2, "james.collingwood@gmail.com", seprator1));
            sbSystemMsg.AppendLine(string.Concat("Phone", seprator2, "703-561-2213", seprator1));
            //sbSystemMsg.AppendLine(string.Concat("Qualification" , seprator2, "Degree",seprator4 ,"B.A, Percentage: 59%, University: Osaka State University", seprator3));
            //sbSystemMsg.AppendLine(string.Concat("Qualification", seprator2, "Intermidiate, Percentage: 59%, University: Osaka State University", seprator1));
            sbSystemMsg.AppendLine(string.Concat("Work History" , seprator2, "Company: ABC Ltd., Designation: Sr. Software Engineer, Period: May, 2005 - June, 2008 (3 Years 1 Month)" + Environment.NewLine + "Company: Microsoft Ltd.., Designation: Sr. Engineering Manager, Period: June, 2008 - August, 2009 (1 Years 2 Month)", seprator1));
            sbSystemMsg.AppendLine(string.Concat("Summary" , seprator2, "17+ years of Experience in designing and developing applications using Microsoft Technology Stack (C#, VB.Net,.Net, .Net Core, ASP.Net MVC, ASP.Net, SQL Server) , Javascript Frameworks(Angular, D3JS) and Cloud (Azure)", seprator1));

            conversation = ChatGPT.CreateConversation();
            conversation.AppendUserInput(sbPrompt.ToString());
            conversation.AppendSystemMessage(sbSystemMsg.ToString());
            var returnChat = (await SendRequestAsync()).Split(seprator1);

            candidate.Name = GetVal(returnChat,"Name", seprator2);
            candidate.Email = returnChat[1].Split(seprator2)[1].Trim();
            candidate.Phone = returnChat[2].Split(seprator2)[1].Trim();
            candidate.Modified = DateTime.Now;

            //conversation.AppendUserInput("question_to_ask: We require candidate with skills Software Engineering, C#, SQL, Javascript. How much skills of this candidate matches with desired skills in scale from 1 to 5 considering his/her skills mentioned in resume and compare with desired skills.");
            //conversation.AppendSystemMessage("1 is poor match, 5 is excellent match");
            //conversation.AppendSystemMessage("Desired format:" + "Software Engineering - 2,C# - 1 , SQL - 1, Javascript - 3");
            //var rating = await SendRequestAsync();
            //candidate.Rating = int.Parse(rating.Split("-")[1][1].ToString());

            //conversation.AppendUserInput("question_to_ask: What are key skills of candidate?");
            //conversation.AppendSystemMessage("Desired format: Software Engineering,C#,SQL,Javascript,Management,Leadership");
            //var rating = await SendRequestAsync();


            //Get local template file path from config
            var templateFilePath = Config.GetRequiredSection("Settings:TemplateFilePath").Value.ToString();
            var redultDirpath = Config.GetRequiredSection("Settings:RedultDirpath").Value.ToString(); ;

            var candidateDTO = new CandidateDTO();
            candidateDTO.Name = candidate.Name;
            candidateDTO.Email = candidate.Email;
            candidateDTO.Phone = candidate.Phone;
            candidateDTO.Summary = GetVal(returnChat, "Summary", seprator2); ;

            candidate.FilePath = TransformFile(candidateDTO, templateFilePath, redultDirpath);

            await Task.Delay(waitMillisecond);

        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        return candidate;
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

            using (StreamReader sr = new StreamReader(wordDoc.MainDocumentPart.GetStream()))
            {
                docText = sr.ReadToEnd();
            }

            docText = docText.Replace("NME", candidate.Name);
            docText = docText.Replace("EML", candidate.Email);
            docText = docText.Replace("MOB", candidate.Phone);
            docText = docText.Replace("SUMM", candidate.Summary);

            using (StreamWriter sw = new StreamWriter(wordDoc.MainDocumentPart.GetStream(FileMode.Create)))
            {
                sw.Write(docText);
            }

            return targetFilePath;
        }
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


