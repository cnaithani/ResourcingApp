using OpenAI_API.Chat;
using OpenAI_API;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.ComponentModel;
using System.Reflection;
using System.Text.RegularExpressions;

namespace CRToolKit
{
    /// <summary>
    /// Static class containing methods for interacting with the ChatGPT API.
    /// </summary>
    static class ChatGPT
    {
        private static OpenAIAPI api;
        private static OpenAIAPI apiForAzureTurboChat;
        private static ChatGPTHttpClientFactory chatGPTHttpClient;
        private static readonly TimeSpan timeout = new(0, 0, 120);

        /// <summary>
        /// Asynchronously gets a response from a chatbot.
        /// </summary>
        /// <param name="options">The options for the chatbot.</param>
        /// <param name="systemMessage">The system message to send to the chatbot.</param>
        /// <param name="userInput">The user input to send to the chatbot.</param>
        /// <param name="stopSequences">The stop sequences to use for ending the conversation.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>The response from the chatbot.</returns>
        public static async Task<string> GetResponseAsync(ChatOptions options, string systemMessage, string userInput, string[] stopSequences, CancellationToken cancellationToken)
        {
            Conversation chat = CreateConversationForCompletions(options, systemMessage, userInput, stopSequences);

            Task<string> task = chat.GetResponseFromChatbotAsync();

            await Task.WhenAny(task, Task.Delay(timeout, cancellationToken)).ConfigureAwait(false);

            if (task.IsFaulted)
            {
                throw task.Exception.InnerException ?? task.Exception;
            }

            cancellationToken.ThrowIfCancellationRequested();

            return await task;
        }

        /// <summary>
        /// Asynchronously gets the response from the chatbot.
        /// </summary>
        /// <param name="options">The options for the chat.</param>
        /// <param name="systemMessage">The system message to display in the chat.</param>
        /// <param name="userInput">The user input in the chat.</param>
        /// <param name="stopSequences">The stop sequences to end the conversation.</param>
        /// <param name="resultHandler">The action to handle the chatbot response.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static async Task GetResponseAsync(ChatOptions options, string systemMessage, string userInput, string[] stopSequences, Action<string> resultHandler, CancellationToken cancellationToken)
        {
            Conversation chat = CreateConversationForCompletions(options, systemMessage, userInput, stopSequences);

            Task task = chat.StreamResponseFromChatbotAsync(resultHandler);

            await Task.WhenAny(task, Task.Delay(timeout, cancellationToken)).ConfigureAwait(false);

            if (task.IsFaulted)
            {
                throw task.Exception.InnerException ?? task.Exception;
            }

            cancellationToken.ThrowIfCancellationRequested();
        }

        /// <summary>
        /// Creates a conversation with the specified options and system message.
        /// </summary>
        /// <param name="options">The options to use for the conversation.</param>
        /// <param name="systemMessage">The system message to append to the conversation.</param>
        /// <returns>The created conversation.</returns>
        public static Conversation CreateConversation(string systemMessage = "")
        {
            ChatOptions options = new ChatOptions();
            Conversation chat;
            CreateApiHandler(options);

            chat = api.Chat.CreateConversation();

            chat.AppendSystemMessage(systemMessage);

            chat.RequestParameters.Temperature = options.Temperature;
            chat.RequestParameters.MaxTokens = options.MaxTokens;
            chat.RequestParameters.TopP = options.TopP;
            chat.RequestParameters.FrequencyPenalty = options.FrequencyPenalty;
            chat.RequestParameters.PresencePenalty = options.PresencePenalty;

            chat.Model = options.Model.GetStringValue();

            return chat;
        }

        /// <summary>
        /// Creates a conversation for Completions with the given parameters.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="systemMessage">The system message.</param>
        /// <param name="userInput">The user input.</param>
        /// <param name="stopSequences">The stop sequences.</param>
        /// <returns>
        /// The created conversation.
        /// </returns>
        private static Conversation CreateConversationForCompletions(ChatOptions options, string systemMessage, string userInput, string[] stopSequences)
        {
            Conversation chat = CreateConversation(systemMessage);

            if (options.MinifyRequests)
            {
                userInput = TextFormat.MinifyText(userInput);
            }

            userInput = TextFormat.RemoveCharactersFromText(userInput, options.CharactersToRemoveFromRequests.Split(','));

            chat.AppendUserInput(userInput);

            if (stopSequences != null && stopSequences.Length > 0)
            {
                chat.RequestParameters.MultipleStopSequences = stopSequences;
            }

            return chat;
        }

