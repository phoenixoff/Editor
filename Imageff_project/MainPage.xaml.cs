﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Input;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using Windows.UI.ViewManagement;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using Windows.Storage.Streams;
using RemedyPic.RemedyClasses;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.UI.ApplicationSettings;
using Windows.Media.Capture;
using Windows.System.UserProfile;
using System.Diagnostics;
using System.Text.RegularExpressions;
using RemedyPic.UserControls;
using RemedyPic.UserControls.Popups;

#region Namespace RemedyPic
namespace RemedyPic
{
    #region MainPage class
    public sealed partial class MainPage
    {


        #region Variables

        // Those are all the global variables, that are used in MainPage.xaml.cs file.

        // Those are used for the import/export functions.
        public Configuration configFile = new Configuration();
        public List<string> effectsApplied = new List<string>();

		public static MainPage Current;

        // Undo Redo archive
        public List<byte[]> archive_data = new List<byte[]>();
        public int archive_current_index = -1;     // -1 because we don`t have saved pixel array

        public double widthHeightRatio = 0;
        public bool keepProportions = true;
        public bool calledByOther = false;

        // mruToken is used for LoadState and SaveState functions.
        public string mruToken = null;

        // Those store the corner positions of 
        // the canvas, used for croping.
        public double canvasStartX = 0.00;
        public double canvasStartY = 0.00;
        public double canvasEndX = 0.00;
        public double canvasEndY = 0.00;

        // This variable holds the current file that we are using.
        public StorageFile file;

        // String variables that hold the current applied changes to the image.
        public string appliedFilters = null, appliedColors = null,
                           appliedRotations = null, appliedFrame = null, appliedFrameColor = null;

        // We create two WriteableBitmap variables.
        // One for the original image and one for the small bitmaps.
        // They are used to display the image on the screen.
        public WriteableBitmap bitmapImage, exampleBitmap;

        // The streams are used to save the pixels as a Stream to the WriteableBitmap objects.
        public Stream bitmapStream, exampleStream;

        // This is set true when the user opens a picture.
        public bool pictureIsLoaded = false;

        // Colorize selected colors
        public bool redForColorize, greenForColorize, blueForColorize, yellowForColorize,
                         orangeForColorize, purpleForColorize, cyanForColorize, limeForColorize = false;

        // We create three RemedyImages.
        // One for the original displayed image, one for the small images and
        // one to hold the original loaded image so we can get back to it at any time
        public RemedyImage image = new RemedyImage();
        public RemedyImage imageOriginal = new RemedyImage();
        public RemedyImage uneditedImage = new RemedyImage();

        // We create two streams for two of the WriteableBitmap objects.
        public Stream uneditedStream;
        public WriteableBitmap uneditedBitmap;

        // The dictionary holds the history of all previous pointer locations. It is used by the crop function.
        Dictionary<uint, Point?> pointerPositionHistory = new Dictionary<uint, Point?>();

        // This bool variable checks if the user 
        // has made any changes and if he saved them.
        public bool Saved = true;
        public string PopupCalledBy = "";

		public MenuPopup Menu;
        public MainOptionsPanel Panel;
        public DisplayImage imageDisplayed;
        public RemedyColors ColorsPopup;
        #endregion

        public MainPage()
        {
            // This function is called when the page is loaded in the beginning.
            // We first initialize the interface, then drop the picture border out
            // so we can animate it later. 
            // Then the charms are registered for using later. 
            // After this, the events are generated for the image.
            // They are used later for the image panning and crop function.
            // Finally, we set the selected crop region width and height.
            this.InitializeComponent();
			
            Current = this;

            Menu = new MenuPopup();
            Panel = new MainOptionsPanel();

            double displayImageWidth = (Window.Current.Bounds.Width * (569.00 / 683.00)) * 0.90;
            double displayImageHeight = Window.Current.Bounds.Height * 0.90;

            imageDisplayed = new DisplayImage(displayImageWidth, displayImageHeight);

            MainMenuPanel.Children.Add(Menu);
            PanelStack.Children.Add(Panel);
            ImageStack.Children.Add(imageDisplayed);

            ColorsPopup = new RemedyColors();

            setPopupsHeight();

            SmallPopups.Children.Add(ColorsPopup);

            RegisterCharms();
        }

        #region Charms
        public void RegisterCharms()
        {
            // If the user chooses the charm, we register the share charm 
            // and the settings charm so we can get all the available apps for sharing.
            DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested += new TypedEventHandler<DataTransferManager,
                DataRequestedEventArgs>(this.ShareImageHandler);
            SettingsPane.GetForCurrentView().CommandsRequested += OnSettingsPaneCommandRequested;
        }

		public void ShowPopup(string Popup)
		{
			ColorsPopup.Popup.IsOpen = true;
		}

        public void OnSettingsPaneCommandRequested(SettingsPane sender, SettingsPaneCommandsRequestedEventArgs args)
        {
            // We add the Feedback settings to the Settings charm.
            args.Request.ApplicationCommands.Add(new SettingsCommand("commandID",
                                                                     "Feedback", FeedbackPopup));
        }

        public void FeedbackPopup(IUICommand command)
        {
            // This event occures when the user clicks on the Feedback in the settings charm.
            Feedback.IsOpen = true;
        }

