using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace biometria_4;

public static class Algorithm
{
    public static Bitmap Pixelation(Bitmap bitmap, int pixelSize)
    {
        BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
        byte[] vs = new byte[data.Height * data.Stride];
        Marshal.Copy(data.Scan0, vs, 0, vs.Length);

        for (int i = 0; i < bitmap.Height; i += pixelSize)
        {
            for (int j = 0; j < bitmap.Width * 3; j += pixelSize * 3)
            {
                if ((i + pixelSize / 2) * bitmap.Width * 3 + j + pixelSize * 3 / 2 + 2 >= vs.Length)
                    break;
                byte middlePixelR = vs[(i + pixelSize / 2) * bitmap.Width * 3 + j + pixelSize * 3 / 2];
                byte middlePixelG = vs[(i + pixelSize / 2) * bitmap.Width * 3 + j + pixelSize * 3 / 2 + 1];
                byte middlePixelB = vs[(i + pixelSize / 2) * bitmap.Width * 3 + j + pixelSize * 3 / 2 + 2];
                for (int k = i; k <= i + pixelSize; k++)
                {
                    for (int l = j; l <= j + pixelSize * 3; l += 3)
                    {
                        if (k * bitmap.Width * 3 + l + 2 >= vs.Length)
                            break;
                        vs[k * bitmap.Width * 3 + l] = middlePixelR;
                        vs[k * bitmap.Width * 3 + l + 1] = middlePixelG;
                        vs[k * bitmap.Width * 3 + l + 2] = middlePixelB;
                    }
                }
            }
        }
        Marshal.Copy(vs, 0, data.Scan0, vs.Length);
        bitmap.UnlockBits(data);

        return bitmap;
    }
    public static int Compare(byte[] a, byte[] b)
    {
        if (a[0] + a[1] + a[2] > b[0] + b[1] + b[2])
            return 1;
        return 0;
    }
    public static Bitmap Median(Bitmap bitmap, int pixelSize)
    {
        BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
        byte[] vs = new byte[data.Height * data.Stride];
        Marshal.Copy(data.Scan0, vs, 0, vs.Length);

        for (int i = 0; i < bitmap.Height; i += pixelSize)
        {
            for (int j = 0; j < bitmap.Width * 3; j += pixelSize * 3)
            {
                if ((i + pixelSize / 2) * bitmap.Width * 3 + j + pixelSize * 3 / 2 + 2 >= vs.Length)
                    break;
                List<byte[]> medianList = new();
                for (int k = i; k <= i + pixelSize; k++)
                {
                    for (int l = j; l <= j + pixelSize * 3; l += 3)
                    {
                        if (k * bitmap.Width * 3 + l + 2 >= vs.Length)
                            break;
                        medianList.Add(new byte[] { vs[k * bitmap.Width * 3 + l], vs[k * bitmap.Width * 3 + l + 1], vs[k * bitmap.Width * 3 + l + 2] });
                    }
                }
                medianList.Sort(Compare);
                var median = medianList[medianList.Count / 2];
                for (int k = i; k <= i + pixelSize; k++)
                {
                    for (int l = j; l <= j + pixelSize * 3; l += 3)
                    {
                        if (k * bitmap.Width * 3 + l + 2 >= vs.Length)
                            break;
                        vs[k * bitmap.Width * 3 + l] = median[0];
                        vs[k * bitmap.Width * 3 + l + 1] = median[1];
                        vs[k * bitmap.Width * 3 + l + 2] = median[2];
                    }
                }
            }
        }
        Marshal.Copy(vs, 0, data.Scan0, vs.Length);
        bitmap.UnlockBits(data);

        return bitmap;
    }

    public static Bitmap LinearFilter(Bitmap bitmap, double[,] xFilterMatrix,
                                        double[,]? yFilterMatrix, double bias = 1, bool grayscale = false)
    {
        BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppRgb);
        byte[] pixelBuffer = new byte[data.Stride *
                                  bitmap.Height];

        byte[] resultBuffer = new byte[data.Stride *
                                       data.Height];

