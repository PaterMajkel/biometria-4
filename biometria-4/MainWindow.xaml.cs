﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace biometria_4
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Bitmap? sourceImage = null;
        Bitmap? imageToEdit = null;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void OpenFile(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.jpg;*.png)|*.jpg;*.png|All files (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                string fileName = openFileDialog.FileName;
                imageToEdit = this.sourceImage = new Bitmap($"{fileName}");
                SourceImage.Source = ImageSourceFromBitmap(this.sourceImage);
            }
        }
        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]

        public static extern bool DeleteObject([In] IntPtr hObject);

        public ImageSource ImageSourceFromBitmap(Bitmap bmp)
        {
            var handle = bmp.GetHbitmap();
            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally { DeleteObject(handle); }
        }

        private void Pixelation_Click(object sender, RoutedEventArgs e)
        {
            if (sourceImage == null)
            {
                MessageBox.Show("You haven't uploaded any files", "Image error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            Bitmap bitmap = new Bitmap(this.sourceImage.Width, this.sourceImage.Height);
            bitmap = (Bitmap)this.imageToEdit.Clone();
            FilteredImage.Source = ImageSourceFromBitmap(Algorithm.Pixelation(bitmap, Range.Value % 2 == 1 ? (int)Range.Value + 1 : (int)Range.Value));
        }
        private void Median_Click(object sender, RoutedEventArgs e)
        {
            if (sourceImage == null)
            {
                MessageBox.Show("You haven't uploaded any files", "Image error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            Bitmap bitmap = new Bitmap(this.sourceImage.Width, this.sourceImage.Height);
            bitmap = (Bitmap)this.imageToEdit.Clone();
            FilteredImage.Source = ImageSourceFromBitmap(Algorithm.Median(bitmap, Range.Value % 2 == 1 ? (int)Range.Value + 1 : (int)Range.Value));
        }
    }
}