        public async void ShareImageHandler(DataTransferManager sender,
            DataRequestedEventArgs e)
        {
            // This handles the Share charm.

            if (!pictureIsLoaded)
            {
                // First we check if the user has loaded an image. If not, warn him.
                e.Request.FailWithDisplayText("Load an image and try sharing again! :)");
            }
            else
            {
                // If the user has loaded the image, we set the title, the description
                // and set RemedyPic as application name.
                DataRequest request = e.Request;
                request.Data.Properties.Title = "RemedyPic";
                request.Data.Properties.Description = "Share your image.";
                request.Data.Properties.ApplicationName = "RemedyPic";

                // Because we are making async calls in the DataRequested event handler,
                // we need to get the deferral first.
                DataRequestDeferral deferral = request.GetDeferral();

                // Make sure we always call Complete on the deferral.
                try
                {
                    // We save the current edited image to a temporary file in the application local folder on the current machine.
                    // This way, we can add more share handlers and use the Mail Share option.
                    using (IRandomAccessStream stream = new InMemoryRandomAccessStream())
                    {
                        Stream pixelStream = bitmapImage.PixelBuffer.AsStream();
                        byte[] pixels = new byte[pixelStream.Length];
                        await pixelStream.ReadAsync(pixels, 0, pixels.Length);

                        BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);
                        encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, (uint)bitmapImage.PixelWidth, (uint)bitmapImage.PixelHeight, 96.0, 96.0, pixels);

                        List<IStorageItem> imageItems = new List<IStorageItem>();
                        await SaveFile(false);
                        imageItems.Add(file);
                        request.Data.SetStorageItems(imageItems);
                        RandomAccessStreamReference imageStreamRef = RandomAccessStreamReference.CreateFromFile(file);
                        request.Data.Properties.Thumbnail = imageStreamRef;
                        request.Data.SetBitmap(imageStreamRef);

                        await encoder.FlushAsync();
                    }
                }
                finally
                {
                    deferral.Complete();
                }
            }
        }
        #endregion

        #region LoadState
        protected async override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
            // This loads information if the app had quit unexpected.
            if (pageState != null && pageState.ContainsKey("mruToken"))
            {
                object value = null;
                if (pageState.TryGetValue("mruToken", out value))
                {
                    if (value != null)
                    {
                        mruToken = value.ToString();

                        // Open the file via the token that you stored when adding this file into the MRU list.
                        Windows.Storage.StorageFile file =
                            await Windows.Storage.AccessCache.StorageApplicationPermissions.MostRecentlyUsedList.GetFileAsync(mruToken);

                        if (file != null)
                        {
                            // Open a stream for the selected file.
                            Windows.Storage.Streams.IRandomAccessStream fileStream =
                                await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
                            bitmapImage.SetSource(fileStream);
                            imageDisplayed.displayImage.Source = bitmapImage;

                            // Set the data context for the page.
                            this.DataContext = file;
                        }
                    }
                }
            }
        }
        #endregion

        #region Save State
        protected override void SaveState(Dictionary<String, Object> pageState)
        {
            // This saves information if the app quits unexpected.
            if (!String.IsNullOrEmpty(mruToken))
            {
                pageState["mruToken"] = mruToken;
            }
        }
        #endregion

        #region Functions, called when opening an image.


        public async void doAllCalculations()
        {
            // We make all the required calculations in order for
            // the app elements to appear and work normal.
            uneditedBitmap = bitmapImage;

            // Resize the original image for faster work.
            // Note that we only set the resize to the small images.
            // The original big image is left in original resolution.
            // After this we get the image pixels as streams and then
            // write the streams to the RemedyImage arrays.
            exampleBitmap = await ResizeImage(bitmapImage, (uint)(bitmapImage.PixelWidth / 5), (uint)(bitmapImage.PixelHeight / 5));
            exampleStream = exampleBitmap.PixelBuffer.AsStream();
            bitmapStream = bitmapImage.PixelBuffer.AsStream();
            uneditedStream = uneditedBitmap.PixelBuffer.AsStream();
            imageOriginal.srcPixels = new byte[(uint)bitmapStream.Length];
            image.srcPixels = new byte[(uint)exampleStream.Length];
            uneditedImage.srcPixels = new byte[(uint)uneditedStream.Length];
            await exampleStream.ReadAsync(image.srcPixels, 0, image.srcPixels.Length);
            await bitmapStream.ReadAsync(imageOriginal.srcPixels, 0, imageOriginal.srcPixels.Length);
            await uneditedStream.ReadAsync(uneditedImage.srcPixels, 0, uneditedImage.srcPixels.Length);

            // Reset all sliders
            ResetAllSliders();

            // Reset archive and archive index and add new image
            archive_data.Clear();
            archive_current_index = -1;
            ArchiveAddArray();

            // Clear array with effects
            effectsApplied.Clear();

            // Set the small WriteableBitmap objects to the three XAML Image objects.
            setElements(ColorsPopup.ColorsExamplePicture, exampleBitmap);
            setElements(RotationsExamplePicture, exampleBitmap);
            setElements(ExposureExamplePicture, exampleBitmap);

            // Make the images ready for work.
            prepareImage(exampleStream, exampleBitmap, image);
            setStream(exampleStream, exampleBitmap, image);
            prepareImage(uneditedStream, uneditedBitmap, uneditedImage);
            setStream(uneditedStream, uneditedBitmap, uneditedImage);
            prepareImage(bitmapStream, bitmapImage, imageOriginal);
            setStream(bitmapStream, bitmapImage, imageOriginal);

            Panel.ZoomStack.Visibility = Visibility.Visible;

            // set the small WriteableBitmap objects to the filter buttons.
            setFilterBitmaps();

            // Display the file name.
            setFileProperties(file);

            // Set the WriteableBitmap as source to the XAML Image object. This makes the picture appear on the screen.
            imageDisplayed.displayImage.Source = bitmapImage;
            imageDisplayed.AnimateInPicture.Begin();

            // We check the CheckBox that is required for the image to move by default.
            Panel.ImageMoving.IsChecked = true;

            // We set the imagePanel maximum height so the image not to go out of the screen
            imageDisplayed.displayImage.MaxWidth = imageDisplayed.imageBorder.ActualWidth * 0.90;
            imageDisplayed.displayImage.MaxHeight = imageDisplayed.imageBorder.ActualHeight * 0.90;



            widthHeightRatio = (double)bitmapImage.PixelWidth / (double)bitmapImage.PixelHeight;
            newWidth.Text = bitmapImage.PixelWidth.ToString();
            newHeight.Text = bitmapImage.PixelHeight.ToString();

            // Show the interface.
            showInterface();
        }


        public async void setExampleImage()
        {
            exampleBitmap = await ResizeImage(bitmapImage, (uint)(bitmapImage.PixelWidth / 5), (uint)(bitmapImage.PixelHeight / 5));
            exampleStream = exampleBitmap.PixelBuffer.AsStream();
            image.srcPixels = new byte[(uint)exampleStream.Length];
            await exampleStream.ReadAsync(image.srcPixels, 0, image.srcPixels.Length);
            setElements(ColorsPopup.ColorsExamplePicture, exampleBitmap);
            setElements(RotationsExamplePicture, exampleBitmap);
            setElements(ExposureExamplePicture, exampleBitmap);
            prepareImage(exampleStream, exampleBitmap, image);
            setStream(exampleStream, exampleBitmap, image);
        }

        public void ResetAllSliders()
        {
            ResetFilterMenuData();
            ResetColorMenuData();
            ResetExposureMenuData();
            ResetRotateMenuData();
            ResetColorizeMenuData();
            ResetFrameMenuData();
        }

        // Reset data of Filter menu
        public void ResetFilterMenuData()
        {
            appliedFilters = null;
            deselectFilters();
        }

        // Reset the data of Color menu
        public void ResetColorMenuData()
        {
            appliedColors = null;

            ColorsPopup.BlueColorSlider.Value = 0;
            ColorsPopup.GreenColorSlider.Value = 0;
            ColorsPopup.RedColorSlider.Value = 0;

            ColorsPopup.BlueContrastSlider.Value = 0;
            ColorsPopup.GreenContrastSlider.Value = 0;
            ColorsPopup.RedContrastSlider.Value = 0;
        }

        // Reset the slider values of Exposure Menu
        public void ResetExposureMenuData()
        {
            brightSlider.Value = 0;

            BlueGammaSlider.Value = 10;
            GreenGammaSlider.Value = 10;
            RedGammaSlider.Value = 10;
        }

        // Reset the data of Rotate menu
        public void ResetRotateMenuData()
        {
            appliedRotations = null;
        }

        // Reset the data of Colorize menu
        public void ResetColorizeMenuData()
        {
            redForColorize = greenForColorize = blueForColorize = yellowForColorize =
                             orangeForColorize = purpleForColorize = cyanForColorize =
                             limeForColorize = false;
            deselectColorizeGridItems();
        }

        // Reset the data of Frame menu
        public void ResetFrameMenuData()
        {
            appliedFrameColor = "black";
            BlackFrameColor.IsSelected = true;
            FrameWidthPercent.Value = 1;
            appliedFrame = null;
        }
        public void setPopupsHeight()
        {
            // We set the popups height to match the current machine's resolution
            Filters.Height = Window.Current.Bounds.Height;
            contentGrid.Height = Window.Current.Bounds.Height;
            SmallPopups.Height = Window.Current.Bounds.Height;
            ColorsPopup.Popup.Height = Window.Current.Bounds.Height;
            ColorsPopup.Height = Window.Current.Bounds.Height;
            Rotations.Height = Window.Current.Bounds.Height;
            ImageOptions.Height = Window.Current.Bounds.Height;
            Colorize.Height = Window.Current.Bounds.Height;
            Frames.Height = Window.Current.Bounds.Height;
            Histogram.Height = Window.Current.Bounds.Height;
            FeedbackGrid.Height = Window.Current.Bounds.Height;
            Exposure.Height = Window.Current.Bounds.Height;
            CustomFilter.Height = Window.Current.Bounds.Height;
            notSaved.Width = Window.Current.Bounds.Width;
            notSavedGrid.Width = Window.Current.Bounds.Width;
        }

        public void setElements(Windows.UI.Xaml.Controls.Image imageElement, WriteableBitmap source)
        {
            // We set the XAML Image object a bitmap as a source 
            // and then set the width and height to be proportional to the actual bitmap
            imageElement.Source = source;
            imageElement.Width = bitmapImage.PixelWidth / 4;
            imageElement.Height = bitmapImage.PixelHeight / 4;
        }

        public void showInterface()
        {
            // Called when the image is loaded.
            // It shows the interface.
            Panel.Zoom.Visibility = Visibility.Visible;
            Menu.menuPopup.IsOpen = true;
            Panel.UndoRedo.Visibility = Visibility.Visible;
        }
        #endregion

        #region Filter functions

        #region Invert Filter
        // Invert filter function
        public void OnInvertClick(object sender, RoutedEventArgs e)
        {
            if (pictureIsLoaded)
            {
                appliedFilters = "invert";
                prepareImage(exampleStream, exampleBitmap, image);
                int[,] coeff = new int[5, 5];
                int offset = 0, scale = 0;
                Invert_SetValues(ref coeff, ref offset, ref scale);

                CustomFilter custom_image = new CustomFilter(image.srcPixels, image.width, image.height, offset, scale, coeff);
                image.dstPixels = custom_image.Filter();
                setStream(exampleStream, exampleBitmap, image);
            }
        }

        // Set the matrix for invert filter
        public void Invert_SetValues(ref int[,] coeff, ref int offset, ref int scale)
        {
            coeff[2, 2] = -1;
            offset = 255;
            scale = 1;
        }
        #endregion

        #region B&W Filter
        public void OnBlackWhiteClick(object sender, RoutedEventArgs e)
        {
            // This occures when OnBlackWhiteButton is clicked
            if (pictureIsLoaded)
            {
                // First we prepare the image for filtrating, then we call the filter.
                // After that we save the new data to the current image,
                // reset all other highlighted buttons and make the B&W button selected
                appliedFilters = "blackwhite";
                prepareImage(exampleStream, exampleBitmap, image);
                image.BlackAndWhite(image.dstPixels, image.srcPixels);
                setStream(exampleStream, exampleBitmap, image);
            }
        }
        #endregion

        #region Emboss Filter
        // Emboss filter function
        public void OnEmbossClick(object sender, RoutedEventArgs e)
        {
            if (pictureIsLoaded)
            {
                appliedFilters = "emboss";
                prepareImage(exampleStream, exampleBitmap, image);
                int[,] coeff = new int[5, 5];
                int offset = 0, scale = 0;
                Emboss_SetValues(ref coeff, ref offset, ref scale);

                CustomFilter custom_image = new CustomFilter(image.srcPixels, image.width, image.height, offset, scale, coeff);
                image.dstPixels = custom_image.Filter();
                setStream(exampleStream, exampleBitmap, image);
            }
        }

        // Set the matrix for emboss filter
        public void Emboss_SetValues(ref int[,] coeff, ref int offset, ref int scale)
        {
            coeff[2, 2] = 1;
            coeff[3, 3] = -1;
            offset = 128;
            scale = 1;
        }
        #endregion

        #region Emboss 2 Filter
        // Emboss 2 filter function
        public void OnEmboss2Click(object sender, RoutedEventArgs e)
        {
            if (pictureIsLoaded)
            {
                appliedFilters = "emboss2";
                prepareImage(exampleStream, exampleBitmap, image);
                int[,] coeff = new int[5, 5];
                int offset = 0, scale = 0;
                Emboss2_SetValues(ref coeff, ref offset, ref scale);

                CustomFilter custom_image = new CustomFilter(image.srcPixels, image.width, image.height, offset, scale, coeff);
                image.dstPixels = custom_image.Filter();
                setStream(exampleStream, exampleBitmap, image);
            }
        }

        // Set the matrix for emboss2 filter
        public void Emboss2_SetValues(ref int[,] coeff, ref int offset, ref int scale)
        {
            coeff[2, 2] = 1;
            coeff[2, 1] = -1;
            coeff[1, 2] = -1;
            coeff[1, 1] = -2;
            coeff[2, 3] = 1;
            coeff[3, 2] = 1;
            coeff[4, 3] = 2;
            offset = 0;
            scale = 1;
        }
        #endregion

        #region Sharpen Filter
        // Sharpen filter function
        public void OnSharpenClick(object sender, RoutedEventArgs e)
        {
            if (pictureIsLoaded)
            {
                appliedFilters = "sharpen";
                prepareImage(exampleStream, exampleBitmap, image);
                int[,] coeff = new int[5, 5];
                int offset = 0, scale = 0;
                Sharpen_SetValues(ref coeff, ref offset, ref scale);

                CustomFilter custom_image = new CustomFilter(image.srcPixels, image.width, image.height, offset, scale, coeff);
                image.dstPixels = custom_image.Filter();

                setStream(exampleStream, exampleBitmap, image);
            }
        }

        // Set the matrix for sharpen filter
        public void Sharpen_SetValues(ref int[,] coeff, ref int offset, ref int scale)
        {
            coeff[2, 2] = 5;
            coeff[2, 3] = -1;
            coeff[2, 1] = -1;
            coeff[1, 2] = -1;
            coeff[3, 2] = -1;
            offset = 0;
            scale = 1;
        }
        #endregion

        #region Blur Filter
        // Blur filter function
        public void OnBlurClick(object sender, RoutedEventArgs e)
        {
            if (pictureIsLoaded)
            {
                appliedFilters = "blur";
                prepareImage(exampleStream, exampleBitmap, image);
                int[,] coeff = new int[5, 5];
                int offset = 0, scale = 0;
                Blur_SetValues(ref coeff, ref offset, ref scale);

                CustomFilter custom_image = new CustomFilter(image.srcPixels, image.width, image.height, offset, scale, coeff);
                image.dstPixels = custom_image.Filter();

                setStream(exampleStream, exampleBitmap, image);
            }
        }

        // Set the matrix for blur filter
        public void Blur_SetValues(ref int[,] coeff, ref int offset, ref int scale)
        {
            coeff[2, 2] = 3;
            coeff[0, 0] = coeff[1, 0] = coeff[2, 0] = coeff[3, 0] = coeff[4, 0] = 1;
            coeff[0, 1] = coeff[0, 2] = coeff[0, 3] = coeff[4, 1] = coeff[4, 2] = coeff[4, 3] = 1;
            coeff[0, 4] = coeff[1, 4] = coeff[2, 4] = coeff[3, 4] = coeff[4, 4] = 1;
            coeff[1, 1] = coeff[2, 1] = coeff[3, 1] = 2;
            coeff[1, 2] = coeff[3, 2] = 2;
            coeff[1, 3] = coeff[2, 3] = coeff[3, 3] = 2;
            offset = 0;
            scale = 35;
        }
        #endregion

        #region Blur2 Filter
        // Blur2 filter function
        public void OnBlur2Click(object sender, RoutedEventArgs e)
        {
            if (pictureIsLoaded)
            {
                appliedFilters = "blur2";
                prepareImage(exampleStream, exampleBitmap, image);
                int[,] coeff = new int[5, 5];
                int offset = 0, scale = 0;
                Blur2_SetValues(ref coeff, ref offset, ref scale);

                CustomFilter custom_image = new CustomFilter(image.srcPixels, image.width, image.height, offset, scale, coeff);
                image.dstPixels = custom_image.Filter();

                setStream(exampleStream, exampleBitmap, image);
            }
        }

        // Set the matrix for blur2 filter
        public void Blur2_SetValues(ref int[,] coeff, ref int offset, ref int scale)
        {
            coeff[2, 2] = 1;
            coeff[1, 1] = coeff[2, 1] = coeff[3, 1] = 1;
            coeff[1, 2] = coeff[3, 2] = 1;
            coeff[1, 3] = coeff[2, 3] = coeff[3, 3] = 1;
            offset = 0;
            scale = 9;
        }

        #endregion

        #region EdgeDetect Filter
        // EdgeDetect filter function
        public void OnEdgeDetectClick(object sender, RoutedEventArgs e)
        {
            if (pictureIsLoaded)
            {
                appliedFilters = "EdgeDetect";
                prepareImage(exampleStream, exampleBitmap, image);
                int[,] coeff = new int[5, 5];
                int offset = 0, scale = 0;
                EdgeDetect_SetValues(ref coeff, ref offset, ref scale);

                CustomFilter custom_image = new CustomFilter(image.srcPixels, image.width, image.height, offset, scale, coeff);
                image.dstPixels = custom_image.Filter();

                setStream(exampleStream, exampleBitmap, image);
            }
        }

        // Set the matrix for edgeDetect filter
        public void EdgeDetect_SetValues(ref int[,] coeff, ref int offset, ref int scale)
        {
            coeff[2, 2] = -4;
            coeff[1, 2] = coeff[2, 1] = coeff[2, 3] = coeff[3, 2] = 1;
            offset = 0;
            scale = 1;
        }
        #endregion

        #region EdgeEnhance Filter
        // EdgeEnhance filter function
        public void OnEdgeEnhanceClick(object sender, RoutedEventArgs e)
        {
            if (pictureIsLoaded)
            {
                appliedFilters = "EdgeEnhance";
                prepareImage(exampleStream, exampleBitmap, image);
                int[,] coeff = new int[5, 5];
                int offset = 0, scale = 0;
                EdgeEnhance_SetValues(ref coeff, ref offset, ref scale);

                CustomFilter custom_image = new CustomFilter(image.srcPixels, image.width, image.height, offset, scale, coeff);
                image.dstPixels = custom_image.Filter();

                setStream(exampleStream, exampleBitmap, image);
            }
        }

        // Set the matrix for edgeEnhance filter
        public void EdgeEnhance_SetValues(ref int[,] coeff, ref int offset, ref int scale)
        {
            coeff[2, 2] = 1;
            coeff[3, 0] = -1;
            offset = 0;
            scale = 1;
        }

        #endregion

        #endregion

        #region Save

        public async Task<bool> SaveFile(bool picker)
        {
            // Only execute if there is a picture that is loaded
            if (pictureIsLoaded)
            {
                file = null;
                ImageLoadingRing.IsActive = true;
                // If the picker variable is true, we call a FilePicker.
                // If it's not, we save a temporary file without notifying the user to the local directory of the app.
                if (picker == true)
                {
                    FileSavePicker savePicker = new FileSavePicker();
                    Saved = true;
                    // Set My Documents folder as suggested location if no past selected folder is available
                    savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

                    // Dropdown of file types the user can save the file as
                    savePicker.FileTypeChoices.Add("JPEG", new List<string>() { ".jpg" });
                    savePicker.FileTypeChoices.Add("PNG", new List<string>() { ".png" });
                    savePicker.FileTypeChoices.Add("Bitmap", new List<string>() { ".bmp" });

                    savePicker.SuggestedFileName = fileName.Text;

                    // Default file name if the user does not type one in or select a file to replace
                    file = await savePicker.PickSaveFileAsync();
                }
                else
                {
                    file = await ApplicationData.Current.LocalFolder.CreateFileAsync("temp.jpg", CreationCollisionOption.ReplaceExisting);
                }
                System.Guid fileType = BitmapEncoder.JpegEncoderId;

                // File is null if the user press Cancel without choosing file
                if (file != null)
                {
                    // Check the file type that the user had selected and set the BitmapEncoder to that type
                    switch (file.FileType)
                    {
                        case ".jpeg":
                        case ".jpg":
                            fileType = BitmapEncoder.JpegEncoderId;
                            break;
                        case ".png":
                            fileType = BitmapEncoder.PngEncoderId;
                            break;
                        case ".bmp":
                            fileType = BitmapEncoder.BmpEncoderId;
                            break;
                        default:
                            break;
                    }

                    using (IRandomAccessStream writeStream = await file.OpenAsync(FileAccessMode.ReadWrite))
                    {
                        BitmapEncoder encoder = await BitmapEncoder.CreateAsync(fileType, writeStream);
                        encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied,
                                                           (uint)bitmapImage.PixelWidth, (uint)bitmapImage.PixelHeight, 96.0, 96.0, imageOriginal.dstPixels);
                        // Flush all the data to the encoder(file)
                        await encoder.FlushAsync();
                    }
                }
            }
            ImageLoadingRing.IsActive = false;
            return true;
        }
        #endregion

        public void pauseTimer(int miliseconds)
        {
            // This pauses the calling function for N miliseconds.
            Stopwatch sw = new Stopwatch(); // sw cotructor
            sw.Start(); // starts the stopwatch
            for (int i = 0; ; i++)
            {
                if (i % 100000 == 0) // if in 100000th iteration (could be any other large number
                // depending on how often you want the time to be checked) 
                {
                    sw.Stop(); // stop the time measurement
                    if (sw.ElapsedMilliseconds > miliseconds) // check if desired period of time has elapsed
                    {
                        break; // if more than the given milliseconds have passed, stop looping and return
                        // to the existing code
                    }
                    else
                    {
                        sw.Start(); // if less than the given milliseconds have elapsed, continue looping
                        // and resume time measurement
                    }
                }
            }
        }

        void setFileProperties(Windows.Storage.StorageFile file)
        {
            // This sets the file name to the text box
            fileName.Text = file.Name;
            if (fileName.Text.Length > 20)
                fileName.FontSize = 55;
            if (fileName.Text.Length > 50)
                fileName.FontSize = 35;
            if (fileName.Text.Length < 15)
                fileName.FontSize = 85;
        }

        public void setStream(Stream givenStream, WriteableBitmap givenBitmap, RemedyImage givenImage)
        {
            // This sets the pixels to the bitmap
            // and makes the ApplyReset stackPanel of the current popup to appear.
            givenStream.Seek(0, SeekOrigin.Begin);
            givenStream.Write(givenImage.dstPixels, 0, givenImage.dstPixels.Length);
            givenBitmap.Invalidate();
            if (givenImage == image)
            {
                if (PopupFilters.IsOpen)
                    FilterApplyReset.Visibility = Visibility.Visible;
                else if (ColorsPopup.Popup.IsOpen)
                    ColorsPopup.ColorApplyReset.Visibility = Visibility.Visible;
                else if (PopupRotations.IsOpen)
                    RotateApplyReset.Visibility = Visibility.Visible;
                else if (PopupExposure.IsOpen)
                    ExposureApplyReset.Visibility = Visibility.Visible;
            }
        }

        public void prepareImage(Stream stream, WriteableBitmap bitmap, RemedyImage givenImage)
        {
            // This calculates the width and height of the bitmap image
            // and sets the Stream and the pixels byte array
            givenImage.width = (int)bitmap.PixelWidth;
            givenImage.height = (int)bitmap.PixelHeight;
            stream = bitmap.PixelBuffer.AsStream();
            givenImage.dstPixels = new byte[4 * bitmap.PixelWidth * bitmap.PixelHeight];
            givenImage.Reset();
        }

        #region Undo and Redo

        // Undo button click
        public void OnUndoClick(object sender, RoutedEventArgs e)
        {
            ImageLoadingRing.IsActive = true;

            if (archive_current_index > 0) // Check if there is no more for undo
            {
                archive_current_index--;
                if (effectsApplied.Count > 0 && (Regex.IsMatch(effectsApplied[archive_current_index], "Crop") || Regex.IsMatch(effectsApplied[archive_current_index], "Resize")))
                {                    
                    string[] sizes = effectsApplied[archive_current_index].Split(' ');
                    imageOriginal.width = Convert.ToInt32(sizes[1]);
                    imageOriginal.height = Convert.ToInt32(sizes[2]);
                    imageOriginal.srcPixels = (byte[])archive_data[archive_current_index].Clone();
                    imageOriginal.dstPixels = (byte[])archive_data[archive_current_index].Clone();
                    bitmapImage = new WriteableBitmap(imageOriginal.width, imageOriginal.height);
                    bitmapStream = bitmapImage.PixelBuffer.AsStream();
                }
                ArchiveSetNewImage();
                setExampleImage();
            }
            ImageLoadingRing.IsActive = false;
        }

        //Redo button click
        public void OnRedoClick(object sender, RoutedEventArgs e)
        {
            ImageLoadingRing.IsActive = true;

            if (archive_current_index < archive_data.Count - 1) // Check if there is array for redo
            {
                archive_current_index++;
                if (Regex.IsMatch(effectsApplied[archive_current_index - 1], "Crop") || Regex.IsMatch(effectsApplied[archive_current_index - 1], "Resize"))
                {
                    string[] sizes = effectsApplied[archive_current_index - 1].Split(' ');
                    imageOriginal.width = Convert.ToInt32(sizes[3]);
                    imageOriginal.height = Convert.ToInt32(sizes[4]);
                    imageOriginal.srcPixels = (byte[])archive_data[archive_current_index].Clone();
                    imageOriginal.dstPixels = (byte[])archive_data[archive_current_index].Clone();
                    bitmapImage = new WriteableBitmap(imageOriginal.width, imageOriginal.height);
                    bitmapStream = bitmapImage.PixelBuffer.AsStream();
                }                
                ArchiveSetNewImage();
            }
            ImageLoadingRing.IsActive = false;
        }

        public void ArchiveSetNewImage()
        {
            prepareImage(bitmapStream, bitmapImage, imageOriginal);
            imageOriginal.srcPixels = (byte[])archive_data[archive_current_index].Clone();
            imageOriginal.dstPixels = (byte[])archive_data[archive_current_index].Clone();
            setStream(bitmapStream, bitmapImage, imageOriginal);
            setExampleImage();
            setFilterBitmaps();
            imageDisplayed.displayImage.Source = bitmapImage;
        }

        // Add pixel array to the archive and increment current index of the archive
        public void ArchiveAddArray()
        {
            if (archive_current_index != -1 && archive_current_index != archive_data.Count - 1)
            {
                archive_data.RemoveRange(archive_current_index + 1, archive_data.Count - 1 - archive_current_index);
                effectsApplied.RemoveRange(archive_current_index, effectsApplied.Count - archive_current_index); // Here we don`t save the start image, so we have -1 index of archive_current_index
            }
            archive_data.Add((byte[])imageOriginal.srcPixels.Clone());
            archive_current_index++;
        }
        #endregion


        #region Resizing an image

        // This function resize the passed WriteableBitmap object with the passed width and height.
        // The passed width and height must be proportional of the original width and height( /2, /3, /4 ..).
        public async Task<WriteableBitmap> ResizeImage(WriteableBitmap baseWriteBitmap, uint width, uint height)
        {
            // Get the pixel buffer of the writable bitmap in bytes
            Stream stream = baseWriteBitmap.PixelBuffer.AsStream();
            byte[] pixels = new byte[(uint)stream.Length];
            await stream.ReadAsync(pixels, 0, pixels.Length);

            //Encoding the data of the PixelBuffer we have from the writable bitmap
            var inMemoryRandomStream = new InMemoryRandomAccessStream();
            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, inMemoryRandomStream);
            encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, (uint)baseWriteBitmap.PixelWidth, (uint)baseWriteBitmap.PixelHeight, 96, 96, pixels);
            await encoder.FlushAsync();

            // At this point we have an encoded image in inMemoryRandomStream
            // We apply the transform and decode
            var transform = new BitmapTransform
            {
                ScaledWidth = width,
                ScaledHeight = height,
            };
            inMemoryRandomStream.Seek(0);
            var decoder = await BitmapDecoder.CreateAsync(inMemoryRandomStream);
            var pixelData = await decoder.GetPixelDataAsync(
                            BitmapPixelFormat.Bgra8,
                            BitmapAlphaMode.Straight,
                            transform,
                            ExifOrientationMode.IgnoreExifOrientation,
                            ColorManagementMode.DoNotColorManage);

            // An array containing the decoded image data
            var sourceDecodedPixels = pixelData.DetachPixelData();

            // We encode the image buffer again:

            // Encoding data
            var inMemoryRandomStream2 = new InMemoryRandomAccessStream();
            var encoder2 = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, inMemoryRandomStream2);
            encoder2.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, width, height, 96, 96, sourceDecodedPixels);
            await encoder2.FlushAsync();
            inMemoryRandomStream2.Seek(0);

            // Finally the resized WritableBitmap
            var bitmap = new WriteableBitmap((int)width, (int)height);
            await bitmap.SetSourceAsync(inMemoryRandomStream2);
            return bitmap;
        }
        #endregion

        #region Rotate
        public async Task<WriteableBitmap> RotateImage(WriteableBitmap baseWriteBitmap, uint width, uint height, string position)
        {
            // Get the pixel buffer of the writable bitmap in bytes
            Stream stream = baseWriteBitmap.PixelBuffer.AsStream();
            byte[] pixels = new byte[(uint)stream.Length];
            await stream.ReadAsync(pixels, 0, pixels.Length);

            //Encoding the data of the PixelBuffer we have from the writable bitmap
            var inMemoryRandomStream = new InMemoryRandomAccessStream();
            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, inMemoryRandomStream);
            encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, (uint)baseWriteBitmap.PixelWidth, (uint)baseWriteBitmap.PixelHeight, 96, 96, pixels);
            await encoder.FlushAsync();

            // At this point we have an encoded image in inMemoryRandomStream
            // We apply the transform and decode

            BitmapRotation rotateTo = BitmapRotation.None;

            if (position == "right")
            {
                rotateTo = BitmapRotation.Clockwise90Degrees;
            }
            else if (position == "left")
            {
                rotateTo = BitmapRotation.Clockwise270Degrees;
            }

            var transform = new BitmapTransform
            {
                ScaledWidth = width,
                ScaledHeight = height,
                Rotation = rotateTo
            };
            inMemoryRandomStream.Seek(0);
            var decoder = await BitmapDecoder.CreateAsync(inMemoryRandomStream);
            var pixelData = await decoder.GetPixelDataAsync(
                            BitmapPixelFormat.Bgra8,
                            BitmapAlphaMode.Straight,
                            transform,
                            ExifOrientationMode.IgnoreExifOrientation,
                            ColorManagementMode.DoNotColorManage);

            // An array containing the decoded image data
            var sourceDecodedPixels = pixelData.DetachPixelData();

            // We encode the image buffer again:

            // Encoding data
            var inMemoryRandomStream2 = new InMemoryRandomAccessStream();
            var encoder2 = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, inMemoryRandomStream2);
            encoder2.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, height, width, 96, 96, sourceDecodedPixels);
            await encoder2.FlushAsync();
            inMemoryRandomStream2.Seek(0);

            // Finally the resized WritableBitmap
            var bitmap = new WriteableBitmap((int)width, (int)height);
            await bitmap.SetSourceAsync(inMemoryRandomStream2);
            return bitmap;
        }

        #endregion


        #region Filters
        // Change the image with black and white filter applied
        private void doBlackWhite(Stream stream, WriteableBitmap bitmap, RemedyImage givenImage)
        {
            prepareImage(stream, bitmap, givenImage);
            givenImage.BlackAndWhite(givenImage.dstPixels, givenImage.srcPixels);
            setStream(stream, bitmap, givenImage);
        }

        // Change the image with invert filter applied
        public void doInvert(Stream stream, WriteableBitmap bitmap, RemedyImage givenImage)
        {
            prepareImage(stream, bitmap, givenImage);
            int[,] coeff = new int[5, 5];
            int offset = 0, scale = 0;
            Invert_SetValues(ref coeff, ref offset, ref scale);
            CustomFilter custom_image = new CustomFilter(givenImage.srcPixels, givenImage.width, givenImage.height, offset, scale, coeff);
            givenImage.dstPixels = custom_image.Filter();
            setStream(stream, bitmap, givenImage);
        }

        // Change the image with emboss filter applied
        public void doEmboss(Stream stream, WriteableBitmap bitmap, RemedyImage givenImage)
        {
            prepareImage(stream, bitmap, givenImage);
            int[,] coeff = new int[5, 5];
            int offset = 0, scale = 0;
            Emboss_SetValues(ref coeff, ref offset, ref scale);
            CustomFilter custom_image = new CustomFilter(givenImage.srcPixels, givenImage.width, givenImage.height, offset, scale, coeff);
            givenImage.dstPixels = custom_image.Filter();
            setStream(stream, bitmap, givenImage);
        }

        // Change the image with emboss2 filter applied
        public void doEmboss2(Stream stream, WriteableBitmap bitmap, RemedyImage givenImage)
        {
            prepareImage(stream, bitmap, givenImage);
            int[,] coeff = new int[5, 5];
            int offset = 0, scale = 0;
            Emboss2_SetValues(ref coeff, ref offset, ref scale);
            CustomFilter custom_image = new CustomFilter(givenImage.srcPixels, givenImage.width, givenImage.height, offset, scale, coeff);
            givenImage.dstPixels = custom_image.Filter();
            setStream(stream, bitmap, givenImage);
        }

        // Change the image with sharpen filter applied
        public void doSharpen(Stream stream, WriteableBitmap bitmap, RemedyImage givenImage)
        {
            prepareImage(stream, bitmap, givenImage);
            int[,] coeff = new int[5, 5];
            int offset = 0, scale = 0;
            Sharpen_SetValues(ref coeff, ref offset, ref scale);
            CustomFilter custom_image = new CustomFilter(givenImage.srcPixels, givenImage.width, givenImage.height, offset, scale, coeff);
            givenImage.dstPixels = custom_image.Filter();
            setStream(stream, bitmap, givenImage);
        }

        // Change the image with noise filter applied
        public void doNoise(Stream stream, WriteableBitmap bitmap, RemedyImage givenImage)
        {
            prepareImage(stream, bitmap, givenImage);

            givenImage.Noise(givenImage.Noise_GetSquareWidth(20));
            setStream(stream, bitmap, givenImage);
        }

        // Change the image with hardnoise filter applied
        public void doHardNoise(Stream stream, WriteableBitmap bitmap, RemedyImage givenImage)
        {
            prepareImage(stream, bitmap, givenImage);

            givenImage.Noise(1);
            setStream(stream, bitmap, givenImage);
        }

        // Change the image with blur filter applied
        public void doBlur(Stream stream, WriteableBitmap bitmap, RemedyImage givenImage)
        {
            prepareImage(stream, bitmap, givenImage);
            int[,] coeff = new int[5, 5];
            int offset = 0, scale = 0;
            Blur_SetValues(ref coeff, ref offset, ref scale);
            CustomFilter custom_image = new CustomFilter(givenImage.srcPixels, givenImage.width, givenImage.height, offset, scale, coeff);
            givenImage.dstPixels = custom_image.Filter();
            setStream(stream, bitmap, givenImage);
        }

        // Change the image with blur2 filter applied
        public void doBlur2(Stream stream, WriteableBitmap bitmap, RemedyImage givenImage)
        {
            prepareImage(stream, bitmap, givenImage);
            int[,] coeff = new int[5, 5];
            int offset = 0, scale = 0;
            Blur2_SetValues(ref coeff, ref offset, ref scale);
            CustomFilter custom_image = new CustomFilter(givenImage.srcPixels, givenImage.width, givenImage.height, offset, scale, coeff);
            givenImage.dstPixels = custom_image.Filter();
            setStream(stream, bitmap, givenImage);
        }

        // Change the image with edgeDetect filter applied
        public void doEdgeDetect(Stream stream, WriteableBitmap bitmap, RemedyImage givenImage)
        {
            prepareImage(stream, bitmap, givenImage);
            int[,] coeff = new int[5, 5];
            int offset = 0, scale = 0;
            EdgeDetect_SetValues(ref coeff, ref offset, ref scale);
            CustomFilter custom_image = new CustomFilter(givenImage.srcPixels, givenImage.width, givenImage.height, offset, scale, coeff);
            givenImage.dstPixels = custom_image.Filter();
            setStream(stream, bitmap, givenImage);
        }

        // Change the image with edgeEnhance filter applied
        public void doEdgeEnhance(Stream stream, WriteableBitmap bitmap, RemedyImage givenImage)
        {
            prepareImage(stream, bitmap, givenImage);
            int[,] coeff = new int[5, 5];
            int offset = 0, scale = 0;
            EdgeEnhance_SetValues(ref coeff, ref offset, ref scale);
            CustomFilter custom_image = new CustomFilter(givenImage.srcPixels, givenImage.width, givenImage.height, offset, scale, coeff);
            givenImage.dstPixels = custom_image.Filter();
            setStream(stream, bitmap, givenImage);

        }

        // Change the image with retro filter applied
        public void doRetro(Stream stream, WriteableBitmap bitmap, RemedyImage givenImage)
        {
            prepareImage(stream, bitmap, givenImage);
            givenImage.ColorChange(0, 0, 0, 50, 50, -50);
            setStream(stream, bitmap, givenImage);
        }

        // Change the image with darken filter applied
        public void doDarken(Stream stream, WriteableBitmap bitmap, RemedyImage givenImage)
        {
            prepareImage(stream, bitmap, givenImage);
            givenImage.ColorChange(0, 0, 0, 50, 50, 0);
            setStream(stream, bitmap, givenImage);
        }

        // Change the image with brighten filter applied
        public void doBrighten(Stream stream, WriteableBitmap bitmap, RemedyImage givenImage)
        {
            prepareImage(stream, bitmap, givenImage);
            givenImage.ColorChange(70, 70, 70, 0, 0, 0);
            setStream(stream, bitmap, givenImage);
        }

        // Change the image with shadow filter applied
        public void doShadow(Stream stream, WriteableBitmap bitmap, RemedyImage givenImage)
        {
            prepareImage(stream, bitmap, givenImage);
            givenImage.ColorChange(-80, -80, -80, 0, 0, 0);
            setStream(stream, bitmap, givenImage);
        }

        // Change the image with crystal filter applied
        public void doCrystal(Stream stream, WriteableBitmap bitmap, RemedyImage givenImage)
        {
            prepareImage(stream, bitmap, givenImage);
            givenImage.ColorChange(0, 0, 0, 50, 35, 35);
            setStream(stream, bitmap, givenImage);
        }
        #endregion

        #region Apply Buttons
        // Event for apply button on Filters popup. Sets the image with the applied filter
        public void OnFilterApplyClick(object sender, RoutedEventArgs e)
        {
            ApplyFilter(appliedFilters);
            FilterApplyReset.Visibility = Visibility.Collapsed;
            Menu.SelectFilters.IsChecked = false;
            setFilterBitmaps();
            Saved = false;
        }

        public void ApplyFilter(string filter)
        {
            ImageLoadingRing.IsActive = true;
            switch (filter)
            {
                case "blackwhite":
                    doBlackWhite(bitmapStream, bitmapImage, imageOriginal);
                    doBlackWhite(exampleStream, exampleBitmap, image);
                    break;
                case "invert":
                    doInvert(bitmapStream, bitmapImage, imageOriginal);
                    doInvert(exampleStream, exampleBitmap, image);
                    break;
                case "emboss":
                    doEmboss(bitmapStream, bitmapImage, imageOriginal);
                    doEmboss(exampleStream, exampleBitmap, image);
                    break;
                case "emboss2":
                    doEmboss2(bitmapStream, bitmapImage, imageOriginal);
                    doEmboss2(exampleStream, exampleBitmap, image);
                    break;
                case "blur":
                    doBlur(bitmapStream, bitmapImage, imageOriginal);
                    doBlur(exampleStream, exampleBitmap, image);
                    break;
                case "blur2":
                    doBlur2(bitmapStream, bitmapImage, imageOriginal);
                    doBlur2(exampleStream, exampleBitmap, image);
                    break;
                case "sharpen":
                    doSharpen(bitmapStream, bitmapImage, imageOriginal);
                    doSharpen(exampleStream, exampleBitmap, image);
                    break;
                case "noise":
                    doNoise(bitmapStream, bitmapImage, imageOriginal);
                    doNoise(exampleStream, exampleBitmap, image);
                    break;
                case "hardNoise":
                    doHardNoise(bitmapStream, bitmapImage, imageOriginal);
                    doHardNoise(exampleStream, exampleBitmap, image);
                    break;
                case "EdgeDetect":
                    doEdgeDetect(bitmapStream, bitmapImage, imageOriginal);
                    doEdgeDetect(exampleStream, exampleBitmap, image);
                    break;
                case "EdgeEnhance":
                    doEdgeEnhance(bitmapStream, bitmapImage, imageOriginal);
                    doEdgeEnhance(exampleStream, exampleBitmap, image);
                    break;
                case "retro":
                    doRetro(bitmapStream, bitmapImage, imageOriginal);
                    doRetro(exampleStream, exampleBitmap, image);
                    break;
                case "darken":
                    doDarken(bitmapStream, bitmapImage, imageOriginal);
                    doDarken(exampleStream, exampleBitmap, image);
                    break;
                case "brighten":
                    doBrighten(bitmapStream, bitmapImage, imageOriginal);
                    doBrighten(exampleStream, exampleBitmap, image);
                    break;
                case "shadow":
                    doShadow(bitmapStream, bitmapImage, imageOriginal);
                    doShadow(exampleStream, exampleBitmap, image);
                    break;
                case "crystal":
                    doCrystal(bitmapStream, bitmapImage, imageOriginal);
                    doCrystal(exampleStream, exampleBitmap, image);
                    break;
                default:
                    break;
            }
            image.srcPixels = (byte[])image.dstPixels.Clone();
            imageOriginal.srcPixels = (byte[])imageOriginal.dstPixels.Clone();
            ArchiveAddArray();
            effectsApplied.Add("Filter = " + filter);
            ResetFilterMenuData();
            ImageLoadingRing.IsActive = false;
        }


        // Event for apply button on  Rotate popup. Sets the image with the applied flip
        public void OnRotateApplyClick(object sender, RoutedEventArgs e)
        {
            ImageLoadingRing.IsActive = true;
           // SelectRotations.IsChecked = false;
            ApplyRotate(appliedRotations);

            setFilterBitmaps();
            ImageLoadingRing.IsActive = false;
            RotateApplyReset.Visibility = Visibility.Collapsed;
            Saved = false;
        }

        public void ApplyRotate(string rotation)
        {
            switch (rotation)
            {
                case "hflip":
                    prepareImage(bitmapStream, bitmapImage, imageOriginal);
                    imageOriginal.HFlip();
                    setStream(bitmapStream, bitmapImage, imageOriginal);
                    break;
                case "vflip":
                    prepareImage(bitmapStream, bitmapImage, imageOriginal);
                    imageOriginal.VFlip();
                    setStream(bitmapStream, bitmapImage, imageOriginal);
                    break;
                default:
                    break;
            }
            image.srcPixels = (byte[])image.dstPixels.Clone();
            imageOriginal.srcPixels = (byte[])imageOriginal.dstPixels.Clone();
            ArchiveAddArray();
            effectsApplied.Add("Flip = " + rotation);
            ResetRotateMenuData();
        }

        // Event for apply button on Colorize popup. Sets the image with the applied color
        public void OnColorizeApplyClick(object sender, RoutedEventArgs e)
        {
            doColorize(exampleStream, exampleBitmap, image);
            ApplyColorize();
            setFilterBitmaps();
            ColorizeApplyReset.Visibility = Visibility.Collapsed;
            Menu.SelectColorize.IsChecked = false;
            Saved = false;
        }

        public void ApplyColorize()
        {
            ImageLoadingRing.IsActive = true;
            image.srcPixels = (byte[])image.dstPixels.Clone();
            imageOriginal.srcPixels = (byte[])imageOriginal.dstPixels.Clone();
            ArchiveAddArray();
            Colorize_SetColorizeEffect();
            ImageLoadingRing.IsActive = false;
        }

        public void Colorize_SetColorizeEffect()
        {
            string colorizeColors = "";
            Colorize_GetColorizeColors(ref colorizeColors);
            effectsApplied.Add("Colorize = " + colorizeColors);

        }

        public void Colorize_GetColorizeColors(ref string colorizeColors)
        {
            if (blueForColorize)
            {
                Colorize_CheckForFirsColor(ref colorizeColors);
                colorizeColors += "blue";
            }
            if (redForColorize)
            {
                Colorize_CheckForFirsColor(ref colorizeColors);
                colorizeColors += "red";
            }
            if (greenForColorize)
            {
                Colorize_CheckForFirsColor(ref colorizeColors);
                colorizeColors += "green";
            }
            if (yellowForColorize)
            {
                Colorize_CheckForFirsColor(ref colorizeColors);
                colorizeColors += "yellow";
            }
            if (orangeForColorize)
            {
                Colorize_CheckForFirsColor(ref colorizeColors);
                colorizeColors += "orange";
            }
            if (purpleForColorize)
            {
                Colorize_CheckForFirsColor(ref colorizeColors);
                colorizeColors += "purple";
            }
            if (cyanForColorize)
            {
                Colorize_CheckForFirsColor(ref colorizeColors);
                colorizeColors += "cyan";
            }
            if (limeForColorize)
            {
                Colorize_CheckForFirsColor(ref colorizeColors);
                colorizeColors += "lime";
            }
        }

        public void Colorize_CheckForFirsColor(ref string colorizeColors)
        {
            if (!colorizeColors.Equals(""))
            {
                colorizeColors += ",";
            }

        }
        // Event for apply button on Exposure popup. Sets the image with the applied exposure
        public void OnExposureApplyClick(object sender, RoutedEventArgs e)
        {
            ApplyExposure(appliedColors);
            setFilterBitmaps();
            Saved = false;
        }

        public void ApplyExposure(string effect)
        {
            ImageLoadingRing.IsActive = true;
            ExposureApplyReset.Visibility = Visibility.Collapsed;
            Menu.SelectExposure.IsChecked = false;

            switch (effect)
            {
                case "gammadarken":
                    doGammaDarken();
                    break;
                case "gammalighten":
                    doGammaLighten();
                    break;
                default:
                    break;
            }

            image.srcPixels = (byte[])image.dstPixels.Clone();
            imageOriginal.srcPixels = (byte[])imageOriginal.dstPixels.Clone();
            ArchiveAddArray();
            effectsApplied.Add("Exposure = " + brightSlider.Value + "," + BlueGammaSlider.Value + "," + GreenGammaSlider.Value + "," + RedGammaSlider.Value);
            ResetExposureMenuData();
            ImageLoadingRing.IsActive = false;
        }

        public void doGammaDarken()
        {
            prepareImage(bitmapStream, bitmapImage, imageOriginal);
            imageOriginal.dstPixels = (byte[])imageOriginal.srcPixels.Clone();
            imageOriginal.GammaChange(BlueGammaSlider.Value, GreenGammaSlider.Value, RedGammaSlider.Value);
            imageOriginal.Darken(brightSlider.Value);
            setStream(bitmapStream, bitmapImage, imageOriginal);
        }

        public void doGammaLighten()
        {
            prepareImage(bitmapStream, bitmapImage, imageOriginal);
            imageOriginal.dstPixels = (byte[])imageOriginal.srcPixels.Clone();
            imageOriginal.GammaChange(BlueGammaSlider.Value, GreenGammaSlider.Value, RedGammaSlider.Value);
            imageOriginal.Lighten(brightSlider.Value);
            setStream(bitmapStream, bitmapImage, imageOriginal);
        }

        #endregion

        #region Reset Buttons
        // All those events reset the interface and return the last applied image.
        public void OnFilterResetClick(object sender, RoutedEventArgs e)
        {
            // This resets the interface and returns the last applied image.
            if (pictureIsLoaded)
            {
                prepareImage(exampleStream, exampleBitmap, image);
                image.Reset();
                setStream(exampleStream, exampleBitmap, image);
            }
            FilterApplyReset.Visibility = Visibility.Collapsed;
            ResetFilterMenuData();
            deselectFilters();
        }

        public void OnColorResetClick(object sender, RoutedEventArgs e)
        {
            // This resets the interface and returns the last applied image.
            ResetColorMenuData();
            prepareImage(exampleStream, exampleBitmap, image);
            image.Reset();
            setStream(exampleStream, exampleBitmap, image);
            ColorsPopup.ColorApplyReset.Visibility = Visibility.Collapsed;
        }

        public void OnRotateResetClick(object sender, RoutedEventArgs e)
        {
            prepareImage(exampleStream, exampleBitmap, image);
            image.Reset();
            setStream(exampleStream, exampleBitmap, image);
            ResetRotateMenuData();
            RotateApplyReset.Visibility = Visibility.Collapsed;
        }

        public void OnColorizeResetClick(object sender, RoutedEventArgs e)
        {
            deselectColorizeGridItems();
            prepareImage(bitmapStream, bitmapImage, imageOriginal);
            imageOriginal.dstPixels = (byte[])imageOriginal.srcPixels.Clone();
            setStream(bitmapStream, bitmapImage, imageOriginal);
            redForColorize = greenForColorize = blueForColorize = yellowForColorize =
                 orangeForColorize = purpleForColorize = cyanForColorize =
                 limeForColorize = false;
        }

        public void OnExposureResetClick(object sender, RoutedEventArgs e)
        {
            ResetExposureMenuData();
            ExposureApplyReset.Visibility = Visibility.Collapsed;
        }
        #endregion


        #region Frames
        // The events are called when a frame button is clicked.


        // Set standard frame to the image
        public void OnStandardClick(object sender, RoutedEventArgs e)
        {
            FramesApplyReset.Visibility = Visibility.Visible;

            if (pictureIsLoaded)
            {
                appliedFrame = "standard";
                ApplyStandardFrame((int)FrameWidthPercent.Value);
            }
        }

        public void ApplyStandardFrame(int thick)
        {
            prepareImage(bitmapStream, bitmapImage, imageOriginal);
            imageOriginal.dstPixels = (byte[])imageOriginal.srcPixels.Clone();
            imageOriginal.Frames_StandardLeftSide(Frame_GetFrameColor(), thick);
            imageOriginal.Frames_StandardTopSide(Frame_GetFrameColor(), thick);
            imageOriginal.Frames_StandardRightSide(Frame_GetFrameColor(), thick);
            imageOriginal.Frames_StandardBottomSide(Frame_GetFrameColor(), thick);
            setStream(bitmapStream, bitmapImage, imageOriginal);
        }

        // Set standard frame (only UP or DOWN) to the image
        public void OnStandardUpDownClick(object sender, RoutedEventArgs e)
        {
            FramesApplyReset.Visibility = Visibility.Visible;

            if (pictureIsLoaded)
            {
                appliedFrame = "standard up down";
                ApplyStandartUpDownFrame((int)FrameWidthPercent.Value);
            }
        }

        public void ApplyStandartUpDownFrame(int thick)
        {
            prepareImage(bitmapStream, bitmapImage, imageOriginal);
            imageOriginal.dstPixels = (byte[])imageOriginal.srcPixels.Clone();
            imageOriginal.Frames_StandardTopSide(Frame_GetFrameColor(), thick);
            imageOriginal.Frames_StandardBottomSide(Frame_GetFrameColor(), thick);
            setStream(bitmapStream, bitmapImage, imageOriginal);
        }

        // Set standard frame (only LEFT or RIGHT) to the image
        public void OnStandardLeftRightClick(object sender, RoutedEventArgs e)
        {
            FramesApplyReset.Visibility = Visibility.Visible;

            if (pictureIsLoaded)
            {
                appliedFrame = "standard left right";
                ApplyStandardLeftRightFrame((int)FrameWidthPercent.Value);
            }
        }

        public void ApplyStandardLeftRightFrame(int thick)
        {
            prepareImage(bitmapStream, bitmapImage, imageOriginal);
            imageOriginal.dstPixels = (byte[])imageOriginal.srcPixels.Clone();
            imageOriginal.Frames_StandardLeftSide(Frame_GetFrameColor(), thick);
            imageOriginal.Frames_StandardRightSide(Frame_GetFrameColor(), thick);
            setStream(bitmapStream, bitmapImage, imageOriginal);
        }

        // Set darkness frame to the image
        public void OnDarknessClick(object sender, RoutedEventArgs e)
        {
            FramesApplyReset.Visibility = Visibility.Visible;

            if (pictureIsLoaded)
            {
                appliedFrame = "darkness";
                ApplyDarknessFrame((int)FrameWidthPercent.Value);
            }
        }

        public void ApplyDarknessFrame(int thick)
        {
            prepareImage(bitmapStream, bitmapImage, imageOriginal);
            imageOriginal.dstPixels = (byte[])imageOriginal.srcPixels.Clone();
            imageOriginal.Frames_DarknessLeftSide(thick);
            imageOriginal.Frames_DarknessTopSide(thick);
            imageOriginal.Frames_DarknessRightSide(thick);
            imageOriginal.Frames_DarknessBottomSide(thick);
            setStream(bitmapStream, bitmapImage, imageOriginal);
        }

        // Set darkness frame (only left or right) to the image
        public void OnDarknessLeftRightClick(object sender, RoutedEventArgs e)
        {
            FramesApplyReset.Visibility = Visibility.Visible;

            if (pictureIsLoaded)
            {
                appliedFrame = "darkness left right";
                ApplyDarknessLeftRightFrame((int)FrameWidthPercent.Value);
            }
        }

        public void ApplyDarknessLeftRightFrame(int thick)
        {
            prepareImage(bitmapStream, bitmapImage, imageOriginal);
            imageOriginal.dstPixels = (byte[])imageOriginal.srcPixels.Clone();
            imageOriginal.Frames_DarknessLeftSide(thick);
            imageOriginal.Frames_DarknessRightSide(thick);
            setStream(bitmapStream, bitmapImage, imageOriginal);
        }

        // Set darkness frame (only up or down) to the image
        public void OnDarknessUpDownSidesClick(object sender, RoutedEventArgs e)
        {
            FramesApplyReset.Visibility = Visibility.Visible;

            if (pictureIsLoaded)
            {
                appliedFrame = "darkness up down";
                ApplyDarknessUpDownFrame((int)FrameWidthPercent.Value);
            }
        }

        public void ApplyDarknessUpDownFrame(int thick)
        {
            prepareImage(bitmapStream, bitmapImage, imageOriginal);
            imageOriginal.dstPixels = (byte[])imageOriginal.srcPixels.Clone();
            imageOriginal.Frames_DarknessTopSide(thick);
            imageOriginal.Frames_DarknessBottomSide(thick);
            setStream(bitmapStream, bitmapImage, imageOriginal);
        }

        // Set smooth darkness frame to the image
        public void OnSmoothDarknessClick(object sender, RoutedEventArgs e)
        {
            FramesApplyReset.Visibility = Visibility.Visible;

            if (pictureIsLoaded)
            {
                appliedFrame = "smooth darkness";
                ApplySmoothDarknessFrame((int)FrameWidthPercent.Value);
            }
        }

        public void ApplySmoothDarknessFrame(int thick)
        {
            prepareImage(bitmapStream, bitmapImage, imageOriginal);
            imageOriginal.dstPixels = (byte[])imageOriginal.srcPixels.Clone();
            imageOriginal.Frames_SmoothDarkness(thick);
            setStream(bitmapStream, bitmapImage, imageOriginal);
        }

        // Set standard frame with smooth angles to the image
        public void OnStandardAngleClick(object sender, RoutedEventArgs e)
        {
            FramesApplyReset.Visibility = Visibility.Visible;

            if (pictureIsLoaded)
            {
                appliedFrame = "standard angle";
                ApplyStandardAngleFrame((int)FrameWidthPercent.Value);
            }
        }

        public void ApplyStandardAngleFrame(int thick)
        {
            prepareImage(bitmapStream, bitmapImage, imageOriginal);
            imageOriginal.dstPixels = (byte[])imageOriginal.srcPixels.Clone();
            imageOriginal.Frames_StandardLeftSide(Frame_GetFrameColor(), thick);
            imageOriginal.Frames_StandardTopSide(Frame_GetFrameColor(), thick);
            imageOriginal.Frames_StandardRightSide(Frame_GetFrameColor(), thick);
            imageOriginal.Frames_StandardBottomSide(Frame_GetFrameColor(), thick);
            imageOriginal.Frames_StandartAngle(Frame_GetFrameColor(), thick);
            setStream(bitmapStream, bitmapImage, imageOriginal);
        }

        // Set smooth angles frame to the image
        public void OnAngleClick(object sender, RoutedEventArgs e)
        {
            FramesApplyReset.Visibility = Visibility.Visible;

            if (pictureIsLoaded)
            {
                appliedFrame = "angle";
                ApplyAngleFrame((int)FrameWidthPercent.Value);
            }
        }

        public void ApplyAngleFrame(int thick)
        {
            prepareImage(bitmapStream, bitmapImage, imageOriginal);
            imageOriginal.dstPixels = (byte[])imageOriginal.srcPixels.Clone();
            imageOriginal.Frames_Angle(Frame_GetFrameColor(), thick);
            setStream(bitmapStream, bitmapImage, imageOriginal);
        }

        // Apply the frame on the image
        public void OnApplyFramesClick(object sender, RoutedEventArgs e)
        {
            imageOriginal.srcPixels = (byte[])imageOriginal.dstPixels.Clone();
            ArchiveAddArray();
            effectsApplied.Add("Frame = " + FrameWidthPercent.Value + "," + appliedFrameColor + "," + appliedFrame);
            setExampleImage();
            setFilterBitmaps();
            FramesApplyReset.Visibility = Visibility.Collapsed;
            ResetFrameMenuData();
            Saved = false;
        }

        // Reset the image (return the pixels before applying the frame)
        public void OnResetFramesClick(object sender, RoutedEventArgs e)
        {
            if (pictureIsLoaded)
            {
                ResetFrameMenuData();
                prepareImage(bitmapStream, bitmapImage, imageOriginal);
                imageOriginal.Reset();
                setStream(bitmapStream, bitmapImage, imageOriginal);
            }

            FramesApplyReset.Visibility = Visibility.Collapsed;
        }

        // If black color is selected
        public void BlackFrameTapped(object sender, TappedRoutedEventArgs e)
        {
            appliedFrameColor = "black";
        }

        // If gray color is selected
        public void GrayFrameTapped(object sender, TappedRoutedEventArgs e)
        {
            appliedFrameColor = "gray";
        }

        // If white color is selected
        public void WhiteFrameTapped(object sender, TappedRoutedEventArgs e)
        {
            appliedFrameColor = "white";
        }

        // If blue color is selected
        public void BlueFrameTapped(object sender, TappedRoutedEventArgs e)
        {
            appliedFrameColor = "blue";
        }

        // If lime color is selected
        public void LimeFrameTapped(object sender, TappedRoutedEventArgs e)
        {
            appliedFrameColor = "lime";
        }

        // If yellow color is selected
        public void YellowFrameTapped(object sender, TappedRoutedEventArgs e)
        {
            appliedFrameColor = "yellow";
        }

        // If cyan color is selected
        public void CyanFrameTapped(object sender, TappedRoutedEventArgs e)
        {
            appliedFrameColor = "cyan";
        }

        // If magenta color is selected
        public void MagentaFrameTapped(object sender, TappedRoutedEventArgs e)
        {
            appliedFrameColor = "magenta";
        }

        // If silver color is selected
        public void SilverFrameTapped(object sender, TappedRoutedEventArgs e)
        {
            appliedFrameColor = "silver";
        }

        // If maroon color is selected
        public void MaroonFrameTapped(object sender, TappedRoutedEventArgs e)
        {
            appliedFrameColor = "maroon";
        }

        // If olive color is selected
        public void OliveFrameTapped(object sender, TappedRoutedEventArgs e)
        {
            appliedFrameColor = "olive";
        }

        // If green color is selected
        public void GreenFrameTapped(object sender, TappedRoutedEventArgs e)
        {
            appliedFrameColor = "green";
        }

        // If purple color is selected
        public void PurpleFrameTapped(object sender, TappedRoutedEventArgs e)
        {
            appliedFrameColor = "purple";
        }

        // If teal color is selected
        public void TealFrameTapped(object sender, TappedRoutedEventArgs e)
        {
            appliedFrameColor = "teal";
        }

        // If navy color is selected
        public void NavyFrameTapped(object sender, TappedRoutedEventArgs e)
        {
            appliedFrameColor = "navy";
        }

        // If red color is selected
        public void RedFrameTapped(object sender, TappedRoutedEventArgs e)
        {
            appliedFrameColor = "red";
        }

        // Get the B G R value of selected color
        public byte[] Frame_GetFrameColor()
        {
            byte[] Color = { 0, 0, 0 };

            switch (appliedFrameColor)
            {
                case "black":
                    {
                        Color[0] = 0;
                        Color[1] = 0;
                        Color[2] = 0;
                        break;
                    }
                case "gray":
                    {
                        Color[0] = 128;
                        Color[1] = 128;
                        Color[2] = 128;
                        break;
                    }
                case "white":
                    {
                        Color[0] = 255;
                        Color[1] = 255;
                        Color[2] = 255;
                        break;
                    }
                case "lime":
                    {
                        Color[0] = 0;
                        Color[1] = 255;
                        Color[2] = 0;
                        break;
                    }
                case "yellow":
                    {
                        Color[0] = 0;
                        Color[1] = 255;
                        Color[2] = 255;
                        break;
                    }
                case "blue":
                    {
                        Color[0] = 255;
                        Color[1] = 0;
                        Color[2] = 0;
                        break;
                    }
                case "red":
                    {
                        Color[0] = 0;
                        Color[1] = 0;
                        Color[2] = 255;
                        break;
                    }
                case "cyan":
                    {
                        Color[0] = 255;
                        Color[1] = 255;
                        Color[2] = 0;
                        break;
                    }
                case "magenta":
                    {
                        Color[0] = 255;
                        Color[1] = 0;
                        Color[2] = 255;
                        break;
                    }
                case "silver":
                    {
                        Color[0] = 192;
                        Color[1] = 192;
                        Color[2] = 192;
                        break;
                    }
                case "maroon":
                    {
                        Color[0] = 0;
                        Color[1] = 0;
                        Color[2] = 128;
                        break;
                    }
                case "olive":
                    {
                        Color[0] = 0;
                        Color[1] = 128;
                        Color[2] = 128;
                        break;
                    }
                case "green":
                    {
                        Color[0] = 0;
                        Color[1] = 128;
                        Color[2] = 0;
                        break;
                    }
                case "purple":
                    {
                        Color[0] = 128;
                        Color[1] = 0;
                        Color[2] = 128;
                        break;
                    }
                case "teal":
                    {
                        Color[0] = 128;
                        Color[1] = 128;
                        Color[2] = 0;
                        break;
                    }
                case "navy":
                    {
                        Color[0] = 128;
                        Color[1] = 0;
                        Color[2] = 0;
                        break;
                    }
            }
            return Color;
        }
        #endregion

        #region Rotate
        // The events are called when a Rotate button is clicked.

        public void OnHFlipClick(object sender, RoutedEventArgs e)
        {
            appliedRotations = "hflip";
            prepareImage(exampleStream, exampleBitmap, image);
            image.HFlip();
            setStream(exampleStream, exampleBitmap, image);
        }

        public void OnVFlipClick(object sender, RoutedEventArgs e)
        {
            appliedRotations = "vflip";
            prepareImage(exampleStream, exampleBitmap, image);
            image.VFlip();
            setStream(exampleStream, exampleBitmap, image);
        }

        public void OnRotateClick(object sender, RoutedEventArgs e)
        {
            ImageLoadingRing.IsActive = true;
            Menu.SelectRotations.IsChecked = false;

            if (e.OriginalSource.Equals(RotateLeft))
            {
                RotateBitmap("RotateLeft");
            }
            else if (e.OriginalSource.Equals(RotateRight))
            {
                RotateBitmap("RotateRight");
            }

            ImageLoadingRing.IsActive = false;
        }

        public async void RotateBitmap(string givenElementString)
        {
            if (givenElementString == "RotateLeft")
            {
                bitmapImage = await RotateImage(bitmapImage, (uint)bitmapImage.PixelWidth, (uint)bitmapImage.PixelHeight, "left");
            }
            else if (givenElementString == "RotateRight")
            {
                bitmapImage = await RotateImage(bitmapImage, (uint)bitmapImage.PixelWidth, (uint)bitmapImage.PixelHeight, "right");
            }

            imageDisplayed.displayImage.Source = bitmapImage;

            bitmapStream = bitmapImage.PixelBuffer.AsStream();
            imageOriginal.srcPixels = new byte[(uint)bitmapStream.Length];
            await bitmapStream.ReadAsync(imageOriginal.srcPixels, 0, imageOriginal.srcPixels.Length);
            prepareImage(bitmapStream, bitmapImage, imageOriginal);
            setStream(bitmapStream, bitmapImage, imageOriginal);

            setExampleBitmaps();
            setFilterBitmaps();

            imageDisplayed.sourceImagePixelHeight = (uint)bitmapImage.PixelHeight;
            imageDisplayed.sourceImagePixelWidth = (uint)bitmapImage.PixelWidth;
        }

        #endregion

        #region Exposure
        // The event is called when the Gama slider or Brighr slider is changed.
        public void OnExposureChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (pictureIsLoaded)
            {
                prepareImage(exampleStream, exampleBitmap, image);
                image.dstPixels = (byte[])image.srcPixels.Clone();
                image.GammaChange(BlueGammaSlider.Value, GreenGammaSlider.Value, RedGammaSlider.Value);
                // We check if the changed value 
                // is higher than 0 - we call the brightness function
                // is lower than 0  - we call the darkness function
                // And finally we save the new byte array to the image.
                if (brightSlider.Value < 0)
                {
                    appliedColors = "gammadarken";
                    image.Darken(brightSlider.Value);
                }
                else if (brightSlider.Value >= 0)
                {
                    appliedColors = "gammalighten";
                    image.Lighten(brightSlider.Value);
                }
                setStream(exampleStream, exampleBitmap, image);
            }
        }

        #endregion

        #region Back buttons

        public void BackPopupClicked(object sender, RoutedEventArgs e)
        {
            // If the back popup button is clicked, close all popups.
            Menu.SelectColors.IsChecked = false;
            Menu.SelectFilters.IsChecked = false;
            Menu.SelectRotations.IsChecked = false;
            Menu.SelectOptions.IsChecked = false;
            Menu.SelectColorize.IsChecked = false;
            Menu.SelectFrames.IsChecked = false;
            Menu.SelectHistogram.IsChecked = false;
            Menu.SelectExposure.IsChecked = false;
            Menu.SelectCustom.IsChecked = false;
        }

        public void BackFeedbackClicked(object sender, RoutedEventArgs e)
        {
            // If the back feedback button is clicked, close the feedback and show the settings charm.
            Feedback.IsOpen = false;
            SettingsPane.Show();
        }
        #endregion

 
        public void ImagePointerReleased(object sender, PointerRoutedEventArgs e)
        {
            imageDisplayed.forceManipulationsToEnd = true;
        }


        #region Small Bitmaps for Filters
        public async void setFilterBitmaps()
        {
            // This creates temporary Streams and WriteableBitmap objects for every filter available.
            // We set the bitmaps as source to the XAML Image objects.
            // After this, we apply different filter for each of the WriteableBitmap objects.

            RemedyImage filterimage = new RemedyImage();
            uint newWidth = (uint)bitmapImage.PixelWidth;
            uint newHeight = (uint)bitmapImage.PixelHeight;

            while (newWidth > 150 && newHeight > 150)
            {
                newWidth = newWidth / 2;
                newHeight = newHeight / 2;
            }

            Stream
            blackWhiteStream = null,
            emboss2Stream = null,
            embossStream = null,
            invertStream = null,
            blurStream = null,
            blur2Stream = null,
            sharpenStream = null,
            noiseStream = null;

            WriteableBitmap
            blackWhiteBitmap = await ResizeImage(bitmapImage, newWidth, newHeight),
            embossBitmap = await ResizeImage(bitmapImage, newWidth, newHeight),
            emboss2Bitmap = await ResizeImage(bitmapImage, newWidth, newHeight),
            invertBitmap = await ResizeImage(bitmapImage, newWidth, newHeight),
            blurBitmap = await ResizeImage(bitmapImage, newWidth, newHeight),
            blur2Bitmap = await ResizeImage(bitmapImage, newWidth, newHeight),
            sharpenBitmap = await ResizeImage(bitmapImage, newWidth, newHeight),
            noiseBitmap = await ResizeImage(bitmapImage, newWidth, newHeight);

            blackWhiteFilter.Source = blackWhiteBitmap;
            embossFilter.Source = embossBitmap;
            emboss2Filter.Source = emboss2Bitmap;
            invertFilter.Source = invertBitmap;
            blurFilter.Source = blurBitmap;
            blur2Filter.Source = blur2Bitmap;
            sharpenFilter.Source = sharpenBitmap;
            noiseFilter.Source = noiseBitmap;

            blackWhiteStream = blackWhiteBitmap.PixelBuffer.AsStream();
            embossStream = embossBitmap.PixelBuffer.AsStream();
            emboss2Stream = emboss2Bitmap.PixelBuffer.AsStream();
            invertStream = invertBitmap.PixelBuffer.AsStream();
            blurStream = blurBitmap.PixelBuffer.AsStream();
            blur2Stream = blur2Bitmap.PixelBuffer.AsStream();
            sharpenStream = sharpenBitmap.PixelBuffer.AsStream();
            noiseStream = noiseBitmap.PixelBuffer.AsStream();

            initializeBitmap(blackWhiteStream, blackWhiteBitmap, filterimage);
            initializeBitmap(embossStream, embossBitmap, filterimage);
            initializeBitmap(emboss2Stream, emboss2Bitmap, filterimage);
            initializeBitmap(invertStream, invertBitmap, filterimage);
            initializeBitmap(blurStream, blurBitmap, filterimage);
            initializeBitmap(blur2Stream, blur2Bitmap, filterimage);
            initializeBitmap(sharpenStream, sharpenBitmap, filterimage);
            initializeBitmap(noiseStream, noiseBitmap, filterimage);

            prepareImage(blackWhiteStream, blackWhiteBitmap, filterimage);
            setStream(blackWhiteStream, blackWhiteBitmap, filterimage);

            doFilter(blackWhiteStream, blackWhiteBitmap, filterimage, "blackwhite");
            doFilter(embossStream, embossBitmap, filterimage, "emboss");
            doFilter(emboss2Stream, emboss2Bitmap, filterimage, "emboss2");
            doFilter(invertStream, invertBitmap, filterimage, "invert");
            doFilter(blurStream, blurBitmap, filterimage, "blur");
            doFilter(blur2Stream, blur2Bitmap, filterimage, "blur2");
            doFilter(sharpenStream, sharpenBitmap, filterimage, "sharpen");
            doFilter(noiseStream, noiseBitmap, filterimage, "noise");

            Stream
            hardNoiseStream = null,
            retroStream = null,
            darkenStream = null,
            edgeDetectStream = null,
            edgeEnhanceStream = null,
            brightenStream = null,
            shadowStream = null,
            crystalStream = null;

            WriteableBitmap
            hardNoiseBitmap = await ResizeImage(bitmapImage, newWidth, newHeight),
            edgeDetectBitmap = await ResizeImage(bitmapImage, newWidth, newHeight),
            edgeEnhanceBitmap = await ResizeImage(bitmapImage, newWidth, newHeight),
            retroBitmap = await ResizeImage(bitmapImage, newWidth, newHeight),
            darkenBitmap = await ResizeImage(bitmapImage, newWidth, newHeight),
            brightenBitmap = await ResizeImage(bitmapImage, newWidth, newHeight),
            shadowBitmap = await ResizeImage(bitmapImage, newWidth, newHeight),
            crystalBitmap = await ResizeImage(bitmapImage, newWidth, newHeight);

            hardNoiseFilter.Source = hardNoiseBitmap;
            edgeDetectFilter.Source = edgeDetectBitmap;
            edgeEnhanceFilter.Source = edgeEnhanceBitmap;
            retroFilter.Source = retroBitmap;
            darkenFilter.Source = darkenBitmap;
            brightenFilter.Source = brightenBitmap;
            shadowFilter.Source = shadowBitmap;
            crystalFilter.Source = crystalBitmap;

            hardNoiseStream = hardNoiseBitmap.PixelBuffer.AsStream();
            edgeDetectStream = edgeDetectBitmap.PixelBuffer.AsStream();
            edgeEnhanceStream = edgeEnhanceBitmap.PixelBuffer.AsStream();
            retroStream = retroBitmap.PixelBuffer.AsStream();
            darkenStream = darkenBitmap.PixelBuffer.AsStream();
            brightenStream = brightenBitmap.PixelBuffer.AsStream();
            shadowStream = shadowBitmap.PixelBuffer.AsStream();
            crystalStream = crystalBitmap.PixelBuffer.AsStream();

            initializeBitmap(hardNoiseStream, hardNoiseBitmap, filterimage);
            initializeBitmap(edgeDetectStream, edgeDetectBitmap, filterimage);
            initializeBitmap(edgeEnhanceStream, edgeEnhanceBitmap, filterimage);
            initializeBitmap(retroStream, retroBitmap, filterimage);
            initializeBitmap(darkenStream, darkenBitmap, filterimage);
            initializeBitmap(brightenStream, brightenBitmap, filterimage);
            initializeBitmap(shadowStream, shadowBitmap, filterimage);
            initializeBitmap(crystalStream, crystalBitmap, filterimage);

            prepareImage(hardNoiseStream, hardNoiseBitmap, filterimage);
            setStream(hardNoiseStream, hardNoiseBitmap, filterimage);

            doFilter(hardNoiseStream, hardNoiseBitmap, filterimage, "hardNoise");
            doFilter(edgeDetectStream, edgeDetectBitmap, filterimage, "EdgeDetect");
            doFilter(edgeEnhanceStream, edgeEnhanceBitmap, filterimage, "EdgeEnhance");
            doFilter(retroStream, retroBitmap, filterimage, "retro");
            doFilter(darkenStream, darkenBitmap, filterimage, "darken");
            doFilter(brightenStream, brightenBitmap, filterimage, "brighten");
            doFilter(shadowStream, shadowBitmap, filterimage, "shadow");
            doFilter(crystalStream, crystalBitmap, filterimage, "crystal");
        }

        public async void initializeBitmap(Stream givenStream, WriteableBitmap givenBitmap, RemedyImage givenImage)
        {
            // This makes the required operations for initializing the WriteableBitmap.
            givenStream = givenBitmap.PixelBuffer.AsStream();
            givenImage.srcPixels = new byte[(uint)givenStream.Length];
            await givenStream.ReadAsync(givenImage.srcPixels, 0, givenImage.srcPixels.Length);
        }

        public void doFilter(Stream givenStream, WriteableBitmap givenBitmap, RemedyImage givenImage, string filter)
        {
            // Filter the passed image with the passed filter as a string.
            switch (filter)
            {
                case "blackwhite":
                    doBlackWhite(givenStream, givenBitmap, givenImage);
                    break;
                case "invert":
                    doInvert(givenStream, givenBitmap, givenImage);
                    break;
                case "emboss":
                    doEmboss(givenStream, givenBitmap, givenImage);
                    break;
                case "emboss2":
                    doEmboss2(givenStream, givenBitmap, givenImage);
                    break;
                case "blur":
                    doBlur(givenStream, givenBitmap, givenImage);
                    break;
                case "blur2":
                    doBlur2(givenStream, givenBitmap, givenImage);
                    break;
                case "sharpen":
                    doSharpen(givenStream, givenBitmap, givenImage);
                    break;
                case "noise":
                    doNoise(givenStream, givenBitmap, givenImage);
                    break;
                case "hardNoise":
                    doHardNoise(givenStream, givenBitmap, givenImage);
                    break;
                case "EdgeDetect":
                    doEdgeDetect(givenStream, givenBitmap, givenImage);
                    break;
                case "EdgeEnhance":
                    doEdgeEnhance(givenStream, givenBitmap, givenImage);
                    break;
                case "retro":
                    doRetro(givenStream, givenBitmap, givenImage);
                    break;
                case "darken":
                    doDarken(givenStream, givenBitmap, givenImage);
                    break;
                case "brighten":
                    doBrighten(givenStream, givenBitmap, givenImage);
                    break;
                case "shadow":
                    doShadow(givenStream, givenBitmap, givenImage);
                    break;
                case "crystal":
                    doCrystal(givenStream, givenBitmap, givenImage);
                    break;
                default:
                    break;
            }
        }
        #endregion

        #region Filters Check Buttons
        public void blackWhiteChecked(object sender, RoutedEventArgs e)
        {
            appliedFilters = "blackwhite";
            deselectFilters();
            FilterApplyReset.Visibility = Visibility.Visible;
        }

        public void invertChecked(object sender, RoutedEventArgs e)
        {
            appliedFilters = "invert";
            deselectFilters();
            FilterApplyReset.Visibility = Visibility.Visible;
        }

        public void sharpenChecked(object sender, RoutedEventArgs e)
        {
            appliedFilters = "sharpen";
            deselectFilters();
            FilterApplyReset.Visibility = Visibility.Visible;
        }

        public void colorizeChecked(object sender, RoutedEventArgs e)
        {
            appliedFilters = "colorize";
            deselectFilters();
            FilterApplyReset.Visibility = Visibility.Visible;
        }

        public void retroChecked(object sender, RoutedEventArgs e)
        {
            appliedFilters = "retro";
            deselectFilters();
            FilterApplyReset.Visibility = Visibility.Visible;
        }

        public void darkenChecked(object sender, RoutedEventArgs e)
        {
            appliedFilters = "darken";
            deselectFilters();
            FilterApplyReset.Visibility = Visibility.Visible;
        }

        public void noiseChecked(object sender, RoutedEventArgs e)
        {
            appliedFilters = "noise";
            deselectFilters();
            FilterApplyReset.Visibility = Visibility.Visible;
        }

        public void hardNoiseChecked(object sender, RoutedEventArgs e)
        {
            appliedFilters = "hardNoise";
            deselectFilters();
            FilterApplyReset.Visibility = Visibility.Visible;
        }

        public void embossChecked(object sender, RoutedEventArgs e)
        {
            appliedFilters = "emboss";
            deselectFilters();
            FilterApplyReset.Visibility = Visibility.Visible;
        }

        public void emboss2Checked(object sender, RoutedEventArgs e)
        {
            appliedFilters = "emboss2";
            deselectFilters();
            FilterApplyReset.Visibility = Visibility.Visible;
        }

        public void blurChecked(object sender, RoutedEventArgs e)
        {
            appliedFilters = "blur";
            deselectFilters();
            FilterApplyReset.Visibility = Visibility.Visible;
        }

        public void blur2Checked(object sender, RoutedEventArgs e)
        {
            appliedFilters = "blur2";
            deselectFilters();
            FilterApplyReset.Visibility = Visibility.Visible;
        }

        public void edgeDetectChecked(object sender, RoutedEventArgs e)
        {
            appliedFilters = "EdgeDetect";
            deselectFilters();
            FilterApplyReset.Visibility = Visibility.Visible;
        }

        public void edgeEnhanceChecked(object sender, RoutedEventArgs e)
        {
            appliedFilters = "EdgeEnhance";
            deselectFilters();
            FilterApplyReset.Visibility = Visibility.Visible;
        }

        public void brightenChecked(object sender, RoutedEventArgs e)
        {
            appliedFilters = "brighten";
            deselectFilters();
            FilterApplyReset.Visibility = Visibility.Visible;
        }

        public void shadowChecked(object sender, RoutedEventArgs e)
        {
            appliedFilters = "shadow";
            deselectFilters();
            FilterApplyReset.Visibility = Visibility.Visible;
        }

        public void crystalChecked(object sender, RoutedEventArgs e)
        {
            appliedFilters = "crystal";
            deselectFilters();
            FilterApplyReset.Visibility = Visibility.Visible;
        }

        public void filterUnchecked(object sender, RoutedEventArgs e)
        {
            var filterSender = sender as ToggleButton;
            filterSender.IsChecked = false;
            FilterApplyReset.Visibility = Visibility.Collapsed;
        }

        public void deselectFilters()
        {
            String without = appliedFilters;
            if (without != "blackwhite")
                blackWhiteCheck.IsChecked = false;
            if (without != "invert")
                invertCheck.IsChecked = false;
            if (without != "sharpen")
                sharpenCheck.IsChecked = false;
            if (without != "noise")
                noiseCheck.IsChecked = false;
            if (without != "hardNoise")
                hardNoiseCheck.IsChecked = false;
            if (without != "emboss2")
                emboss2Check.IsChecked = false;
            if (without != "emboss")
                embossCheck.IsChecked = false;
            if (without != "EdgeDetect")
                edgeDetectCheck.IsChecked = false;
            if (without != "EdgeEnhance")
                edgeEnhanceCheck.IsChecked = false;
            if (without != "blur2")
                blur2Check.IsChecked = false;
            if (without != "blur")
                blurCheck.IsChecked = false;
            if (without != "retro")
                retroCheck.IsChecked = false;
            if (without != "darken")
                darkenCheck.IsChecked = false;
            if (without != "brighten")
                brightenCheck.IsChecked = false;
            if (without != "shadow")
                shadowCheck.IsChecked = false;
            if (without != "crystal")
                crystalCheck.IsChecked = false;
            if (appliedFilters == null)
                FilterApplyReset.Visibility = Visibility.Collapsed;
        }
        #endregion

        public void GridDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            // A simple "hot key".
            // When the user double-clicks on the interface, 
            // the currently opened popup closes.
            Menu.deselectPopups();
        }

        #region Image Options
        public async void SetLockPic_Clicked(object sender, RoutedEventArgs e)
        {
            // This sets the current image as a wallpaper on the lock screen of the current user and inform him that everything was okay.
            bool savedFile = false;
            savedFile = await SaveFile(false);
            while (!savedFile)
            {

            }
            await LockScreen.SetImageFileAsync(file);
            MessageDialog messageDialog = new MessageDialog("Picture set! :)", "All done");
            await messageDialog.ShowAsync();
            await deleteUsedFile();
        }

        public async void SetAccountPic_Clicked(object sender, RoutedEventArgs e)
        {
            // This sets the current image as an avatar of the current user and inform him that everything was okay.
            bool savedFile = false;
            savedFile = await SaveFile(false);
            while (!savedFile)
            {

            }
            SetAccountPictureResult result = await UserInformation.SetAccountPicturesAsync(null, file, null);

            if (result == SetAccountPictureResult.Success)
            {
                MessageDialog messageDialog = new MessageDialog("Picture set! :)", "All done");
                await messageDialog.ShowAsync();
                await deleteUsedFile();
            }
            else
            {
                MessageDialog messageDialog = new MessageDialog("Something failed :(", "Close");
                await messageDialog.ShowAsync();
            }
        }

        public void ReturnOriginal_Clicked(object sender, RoutedEventArgs e)
        {
            // Restore the original image.
            RestoreOriginalBitmap();
        }

        public async void RestoreOriginalBitmap()
        {
            // Reset the current image.
            imageOriginal.srcPixels = (byte[])uneditedImage.srcPixels.Clone();
            imageOriginal.dstPixels = (byte[])uneditedImage.dstPixels.Clone();
            bitmapStream = uneditedStream;
            bitmapImage = uneditedBitmap;
            prepareImage(bitmapStream, bitmapImage, imageOriginal);
            setStream(bitmapStream, bitmapImage, imageOriginal);
            imageDisplayed.displayImage.Source = bitmapImage;

            exampleBitmap = await ResizeImage(bitmapImage, (uint)(bitmapImage.PixelWidth / 5), (uint)(bitmapImage.PixelHeight / 5));
            exampleStream = exampleBitmap.PixelBuffer.AsStream();
            image.srcPixels = new byte[(uint)exampleStream.Length];
            await exampleStream.ReadAsync(image.srcPixels, 0, image.srcPixels.Length);
            prepareImage(exampleStream, exampleBitmap, image);
            setStream(exampleStream, exampleBitmap, image);
            setElements(ColorsPopup.ColorsExamplePicture, exampleBitmap);
            setElements(RotationsExamplePicture, exampleBitmap);
            setElements(ExposureExamplePicture, exampleBitmap);

            setFilterBitmaps();
            imageDisplayed.selectedRegion.ResetCorner(0, 0, imageDisplayed.displayImage.ActualWidth, imageDisplayed.displayImage.ActualHeight);
        }

        #endregion

        public async Task deleteUsedFile()
        {
            // Deletes the temporary created file.
            if (imageOriginal.dstPixels != null)
            {
                file = await ApplicationData.Current.LocalFolder.GetFileAsync("temp.jpg");
                await file.DeleteAsync();
            }
        }

        public void HistogramClicked(object sender, RoutedEventArgs e)
        {
            // Equalize the histogram of the current image.
            Menu.SelectHistogram.IsChecked = false;
            equalizeHistogram();
            setFilterBitmaps();
        }

        public void equalizeHistogram()
        {
            prepareImage(bitmapStream, bitmapImage, imageOriginal);
            imageOriginal.MakeHistogramEqualization();
            setStream(bitmapStream, bitmapImage, imageOriginal);
            prepareImage(exampleStream, exampleBitmap, image);
            image.MakeHistogramEqualization();
            setStream(exampleStream, exampleBitmap, image);
            image.srcPixels = (byte[])image.dstPixels.Clone();
            imageOriginal.srcPixels = (byte[])imageOriginal.dstPixels.Clone();
            ArchiveAddArray();
            effectsApplied.Add("Histogram = true");
        }

        #region Crop region

        #region Select Region methods

        public void Corner_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            // If a pointer presses in the corner, it means that the user starts to move the corner.
            // 1. Capture the pointer, so that the UIElement can get the Pointer events (PointerMoved,
            //    PointerReleased...) even the pointer is outside of the UIElement.
            // 2. Record the start position of the move.
            (sender as UIElement).CapturePointer(e.Pointer);

            Windows.UI.Input.PointerPoint pt = e.GetCurrentPoint(this);

            // Record the start point of the pointer.
            pointerPositionHistory[pt.PointerId] = pt.Position;

            e.Handled = true;
        }

        void Corner_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            // If a pointer which is captured by the corner moves，the select region will be updated.
            Windows.UI.Input.PointerPoint pt = e.GetCurrentPoint(this);
            uint ptrId = pt.PointerId;
            calculateCanvasCorners();

            if (pointerPositionHistory.ContainsKey(ptrId) && pointerPositionHistory[ptrId].HasValue)
            {

                Point currentPosition = pt.Position;
                Point previousPosition = pointerPositionHistory[ptrId].Value;

                double xUpdate = 0.0;
                double yUpdate = 0.0;

                // Those scary if's check the new position so the user 
                // can't expand the crop region if the pointer is out of the image.             
                if ((currentPosition.X > canvasStartX && currentPosition.X < canvasEndX)
                    || (currentPosition.X > previousPosition.X && currentPosition.X > canvasEndX)
                    || (currentPosition.X < previousPosition.X && currentPosition.X < canvasStartX))
                {
                    xUpdate = currentPosition.X - previousPosition.X;
                }
                else
                {
                    xUpdate = 0.0;
                }
                if ((currentPosition.Y > canvasStartY && currentPosition.Y < canvasEndY)
                    || (currentPosition.Y > previousPosition.Y && currentPosition.Y > canvasEndY)
                    || (currentPosition.Y < previousPosition.Y && currentPosition.Y < canvasStartY))
                {
                    yUpdate = currentPosition.Y - previousPosition.Y;
                }
                else
                {
                    yUpdate = 0.0;
                }

                imageDisplayed.selectedRegion.UpdateCorner((sender as ContentControl).Tag as string, xUpdate, yUpdate);

                pointerPositionHistory[ptrId] = currentPosition;

            }
            e.Handled = true;
        }

        public void Corner_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            // The pressed pointer is released, which means that the move is ended.
            // 1. Release the Pointer.
            // 2. Clear the position history of the Pointer.
            uint ptrId = e.GetCurrentPoint(this).PointerId;
            if (this.pointerPositionHistory.ContainsKey(ptrId))
            {
                this.pointerPositionHistory.Remove(ptrId);
            }

            (sender as UIElement).ReleasePointerCapture(e.Pointer);

            e.Handled = true;


        }

        void selectRegion_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            imageDisplayed.selectedRegion.UpdateSelectedRect(e.Delta.Scale, e.Delta.Translation.X, e.Delta.Translation.Y);
            e.Handled = true;
        }

        #endregion



        public async void UpdatePreviewImage()
        {
            // Updates the current image with the new cropped one.
            ImageLoadingRing.IsActive = true;
            await SaveFile(false);

            double sourceImageWidthScale = imageDisplayed.imageCanvas.Width / imageDisplayed.sourceImagePixelWidth;
            double sourceImageHeightScale = imageDisplayed.imageCanvas.Height / imageDisplayed.sourceImagePixelHeight;

            Size previewImageSize = new Size(
                imageDisplayed.selectedRegion.SelectedRect.Width / sourceImageWidthScale,
                imageDisplayed.selectedRegion.SelectedRect.Height / sourceImageHeightScale);

            int OrignalWidth = imageOriginal.width;
            int OriginalHeight = imageOriginal.height;
            
            if (previewImageSize.Width <=imageDisplayed.imageCanvas.Width &&
                previewImageSize.Height <=imageDisplayed.imageCanvas.Height)
            {
                imageDisplayed.displayImage.Stretch = Windows.UI.Xaml.Media.Stretch.None;
            }
            else
            {
                imageDisplayed.displayImage.Stretch = Windows.UI.Xaml.Media.Stretch.Uniform;
            }

            bitmapImage = await CropBitmap.GetCroppedBitmapAsync(
                   file,
                   new Point(imageDisplayed.selectedRegion.SelectedRect.X / sourceImageWidthScale, imageDisplayed.selectedRegion.SelectedRect.Y / sourceImageHeightScale),
                   previewImageSize,
                   1);

            // After the cropping is done, we set the new bitmapImage objects again.
            bitmapStream = bitmapImage.PixelBuffer.AsStream();
            imageOriginal.srcPixels = new byte[(uint)bitmapStream.Length];
            await bitmapStream.ReadAsync(imageOriginal.srcPixels, 0, imageOriginal.srcPixels.Length);
            imageOriginal.Reset();

            setExampleBitmaps();
            setFilterBitmaps();

            Menu.SelectCrop.IsChecked = false;

            imageDisplayed.sourceImagePixelHeight = (uint)bitmapImage.PixelHeight;
            imageDisplayed.sourceImagePixelWidth = (uint)bitmapImage.PixelWidth;

            ArchiveAddArray();
            effectsApplied.Add("Crop " + OrignalWidth + " " + OriginalHeight + " " + (int)previewImageSize.Width + " " + (int)previewImageSize.Height);
            ImageLoadingRing.IsActive = false;
            imageDisplayed.displayImage.Source = bitmapImage;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // Called when the current page is displayed.
            base.OnNavigatedTo(e);

           imageDisplayed.selectedRegion.PropertyChanged += selectedRegion_PropertyChanged;

            // Handle the pointer events of the corners. 
            AddCornerEvents(imageDisplayed.topLeftCorner);
            AddCornerEvents(imageDisplayed.topRightCorner);
            AddCornerEvents(imageDisplayed.bottomLeftCorner);
            AddCornerEvents(imageDisplayed.bottomRightCorner);

            // Handle the manipulation events of the selectRegion
            imageDisplayed.selectRegion.ManipulationDelta += selectRegion_ManipulationDelta;

            imageDisplayed.displayImage.SizeChanged += sourceImage_SizeChanged;

        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            // Called when the current page is removed.
            base.OnNavigatedFrom(e);

            imageDisplayed.selectedRegion.PropertyChanged -= selectedRegion_PropertyChanged;

            // Handle the pointer events of the corners. 
            RemoveCornerEvents(imageDisplayed.topLeftCorner);
            RemoveCornerEvents(imageDisplayed.topRightCorner);
            RemoveCornerEvents(imageDisplayed.bottomLeftCorner);
            RemoveCornerEvents(imageDisplayed.bottomRightCorner);

            // Handle the manipulation events of the selectRegion
            imageDisplayed.selectRegion.ManipulationDelta -= selectRegion_ManipulationDelta;

            imageDisplayed.displayImage.SizeChanged -= sourceImage_SizeChanged;

        }

        public void AddCornerEvents(Control corner)
        {
            corner.PointerPressed += Corner_PointerPressed;
            corner.PointerMoved += Corner_PointerMoved;
            corner.PointerReleased += Corner_PointerReleased;
        }

        public void RemoveCornerEvents(Control corner)
        {
            corner.PointerPressed -= Corner_PointerPressed;
            corner.PointerMoved -= Corner_PointerMoved;
            corner.PointerReleased -= Corner_PointerReleased;
        }

        void sourceImage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Called when the original image size is changed.
            // It calculates the new width and height.

            if (e.NewSize.IsEmpty || double.IsNaN(e.NewSize.Height) || e.NewSize.Height <= 0)
            {
                imageDisplayed.imageCanvas.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                imageDisplayed.selectedRegion.OuterRect = Rect.Empty;
                imageDisplayed.selectedRegion.ResetCorner(0, 0, 0, 0);
            }
            else
            {
                imageDisplayed.imageCanvas.Height = e.NewSize.Height;
                imageDisplayed.imageCanvas.Width = e.NewSize.Width;
                imageDisplayed.selectedRegion.OuterRect = new Rect(0, 0, e.NewSize.Width, e.NewSize.Height);

                if (e.PreviousSize.IsEmpty || double.IsNaN(e.PreviousSize.Height) || e.PreviousSize.Height <= 0)
                {
                    imageDisplayed.selectedRegion.ResetCorner(0, 0, e.NewSize.Width, e.NewSize.Height);
                }
                else
                {
                    double scale = e.NewSize.Height / e.PreviousSize.Height;
                    imageDisplayed.selectedRegion.ResizeSelectedRect(scale);
                }

            }
        }

        public void calculateCanvasCorners()
        {
            canvasStartX = 114.00 / 683.00;
            canvasStartX = Window.Current.Bounds.Width * canvasStartX;
            canvasStartX = (Window.Current.Bounds.Width -imageDisplayed.displayImage.ActualWidth) - canvasStartX;
            canvasStartX = canvasStartX / 2;

            canvasEndX = canvasStartX +imageDisplayed.displayImage.ActualWidth;

            double temp = Window.Current.Bounds.Height - 140;
            canvasStartY = 49.00 / 631.00;
            canvasStartY = temp * canvasStartY;
            canvasStartY = (Window.Current.Bounds.Height -imageDisplayed.displayImage.ActualHeight) - canvasStartY;
            canvasStartY = canvasStartY / 2;

            canvasEndY = canvasStartY +imageDisplayed.displayImage.ActualHeight;
        }

        void selectedRegion_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Called when the user has dragged the crop corner.
            double widthScale = imageDisplayed.imageCanvas.Width / imageDisplayed.sourceImagePixelWidth;
            double heightScale = imageDisplayed.imageCanvas.Height / imageDisplayed.sourceImagePixelHeight;

            if (imageDisplayed.selectedRegion.SelectedRect.Width !=imageDisplayed.displayImage.ActualWidth ||
                imageDisplayed.selectedRegion.SelectedRect.Height !=imageDisplayed.displayImage.ActualHeight)
                Panel.CropApply.Visibility = Visibility.Visible;
            else
                Panel.CropApply.Visibility = Visibility.Collapsed;
            Panel.selectInfoInBitmapText.Text = string.Format("Resolution: {0}x{1}",
                Math.Floor(imageDisplayed.selectedRegion.SelectedRect.Width / widthScale),
                Math.Floor(imageDisplayed.selectedRegion.SelectedRect.Height / heightScale));
        }

        public void saveImageButton_Click(object sender, RoutedEventArgs e)
        {
            // When the user clicks Apply, the image is cropped.
            UpdatePreviewImage();
        }

        #endregion

        #region Resizing the image

        public void OnNewWidthTextChanged(object sender, TextChangedEventArgs e)
        {
            int temp;

            if (keepProportions && newWidth.Text != "" && int.TryParse(newWidth.Text, out temp) && !calledByOther)
            {
                newHeight.Text = (Math.Round(temp / widthHeightRatio)).ToString();
            }
            calledByOther = !calledByOther;
            if (newWidth.Text != "")
            {
                ApplyResize.Visibility = Visibility.Visible;
            }
            else
            {
                ApplyResize.Visibility = Visibility.Collapsed;
            }
        }

        public void OnNewHeightTextChanged(object sender, TextChangedEventArgs e)
        {
            int temp;
            if (keepProportions && newHeight.Text != "" && int.TryParse(newHeight.Text, out temp) && !calledByOther)
            {
                newWidth.Text = (Math.Round(temp * widthHeightRatio)).ToString();
            }
            calledByOther = !calledByOther;
            if (newHeight.Text != "")
            {
                ApplyResize.Visibility = Visibility.Visible;
            }
            else
            {
                ApplyResize.Visibility = Visibility.Collapsed;
            }
        }

        public void OnKeepPropsUnchecked(object sender, RoutedEventArgs e)
        {
            keepProportions = false;
        }

        public void OnKeepPropsChecked(object sender, RoutedEventArgs e)
        {
            keepProportions = true;
        }

        public void Resize_Checked(object sender, RoutedEventArgs e)
        {
            ResizePanel.Visibility = Visibility.Visible;
        }

        public void Resize_Unchecked(object sender, RoutedEventArgs e)
        {
            ResizePanel.Visibility = Visibility.Collapsed;
        }

        public async void ApplyResize_Clicked(object sender, RoutedEventArgs e)
        {
            // Resize the current image.
            int a = 0;
            MessageDialog messageDialog = new MessageDialog("Please, input only numbers.", "Error");
            if (!int.TryParse(newWidth.Text, out a) || !int.TryParse(newHeight.Text, out a))
            {
                await messageDialog.ShowAsync();
                return;
            }
            int resizeWidth = Convert.ToInt32(newWidth.Text);
            int resizeHeight = Convert.ToInt32(newHeight.Text);
            int OrignalWidth = imageOriginal.width;
            int OriginalHeight = imageOriginal.height;

            ApplyResize.Visibility = Visibility.Collapsed;

            bitmapImage = await ResizeImage(bitmapImage, (uint)(resizeWidth), (uint)(resizeHeight));
            bitmapStream = bitmapImage.PixelBuffer.AsStream();
            imageOriginal.srcPixels = new byte[(uint)bitmapStream.Length];
            image.srcPixels = new byte[(uint)exampleStream.Length];
            await bitmapStream.ReadAsync(imageOriginal.srcPixels, 0, imageOriginal.srcPixels.Length);
            prepareImage(bitmapStream, bitmapImage, imageOriginal);
            setStream(bitmapStream, bitmapImage, imageOriginal);
            imageDisplayed.displayImage.Source = bitmapImage;
            ArchiveAddArray();
            effectsApplied.Add("Resize " + OrignalWidth + " " + OriginalHeight + " " + (int)resizeWidth + " " + (int)resizeHeight);
            imageDisplayed.displayImage.Source = bitmapImage;
            setFilterBitmaps();
        }

        #endregion

        #region Colorize

        #region Colorize events
        // Events for checking and unchecking the colorize rectangles.
        public void blueColorize_Checked(object sender, RoutedEventArgs e)
        {
            blueForColorize = true;
            doColorize(bitmapStream, bitmapImage, imageOriginal);
            blueRect.Fill = new SolidColorBrush(Color.FromArgb(255, 0, 0, 255));
        }
        public void blueColorize_Unchecked(object sender, RoutedEventArgs e)
        {
            blueForColorize = false;
            doColorize(bitmapStream, bitmapImage, imageOriginal);
            blueRect.Fill = new SolidColorBrush(Color.FromArgb(100, 0, 0, 255));
        }

        public void redColorize_Checked(object sender, RoutedEventArgs e)
        {
            redForColorize = true;
            doColorize(bitmapStream, bitmapImage, imageOriginal);
            redRect.Fill = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
        }
        public void redColorize_Unchecked(object sender, RoutedEventArgs e)
        {
            redForColorize = false;
            doColorize(bitmapStream, bitmapImage, imageOriginal);
            redRect.Fill = new SolidColorBrush(Color.FromArgb(100, 255, 0, 0));
        }

        public void yellowColorize_Checked(object sender, RoutedEventArgs e)
        {
            yellowForColorize = true;
            doColorize(bitmapStream, bitmapImage, imageOriginal);
            yellowRect.Fill = new SolidColorBrush(Color.FromArgb(255, 255, 255, 0));
        }
        public void yellowColorize_Unchecked(object sender, RoutedEventArgs e)
        {
            yellowForColorize = false;
            doColorize(bitmapStream, bitmapImage, imageOriginal);
            yellowRect.Fill = new SolidColorBrush(Color.FromArgb(100, 255, 255, 0));
        }

        public void orangeColorize_Checked(object sender, RoutedEventArgs e)
        {
            orangeForColorize = true;
            doColorize(bitmapStream, bitmapImage, imageOriginal);
            orangeRect.Fill = new SolidColorBrush(Color.FromArgb(255, 255, 116, 0));
        }
        public void orangeColorize_Unchecked(object sender, RoutedEventArgs e)
        {
            orangeForColorize = false;
            doColorize(bitmapStream, bitmapImage, imageOriginal);
            orangeRect.Fill = new SolidColorBrush(Color.FromArgb(100, 255, 116, 0));
        }

        public void greenColorize_Checked(object sender, RoutedEventArgs e)
        {
            greenForColorize = true;
            doColorize(bitmapStream, bitmapImage, imageOriginal);
            greenRect.Fill = new SolidColorBrush(Color.FromArgb(255, 0, 120, 0));
        }
        public void greenColorize_Unchecked(object sender, RoutedEventArgs e)
        {
            greenForColorize = false;
            doColorize(bitmapStream, bitmapImage, imageOriginal);
            greenRect.Fill = new SolidColorBrush(Color.FromArgb(100, 0, 90, 0));
        }

        public void cyanColorize_Checked(object sender, RoutedEventArgs e)
        {
            cyanForColorize = true;
            doColorize(bitmapStream, bitmapImage, imageOriginal);
            cyanRect.Fill = new SolidColorBrush(Color.FromArgb(255, 0, 255, 255));
        }
        public void cyanColorize_Unchecked(object sender, RoutedEventArgs e)
        {
            cyanForColorize = false;
            doColorize(bitmapStream, bitmapImage, imageOriginal);
            cyanRect.Fill = new SolidColorBrush(Color.FromArgb(100, 0, 255, 255));
        }

        public void purpleColorize_Checked(object sender, RoutedEventArgs e)
        {
            purpleForColorize = true;
            doColorize(bitmapStream, bitmapImage, imageOriginal);
            purpleRect.Fill = new SolidColorBrush(Color.FromArgb(255, 255, 0, 255));
        }
        public void purpleColorize_Unchecked(object sender, RoutedEventArgs e)
        {
            purpleForColorize = false;
            doColorize(bitmapStream, bitmapImage, imageOriginal);
            purpleRect.Fill = new SolidColorBrush(Color.FromArgb(100, 255, 0, 255));
        }

        public void limeColorize_Checked(object sender, RoutedEventArgs e)
        {
            limeForColorize = true;
            doColorize(bitmapStream, bitmapImage, imageOriginal);
            limeRect.Fill = new SolidColorBrush(Color.FromArgb(255, 25, 255, 25));
        }
        public void limeColorize_Unchecked(object sender, RoutedEventArgs e)
        {
            limeForColorize = false;
            doColorize(bitmapStream, bitmapImage, imageOriginal);
            limeRect.Fill = new SolidColorBrush(Color.FromArgb(100, 25, 255, 25));
        }

        #endregion

        public void deselectColorizeGridItems()
        {
            // Deselect all colorize rectangles and return their original color.
            redForColorize = greenForColorize = blueForColorize = yellowForColorize =
                 orangeForColorize = purpleForColorize = cyanForColorize =
                 limeForColorize = false;
            blueColorize.IsChecked = false;
            redColorize.IsChecked = false;
            greenColorize.IsChecked = false;
            yellowColorize.IsChecked = false;
            orangeColorize.IsChecked = false;
            purpleColorize.IsChecked = false;
            cyanColorize.IsChecked = false;
            limeColorize.IsChecked = false;
            blueRect.Fill = new SolidColorBrush(Color.FromArgb(100, 0, 0, 255));
            redRect.Fill = new SolidColorBrush(Color.FromArgb(100, 255, 0, 0));
            greenRect.Fill = new SolidColorBrush(Color.FromArgb(100, 0, 90, 0));
            yellowRect.Fill = new SolidColorBrush(Color.FromArgb(100, 255, 255, 0));
            orangeRect.Fill = new SolidColorBrush(Color.FromArgb(100, 255, 116, 0));
            purpleRect.Fill = new SolidColorBrush(Color.FromArgb(100, 255, 0, 255));
            cyanRect.Fill = new SolidColorBrush(Color.FromArgb(100, 0, 255, 255));
            limeRect.Fill = new SolidColorBrush(Color.FromArgb(100, 25, 255, 25));
            ColorizeApplyReset.Visibility = Visibility.Collapsed;
        }

        public void doColorize(Stream stream, WriteableBitmap bitmap, RemedyImage givenImage)
        {
            // Call the colorize function
            prepareImage(stream, bitmap, givenImage);
            givenImage.Colorize(blueForColorize, redForColorize, greenForColorize, yellowForColorize,
                                        orangeForColorize, purpleForColorize, cyanForColorize, limeForColorize);
            setStream(stream, bitmap, givenImage);
            ColorizeApplyReset.Visibility = Visibility.Visible;
        }

        #endregion

        public void FramesBorder_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            // This event completes when the mouse pointer enter the frame border.
            var borderSender = sender as Border;
            borderSender.BorderBrush = new Windows.UI.Xaml.Media.SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
        }

        public void FramesBorder_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            // This event completes when the mouse pointer exit the frame border.
            var borderSender = sender as Border;
            borderSender.BorderBrush = new Windows.UI.Xaml.Media.SolidColorBrush(Color.FromArgb(255, 25, 112, 0));
        }


        #region Custom Filter

        public void OnCoeffChanged(object sender, TextChangedEventArgs text)
        {
            CustomApplyReset.Visibility = Visibility.Visible;
            TextBox val = (TextBox)text.OriginalSource;
            if (val != null)
            {
                CustomFilter_CheckValue(ref val);
            }
            CustomFilter_CalculateScaleOffset();
        }

        // SCALE AND OFFSET
        public void OnScaleChanged(object sender, TextChangedEventArgs text)
        {
            CustomApplyReset.Visibility = Visibility.Visible;
            CustomFilter_CheckScale();
        }

        public void OnOffsetChanged(object sender, TextChangedEventArgs text)
        {
            CustomApplyReset.Visibility = Visibility.Visible;
            CustomFilter_CheckValue(ref Offset);
        }

        // Check if the value of text box is number and set scale and offset
        public void CustomFilter_CheckValue(ref TextBox coeff)
        {
            try
            {
                Convert.ToInt32(coeff.Text);
            }
            catch (FormatException e)
            {
                if (!(coeff.Text.Length == 1 && coeff.Text[0] == '-'))
                    coeff.Text = "";
            }
        }

        // Check if the scale is > 0
        public void CustomFilter_CheckScale()
        {
            try
            {
                int val = Convert.ToInt32(Scale.Text);

                if (val == 0)
                    Scale.Text = "";
            }
            catch (FormatException e)
            {
                Scale.Text = "";
            }
        }

        //Calculate the Scale and offset
        public void CustomFilter_CalculateScaleOffset()
        {
            int[,] coeff = new int[5, 5];
            int offset = 0, scale = 1;
            CustomFilter_SetValues(ref coeff, ref offset, ref scale);
            int sum = 0;

            foreach (int val in coeff)
            {
                sum += val;
            }

            if (sum != 0)
                scale = Math.Abs(sum);
            else
                scale = 1;

            if (sum / scale == 1)
                offset = 0;
            else if (sum == 0)
                offset = 128;
            else if (sum / scale == -1)
                offset = 255;

            Scale.Text = scale.ToString();
            Offset.Text = offset.ToString();
        }

        // Review button click
        public void OnCustomReviewClick(object sender, RoutedEventArgs e)
        {
            CustomReview();
        }

        public void CustomReview()
        {
            ImageLoadingRing.IsActive = true;

            if (pictureIsLoaded)
            {
                prepareImage(bitmapStream, bitmapImage, imageOriginal);
                int[,] coeff = new int[5, 5];
                int offset = 0, scale = 0;
                CustomFilter_SetValues(ref coeff, ref offset, ref scale);

                CustomFilter custom_image = new CustomFilter(imageOriginal.srcPixels, imageOriginal.width, imageOriginal.height, offset, scale, coeff);
                imageOriginal.dstPixels = custom_image.Filter();

                setStream(bitmapStream, bitmapImage, imageOriginal);
            }

            ImageLoadingRing.IsActive = false;
        }

        // Apply button click
        public void OnCustomApplyClick(object sender, RoutedEventArgs e)
        {
            CustomApply();
            CustomApplyReset.Visibility = Visibility.Collapsed;
            setExampleBitmaps();
            setFilterBitmaps();
            Saved = false;
        }

        public void CustomApply()
        {
            ImageLoadingRing.IsActive = true;

            if (pictureIsLoaded)
            {
                prepareImage(bitmapStream, bitmapImage, imageOriginal);
                int[,] coeff = new int[5, 5];
                int offset = 0, scale = 0;
                CustomFilter_SetValues(ref coeff, ref offset, ref scale);

                CustomFilter custom_image = new CustomFilter(imageOriginal.srcPixels, imageOriginal.width, imageOriginal.height, offset, scale, coeff);
                imageOriginal.dstPixels = custom_image.Filter();

                setStream(bitmapStream, bitmapImage, imageOriginal);
            }

            imageOriginal.srcPixels = (byte[])imageOriginal.dstPixels.Clone();
            ArchiveAddArray();
            effectsApplied.Add("Custom = " + "TODO");
            CustomFilter_ResetValues();
            ImageLoadingRing.IsActive = false;
        }

        // Reset button click
        public void OnCustomResetClick(object sender, RoutedEventArgs e)
        {
            CustomFilter_ResetValues();

            if (pictureIsLoaded)
            {
                prepareImage(bitmapStream, bitmapImage, imageOriginal);
                imageOriginal.dstPixels = (byte[])imageOriginal.srcPixels.Clone();
                setStream(bitmapStream, bitmapImage, imageOriginal);
            }
            CustomApplyReset.Visibility = Visibility.Collapsed;
        }

        // Reset All values of custom filter
        public void CustomFilter_ResetValues()
        {
            coeff00.Text = coeff10.Text = coeff20.Text = coeff30.Text = coeff40.Text = "";
            coeff01.Text = coeff11.Text = coeff21.Text = coeff31.Text = coeff41.Text = "";
            coeff02.Text = coeff12.Text = coeff32.Text = coeff42.Text = "";
            coeff03.Text = coeff13.Text = coeff23.Text = coeff33.Text = coeff43.Text = "";
            coeff04.Text = coeff14.Text = coeff24.Text = coeff34.Text = coeff44.Text = "";
            coeff22.Text = "1";

            Scale.Text = "1";
            Offset.Text = "0";
        }

        // Set the matrix for custom filter
        public void CustomFilter_SetValues(ref int[,] coeff, ref int offset, ref int scale)
        {
            CustomFilter_SetValue(ref coeff[0, 0], coeff00);
            CustomFilter_SetValue(ref coeff[1, 0], coeff10);
            CustomFilter_SetValue(ref coeff[2, 0], coeff20);
            CustomFilter_SetValue(ref coeff[3, 0], coeff30);
            CustomFilter_SetValue(ref coeff[4, 0], coeff40);

            CustomFilter_SetValue(ref coeff[0, 1], coeff01);
            CustomFilter_SetValue(ref coeff[1, 1], coeff11);
            CustomFilter_SetValue(ref coeff[2, 1], coeff21);
            CustomFilter_SetValue(ref coeff[3, 1], coeff31);
            CustomFilter_SetValue(ref coeff[4, 1], coeff41);

            CustomFilter_SetValue(ref coeff[0, 2], coeff02);
            CustomFilter_SetValue(ref coeff[1, 2], coeff12);
            CustomFilter_SetValue(ref coeff[2, 2], coeff22);
            CustomFilter_SetValue(ref coeff[3, 2], coeff32);
            CustomFilter_SetValue(ref coeff[4, 2], coeff42);

            CustomFilter_SetValue(ref coeff[0, 3], coeff03);
            CustomFilter_SetValue(ref coeff[1, 3], coeff13);
            CustomFilter_SetValue(ref coeff[2, 3], coeff23);
            CustomFilter_SetValue(ref coeff[3, 3], coeff33);
            CustomFilter_SetValue(ref coeff[4, 3], coeff43);

            CustomFilter_SetValue(ref coeff[0, 4], coeff04);
            CustomFilter_SetValue(ref coeff[1, 4], coeff14);
            CustomFilter_SetValue(ref coeff[2, 4], coeff24);
            CustomFilter_SetValue(ref coeff[3, 4], coeff34);
            CustomFilter_SetValue(ref coeff[4, 4], coeff44);

            CustomFilter_SetScale(ref scale);
            CustomFilter_SetValue(ref offset, Offset);
        }

        // Set one coeff of matrix
        public void CustomFilter_SetValue(ref int coeff, TextBox val)
        {
            int new_val = 0;
            try
            {
                new_val = Convert.ToInt32(val.Text);
            }
            catch (FormatException e)
            {
                if (!(val.Text.Length == 1 && val.Text[0] == '-'))
                    val.Text = "";
            }

            if (new_val != 0)
                coeff = new_val;
        }

        // Set the scale value of custom filter
        public void CustomFilter_SetScale(ref int scale)
        {
            int new_val = 0;
            try
            {
                new_val = Convert.ToInt32(Scale.Text);

                if (new_val <= 0)
                {
                    Scale.Text = "";
                    scale = 1;
                }
            }
            catch (FormatException e)
            {
                Scale.Text = "";
                scale = 1;
            }

            if (new_val > 0)
                scale = new_val;
        }

        #endregion


        #region Export/Import
        public void OnExportButtonClick(object sender, RoutedEventArgs e)
        {
            if (archive_current_index != archive_data.Count - 1)
            {
                archive_data.RemoveRange(archive_current_index + 1, archive_data.Count - 1 - archive_current_index);
                effectsApplied.RemoveRange(archive_current_index, effectsApplied.Count - archive_current_index); // Here we don`t save the start image, so we have -1 index of archive_current_index
            }
            configFile.Export(effectsApplied);
        }

        public void onImportButtonClick(object sender, RoutedEventArgs e)
        {
            if (archive_current_index != archive_data.Count - 1)
            {
                archive_data.RemoveRange(archive_current_index + 1, archive_data.Count - 1 - archive_current_index);
                effectsApplied.RemoveRange(archive_current_index, effectsApplied.Count - archive_current_index); // Here we don`t save the start image, so we have -1 index of archive_current_index
            }

            ImageLoadingRing.IsActive = true;
            DarkenBorder.Visibility = Visibility.Visible;

            for (int i = 0; i < configFile.effects.Count; i += 2)
            {
                checkEffect(i);
            }
            setFilterBitmaps();
            ImageLoadingRing.IsActive = false;
            DarkenBorder.Visibility = Visibility.Collapsed;
        }

        public void checkEffect(int i)
        {
            string[] temp = new string[10];
            switch (configFile.effects[i])
            {
                case "Filter":
                    ApplyFilter(configFile.effects[i + 1]);
                    break;

                case "Color":
                    temp = configFile.effects[i + 1].Split(',');
                    importColor(temp);
                    break;

                case "Contrast":
                    temp = configFile.effects[i + 1].Split(',');
                    importContrast(temp);
                    break;

                case "Exposure":
                    temp = configFile.effects[i + 1].Split(',');
                    importExposure(temp);
                    break;

                case "Flip":
                    temp = configFile.effects[i + 1].Split(',');
                    ApplyRotate(temp[0]);
                    break;

                case "Colorize":
                    temp = configFile.effects[i + 1].Split(',');
                    importColorize(temp);
                    break;

                case "Frame":
                    temp = configFile.effects[i + 1].Split(',');
                    checkAndApplyFrames(temp);
                    imageOriginal.srcPixels = (byte[])imageOriginal.dstPixels.Clone();
                    break;

                case "Rotate":
                    temp = configFile.effects[i + 1].Split(',');
                    RotateBitmap(temp[0]);
                    break;

                case "Histogram":
                    if (configFile.effects[i + 1] == "true")
                    {
                        equalizeHistogram();
                    }
                    break;
                default: break;
            }
        }

        #region Import Functions
        public void importColor(string[] temp)
        {
            ColorsPopup.BlueColorSlider.Value = Convert.ToDouble(temp[0]);
            ColorsPopup.GreenColorSlider.Value = Convert.ToDouble(temp[1]);
            ColorsPopup.RedColorSlider.Value = Convert.ToDouble(temp[2]);
            ColorsPopup.ApplyColor();
        }

        public void importContrast(string[] temp)
        {
            ColorsPopup.BlueContrastSlider.Value = Convert.ToDouble(temp[0]);
            ColorsPopup.GreenContrastSlider.Value = Convert.ToDouble(temp[1]);
            ColorsPopup.RedContrastSlider.Value = Convert.ToDouble(temp[2]);
            ColorsPopup.ApplyColor();
        }

        public void importExposure(string[] temp)
        {
            brightSlider.Value = Convert.ToDouble(temp[0]);
            BlueGammaSlider.Value = Convert.ToDouble(temp[1]);
            GreenGammaSlider.Value = Convert.ToDouble(temp[2]);
            RedGammaSlider.Value = Convert.ToDouble(temp[3]);
            if (brightSlider.Value < 0)
                ApplyExposure("gammadarken");
            else
                ApplyExposure("gammalighten");
        }

        public void importColorize(string[] temp)
        {
            for (int k = 0; k < temp.Length; k++)
            {
                checkColorizeColor(temp[k]);
            }
            doColorize(bitmapStream, bitmapImage, imageOriginal);
            ApplyColorize();
        }
        #endregion

        public void checkColorizeColor(string color)
        {
            switch (color)
            {
                case "red":
                    redForColorize = true;
                    redRect.Fill = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    break;
                case "blue":
                    blueForColorize = true;
                    blueRect.Fill = new SolidColorBrush(Color.FromArgb(255, 0, 0, 255));
                    break;
                case "green":
                    greenForColorize = true;
                    greenRect.Fill = new SolidColorBrush(Color.FromArgb(255, 0, 120, 0));
                    break;
                case "lime":
                    limeForColorize = true;
                    limeRect.Fill = new SolidColorBrush(Color.FromArgb(255, 25, 255, 25));
                    break;
                case "yellow":
                    yellowForColorize = true;
                    yellowRect.Fill = new SolidColorBrush(Color.FromArgb(255, 255, 255, 0));
                    break;
                case "cyan":
                    cyanForColorize = true;
                    cyanRect.Fill = new SolidColorBrush(Color.FromArgb(255, 0, 255, 255));
                    break;
                case "orange":
                    orangeForColorize = true;
                    orangeRect.Fill = new SolidColorBrush(Color.FromArgb(255, 255, 116, 0));
                    break;
                case "purple":
                    purpleForColorize = true;
                    purpleRect.Fill = new SolidColorBrush(Color.FromArgb(255, 255, 0, 255));
                    break;
                default:
                    break;
            }
        }

        public void checkAndApplyFrames(string[] frameStats)
        {
            int thickPercent = Convert.ToInt32(frameStats[0]);
            appliedFrameColor = frameStats[1];
            string frameType = frameStats[2];

            switch (frameType)
            {
                case "standard":
                    ApplyStandardFrame(thickPercent);
                    break;
                case "standard up down":
                    ApplyStandartUpDownFrame(thickPercent);
                    break;
                case "standard left right":
                    ApplyStandardLeftRightFrame(thickPercent);
                    break;
                case "darkness":
                    ApplyDarknessFrame(thickPercent);
                    break;
                case "darkness left right":
                    ApplyDarknessLeftRightFrame(thickPercent);
                    break;
                case "darkness up down":
                    ApplyDarknessUpDownFrame(thickPercent);
                    break;
                case "smooth darkness":
                    ApplySmoothDarknessFrame(thickPercent);
                    break;
                case "standard angle":
                    ApplyStandardAngleFrame(thickPercent);
                    break;
                case "angle":
                    ApplyAngleFrame(thickPercent);
                    break;
                default:
                    break;
            }
        }

        public async void onImportFileSelectButtonClick(object sender, RoutedEventArgs e)
        {
            bool imported = await configFile.Import(importFileName);
            if (imported)
                importFilePanel.Visibility = Visibility.Visible;
            else if (configFile == null)
                importFilePanel.Visibility = Visibility.Collapsed;
        }
        #endregion


        public void OnCancelSaveClicked(object sender, RoutedEventArgs e)
        {
            notSaved.IsOpen = false;
            DarkenBorder.Visibility = Visibility.Collapsed;
        }

        public async void OnSaveClicked(object sender, RoutedEventArgs e)
        {
            OnCancelSaveClicked(sender, e);
            if (e.OriginalSource.Equals(YesSave))
            {
                await SaveFile(true);
            }
            if (PopupCalledBy == "Browse")
            {
                Panel.GetPhoto();
            }
            else if (PopupCalledBy == "Camera")
            {
                Panel.getCameraPhoto();
            }
        }

        public async void setExampleBitmaps()
        {
            exampleBitmap = await ResizeImage(bitmapImage, (uint)(bitmapImage.PixelWidth / 5), (uint)(bitmapImage.PixelHeight / 5));

            exampleStream = exampleBitmap.PixelBuffer.AsStream();
            image.srcPixels = new byte[(uint)exampleStream.Length];
            await exampleStream.ReadAsync(image.srcPixels, 0, image.srcPixels.Length);

            prepareImage(exampleStream, exampleBitmap, image);
            setStream(exampleStream, exampleBitmap, image);
            setElements(ColorsPopup.ColorsExamplePicture, exampleBitmap);
            setElements(RotationsExamplePicture, exampleBitmap);
            setElements(ExposureExamplePicture, exampleBitmap);
        }

    }
    #endregion
}
#endregion