        Marshal.Copy(data.Scan0, pixelBuffer, 0,
                                   pixelBuffer.Length);
        bitmap.UnlockBits(data);

        if (grayscale == true)
        {
            float rgb = 0;


            for (int k = 0; k < pixelBuffer.Length; k += 4)
            {
                rgb = pixelBuffer[k] * 0.11f;
                rgb += pixelBuffer[k + 1] * 0.59f;
                rgb += pixelBuffer[k + 2] * 0.3f;


                pixelBuffer[k] = (byte)rgb;
                pixelBuffer[k + 1] = pixelBuffer[k];
                pixelBuffer[k + 2] = pixelBuffer[k];
                pixelBuffer[k + 3] = 255;
            }
        }
        double blueX = 0.0;
        double greenX = 0.0;
        double redX = 0.0;

        double blueY = 0.0;
        double greenY = 0.0;
        double redY = 0.0;

        double blueTotal = 0.0;
        double greenTotal = 0.0;
        double redTotal = 0.0;

        int filterOffset = 1;
        int calcOffset = 0;

        int byteOffset = 0;

        for (int offsetY = filterOffset; offsetY < bitmap.Height - filterOffset - 1; offsetY++)
        {
            for (int offsetX = filterOffset; offsetX < bitmap.Width - filterOffset - 1; offsetX++)
            {
                blueX = greenX = redX = 0;
                blueY = greenY = redY = 0;
                blueTotal = greenTotal = redTotal = 0.0;
                byteOffset = offsetY *
                             data.Stride +
                             offsetX * 4;

                for (int filterY = -filterOffset; filterY <= filterOffset; filterY++)
                {
                    for (int filterX = -filterOffset; filterX <= filterOffset; filterX++)
                    {
                        calcOffset = byteOffset + filterX * 4 + filterY * data.Stride;

                        blueX += (double)pixelBuffer[calcOffset] * xFilterMatrix[filterY + filterOffset, filterX + filterOffset];
                        greenX += (double)pixelBuffer[calcOffset + 1] * xFilterMatrix[filterY + filterOffset, filterX + filterOffset];
                        redX += (double)pixelBuffer[calcOffset + 2] * xFilterMatrix[filterY + filterOffset, filterX + filterOffset];
                        if (yFilterMatrix != null)
                        {
                            blueY += (double)pixelBuffer[calcOffset] * yFilterMatrix[filterY + filterOffset, filterX + filterOffset];
                            greenY += (double)pixelBuffer[calcOffset + 1] * yFilterMatrix[filterY + filterOffset, filterX + filterOffset];
                            redY += (double)pixelBuffer[calcOffset + 2] * yFilterMatrix[filterY + filterOffset, filterX + filterOffset];
                        }
                    }
                }
                if (yFilterMatrix != null)
                {
                    blueTotal = Math.Sqrt(blueX * blueX + blueY * blueY);
                    greenTotal = Math.Sqrt(greenX * greenX + greenY * greenY);
                    redTotal = Math.Sqrt(redX * redX + redY * redY);
                }
                else
                {
                    blueTotal = bias * blueX;
                    greenTotal = bias * greenX;
                    redTotal = bias * redX;
                }


                if (blueTotal > 255)
                    blueTotal = 255;
                else if (blueTotal < 0)
                    blueTotal = 0;

                if (greenTotal > 255)
                    greenTotal = 255;
                else if (greenTotal < 0)
                    greenTotal = 0;

                if (redTotal > 255)
                    redTotal = 255;
                else if (redTotal < 0)
                    redTotal = 0;

                resultBuffer[byteOffset] = (byte)(blueTotal);
                resultBuffer[byteOffset + 1] = (byte)(greenTotal);
                resultBuffer[byteOffset + 2] = (byte)(redTotal);
                resultBuffer[byteOffset + 3] = 255;
            }
        }

        Bitmap resultBitmap = new Bitmap(bitmap.Width,
                                         bitmap.Height);

        BitmapData data2 = resultBitmap.LockBits(new Rectangle(0, 0, resultBitmap.Width, resultBitmap.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppRgb);

        Marshal.Copy(resultBuffer, 0, data2.Scan0,
                                   resultBuffer.Length);
        resultBitmap.UnlockBits(data2);

        return resultBitmap;
    }