        /// <summary>
        /// Creates an API handler with the given API key and proxy.
        /// </summary>
        /// <param name="options">All configurations to create the connection</param>
        private static void CreateApiHandler(ChatOptions options)
        {
            if (api == null)
            {
                chatGPTHttpClient = new(options);
                APIAuthentication auth;
                auth = new(options.OpenAIApiKey);
                api = new(auth);
                api.HttpClientFactory = chatGPTHttpClient;
            }
            else if (api.Auth.ApiKey != options.OpenAIApiKey)
            {
                api.Auth.ApiKey = options.OpenAIApiKey;
            }
        }
    }

    /// <summary>
    /// Enum containing the different types of model languages.
    /// </summary>
    public enum ModelLanguageEnum
    {
        [EnumStringValue("gpt-3.5-turbo")]
        GPT_3_5_Turbo,
        [EnumStringValue("gpt-3.5-turbo-1106")]
        GPT_3_5_Turbo_1106,
        [EnumStringValue("gpt-4")]
        GPT_4,
        [EnumStringValue("gpt-4-32k")]
        GPT_4_32K,
        [EnumStringValue("gpt-4-turbo-preview")]
        GPT_4_Turbo
    }

    /// <summary>
    /// Enum to represent the different OpenAI services available.
    /// </summary>
    public enum OpenAIService
    {
        OpenAI
    }

    public class EnumStringValue : Attribute
    {
        private readonly string value;

        /// <summary>
        /// Initializes a new instance of the EnumStringValue class with the specified value.
        /// </summary>
        /// <param name="value">The value of the EnumStringValue.</param>
        public EnumStringValue(string value)
        {
            this.value = value;
        }

        /// <summary>
        /// Gets the value of the property.
        /// </summary>
        /// <returns>The value of the property.</returns>
        public string Value
        {
            get
            {
                return value;
            }
        }
    }

    class ChatGPTHttpClientFactory : IHttpClientFactory
    {
        private readonly Dictionary<string, HttpClientCustom> httpClients = new();
        private static readonly object objLock = new();
        private string m_proxy;
        private readonly ChatOptions options;

        /// <summary>
        /// Initializes a new instance of the ChatGPTHttpClientFactory class.
        /// </summary>
        /// <param name="options">The options for configuring the HttpClient.</param>
        public ChatGPTHttpClientFactory(ChatOptions options)
        {
            this.options = options;
        }

        /// <summary>
        /// Creates an HttpClient with the given name.
        /// </summary>
        /// <param name="name">The name of the HttpClient.</param>
        /// <returns>The created HttpClient.</returns>
        public HttpClient CreateClient(string name)
        {
            if (!httpClients.TryGetValue(name, out HttpClientCustom client))
            {
                lock (objLock)
                {
                    if (!httpClients.TryGetValue(name, out client))
                    {
                        client = CreateHttpClient(CreateMessageHandler());
                        httpClients.Add(name, client);
                    }
                }
            }

            if (client.IsDisposed)
            {
                httpClients.Remove(name);
                return CreateClient(name);
            }

            return client;
        }

       

        /// <summary>
        /// Creates an HttpClient with the specified message handler and default settings.
        /// </summary>
        /// <param name="handler">The message handler.</param>
        /// <returns>An HttpClient with the specified message handler and default settings.</returns>
        protected HttpClientCustom CreateHttpClient(HttpMessageHandler handler)
        {
            HttpClientCustom lookHttp = new(handler);

            lookHttp.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
            lookHttp.DefaultRequestHeaders.Connection.Add("keep-alive");
            lookHttp.Timeout = new TimeSpan(0, 0, 120);

            return lookHttp;
        }

        /// <summary>
        /// Creates an HttpMessageHandler with the specified proxy settings.
        /// </summary>
        /// <param name="proxy">The proxy settings to use.</param>
        /// <returns>An HttpMessageHandler with the specified proxy settings.</returns>
        protected HttpMessageHandler CreateMessageHandler()
        {
            HttpClientHandler handler;
            handler = new HttpClientHandler();

            handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            handler.UseCookies = true;
            handler.AllowAutoRedirect = true;
            handler.ServerCertificateCustomValidationCallback = (a, b, c, d) => true;
            handler.MaxConnectionsPerServer = 256;
            handler.SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls11 | System.Security.Authentication.SslProtocols.Tls;

            return handler;
        }
    }

