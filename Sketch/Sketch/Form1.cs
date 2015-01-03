using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sketch
{
    public partial class Form1 : Form
    {
        enum DrawType {
            Free,
            Line
        };

        Graphics graphics;
        DrawType drawType = DrawType.Free;

        bool isDrawing = false;
        bool isMoving = false;
        Point start = new Point(0, 0);
        Point end = new Point(0, 0);
        List<Point> points = new List<Point>();

        string appdir = "";
        string outfile = "";
        Size mergin;

        private void x_setImage1()
        {
            outfile = "img1points.txt";
            String s = appdir + "image1.bmp";
            if (System.IO.File.Exists(s))
            {
                pictureBox.Image = Image.FromFile(s);
                pictureBox.Size = pictureBox.Image.Size;
                this.Size = pictureBox.Image.Size + mergin;
                graphics = Graphics.FromImage(pictureBox.Image);
            }
            else
            {
                MessageBox.Show("Not found image1.bmp", "Sketch", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                pictureBox.Image = new Bitmap(Screen.PrimaryScreen.WorkingArea.Width, Screen.PrimaryScreen.WorkingArea.Height);
                graphics = Graphics.FromImage(pictureBox.Image);
            }
        }
        private void x_setImage2()
        {
            outfile = "img2points.txt";
            String s = appdir + "image2.bmp";
            if (System.IO.File.Exists(s))
            {
                pictureBox.Image = Image.FromFile(s);
                pictureBox.Size = pictureBox.Image.Size;
                this.Size = pictureBox.Image.Size + mergin;
                graphics = Graphics.FromImage(pictureBox.Image);
            }
            else
            {
                MessageBox.Show("Not found image2.bmp", "Sketch", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                pictureBox.Image = new Bitmap(Screen.PrimaryScreen.WorkingArea.Width, Screen.PrimaryScreen.WorkingArea.Height);
                graphics = Graphics.FromImage(pictureBox.Image);
            }
        }
        private void x_addLinePoints(Point start, Point end)
        {
            Point dst = new Point(end.X - start.X, end.Y - start.Y);

            if (Math.Abs(dst.X) < Math.Abs(dst.Y))
            {
                int sign = 1;
                double tan = (double)dst.X / (double)dst.Y;
                if (dst.Y < 0)
                {
                    dst.Y = -dst.Y;
                    sign = -1;
                }
                for (int y = 1; y <= dst.Y; y++)
                {
                    int x = (int)(Math.Round(tan * (double)y));
                    // 描画点の追加
                    points.Add(new Point(sign * x + start.X, sign * y + start.Y));
                }
            }
            else
            {
                int sign = 1;
                double tan = (double)dst.Y / (double)dst.X;
                if (dst.X < 0)
                {
                    dst.X = -dst.X;
                    sign = -1;
                }
                for (int x = 1; x <= dst.X; x++)
                {
                    int y = (int)(Math.Round(tan * (double)x));
                    // 描画点の追加
                    points.Add(new Point(sign * x + start.X, sign * y + start.Y));
                }
            }
        }

        public Form1()
        {
            InitializeComponent();

            // 実行ファイルのフォルダパス取得
            string path = Application.ExecutablePath;
            appdir = System.IO.Path.GetDirectoryName(path) + @"\";
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            mergin = this.Size - pictureBox.Size;
            x_setImage1();
            toolStripButton1.Checked = true;
            toolStripButton3.Checked = true;
        }

        private void pictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            isDrawing = true;
            start.X = end.X = e.X;
            start.Y = end.Y = e.Y;

            // 描画点の初期化と初期位置の追加
            points.Clear();
            points.Add(new Point(e.X, e.Y));
        }

        private void pictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            isDrawing = false;
            isMoving = false;

            if (drawType == DrawType.Line)
            {
                x_addLinePoints(start, end);
                // 線の描画
                graphics.DrawLine(Pens.Magenta, start, end);
            }
            
            // 描画点列をファイルに保存
            System.IO.StreamWriter sw = new System.IO.StreamWriter(appdir + outfile, false);
            foreach (Point p in points) {
                sw.WriteLine(p.X + ", " + p.Y);
            }
            sw.Close();

            // デバッグ
            foreach (Point p in points)
            {
                graphics.FillRectangle(Brushes.Black, new Rectangle(p.X, p.Y, 1, 1));
            }

            pictureBox.Refresh();
        }

        private void pictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isDrawing) return;

            end.X = e.X;
            end.Y = e.Y;
            
            switch ( drawType )
            {
                case DrawType.Free:
                    // 描画点の追加
                    x_addLinePoints(start, end);
                    // 線の描画
                    graphics.DrawLine(Pens.Magenta, start, end);
                    start.X = e.X;
                    start.Y = e.Y;
                    break;
                case DrawType.Line:
                    isMoving = true;
                    break;
                default:
                    break;
            }
 
            pictureBox.Refresh();
        }

        private void pictureBox_Paint(object sender, PaintEventArgs e)
        {
            if (!isMoving) return;
            e.Graphics.DrawLine(Pens.Lime, start, end);
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            toolStripButton1.Checked = true;
            toolStripButton2.Checked = false;
            x_setImage1();
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            toolStripButton1.Checked = false;
            toolStripButton2.Checked = true;
            x_setImage2();
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            toolStripButton3.Checked = true;
            toolStripButton4.Checked = false;
            drawType = DrawType.Free;
        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            toolStripButton3.Checked = false;
            toolStripButton4.Checked = true;
            drawType = DrawType.Line;
        }
   }
}
