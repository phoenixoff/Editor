﻿

#pragma checksum "C:\Users\Phoenix\Editor\Imageff_project\MainPage.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "B9A81BEDD6308BFB76074288B1726D51"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace RemedyPic
{
    partial class MainPage : global::RemedyPic.Common.LayoutAwarePage, global::Windows.UI.Xaml.Markup.IComponentConnector
    {
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Windows.UI.Xaml.Build.Tasks"," 4.0.0.0")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
 
        public void Connect(int connectionId, object target)
        {
            switch(connectionId)
            {
            case 1:
                #line 288 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.OnBlackWhiteClick;
                 #line default
                 #line hidden
                break;
            case 2:
                #line 289 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.OnInvertClick;
                 #line default
                 #line hidden
                break;
            case 3:
                #line 290 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.OnEmbossClick;
                 #line default
                 #line hidden
                break;
            case 4:
                #line 291 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.OnSharpenClick;
                 #line default
                 #line hidden
                break;
            case 5:
                #line 292 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.OnBlurClick;
                 #line default
                 #line hidden
                break;
            case 6:
                #line 295 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.RangeBase)(target)).ValueChanged += this.OnValueChanged;
                 #line default
                 #line hidden
                break;
            case 7:
                #line 306 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.RangeBase)(target)).ValueChanged += this.OnRColorChanged;
                 #line default
                 #line hidden
                break;
            case 8:
                #line 307 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.RangeBase)(target)).ValueChanged += this.OnGColorChanged;
                 #line default
                 #line hidden
                break;
            case 9:
                #line 308 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.RangeBase)(target)).ValueChanged += this.OnBColorChanged;
                 #line default
                 #line hidden
                break;
            case 10:
                #line 310 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.OnApplyClick;
                 #line default
                 #line hidden
                break;
            case 11:
                #line 311 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.OnResetClick;
                 #line default
                 #line hidden
                break;
            case 12:
                #line 269 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.GetPhotoButton_Click;
                 #line default
                 #line hidden
                break;
            case 13:
                #line 270 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.OnSaveButtonClick;
                 #line default
                 #line hidden
                break;
            }
            this._contentLoaded = true;
        }
    }
}


