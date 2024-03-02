
using System.Threading.Tasks;
using System.Threading;
using CommunityToolkit.Maui.Storage;
using System.Xml;
using OpenAI_API.Chat;
using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ResourcingToolKit.Models;
using ResourcingToolKit.DTO;
using System.Runtime.ConstrainedExecution;
using Microsoft.Extensions.Configuration;
using Microsoft.Maui.Controls;
using Newtonsoft.Json;
using DocumentFormat.OpenXml.Drawing;
using ResourcingToolKit.Classes;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using DocumentFormat.OpenXml.Math;

namespace ResourcingToolKit;

public partial class MainPage : ContentPage
{
    int count = 0;
    string folderPath = null;
    int totalFile = 0;
    int processedFile = 0;
    int errorFile = 0;
    int waitMillisecond = 45000;
    SimpleDocWriter writer = new SimpleDocWriter();
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
        lblStatus.Text += " Started Processing...";
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
    TRYAGAIN:

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
            sbPrompt.AppendLine("\"Qualification\"");
            sbPrompt.AppendLine("[{");
            sbPrompt.AppendLine("\"Degree\" <string>");
            sbPrompt.AppendLine("\"Certification\" <string>");
            sbPrompt.AppendLine("\"University\" <string>");
            sbPrompt.AppendLine("\"College\" <string>");
            sbPrompt.AppendLine("\"Month\" <string>");
            sbPrompt.AppendLine("\"Year\" <string>");
            sbPrompt.AppendLine("\"Location\" <string>");
            sbPrompt.AppendLine("}]");
            sbPrompt.AppendLine("\"WorkHistory\"");
            sbPrompt.AppendLine("[{");
            sbPrompt.AppendLine("\"Company\" <string>");
            sbPrompt.AppendLine("\"Position\" <string>");
            sbPrompt.AppendLine("\"Duration\" <string>");
            sbPrompt.AppendLine("\"Location\" <string>");
            sbPrompt.AppendLine("}]");
            //sbPrompt.AppendLine("\"Summary\" <string>");
            sbPrompt.AppendLine("\"Address\" <string>");
            sbPrompt.AppendLine("\"Linkedin\" <string>");
            sbPrompt.AppendLine("Do not infer any data based on previous training, strictly use only source text given below as input.");
            sbPrompt.AppendLine("========");
            sbPrompt.AppendLine(txt);
            sbPrompt.AppendLine("========");

            conversation = ChatGPT.CreateConversation();
            conversation.AppendUserInput(sbPrompt.ToString());
            var returnChat = await SendRequestAsync();
            returnChat = returnChat.Replace("```json", string.Empty).Replace("```", string.Empty);
            returnChat = Regex.Replace(returnChat, "not provided", string.Empty, RegexOptions.IgnoreCase);

            var candidateDTO = JsonConvert.DeserializeObject<CandidateDTO>(returnChat);
            JSONTransformer.Transform(candidateDTO);
            conversation.AppendUserInput("Please suggest objective/summary for candidate resume");
            conversation.AppendSystemMessage("Please use first person narrative.");
            returnChat = await SendRequestAsync();
            int index = returnChat.IndexOf(": \n");
            if (index >= 0)
                returnChat=  returnChat.Substring(index + 1); // +1 to exclude the delimiter itself
            
            candidateDTO.Summary = returnChat.Trim();
            await GetWork(conversation, candidateDTO.WorkHistory);
            await GetRating(conversation, candidateDTO);
            await GetResumeHeadlines(conversation, candidateDTO);

            //Get local template file path from config
            //var templateFilePath = Config.GetRequiredSection("AppSettings:TemplateFilePath").Value.ToString();
            //var redultDirpath = Config.GetRequiredSection("AppSettings:RedultDirpath").Value.ToString();
            var setting = await App.Database.database.Table<Models.AppSettings>().FirstOrDefaultAsync();
            if (setting == null || setting.TemplateFile == null)
            {
                lblStatus.Text = "Please select templete file from settings screen!";
                return null;
            }
            if (setting.ProcessingFolder == null)
            {
                lblStatus.Text = "Please select result folder from settings screen!";
                return null;
            }
            var templateFilePath = setting.TemplateFile;
            var redultDirpath = setting.ProcessingFolder;
            string filePath = TransformFile(candidateDTO, templateFilePath, redultDirpath); 
           
