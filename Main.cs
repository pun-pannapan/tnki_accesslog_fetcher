using RestSharp;
using System.Configuration;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using Timer = System.Timers.Timer;

namespace tnki_accesslog_fetcher
{
    public partial class Main : Form
    {
        private Timer schedulerTimer;
        private string urlBase;
        private string urlLogin;
        private string urlPull;
        private string username;
        private string password;
        private string loginType;
        private string browserToken;
        private int intervalMinutes;
        private string outputPath;
        private string outputSessionFile;
        private NotifyIcon trayIcon;

        public Main()
        {
            InitializeComponent();
            LoadConfig();
            InitTimer();

            // Create tray icon
            trayIcon = new NotifyIcon
            {
                Icon = new Icon("RangerHero.ico"),
                Text = "HRRangerFetcher is running...",
                Visible = true
            };
            trayIcon.DoubleClick += (s, e) =>
            {
                this.Show();
                this.WindowState = FormWindowState.Normal;
            };
            this.Resize += (s, e) =>
            {
                if (this.WindowState == FormWindowState.Minimized)
                {
                    this.Hide();
                }
            };
            this.FormClosing += (s, e) =>
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
            };
        }
        private void LoadConfig()
        {
            urlBase = ConfigurationManager.AppSettings["urlBase"] ?? throw new InvalidOperationException("Configuration 'url Base' is missing or null.");
            urlLogin = ConfigurationManager.AppSettings["urlLogin"] ?? throw new InvalidOperationException("Configuration 'url Login' is missing or null.");
            urlPull = ConfigurationManager.AppSettings["urlPull"] ?? throw new InvalidOperationException("Configuration 'url Pull' is missing or null.");
            username = ConfigurationManager.AppSettings["username"] ?? throw new InvalidOperationException("Configuration 'username' is missing or null.");
            password = ConfigurationManager.AppSettings["password"] ?? throw new InvalidOperationException("Configuration 'password' is missing or null.");
            loginType = ConfigurationManager.AppSettings["loginType"] ?? throw new InvalidOperationException("Configuration 'loginType' is missing or null.");
            browserToken = ConfigurationManager.AppSettings["browserToken"] ?? throw new InvalidOperationException("Configuration 'browserToken' is missing or null.");
            intervalMinutes = int.TryParse(ConfigurationManager.AppSettings["intervalMinutes"], out var minutes)
                ? minutes
                : throw new InvalidOperationException("Configuration 'intervalMinutes' is missing, null, or not a valid integer.");
            outputPath = ConfigurationManager.AppSettings["outputPath"] ?? throw new InvalidOperationException("Configuration 'outputPath' is missing or null.");
            outputSessionFile = ConfigurationManager.AppSettings["outputSessionFile"] ?? throw new InvalidOperationException("Configuration 'outputSessionFile' is missing or null.");
            outputSessionFile = string.Format("{0}\\{1}", outputPath, outputSessionFile);
        }
        private void InitTimer()
        {
            schedulerTimer = new Timer(intervalMinutes * 60 * 1000);
            schedulerTimer.Elapsed += async (s, e) => await CallLoginApiTwoTime();
            schedulerTimer.AutoReset = true;
        }
        private async void btn_fetchnow_Click(object sender, EventArgs e)
        {
            await CallLoginApiTwoTime();
            MessageBox.Show($"Fetch access log complete");
        }
        private void btn_schedulepull_Click(object sender, EventArgs e)
        {
            schedulerTimer.Start();
            MessageBox.Show($"Scheduler started. Interval: {intervalMinutes} minutes");
        }
        private async Task CallLoginApiTwoTime()
        {
            try
            {
                var baseAddress = new Uri(urlBase);
                var cookieContainer = new CookieContainer();

                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true,
                    CookieContainer = cookieContainer,
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
                    UseCookies = true
                };

                using var client = new HttpClient(handler) { BaseAddress = baseAddress };

                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("PostmanRuntime/7.44.1");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
                client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
                client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
                client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));
                client.DefaultRequestHeaders.Connection.Add("keep-alive");
                client.DefaultRequestHeaders.Add("Postman-Token", Guid.NewGuid().ToString().ToLower());

                var boundary = "--------------------------271691758390784738182967";
                var form = new MultipartFormDataContent(boundary);
                form.Headers.ContentType = new MediaTypeHeaderValue("multipart/form-data");
                form.Headers.ContentType.Parameters.Add(new NameValueHeaderValue("boundary", boundary));
                form.Add(new StringContent(username), "username");
                form.Add(new StringContent(password), "password");
                form.Add(new StringContent(loginType), "loginType");
                form.Add(new StringContent(browserToken), "browserToken");

                var responseFirstLogin = await client.PostAsync(urlLogin, form);
                responseFirstLogin.EnsureSuccessStatusCode();

                var sessionCookieFirstLogin = cookieContainer.GetCookies(baseAddress)["SESSION"];

                var responseSecondLogin = await client.PostAsync(urlLogin, form);
                responseSecondLogin.EnsureSuccessStatusCode();
                var sessionCookieSecondLogin = cookieContainer.GetCookies(baseAddress)["SESSION"];
               
                boundary = "----WebKitFormBoundaryphd11S0gXickyhWo";
                var content = new MultipartFormDataContent(boundary);

                var endTime = DateTime.Now;
                var startTime = endTime;
                string queryConditions = $"startTime={startTime:yyyy-MM-dd}%2000:00:00&endTime={endTime:yyyy-MM-dd}%2023:59:59";

                // เพิ่ม form fields
                content.Add(new StringContent("{\"eventTime\":\"Time\",\"areaName\":\"Area+Name\",\"devAlias\":\"Device+Name\",\"eventPointName\":\"Event+Point\",\"eventName\":\"Event+Description\",\"levelAndEventPriority\":\"Event+Level\",\"vidLinkageHandle\":\"Media+File\",\"pin\":\"Personnel+ID\",\"name\":\"First+Name\",\"lastName\":\"Last+Name\",\"cardNo\":\"Card+Number\",\"deptName\":\"Department+Name\",\"readerName\":\"Reader+Name\",\"verifyModeName\":\"Verification+Mode\"}"), "jsonColumn");
                content.Add(new StringContent("com.zkteco.zkbiosecurity.acc.vo.AccTransactionItem"), "pageXmlPath");
                content.Add(new StringContent("All+Transactions"), "tableNameParam");
                content.Add(new StringContent("accTransaction.do?list"), "treeId");
                content.Add(new StringContent("50"), "pageSize");
                content.Add(new StringContent("99999999"), "records");
                content.Add(new StringContent("100000"), "maxExportCount");
                content.Add(new StringContent(password), "loginPwd"); // ใช้ password จาก config
                content.Add(new StringContent("0"), "isEncrypt");
                content.Add(new StringContent("CSV"), "reportType");
                content.Add(new StringContent("false"), "reportType_new_value");
                content.Add(new StringContent("1"), "exportType");
                content.Add(new StringContent(queryConditions), "queryConditions");
                content.Add(new StringContent(browserToken), "browserToken");

                string filename = $"All+Transactions_{DateTime.Now:yyyyMMddHHmmss}.csv";
                string fullOutputPath = Path.Combine(outputPath, filename);

                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("Cookie", $"org.springframework.web.servlet.i18n.CookieLocaleResolver.LOCALE=en-US; agreePolicy=1; SESSION={sessionCookieSecondLogin?.Value};");
                //cookieContainer.Add(baseAddress, new Cookie("SESSION", sessionCookie?.Value));

                var request = new HttpRequestMessage(HttpMethod.Post, urlPull)
                {
                    Content = content
                };
                foreach (var h in client.DefaultRequestHeaders)
                {
                    request.Headers.TryAddWithoutValidation(h.Key, h.Value);
                }

                string curlCommand = await BuildCurl(request, fullOutputPath);

                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {curlCommand}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                string stdout = await process.StandardOutput.ReadToEndAsync();
                string stderr = await process.StandardError.ReadToEndAsync();
                process.WaitForExit();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }
        private async Task CallLoginApi()
        {
            try
            {
                var baseAddress = new Uri("https://10.2.1.120:8098");
                var cookieContainer = new CookieContainer();

                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true,
                    //CookieContainer = cookieContainer,
                    //AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli
                    CookieContainer = cookieContainer,
                    //ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
                    UseCookies = true
                };
                //cookieContainer.Add(baseAddress, new Cookie("SESSION", "OTMwMjEzZGMtMGEzZS00NWVjLWI4ZjUtYzRkYjdlOGMxYjBk"));

                using var client = new HttpClient(handler) { BaseAddress = baseAddress };

                // สร้าง byte array แบบสุ่ม (36 bytes จะให้ผล Base64 ~48 ตัวอักษร)
                var randomBytes = new byte[36];
                using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
                {
                    rng.GetBytes(randomBytes);
                }

                // แปลงเป็น Base64 แล้วลบ padding (=)
                var randomCookie = Convert.ToBase64String(randomBytes).TrimEnd('=');

                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("PostmanRuntime/7.44.1");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));               
                client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
                client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
                client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));
                client.DefaultRequestHeaders.Connection.Add("keep-alive");
                client.DefaultRequestHeaders.Add("Postman-Token", Guid.NewGuid().ToString().ToLower());
                client.DefaultRequestHeaders.Add("Cookie", "SESSION="+ randomCookie);


                var boundary = "--------------------------271691758390784738182967";
                var form = new MultipartFormDataContent(boundary);
                form.Headers.ContentType = new MediaTypeHeaderValue("multipart/form-data");
                form.Headers.ContentType.Parameters.Add(new NameValueHeaderValue("boundary", boundary));
                form.Add(new StringContent("devadmin"), "username");
                form.Add(new StringContent("161ebd7d45089b3446ee4e0d86dbcf92"), "password");
                form.Add(new StringContent("NORMAL"), "loginType");
                form.Add(new StringContent("d97e00e85dae5961ac05bb9cc6b717db"), "browserToken");

                //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls13;
                var response = await client.PostAsync("/login.do", form).ConfigureAwait(true);
                var setCookieHeader = response.Headers
                .FirstOrDefault(h => h.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase));
                response.EnsureSuccessStatusCode();

                //////var client2 = new HttpClient(handler);
                //////ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                //////var request2 = new HttpRequestMessage(HttpMethod.Post, "https://10.2.1.120:8098/login.do");
                ////////request2.Headers.Add("Cookie", "SESSION=ZThhYmIxY2EtZTE4Zi00Y2Q2LWEzZmYtY2VhNGY4ZDkyOTQ4");
                //////var content2 = new MultipartFormDataContent();
                //////content2.Add(new StringContent("devadmin"), "username");
                //////content2.Add(new StringContent("161ebd7d45089b3446ee4e0d86dbcf92"), "password");
                //////content2.Add(new StringContent("NORMAL"), "loginType");
                //////content2.Add(new StringContent("d97e00e85dae5961ac05bb9cc6b717db"), "browserToken");
                //////request2.Content = content2;
                //////var response2 = await client2.SendAsync(request2);
                //////response2.EnsureSuccessStatusCode();

                //            var options = new RestClientOptions("https://10.2.1.120:8098")
                //            {
                //                MaxTimeout = -1,
                //                RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true
                //            };
                //            var client2 = new RestClient(options);
                //            var request2 = new RestRequest("/login.do", Method.Post);
                //            request2.AddHeader("Cookie", "SESSION=ZThhYmIxY2EtZTE4Zi00Y2Q2LWEzZmYtY2VhNGY4ZDkyOTQ4");
                //            request2.AlwaysMultipartFormData = true;
                //            request2.AddParameter("username", "devadmin");
                //            request2.AddParameter("password", "161ebd7d45089b3446ee4e0d86dbcf92");
                //            request2.AddParameter("loginType", "NORMAL");
                //            request2.AddParameter("browserToken", "d97e00e85dae5961ac05bb9cc6b717db");
                //            RestResponse response2 = await client2.ExecuteAsync(request2);
                //            var setCookieHeader = response2.Headers
                //.FirstOrDefault(h => h.Name.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase));
                //            var sessionCookie = setCookieHeader != null ? new Cookie("SESSION", setCookieHeader.Value.ToString().Split(';')[0]) : null;

                var sessionCookie = cookieContainer.GetCookies(baseAddress)["SESSION"];

                boundary = "----WebKitFormBoundaryphd11S0gXickyhWo";
                var content = new MultipartFormDataContent(boundary);

                var endTime = DateTime.Now;
                var startTime = endTime;
                string queryConditions = $"startTime={startTime:yyyy-MM-dd}%2000:00:00&endTime={endTime:yyyy-MM-dd}%2023:59:59";

                // เพิ่ม form fields
                content.Add(new StringContent("{\"eventTime\":\"Time\",\"areaName\":\"Area+Name\",\"devAlias\":\"Device+Name\",\"eventPointName\":\"Event+Point\",\"eventName\":\"Event+Description\",\"levelAndEventPriority\":\"Event+Level\",\"vidLinkageHandle\":\"Media+File\",\"pin\":\"Personnel+ID\",\"name\":\"First+Name\",\"lastName\":\"Last+Name\",\"cardNo\":\"Card+Number\",\"deptName\":\"Department+Name\",\"readerName\":\"Reader+Name\",\"verifyModeName\":\"Verification+Mode\"}"), "jsonColumn");
                content.Add(new StringContent("com.zkteco.zkbiosecurity.acc.vo.AccTransactionItem"), "pageXmlPath");
                content.Add(new StringContent("All+Transactions"), "tableNameParam");
                content.Add(new StringContent("accTransaction.do?list"), "treeId");
                content.Add(new StringContent("50"), "pageSize");
                content.Add(new StringContent("99999999"), "records");
                content.Add(new StringContent("100000"), "maxExportCount");
                content.Add(new StringContent(password), "loginPwd"); // ใช้ password จาก config
                content.Add(new StringContent("0"), "isEncrypt");
                content.Add(new StringContent("CSV"), "reportType");
                content.Add(new StringContent("false"), "reportType_new_value");
                content.Add(new StringContent("1"), "exportType");
                content.Add(new StringContent(queryConditions), "queryConditions");
                content.Add(new StringContent(browserToken), "browserToken");

                string filename = $"All+Transactions_{DateTime.Now:yyyyMMddHHmmss}.csv";
                string fullOutputPath = Path.Combine(outputPath, filename);

                //client.DefaultRequestHeaders.Add("Cookie", $"org.springframework.web.servlet.i18n.CookieLocaleResolver.LOCALE=en-US; agreePolicy=1; SESSION={sessionCookie?.Value}; Path=/; Secure; HttpOnly;");
                //cookieContainer.Add(baseAddress, new Cookie("SESSION", sessionCookie?.Value));

                var request = new HttpRequestMessage(HttpMethod.Post, urlPull)
                {
                    Content = content
                };
                foreach (var h in client.DefaultRequestHeaders)
                {
                    request.Headers.TryAddWithoutValidation(h.Key, h.Value);
                }
                //request.Headers.Add("Cookie", $"org.springframework.web.servlet.i18n.CookieLocaleResolver.LOCALE=en-US; agreePolicy=1; SESSION={sessionCookie?.Value};");

                string curlCommand = await BuildCurl(request, fullOutputPath);

                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {curlCommand}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                string stdout = await process.StandardOutput.ReadToEndAsync();
                string stderr = await process.StandardError.ReadToEndAsync();
                process.WaitForExit();                
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private async Task<string> BuildCurl(HttpRequestMessage request, string outputPath)
        {
            var curl = new StringBuilder();
            curl.Append($"curl --insecure --location \"{request.RequestUri}\"");

            foreach (var header in request.Headers)
            {
                foreach (var value in header.Value)
                {
                    curl.Append($" --header \"{header.Key.ToLower()}: {value}\"");
                }
            }
            if (request.Content?.Headers != null)
            {
                foreach (var header in request.Content.Headers)
                {
                    foreach (var value in header.Value)
                    {
                        curl.Append($" --header \"{header.Key.ToLower()}: {value}\"");
                    }
                }
            }

            if (request.Content is MultipartFormDataContent multiPart)
            {
                foreach (var content in multiPart)
                {
                    var name = content.Headers.ContentDisposition?.Name?.Trim('"');
                    var value = await content.ReadAsStringAsync();
                    value = value.Replace("\"", "\\\""); // escape "
                    curl.Append($" --form \"{name}={value}\"");
                }
            }

            curl.Append($" --output \"{outputPath}\"");

            return curl.ToString();
        }
    }
}
