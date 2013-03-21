using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ZoomifyDownloader
{
    public partial class Form1 : Form
    {
        protected ImageFormat outFormat = ImageFormat.Png;

        public Form1()
        {
            InitializeComponent();
            textBox2.Text = System.Environment.GetFolderPath(Environment.SpecialFolder.MyPictures, Environment.SpecialFolderOption.Create);
        }

        private void button1_click(object sender, EventArgs e)
        {

            Bitmap download = Dezoomify.Download(textBox1.Text, SetDownloadProgress);
            if( download != null ) {
                //Check for if file with name already exists.
                //Allow user to specify what image format to ouput.
                //Allow user to set options of output image format.
                //Save frequent and last used settings.
                //Allow batch downloading and renaming.
                //Allow renaming masks.
                //Optional open folder upon finish.                
                download.Save(textBox2.Text + "\\" + textBox3.Text+"." + outFormat.ToString().ToLower(), outFormat);                
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Console.WriteLine("File will be saved to: " + textBox2.Text + "\\" + textBox3.Text + "." + outFormat.ToString().ToLower());
            System.Diagnostics.Process.Start("explorer.exe", @textBox2.Text);
        }

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
    }
}
