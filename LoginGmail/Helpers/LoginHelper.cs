using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LoginGmail.Helpers
{
    class LoginHelper
    {
        public static readonly Configs config = JsonConvert.DeserializeObject<Configs>(File.ReadAllText("Configs.json"));
        public static string[] profiles = File.ReadAllLines("Profiles.txt").Where(x => x.Trim() != string.Empty).Where(x => Directory.Exists(x)).ToArray();
        public static string[] mails = File.ReadAllLines("Mails.txt").Where(x => x.Trim() != string.Empty).ToArray();
        public static string[] cookies = File.ReadAllLines("Cookies.txt").Where(x => x.Trim() != string.Empty).ToArray();
        public static string[] sshClients = new string[] { };

        public LoginHelper()
        {
            if (config.Fake_IP == 1)
            {
                DownloadSsh();
            }
        }

        public bool DownloadSsh()
        {
            var status = false;
            try
            {
                using (var client = new WebClient())
                {
                    var sshClients1 = new string[] { };
                    var sshClients2 = new string[] { };
                    //try
                    //{
                    //    var json = client.DownloadString("https://ssh24h.com/APIv2?token=66920a46b12e7aa9c9d35acdf8b48b41&code=" + config.Location);
                    //    sshClients1 = JsonConvert.DeserializeObject<SshResult>(json).ListSSH;
                    //}
                    //catch (Exception)
                    //{
                    //    using (StreamWriter writer = new StreamWriter("Error.txt", true))
                    //    {
                    //        writer.WriteLine("Download 1 lỗi!");
                    //    }
                    //}

                    try
                    {
                        var sshClientsText = client.DownloadString("http://khuongssh.xyz/api.php?username=dauanh2110&country=" + config.Location);
                        //var sshClientsText = File.ReadAllText("ssh.txt");
                        sshClients2 = sshClientsText.Split(
                            new[] { Environment.NewLine },
                            StringSplitOptions.None
                        );
                    }
                    catch (Exception)
                    {
                        using (StreamWriter writer = new StreamWriter("Error.txt", true))
                        {
                            writer.WriteLine("Download 2 lỗi!");
                        }
                    }

                    sshClients = sshClients1.Concat(sshClients2).ToArray();
                }

                status = true;
            }
            catch (Exception)
            {
                using (StreamWriter writer = new StreamWriter("Error.txt", true))
                {
                    writer.WriteLine("Download lỗi!");
                }
            }

            return status;
        }

        public bool Login(string profile, string mail)
        {

            var status = false;
            ChromeDriver driver = null;
            var email = string.Empty;
            try
            {
                var mails = mail.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                if (mails.Length < 3) throw new Exception("Wrong email, password, email recovery format");
                email = mails[0];
                string password = mails[1];
                string recoveryEmail = mails[2];

                string profile_path = Path.GetDirectoryName(profile);
                string profile_name = Path.GetFileName(profile);
                var service = ChromeDriverService.CreateDefaultService();
                service.HideCommandPromptWindow = true;
                var options = new ChromeOptions();
                options.AddArguments("--user-data-dir=" + profile_path);
                options.AddArguments("--profile-directory=" + profile_name);
                //options.AddArguments("--proxy-server=socks5://127.0.0.1:1080");
                options.AddArguments("disable-infobars");
                options.AddArguments("start-maximized");

                driver = new ChromeDriver(service, options);
                driver.Navigate().GoToUrl("https://accounts.google.com/signin/v2/identifier");
                Thread.Sleep(config.Page_Load);

                var elements = driver.FindElements(By.XPath("//*/div[@id='profileIdentifier']")).Where(x => x.Displayed);
                if (elements.Count() == 0)
                {
                    var mailInput = driver.FindElement(By.XPath("//*/input[@id='identifierId']"));
                    mailInput.SendKeys(Keys.Control + "a");
                    mailInput.SendKeys(email);
                    Thread.Sleep(config.Wait_Enter);
                    mailInput.SendKeys(Keys.Enter);
                    Thread.Sleep(config.Page_Load);
                }

                var passwordInput = driver.FindElementsByTagName("input").First(x => x.GetAttribute("name") == "password");
                passwordInput.SendKeys(Keys.Control + "a");
                passwordInput.SendKeys(password);
                Thread.Sleep(config.Wait_Enter);
                passwordInput.SendKeys(Keys.Enter);
                Thread.Sleep(config.Page_Load);

                var confirmRecoveryEmailOptionButtons = driver.FindElementsByClassName("vdE7Oc").Where(x => x.Displayed).ToArray();
                var numOptions = confirmRecoveryEmailOptionButtons.Length;

                if (numOptions > 0)
                {
                    IWebElement confirmRecoveryEmailOptionButton = null;
                    if (numOptions == 1)
                    {
                        confirmRecoveryEmailOptionButton = confirmRecoveryEmailOptionButtons[0];
                        confirmRecoveryEmailOptionButton.Click();
                        Thread.Sleep(config.Page_Load);
                    }
                    else
                    {
                        try
                        {
                            confirmRecoveryEmailOptionButton = confirmRecoveryEmailOptionButtons[numOptions - 2];
                            confirmRecoveryEmailOptionButton.Click();
                            Thread.Sleep(config.Page_Load);
                            var secretQuestionResponses = driver.FindElementsByTagName("input").Where(x => x.GetAttribute("name") == "knowledgePreregisteredEmailResponse").Where(x => x.Displayed);
                            if (secretQuestionResponses.Count() == 0)
                            {
                                var otherMethodButton = driver.FindElement(By.XPath("//*/div[@class='daaWTb']/div[@class='U26fgb O0WRkf oG5Srb HQ8yf C0oVfc nDKKZc NpwL8d NaOGkc']/content[@class='CwaK9']/span[@class='RveJvd snByac']"));
                                otherMethodButton.Click();
                                Thread.Sleep(config.Page_Load);
                                confirmRecoveryEmailOptionButtons = driver.FindElementsByClassName("vdE7Oc").Where(x => x.Displayed).ToArray();
                                confirmRecoveryEmailOptionButton = confirmRecoveryEmailOptionButtons[numOptions - 3];
                                confirmRecoveryEmailOptionButton.Click();
                                Thread.Sleep(config.Page_Load);
                            }
                        }
                        catch (Exception ex)
                        {
                            using (StreamWriter writer = new StreamWriter("Error.txt", true))
                            {
                                writer.WriteLine("Email: " + email + "|" + ex.ToString());
                            }
                        }
                    }
                }

                var recoveryMailInputs = driver.FindElementsByTagName("input").Where(x => x.GetAttribute("name") == "knowledgePreregisteredEmailResponse");
                if (recoveryMailInputs.Count() > 0)
                {
                    var recoveryMailInput = recoveryMailInputs.First();
                    recoveryMailInput.SendKeys(Keys.Control + "a");
                    recoveryMailInput.SendKeys(recoveryEmail);
                    Thread.Sleep(config.Wait_Enter);
                    recoveryMailInput.SendKeys(Keys.Enter);
                    Thread.Sleep(config.Page_Load);
                }
                driver.Navigate().GoToUrl("https://mail.google.com/mail/u/0/#inbox");
                Thread.Sleep(config.Page_Load);

                status = true;
            }
            catch (Exception e)
            {
                using (StreamWriter writer = new StreamWriter("Error.txt", true))
                {
                    writer.WriteLine("Profile: " + Path.GetFileName(profile) + "|Email: " + email + "|" + e.ToString());
                }
            }

            if (driver != null)
            {
                driver.Close();
                driver.Quit();
            }

            return status;
        }

        public void ForwardSsh(ref SshClient client, ref ForwardedPortDynamic port)
        {
            var random = new Random();
            do
            {
                try
                {
                    var randomSshClient = sshClients[random.Next(sshClients.Length)];
                    var sshClientInfo = randomSshClient.Split('|');
                    client = new SshClient(sshClientInfo[0], sshClientInfo[1], sshClientInfo[2]);
                    client.KeepAliveInterval = new TimeSpan(0, 0, config.IP_Alive_Interval);
                    client.ConnectionInfo.Timeout = new TimeSpan(0, 0, config.IP_Timeout);
                    client.Connect();
                    if (client.IsConnected)
                    {
                        port = new ForwardedPortDynamic("127.0.0.1", 1080);
                        client.AddForwardedPort(port);
                        port.Start();
                        break;
                    }
                }
                catch (Exception ex)
                {
                    File.AppendAllText("Error.txt", ex.ToString());
                }
            } while (true);
        }

        public bool LoginFakeIP(string profile, string mail)
        {

            var status = false;
            ChromeDriver driver = null;
            ForwardedPortDynamic port = null;
            SshClient client = null;
            ForwardSsh(ref client, ref port);
            var email = string.Empty;
            try
            {
                var mails = mail.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                if (mails.Length < 3) throw new Exception("Wrong email, password, email recovery format");
                email = mails[0];
                string password = mails[1];
                string recoveryEmail = mails[2];

                string profile_path = Path.GetDirectoryName(profile);
                string profile_name = Path.GetFileName(profile);
                var service = ChromeDriverService.CreateDefaultService();
                service.HideCommandPromptWindow = true;
                var options = new ChromeOptions();
                options.AddArguments("--user-data-dir=" + profile_path);
                options.AddArguments("--profile-directory=" + profile_name);
                options.AddArguments("--proxy-server=socks5://127.0.0.1:1080");
                options.AddArguments("disable-infobars");
                options.AddArguments("start-maximized");

                driver = new ChromeDriver(service, options);
                driver.Navigate().GoToUrl("https://accounts.google.com/signin/v2/identifier");
                Thread.Sleep(config.Page_Load);

                var elements = driver.FindElements(By.XPath("//*/div[@id='profileIdentifier']")).Where(x => x.Displayed);
                if (elements.Count() == 0)
                {
                    var mailInput = driver.FindElement(By.XPath("//*/input[@id='identifierId']"));
                    mailInput.SendKeys(Keys.Control + "a");
                    mailInput.SendKeys(email);
                    Thread.Sleep(config.Wait_Enter);
                    mailInput.SendKeys(Keys.Enter);
                    Thread.Sleep(config.Page_Load);
                }

                var passwordInput = driver.FindElementsByTagName("input").First(x => x.GetAttribute("name") == "password");
                passwordInput.SendKeys(Keys.Control + "a");
                passwordInput.SendKeys(password);
                Thread.Sleep(config.Wait_Enter);
                passwordInput.SendKeys(Keys.Enter);
                Thread.Sleep(config.Page_Load);

                var confirmRecoveryEmailOptionButtons = driver.FindElementsByClassName("vdE7Oc").Where(x => x.Displayed).ToArray();
                var numOptions = confirmRecoveryEmailOptionButtons.Length;

                if (numOptions > 0)
                {
                    IWebElement confirmRecoveryEmailOptionButton = null;
                    if (numOptions == 1)
                    {
                        confirmRecoveryEmailOptionButton = confirmRecoveryEmailOptionButtons[0];
                        confirmRecoveryEmailOptionButton.Click();
                        Thread.Sleep(config.Page_Load);
                    }
                    else
                    {
                        try
                        {
                            confirmRecoveryEmailOptionButton = confirmRecoveryEmailOptionButtons[numOptions - 2];
                            confirmRecoveryEmailOptionButton.Click();
                            Thread.Sleep(config.Page_Load);
                            var secretQuestionResponses = driver.FindElementsByTagName("input").Where(x => x.GetAttribute("name") == "knowledgePreregisteredEmailResponse").Where(x => x.Displayed);
                            if (secretQuestionResponses.Count() == 0)
                            {
                                var otherMethodButton = driver.FindElement(By.XPath("//*/div[@class='daaWTb']/div[@class='U26fgb O0WRkf oG5Srb HQ8yf C0oVfc nDKKZc NpwL8d NaOGkc']/content[@class='CwaK9']/span[@class='RveJvd snByac']"));
                                otherMethodButton.Click();
                                Thread.Sleep(config.Page_Load);
                                confirmRecoveryEmailOptionButtons = driver.FindElementsByClassName("vdE7Oc").Where(x => x.Displayed).ToArray();
                                confirmRecoveryEmailOptionButton = confirmRecoveryEmailOptionButtons[numOptions - 3];
                                confirmRecoveryEmailOptionButton.Click();
                                Thread.Sleep(config.Page_Load);
                            }
                        }
                        catch (Exception ex)
                        {
                            using (StreamWriter writer = new StreamWriter("Error.txt", true))
                            {
                                writer.WriteLine("Email: " + email + "|" + ex.ToString());
                            }
                        }
                    }
                }

                var recoveryMailInputs = driver.FindElementsByTagName("input").Where(x => x.GetAttribute("name") == "knowledgePreregisteredEmailResponse");
                if (recoveryMailInputs.Count() > 0)
                {
                    var recoveryMailInput = recoveryMailInputs.First();
                    recoveryMailInput.SendKeys(Keys.Control + "a");
                    recoveryMailInput.SendKeys(recoveryEmail);
                    Thread.Sleep(config.Wait_Enter);
                    recoveryMailInput.SendKeys(Keys.Enter);
                    Thread.Sleep(config.Page_Load);
                }
                driver.Navigate().GoToUrl("https://mail.google.com/mail/u/0/#inbox");
                Thread.Sleep(config.Page_Load);

                status = true;
            }
            catch (Exception e)
            {
                using (StreamWriter writer = new StreamWriter("Error.txt", true))
                {
                    writer.WriteLine("Profile: " + Path.GetFileName(profile) + "|Email: " + email + "|" + e.ToString());
                }
            }

            if (driver != null)
            {
                driver.Close();
                driver.Quit();
            }

            if (port != null)
            {
                port.Stop();
            }

            if (client != null)
            {
                client.Disconnect();
            }

            return status;
        }

        public bool LoginByCookie(string profile, string allCookies)
        {

            var status = false;
            ChromeDriver driver = null;
            var email = string.Empty;
            try
            {
                string profile_path = Path.GetDirectoryName(profile);
                string profile_name = Path.GetFileName(profile);
                var service = ChromeDriverService.CreateDefaultService();
                service.HideCommandPromptWindow = true;
                var options = new ChromeOptions();
                options.AddArguments("--user-data-dir=" + profile_path);
                options.AddArguments("--profile-directory=" + profile_name);
                options.AddArguments("disable-infobars");
                options.AddArguments("start-maximized");
                driver = new ChromeDriver(service, options);

                driver.Navigate().GoToUrl("https://www.youtube.com");
                var cookies = allCookies.Split(';');
                foreach (var cookie in cookies)
                {
                    var pair = cookie.Split('=');
                    if (pair.Count() == 2)
                    {
                        driver.Manage().Cookies.AddCookie(new OpenQA.Selenium.Cookie(pair[0].Trim(), pair[1].Trim()));
                    }
                }
                driver.Navigate().Refresh();

                Thread.Sleep(config.Page_Load);

                status = true;
            }
            catch (Exception e)
            {
                using (StreamWriter writer = new StreamWriter("Error.txt", true))
                {
                    writer.WriteLine("Profile: " + Path.GetFileName(profile) + "|Email: " + email + "|" + e.ToString());
                }
            }

            if (driver != null)
            {
                driver.Close();
                driver.Quit();
            }

            return status;
        }

        public bool LoginByCookieFakeIP(string profile, string allCookies)
        {

            var status = false;
            ChromeDriver driver = null;
            var email = string.Empty;
            ForwardedPortDynamic port = null;
            SshClient client = null;
            ForwardSsh(ref client, ref port);
            try
            {
                string profile_path = Path.GetDirectoryName(profile);
                string profile_name = Path.GetFileName(profile);
                var service = ChromeDriverService.CreateDefaultService();
                service.HideCommandPromptWindow = true;
                var options = new ChromeOptions();
                options.AddArguments("--user-data-dir=" + profile_path);
                options.AddArguments("--profile-directory=" + profile_name);
                options.AddArguments("--proxy-server=socks5://127.0.0.1:1080");
                options.AddArguments("disable-infobars");
                options.AddArguments("start-maximized");
                driver = new ChromeDriver(service, options);

                driver.Navigate().GoToUrl("https://www.youtube.com");
                var cookies = allCookies.Split(';');
                foreach (var cookie in cookies)
                {
                    var pair = cookie.Split('=');
                    if (pair.Count() == 2)
                    {
                        driver.Manage().Cookies.AddCookie(new OpenQA.Selenium.Cookie(pair[0].Trim(), pair[1].Trim()));
                    }
                }
                driver.Navigate().Refresh();

                Thread.Sleep(config.Page_Load);

                status = true;
            }
            catch (Exception e)
            {
                using (StreamWriter writer = new StreamWriter("Error.txt", true))
                {
                    writer.WriteLine("Profile: " + Path.GetFileName(profile) + "|Email: " + email + "|" + e.ToString());
                }
            }

            if (driver != null)
            {
                driver.Close();
                driver.Quit();
            }

            if (port != null)
            {
                port.Stop();
            }

            if (client != null)
            {
                client.Disconnect();
            }

            return status;
        }
    }
}
