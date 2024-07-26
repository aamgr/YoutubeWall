using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Net.Http;
using System.Net;

using System.Web;
using System.Windows;
using System.Threading;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Diagnostics;
using Microsoft.JScript;
using EO.WebEngine;
using Microsoft.SyndicationFeed;
using System.Xml;
using System.ServiceModel.Syndication;
using EO.WebBrowser;

namespace YoutubeSuiveur
{
    public partial class Form1 : Form
    {
        struct Video
        {
            public string url;
            public string title;
        }

        Boolean changeConfig = false;

        EO.WinForm.WebControl[] chromiumWebBrowser;
        EO.WebBrowser.WebView[] webView;
        Button[] buttonUp;
        Button[] buttonDown;
        TextBox[] textBox;
        Panel[] panel;
        Dictionary<string, List<Video>> video_lists = new Dictionary<string, List<Video>>();
  //      static int savePanelPosX = 0, savePanelPosY = 0;
        static Boolean full = false;
        static Size save_size = new Size(0, 0);
        static Point save_pos;
        int[] num_currentVideo;

        public Form1()
        {
            InitializeComponent();
        }

        private async void Form1_Load(object sender, EventArgs e)
        {                     
            this.WindowState = FormWindowState.Maximized;
            Form1_Resize(sender, e);
            if (!File.Exists("Config.txt"))
            {
                System.Windows.MessageBox.Show("Configuration file is missing, create new emply: " + AppDomain.CurrentDomain.BaseDirectory + "Config.txt", "Warning");
                FileStream fil=File.Create("Config.txt");
                fil.Close();
            }
            StreamReader sr = new StreamReader("Config.txt");
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                dataGridView1.Rows.Add(line.Split('\t'));
            }
            sr.Close();
        }

         public static string ReplaceLastOccurrence(string source, string find, string replace)
        {
            int place = source.LastIndexOf(find);

            if (place == -1)
                return source;
            return source.Remove(place, find.Length).Insert(place, replace);
        }

        private void configurationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (pan_config.Visible == true)
            {
                if(dataGridView1.SelectedCells.Count>0)
                    dataGridView1.ClearSelection();
                if (dataGridView1.CurrentCell!=null && dataGridView1.CurrentCell.RowIndex>0 && dataGridView1.CurrentCell.ColumnIndex>=0)
                    dataGridView1.CurrentCell= null;

                //on verifie deja que c'est bien rempli
                for (int row = 0; row < dataGridView1.RowCount-1; row++)
                {
                    if (dataGridView1.Rows[row].Cells[0].Value == null || dataGridView1.Rows[row].Cells[1].Value == null ||
                        dataGridView1.Rows[row].Cells[0].Value.ToString().Length == 0 || dataGridView1.Rows[row].Cells[1].Value.ToString().Length == 0 )
                    {
                        System.Windows.MessageBox.Show("Configuration error row: " + row, "Error");
                        dataGridView1.ClearSelection();
                        dataGridView1.Rows[row].Selected = true;
                        if(dataGridView1.Rows[row].Cells[0].Value == null || dataGridView1.Rows[row].Cells[0].Value.ToString().Length == 0)
                            dataGridView1.CurrentCell = dataGridView1.Rows[row].Cells[0];
                        else
                            dataGridView1.CurrentCell = dataGridView1.Rows[row].Cells[1];
                        return;
                    }
                }
                configurationToolStripMenuItem.Text = "Configuration";
                pan_config.Visible = false;
                pan_video.Visible = true;
                if (changeConfig)
                {

                    //on enregistre la config
                    StreamWriter sw = new StreamWriter("Config.txt");
                    string line;
                    for (int row = 0; row < dataGridView1.RowCount-1; row++)
                    {
                        if (dataGridView1.Rows[row].Cells[2].Value == null)
                            dataGridView1.Rows[row].Cells[2].Value = "";

                        line = dataGridView1.Rows[row].Cells[0].Value.ToString() + '\t' + dataGridView1.Rows[row].Cells[1].Value.ToString() + '\t' + dataGridView1.Rows[row].Cells[2].Value.ToString() + '\t';
                        sw.WriteLine(line);
                    }
                    sw.Close();
                }
                reloadToolStripMenuItem_Click(sender, e);
            }
            else
            {
                configurationToolStripMenuItem.Text = "Validate Configuration";
                changeConfig = false;
                pan_video.Visible = false;
                pan_config.Visible = true;
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }
        protected override void WndProc(ref Message m)
        {
            const int WM_DWMWINDOWMAXIMIZEDCHANG = 0x0321;     // Maximize event - SC_MAXIMIZE from Winuser.h  Restore event - SC_RESTORE from Winuser.h
            const int SC_MAXIMIZE = 0xF030;
            const int SC_RESTORE = 0xF120;
            const int WM_WINDOWPOSCHANGED = 0x0047; //declanche tout le temps
            const int WM_SYSCOMMAND = 0x0112;
            const int WM_SIZE = 0x0005; //bugged declanche aussi si on redimentioe un controle de la feentre

            if (m.Msg == WM_SYSCOMMAND)
            {
                if (m.WParam == new IntPtr(SC_RESTORE))    //declanche avant de redimentioner
                {
                    base.WndProc(ref m);             
                    Form1_Resize(null, null);
                    return;
                }
                if (m.WParam == new IntPtr(SC_MAXIMIZE) )   //declanche avant de redimentioner
                {
                    base.WndProc(ref m);             
                    Form1_Resize(null, null);
                    return;
                }
            }
            base.WndProc(ref m);
        }
        private void Form1_Resize(object sender, EventArgs e)
        {
            pan_config.Height = this.Height - menuStrip1.Height - 45;
            pan_config.Width = this.Width - 24;
            dataGridView1.Height = this.Height - menuStrip1.Height - 45;
            dataGridView1.Width = this.Width - 24;

            pan_video.Height = this.Height - menuStrip1.Height - 45;
            pan_video.Width = this.Width - 24;
            if (!pan_video.Visible && !pan_config.Visible && panel!=null && panel[video_resize_save_num_chromiumWebBrowser]!=null)
            {
                panel[video_resize_save_num_chromiumWebBrowser].Size = pan_video.Size;
                chromiumWebBrowser[video_resize_save_num_chromiumWebBrowser].Size = panel[video_resize_save_num_chromiumWebBrowser].Size;
            }
        }

