﻿using System;
using System.Collections.Generic;
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
        private Capture capture = null;        //takes images from camera as image frames
        private bool captureInProgress; // checks if capture is executing
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

        private string path = "textfile.xml";

        private void loaddataface()
        {
            //***Tải dữ liệu từ bảng face của file XML vào ListView***//
            try
            {
                listView1.Items.Clear();
                DataSet datafaceSet = new DataSet();
                datafaceSet.ReadXml(path);
                DataTable dt = new DataTable();
                dt = datafaceSet.Tables["face"];
                int i = 0;
                foreach (DataRow dr in dt.Rows)
                {
                    listView1.Items.Add(dr["ID"].ToString());
                    listView1.Items[i].SubItems.Add(dr["height"].ToString());
                    listView1.Items[i].SubItems.Add(dr["width"].ToString());
                    i++;
                }
            }
            catch (Exception)
            {
                
            }
            //**********************************************************//
        }
        private void loaddataeyes()
        {
            //**Tải dữ liệu từ bảng eye của file XML vào ListView**//
            try
            {
                listView2.Items.Clear();
                DataSet dataSet = new DataSet();
                dataSet.ReadXml(path);
                DataTable dt = new DataTable();
                dt = dataSet.Tables["eye"];
                int i = 0;
                foreach (DataRow dr in dt.Rows)
                {
                    listView2.Items.Add(dr["ID"].ToString());
                    listView2.Items[i].SubItems.Add(dr["height"].ToString());
                    listView2.Items[i].SubItems.Add(dr["width"].ToString());
                    i++;
                }
            }
            catch (Exception)
            {

            }
            //**************************************************************//
        }
            

      
        private void ProcessFrame(object sender, EventArgs arg)
        {
            Mat frame = new Mat();
            capture.Retrieve(frame, 0);

            Mat image = frame; //Read the files as an 8-bit Bgr image  
            long detectionTime;
            List<Rectangle> faces = new List<Rectangle>();
            List<Rectangle> eyes = new List<Rectangle>();
      

            //The cuda cascade classifier doesn't seem to be able to load "haarcascade_frontalface_default.xml" file in this release
            //disabling CUDA module for now

            bool tryUseCuda = false;
            bool tryUseOpenCL = true;


            DetectFace.Detect(
              image, "haarcascade_frontalface_default.xml", "haarcascade_eye.xml",
              faces, eyes, 
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

                //**Xác định ID cho bảng face**//
                string day = DateTime.Now.Day.ToString();
                string month = DateTime.Now.Month.ToString();
                string year = DateTime.Now.Year.ToString();
                string hour = DateTime.Now.Hour.ToString();
                string minute = DateTime.Now.Minute.ToString();
                string second = DateTime.Now.Second.ToString();
                long id = long.Parse(day + month + year + hour + minute + second);
                //****************************//

                //**Thêm dữ liệu vào bảng face của file xml**//
                try
                {
                    XDocument testXML = XDocument.Load(path);
                    XElement newFaceDetect = new XElement("face",
                    new XElement("height", face.Size.Height),
                    new XElement("width", face.Size.Width));

                    var lastface = testXML.Descendants("face").Last();
                    long newID = Convert.ToInt64(lastface.Attribute("ID").Value);
                    newFaceDetect.SetAttributeValue("ID", id);

                    testXML.Element("FaceDetect").Add(newFaceDetect);
                    testXML.Save(path);
                    loaddataface();
                }
                catch (Exception err)
                {
                    MessageBox.Show(err.Message);
                }
                //***************************************************//
            }
            

            foreach (Rectangle eye in eyes)
            {
                CvInvoke.Rectangle(image, eye, new Bgr(Color.Black).MCvScalar, 2);
                //**Xác định ID cho bảng eye**//
                string day = DateTime.Now.Day.ToString();                            
                string month = DateTime.Now.Month.ToString();                       
                string year = DateTime.Now.Year.ToString();                         
                string hour = DateTime.Now.Hour.ToString();                         
                string minute = DateTime.Now.Minute.ToString();                     
                string second = DateTime.Now.Second.ToString();                     
                long id = long.Parse(day + month + year + hour + minute + second); 
                //****************************//

                //**Thêm dữ liệu vào bảng eye của file XML**// 
                try
                {
                    XDocument testXML = XDocument.Load(path);
                    XElement newFaceDetect = new XElement("eye",
                    new XElement("height", eye.Size.Height),
                    new XElement("width", eye.Size.Width));

                    var lasteyes = testXML.Descendants("eye").Last();
                    long newID = Convert.ToInt64(lasteyes.Attribute("ID").Value);
                    newFaceDetect.SetAttributeValue("ID", id);

                    testXML.Element("FaceDetect").Add(newFaceDetect);
                    testXML.Save(path);
                    loaddataeyes();
                }
                catch (Exception err)
                {
                    MessageBox.Show(err.Message);
                }
                //*********************************************//
            }
                

            imageBox1 .Image = frame;
            
           

        }
        private void ReleaseData()
        {
            if (capture != null)
                capture.Dispose();
        }

   
        //Bắt đầu và tạm dừng sử dụng camera
        private void btnStart_Click_1(object sender, EventArgs e)
        {
            if (capture != null)
            {
                if (captureInProgress)
                {  //stop the capture
                    btnStart.Image = NhanDangKhuonMat.Properties.Resources.play_button;
                    capture.Pause();
                }
                else
                {
                    //start the capture
                    btnStart.Image = NhanDangKhuonMat.Properties.Resources.pause_button;
                    
                    capture.Start();
                }

                captureInProgress = !captureInProgress;
            }
        }

        //Lật camera từ trên xuống dưới
        private void button2_Click_1(object sender, EventArgs e)
        {
            if (capture != null) capture.FlipVertical = !capture.FlipVertical;
        }

        //Lật camera phải qua trái
        private void button1_Click(object sender, EventArgs e)
        {
            if (capture != null) capture.FlipHorizontal = !capture.FlipHorizontal;
        }
        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void listView2_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void listView2_SelectedIndexChanged_1(object sender, EventArgs e)
        {

        }

        //Hiển thị ListView
        private void button3_Click(object sender, EventArgs e)
        {
            loaddataface();
            loaddataeyes();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            //Xoa tung cai mot
            /*if (listView1.SelectedItems.Count == 0) return;   
            listView1.SelectedItems[0].Remove();*/
            
            //Xoa tat ca
            listView1.Items.Clear();
            listView2.Items.Clear();

            //Xóa dữ liệu trong file XML
            XDocument testXML = XDocument.Load(path);
            testXML.Element("FaceDetect").Remove();
            testXML.Save(path);

            //********************************************//
        }
    }
}
