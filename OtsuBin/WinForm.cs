using System;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace testtest
{
    public partial class BinForm : Form
    {
        public BinForm()
        {
            InitializeComponent();
        }

        string imagefilterof = "Image Files(*.BMP;*.JPG;*.GIF)|*.BMP;*.JPG;*.GIF";
        string imagefiltersf = "*.BMP|*.BMP|*.JPG|*.JPG";
        Bitmap GrayScaleImg;
        Bitmap DefaultImg;
        Bitmap BinImg;
        byte BinThreshold = 0;

        public static byte[,] BitmapToGrayScaleByte(Bitmap Img) // Метод для перевода изображения в байтовый массив
        {
            BitmapData bitmap_data = new BitmapData();
            bitmap_data = Img.LockBits(new System.Drawing.Rectangle(0, 0, Img.Width, Img.Height),
                                        ImageLockMode.ReadOnly, Img.PixelFormat);
            int pixelsize = System.Drawing.Image.GetPixelFormatSize(bitmap_data.PixelFormat) / 8;

            IntPtr pointer = bitmap_data.Scan0;
            int nbytes = bitmap_data.Height * bitmap_data.Stride;
            byte[] imagebytes = new byte[nbytes];
            System.Runtime.InteropServices.Marshal.Copy(pointer, imagebytes, 0, nbytes);

            double red;
            double green;
            double blue;
            byte gray;

            int helpint = 0;
            int addint = 0;
            if ((double)bitmap_data.Stride / (double)pixelsize != (double)bitmap_data.Width) // классный грязный хак!(изображение не "наклоняется")
            {
                if ((double)bitmap_data.Stride / (double)pixelsize - (double)bitmap_data.Width == 1)
                {
                    helpint = 1;
                    addint = 1;
                }
                else
                {
                    if ((double)bitmap_data.Stride / (double)pixelsize - (double)(bitmap_data.Width) > 0.6)
                        addint = 2;
                    if ((double)bitmap_data.Stride / (double)pixelsize - (double)(bitmap_data.Width) < 0.6)
                        addint = 3;
                }
            }
            var _grayscale_array = new byte[bitmap_data.Height, bitmap_data.Width + addint];

            if (pixelsize >= 3)
            {
                for (int I = 0; I < bitmap_data.Height; I++)
                {
                    for (int J = 0; J < bitmap_data.Width + helpint; J++)
                    {
                        int position = (I * bitmap_data.Stride) + (J * pixelsize);
                        blue = imagebytes[position];
                        green = imagebytes[position + 1];
                        red = imagebytes[position + 2];
                        gray = (byte)Math.Floor(0.299 * red + 0.587 * green + 0.114 * blue);
                        _grayscale_array[I, J] = gray;
                    }
                }
            }

            Img.UnlockBits(bitmap_data);


            return _grayscale_array;
        }


        public static Bitmap ByteToGrayBitmap(byte[] rawBytes, int width, int height) // метод для перевода байтового массива в серое изображение
        {
            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
            BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height),
                                            ImageLockMode.WriteOnly, bitmap.PixelFormat);

            Marshal.Copy(rawBytes, 0, bitmapData.Scan0, rawBytes.Length);
            bitmap.UnlockBits(bitmapData);

            var pal = bitmap.Palette;
            for (int i = 0; i < 256; i++) pal.Entries[i] = Color.FromArgb(i, i, i);
            bitmap.Palette = pal;


            return bitmap;
        }

        public static byte Otsu(byte[] image)
        {
            int[] hist = new int[256];
            for (int i = 0; i < image.Length; i++)
                hist[image[i]]++;
            int m = 0;
            int n = 0;
            for (int t = 0; t < 256; t++)
            {
                m += t * hist[t];
                n += hist[t];
            }

            float maxSigma = -1;
            int threshold = 0;
            int alpha1 = 0;
            int beta1 = 0;

            for (int t = 0; t < 256; t++)
            {
                alpha1 += t * hist[t];
                beta1 += hist[t];
                float w1 = (float)beta1 / n;
                float a = (float)alpha1 / beta1 - (float)(m - alpha1) / (n - beta1);
                float sigma = w1 * (1 - w1) * a * a;
                if (sigma > maxSigma)
                {
                    maxSigma = sigma;
                    threshold = t;
                }
            }


            return (byte)threshold;
        }

        public static byte[] Binarization(byte[] Img, byte Threshold)
        {
            for (int i = 0; i < Img.Length; i++)
                Img[i] = Img[i] > Threshold ? (byte)255 : (byte)0;
            return Img;
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog of = new OpenFileDialog();
            of.Filter = imagefilterof;
            if (of.ShowDialog() != DialogResult.Cancel)
            {
                DateTime start = DateTime.Now;
                DefaultImg = new Bitmap(of.FileName);
                pictureBox1.Image = DefaultImg;


                var _grayscale_array = BitmapToGrayScaleByte(DefaultImg);


                byte[] result = new byte[_grayscale_array.Length];
                Buffer.BlockCopy(_grayscale_array, 0, result, 0, _grayscale_array.Length);

                DateTime t1 = DateTime.Now;
                GrayScaleImg = ByteToGrayBitmap(result, DefaultImg.Width, DefaultImg.Height);
                pictureBox2.Image = GrayScaleImg;
                DateTime t2 = DateTime.Now;

                BinThreshold = Otsu(result);
                result = Binarization(result, BinThreshold);
                BinImg = ByteToGrayBitmap(result, DefaultImg.Width, DefaultImg.Height);
                pictureBox3.Image = BinImg;

                TimeSpan worktime = DateTime.Now - start - (t2 - t1);

                toolStripStatusLabel1.Text = "Binarization threshold = " + BinThreshold + ". Done for " + worktime.ToString();


                saveToolStripMenuItem.Enabled = true;
                grayscaleToolStripMenuItem.Enabled = true;
                binaryToolStripMenuItem.Enabled = true;
            }
        }

        private void grayscaleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog sf = new SaveFileDialog();
            sf.Filter = imagefiltersf;
            if (sf.ShowDialog() != DialogResult.Cancel)
                pictureBox2.Image.Save(sf.FileName);
        }

        private void binaryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog sf = new SaveFileDialog();
            sf.Filter = imagefiltersf;
            if (sf.ShowDialog() != DialogResult.Cancel)
                pictureBox3.Image.Save(sf.FileName);
        }
    }
}