        private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            changeConfig = true;
     /*       if (e.RowIndex<0)
                return;
           if(dataGridView1.Rows[e.RowIndex].Cells[2].Value == null)
                dataGridView1.Rows[e.RowIndex].Cells[2].Value = "";
            if (e.RowIndex > 0 && (dataGridView1.Rows[e.RowIndex].Cells[0].Value == null && dataGridView1.Rows[e.RowIndex].Cells[1].Value == null))
                dataGridView1.Rows.RemoveAt(e.RowIndex);*/
        }

        Point panel_position(int num_panel)
        {
            int windowsSize = (this.Size.Width / 300);
            if (windowsSize == 0)
                windowsSize = 1;
            Point pos=new Point();
            pos.X = 3 +( num_panel %windowsSize)*300; 
            pos.Y = 6+ (num_panel/windowsSize)*250;
            return pos;
        }
        private void reloadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(sender.ToString()=="Reload Config")
            {
                StreamReader sr = new StreamReader("Config.txt");
                string line;
                dataGridView1.Rows.Clear();
                while ((line = sr.ReadLine()) != null)
                {
                    dataGridView1.Rows.Add(line.Split('\t'));
                }
                sr.Close();
            }
            
            for (int row = 0; row < dataGridView1.RowCount - 1; row++)
            {
                if (dataGridView1.Rows[row].Cells[0].Value == null || dataGridView1.Rows[row].Cells[1].Value == null)
                {
                    System.Windows.MessageBox.Show("Erreur database line: " + row, "Error");
                    continue;
                }
                string site = dataGridView1.Rows[row].Cells[0].Value.ToString();
                string chanel = dataGridView1.Rows[row].Cells[1].Value.ToString();
                string lastVid = "";
                if (dataGridView1.Rows[row].Cells[2].Value!=null)
                    lastVid = dataGridView1.Rows[row].Cells[2].Value.ToString();

                if (site.Equals("youtube", StringComparison.CurrentCultureIgnoreCase))
                    site = "https://www.youtube.com/@" + chanel + "/videos";
                else if (site.Equals("odysee", StringComparison.CurrentCultureIgnoreCase))
                    site = "https://odysee.com/@" + chanel + ":a?view=content";
                else
                {
                    if (cb_error.Checked) 
                        System.Windows.MessageBox.Show("Erreur site " + chanel + " non implémenté", "Error");
                    continue;
                }
            }
        }


        //chargement d'une liste de video
        Video[] loadvideoList(string name, string site)
        {
            //get id from name
            string id = " ";
            string chanel;
            string response;
            var client = new WebClient();
            client.Encoding = System.Text.Encoding.UTF8;
            string rss_chanel;

            if (site.Equals("youtube", StringComparison.CurrentCultureIgnoreCase))
            {
                chanel = "https://www.youtube.com/@" + name;
                try
                {
                    response = client.DownloadString(chanel);
                }
                catch (Exception e)
                {
                    if(cb_error.Checked)
                        System.Windows.MessageBox.Show(e.Message+"\n\nChanel:" + chanel, "Configuration chanel error");
                    return new Video[0];
                }
                if (response.LastIndexOf("channel_id=") <= 0)
                {
                    if (cb_error.Checked) 
                        System.Windows.MessageBox.Show("Cant get channel_id= from youtube, chanel=" + chanel, "Error");
                    return new Video[0];
                }
                id = response.Substring(response.LastIndexOf("channel_id=") + "channel_id=".Length, response.IndexOf("\"", response.LastIndexOf("channel_id=")) - response.LastIndexOf("channel_id=") - "channel_id=".Length);
                rss_chanel = "https://www.youtube.com/feeds/videos.xml?channel_id=" + id;
            }
            else if (site.Equals("odysee", StringComparison.CurrentCultureIgnoreCase))
            {
              //  chanel = "https://odysee.com/@" + name;
              //  response = client.DownloadString(chanel);
                rss_chanel = "https://odysee.com/$/rss/@" + name;
            }
            else
                return new Video[0];

            var reader = XmlReader.Create(rss_chanel);
            SyndicationFeed feed = new SyndicationFeed();
            try
            {
                feed = SyndicationFeed.Load(reader);
            }
            catch (Exception e)
            {
                if (cb_error.Checked) 
                    System.Windows.MessageBox.Show("Rss chanel unknow: "+ rss_chanel, "Error");
            }
            reader.Close();
            IEnumerable<Video> films = new Video[]{};
            Video[] videoList = new Video[0];
            if (site.Equals("youtube", StringComparison.CurrentCultureIgnoreCase))
            {
                films = (from itm in feed.Items
                         select new Video
                         {
                             title = itm.Title.Text,
                             url = "https://www.youtube.com/embed/"+itm.Id.Substring(("yt: video:").Length-1)
                         }).ToList();
            }
            if (site.Equals("odysee", StringComparison.CurrentCultureIgnoreCase))
            {
                films = (from itm in feed.Items
                         select new Video
                         {
                             title = itm.Title.Text,
                             url = "https://odysee.com/$/embed" + HttpUtility.UrlDecode(itm.Id).Substring((HttpUtility.UrlDecode(itm.Id)).LastIndexOf("/"))
                         });
                
            }
            videoList = (films.ToList()).ToArray();
#if false
            chanel = "https://www.toptal.com/developers/feed2json/convert?url=" + rss_chanel;
            //chanel = "https://api.rss2json.com/v1/api.json?rss_url=" + rss_chanel;                //plante si plus de 20 requettes
            Video[] videoList1 = new Video[0]; 
            string data = client.DownloadString(chanel);
                       dynamic films1 = Newtonsoft.Json.JsonConvert.DeserializeObject(data);
                      // dynamic films = Newtonsoft.Json.JsonConvert.DeserializeObject(chanel);

                      videoList1 = new Video[films1.items.Count];
                       string film;
                       for (int num_video = 0; num_video < films1.items.Count; num_video++)
                       {
                           if (site.Equals("youtube", StringComparison.CurrentCultureIgnoreCase))
                           {
                               film = films1.items[num_video].url;
                               film = film.Replace("watch?v=", "embed/");
                           }
                           else
                           {
                               film = HttpUtility.UrlDecode(films1.items[num_video].url.ToString());
                               film = film.Substring(0, film.LastIndexOf(':') + 2);
                               film = ReplaceLastOccurrence(film,"/", "/$/embed/");
                           }
                           videoList1[num_video].url = film;
                           videoList1[num_video].title = HttpUtility.HtmlDecode(films1.items[num_video].title.ToString());
                       }
#endif
            return videoList;
        }
        
        private void pan_video_Click(object sender, EventArgs e)
        {
            //recherche le bon chromiumWebBrowser1 qui est sous la souris
            if (full)
            {
                this.Size = save_size;
                pan_video.Size = save_size;
                this.Location = save_pos;
            }
            else
            {
                save_size = this.Size;
                save_pos = this.Location;
                this.Location = new Point(0, 0);
                this.Size = Screen.FromControl(this).Bounds.Size;
                pan_video.Size = Screen.FromControl(this).Bounds.Size;              
            }
            full = !full;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
        }

        void chargevideo(int num_chromiumWebBrowser)
        {      
            string embed;
            if (video_lists.ElementAt(num_chromiumWebBrowser).Value.Count <= 0)
            {
                textBox[num_chromiumWebBrowser].BackColor = Color.Red;
                textBox[num_chromiumWebBrowser].Text = "CONFIGURATION ERROR: UNKNOWN CHANEL "+ video_lists.ElementAt(num_chromiumWebBrowser).Key;
                textBox[num_chromiumWebBrowser].Refresh();
                return;
            }
            Video video = video_lists.ElementAt(num_chromiumWebBrowser).Value[num_currentVideo[num_chromiumWebBrowser]];
            textBox[num_chromiumWebBrowser].Text = HttpUtility.HtmlDecode(video.title);
            if(num_currentVideo[num_chromiumWebBrowser]==0)
            {
                string lastVid = dataGridView1.Rows[num_chromiumWebBrowser].Cells[2].Value.ToString();
                string CurentVideo=video.url;
                StringComparison comp = StringComparison.OrdinalIgnoreCase;

                CurentVideo = CurentVideo.Substring(CurentVideo.LastIndexOf('/') + 1);
                if (lastVid != CurentVideo)
                    textBox[num_chromiumWebBrowser].BackColor = System.Drawing.Color.GreenYellow;
                else
                    textBox[num_chromiumWebBrowser].BackColor = System.Drawing.Color.White;
            }
            /*            if (video.url.IndexOf("odysee") >= 0)
                        {
                            embed = @"
            <html>
                <head>
                <head>
                    <meta http-equiv='X-UA-Compatible' content='IE=Edge'/>
                    <meta charset='utf8'>
                </head>
                <body  style='overflow: hidden; margin: 0 0 0 0;'>           
                    <iframe id='odysee-iframe' style='width:100%; aspect-ratio:16 / 9;' src='" + video.url + @"' allowfullscreen></iframe>    </body>
            </html>";

                            chromiumWebBrowser[num_chromiumWebBrowser].WebView.Url=video.url;//chromiumWebBrowser[num_chromiumWebBrowser].LoadUrl(video.url);
                        }
                        else
                        {
                            embed = @"
            <html overflow='auto' margin='0px' padding='0px' height='100%' border='none' >
                <head>
                    <meta http-equiv='X-UA-Compatible' content='IE=Edge'/>
                </head>
                <body margin='0px' padding='0px' height='100%' border='none'>
                    <iframe src='" + video.url + @"' margin='0px' padding='0px' border='none' frameborder='0' marginheight='0' marginwidth='0' height='100%' width='100%' scrolling='auto' display='block' overflow-y='auto' overflow-x='hidden' allow='fullscreen' allowfullscreen>
                    </iframe>
                </body>
            </html>
            ";
                             chromiumWebBrowser[num_chromiumWebBrowser].WebView.Url = video.url;//LoadHtml(embed);
                        }*/
            chromiumWebBrowser[num_chromiumWebBrowser].WebView.Url = video.url;
        }
        bool emulate = false;
        private async void startToolStripMenuItem_Click(object sender, EventArgs e)
        {
            emulate = ((ToolStripMenuItem)sender).Name == "testToolStripMenuItem";

            if (buttonUp != null)
            {
                //on supprime tout
                for (int num_video = 0; num_video < video_lists.Count && buttonUp != null; num_video++)
                {
                    buttonUp[num_video].Dispose();
                    buttonDown[num_video].Dispose();
                    textBox[num_video].Dispose();
                    panel[num_video].Dispose();
                    chromiumWebBrowser[num_video].Dispose();
                    webView[num_video].Dispose();
                }
                pan_video.Visible = true;
            }
            
            {
                //on enregistre touts les liens de toutes les video en table
                video_lists.Clear();
                if (emulate)
                {
                    video_lists.Add("test html5", new List<Video>());
                     Video video;
                    video.title = "Test html5";
                    video.url = "https://html5test.com/";
                    video_lists["test html5"].Add(video);

                    video_lists.Add("yahoo", new List<Video>());
                    video.title = "moteur de recherche yahoo";
                    video.url = "www.yahoo.com";
                    video_lists["yahoo"].Add(video);

                    video_lists.Add("youtube", new List<Video>());
                    video.title = "video youtube";
                    video.url = "https://www.youtube.com/embed/Zwj9sxs1hEI?feature=oembed";
                    video_lists["youtube"].Add(video);

                    video_lists.Add("test video", new List<Video>());
                    video.title = "Test local video C:\\test.mp4";
                    video.url = "C:\\test.mp4";
                    video_lists["test video"].Add(video);

                    video_lists.Add("test web video", new List<Video>());
                    video.title = "Video odysee";
                    video.url = "https://odysee.com/$/embed/@coreteks:5/the-apu-revolution-is-here.-amd-at-ces:8?r=00000000000000000000000000000000&autoplay=false";
                    video_lists["test web video"].Add(video);

                    video_lists.Add("test navigator", new List<Video>());
                    video.title = "Test navigator";
                    video.url = "http://ott.dolby.com/codec_test/index.html";
                    video_lists["test navigator"].Add(video);
                
                }
                else
                {
                    for (int row = 0; row < dataGridView1.RowCount - 1; row++)
                    {
                        //on ajoute le nom de la chaine
                        video_lists.Add(dataGridView1.Rows[row].Cells[1].Value.ToString(), new List<Video>());
                        //on ajoute les video associées
                        Video[] video_list = loadvideoList(dataGridView1.Rows[row].Cells[1].Value.ToString(), dataGridView1.Rows[row].Cells[0].Value.ToString());
                        foreach (Video video in video_list)
                            video_lists[dataGridView1.Rows[row].Cells[1].Value.ToString()].Add(video);
                    }
                }
                num_currentVideo = null;
                chromiumWebBrowser = new EO.WinForm.WebControl/*ChromiumWebBrowser*/[video_lists.Count];
                webView = new EO.WebBrowser.WebView[video_lists.Count];
                buttonUp = new System.Windows.Forms.Button[video_lists.Count];
                buttonDown = new System.Windows.Forms.Button[video_lists.Count];
                textBox = new System.Windows.Forms.TextBox[video_lists.Count];
                panel = new System.Windows.Forms.Panel[video_lists.Count];
                num_currentVideo = new int[video_lists.Count];

        //        int posX = 0, posY = 0;
                int nbVideoTGoDraw = video_lists.Count;
                for (int num_video = 0; num_video < nbVideoTGoDraw; num_video++)
                {
                    num_currentVideo[num_video] = 0;
                    buttonUp[num_video] = new System.Windows.Forms.Button();
                    buttonDown[num_video] = new System.Windows.Forms.Button();
                    textBox[num_video] = new System.Windows.Forms.TextBox();
                    panel[num_video] = new System.Windows.Forms.Panel();

                    buttonUp[num_video].Font = new System.Drawing.Font("Symbol", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                    buttonUp[num_video].Margin = new System.Windows.Forms.Padding(0);
                    buttonUp[num_video].Name = "buttonUp" + num_video;
                    buttonUp[num_video].Size = new System.Drawing.Size(20, 44);
                    buttonUp[num_video].Text = ">";
                    buttonUp[num_video].TextAlign = System.Drawing.ContentAlignment.TopCenter;
                    buttonUp[num_video].UseVisualStyleBackColor = true;
                    buttonUp[num_video].Click += new System.EventHandler(buttonUp_Click);

                    buttonDown[num_video].Font = new System.Drawing.Font("Symbol", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                    buttonDown[num_video].Margin = new System.Windows.Forms.Padding(0);
                    buttonDown[num_video].Name = "buttonDown" + num_video;
                    buttonDown[num_video].Size = new System.Drawing.Size(20, 44);
                    buttonDown[num_video].Text = "<";
                    buttonDown[num_video].TextAlign = System.Drawing.ContentAlignment.TopCenter;
                    buttonDown[num_video].UseVisualStyleBackColor = true;
                    buttonDown[num_video].Click += new System.EventHandler(buttonDown_Click);

                    textBox[num_video].Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                    textBox[num_video].BorderStyle = System.Windows.Forms.BorderStyle.None;
                    textBox[num_video].Multiline = true;
                    textBox[num_video].Size = new System.Drawing.Size(252, 49);
                    textBox[num_video].Name="textBox" + num_video;
                    textBox[num_video].ReadOnly = true;

                    chromiumWebBrowser[num_video] = new EO.WinForm.WebControl();// ChromiumWebBrowser();
                    //chromiumWebBrowser[num_video].ActivateBrowserOnCreation = false;
                    webView[num_video] = new EO.WebBrowser.WebView();

                    chromiumWebBrowser[num_video].WebView = webView[num_video];
                    chromiumWebBrowser[num_video].WebView.NewWindow += newWindows_event;
                    chromiumWebBrowser[num_video].MouseEnter += WebViewMouseEnter_event;
                    //chromiumWebBrowser[num_video].MouseLeave += WebView_MouseLeave;
                    chromiumWebBrowser[num_video].WebView.Engine.Options.AllowProprietaryMediaFormats();

                    chromiumWebBrowser[num_video].Name = "chromiumWebBrowser" + num_video;
                    chromiumWebBrowser[num_video].Size = new System.Drawing.Size(300, 250);
                    chromiumWebBrowser[num_video].Margin = new Padding(0, 0, 0, 0);
                    chromiumWebBrowser[num_video].Padding = new Padding(0, 0, 0, 0);
                    chromiumWebBrowser[num_video].MouseDown += webControl_MouseDown;
                    //chromiumWebBrowser[num_video].PreviewKeyDown += webControl_previewKeyDown;

                    panel[num_video].BackColor = System.Drawing.Color.White;
                    panel[num_video].BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
                    panel[num_video].Name = "panel" + num_video;
                    panel[num_video].Size = new System.Drawing.Size(300, 250);
                    panel[num_video].Controls.Add(textBox[num_video]);
                    panel[num_video].Controls.Add(buttonUp[num_video]);
                    panel[num_video].Controls.Add(buttonDown[num_video]);
                    panel[num_video].Controls.Add(chromiumWebBrowser[num_video]);

                    pan_video.Controls.Add(panel[num_video]);

                    panel[num_video].Location = panel_position(num_video);// new System.Drawing.Point(3 + posX * 300, 6 + posY * 250);
                    buttonUp[num_video].Location = new System.Drawing.Point(3 + 300 - 25, 6);
                    buttonDown[num_video].Location = new System.Drawing.Point(2, 6);
                    textBox[num_video].Location = new System.Drawing.Point(23, 1);
                    chromiumWebBrowser[num_video].Location = new System.Drawing.Point(0, 50);

              /*      posX++;
                    if ((posX + 1) * 300 > this.Size.Width)
                    {
                        posY++;
                        posX = 0;
                    }*/
                }

                for (int num_chromiumWebBrowser = 0; num_chromiumWebBrowser < nbVideoTGoDraw; num_chromiumWebBrowser++)
                {
                    if (emulate)
                    {
                        Video video = video_lists.ElementAt(num_chromiumWebBrowser).Value[num_currentVideo[num_chromiumWebBrowser]];
                        chromiumWebBrowser[num_chromiumWebBrowser].WebView.Url = video.url;//LoadUrl(video.url);
                        textBox[num_chromiumWebBrowser].Text = HttpUtility.HtmlDecode(video.title);
                    }
                    else
                        chargevideo(num_chromiumWebBrowser);
                }
            }
        }

        private void buttonUp_Click(object sender, EventArgs e)
        {
            int num_chromiumWebBrowser;
            if (!int.TryParse(((Button)sender).Name.Substring("buttonUp".Length), out num_chromiumWebBrowser))
            {
                return;
            }
            if (num_currentVideo[num_chromiumWebBrowser]+1 < video_lists.ElementAt(num_chromiumWebBrowser).Value.Count)
            {
                num_currentVideo[num_chromiumWebBrowser]++;
                chargevideo(num_chromiumWebBrowser);
            }
        }
        
        private void buttonDown_Click(object sender, EventArgs e)
        {
            int num_chromiumWebBrowser;
            if (!int.TryParse(((Button)sender).Name.Substring("buttonDown".Length), out num_chromiumWebBrowser))
            {
                return;
            }
            if (num_currentVideo[num_chromiumWebBrowser] > 0)
            {
                num_currentVideo[num_chromiumWebBrowser]--;
                chargevideo(num_chromiumWebBrowser);
            }
        }

        private delegate void SetControlPropertyThreadSafeDelegate(Control control, string propertyName, object propertyValue);
        public static void SetControlPropertyThreadSafe(Control control, string propertyName, object propertyValue)
        {
            if (control.InvokeRequired)
            {
                control.Invoke(new SetControlPropertyThreadSafeDelegate(SetControlPropertyThreadSafe),new object[] { control, propertyName, propertyValue });
            }
            else
            {
                control.GetType().InvokeMember(propertyName, BindingFlags.SetProperty, null, control, new object[] { propertyValue });
            }
        }

        int video_resize_save_num_chromiumWebBrowser=0;
        public void video_resize(int num_chromiumWebBrowser,bool augmente)
        {
            if (pan_video.Visible && augmente)  //
            {
                video_resize_save_num_chromiumWebBrowser=num_chromiumWebBrowser;
                SetControlPropertyThreadSafe(pan_video, "Visible", false);
      //          savePanelPosX = panel[num_chromiumWebBrowser].Location.X;
       //         savePanelPosY = panel[num_chromiumWebBrowser].Location.Y;
                pan_video.BeginInvoke(new Action(() => pan_video.Controls.Remove(panel[num_chromiumWebBrowser])));
                SetControlPropertyThreadSafe(panel[num_chromiumWebBrowser], "Parent", this);// 
                SetControlPropertyThreadSafe(panel[num_chromiumWebBrowser], "Size", new Size(this.Size.Width-3-19, this.Size.Height-25-44));
                SetControlPropertyThreadSafe(panel[num_chromiumWebBrowser], "Location", new System.Drawing.Point(3, 25));
                SetControlPropertyThreadSafe(chromiumWebBrowser[num_chromiumWebBrowser], "Size", new Size(panel[num_chromiumWebBrowser].Width, panel[num_chromiumWebBrowser].Height));
                SetControlPropertyThreadSafe(chromiumWebBrowser[num_chromiumWebBrowser], "Location", new System.Drawing.Point(0,0));
            }
            if (!pan_video.Visible && !augmente && video_resize_save_num_chromiumWebBrowser >=0)    //on diminue
            {
                num_chromiumWebBrowser = video_resize_save_num_chromiumWebBrowser;
                pan_video.BeginInvoke(new Action(() =>
                pan_video.Controls.Add(panel[num_chromiumWebBrowser])
                )); 
                SetControlPropertyThreadSafe(pan_video, "Visible", true);
                SetControlPropertyThreadSafe(chromiumWebBrowser[video_resize_save_num_chromiumWebBrowser], "Size", new Size(300,200));
                SetControlPropertyThreadSafe(chromiumWebBrowser[num_chromiumWebBrowser], "Location", new System.Drawing.Point(0,50));
                SetControlPropertyThreadSafe(panel[video_resize_save_num_chromiumWebBrowser], "Size", new System.Drawing.Size(300, 250));
                int num_vid = video_resize_save_num_chromiumWebBrowser;
                panel[num_vid].BeginInvoke(new Action(() => panel[num_vid].Location = panel_position(num_vid)));
                panel[num_vid].BeginInvoke(new Action(() => panel[num_vid].Refresh()));
                // SetControlPropertyThreadSafe(panel[video_resize_save_num_chromiumWebBrowser], "Location", panel_position(video_resize_save_num_chromiumWebBrowser));

                if (!emulate)
                {
                    if (video_lists.ElementAt(num_chromiumWebBrowser).Value.Count <= 0)
                    {
                        textBox[num_chromiumWebBrowser].BackColor = Color.Red;
                        textBox[num_chromiumWebBrowser].Text = "CONFIGURATION ERROR: UNKNOWN CHANEL " + video_lists.ElementAt(num_chromiumWebBrowser).Key;
                        textBox[num_chromiumWebBrowser].Refresh();
                        video_resize_save_num_chromiumWebBrowser = -1;
                        return;
                    }
                    Video video = video_lists.ElementAt(video_resize_save_num_chromiumWebBrowser).Value[num_currentVideo[video_resize_save_num_chromiumWebBrowser]];
                    video.url = video.url.Substring(video.url.LastIndexOf('/') + 1);
                    string lastVidRead = dataGridView1.Rows[video_resize_save_num_chromiumWebBrowser].Cells[2].Value.ToString();

                    int last_video_order = 0, current_read_video_order = 0;

                    string Vid_url;
                    //we try to fing the order of last video viewed
                    do
                    {
                        Vid_url = video_lists.ElementAt(video_resize_save_num_chromiumWebBrowser).Value[last_video_order].url;
                        Vid_url = Vid_url.Substring(Vid_url.LastIndexOf('/') + 1);
                    } while (lastVidRead != Vid_url && (++last_video_order < video_lists.ElementAt(video_resize_save_num_chromiumWebBrowser).Value.Count));

                    //we try to find the current video that we are view
                    do
                    {
                        Vid_url = video_lists.ElementAt(video_resize_save_num_chromiumWebBrowser).Value[current_read_video_order].url;
                        Vid_url = Vid_url.Substring(Vid_url.LastIndexOf('/') + 1);
                    } while (video.url != Vid_url && (++current_read_video_order < video_lists.ElementAt(video_resize_save_num_chromiumWebBrowser).Value.Count));

                    //on a lu une nouvelle video: on enregistre en table
                    if (current_read_video_order < last_video_order)
                    {
                        dataGridView1.Rows[video_resize_save_num_chromiumWebBrowser].Cells[2].Value = video.url;
                        last_video_order = current_read_video_order;

                        //on sauve dans le fichier
                        StreamWriter sw = new StreamWriter("Config.txt");
                        string line;
                        for (int row = 0; row < dataGridView1.RowCount; row++)
                        {
                            if (dataGridView1.Rows[row].Cells[0].Value == null || dataGridView1.Rows[row].Cells[1].Value == null || dataGridView1.Rows[row].Cells[2].Value == null)
                                continue;
                            line = dataGridView1.Rows[row].Cells[0].Value.ToString() + '\t' + dataGridView1.Rows[row].Cells[1].Value.ToString() + '\t' + dataGridView1.Rows[row].Cells[2].Value.ToString() + '\t';
                            sw.WriteLine(line);
                        }
                        sw.Close();
                    }

                    if (last_video_order > 0)
                        SetControlPropertyThreadSafe(textBox[video_resize_save_num_chromiumWebBrowser], "BackColor", System.Drawing.Color.GreenYellow);//textBox[num_chromiumWebBrowser].BackColor = System.Drawing.Color.GreenYellow;
                    else
                        SetControlPropertyThreadSafe(textBox[video_resize_save_num_chromiumWebBrowser], "BackColor", System.Drawing.Color.White);//textBox[num_chromiumWebBrowser].BackColor = System.Drawing.Color.White;
                }
                video_resize_save_num_chromiumWebBrowser = -1;
            }
        }

        public IEnumerable<Control> GetAll(Control control, Type type)
        {
            var controls = control.Controls.Cast<Control>();

            return controls.SelectMany(ctrl => GetAll(ctrl, type))
                                      .Concat(controls)
                                      .Where(c => c.GetType() == type);
        }

        private void testToolStripMenuItem_Click(object sender, EventArgs e)
        {
            startToolStripMenuItem_Click(sender, e);
        }

        private void webControl_MouseDown(object sender, MouseEventArgs e)
        {
            int num_chromiumWebBrowser;
            if (int.TryParse(((EO.WinForm.WebControl)sender).Name.Substring("chromiumWebBrowser".Length), out num_chromiumWebBrowser))
            {
                video_resize(num_chromiumWebBrowser, true);
                ((EO.WinForm.WebControl)sender).MouseDown -= webControl_MouseDown;
                ((EO.WinForm.WebControl)sender).PreviewKeyDown += webControl_previewKeyDown;
                buttonUp[num_chromiumWebBrowser].PreviewKeyDown += webControl_previewKeyDown;
                buttonDown[num_chromiumWebBrowser].PreviewKeyDown += webControl_previewKeyDown;
                textBox[num_chromiumWebBrowser].PreviewKeyDown += webControl_previewKeyDown;
            }
        }
        private void newWindows_event(object sender, NewWindowEventArgs e)
        {
            ((EO.WebBrowser.WebView)sender).Url = e.TargetUrl;
        }
        private Dictionary<int, Dictionary<int, string>> ClipBoardValues(string clipboardValue)
        {
            Dictionary<int, Dictionary<int, string>>
            copyValues = new Dictionary<int, Dictionary<int, string>>();

            String[] lines = clipboardValue.Split('\n');

            for (int i = 0; i <= lines.Length - 1; i++)
            {
                copyValues[i] = new Dictionary<int, string>();
                String[] lineContent = lines[i].Split('\t');

                //if an empty cell value copied, then set the dictionary with an empty string
                //else Set value to dictionary
                if (lineContent.Length == 0)
                    copyValues[i][0] = string.Empty;
                else
                {
                    for (int j = 0; j <= lineContent.Length - 1; j++)
                        copyValues[i][j] = lineContent[j];
                }
            }
            return copyValues;
        }
        private void dataGridView1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.C && Control.ModifierKeys == Keys.Control)
            {
                System.Windows.Clipboard.SetDataObject(dataGridView1.SelectedCells);
            }
            if (e.KeyCode == Keys.V && Control.ModifierKeys == Keys.Control)
            {
                Dictionary<int, Dictionary<int, string>> cbValue = ClipBoardValues(System.Windows.Clipboard.GetText());
                int FirstCopy_row=0;
                int FirstCopy_col = 0;
                if (dataGridView1.CurrentCell != null)
                {
                    FirstCopy_row = dataGridView1.CurrentCell.RowIndex;
                    FirstCopy_col = dataGridView1.CurrentCell.ColumnIndex;
                }
                int Copy_row= FirstCopy_row ;
                int Copy_col=FirstCopy_col;

                for (int row = 0; row < cbValue.Count; row++, Copy_row++)
                {
                    Copy_col = FirstCopy_col;
                    if (dataGridView1.RowCount-1 <= Copy_row)
                    {
                        string data="";
                        if (cbValue[row].Count == 1 && cbValue[row][0].Length == 0) //emply row
                            continue;
                        for (int col = 0; col < cbValue[row].Count; col++)
                            data += cbValue[row][col]+"\t";
                        dataGridView1.Rows.Add(data.Split('\t'));
                        dataGridView1.Refresh();
                    }
                    else
                    {
                        for (int col = 0; col < cbValue[row].Count; col++, Copy_col++)
                        {
                            dataGridView1.Rows[Copy_row].Cells[Copy_col].Value = cbValue[row][col];
                            dataGridView1.Refresh();
                        }
                    }
                }
            }
            if (e.KeyCode == Keys.Delete)
            {
                int cpt = 0;
                while(dataGridView1.SelectedCells.Count>0 && cpt< dataGridView1.SelectedCells.Count)
                {
                    dataGridView1.SelectedCells[cpt].Value = "";
                    dataGridView1.Refresh();

                    if ((dataGridView1.Rows[dataGridView1.SelectedCells[cpt].RowIndex].Cells[0].Value==null ||
                        dataGridView1.Rows[dataGridView1.SelectedCells[cpt].RowIndex].Cells[0].Value.ToString().Length == 0) &&
                        (dataGridView1.Rows[dataGridView1.SelectedCells[cpt].RowIndex].Cells[1].Value==null ||
                       dataGridView1.Rows[dataGridView1.SelectedCells[cpt].RowIndex].Cells[1].Value.ToString().Length == 0) &&
                       (dataGridView1.Rows[dataGridView1.SelectedCells[cpt].RowIndex].Cells[2].Value==null ||
                       dataGridView1.Rows[dataGridView1.SelectedCells[cpt].RowIndex].Cells[2].Value.ToString().Length == 0))
                    {
                        bool bugdetect = false;
                        if (dataGridView1.SelectedCells.Count == 3)
                            bugdetect = true;
                        if (!(dataGridView1.Rows[dataGridView1.SelectedCells[cpt].RowIndex].Cells[0].Value == null &&
                            dataGridView1.Rows[dataGridView1.SelectedCells[cpt].RowIndex].Cells[1].Value == null &&
                            dataGridView1.Rows[dataGridView1.SelectedCells[cpt].RowIndex].Cells[2].Value == null))
                        {
                            if (dataGridView1.SelectedCells[cpt].RowIndex < dataGridView1.Rows.Count - 1)
                                dataGridView1.Rows.RemoveAt(dataGridView1.SelectedCells[cpt].RowIndex);
                            else
                            {
                                cpt++;
                                continue;
                            }
                        }
                        //dataGridView1.Refresh();
                        if (bugdetect)
                        {
                            dataGridView1.CurrentCell = null; //dataGridView1.Refresh();
                        }
                        cpt = 0;
                    }
                    else
                        cpt++;
                }
            }
        }

     /*   public void WebViewMouseEnter_event(object sender, EventArgs e)
        {

            return;
        }*/
        private void webControl_previewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyData == Keys.Escape)
            {
                int num_chromiumWebBrowser;
                string name="";
                if (sender is TextBox)
                    name = ((TextBox)sender).Name;
                if (sender is Button)
                    name = ((Button)sender).Name;
                if (sender is EO.WinForm.WebControl)
                    name = ((EO.WinForm.WebControl)sender).Name;//(Control)sender.Name.Substring("chromiumWebBrowser".Length), out num_chromiumWebBrowser)
                if (int.TryParse(string.Concat(name.ToArray().Reverse().TakeWhile(char.IsNumber).Reverse()), out num_chromiumWebBrowser))
                {
                    video_resize(-1, false);
                    chromiumWebBrowser[num_chromiumWebBrowser].MouseDown += webControl_MouseDown;
                    chromiumWebBrowser[num_chromiumWebBrowser].PreviewKeyDown -= webControl_previewKeyDown;
                    buttonUp[num_chromiumWebBrowser].PreviewKeyDown -= webControl_previewKeyDown;
                    buttonDown[num_chromiumWebBrowser].PreviewKeyDown -= webControl_previewKeyDown;
                    textBox[num_chromiumWebBrowser].PreviewKeyDown -= webControl_previewKeyDown;
                }
             }
        }

        private void WebViewMouseEnter_event(object sender, EventArgs e)
        {
            int num_chromiumWebBrowser;
            if (int.TryParse(((EO.WinForm.WebControl)sender).Name.Substring("chromiumWebBrowser".Length), out num_chromiumWebBrowser))
                toolTip1.Show(dataGridView1.Rows[num_chromiumWebBrowser].Cells[1].Value.ToString(), chromiumWebBrowser[num_chromiumWebBrowser],2000);

        }
    }
}
