using Nokia.Graphics.Imaging;
using System.IO;
using System.Windows.Media;

namespace LensBlurApp.Models
{
    public class Model
    {
        private static Stream _originalImageStream;
        private static Bitmap _annotationsBitmap;

        public static readonly SolidColorBrush ForegroundBrush = new SolidColorBrush(Colors.Red);
        public static readonly SolidColorBrush BackgroundBrush = new SolidColorBrush(Colors.Blue);

        public static Stream OriginalImage
        {
            get
            {
                return _originalImageStream;
            }

            set
            {
                if (_originalImageStream == value) return;
                _originalImageStream?.Close();

                _originalImageStream = value;
            }
        }

        public static Bitmap AnnotationsBitmap
        {
            get
            {
                return _annotationsBitmap;
            }

            set
            {
                if (_annotationsBitmap == value) return;
                _annotationsBitmap?.Dispose();

                _annotationsBitmap = value;
            }
        }

        public static LensBlurPredefinedKernelShape KernelShape { get; set; }
        public static double KernelSize { get; set; }
        public static bool Saved { get; set; }
    }
}
