using LoginGmail.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LoginGmail
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            if (IsProgramExpired()) this.Close();

            InitializeComponent();
        }

        static bool IsProgramExpired()
        {
            var status = true;
            try
            {
                var myHttpWebRequest = (HttpWebRequest)WebRequest.Create("http://www.microsoft.com");
                var response = myHttpWebRequest.GetResponse();
                string todaysDates = response.Headers["date"];

                var expiredTime = new DateTime(2019, 10, 1);
                var currentTime = DateTime.ParseExact(todaysDates,
                                       "ddd, dd MMM yyyy HH:mm:ss 'GMT'",
                                       CultureInfo.InvariantCulture.DateTimeFormat,
                                       DateTimeStyles.AssumeUniversal);
                if (DateTime.Compare(currentTime, expiredTime) < 0) status = false;
            }
            catch (Exception) { }

            return status;
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            btnStart.IsEnabled = false;
            lblStatus.Content = "Đang chạy...";
            var thread = new Thread(StartProgram);
            thread.Start();
        }

        public void StartProgram()
        {
            var loginHelper = new LoginHelper();
            try
            {
                var nSuccess = 0;
                do
                {
                    var doneProfiles = File.ReadAllLines("DoneProfiles.txt").Where(x => x.Trim() != string.Empty).ToArray();
                    var doneMails = File.ReadAllLines("DoneMails.txt").Where(x => x.Trim() != string.Empty).ToArray();
                    var remainProfiles = LoginHelper.profiles.Except(doneProfiles).ToArray();
                    var remainMails = LoginHelper.mails.Except(doneMails).ToArray();
                    if (remainProfiles.Count() == 0) break;
                    var status = false;
                    if (LoginHelper.config.Fake_IP == 0)
                    {
                        if (LoginHelper.config.Login_Type == 1)
                        {
                            status = loginHelper.Login(remainProfiles[0], remainMails[0]);
                        }
                        else
                        {
                            status = loginHelper.LoginByCookie(remainProfiles[0], remainMails[0]);
                        }
                    }
                    else
                    {
                        if (LoginHelper.config.Login_Type == 1)
                        {
                            status = loginHelper.LoginFakeIP(remainProfiles[0], remainMails[0]);
                        }
                        else
                        {
                            status = loginHelper.LoginByCookieFakeIP(remainProfiles[0], remainMails[0]);
                        }
                    }


                    if (status)
                    {
                        nSuccess++;
                        File.AppendAllText("DoneProfiles.txt", remainProfiles[0] + "\n");
                        File.AppendAllText("DoneMails.txt", remainMails[0] + "\n");
                        this.Dispatcher.Invoke(() =>
                        {
                            lblLoggedInEmails.Content = nSuccess.ToString();
                        });
                    }
                } while (true);
            }
            catch (Exception ex)
            {
                File.AppendAllText("Error.txt", ex.ToString());
            }

            this.Dispatcher.Invoke(() =>
            {
                lblStatus.Content = "Hoàn thành";
            });
        }
    }
}
