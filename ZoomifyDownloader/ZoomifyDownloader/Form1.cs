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
        protected ImageFormat outFormat = ImageFormat.Png;
        public string SettingsFile = "settings.xml";
        protected string ExecutionPath;
        protected SaveFileConflictMode saveMode = SaveFileConflictMode.Ask;
        public string lastSavedFile = "";

        public Form1()
        {
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
            string saveFilePath = path + "\\" + filename + "." + outFormat.ToString().ToLower();
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


        // Starts download.
        private void button1_click(object sender, EventArgs e)
        {
            SaveSettings(SettingsFile);
            // Download using a URL that points directly to the zoomify folder.   
            if (radioButton1.Checked)
            {
                DownloadDirectURL(textBox1.Text, textBox2.Text, textBox3.Text);
                if (checkBox1.Checked)
                {
                    OpenFolder(this, null);
                }
            }
            else if (radioButton2.Checked)
            {
                // Batch download using a list of URL that point directly to the zoomify folder.
                // Open File
                StreamReader streamReader = new StreamReader(textBox4.Text);
                string text = streamReader.ReadToEnd();
                streamReader.Close();
                List<string> urls = new List<string>();
                // Extract Paths
                string[] filter = { "\r\n", "\r", "\n" };
                urls.AddRange(text.Split(filter, StringSplitOptions.RemoveEmptyEntries));
                // Validate that each item is a url.
                foreach (string i in urls)
                {
                    // Download & Save
                    DownloadDirectURL(i, textBox2.Text, textBox3.Text, true);
                }
                if (checkBox1.Checked)
                {
                    OpenFolder(this, null);
                }
                saveMode = SaveFileConflictMode.Ask;
            }
            
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
