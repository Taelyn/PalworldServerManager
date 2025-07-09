﻿using Palworld.RESTSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PalworldServerManager
{
    public partial class Form_ServerRestart : Form
    {



        private List<Tuple<CheckBox, DateTimePicker, DateTimePicker, Button>> settingsList;
        private Timer timer;
        private Form1 mainForm;


        public Form_ServerRestart(Form1 form)
        {
            InitializeComponent();
            mainForm = form;
        }

        private void Form_ServerRestart_Load(object sender, EventArgs e)
        {
            settingsList = new List<Tuple<CheckBox, DateTimePicker, DateTimePicker, Button>>();

            // Load settings from file
            LoadSettings();

            // Create and configure the timer
            timer = new Timer();
            timer.Interval = 1000; // Check every second
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            CheckSettings();
        }

        private void button_addSchedule_Click(object sender, EventArgs e)
        {
            CheckBox checkBox = new CheckBox();
            DateTimePicker datePicker = new DateTimePicker();
            DateTimePicker timePicker = new DateTimePicker();
            Button deleteButton = new Button();

            int rowCount = tableLayoutPanel1.RowCount;
            tableLayoutPanel1.RowCount = rowCount + 1;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.AutoSize));



            // Add controls to the tableLayoutPanel
            tableLayoutPanel1.Controls.Add(checkBox, 0, rowCount);
            tableLayoutPanel1.Controls.Add(datePicker, 1, rowCount);
            tableLayoutPanel1.Controls.Add(timePicker, 2, rowCount);
            tableLayoutPanel1.Controls.Add(deleteButton, 3, rowCount);

            // Set properties for the controls
            checkBox.Text = "Enable";
            checkBox.Dock = DockStyle.Fill;

            datePicker.Format = DateTimePickerFormat.Long;
            datePicker.ShowCheckBox = true;
            datePicker.Checked = false;
            datePicker.Dock = DockStyle.Fill;

            timePicker.Format = DateTimePickerFormat.Time;
            timePicker.Dock = DockStyle.Fill;
            timePicker.ShowUpDown = true;

            deleteButton.Dock = DockStyle.Fill;
            deleteButton.Text = "Delete";
            deleteButton.Click += (deleteSender, deleteArgs) => DeleteRow(deleteButton);



            // Add controls to the settings list
            settingsList.Add(new Tuple<CheckBox, DateTimePicker, DateTimePicker, Button>(checkBox, datePicker, timePicker, deleteButton));
            // Save settings to file


        }

        private void DeleteRow(Button deleteButton)
        {
            // Find the corresponding tuple in settingsList based on the deleteButton
            var tupleToDelete = settingsList.Find(tuple => tuple.Item4 == deleteButton);

            if (tupleToDelete != null)
            {
                // Get the index of the tuple in settingsList
                int tupleIndex = settingsList.IndexOf(tupleToDelete);

                // Remove controls from the tableLayoutPanel
                tableLayoutPanel1.Controls.Remove(tupleToDelete.Item1);
                tableLayoutPanel1.Controls.Remove(tupleToDelete.Item2);
                tableLayoutPanel1.Controls.Remove(tupleToDelete.Item3);
                tableLayoutPanel1.Controls.Remove(tupleToDelete.Item4);

                // Remove the row from tableLayoutPanel1
                tableLayoutPanel1.RowStyles.RemoveAt(tupleIndex);
                tableLayoutPanel1.RowCount--;

                // Remove the tuple from the settingsList
                settingsList.Remove(tupleToDelete);
            }
        }


        private async void CheckSettings()
        {
            if (mainForm.isServerStarted)
            {
                //Current time
                DateTime currentDateTime = DateTime.Now;
                //Debug.WriteLine($"Current Time: {currentDateTime}");
                string currentDateString = currentDateTime.ToString("yyyy/MM/dd");
                string currentTimeString = currentDateTime.ToString("HH:mm:ss");
                foreach (var setting in settingsList)
                {
                    if (setting.Item1.Checked)
                    {
                        // Get the index of the current tuple in settingsList
                        int rowIndex = settingsList.IndexOf(setting);

                        //Item Time
                        DateTime itemsDateTime = setting.Item2.Value.Date + setting.Item3.Value.TimeOfDay;

                        string itemDateString = itemsDateTime.ToString("yyyy/MM/dd");
                        string itemTimeString = itemsDateTime.ToString("HH:mm:ss");
                        //Using time
                        string usingDate;
                        string usingTime = itemTimeString;


                        //If date checkbox is checked, it will set using date as today
                        if (setting.Item2.Checked)
                        {
                            usingDate = itemDateString;
                        }
                        else
                        {
                            usingDate = currentDateString;
                        }

                        Debug.WriteLine($"ROWINDEX & Date: {rowIndex}, {usingDate},{usingTime}");
                        //When matched
                        if (usingDate == currentDateString && usingTime == currentTimeString)
                        {
                            //MessageBox.Show("Matched");
                            if (mainForm != null)
                            {
                                var client = RestAPI.CreatePalworldClient();
                                await client.ShutdownASync(300, "Server will reboot in 5 minutes! Please log out to prevent data loss. Kick in 4 minutes");

                                // Wait 3 minutes before starting 2-minute warning
                                await Task.Delay(180000); // 3 minutes
                                await client.BroadcastMessageASync("Server will reboot in 2 minutes! Please log out to prevent data loss. Kick in 1 minute");

                                // Wait 1 more minute for 1-minute warning
                                await Task.Delay(60000); // 1 minute
                                await client.BroadcastMessageASync("Server will reboot in 1 minute! Kick now");

                                var players = await client.GetPlayersASync();

                                foreach (Player player in players.players)
                                {
                                    await client.KickPlayerASync(player.UserID, "Server Reboot! Please wait 2 minutes before logging in");
                                }

                                await client.SaveWorldASync();

                                client.Dispose();

                                //mainForm.StopServer();

                                Process[] processes;
                                do
                                {
                                    await Task.Delay(5000); // check every 5s
                                    processes = Process.GetProcessesByName("PalServer-Win64-Shipping-Cmd");
                                } while (processes.Length > 0);

                                mainForm.isServerStarted = false;
                                mainForm.StartServer();
                            }
                            else
                            {
                                Debug.WriteLine("mainForm is null");
                            }
                        }
                    }
                }
            }
        }

        private void LoadSettings()
        {
            if (File.Exists("ServerRestartScheduleSettings.json"))
            {
                string json = File.ReadAllText("ServerRestartScheduleSettings.json");
                var settings = JsonSerializer.Deserialize<List<ScheduleSettings>>(json);

                foreach (var setting in settings)
                {
                    // Create controls
                    CheckBox checkBox = new CheckBox();
                    DateTimePicker datePicker = new DateTimePicker();
                    DateTimePicker timePicker = new DateTimePicker();
                    Button deleteButton = new Button();

                    // Set control properties
                    checkBox.Text = "Enable";
                    checkBox.Dock = DockStyle.Fill;
                    checkBox.Checked = setting.Enabled;

                    datePicker.Value = setting.Date;
                    datePicker.Format = DateTimePickerFormat.Long;
                    datePicker.ShowCheckBox = true;
                    datePicker.Checked = setting.DateEnabled; // Set the Checked property based on setting
                    datePicker.Dock = DockStyle.Fill;

                    timePicker.Value = DateTime.Today + setting.Time;
                    timePicker.Format = DateTimePickerFormat.Time;
                    timePicker.Dock = DockStyle.Fill;
                    timePicker.ShowUpDown = true;

                    deleteButton.Dock = DockStyle.Fill;
                    deleteButton.Text = "Delete";
                    deleteButton.Click += (deleteSender, deleteArgs) => DeleteRow(deleteButton);

                    int rowCount = tableLayoutPanel1.RowCount;
                    tableLayoutPanel1.RowCount = rowCount + 1;
                    tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.AutoSize));

                    tableLayoutPanel1.Controls.Add(checkBox, 0, rowCount);
                    tableLayoutPanel1.Controls.Add(datePicker, 1, rowCount);
                    tableLayoutPanel1.Controls.Add(timePicker, 2, rowCount);
                    tableLayoutPanel1.Controls.Add(deleteButton, 3, rowCount);

                    // Add controls to the settings list
                    settingsList.Add(new Tuple<CheckBox, DateTimePicker, DateTimePicker, Button>(checkBox, datePicker, timePicker, deleteButton));
                }
            }
        }

        private void SaveSettings()
        {
            var settings = new List<ScheduleSettings>();

            foreach (var setting in settingsList)
            {
                settings.Add(new ScheduleSettings
                {
                    Enabled = setting.Item1.Checked,
                    Date = setting.Item2.Value,
                    DateEnabled = setting.Item2.Checked, // Save the checked state of the DatePicker
                    Time = setting.Item3.Value.TimeOfDay
                });
            }

            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText("ServerRestartScheduleSettings.json", json);
            MessageBox.Show("Server Restart Schedule Saved");
        }

        // ScheduleSettings class
        [Serializable]
        public class ScheduleSettings
        {
            public bool Enabled { get; set; }
            public bool DateEnabled { get; set; }
            public DateTime Date { get; set; }
            public TimeSpan Time { get; set; }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            SaveSettings();
        }
    }
}
