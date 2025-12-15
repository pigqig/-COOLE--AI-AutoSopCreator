using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace AutoSopCreator
{
    public static class ImageHelper
    {
        public static Bitmap CropImage(Bitmap original, Rectangle cropArea)
        {
            Bitmap target = new Bitmap(cropArea.Width, cropArea.Height);
            using (Graphics g = Graphics.FromImage(target))
            {
                g.DrawImage(original, new Rectangle(0, 0, target.Width, target.Height),
                            cropArea,
                            GraphicsUnit.Pixel);
            }
            return target;
        }

        public static string ToBase64(Bitmap bitmap, long quality = 75L)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                ImageCodecInfo jpgEncoder = GetEncoder(ImageFormat.Jpeg);
                System.Drawing.Imaging.Encoder myEncoder = System.Drawing.Imaging.Encoder.Quality;
                EncoderParameters myEncoderParameters = new EncoderParameters(1);
                EncoderParameter myEncoderParameter = new EncoderParameter(myEncoder, quality);
                myEncoderParameters.Param[0] = myEncoderParameter;

                bitmap.Save(ms, jpgEncoder, myEncoderParameters);
                
                byte[] byteImage = ms.ToArray();
                return Convert.ToBase64String(byteImage);
            }
        }

        public static MemoryStream ToCompressedStream(Bitmap bitmap, long quality = 75L)
        {
            MemoryStream ms = new MemoryStream();
            ImageCodecInfo jpgEncoder = GetEncoder(ImageFormat.Jpeg);
            System.Drawing.Imaging.Encoder myEncoder = System.Drawing.Imaging.Encoder.Quality;
            EncoderParameters myEncoderParameters = new EncoderParameters(1);
            EncoderParameter myEncoderParameter = new EncoderParameter(myEncoder, quality);
            myEncoderParameters.Param[0] = myEncoderParameter;

            bitmap.Save(ms, jpgEncoder, myEncoderParameters);
            ms.Seek(0, SeekOrigin.Begin); 
            return ms;
        }

        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            return codecs.FirstOrDefault(codec => codec.FormatID == format.Guid);
        }
    }
}