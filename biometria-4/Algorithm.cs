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
        byte[] vs = new byte[data.Height* data.Stride];
        Marshal.Copy(data.Scan0, vs, 0, vs.Length);

        for(int i=0; i< bitmap.Height; i += pixelSize)
        {
            for (int j = 0; j < bitmap.Width*3; j += pixelSize*3)
            {
                if ((i + pixelSize / 2) * bitmap.Width * 3 + j + pixelSize*3 / 2 + 2 >= vs.Length)
                    break;
                byte middlePixelR = vs[(i + pixelSize / 2) * bitmap.Width * 3 + j + pixelSize*3 / 2];
                byte middlePixelG = vs[(i + pixelSize / 2) * bitmap.Width * 3 + j + pixelSize*3 / 2+1];
                byte middlePixelB = vs[(i + pixelSize / 2) * bitmap.Width * 3 + j + pixelSize*3 / 2+2];
                for(int k = i; k<=i+pixelSize; k++)
                {
                    for (int l = j; l <= j + pixelSize*3; l+=3)
                    {
                        if (k * bitmap.Width * 3 + l + 2 >= vs.Length)
                            break;
                        vs[k * bitmap.Width * 3 + l] = middlePixelR;
                        vs[k * bitmap.Width * 3 + l+1] = middlePixelG;
                        vs[k * bitmap.Width * 3 + l+2] = middlePixelB;
                    }
                }
            }
        }
        Marshal.Copy(vs, 0, data.Scan0, vs.Length);
        bitmap.UnlockBits(data);

        return bitmap;
    }

}
