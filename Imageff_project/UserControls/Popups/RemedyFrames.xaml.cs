﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace RemedyPic.UserControls.Popups
{
    public sealed partial class RemedyFrames : UserControl
    {
        MainPage rootPage = MainPage.Current;

        public string appliedFrame = null, appliedFrameColor = null;

        public RemedyFrames()
        {
            this.InitializeComponent();
        }


        // The events are called when a frame button is clicked.


        // Set standard frame to the image
        public void OnStandardClick(object sender, RoutedEventArgs e)
        {
            FramesApplyReset.Visibility = Visibility.Visible;

            if (rootPage.pictureIsLoaded)
            {
                appliedFrame = "standard";
                ApplyStandardFrame((int)FrameWidthPercent.Value);
            }
        }

        public void ApplyStandardFrame(int thick)
        {
            rootPage.prepareImage(rootPage.bitmapStream, rootPage.bitmapImage, rootPage.imageOriginal);
            rootPage.imageOriginal.dstPixels = (byte[])rootPage.imageOriginal.srcPixels.Clone();
            rootPage.imageOriginal.Frames_StandardLeftSide(Frame_GetFrameColor(), thick);
            rootPage.imageOriginal.Frames_StandardTopSide(Frame_GetFrameColor(), thick);
            rootPage.imageOriginal.Frames_StandardRightSide(Frame_GetFrameColor(), thick);
            rootPage.imageOriginal.Frames_StandardBottomSide(Frame_GetFrameColor(), thick);
            rootPage.setStream(rootPage.bitmapStream, rootPage.bitmapImage,rootPage.imageOriginal);
        }

        // Set standard frame (only UP or DOWN) to the image
        public void OnStandardUpDownClick(object sender, RoutedEventArgs e)
        {
            FramesApplyReset.Visibility = Visibility.Visible;

            if (rootPage.pictureIsLoaded)
            {
                appliedFrame = "standard up down";
                ApplyStandartUpDownFrame((int)FrameWidthPercent.Value);
            }
        }

        public void ApplyStandartUpDownFrame(int thick)
        {
            rootPage.prepareImage(rootPage.bitmapStream, rootPage.bitmapImage,rootPage.imageOriginal);
            rootPage.imageOriginal.dstPixels = (byte[])rootPage.imageOriginal.srcPixels.Clone();
            rootPage.imageOriginal.Frames_StandardTopSide(Frame_GetFrameColor(), thick);
            rootPage.imageOriginal.Frames_StandardBottomSide(Frame_GetFrameColor(), thick);
            rootPage.setStream(rootPage.bitmapStream, rootPage.bitmapImage,rootPage.imageOriginal);
        }

        // Set standard frame (only LEFT or RIGHT) to the image
        public void OnStandardLeftRightClick(object sender, RoutedEventArgs e)
        {
            FramesApplyReset.Visibility = Visibility.Visible;

            if (rootPage.pictureIsLoaded)
            {
                appliedFrame = "standard left right";
                ApplyStandardLeftRightFrame((int)FrameWidthPercent.Value);
            }
        }

        public void ApplyStandardLeftRightFrame(int thick)
        {
            rootPage.prepareImage(rootPage.bitmapStream, rootPage.bitmapImage,rootPage.imageOriginal);
            rootPage.imageOriginal.dstPixels = (byte[])rootPage.imageOriginal.srcPixels.Clone();
            rootPage.imageOriginal.Frames_StandardLeftSide(Frame_GetFrameColor(), thick);
            rootPage.imageOriginal.Frames_StandardRightSide(Frame_GetFrameColor(), thick);
            rootPage.setStream(rootPage.bitmapStream, rootPage.bitmapImage,rootPage.imageOriginal);
        }

        // Set darkness frame to the image
        public void OnDarknessClick(object sender, RoutedEventArgs e)
        {
            FramesApplyReset.Visibility = Visibility.Visible;

            if (rootPage.pictureIsLoaded)
            {
                appliedFrame = "darkness";
                ApplyDarknessFrame((int)FrameWidthPercent.Value);
            }
        }

        public void ApplyDarknessFrame(int thick)
        {
            rootPage.prepareImage(rootPage.bitmapStream, rootPage.bitmapImage,rootPage.imageOriginal);
            rootPage.imageOriginal.dstPixels = (byte[])rootPage.imageOriginal.srcPixels.Clone();
            rootPage.imageOriginal.Frames_DarknessLeftSide(thick);
            rootPage.imageOriginal.Frames_DarknessTopSide(thick);
            rootPage.imageOriginal.Frames_DarknessRightSide(thick);
            rootPage.imageOriginal.Frames_DarknessBottomSide(thick);
            rootPage.setStream(rootPage.bitmapStream, rootPage.bitmapImage,rootPage.imageOriginal);
        }

        // Set darkness frame (only left or right) to the image
        public void OnDarknessLeftRightClick(object sender, RoutedEventArgs e)
        {
            FramesApplyReset.Visibility = Visibility.Visible;

            if (rootPage.pictureIsLoaded)
            {
                appliedFrame = "darkness left right";
                ApplyDarknessLeftRightFrame((int)FrameWidthPercent.Value);
            }
        }

        public void ApplyDarknessLeftRightFrame(int thick)
        {
            rootPage.prepareImage(rootPage.bitmapStream, rootPage.bitmapImage,rootPage.imageOriginal);
            rootPage.imageOriginal.dstPixels = (byte[])rootPage.imageOriginal.srcPixels.Clone();
            rootPage.imageOriginal.Frames_DarknessLeftSide(thick);
            rootPage.imageOriginal.Frames_DarknessRightSide(thick);
            rootPage.setStream(rootPage.bitmapStream, rootPage.bitmapImage,rootPage.imageOriginal);
        }

        // Set darkness frame (only up or down) to the image
        public void OnDarknessUpDownSidesClick(object sender, RoutedEventArgs e)
        {
            FramesApplyReset.Visibility = Visibility.Visible;

            if (rootPage.pictureIsLoaded)
            {
                appliedFrame = "darkness up down";
                ApplyDarknessUpDownFrame((int)FrameWidthPercent.Value);
            }
        }

        public void ApplyDarknessUpDownFrame(int thick)
        {
            rootPage.prepareImage(rootPage.bitmapStream, rootPage.bitmapImage,rootPage.imageOriginal);
            rootPage.imageOriginal.dstPixels = (byte[])rootPage.imageOriginal.srcPixels.Clone();
            rootPage.imageOriginal.Frames_DarknessTopSide(thick);
            rootPage.imageOriginal.Frames_DarknessBottomSide(thick);
            rootPage.setStream(rootPage.bitmapStream, rootPage.bitmapImage,rootPage.imageOriginal);
        }

        // Set smooth darkness frame to the image
        public void OnSmoothDarknessClick(object sender, RoutedEventArgs e)
        {
            FramesApplyReset.Visibility = Visibility.Visible;

            if (rootPage.pictureIsLoaded)
            {
                appliedFrame = "smooth darkness";
                ApplySmoothDarknessFrame((int)FrameWidthPercent.Value);
            }
        }

        public void ApplySmoothDarknessFrame(int thick)
        {
            rootPage.prepareImage(rootPage.bitmapStream, rootPage.bitmapImage,rootPage.imageOriginal);
            rootPage.imageOriginal.dstPixels = (byte[])rootPage.imageOriginal.srcPixels.Clone();
            rootPage.imageOriginal.Frames_SmoothDarkness(thick);
            rootPage.setStream(rootPage.bitmapStream, rootPage.bitmapImage,rootPage.imageOriginal);
        }

        // Set standard frame with smooth angles to the image
        public void OnStandardAngleClick(object sender, RoutedEventArgs e)
        {
            FramesApplyReset.Visibility = Visibility.Visible;

            if (rootPage.pictureIsLoaded)
            {
                appliedFrame = "standard angle";
                ApplyStandardAngleFrame((int)FrameWidthPercent.Value);
            }
        }

        public void ApplyStandardAngleFrame(int thick)
        {
            rootPage.prepareImage(rootPage.bitmapStream, rootPage.bitmapImage,rootPage.imageOriginal);
            rootPage.imageOriginal.dstPixels = (byte[])rootPage.imageOriginal.srcPixels.Clone();
            rootPage.imageOriginal.Frames_StandardLeftSide(Frame_GetFrameColor(), thick);
            rootPage.imageOriginal.Frames_StandardTopSide(Frame_GetFrameColor(), thick);
            rootPage.imageOriginal.Frames_StandardRightSide(Frame_GetFrameColor(), thick);
            rootPage.imageOriginal.Frames_StandardBottomSide(Frame_GetFrameColor(), thick);
            rootPage.imageOriginal.Frames_StandartAngle(Frame_GetFrameColor(), thick);
            rootPage.setStream(rootPage.bitmapStream, rootPage.bitmapImage,rootPage.imageOriginal);
        }

        // Set smooth angles frame to the image
        public void OnAngleClick(object sender, RoutedEventArgs e)
        {
            FramesApplyReset.Visibility = Visibility.Visible;

            if (rootPage.pictureIsLoaded)
            {
                appliedFrame = "angle";
                ApplyAngleFrame((int)FrameWidthPercent.Value);
            }
        }

        public void ApplyAngleFrame(int thick)
        {
            rootPage.prepareImage(rootPage.bitmapStream, rootPage.bitmapImage,rootPage.imageOriginal);
            rootPage.imageOriginal.dstPixels = (byte[])rootPage.imageOriginal.srcPixels.Clone();
            rootPage.imageOriginal.Frames_Angle(Frame_GetFrameColor(), thick);
            rootPage.setStream(rootPage.bitmapStream, rootPage.bitmapImage,rootPage.imageOriginal);
        }

        // Apply the frame on the image
        public void OnApplyFramesClick(object sender, RoutedEventArgs e)
        {
            rootPage.imageOriginal.srcPixels = (byte[])rootPage.imageOriginal.dstPixels.Clone();
            rootPage.Panel.ArchiveAddArray();
			rootPage.OptionsPopup.effectsApplied.Add("Frame = " + FrameWidthPercent.Value + "," + appliedFrameColor + "," + appliedFrame);
            rootPage.setExampleBitmaps();
            rootPage.FiltersPopup.setFilterBitmaps();
            FramesApplyReset.Visibility = Visibility.Collapsed;
            ResetFrameMenuData();
            rootPage.Saved = false;
        }

        // Reset the image (return the pixels before applying the frame)
        public void OnResetFramesClick(object sender, RoutedEventArgs e)
        {
            if (rootPage.pictureIsLoaded)
            {
                ResetFrameMenuData();
                rootPage.prepareImage(rootPage.bitmapStream, rootPage.bitmapImage,rootPage.imageOriginal);
                rootPage.imageOriginal.Reset();
                rootPage.setStream(rootPage.bitmapStream, rootPage.bitmapImage,rootPage.imageOriginal);
            }

            FramesApplyReset.Visibility = Visibility.Collapsed;
        }

        // When frame width is changed
        public void OnFrameWidthChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            ReDisplay();
        }

        private void ReDisplay()
        {
            if (appliedFrame != null)
            {
                ApplyFrameType(appliedFrame, (int)FrameWidthPercent.Value);
            }
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

        public void checkAndApplyFrames(string[] frameStats)
        {
            int thickPercent = Convert.ToInt32(frameStats[0]);
            appliedFrameColor = frameStats[1];
            string FrameType = frameStats[2];

            ApplyFrameType(FrameType, thickPercent);
        }

        private void ApplyFrameType(string FrameType, int thickPercent)
        {
            switch (FrameType)
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

        // Reset the data of Frame menu
        public void ResetFrameMenuData()
        {
            appliedFrameColor = "black";
            appliedFrame = null;
            BlackFrameColor.IsSelected = true;
            FrameWidthPercent.Value = 1;
        }

        public void BackPopupClicked(object sender, RoutedEventArgs e)
        {
            rootPage.BackPopupClicked(sender, e);
        }

        // When color is changed
        private void SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (rootPage.pictureIsLoaded)
            {
                GetSelectedFrameColor();
                ReDisplay();
            }
        }

        // Get selected color of the frame
        private void GetSelectedFrameColor()
        {
            if (BlackFrameColor.IsSelected == true)
            {
                appliedFrameColor = "black";
            }
            else if (GrayFrameColor.IsSelected == true)
            {
                appliedFrameColor = "gray";
            }
            else if (WhiteFrameColor.IsSelected == true)
            {
                appliedFrameColor = "white";
            }
            else if (BlueFrameColor.IsSelected == true)
            {
                appliedFrameColor = "blue";
            }
            else if (LimeFrameColor.IsSelected == true)
            {
                appliedFrameColor = "lime";
            }
            else if (YellowFrameColor.IsSelected == true)
            {
                appliedFrameColor = "yellow";
            }
            else if (CyanFrameColor.IsSelected == true)
            {
                appliedFrameColor = "cyan";
            }
            else if (MagentaFrameColor.IsSelected == true)
            {
                appliedFrameColor = "magenta";
            }
            else if (SilverFrameColor.IsSelected == true)
            {
                appliedFrameColor = "silver";
            }
            else if (MaroonFrameColor.IsSelected == true)
            {
                appliedFrameColor = "maroon";
            }
            else if (OliveFrameColor.IsSelected == true)
            {
                appliedFrameColor = "olive";
            }
            else if (GreenFrameColor.IsSelected == true)
            {
                appliedFrameColor = "green";
            }
            else if (PurpleFrameColor.IsSelected == true)
            {
                appliedFrameColor = "purple";
            }
            else if (TealFrameColor.IsSelected == true)
            {
                appliedFrameColor = "teal";
            }
            else if (NavyFrameColor.IsSelected == true)
            {
                appliedFrameColor = "navy";
            }
            else if (RedFrameColor.IsSelected == true)
            {
                appliedFrameColor = "red";
            }
        }
        
    }
}