    public static double GetDeviation(List<byte[]> input, byte[] currentPixel)
    {
        List<int> result = new List<int>();
        foreach (var item in input)
        {
            result.Add(item[0] + item[1] + item[2]);
        }

        int min = result.Min();
        int max = result.Max();
        int mean = (max + min) / 2;
        return Math.Sqrt((Math.Pow((double)(currentPixel[0] + currentPixel[1] + currentPixel[2] - mean), 2) + Math.Pow((double)(min - mean), 2) + Math.Pow((double)(max - mean), 2)) / 2);
    }
    public static byte[] GetAvarage(List<byte[]> input)
    {
        int allR = 0;
        int allG = 0;
        int allB = 0;
        foreach (var item in input)
        {
            allR += item[0];
            allG += item[1];
            allB += item[2];
        }

        return new byte[] { (byte)(allR / input.Count), (byte)(allG / input.Count), (byte)(allB / input.Count) };
    }
    //public static Bitmap Kuvahara(Bitmap bitmap, int pixelSize)
    //{
    //    BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
    //    byte[] vs = new byte[data.Height * data.Stride];
    //    Marshal.Copy(data.Scan0, vs, 0, vs.Length);

    //    for (int i = pixelSize; i < bitmap.Height; i += pixelSize)
    //    {
    //        for (int j = pixelSize * 3; j < bitmap.Width * 3; j += pixelSize * 3)
    //        {
    //            if ((i + pixelSize / 2) * bitmap.Width * 3 + j + pixelSize * 3 / 2 + 2 >= vs.Length)
    //                break;
    //            List<byte[]> quadrant1 = new();
    //            List<byte[]> quadrant2 = new();
    //            List<byte[]> quadrant3 = new();
    //            List<byte[]> quadrant4 = new();
    //            for (int k = i - pixelSize; k <= i + pixelSize; k++)
    //            {
    //                for (int l = j - pixelSize * 3; l <= j + pixelSize * 3; l += 3)
    //                {
    //                    if (k * bitmap.Width * 3 + l + 2 > vs.Length)
    //                        break;
    //                    //mogłem coś tutaj zepsuć
    //                    if (k <= i && l <= j)
    //                        quadrant1.Add(new byte[] { vs[k * bitmap.Width * 3 + l], vs[k * bitmap.Width * 3 + l + 1], vs[k * bitmap.Width * 3 + l + 2] });
    //                    else if (k <= i && l >= j)
    //                        quadrant2.Add(new byte[] { vs[k * bitmap.Width * 3 + l], vs[k * bitmap.Width * 3 + l + 1], vs[k * bitmap.Width * 3 + l + 2] });
    //                    else if (k >= i && l <= j)
    //                        quadrant3.Add(new byte[] { vs[k * bitmap.Width * 3 + l], vs[k * bitmap.Width * 3 + l + 1], vs[k * bitmap.Width * 3 + l + 2] });
    //                    else if (k >= i && l >= j)
    //                        quadrant4.Add(new byte[] { vs[k * bitmap.Width * 3 + l], vs[k * bitmap.Width * 3 + l + 1], vs[k * bitmap.Width * 3 + l + 2] });

    //                }
    //            }

    //            List<double> stds = new List<double>();
    //            stds.Add(GetDeviation(quadrant1, new byte[] { vs[i * bitmap.Width * 3 + j], vs[i * bitmap.Width * 3 + j + 1], vs[i * bitmap.Width * 3 + j + 2] }));
    //            stds.Add(GetDeviation(quadrant2, new byte[] { vs[i * bitmap.Width * 3 + j], vs[i * bitmap.Width * 3 + j + 1], vs[i * bitmap.Width * 3 + j + 2] }));
    //            stds.Add(GetDeviation(quadrant3, new byte[] { vs[i * bitmap.Width * 3 + j], vs[i * bitmap.Width * 3 + j + 1], vs[i * bitmap.Width * 3 + j + 2] }));
    //            stds.Add(GetDeviation(quadrant4, new byte[] { vs[i * bitmap.Width * 3 + j], vs[i * bitmap.Width * 3 + j + 1], vs[i * bitmap.Width * 3 + j + 2] }));

