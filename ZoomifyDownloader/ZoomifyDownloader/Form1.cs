using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Xml;

namespace ZoomifyDownloader
{
    // TODO:
    // Pull zoomify links out of webpage that is display to the public.
    // Allow user to set options of output image format.
    // Save frequent used settings.
    // Allow batch downloading and renaming.
    // Allow renaming masks.  
    public partial class Form1 : Form
    {
        private static readonly ImageFormat defaultFormat = ImageFormat.Png;
        private static readonly string batchCommandNewMask = "*mask*";
        private static readonly string batchCommandNewFolder = "*dir*";
        private static readonly string batchCommandNewFormat = "*format*";
        private static readonly string batchCommandNewConflict = "*conflict*";

        protected ImageFormat outFormat;
        public string SettingsFile = "settings.xml";
        protected string ExecutionPath;
        protected SaveFileConflictMode saveMode = SaveFileConflictMode.Ask;
        public string lastSavedFile = "";
        private DateTime opStartTime;
        private DateTime opEndTime;
        private int itemsDownloaded = 0;

        public Form1()
        {
            outFormat = defaultFormat;
            InitializeComponent();
            textBox2.Text = System.Environment.GetFolderPath(Environment.SpecialFolder.MyPictures, Environment.SpecialFolderOption.Create);
            comboBox1.SelectedIndex = 0;
            radioButton1.Checked = true;
            ExecutionPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);
            ExecutionPath = ExecutionPath.Substring( ExecutionPath.IndexOf("file:\\") + 6);
            Console.WriteLine("Checking for settings file in: " + ExecutionPath + "\\" + SettingsFile);
            if (File.Exists(ExecutionPath + "\\" + SettingsFile))
            {
                Console.WriteLine("Settings file found!");
                LoadSettings(ExecutionPath + "\\" + SettingsFile);
            }
            
        }

