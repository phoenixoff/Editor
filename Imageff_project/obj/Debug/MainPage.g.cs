﻿

#pragma checksum "C:\Users\Phoenix\My Documents\GitHub\Editor\Imageff_project\MainPage.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "F2363CDA624A5596EB6F85B8505FDAFF"
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
                #line 10 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.FrameworkElement)(target)).SizeChanged += this.PageSizeChanged;
                 #line default
                 #line hidden
                break;
            case 2:
                #line 17 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.UIElement)(target)).PointerReleased += this.ImagePointerReleased;
                 #line default
                 #line hidden
                #line 17 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.UIElement)(target)).DoubleTapped += this.GridDoubleTapped;
                 #line default
                 #line hidden
                break;
            case 3:
                #line 234 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.OnSaveClicked;
                 #line default
                 #line hidden
                break;
            case 4:
                #line 235 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.OnSaveClicked;
                 #line default
                 #line hidden
                break;
            case 5:
                #line 236 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.OnCancelSaveClicked;
                 #line default
                 #line hidden
                break;
            case 6:
                #line 191 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.BackFeedbackClicked;
                 #line default
                 #line hidden
                break;
            case 7:
                #line 136 "..\..\MainPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.BackFeedbackClicked;
                 #line default
                 #line hidden
                break;
            }
            this._contentLoaded = true;
        }
    }
}


