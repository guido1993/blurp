using LensBlurApp.Models;
using LensBlurApp.Resources;
using Microsoft.Phone.Info;
using Microsoft.Phone.Shell;
using Microsoft.Xna.Framework.Media;
using Nokia.Graphics.Imaging;
using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Windows.Storage.Streams;

namespace LensBlurApp.Pages
{
    public partial class EffectPage
    {
        private bool _processing;
        private bool _processingPending;
        private const LensBlurPredefinedKernelShape Shape = LensBlurPredefinedKernelShape.Circle;
        private ApplicationBarIconButton _saveButton;

        private bool Processing
        {
            get
            {
                return _processing;
            }

            set
            {
                if (_processing == value) return;
                _processing = value;

                ProgressBar.IsIndeterminate = _processing;
                ProgressBar.Visibility = _processing ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public EffectPage()
        {
            InitializeComponent();

            CreateButtons();

            SizeSlider.ValueChanged += SizeSlider_ValueChanged;
        }

        private void CreateButtons()
        {
            _saveButton = new ApplicationBarIconButton
            {
                Text = AppResources.EffectPage_SaveButton,
                IconUri = new Uri("Assets/Icons/Save.png", UriKind.Relative),
            };

            _saveButton.Click += SaveButton_Click;

            ApplicationBar.Buttons.Add(_saveButton);
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            AttemptSave();
        }

        private void SizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            AttemptUpdatePreviewAsync();

            Model.Saved = false;

            AdaptButtonsToState();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (Model.OriginalImage == null || Model.AnnotationsBitmap == null)
            {
                NavigationService.GoBack();
            }
            else
            {
                AdaptButtonsToState();

                AttemptUpdatePreviewAsync();
            }
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if (Processing && e.IsCancelable)
            {
                e.Cancel = true;
            }

            base.OnNavigatingFrom(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            PreviewImage.Source = null;
        }

        private void AdaptButtonsToState()
        {
            _saveButton.IsEnabled = !Model.Saved;
        }

        private async void AttemptUpdatePreviewAsync()
        {
            if (!Processing)
            {
                Processing = true;

                Model.OriginalImage.Position = 0;

                using (var source = new StreamImageSource(Model.OriginalImage))
                using (var segmenter = new InteractiveForegroundSegmenter(source))
                using (var annotationsSource = new BitmapImageSource(Model.AnnotationsBitmap))
                {
                    segmenter.Quality = 0.5;
                    segmenter.AnnotationsSource = annotationsSource;

                    var foregroundColor = Model.ForegroundBrush.Color;
                    var backgroundColor = Model.BackgroundBrush.Color;

                    segmenter.ForegroundColor = Windows.UI.Color.FromArgb(foregroundColor.A, foregroundColor.R, foregroundColor.G, foregroundColor.B);
                    segmenter.BackgroundColor = Windows.UI.Color.FromArgb(backgroundColor.A, backgroundColor.R, backgroundColor.G, backgroundColor.B);

                    do
                    {
                        _processingPending = false;

                        var previewBitmap = new WriteableBitmap((int)Model.AnnotationsBitmap.Dimensions.Width, (int)Model.AnnotationsBitmap.Dimensions.Height);

                        using (var effect = new LensBlurEffect(source, new LensBlurPredefinedKernel(Shape, (uint)SizeSlider.Value)))
                        using (var renderer = new WriteableBitmapRenderer(effect, previewBitmap))
                        {
                            effect.KernelMap = segmenter;

                            await renderer.RenderAsync();

                            PreviewImage.Source = previewBitmap;

                            previewBitmap.Invalidate();
                        }
                    }
                    while (_processingPending);
                }

                Processing = false;
            }
            else
            {
                _processingPending = true;
            }
        }

        private async void AttemptSave()
        {
            if (Processing) return;
            Processing = true;

            GC.Collect();

            var lowMemory = false;

            try
            {
                var result = (long)DeviceExtendedProperties.GetValue("ApplicationWorkingSetLimit");

                lowMemory = result / 1024 / 1024 < 300;
            }
            catch (ArgumentOutOfRangeException)
            {
            }

            IBuffer buffer;

            Model.OriginalImage.Position = 0;

            using (var source = new StreamImageSource(Model.OriginalImage))
            using (var segmenter = new InteractiveForegroundSegmenter(source))
            using (var annotationsSource = new BitmapImageSource(Model.AnnotationsBitmap))
            {
                segmenter.Quality = lowMemory ? 0.5 : 1;
                segmenter.AnnotationsSource = annotationsSource;

                var foregroundColor = Model.ForegroundBrush.Color;
                var backgroundColor = Model.BackgroundBrush.Color;

                segmenter.ForegroundColor = Windows.UI.Color.FromArgb(foregroundColor.A, foregroundColor.R, foregroundColor.G, foregroundColor.B);
                segmenter.BackgroundColor = Windows.UI.Color.FromArgb(backgroundColor.A, backgroundColor.R, backgroundColor.G, backgroundColor.B);

                using (var effect = new LensBlurEffect(source, new LensBlurPredefinedKernel(Shape, (uint)SizeSlider.Value)))
                using (var renderer = new JpegRenderer(effect))
                {
                    effect.KernelMap = segmenter;

                    buffer = await renderer.RenderAsync();
                }
            }

            using (var library = new MediaLibrary())
            using (var stream = buffer.AsStream())
            {
                library.SavePicture("lensblur_" + DateTime.Now.Ticks, stream);

                Model.Saved = true;

                AdaptButtonsToState();
            }

            Processing = false;
        }
    }
}