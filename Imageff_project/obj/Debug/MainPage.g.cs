﻿

#pragma checksum "C:\Users\Phoenix\Editor\Imageff_project\MainPage.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "7A152797F03986CCA3D6941D7E2E818A"
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
                #line 243 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.OnBlackWhiteClick;
                 #line default
                 #line hidden
                break;
            case 2:
                #line 244 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.OnInvertClick;
                 #line default
                 #line hidden
                break;
            case 3:
                #line 246 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.RangeBase)(target)).ValueChanged += this.OnSliderChanged;
                 #line default
                 #line hidden
                break;
            case 4:
                #line 222 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.GetPhotoButton_Click;
                 #line default
                 #line hidden
                break;
            case 5:
                #line 223 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.OnSaveButtonClick;
                 #line default
                 #line hidden
                break;
            }
            this._contentLoaded = true;
        }
    }
}


