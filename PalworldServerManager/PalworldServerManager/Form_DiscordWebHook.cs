using Discord;
using Discord.Webhook;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PalworldServerManager
{
    public partial class Form_DiscordWebHook : Form
    {
        //Webhook Guide: https://birdie0.github.io/discord-webhooks-guide/index.html
        //Avatar: https://i.imgur.com/qhTj0IT.jpeg
        //COLOR: 16761035
        Form_RCON rconForm;

        private string WebhookUrl;
        private string txtUsername;
        private string txtAvatarURL;

        //Embed
        private string txtEmbedTitle;
        private string txtEmbedDescription;
        private string txtEmbedColor;
        private string txtEmbedAuthor_name;
        private string txtEmbedAuthor_url;
        private string txtEmbedAuthor_icon;
        private string txtEmbedImage_url;
        private string txtEmbedThumbnail_url;
        private string txtEmbedFooter_text;
        private string txtEmbedFooter_url;


        public Form_DiscordWebHook(Form1 form)
        {
            InitializeComponent();
            rconForm = form.rconForm;
        }

        private void Form_DiscordWebHook_Load(object sender, EventArgs e)
        {
            LoadData();
        }


        private async Task SendMessageToWebhook(EmbedBuilder embed)
        {
            try
            {
                //var config = new DiscordRestConfig
                //{
                //    RestClientProvider = DefaultRestClientProvider.Create(true, null),
                //    LogLevel = LogSeverity.Info,
                //    DefaultRetryMode = RetryMode.AlwaysRetry,
                //};

                var webhook = new DiscordWebhookClient(WebhookUrl);
                
                await webhook.ModifyWebhookAsync(webhook =>
                {
                    webhook.Name = txtUsername;
                    webhook.Image = new Image(txtAvatarURL);
                });

                // Send the embed using the webhook client
                await webhook.SendMessageAsync(embeds: [embed.Build()]);
                
                Debug.WriteLine("Message sent successfully!");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed to send message to webhook: " + ex);
            }            
        }

        private void button_test_Click(object sender, EventArgs e)
        {
            SendEmbed();
        }


        public async void SendEmbed(string customTitle = null, string customMessage = null)
        {
            string sendMessage;
            string sendTitle;
            string sendFooter;
            if (customMessage != null)
            {
                sendMessage = customMessage;
            }
            else
            {
                sendMessage = txtEmbedDescription;
            }
            
            if (customTitle != null)
            {
                sendTitle = customTitle;
            }
            else
            {
                sendTitle = txtEmbedTitle;
            }

            //rconForm
            if (rconForm.isAutoUpdatePlayers)
            {
                sendFooter = "Online Players: " + rconForm.playerAmount.ToString();
            }
            else
            {
                sendFooter = txtEmbedFooter_text;
            }

            var embed = new EmbedBuilder
            {
                Color = ParseDiscordColor(txtEmbedColor),
                Title = sendTitle,
                Description = sendMessage,
                Author = new EmbedAuthorBuilder()
                {
                    Name = txtEmbedAuthor_name,
                    Url = txtEmbedAuthor_url,
                    IconUrl = txtEmbedAuthor_icon
                },
                ThumbnailUrl = txtEmbedThumbnail_url,
                ImageUrl = txtEmbedImage_url,
                Timestamp = DateTime.Now,
                Footer = new EmbedFooterBuilder
                {
                    Text = sendFooter,
                    IconUrl = txtEmbedFooter_url,
                }
            };

            await SendMessageToWebhook(embed);
        }

        private static Color ParseDiscordColor(string hexColor)
        {
            if (hexColor.StartsWith("#"))
                hexColor = hexColor[1..];

            if (hexColor.Length != 6)
                throw new ArgumentException("Invalid color format. Use RRGGBB or #RRGGBB");

            var r = Convert.ToByte(hexColor[..2], 16);
            var g = Convert.ToByte(hexColor.Substring(2, 2), 16);
            var b = Convert.ToByte(hexColor.Substring(4, 2), 16);

            return new Color(r, g, b);
        }

        private void button_save_Click(object sender, EventArgs e)
        {
            SaveData();
            
        }

        private void PreSend()
        {
            WebhookUrl = textBox_webhookURL.Text;
            txtUsername = textBox_username.Text;
            txtAvatarURL = textBox_avatarURL.Text;
            //Embeds
            txtEmbedTitle = textBox_embedTitle.Text;
            txtEmbedDescription = textBox_embedDescription.Text;
            txtEmbedColor = textBox_embedColor.Text;
            txtEmbedAuthor_name = textBox_embedAuthorName.Text;
            txtEmbedAuthor_url = textBox_embedAuthorURL.Text;
            txtEmbedAuthor_icon = textBox_embedAuthorIconURL.Text;
            txtEmbedImage_url = textBox_embedImageURL.Text;
            txtEmbedThumbnail_url = textBox_embedThumbnailURL.Text;
            txtEmbedFooter_text = textBox_embedFooterText.Text;
            txtEmbedFooter_url = textBox_embedFooterURL.Text;
            if (string.IsNullOrEmpty(textBox_embedColor.Text))
            {
                txtEmbedColor = 16761035.ToString();
            }
            if (string.IsNullOrEmpty(textBox_username.Text))
            {
                txtUsername = "Palworld Server Manager";
            }
            if (string.IsNullOrEmpty(textBox_avatarURL.Text))
            {
                txtAvatarURL = "https://i.imgur.com/qhTj0IT.jpeg";
            }
        }

        private void SaveData()
        {
            // Create an instance of SaveLoadData and set its properties from the TextBoxes
            var data = new SaveLoadData
            {
                json_webhookURL = textBox_webhookURL.Text,
                json_username = textBox_username.Text,
                json_avatarURL = textBox_avatarURL.Text,

                json_embedTitle = textBox_embedTitle.Text,
                json_embedDescription = textBox_embedDescription.Text,
                json_embedColor = textBox_embedColor.Text,
                json_embedAuthorName = textBox_embedAuthorName.Text,
                json_embedAuthorURL = textBox_embedAuthorURL.Text,
                json_embedAuthorIconURL = textBox_embedAuthorIconURL.Text,
                json_embedImageURL = textBox_embedImageURL.Text,
                json_embedThumbnailURL = textBox_embedThumbnailURL.Text,
                json_embedFooterText = textBox_embedFooterText.Text,
                json_embedFooterURL = textBox_embedFooterURL.Text

            };

            // Serialize the data to JSON
            string jsonData = JsonConvert.SerializeObject(data);

            try
            {
                // Save JSON to file
                File.WriteAllText("webhooksavedata.json", jsonData);
                Debug.WriteLine("Data saved successfully!");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving data: {ex.Message}");
            }
            PreSend();
        }

        private void LoadData()
        {
            try
            {
                // Read JSON from file
                string jsonData = File.ReadAllText("webhooksavedata.json");

                // Deserialize JSON to DataModel object
                SaveLoadData data = JsonConvert.DeserializeObject<SaveLoadData>(jsonData);

                // Set TextBox values from deserialized properties
                textBox_webhookURL.Text = data.json_webhookURL;
                textBox_username.Text = data.json_username;
                textBox_avatarURL.Text = data.json_avatarURL;
                textBox_embedTitle.Text = data.json_embedTitle;
                textBox_embedDescription.Text = data.json_embedDescription;
                textBox_embedColor.Text = data.json_embedColor;
                textBox_embedAuthorName.Text = data.json_embedAuthorName;
                textBox_embedAuthorURL.Text = data.json_embedAuthorURL;
                textBox_embedAuthorIconURL.Text = data.json_embedAuthorIconURL;
                textBox_embedImageURL.Text = data.json_embedImageURL;
                textBox_embedThumbnailURL.Text = data.json_embedThumbnailURL;
                textBox_embedFooterText.Text = data.json_embedFooterText;
                textBox_embedFooterURL.Text = data.json_embedFooterURL;
                // Set other TextBox controls as needed
            }
            catch (FileNotFoundException)
            {
                Debug.WriteLine("Data file not found. Please save data first.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading data: {ex.Message}");
            }
            PreSend();
        }
    }

    public class SaveLoadData
    {
        public string json_webhookURL { get; set; }
        public string json_username { get; set; }
        public string json_avatarURL { get; set; }
        public string json_embedTitle { get; set; }
        public string json_embedDescription { get; set; }
        public string json_embedColor { get; set; }
        public string json_embedAuthorName { get; set; }
        public string json_embedAuthorURL { get; set; }
        public string json_embedAuthorIconURL { get; set; }
        public string json_embedImageURL { get; set; }
        public string json_embedThumbnailURL { get; set; }
        public string json_embedFooterText { get; set; }
        public string json_embedFooterURL { get; set; }

    }
}