    class HttpClientCustom : HttpClient
    {
        /// <summary>
        /// Checks if the object has been disposed.
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public HttpClientCustom(HttpMessageHandler handler) : base(handler) { }

        /// <summary>
        /// Disposes the object and releases any associated resources.
        /// </summary>
        /// <param name="disposing">A boolean value indicating whether to dispose managed resources.</param>
        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }
    }

    public class ChatOptions 
    {
        public ChatOptions()
        {
            
        }


        #region General

        [Category("General")]
        [DisplayName("OpenAI API Key")]
        [Description("Set API Key. For OpenAI API, see \"https://beta.openai.com/account/api-keys\" for more details.")]
        public string OpenAIApiKey { get; set; } = "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";

        [Category("General")]
        [DisplayName("Single Response")]
        [Description("If true, the entire response will be displayed at once (less undo history but longer waiting time).")]
        [DefaultValue(false)]
        [Browsable(true)]
        public bool SingleResponse { get; set; } = false;

        [Category("General")]
        [DisplayName("Minify Requests")]
        [Description("If true, all requests to OpenAI will be minified. Ideal to save Tokens.")]
        [DefaultValue(false)]
        [Browsable(true)]
        public bool MinifyRequests { get; set; } = false;

        [Category("General")]
        [DisplayName("Characters To Remove From Requests")]
        [Description("Add characters or words to be removed from all requests made to OpenAI. They must be separated by commas, e.g. a,1,TODO:,{")]
        [DefaultValue("")]
        [Browsable(true)]
        public string CharactersToRemoveFromRequests { get; set; } = string.Empty;

        #endregion General

        #region ChatGPT Model Parameters

        [Category("OpenAI Model Parameters")]
        [DisplayName("Model Language")]
        [Description("See \"https://platform.openai.com/docs/models/overview\" for more details.")]
        [DefaultValue(ModelLanguageEnum.GPT_3_5_Turbo_1106)]
        [TypeConverter(typeof(EnumConverter))]
        public ModelLanguageEnum Model { get; set; } = ModelLanguageEnum.GPT_3_5_Turbo_1106;

        [Category("OpenAI Model Parameters")]
        [DisplayName("Max Tokens")]
        [Description("See \"https://help.openai.com/en/articles/4936856-what-are-tokens-and-how-to-count-them\" for more details.")]
        [DefaultValue(2048)]
        public int MaxTokens { get; set; } = 4096;

        [Category("OpenAI Model Parameters")]
        [DisplayName("Temperature")]
        [Description("What sampling temperature to use. Higher values means the model will take more risks. Try 0.9 for more creative applications, and 0 for ones with a well-defined answer.")]
        [DefaultValue(0)]
        [TypeConverter(typeof(DoubleConverter))]
        public double Temperature { get; set; } = 0;

        [Category("OpenAI Model Parameters")]
        [DisplayName("Presence Penalty")]
        [Description("The scale of the penalty applied if a token is already present at all. Should generally be between 0 and 1, although negative numbers are allowed to encourage token reuse.")]
        [DefaultValue(0)]
        [TypeConverter(typeof(DoubleConverter))]
        public double PresencePenalty { get; set; } = 0;

        [Category("OpenAI Model Parameters")]
        [DisplayName("Frequency Penalty")]
        [Description("The scale of the penalty for how often a token is used. Should generally be between 0 and 1, although negative numbers are allowed to encourage token reuse.")]
        [DefaultValue(0)]
        [TypeConverter(typeof(DoubleConverter))]
        public double FrequencyPenalty { get; set; } = 0;

        [Category("OpenAI Model Parameters")]
        [DisplayName("top p")]
        [Description("An alternative to sampling with temperature, called nucleus sampling, where the model considers the results of the tokens with top_p probability mass. So 0.1 means only the tokens comprising the top 10% probability mass are considered.")]
        [DefaultValue(0)]
        [TypeConverter(typeof(DoubleConverter))]
        public double TopP { get; set; } = 0;

        [Category("OpenAI Model Parameters")]
        [DisplayName("Stop Sequences")]
        [Description("Up to 4 sequences where the API will stop generating further tokens. The returned text will not contain the stop sequence. Separate different stop strings by a comma e.g. '},;,stop'")]
        [DefaultValue("")]
        public string StopSequences { get; set; } = string.Empty;

        #endregion ChatGPT Model Parameters   

    }

    internal static class TextFormat
    {
        /// <summary>
        /// Removes all whitespace and break lines from a given string.
        /// </summary>
        /// <param name="textToMinify">The string to minify.</param>
        /// <returns>A minified version of the given string.</returns>
        public static string MinifyText(string textToMinify)
        {
            return Regex.Replace(textToMinify, @"\s+", " ");
        }

        /// <summary>
        /// Removes the specified characters from the given text.
        /// </summary>
        /// <param name="text">The text from which to remove the characters.</param>
        /// <param name="charsToRemove">The characters to remove from the text.</param>
        /// <returns>The text with the specified characters removed.</returns>
        public static string RemoveCharactersFromText(string text, string[] charsToRemove)
        {
            foreach (string character in charsToRemove)
            {
                if (!string.IsNullOrEmpty(character))
                {
                    text = text.Replace(character, string.Empty);
                }
            }

            return text;
        }

        /// <summary>
        /// Gets the comment characters for a given file path.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>The comment characters for the given file path.</returns>
        public static string GetCommentChars(string filePath)
        {
            string extension = System.IO.Path.GetExtension(filePath).TrimStart('.');

            if (extension.Equals("cs", StringComparison.InvariantCultureIgnoreCase) || extension.Equals("js", StringComparison.InvariantCultureIgnoreCase))
            {
                return "//";
            }

            if (extension.Equals("vb", StringComparison.InvariantCultureIgnoreCase))
            {
                return "'";
            }

            if (extension.Equals("sql", StringComparison.InvariantCultureIgnoreCase))
            {
                return "--";
            }

            return "<!--";
        }

        /// <summary>
        /// Formats a given command for a given language 
        /// for example for c#, for visual basic, for sql server, for java script
        /// or by default.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="filePath">The file path.</param>
        /// <returns>A Formatted string</returns>
        public static string FormatForCompleteCommand(string command, string filePath)
        {
            string extension = System.IO.Path.GetExtension(filePath).TrimStart('.');

            string language = string.Empty;

            if (extension.Equals("cs", StringComparison.InvariantCultureIgnoreCase))
            {
                language = "for C#";
            }
            else if (extension.Equals("vb", StringComparison.InvariantCultureIgnoreCase))
            {
                language = "for Visual Basic";
            }
            else if (extension.Equals("sql", StringComparison.InvariantCultureIgnoreCase))
            {
                language = "for SQL Server";
            }
            else if (extension.Equals("js", StringComparison.InvariantCultureIgnoreCase))
            {
                language = "for Java Script";
            }

            return $"{command} {language}: ";
        }

        /// <summary>
        /// Formats a command for a summary.
        /// </summary>
        /// <param name="command">The command to format.</param>
        /// <param name="selectedText">The selected text.</param>
        /// <returns>The formatted command.</returns>
        public static string FormatCommandForSummary(string command, string selectedText)
        {
            string summaryFormat;

            //Is not a function
            if (!(selectedText.Contains("(") && selectedText.Contains(")") && selectedText.Contains("{") && selectedText.Contains("}")))
            {
                summaryFormat = "/// <summary>\r\n/// \r\n/// </summary>";
            }
            else if (selectedText.Contains(" void "))
            {
                if (selectedText.Contains("()"))
                {
                    summaryFormat = "/// <summary>\r\n/// \r\n/// </summary>";
                }
                else
                {
                    summaryFormat = "/// <summary>\r\n/// \r\n/// </summary>\r\n/// <param name=\"\"></param>";
                }
            }
            else
            {
                if (selectedText.Contains("()"))
                {
                    summaryFormat = "/// <summary>\r\n/// \r\n/// </summary>\r\n/// <returns>\r\n/// \r\n/// </returns>";
                }
                else
                {
                    summaryFormat = "/// <summary>\r\n/// \r\n/// </summary>\r\n/// <param name=\"\"></param>\r\n/// <returns>\r\n/// \r\n/// </returns>";
                }
            }

            return string.Format(command, summaryFormat) + Environment.NewLine + "for";
        }

        /// <summary>
        /// Detects the language of the given code.
        /// </summary>
        /// <param name="code">The code to detect the language of.</param>
        /// <returns>The language of the given code, or an empty string if the language could not be determined.</returns>
        public static string DetectCodeLanguage(string code)
        {
            Regex regexXML = new(@"(<\?xml.+?\?>)|(<.+?>.*?<\/.+?>)");
            Regex regexHTML = new(@"(<.+?>.*?<\/.+?>)");
            Regex regexCSharp = new(@"(public|private|protected|internal|static|class|void|string|double|float|in)");
            Regex regexVB = new(@"(Public|Private|Protected|Friend|Static|Class|Sub|Function|End Sub|End Function|Dim|As|Integer|Boolean|String|Double|Single|If|Else|End If|While|End While|For|To|Step|Next|Each|In|Return)");
            Regex regexJS = new(@"(function|do|switch|case|break|continue|let|instanceof|undefined|super|\bconsole\.)");
            Regex regexCSS = new(@"([^{]*\{[^}]*\})");
            Regex regexTSQL1 = new(@"(CREATE|UPDATE|DELETE|INSERT|DROP|SELECT|FROM|WHERE|JOIN|LEFT\s+JOIN|RIGHT\s+JOIN|INNER\s+JOIN|OUTER\s+JOIN|ON|GROUP\s+BY|HAVING|ORDER\s+BY|LIMIT|\bAND\b|\bOR\b|\bNOT\b|\bIN\b|\bBETWEEN\b|\bLIKE\b|\bIS\s+NULL\b|\bIS\s+NOT\s+NULL\b|\bEXISTS\b|\bCOUNT\b|\bSUM\b|\bAVG\b|\bMIN\b|\bMAX\b|\bCAST\b|\bCONVERT\b|\bDATEADD\b|\bDATEDIFF\b|\bDATENAME\b|\bDATEPART\b|\bGETDATE\b|\bYEAR\b|\bMONTH\b|\bDAY\b|\bHOUR\b|\bMINUTE\b|\bSECOND\b|\bTOP\b|\bDISTINCT\b|\bAS\b)");
            Regex regexTSQL2 = new(@"(create|update|delete|insert|drop|select|from|where|join|left\s+join|right\s+join|inner\s+join|outer\s+join|on|group\s+by|having|order\s+by|limit|\band\b|\bor\b|\bnot\b|\bin\b|\bbetween\b|\blike\b|\bis\s+null\b|\bis\s+not\s+null\b|\bexists\b|\bcount\b|\bsum\b|\bavg\b|\bmin\b|\bmax\b|\bcast\b|\bconvert\b|\bdateadd\b|\bdatediff\b|\bdatename\b|\bdatepart\b|\bgetdate\b|\byear\b|\bmonth\b|\bday\b|\bhour\b|\bminute\b|\bsecond\b|\btop\b|\bdistinct\b|\bas\b)");

            if (regexXML.IsMatch(code))
            {
                return "XML";
            }

            if (regexHTML.IsMatch(code))
            {
                return "HTML";
            }

            if (regexCSharp.IsMatch(code))
            {
                return "C#";
            }

            if (regexVB.IsMatch(code))
            {
                return "VB";
            }

            if (regexJS.IsMatch(code))
            {
                return "JavaScript";
            }

            if (regexCSS.IsMatch(code))
            {
                return "CSS";
            }

            if (regexTSQL1.IsMatch(code) || regexTSQL2.IsMatch(code))
            {
                return "TSQL";
            }

            return string.Empty;
        }
    }

    /// <summary>
    /// A helper class for working with enums in C#.
    /// </summary>
    public static class EnumHelper
    {
        /// <summary>
        /// Gets the string value associated with the specified enum item.
        /// </summary>
        /// <param name="enumItem">The enum item.</param>
        /// <returns>The string value associated with the enum item.</returns>
        public static string GetStringValue(this Enum enumItem)
        {
            string enumStringValue = string.Empty;
            Type type = enumItem.GetType();

            FieldInfo objFieldInfo = type.GetField(enumItem.ToString());
            EnumStringValue[] enumStringValues = objFieldInfo.GetCustomAttributes(typeof(EnumStringValue), false) as EnumStringValue[];

            if (enumStringValues.Length > 0)
            {
                enumStringValue = enumStringValues[0].Value;
            }

            return enumStringValue;
        }
    }

    /// <summary>
    /// Represents an attribute that can be used to associate a string value with an enum value.
    /// </summary>
}
