using System;
using System.Collections.Generic;//Su dung List<>
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.Util;
using Emgu.CV.Cuda;
using FaceDetection;
using System.IO;//Thư viện để vào ra file
using System.Xml.Linq;//Import thư viện làm việc với XML

namespace NhanDangKhuonMat
{
    public partial class Form1 : Form
    {
        //declaring global variables
        private Capture capture = null;        //Lay anh tu may anh
        private bool captureInProgress; // kiem tra neu may anh dang thuc thi
        public Form1()
        {
            InitializeComponent();
            CvInvoke.UseOpenCL = false;
            try
            {
                capture = new Capture();
                capture.ImageGrabbed += ProcessFrame;
            }
            catch (NullReferenceException excpt)
            {
                MessageBox.Show(excpt.Message);
            }
        }
     

      
        private void ProcessFrame(object sender, EventArgs arg)
        {
            Mat frame = new Mat();
            capture.Retrieve(frame, 0);

            Mat image = frame; //Doc file theo kieu mau RBG 8-bit 
            long detectionTime;
            List<Rectangle> faces = new List<Rectangle>();
            List<Rectangle> eyesleft = new List<Rectangle>();
            List<Rectangle> eyesright = new List<Rectangle>();
      

            //The cuda cascade classifier doesn't seem to be able to load "haarcascade_frontalface_default.xml" file in this release
            //disabling CUDA module for now

            bool tryUseCuda = false;
            bool tryUseOpenCL = true;


            DetectFace.Detect(
              image, "haarcascade_frontalface_default.xml", "haarcascade_lefteye_2splits.xml", "haarcascade_righteye_2splits.xml",
              faces, eyesleft,eyesright, 
              tryUseCuda,
              tryUseOpenCL,
              out detectionTime);

            

            foreach (Rectangle face in faces)
            {
                CvInvoke.Rectangle(image, face, new Bgr(Color.White).MCvScalar, 3);
                Bitmap c = frame.Bitmap;
                Bitmap bmp = new Bitmap(face.Size.Width, face.Size.Height);
                Graphics g = Graphics.FromImage(bmp);
                g.DrawImage(c, 0, 0, face, GraphicsUnit.Pixel);

                
            }
            

            foreach (Rectangle eyeleft in eyesleft)
            {
                CvInvoke.Rectangle(image, eyeleft, new Bgr(Color.Black).MCvScalar, 3);
                if (eyeleft.X != 0 ) SendKeys.SendWait("{PGUP}");
       
            }

            foreach (Rectangle eyeright in eyesright)
            {
                CvInvoke.Rectangle(image, eyeright, new Bgr(Color.Black).MCvScalar, 3);
                if (eyeright.X != 0) SendKeys.SendWait("{PGDN}");
            }
                

            imageBox2.Image = frame;//thiet lap hinh anh
            
           

        }
        //giai phong du lieu
        private void ReleaseData()
        {
            if (capture != null)
                capture.Dispose();
        }

   
             

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (capture != null)
            {
                if (captureInProgress)
                {  //Dung may anh
                    toolStripMenuItem1.Image = NhanDangKhuonMat.Properties.Resources.play_button;
                    capture.Pause();
                }
                else
                {
                    //bat dau may anh
                    toolStripMenuItem1.Image = NhanDangKhuonMat.Properties.Resources.pause_button;
                    toolStripMenuItem1.Text = "Pause";
                    capture.Start();
                }

                captureInProgress = !captureInProgress;
            }
        }

        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            if (capture != null) capture.FlipVertical = !capture.FlipVertical;
        }

        private void horizonticalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (capture != null) capture.FlipHorizontal = !capture.FlipHorizontal;
        }

        private void toolStripMenuItem6_Click(object sender, EventArgs e)
        {
            
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
        }

        private void loadimageUp()
        {
            int n = imageList1.Images.Count;
            if (i > n - 1) i = 0;
            pictureBox1.Image = imageList1.Images[i];
            i++;
        }
        private void loadimageDown()
        {
            int n = imageList1.Images.Count;
            if (i == 0) i = n-1;
            pictureBox1.Image = imageList1.Images[i];
            i--;
        }

       int i = 0;

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Bạn có chắc chắn thoát?","Thông báo",MessageBoxButtons.YesNo,MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Application.Exit();
            }
        }
        
        //Bắt sự kiện PageUp, PageDown
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            
            if (keyData == Keys.PageUp)
            {
                loadimageUp();
                return true;
            }
           
            if (keyData == Keys.PageDown)
            {
                loadimageDown();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        
    }
}
