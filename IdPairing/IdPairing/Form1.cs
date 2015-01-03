using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.IO;
using System.Diagnostics;


namespace IdPairing
{
    public partial class Form1 : Form
    {
        // アプリケーションの設定値
        public class Settings
        {
            public string expectPath;
            public string[] models;

            public Settings() { }
        }

        Settings settings_ = new Settings();
        Dictionary<string, string> modelExpectList_ = new Dictionary<string, string>();
        Dictionary<string, string> commonExpectList_ = new Dictionary<string, string>();

        //----------------------------------------------------------------------

        private string GetModuleDir()
        {
            // 実行ファイルのフォルダパス取得
            string path = Application.ExecutablePath;
            return System.IO.Path.GetDirectoryName(path) + @"\";
        }

        private void makeExpectDataBase(string expectDir, string modelName)
        {
            string keyFilePattern = "*.txt";
            var keyFileList = new List<string>();
            string str = "";

            modelExpectList_.Clear();
            commonExpectList_.Clear();
            
            // 期待値(共通)一覧を取得
            str = expectDir + @"\_Common\";
            Debug.WriteLine(str);
            if (Directory.Exists(str))
            {
                keyFileList.Clear();
                searchKeyFile(keyFileList, str, keyFilePattern);
                foreach (string f in keyFileList)
                {
                    string path = Path.GetDirectoryName(f);
                    string id = Path.GetFileName(path);
                    commonExpectList_[id] = path;
                    Debug.WriteLine("  " + id + ", " + path);
                }
            }

            // 期待値(モデル別)一覧を取得
            str = expectDir + @"\" + modelName + @"\";
            Debug.WriteLine(str);
            if (Directory.Exists(str))
            {
                keyFileList.Clear();
                searchKeyFile(keyFileList, str, keyFilePattern);
                foreach (string f in keyFileList)
                {
                    string path = Path.GetDirectoryName(f);
                    string id = Path.GetFileName(path);
                    modelExpectList_[id] = path;
                    Debug.WriteLine("  " + id + ", " + path);
                }
            }
        }

        private void searchKeyFile(List<string> keyFileList, string rootDir, string keyFilePattern)
        {
            // keyFilePatternにマッチするファイルを探し、keyFileListに追加
            string[] files = System.IO.Directory.GetFiles(rootDir, keyFilePattern);
            foreach (string file in files)
            {
                keyFileList.Add(file);
            }

            // サブディレクトリを再帰検索
            string[] dirs = System.IO.Directory.GetDirectories(rootDir);
            foreach (string dir in dirs)
            {
                searchKeyFile(keyFileList, dir, keyFilePattern);
            }
        }

        private int callBatch(string batchPath, string param)
        {
            var startInfo = new ProcessStartInfo();
            startInfo.FileName = System.Environment.GetEnvironmentVariable("ComSpec");
            startInfo.CreateNoWindow = false;
            startInfo.UseShellExecute = false;
            startInfo.Arguments = String.Format(@"/c """"{0}"" {1}""", batchPath, param);

            Debug.WriteLine(startInfo.FileName);
            Debug.WriteLine(startInfo.Arguments);

            Process process = Process.Start(startInfo);
            process.WaitForExit();
            return process.ExitCode;
        }

        private void loadSettings(string settingFilePath)
        {
            System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(Settings));
            System.IO.StreamReader sr = new System.IO.StreamReader(settingFilePath);
            settings_ = (Settings)serializer.Deserialize(sr);
            sr.Close();
        }
        private void saveSettings(string settingFilePath)
        {
            System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(Settings));
            System.IO.StreamWriter sw = new System.IO.StreamWriter(settingFilePath, false);
            serializer.Serialize(sw, settings_);
            sw.Close();
        }

        //----------------------------------------------------------------------

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // 設定値のロード
            string settingFile = GetModuleDir() + @"settings.xml";
            if (File.Exists(settingFile))
            {
                loadSettings(settingFile);
            }
            else
            {
                settings_.expectPath = "path";
                settings_.models = new string[] { "test1", "test2" };
                saveSettings(settingFile);
            }

            // modelコンボボックスの初期化
            comboModel.Items.AddRange(settings_.models);
            comboModel.SelectedIndex = 0;
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (string f in files)
            {
                if (Directory.Exists(f))
                {
                    string[] str = { System.IO.Path.GetFileName(f), "", f };
                    ListViewItem item = new ListViewItem(str);
                    listView1.Items.Add(item);
                }
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            listView1.Items.Clear();
        }

        private void btnCall_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listView1.Items)
            {
                if (item.SubItems[1].Text == "")
                {
                    Debug.WriteLine("not expect path!");
                    continue;
                }

                string batch = GetModuleDir() + "test.bat";
                string param = String.Format(@"""{0}"" ""{1}""", item.SubItems[2].Text, item.SubItems[1].Text);
                callBatch(batch, param);
            }
        }

        private void btnCheck_Click(object sender, EventArgs e)
        {
            makeExpectDataBase(settings_.expectPath, comboModel.Text);

            foreach (ListViewItem item in listView1.Items)
            {
                string id = item.SubItems[0].Text;

                if (modelExpectList_.ContainsKey(id))
                {
                    item.SubItems[1].Text = modelExpectList_[id];
                }
                else if (commonExpectList_.ContainsKey(id))
                {
                    item.SubItems[1].Text = commonExpectList_[id];
                }
            }
        }
    }
}