    //            var index = stds.IndexOf(stds.Min());
    //            byte[] avrg = new byte[3];
    //            switch (index)
    //            {
    //                case 0:
    //                    avrg = GetAvarage(quadrant1);
    //                    vs[i * bitmap.Width * 3 + j] = avrg[0];
    //                    vs[i * bitmap.Width * 3 + j] = avrg[1];
    //                    vs[i * bitmap.Width * 3 + j] = avrg[2];
    //                    break;
    //                case 1:
    //                    avrg = GetAvarage(quadrant2);
    //                    vs[i * bitmap.Width * 3 + j] = avrg[0];
    //                    vs[i * bitmap.Width * 3 + j] = avrg[1];
    //                    vs[i * bitmap.Width * 3 + j] = avrg[2];
    //                    break;
    //                case 2:
    //                    avrg = GetAvarage(quadrant3);
    //                    vs[i * bitmap.Width * 3 + j] = avrg[0];
    //                    vs[i * bitmap.Width * 3 + j] = avrg[1];
    //                    vs[i * bitmap.Width * 3 + j] = avrg[2];
    //                    break;
    //                case 3:
    //                    avrg = GetAvarage(quadrant4);
    //                    vs[i * bitmap.Width * 3 + j] = avrg[0];
    //                    vs[i * bitmap.Width * 3 + j] = avrg[1];
    //                    vs[i * bitmap.Width * 3 + j] = avrg[2];
    //                    break;
    //            }
    //        }
    //    }
    //    Marshal.Copy(vs, 0, data.Scan0, vs.Length);
    //    bitmap.UnlockBits(data);

    //    return bitmap;
    //}

