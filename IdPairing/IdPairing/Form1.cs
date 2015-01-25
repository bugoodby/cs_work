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

        // シナリオ情報
        public class ScenarioInfo
        {
            public string id;
            public string targetPath;
            public string expectPath;

            public ScenarioInfo() { }
        }

        Settings settings_ = new Settings();
        Dictionary<string, ScenarioInfo> scenarioInfoDict_ = new Dictionary<string, ScenarioInfo>();
        Dictionary<string, string> modelExpectList_ = new Dictionary<string, string>();
        Dictionary<string, string> commonExpectList_ = new Dictionary<string, string>();

        //----------------------------------------------------------------------

        // 実行ファイルのフォルダパスを返す
        private string GetModuleDir()
        {
            string path = Application.ExecutablePath;
            return System.IO.Path.GetDirectoryName(path) + @"\";
        }

        // 期待値データフォルダを検索し、期待値シナリオ一覧を作成する
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

        // keyFilePatternにマッチするファイルを検索してリスト化する
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

        // バッチファイルを実行する
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

        // 設定ファイル(XML)を読み込む
        private void loadSettings(string settingFilePath)
        {
            System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(Settings));
            System.IO.StreamReader sr = new System.IO.StreamReader(settingFilePath);
            settings_ = (Settings)serializer.Deserialize(sr);
            sr.Close();
        }
        // 設定ファイル(XML)に書き出す
        private void saveSettings(string settingFilePath)
        {
            System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(Settings));
            System.IO.StreamWriter sw = new System.IO.StreamWriter(settingFilePath, false);
            serializer.Serialize(sw, settings_);
            sw.Close();
        }

        // keyFilePatternにマッチするファイルを検索してlistView1に行追加する
        private void searchTarget(string rootDir, string keyFilePattern)
        {
            // keyFilePatternにマッチするファイルを探し、keyFileListに追加
            string[] files = System.IO.Directory.GetFiles(rootDir, keyFilePattern);
            foreach (string file in files)
            {
                ScenarioInfo info = new ScenarioInfo();
                info.id = getIdFromConfFile(file);
                info.targetPath = Path.GetDirectoryName(file);
                scenarioInfoDict_[info.id] = info;

                string[] str = { info.id, "", info.targetPath };
                ListViewItem item = new ListViewItem(str);
                listView1.Items.Add(item);
            }

            // サブディレクトリを再帰検索
            string[] dirs = System.IO.Directory.GetDirectories(rootDir);
            foreach (string dir in dirs)
            {
                searchTarget(dir, keyFilePattern);
            }
        }

        // 構成ファイルを読み込みID文字列を取得する
        private string getIdFromConfFile(string path)
        {
            System.IO.StreamReader sr = new StreamReader(path);

            // 1行ずつ読み込む
            while (sr.Peek() >= 0)
            {
                string line = sr.ReadLine();
                string[] tokens = line.Split(new Char[]{'='}, 2);
                if (tokens.Length == 2 && tokens[0].Trim() == "idvalue")
                {
                    return tokens[1];
                }
            }

            return "(unknown)";
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
#if true
                    searchTarget(f, "config.txt");
#else
                    string[] str = { System.IO.Path.GetFileName(f), "", f };
                    ListViewItem item = new ListViewItem(str);
                    listView1.Items.Add(item);
#endif
                }
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            listView1.Items.Clear();
            scenarioInfoDict_.Clear();
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

                string id = item.SubItems[0].Text;
                ScenarioInfo info = scenarioInfoDict_[id];

                string batch = GetModuleDir() + "test.bat";
                string param = String.Format(@"""{0}"" ""{1}"" ""{2}""", id, info.targetPath, info.expectPath);
                callBatch(batch, param);
            }
        }

        private void btnCheck_Click(object sender, EventArgs e)
        {
            makeExpectDataBase(settings_.expectPath, comboModel.Text);

            foreach (ListViewItem item in listView1.Items)
            {
                string id = item.SubItems[0].Text;
                ScenarioInfo info = scenarioInfoDict_[id];

                // IDがmodelに存在すればそちらを優先
                if (modelExpectList_.ContainsKey(id))
                {
                    info.expectPath = modelExpectList_[id];
                    item.SubItems[1].Text = info.expectPath.Replace(settings_.expectPath, ".");
                }
                // IDがmodelになくてもCommonにあればそちらを採用
                else if (commonExpectList_.ContainsKey(id))
                {
                    info.expectPath = commonExpectList_[id];
                    item.SubItems[1].Text = info.expectPath.Replace(settings_.expectPath, ".");
                }
            }
        }
    }
}
