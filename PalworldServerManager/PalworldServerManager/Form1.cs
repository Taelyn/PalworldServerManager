﻿using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PalworldServerManager
{
    public partial class Form1 : Form
    {
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000;  // WS_EX_COMPOSITED
                return cp;
            }
        }


        public string publicIP;
        public string localIP;

        // To download Palworld Server using steamcmd
        // steamcmd +login anonymous +app_update 2394010 validate +quit
        //
        Form_ServerSettings serverSettingsForm;
        public Form_RCON rconForm;
        Form_ServerRestart serverRestartForm;
        public bool isServerStarted = false;
        public Form_DiscordWebHook discordWebHookForm;

        public Form1()
        {
            LoadLanguage();
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            OnLoad();
        }

        private void LoadLanguage()
        {
            string selectedLanguage = Properties.Settings.Default.Seleceted_Language;

            if (!string.IsNullOrEmpty(selectedLanguage))
            {
                try
                {
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo(selectedLanguage);
                }
                catch (CultureNotFoundException ex)
                {
                    //Default Language if somehow the code fails to get the language in a readable state
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-GB");
                }
            }
            else
            {
                //Default Language
                Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-GB");
            }
        }

        private void OnLoad()
        {
            //Load Form
            rconForm = new Form_RCON();
            serverRestartForm = new Form_ServerRestart(this);
            discordWebHookForm = new Form_DiscordWebHook(this);
            serverSettingsForm = new Form_ServerSettings(this);

            LoadForm(serverSettingsForm, true);
            LoadForm(rconForm, true);
            LoadForm(serverRestartForm, true);
            LoadForm(discordWebHookForm, true);


        }

        private void LoadForm(Form formToLoad, bool isShow)
        {

            if (formToLoad != null)
            {
                formToLoad.TopLevel = false;
                panel_chilForm.Controls.Add(formToLoad);
                formToLoad.FormBorderStyle = FormBorderStyle.None;
                formToLoad.Dock = DockStyle.Fill;
                if (isShow)
                {
                    formToLoad.Show();
                }
                else
                {
                    formToLoad.Hide();
                }

            }
        }

        private void ShowForm(Form formToShow)
        {
            foreach (Control control in panel_chilForm.Controls)
            {
                if (control is Form form)
                {
                    //hide the form
                    form.Hide();
                }
            }

            if (formToShow != null)
            {
                formToShow.Show();
            }
        }

        //MAIN FORM SECTION
        private async Task<string> GetPublicIpAddressAsync()
        {
            try
            {
                using HttpClient httpClient = new();

                // Await the async GET request
                string response = await httpClient.GetStringAsync("https://api.ipify.org?format=json");

                // Parse the JSON response
                using JsonDocument doc = JsonDocument.Parse(response);
                string publicIpAddress = doc.RootElement.GetProperty("ip").GetString();

                Debug.WriteLine(publicIpAddress);
                return publicIpAddress;
            }
            catch (Exception ex)
            {
                return "Error getting public IP address: " + ex.Message;
            }
        }

        private string GetLocalAddress()
        {
            try
            {
                string localIpAddress = "";
                IPAddress[] ip = Dns.GetHostAddresses(Dns.GetHostName());
                foreach (IPAddress address in ip)
                {
                    if (address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        localIpAddress = address.ToString();
                        Debug.WriteLine(localIpAddress);
                        return localIpAddress;
                    }
                }
                return localIpAddress;
            }
            catch (Exception ex)
            {
                return "Error getting local IP address: " + ex.Message;
            }
        }

        private async void button4_Click(object sender, EventArgs e)
        {
            publicIP = await GetPublicIpAddressAsync();
            textBox1.Text = publicIP;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            localIP = GetLocalAddress();
            textBox2.Text = localIP;
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            string zipUrl = "https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip";
            string fileName = "steamcmd.zip";
            string savePath = Path.Combine("D:/", "SteamCMD", fileName);

            try
            {
                using HttpClient httpClient = new();

                // Download the file asynchronously as byte[]
                byte[] data = await httpClient.GetByteArrayAsync(zipUrl);

                // Save the zip file to disk
                await File.WriteAllBytesAsync(savePath, data);

                // Extract the zip archive
                ZipFile.ExtractToDirectory(savePath, Path.Combine("D:/", "SteamCMD"), overwriteFiles: true);

                serverSettingsForm.SendMessageToConsole("Download and extraction of steamcmd completed!");
            }
            catch (Exception ex)
            {
                serverSettingsForm.SendMessageToConsole($"Download steamcmd caught error: {ex.Message}");
            }
            finally
            {
                try { File.Delete(savePath); } catch { /* ignore cleanup errors */ }
            }
        }

        private void RunDownloadServerBatchFile()
        {
            try
            {
                //Run bat file
                try
                {
                    // Create a new process to run the batch file
                    Process process = new()
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = Path.Combine("D:/", "SteamCMD", "Palworld Update.cmd"),
                            UseShellExecute = true,
                            CreateNoWindow = false
                        }
                    };

                    // Start the process
                    process.Start();
                    process.WaitForExit();
                    MessageBox.Show("Finished Download/Verify/Update Server");

                }
                catch (Exception ex)
                {
                    MessageBox.Show($"RunDownloadServerBatFile catched error: {ex.Message}");
                }

                //MessageBox.Show("Batch file generated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"RunDownloadServerBatFile catched error: {ex.Message}");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (!CheckSteamCMD())
            {
                return;
            }

            bool isFirstTime = false;

            if (!CheckPalServer())
            {
                isFirstTime = true;
            }


            if (!isFirstTime)
            {
                DialogResult askBeforeDownloadUpdateVerify = MessageBox.Show("Please create a manual backup before proceeding \npress Yes to continue, press no to cancel the download/update/verify process", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (askBeforeDownloadUpdateVerify == DialogResult.Yes)
                {
                    //What to do
                }
                else
                {
                    return;
                }
            }

            try
            {
                // Start a new thread to run the batch file asynchronously
                Thread thread = new(new ThreadStart(RunDownloadServerBatchFile));
                thread.Start();
                serverSettingsForm.SendMessageToConsole("Started Download/Verify/Update Server");
            }
            catch (Exception ex)
            {
                serverSettingsForm.SendMessageToConsole($"Download/Update/Verify button catched error: {ex.Message}");
            }
        }

        private void RunStartServerBatchFile()
        {
            try
            {
                Process serverProcess = new()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = Path.Combine("G:/", "Palworld - Dedicated Server", "Pal", "Binaries", "Win64", "PalServer-Win64-Shipping-Cmd.exe"),
                        Arguments = serverSettingsForm.serv_customServerLaunchArgument,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = false,
                        RedirectStandardError = false,
                        WindowStyle = ProcessWindowStyle.Hidden
                    },
                    EnableRaisingEvents = false
                };

                serverProcess.Start();

                serverProcess.WaitForExit();
                serverSettingsForm.SendMessageToConsole("Server process exited.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Run server start catched error: {ex.Message}");
            }
        }

        private bool CheckSteamCMD()
        {
            if (File.Exists(Path.Combine("D:/", "SteamCMD", "steamcmd.exe")))
            {
                return true;
            }
            else
            {
                serverSettingsForm.SendMessageToConsole("Missing steamcmd.exe \nPress download steamcmd");
                return false;
            }
        }

        private bool CheckPalServer()
        {
            if (File.Exists(Path.Combine("G:/", "Palworld - Dedicated Server", "PalServer.exe")))
            {
                return true;
            }
            else
            {
                serverSettingsForm.SendMessageToConsole($"Missing PalServer.exe \nPress the download/update/verify server button to validate your files.");
                return false;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            StartServer();
        }

        public void StartServer()
        {
            if (!CheckSteamCMD())
            {
                return;
            }

            if (!CheckPalServer())
            {
                return;
            }

            if (!isServerStarted)
            {
                try
                {
                    serverSettingsForm.SendMessageToConsole("Server Started");

                    // Start a new thread to run the batch file asynchronously
                    Thread thread = new(new ThreadStart(RunStartServerBatchFile));
                    thread.Start();
                    serverSettingsForm.SaveGameTimer_Start();
                    serverSettingsForm.AutoRestartServerTimer_Start();
                    serverSettingsForm.Start_OnCMDCrashRestartTimer();
                    serverSettingsForm.BackUpAlertTimer_Start();
                    serverSettingsForm.ServerRestartAlertTimer_Start();
                    discordWebHookForm.SendEmbed("Notification", "🟢 Server has started");
                    isServerStarted = true;
                    button_startServer.Enabled = false;
                    button_startServer.BackColor = Color.Green;
                    button_stopServer.Enabled = true;

                }
                catch (Exception ex)
                {
                    serverSettingsForm.SendMessageToConsole($"Server start catched error: {ex.Message}");
                }
            }
        }

        private void button_stopServer_Click(object sender, EventArgs e)
        {
            StopServer();
        }

        public void StopServer()
        {
            if (isServerStarted)
            {

                // Specify the name of the process without the .exe extension
                string processName = "PalServer-Win64-Shipping-Cmd";

                // Find and kill the process if it's running
                Process[] processes = Process.GetProcessesByName(processName);
                foreach (Process process in processes)
                {
                    process.Kill();
                }
                isServerStarted = false;
                button_startServer.BackColor = Color.Red;
                button_startServer.Enabled = true;
                button_stopServer.Enabled = false;
                serverSettingsForm.SaveGameTimer_Stop();
                serverSettingsForm.AutoRestartServerTimer_Stop();
                serverSettingsForm.Stop_OnCMDCrashRestartTimer();
                serverSettingsForm.BackUpAlertTimer_Stop();
                serverSettingsForm.ServerRestartAlertTimer_Stop();
                serverSettingsForm.SendMessageToConsole("Server Stopped");
                discordWebHookForm.SendEmbed("Notification", "🔴 Server Stopped");
            }
        }

        //TOOLSTRIPMENU SECTION
        private void englishToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ChangeLanguage("en-GB", "English"); //culture code, languagename(to let myself know what language it is)
            Application.Restart();
        }

        private void chineseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ChangeLanguage("zh-Hans", "Chinese");
            Application.Restart();
        }

        private void ChangeLanguage(string cultureCode, string languageName)
        {
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(cultureCode);
            Properties.Settings.Default.Seleceted_Language = cultureCode;
            Properties.Settings.Default.Save();
        }

        private void rCONToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowForm(rconForm);
        }

        private void serverSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void baseDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDirectoryGiven(Path.Combine("G:/", "Palworld - Dedicated Server"));
        }

        private void instructionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string title = "Instruction";
            string message = "1) Download\r\n2) Create a new folder\r\n3) Copy the exe to the new folder\r\n4) Run the exe\r\n5) Download steamcmd\r\n6) Download/Update/Verify server\r\n7) Run server (to finish off creating server files)\r\n8) Shutdown server\r\n9) Make any changes you like to server settings\r\n10) Save it and start the server\n\n" +
                "Note: If you want others to join, make sure you have portforwarded your ports and added your ports to inbounrd and outbound firewall.\nCommon ports used in palworld: \n8211 ((UDP)Game Server port), \n27015 ((TCP)Steam Port), \n25575 (RCON Port).";

            MessageBox.Show(message, title);
        }

        private void githubToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            string githubUrl = @"https://github.com/Tianyu-00";

            OpenURLGiven(githubUrl);
        }

        private void repoPageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string githubRepoUrl = "https://github.com/TianYu-00/PalworldServerManager";
            OpenURLGiven(githubRepoUrl);

        }

        private void OpenURLGiven(string URL)
        {
            try
            {
                //Process.Start(githubRepoUrl); //Wont work on .net core
                Process.Start(new ProcessStartInfo { FileName = URL, UseShellExecute = true }); //turns useshellexecute on which is defaulted to off after vs update.
                //System.Diagnostics.Process.Start("explorer.exe", URL); //Works
            }
            catch (Exception ex)
            {
                serverSettingsForm.SendMessageToConsole($"Webpage open catched error: {ex.Message}");
            }
        }

        private void OpenFileDirectoryGiven(string directory)
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = directory, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                serverSettingsForm.SendMessageToConsole($"Open file directory given catched error: {ex.Message}");
            }
        }

        private void serverSettingsToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ShowForm(serverSettingsForm);
        }

        private void serverRestartScheduleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowForm(serverRestartForm);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopServer();
        }

        private void nexusModsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string Url = @"https://www.nexusmods.com/palworld/mods/512/?tab=files";

            OpenURLGiven(Url);
        }

        private void githubToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            string Url = @"https://github.com/TianYu-00/PalworldServerManager/releases/latest";

            OpenURLGiven(Url);
        }

        private void discordWebhookToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowForm(discordWebHookForm);
        }
    }
}