            candidate.FilePath = filePath;
            candidate.Name = candidateDTO.Name;
            candidate.Email = candidateDTO.Email;
            candidate.Phone = candidateDTO.Phone;
            candidate.Skills = candidateDTO.KeySkills;
            candidate.Rating = candidateDTO.Rating;
            candidate.Modified = DateTime.Now;

            await App.Database.database.InsertAsync(candidate);

        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("TooManyRequests"))
            {
                await Task.Delay(waitMillisecond);
                goto TRYAGAIN;
            }
            Console.WriteLine(ex.Message);
            await DisplayAlert("Error", ex.Message, "OK");
        }
        return candidate;
    }

    #region Get Data
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
        var keyStr = vals.Where(x => x.Contains(key + seprator)).FirstOrDefault();
        if (String.IsNullOrEmpty(keyStr) || keyStr.ToLower().Contains("not provided"))
            return string.Empty;

        return keyStr.Split(seprator)[1].Trim();
    }
    #endregion

    #region Process JSON Data

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
                if (ex.Message.Contains("TooManyRequests"))
                {
                    await Task.Delay(waitMillisecond);
                    await GetWorkDetails(chat, work);
                }
            }

        }
    }

    async Task GetRating(Conversation chat, CandidateDTO candidate)
    {
    TRYAGAIN:
        try
        {
            var replace1 = "The core competencies of the candidate include:";

            var sbPrompt = new StringBuilder();
            sbPrompt.AppendLine(string.Concat("Please tell core competencies of candidate "));
            sbPrompt.AppendLine("Return answer as comma saprated string ");
            conversation.AppendUserInput(sbPrompt.ToString());
            var response = await SendRequestAsync();
            var skillArr = response.Replace(replace1, string.Empty).Split("\n");
            var skills = string.Empty;
            if (skillArr.Count() > 1)
            {
                skills = skillArr[1];
                skillArr = skillArr[1].Split(",").Select(x => x.Trim().ToLower()).ToArray();
            }
            else
            {
                skills = skillArr[0];
                skillArr = skillArr[0].Split(",").Select(x => x.Trim().ToLower()).ToArray();
            }
            var primaySkill = pSkill.Text.Split(",").Select(x => x.Trim().ToLower()).ToArray();
            var matchCount = primaySkill.Count(x => skillArr.Contains(x));
            var totCount = primaySkill.Count();

            var retCount = (int)Math.Ceiling(decimal.Parse(((((matchCount) * 5) / totCount)).ToString()));
            candidate.KeySkills = skills;
            candidate.Rating = retCount;
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("TooManyRequests"))
            {
                await Task.Delay(waitMillisecond);
                goto TRYAGAIN;
            }
            candidate.Rating = 0;
        }
    }

    async Task GetResumeHeadlines(Conversation chat, CandidateDTO candidate)
    {
    TRYAGAIN:
        try
        {
            var replace1 = "The resume headlines of the candidate are:";
            var sbPrompt = new StringBuilder();
            sbPrompt.AppendLine(string.Concat("Please suggest atleat 4 short resume headlines - "));
            sbPrompt.AppendLine("Return answer as comma saprated string. Headline should be maximum 4 words. Use title case for each headline. ");
            conversation.AppendUserInput(sbPrompt.ToString());
            conversation.AppendSystemMessage("Example - Reliability Engineer, Mechanical Engineering, Root Cause Analysis, Team Supervision");
            var response = await SendRequestAsync();
            var headlineArr = response.Replace(replace1, string.Empty).Split("\n");
            var headlines = string.Empty;
            if (headlineArr.Count() > 1)
            {
                headlines = headlineArr[1];
                headlineArr = headlineArr[1].Split(",").Select(x => x.Trim().ToLower()).ToArray();
            }
            else
            {
                headlines = headlineArr[0];
                headlineArr = headlineArr[0].Split(",").Select(x => x.Trim().ToLower()).ToArray();
            }
            candidate.Headlines = headlineArr;
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("TooManyRequests"))
            {
                await Task.Delay(waitMillisecond);
                goto TRYAGAIN;
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
        conversation.AppendSystemMessage("Please use first person narrative. Logically saprate sections through bullet points if needed.");
        work.Summary = await SendRequestAsync();
        if (!string.IsNullOrEmpty(work.Summary) && (work.Summary.ToLower().Contains("not provided") || work.Summary.ToLower().Contains("i cannot provide") || (work.Summary.ToLower().Contains("did not find any information"))))
        {
            work.Summary = string.Empty;
            //TODO: Log issue - High
        }
    }

    #endregion

    #region Create File
    String TransformFile(CandidateDTO candidate, string templateFilePath, string redultDirpath)
    {
        int ctr = 1;
        var targetFileName = candidate.Name;
        string targetFilePath = null;
        //string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        //redultDirpath = System.IO.Path.Combine(path, "Results");
        targetFilePath = System.IO.Path.Combine(redultDirpath, targetFileName + ".docx");

        if (!Directory.Exists(redultDirpath))
        {
            Directory.CreateDirectory(redultDirpath);
        }

        while (File.Exists(targetFilePath))
        {
            ctr += 1;
            targetFilePath = System.IO.Path.Combine(redultDirpath, targetFileName + " - " + ctr.ToString() + ".docx");
        }
        if (!File.Exists(targetFilePath))
        {
            File.Copy(templateFilePath, targetFilePath,true);
        }

        using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(targetFilePath, true))
        {
            //string? docText = null;
            if (wordDoc.MainDocumentPart is null)
            {
                throw new ArgumentNullException("MainDocumentPart and/or Body is null.");
            }
            
            TransformBasicInfo(candidate, wordDoc);
            TransformWorkHistory(candidate, wordDoc);
            TransformQual(candidate, wordDoc);
            RemoveExtrachars(wordDoc);

            return targetFilePath;
        }
    }
    void TransformBasicInfo(CandidateDTO candidate, WordprocessingDocument doc)
    {
        AddPara(doc, "NME", candidate.Name);
        AddPara(doc, "EML", candidate.Email);
        AddPara(doc, "MOB", candidate.Phone);
        AddPara(doc, "SUMM", candidate.Summary);
        AddPara(doc, "DAWD", candidate.Address);
        AddPara(doc, "LNKIN", candidate.Linkedin);
        AddPara(doc, "SKLL", candidate.KeySkills.Replace(",", " |"));
        if (candidate.Headlines != null && candidate.Headlines.Count() >0) AddPara(doc, "RHL1", System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(candidate.Headlines[0].ToLower()));
        if (candidate.Headlines != null && candidate.Headlines.Count() > 1) AddPara(doc, "RHL2", System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(candidate.Headlines[1].ToLower()));
        if (candidate.Headlines != null && candidate.Headlines.Count() > 2) AddPara(doc, "RHL3", System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(candidate.Headlines[2].ToLower()));
        if (candidate.Headlines != null && candidate.Headlines.Count() > 3) AddPara(doc, "RHL4", System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(candidate.Headlines[3].ToLower()));
    }
    void AddPara(WordprocessingDocument doc, string varNama, string replaceChar)
    {
        var addPara = doc.MainDocumentPart.Document.Body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>().Where(p => p.InnerText.Contains(varNama)).FirstOrDefault();
        if (addPara != null)
            writer.ReplacePara(addPara, varNama, replaceChar);
    }
    void TransformWorkHistory(CandidateDTO candidate, WordprocessingDocument doc)
    {

        var desig = "DESG";
        var summ = "SWUMM";
        var com = "WADD";
        var dur = "DUR";
        int ctrWork = candidate.WorkHistory.Count;
        var writer = new SimpleDocWriter();

        //Repeace work Summs
        var workSummParas = doc.MainDocumentPart.Document.Body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>().Where(p => p.InnerText.Contains(summ)).ToList();
        var desigParas = doc.MainDocumentPart.Document.Body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>().Where(p => p.InnerText.Contains(desig)).ToList();
        var comParas = doc.MainDocumentPart.Document.Body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>().Where(p => p.InnerText.Contains(com)).ToList();
        var durParas = doc.MainDocumentPart.Document.Body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>().Where(p => p.InnerText.Contains(dur)).ToList();

        for (int ctr = 0; ctr < ctrWork; ctr++)
        {
            var workText = candidate.WorkHistory[ctr].Summary.Split("\n- ");
            if (workText.Length>0 && workText[0].EndsWith(":\n"))
            {
                string[] newArray = new string[workText.Length - 1];
                Array.Copy(workText, 1, newArray, 0, newArray.Length);
                workText = newArray;    
            }
            if (workText != null && workText.Length > 0 && workText[0].StartsWith("-"))
            {
                workText[0] = workText[0][1..].Trim();
            }
            
            var para = workSummParas[ctr];
            writer.AddBulletListInPara(doc, para, workText.ToList(), summ + (ctr + 1).ToString());

            para = desigParas[ctr];
            writer.ReplacePara(para, desig + (ctr + 1).ToString(), candidate.WorkHistory[ctr].Position);

            para = comParas[ctr];
            writer.ReplacePara(para, com + (ctr + 1).ToString(), candidate.WorkHistory[ctr].Company);

            para = durParas[ctr];
            writer.ReplacePara(doc,para, dur + (ctr + 1).ToString(), candidate.WorkHistory[ctr].Duration, candidate.WorkHistory[ctr].Position, 130);
        }

        writer.RemovePara(doc, desig);
        writer.RemovePara(doc, summ);
        writer.RemovePara(doc, com);
        writer.RemovePara(doc, dur);
    }
    void TransformQual(CandidateDTO candidate, WordprocessingDocument doc)
    {

        var replacement1 = "QUNV";
        var replacement2 = "QADD";
        var replacement3 = "QDEGREE";
        var replacement4 = "QDATE";
        int ctrWork = candidate.Qualification.Count;
        var writer = new SimpleDocWriter();

        //Repeace work Summs
        var replacementParas1 = doc.MainDocumentPart.Document.Body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>().Where(p => p.InnerText.Contains(replacement1)).ToList();
        var replacementParas2 = doc.MainDocumentPart.Document.Body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>().Where(p => p.InnerText.Contains(replacement2)).ToList();
        var replacementParas3 = doc.MainDocumentPart.Document.Body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>().Where(p => p.InnerText.Contains(replacement3)).ToList();
        var replacementParas4 = doc.MainDocumentPart.Document.Body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>().Where(p => p.InnerText.Contains(replacement4)).ToList();

        for (int ctr = 0; ctr < ctrWork; ctr++)
        {
            var para = replacementParas1[ctr];
            writer.ReplacePara(para, replacement1 + (ctr + 1).ToString(), candidate.Qualification[ctr].University);

            para = replacementParas2[ctr];
            writer.ReplacePara(doc, para, replacement2 + (ctr + 1).ToString(), candidate.Qualification[ctr].Location, candidate.Qualification[ctr].University, 110);

            para = replacementParas3[ctr];
            writer.ReplacePara(para, replacement3 + (ctr + 1).ToString(), candidate.Qualification[ctr].Degree);

            para = replacementParas4[ctr];
            writer.ReplacePara(doc, para, replacement4 + (ctr + 1).ToString(), candidate.Qualification[ctr].DisplayDate, candidate.Qualification[ctr].Degree, 160);
        }

        writer.RemovePara(doc, replacement1);
        writer.RemovePara(doc, replacement2);
        writer.RemovePara(doc, replacement3);
        writer.RemovePara(doc, replacement4);
    }
    void RemoveExtrachars(WordprocessingDocument doc)
    {
        writer.RemoveParagraphsContainingText(doc, "f07c", " LNKIN");
        writer.RemoveParagraphsContainingText(doc, "f07c", " NULL");
        writer.RemoveEmptyLines(doc);
        writer.RemoveText(doc, "NULL");
    }
    #endregion



}