        //Serialize form settings.
        public void SaveSettings(string filename)
        {
            XmlWriterSettings oSettings = new XmlWriterSettings();

            oSettings.Indent = true;
            oSettings.OmitXmlDeclaration = false;
            oSettings.Encoding = Encoding.ASCII;

            using (XmlWriter writer = XmlWriter.Create(filename, oSettings))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("DOWNLOADER_PROPERTIES");

                writer.WriteStartAttribute("TARGET_URL");
                writer.WriteValue(textBox1.Text);
                writer.WriteEndAttribute();

                writer.WriteStartAttribute("BATCH_URL");
                writer.WriteValue(textBox4.Text);
                writer.WriteEndAttribute();

                writer.WriteStartAttribute("DOWNLOAD_FOLDER");
                writer.WriteValue(textBox2.Text);
                writer.WriteEndAttribute();

                writer.WriteStartAttribute("NAME_MASK");
                writer.WriteValue(textBox3.Text);
                writer.WriteEndAttribute();

                writer.WriteStartAttribute("OUTPUT_FORMAT");
                writer.WriteValue(comboBox1.SelectedIndex.ToString());
                writer.WriteEndAttribute();

                writer.WriteStartAttribute("OPEN_FOLDER_ON_FINISH");
                writer.WriteValue(checkBox1.Checked.ToString());
                writer.WriteEndAttribute();

                writer.WriteStartAttribute("DOWNLOAD_MODE");
                writer.WriteValue(   radioButton1.Checked  ? "Target" : "Batch" );
                writer.WriteEndAttribute();

                writer.WriteEndElement();
                writer.WriteEndDocument();

                writer.Flush();
            }
            Console.WriteLine("Saved settings file to: "+filename);
        }
        //Deserialize form settings.
        protected void LoadSettings(string filename)
        {
            Console.WriteLine("Loading settings file from: " + filename);
            XmlDocument settingFile = new XmlDocument();
            settingFile.Load(filename);
            XmlNode properties = settingFile.GetElementsByTagName("DOWNLOADER_PROPERTIES")[0];

            try
            {
                textBox1.Text = properties.Attributes.GetNamedItem("TARGET_URL").Value;
                textBox4.Text = properties.Attributes.GetNamedItem("BATCH_URL").Value;
                textBox2.Text = properties.Attributes.GetNamedItem("DOWNLOAD_FOLDER").Value;
                textBox3.Text = properties.Attributes.GetNamedItem("NAME_MASK").Value;
                comboBox1.SelectedIndex = Convert.ToInt32(properties.Attributes.GetNamedItem("OUTPUT_FORMAT").Value);
                checkBox1.Checked = Convert.ToBoolean(properties.Attributes.GetNamedItem("OPEN_FOLDER_ON_FINISH").Value);
                switch (properties.Attributes.GetNamedItem("DOWNLOAD_MODE").Value)
                {
                    case "Target":
                    default:
                        radioButton1.Checked = true;
                        break;
                    case "Batch":
                        radioButton2.Checked = true;
                        break;                   
                }

            }
            catch
            {
                return;
            }
        }
        //Shows a message dialog asking if the user would like to replace files that already exist.
        protected SaveFileConflictMode QueryConflictDialog(string filename, SaveFileConflictMode yesMode, SaveFileConflictMode noMode)
        {
            //Do Message Dialog
            string messageBoxText = "A file with the name: " + filename + " already exists. Do you want to replace it?";
            string caption = "Warning - Potential Unwanted File Replacement";
            MessageBoxButtons button = MessageBoxButtons.YesNoCancel;
            MessageBoxIcon icon = MessageBoxIcon.Warning;
            // Display message box
            DialogResult result = MessageBox.Show(messageBoxText, caption, button, icon);
            // Process message box results 
            switch (result)
            {
                case DialogResult.Yes:
                    // User pressed Yes button 
                    // ...
                    return yesMode;
                case DialogResult.No:
                default:
                    // User pressed No button
                    // ...
                    return noMode;
                case DialogResult.Cancel:                
                    // User pressed Cancel button 
                    // ...
                    return SaveFileConflictMode.DoNothing;
            }
        }

        private void DownloadDirectURL(string url, string path, string filename)
        {
            DownloadDirectURL(url, path, filename, false);
        }
        private void DownloadDirectURL(string url, string path, string filename, bool batchMode)
        {
            // Check for unwanted file replacement first.
            // Check if the filename already has the right file extension.
            string fileExtension = "."+outFormat.ToString().ToLowerInvariant();
            string saveFilePath = path + "\\" + filename;
            if (!Dezoomify.StringIsLast(filename, fileExtension))
            {
                saveFilePath += fileExtension;
            }
            if (File.Exists(saveFilePath) && saveMode == SaveFileConflictMode.Ask)
            {
                if (batchMode)
                {
                    saveMode = QueryConflictDialog(saveFilePath, SaveFileConflictMode.ReplaceAlways, SaveFileConflictMode.IncrementAlways);
                }
                else
                {
                    saveMode = QueryConflictDialog(saveFilePath, SaveFileConflictMode.Replace, SaveFileConflictMode.Increment);
                }                
            }
            //Open Zoomify Path & Download
            Bitmap download = Dezoomify.Download(url);

            //Save Image on Success
            if (download != null)
            {
                lastSavedFile = Dezoomify.Save(download, outFormat, path, filename, saveMode);
            }
            download.Dispose();
            //Check if the user has specified to always use one conflict resolving method for the rest of the program session.
            if (saveMode != SaveFileConflictMode.IncrementAlways && saveMode != SaveFileConflictMode.ReplaceAlways)
            {
                saveMode = SaveFileConflictMode.Ask;
            }
        }

        // Determine if the URL given is a valid Zoomify Direct URL to the image folder.
        public static bool isDirectURL(string url)
        {
            // Clue 1 - URL does not point to a file ( has no file extension ).
            // Clue 2 - URL is relative ( has no domain. )
            // Test 1 - Try getting an Image Properties file from the URL.
            if (url == null || url.Length == 0 || !url.Contains("/") || !url.Contains(".") || url.Trim().IndexOf("*") == 0) 
            {
                Console.WriteLine("Yeah this isn't a url: "+url);
                return false;
            }
            if ( Dezoomify.TryGetImageProperties(url) == true ) //url.Substring(url.LastIndexOf("\\")).Contains(".") == false && url.Substring(url.IndexOf(".")).Contains("\\") == true &&  )
            {
                return true;
            }
            return false;
        }

        // Converts string to an Imaging.ImageFormat enum.
        public static ImageFormat StringToImageFormat(string input)
        {
            if (input.Contains("png"))
            {
                return ImageFormat.Png;
            }
            else if (input.Contains("jpg") || input.Contains("jpeg")) 
            {
                return ImageFormat.Jpeg;
            }
            else if (input.Contains("bmp"))
            {
                return ImageFormat.Bmp;
            }
            else if (input.Contains("gif"))
            {
                return ImageFormat.Gif;
            }
            else if (input.Contains("tiff"))
            {
                return ImageFormat.Tiff;
            }
            return defaultFormat;
        }

        // Converts string to a SaveFileConflictMode enum.
        public static SaveFileConflictMode StringToConflictMode(string input) 
        {
            input = input.ToLowerInvariant();
            if (input.Contains("ReplaceAlways".ToLowerInvariant())) 
            {
                return SaveFileConflictMode.ReplaceAlways;
            }
            else if (input.Contains("Replace".ToLowerInvariant()))
            {
                return SaveFileConflictMode.Replace;
            }
            else if (input.Contains("IncrementAlways".ToLowerInvariant()))
            {
                return SaveFileConflictMode.IncrementAlways;
            }
            else if (input.Contains("Increment".ToLowerInvariant()))
            {
                return SaveFileConflictMode.Increment;
            }
            else if (input.Contains("DoNothing".ToLowerInvariant()))
            {
                return SaveFileConflictMode.DoNothing;
            }
            return SaveFileConflictMode.Ask;
        }

        // Converts a string with masks to a final string.
        public string FinalizeMask(string masks, string path)
        {
            // Name of file - field makes no sense in zoomify download context.
            masks = masks.Replace("*name*", "");
            // Output file format extension
            masks = masks.Replace("*ext*", outFormat.ToString().ToLowerInvariant());
            // ? URL path
            masks = masks.Replace("*url*", path);
            //
            masks = masks.Replace("*curl*", path);
            //
            masks = masks.Replace("*flatcurl*", path);
            //
            masks = masks.Replace("*subdirs*", "");
            //
            masks = masks.Replace("*flatsubdirs*", "");
            //
            masks = masks.Replace("*text*", "");
            //
            masks = masks.Replace("*flattext*", "");
            //
            masks = masks.Replace("*title*", "");
            //
            masks = masks.Replace("*flattitle*", "");
            //
            masks = masks.Replace("*qstring*", "");
            //
            masks = masks.Replace("*refer*", "");
            //
            masks = masks.Replace("*num*", "");
            //
            masks = masks.Replace("*inum*", "");
            // Hour - Current system time hour.
            masks = masks.Replace("*hh*", System.DateTime.Now.Hour.ToString());
            // Minute - Current system time minute.
            masks = masks.Replace("*mm*", System.DateTime.Now.Minute.ToString());
            // Second - Current system time second.
            masks = masks.Replace("*ss*", System.DateTime.Now.Second.ToString());
            // Day - Current system time day.
            masks = masks.Replace("*d*", System.DateTime.Now.Day.ToString());
            // Month - Current system time month.
            masks = masks.Replace("*m*", System.DateTime.Now.Month.ToString());
            // Year - Current system time year.
            masks = masks.Replace("*y*", System.DateTime.Now.Year.ToString());
            
            // Also - Remove Illegal Characters in the filenams.
            string[] illegalChars = { "<", ">", ":","\"","\\","/","|","?","*" };
            foreach (string i in illegalChars)
            {
                masks = masks.Replace(i, " ");
            }

            //Remove any unneccessary whitespace.
            masks = masks.Trim();

            return masks;
        }

        private void ResetStats()
        {
            opStartTime = new DateTime();
            opEndTime = new DateTime();
            itemsDownloaded = 0;
        }

        private int ResolveIndirectURL(string baseURL, string path, string filename)
        {
            // Since we're here this is probably the mode you want.
            saveMode = SaveFileConflictMode.IncrementAlways;
            int downloads = 0;
            //URL provided is not a direct link to a zoomify folder.
            //Scan page for potential zoomify links.
            //Build relative links into fully formed URLs.
            //Batch download all found zoomify links.
            Console.WriteLine("Start scanning for zoomify links on this page : " + baseURL);
            // Check that the url is an absolute path.
            if (Dezoomify.isAbsoluteURL(baseURL))
            {
                Uri datapath = new Uri(baseURL);
                // Now find all zoomify links.
                string htmlCode = Dezoomify.DownloadFile(textBox1.Text);
                List<string> urls = new List<string>();

                urls.AddRange(Dezoomify.ExtractURLs(baseURL, htmlCode));

                foreach (string i in urls)
                {
                    // Download & Save
                    DownloadDirectURL(i, textBox2.Text, FinalizeMask(textBox3.Text, textBox1.Text), true);
                    downloads++;
                }
            }
            else
            {
                Console.WriteLine("Warning, URL was either relative, or malformed.");
            }
            return downloads;
        }

        // Starts download.
        private void button1_click(object sender, EventArgs e)
        {
            ResetStats();
            //Start Timer
            opStartTime = System.DateTime.Now;
            SaveSettings(SettingsFile);
            // Downloads using a single URL.   
            if (radioButton1.Checked)
            {
                if (isDirectURL(textBox1.Text))
                {
                    //URL provided is a direct link to a zoomify image folder.
                    DownloadDirectURL(textBox1.Text, textBox2.Text, FinalizeMask(textBox3.Text, textBox1.Text));
                    itemsDownloaded++;
                }
                else
                {
                    itemsDownloaded += ResolveIndirectURL(textBox1.Text, textBox2.Text, FinalizeMask(textBox3.Text, textBox1.Text));
                }
            }
            else if (radioButton2.Checked)
            {
                // Batch download using a list of URL that point directly to the zoomify folder.
                // Open File
                StreamReader streamReader = new StreamReader(textBox4.Text);
                string text = streamReader.ReadToEnd();
                streamReader.Close();
                List<string> commands = new List<string>();
                // Extract Paths
                string[] filter = { "\r\n", "\r", "\n" };
                commands.AddRange(text.Split(filter, StringSplitOptions.RemoveEmptyEntries));
                // Validate that each item is a url.
                foreach (string i in commands)
                {
                    // Is it a URL?
                    // Is the line a Direct URL?
                    if ( Dezoomify.isAbsoluteURL(i) )
                    {
                        if (isDirectURL(i)) 
                        {
                            // Download & Save
                            DownloadDirectURL(i, textBox2.Text, FinalizeMask(textBox3.Text, textBox1.Text), true);
                            itemsDownloaded++;
                        } 
                        else
                        {
                            itemsDownloaded += ResolveIndirectURL(i, textBox2.Text, FinalizeMask(textBox3.Text, textBox1.Text));
                        }                        
                    }                   
                    else
                    {
                        // Or is it a new parameter command?
                        //Check if it is a batch command.
                        string labrat = i.ToLowerInvariant().Trim();
                        if (labrat.IndexOf( batchCommandNewMask ) == 0)
                        {
                            //Then any text that comes after the command is now the new renaming mask.
                            textBox3.Text = i.Substring(batchCommandNewMask.Length).Trim();
                        }
                        else if (labrat.IndexOf(batchCommandNewFolder) == 0)
                        {
                            textBox2.Text = i.Substring(batchCommandNewFolder.Length).Trim();
                        }
                        else if (labrat.IndexOf(batchCommandNewFormat) == 0)
                        {
                            outFormat = StringToImageFormat( labrat.Substring(batchCommandNewFormat.Length) );
                        }
                        else if (labrat.IndexOf(batchCommandNewConflict) == 0)
                        {
                            saveMode = StringToConflictMode(labrat.Substring(batchCommandNewConflict.Length));
                        }
                        else
                        {
                            Console.WriteLine("Bad line input :" + i);
                        }
                    }
                }                
            }
            if (checkBox1.Checked)
            {
                OpenFolder(this, null);
            }
            //End Timer
            opEndTime = System.DateTime.Now;
            saveMode = SaveFileConflictMode.Ask;
            label4.Text = "Time Taken : " + (opEndTime - opStartTime).ToString() + " seconds. Items Downloaded : " + itemsDownloaded;
            
        }
        // Opens the folder where the images will be downloaded to.
        private void OpenFolder(object sender, EventArgs e)
        {
            Console.WriteLine("File will be saved to: " + textBox2.Text + "\\" + textBox3.Text + "." + outFormat.ToString().ToLower());
            if (lastSavedFile.Length > 0)
            {
                //Open to folder, selecting last downloaded file.
                System.Diagnostics.Process.Start("explorer.exe", @"/select, " + lastSavedFile);
            }
            else 
            {
                //Open to folder.
                System.Diagnostics.Process.Start("explorer.exe", @textBox2.Text);
            }
            
        }
        // Exits the program.
        private void button2_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }


        // Progress Bar - Requires multithreading and adds more complexity than I care to get into at the moment.
        public void SetDownloadProgress(int value)
        {
            progressBar1.Value = value;
        }

        private void OpenFolderBrowserDialog(object sender, MouseEventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog1 = new FolderBrowserDialog();
            if (textBox2.Text.Length > 0)
            {
                folderBrowserDialog1.SelectedPath = textBox2.Text;
            }
            folderBrowserDialog1.Description = "Please select a valid folder for your downloads.";
            folderBrowserDialog1.ShowDialog();
            Console.WriteLine("Test1");
            if (folderBrowserDialog1.SelectedPath.Length > 0)
            {
                textBox2.Text = folderBrowserDialog1.SelectedPath;
            }
            else
            {
                Console.WriteLine("No Folder was selected.");
            }

            Console.WriteLine(sender.ToString());
        }
        private void OpenFileFolderBrowserDialog(object sender, MouseEventArgs e)
        {
            OpenFileDialog folderBrowserDialog1 = new OpenFileDialog();
            folderBrowserDialog1.Filter = "";
            folderBrowserDialog1.Title = "Please select a valid folder for your downloads.";
            folderBrowserDialog1.ShowDialog();
            textBox2.Text = folderBrowserDialog1.FileName;
            Console.WriteLine("Test2");
        }

        private void OpenFileSaveDialog(object sender, MouseEventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "Jpeg Image|*.jpg|Bitmap Image|*.bmp|Gif Image|*.gif|Portable Network Graphic Image|*.png";
            saveFileDialog1.Title = "Save an Image File";
            saveFileDialog1.InitialDirectory = textBox2.Text;
            saveFileDialog1.ShowDialog();

            // If the file name is not an empty string open it for saving.
            if (saveFileDialog1.FileName != "")
            {
                textBox3.Text = saveFileDialog1.FileName;
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (comboBox1.SelectedIndex)
            {
                case 0:
                default:
                    outFormat = ImageFormat.Png;
                    break;
                case 1:
                    outFormat = ImageFormat.Jpeg;
                    break;
                case 2:
                    outFormat = ImageFormat.Bmp;
                    break;
                case 3:
                    outFormat = ImageFormat.Tiff;
                    break;
                case 4:
                    outFormat = ImageFormat.Gif;
                    break;
            }
            
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            //Make sure that this input is always only a filename - not a path
            Console.WriteLine(sender.ToString());
            textBox3.Text = textBox3.Text.Substring(textBox3.Text.LastIndexOf("\\") + 1);
        }

        private void comboBox1_TextUpdate(object sender, EventArgs e)
        {
            bool validFormat = false;
            for (int i = 0; i < comboBox1.Items.Count; i++)
            {
                if (comboBox1.Text == comboBox1.Items.ToString())
                {
                    validFormat = true;
                    comboBox1.Text = comboBox1.Items[i].ToString();
                    comboBox1.SelectedIndex = i;
                }
            }
            if (!validFormat)
            {
                comboBox1.Text = comboBox1.Items[0].ToString();
                comboBox1.SelectedIndex = 0;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveSettings(SettingsFile);
        }

        private void textBox4_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "Text File|*.txt";
            openFileDialog1.Title = "Save an Image File";
            openFileDialog1.InitialDirectory = textBox2.Text;
            openFileDialog1.ShowDialog();

            // If the file name is not an empty string open it for saving.
            if (openFileDialog1.FileName != "")
            {
                textBox4.Text = openFileDialog1.FileName;
            }
        }
    }
}
