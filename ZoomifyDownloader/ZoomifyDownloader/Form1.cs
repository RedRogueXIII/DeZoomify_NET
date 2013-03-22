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


    public partial class Form1 : Form
    {
        protected ImageFormat outFormat = ImageFormat.Png;
        public string SettingsFile = "settings.xml";
        protected string ExecutionPath;
        protected SaveFileConflictMode saveMode = SaveFileConflictMode.Ask;

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
            }
            catch
            {
                return;
            }
        }

        // Starts download.
        private void button1_click(object sender, EventArgs e)
        {
            //Allow user to set options of output image format.
            //Save frequent and last used settings.
            //Allow batch downloading and renaming.
            //Allow renaming masks.
            //Optional open folder upon finish.      
            if (radioButton1.Checked)
            {
                //Open Zoomify Path & Download
                Bitmap download = Dezoomify.Download(textBox1.Text);

                //Save Image on Success
                if (download != null)
                {
                    string saveFilePath = textBox2.Text + "\\" + textBox3.Text + "." + outFormat.ToString().ToLower();
                    if (File.Exists(saveFilePath) && saveMode == SaveFileConflictMode.Ask)
                    {
                        //DoDialog Box
                        string messageBoxText = "A file with the name: " + textBox3.Text + " already exists. Do you want to replace it?";
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
                                saveMode = SaveFileConflictMode.Replace;
                                Dezoomify.Save(download, outFormat, textBox2.Text, textBox3.Text, saveMode);
                                break;
                            case DialogResult.No:
                                // User pressed No button 
                                // ...
                                saveMode = SaveFileConflictMode.Increment;
                                Dezoomify.Save(download, outFormat, textBox2.Text, textBox3.Text, saveMode);
                                break;
                            case DialogResult.Cancel:
                                // User pressed Cancel button 
                                // ... 
                                break;
                        }
                    }
                    else
                    {
                        Dezoomify.Save(download, outFormat, textBox2.Text, textBox3.Text, saveMode);
                    }
                    
                }
            }
            else if (radioButton2.Checked)
            {
                //Open File
                //Extract Paths
                //Download
                //Save
                StreamReader streamReader = new StreamReader(textBox4.Text);
                string text = streamReader.ReadToEnd();
                streamReader.Close();
            }
            
        }
        // Opens the folder where the images will be downloaded to.
        private void button3_Click(object sender, EventArgs e)
        {
            Console.WriteLine("File will be saved to: " + textBox2.Text + "\\" + textBox3.Text + "." + outFormat.ToString().ToLower());
            System.Diagnostics.Process.Start("explorer.exe", @textBox2.Text);
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
    }
}
