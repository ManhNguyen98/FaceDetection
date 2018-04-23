//----------------------------------------------------------------------------
//  Copyright (C) 2004-2015 by EMGU Corporation. All rights reserved.       
//----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.Structure;
#if !(IOS || NETFX_CORE)
using Emgu.CV.Cuda;
#endif

namespace FaceDetection
{
    public static class DetectFace
    {
        public static void Detect(
          Mat image, String faceFileName, String eyeleftFileName, string eyerightFileName,
          List<Rectangle> faces, List<Rectangle> eyesleft,List<Rectangle> eyesright,
          bool tryUseCuda, bool tryUseOpenCL,
          out long detectionTime)
        {
            Stopwatch watch;

#if !(IOS || NETFX_CORE)
            if (tryUseCuda && CudaInvoke.HasCuda)
            {
                using (CudaCascadeClassifier face = new CudaCascadeClassifier(faceFileName))
                using (CudaCascadeClassifier eyeleft = new CudaCascadeClassifier(eyeleftFileName))
                using (CudaCascadeClassifier eyeright = new CudaCascadeClassifier(eyerightFileName))
                {
                    face.ScaleFactor = 1.1;
                    face.MinNeighbors = 10;
                    face.MinObjectSize = Size.Empty;

                    eyeleft.ScaleFactor = 1.1;
                    eyeleft.MinNeighbors = 10;
                    eyeleft.MinObjectSize = Size.Empty;

                    eyeright.ScaleFactor = 1.1;
                    eyeright.MinNeighbors = 10;
                    eyeright.MinObjectSize = Size.Empty;
                    watch = Stopwatch.StartNew();
                    using (CudaImage<Bgr, Byte> gpuImage = new CudaImage<Bgr, byte>(image))
                    using (CudaImage<Gray, Byte> gpuGray = gpuImage.Convert<Gray, Byte>())
                    using (GpuMat region = new GpuMat())
                    {
                        face.DetectMultiScale(gpuGray, region);
                        Rectangle[] faceRegion = face.Convert(region);
                        faces.AddRange(faceRegion);
                        foreach (Rectangle f in faceRegion)
                        {
                            using (CudaImage<Gray, Byte> faceImg = gpuGray.GetSubRect(f))
                            {
                                //For some reason a clone is required.
                                //Might be a bug of CudaCascadeClassifier in opencv
                                using (CudaImage<Gray, Byte> clone = faceImg.Clone(null))
                                using (GpuMat eyeRegionMat = new GpuMat())
                                {
                                    eyeleft.DetectMultiScale(clone, eyeRegionMat);
                                    Rectangle[] eyeRegion = eyeleft.Convert(eyeRegionMat);
                                    foreach (Rectangle eleft in eyeRegion)
                                    {
                                        Rectangle eyeRectleft = eleft;
                                        eyeRectleft.Offset(f.X, f.Y);
                                        eyesleft.Add(eyeRectleft);

                                    }

                                }
                                using (CudaImage<Gray, Byte> clone = faceImg.Clone(null))
                                using (GpuMat eyeRegionMat = new GpuMat())
                                {
                                    eyeright.DetectMultiScale(clone, eyeRegionMat);
                                    Rectangle[] eyeRegion = eyeright.Convert(eyeRegionMat);
                                    foreach (Rectangle eright in eyeRegion)
                                    {
                                        Rectangle eyeRectright = eright;
                                        eyeRectright.Offset(f.X, f.Y);
                                        eyesright.Add(eyeRectright);

                                    }

                                }
                            }
                        }
                    }
                    watch.Stop();
                }
            }
            else
#endif
            {
                //Many opencl functions require opencl compatible gpu devices. 
                //As of opencv 3.0-alpha, opencv will crash if opencl is enable and only opencv compatible cpu device is presented
                //So we need to call CvInvoke.HaveOpenCLCompatibleGpuDevice instead of CvInvoke.HaveOpenCL (which also returns true on a system that only have cpu opencl devices).
                CvInvoke.UseOpenCL = tryUseOpenCL && CvInvoke.HaveOpenCLCompatibleGpuDevice;


                //Read the HaarCascade objects
                using (CascadeClassifier face = new CascadeClassifier(faceFileName))
                using (CascadeClassifier eyeleft = new CascadeClassifier(eyeleftFileName))
                using (CascadeClassifier eyeright = new CascadeClassifier(eyerightFileName))
                {
                    watch = Stopwatch.StartNew();
                    using (UMat ugray = new UMat())
                    {
                        CvInvoke.CvtColor(image, ugray, Emgu.CV.CvEnum.ColorConversion.Bgr2Gray);

                        //Cân bằng sáng của ảnh
                        CvInvoke.EqualizeHist(ugray, ugray);

                        //Phát hiện các khuôn mặt từ hình ảnh màu xám và lưu các vị trí làm hình chữ nhật
                        // Chiều thứ nhất là kênh
                        // Kích thước thứ hai là chỉ mục của hình chữ nhật trong kênh cụ thể
                        Rectangle[] facesDetected = face.DetectMultiScale(
                           ugray,
                           1.1,
                           10,
                           new Size(20, 20));

                        faces.AddRange(facesDetected);

                        foreach (Rectangle f in facesDetected)
                        {
                            //Sử dụng khu vực của khuôn mặt
                            using (UMat faceRegion = new UMat(ugray, f))
                            {
                                //tìm hình chữ nhật của mắt phải
                                Rectangle[] eyesleftDetected = eyeleft.DetectMultiScale(
                                   faceRegion,
                                   1.1,
                                   10,
                                   new Size(20, 20));
                                foreach (Rectangle eleft in eyesleftDetected)
                                {
                                    Rectangle eyeRectleft = eleft;
                                    eyeRectleft.Offset(f.X, f.Y);
                                    eyesleft.Add(eyeRectleft);
                                }
                                //tìm hình chữ nhật của mắt phải
                                Rectangle[] eyesrightDetected = eyeright.DetectMultiScale(
                                  faceRegion,
                                  1.1,
                                  10,
                                  new Size(20, 20));
                                foreach (Rectangle eright in eyesrightDetected)
                                {
                                    Rectangle eyeRectright = eright;
                                    eyeRectright.Offset(f.X, f.Y);
                                    eyesright.Add(eyeRectright);
                                }
                            }
                        }
                    }
                    watch.Stop();
                }
            }
            detectionTime = watch.ElapsedMilliseconds;//đo tổng thời gian trôi qua
        }
    }
}