    public static Bitmap Kuvahara(Bitmap bitmap, int pixelSize)
    {
        BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
        byte[] vs = new byte[data.Height * data.Stride];
        Marshal.Copy(data.Scan0, vs, 0, vs.Length);
        byte[] vs1 = new byte[vs.Length];
        for (int y = 0; y < data.Height; ++y)
            for (int x = 0; x < data.Stride; x += 3)
            {
                List<byte> upLeft = new List<byte>();
                List<byte> upRight = new List<byte>();
                List<byte> downLeft = new List<byte>();
                List<byte> downRight = new List<byte>();
                for (int z = y - pixelSize; z <= y + pixelSize; ++z)
                {
                    if (z >= 0 && z < data.Height)
                    {
                        for (int i = x - pixelSize * 3; i <= x + pixelSize * 3; i += 3)
                        {
                            if (i >= 0 && i < data.Stride)
                            {
                                if (z <= y)
                                {
                                    if (i <= x)
                                    {
                                        upLeft.Add(vs[z * data.Stride + i]);
                                        upLeft.Add(vs[z * data.Stride + i + 1]);
                                        upLeft.Add(vs[z * data.Stride + i + 2]);
                                    }
                                    if (i >= x)
                                    {
                                        upRight.Add(vs[z * data.Stride + i]);
                                        upRight.Add(vs[z * data.Stride + i + 1]);
                                        upRight.Add(vs[z * data.Stride + i + 2]);
                                    }
                                }
                                if (z >= y)
                                {
                                    if (i <= x)
                                    {
                                        downLeft.Add(vs[z * data.Stride + i]);
                                        downLeft.Add(vs[z * data.Stride + i + 1]);
                                        downLeft.Add(vs[z * data.Stride + i + 2]);
                                    }
                                    if (i >= x)
                                    {
                                        downRight.Add(vs[z * data.Stride + i]);
                                        downRight.Add(vs[z * data.Stride + i + 1]);
                                        downRight.Add(vs[z * data.Stride + i + 2]);
                                    }
                                }

                            }
                        }
                        
                    }
                    
                }
                double[] deviations = new double[4];
                deviations[0] = getDeviation(upLeft);
                deviations[1] = getDeviation(upRight);
                deviations[2] = getDeviation(downLeft);
                deviations[3] = getDeviation(downRight);
                byte[] avgR = new byte[4];
                byte[] avgG = new byte[4];
                byte[] avgB = new byte[4];
                int sumR = 0, sumG = 0, sumB = 0;
                int xd = 0;

                for (int i = 0; i < upLeft.Count; i += 3)
                {
                    sumR += upLeft[i];
                    sumG += upLeft[i + 1];
                    sumB += upLeft[i + 2];
                }
                avgR[0] = (byte)(sumR / (upLeft.Count / 3));
                avgG[0] = (byte)(sumG / (upLeft.Count / 3));
                avgB[0] = (byte)(sumB / (upLeft.Count / 3));
                sumR = sumG = sumB = 0;

                for (int i = 0; i < upRight.Count; i += 3)
                {
                    sumR += upRight[i];
                    sumG += upRight[i + 1];
                    sumB += upRight[i + 2];
                }
                avgR[1] = (byte)(sumR / (upRight.Count / 3));
                avgG[1] = (byte)(sumG / (upRight.Count / 3));
                avgB[1] = (byte)(sumB / (upRight.Count / 3));
                sumR = sumG = sumB = 0;

                for (int i = 0; i < downLeft.Count; i += 3)
                {
                    sumR += downLeft[i];
                    sumG += downLeft[i + 1];
                    sumB += downLeft[i + 2];
                }
                avgR[2] = (byte)(sumR / (downLeft.Count / 3));
                avgG[2] = (byte)(sumG / (downLeft.Count / 3));
                avgB[2] = (byte)(sumB / (downLeft.Count / 3));
                sumR = sumG = sumB = 0;

                for (int i = 0; i < downRight.Count; i += 3)
                {
                    sumR += downRight[i];
                    sumG += downRight[i + 1];
                    sumB += downRight[i + 2];
                }
                avgR[3] = (byte)(sumR / (downRight.Count / 3));
                avgG[3] = (byte)(sumG / (downRight.Count / 3));
                avgB[3] = (byte)(sumB / (downRight.Count / 3));

                int minVal = Array.IndexOf(deviations, deviations.Min());
                vs1[y * data.Stride + x] = avgR[minVal];
                vs1[y * data.Stride + x+1] = avgG[minVal];
                vs1[y * data.Stride + x+2] = avgB[minVal];
            }

        Marshal.Copy(vs1, 0, data.Scan0, vs1.Length);
        bitmap.UnlockBits(data);

        return bitmap;
    }

    public static double getDeviation(List<byte> bruh)
    {
        double result = 0;

        if (bruh.Any())
        {
            double avg = 0;
            foreach(var b in bruh)
            {
                avg += b;
            }
            avg /= bruh.Count;
            double sum = bruh.Sum(d => Math.Pow(d - avg, 2));
            result = Math.Sqrt((sum) / bruh.Count());
        }
        return result;
    }

    public static Bitmap MinRGB(Bitmap bitmap)
    {
        BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
        byte[] vs = new byte[data.Height * data.Stride];
        Marshal.Copy(data.Scan0, vs, 0, vs.Length);

        for (int i = 0; i < bitmap.Height; i += 1)
        {
            for (int j = 0; j < bitmap.Width * 3; j += 3)
            {
                byte R = vs[i * bitmap.Width * 3 + j];
                byte G = vs[i * bitmap.Width * 3 + j + 1];
                byte B = vs[i * bitmap.Width * 3 + j + 2];
                var min = Math.Min(R, Math.Min(G, B));
                if (R != min)
                    R = 0; 
                if (G != min)
                    G = 0; 
                if (B != min)
                    B = 0;
                vs[i * bitmap.Width * 3 + j] = R;
                vs[i * bitmap.Width * 3 + j + 1] = G;
                vs[i * bitmap.Width * 3 + j + 2] = B;
            }
        }
        Marshal.Copy(vs, 0, data.Scan0, vs.Length);
        bitmap.UnlockBits(data);

        return bitmap;
    }

}