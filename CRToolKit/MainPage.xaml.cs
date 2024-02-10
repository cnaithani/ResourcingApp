
using System.Threading.Tasks;
using System.Threading;
using CommunityToolkit.Maui.Storage;
using System.Xml;
using OpenAI_API.Chat;
using System.Text;
using DocumentFormat.OpenXml.Packaging;
using CRToolKit.Models;

namespace CRToolKit;

public partial class MainPage : ContentPage
{
    int count = 0;
    string folderPath = null;
    int totalFile = 0;
    int processedFile = 0;
    int errorFile = 0;
    Conversation conversation;
    List<Candidate> Candidates = new List<Candidate>();

    public MainPage()
    {
        InitializeComponent();


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
            lblStatus.Text = String.Concat("Processed ", processedFile.ToString(), " out of ", totalFile.ToString() + " (", errorFile.ToString()," errors!)");
        }

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
            sbPrompt.AppendLine("Qualification");
            sbPrompt.AppendLine("Work History");
            sbPrompt.AppendLine("Summary");

            sbSystemMsg.AppendLine("Desired format:");
            sbSystemMsg.AppendLine("Name - James Collingwood | ");
            sbSystemMsg.AppendLine("Email - james.collingwood@gmail.com | ");
            sbSystemMsg.AppendLine("Qualification - B.A, Percentage: 59%, University: Osaka State University |");
            sbSystemMsg.AppendLine("Work History - Company: ABC Ltd., Designation: Sr. Software Engineer, Period: May, 2005 - June, 2008 (3 Years 1 Month)" + Environment.NewLine + "Company: Microsoft Ltd.., Designation: Sr. Engineering Manager, Period: June, 2008 - August, 2009 (1 Years 2 Month) |");
            sbSystemMsg.AppendLine("Summary - 17+ years of Experience in designing and developing applications using Microsoft Technology Stack (C#, VB.Net,.Net, .Net Core, ASP.Net MVC, ASP.Net, SQL Server) , Javascript Frameworks(Angular, D3JS) and Cloud (Azure) |");

            conversation = ChatGPT.CreateConversation();
            conversation.AppendUserInput(sbPrompt.ToString());
            conversation.AppendSystemMessage(sbSystemMsg.ToString());
            var returnChat = (await SendRequestAsync()).Split('|');

            candidate.Name = returnChat[0].Split("-")[1].Trim();
            candidate.Email = returnChat[1].Split("-")[1].Trim();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        return candidate;

        //conversation.AppendUserInput("question_to_ask: We require candidate with skills C#, SQL, Javascript, Good Communication, Management skills. Soes this candidate meet our requirements?");
        //lblName.Content = await SendRequestAsync();
    }


    private async System.Threading.Tasks.Task<string> SendRequestAsync()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        Task<string> task = conversation.GetResponseFromChatbotAsync();
        await System.Threading.Tasks.Task.WhenAny(task, System.Threading.Tasks.Task.Delay(Timeout.Infinite, cancellationTokenSource.Token));
        cancellationTokenSource.Token.ThrowIfCancellationRequested();
        return await task;
    }
}


