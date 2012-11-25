using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;


namespace String_Replacer
{
    #region Description
    ///<summary>
    /// Description
    ///</summary>
    public partial class frmMain : Form
    {
        #region Private Member Variables
        private bool IsFolder;
        #endregion

        #region Private Methods
        //Open File button handler
        private void btnOpen_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Filter = "Any type|*.*";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    txbPath.Text = ofd.FileName;
                    IsFolder = false;
                }
            }
            catch (OutOfMemoryException)
            {
                MessageBox.Show("Application can't open files!");
            }
            ChangeView();
        }

        //Open Folder button handler
        private void btnOpenFolder_Click(object sender, EventArgs e)
        {
            try
            {
                FolderBrowserDialog fbd = new FolderBrowserDialog();
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txbPath.Text = fbd.SelectedPath;
                    IsFolder = true;
                }
            }
            catch (OutOfMemoryException)
            {
                MessageBox.Show("Application can't open folders!");
            }
            ChangeView();
        }

        //Start button handler
        private void btnStart_Click(object sender, EventArgs e)
        {
            if (ControlsValidator() == true)
            {
                Replacer();
                pictureBox1.Visible = false;
                pictureBox2.Visible = false;
                pictureBox3.Visible = true;
            }            
        }

        //function
        private bool IsFolderExists(string Path)
        {
            try
            {
                DirectoryInfo di = new DirectoryInfo(Path);
                if (di.Exists == true)
                    return true;
            }
            catch (Exception)
            {
                MessageBox.Show("Can't get access to directory!");
            }
            return false;
        }

        //Function check for existing of file in Path
        private bool IsFileExists(string Path)
        {
            try
            {
                FileInfo fi = new FileInfo(Path);
                if (fi.Exists == true)
                    return true;
            }
            catch (Exception)
            {
                MessageBox.Show("Can't get access to file!");
            }
            return false;
        }

        //Method constraint mask from string entered in Mask TextBox
        private string MaskConstraint(string Mask)
        {
            try
            {
                // Replace . to \.
                Mask = Mask.Replace(".", @"\." /* (".", "\\.") */);
                // Replace ? to .
                Mask = Mask.Replace("?", ".");
                // Replace * to .*
                Mask = Mask.Replace("*", ".*");
                // We need to find string exact to mask:
                Mask = "^" + Mask + "$";
            }
            catch (Exception)
            {
                MessageBox.Show("Mask is wrong!");
                return String.Empty;
            }
            return Mask;
        }

        //Method changes view of controls step by step to help User
        private void ChangeView()
        {
            if (txbPath.Text == String.Empty)
            {
                txbReplaceTo.Enabled = false;
                txbReplaceFrom.Enabled = false;
                txbMask.Enabled = false;
                btnStart.Enabled = false;
                pictureBox1.Visible = true;
                pictureBox2.Visible = false;
                pictureBox3.Visible = false;
            }
            else
            {
                txbReplaceTo.Enabled = true;
                txbReplaceFrom.Enabled = true;
                txbMask.Enabled = true;
                btnStart.Enabled = true;
                pictureBox1.Visible = false;
                pictureBox2.Visible = true;
                pictureBox3.Visible = false;
            }

            if (IsFolder == true)
                txbMask.Enabled = true;
            else
                txbMask.Enabled = false;
        }

        //Function check for file paths in Text Boxes
        private bool ControlsValidator()
        {
            if(txbReplaceFrom.Text != String.Empty && txbPath.Text != String.Empty)
            {
                if(IsFolder == true)
                {
                    if(txbMask.Text != String.Empty && IsFolderExists(txbPath.Text) == true)
                        return true;
                }
                else
                {
                    if (IsFileExists(txbPath.Text) == true)
                        return true;
                }
            }
            return false;
        }
        #endregion

        #region Constructor
        public frmMain()
        {
            InitializeComponent();
            IsFolder = false;
            txbReplaceTo.Enabled = false;
            txbReplaceFrom.Enabled = false;
            txbMask.Enabled = false;
            btnStart.Enabled = false;
            pictureBox2.Visible = false;
            pictureBox3.Visible = false;
        }
        #endregion

        #region Public methods

        //Main function of replacing
        public void Replacer()
        {
            string Mask = MaskConstraint(txbMask.Text);
            string TextToReplace = txbReplaceFrom.Text;
            string TextToAppend = txbReplaceTo.Text;

            //create regular expression object from Mask
            Regex regMask = new Regex(Mask, RegexOptions.IgnoreCase);
            //escape from special symbols
            TextToReplace = Regex.Escape(TextToReplace);
            //create regular expression object from Text
            Regex regText = new Regex(TextToReplace, RegexOptions.Multiline);
            txbLog.Text = String.Empty;
            //if search in folder: need to find all folders and files under it 
            if (IsFolder == true)
            {
                InFolderReplacer(txbPath.Text, regText, regMask, TextToAppend);
            }
            else
            {
                InFileReplacer(txbPath.Text, regText, TextToAppend);
            }
        }

        //Function find directory and text in files under this directory
        public void InFolderReplacer(string Path, Regex regText, Regex regMask, string TextToAppend)
        {
            ulong Matches = 0;
            try
            {
                DirectoryInfo di = new DirectoryInfo(Path);
                Matches = FindTextInFiles(di, regText, regMask, TextToAppend);
            }
            catch (OutOfMemoryException)
            {
                MessageBox.Show("You don't have enough memory for continue work!");
            }
            catch (ArgumentException)
            {
                MessageBox.Show("Please fix folder path!");
            }
            catch (System.Security.SecurityException)
            {
                MessageBox.Show("Application doesn't have enough access rights!");
            }
            catch (PathTooLongException)
            {
                MessageBox.Show("Please input less path!");
            }
            //show status in Log textbox
            txbLog.Text = Matches.ToString() + " files found!" + Environment.NewLine + txbLog.Text;
        }



        // Recurrence function  find text in files of THIS folder 
        // and under all subfolders with recursion call
        public ulong FindTextInFiles(DirectoryInfo di, Regex regText, Regex regMask, string TextToAppend)
        {
            ulong CountOfMatchFiles = 0;
            FileInfo[] fi = null;
            try  // Get List of Files
            {
                fi = di.GetFiles();
                //Search in list of files
                foreach (FileInfo f in fi)
                {
                    if (regMask.IsMatch(f.Name)) //in files which is appropriate to mask
                    {
                        ++CountOfMatchFiles;
                        InFileReplacer(di.FullName + @"\" + f.Name, regText, TextToAppend);
                    }
                }
            }
            catch
            {
                MessageBox.Show("Can't get access to file!");
                return CountOfMatchFiles;
            }
            //search in subfolders 
            try
            {
                DirectoryInfo[] diSub = di.GetDirectories();  // list of subfolders
                foreach (DirectoryInfo diSubDir in diSub) // Recursion for every subfolder
                    CountOfMatchFiles += FindTextInFiles(diSubDir, regText, regMask, TextToAppend);
            }
            catch(Exception)
            {
                MessageBox.Show("Can't search in subfolders!");
                return CountOfMatchFiles;
            }
            return CountOfMatchFiles;
        }

        //Replace text in file
        public void InFileReplacer(string Path, Regex regText, string TextToAppend)
        {
            string Result = String.Empty;
            string Content = String.Empty;
            //read
            try
            {
                StreamReader sr = new StreamReader(Path, Encoding.Default);
                Content = sr.ReadToEnd();
                sr.Close();
            }
            catch (Exception)
            {
                MessageBox.Show("Can't read file!");
            }
            //replace
            try
            {
                Result = regText.Replace(Content, TextToAppend);
            }
            catch (Exception)
            {
                MessageBox.Show("Can't replace text in file!");
            }
            if (Result == Content)
                txbLog.Text += "+ Text is not found in " + Path + Environment.NewLine;
            else
            {
                //write
                try
                {
                    StreamWriter writetofile = new StreamWriter(Path, false, Encoding.Default, Result.Length);
                    writetofile.Write(Result);
                    writetofile.Close();
                }
                catch (Exception)
                {
                    MessageBox.Show("Can't write text in file!");
                }
                txbLog.Text += "+ Text replaced in: " + Path + Environment.NewLine;
            }
        }
        #endregion
    }
    #endregion
}
