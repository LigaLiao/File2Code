using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Composition;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace File2Code
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private Windows.Storage.StorageFile inputFile;
        private Windows.Storage.StorageFile outputFile;
        BackgroundWorker BGWorker;
        public MainPage()
        {
            this.InitializeComponent();
            Windows.UI.ViewManagement.ApplicationView AW = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView();
            AW.SetPreferredMinSize(new Size(500, 400));
            BGWorker = new BackgroundWorker();
            BGWorker.DoWork += _DoWork;
        }

        private async void _DoWork(object sender, DoWorkEventArgs e)
        {
            //await new MessageDialog(inputFile.Name.Remove(inputFile.Name.IndexOf('.'))).ShowAsync();

            try
            {
                var buffer = await Windows.Storage.FileIO.ReadBufferAsync(inputFile);

                StringBuilder txt = new StringBuilder();
                uint lv = 0;
                ulong nv = 0;
                txt.EnsureCapacity(1024 * 1024 * 64);

                txt.Append("static const unsigned char " + inputFile.Name.Remove(inputFile.Name.IndexOf('.')) + "[]  = {");

                for (uint i = 0; i < buffer.Length; i++)
                {
                    if (i % 10 == 0)
                    {
                        txt.Append("\r\n    ");
                    }
                    if ((i + 1) == buffer.Length)
                    {

                        txt.Append("0x" + buffer.GetByte(i).ToString("X2"));
                    }
                    else
                    {
                        txt.Append("0x" + buffer.GetByte(i).ToString("X2") + ", ");
                    }


                    nv = (((ulong)i) * 100 / ((ulong)buffer.Length) + 1);
                    if (lv < nv)
                    {
                        await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            lv = (uint)nv;
                            PB.Value = lv;
                        });
                    }
                }
                txt.Append("\r\n};");
                await Windows.Storage.FileIO.WriteTextAsync(outputFile, txt.ToString());
                outputFile = null;
                inputFile = null;
            }
            catch (Exception ex)
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    new MessageDialog(ex.Message).ShowAsync();
                });
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (!BGWorker.IsBusy)
            {
                try
                {
                    PB.Value = 0;

                    var openPicker = new Windows.Storage.Pickers.FileOpenPicker();
                    openPicker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
                    openPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.ComputerFolder;
                    openPicker.FileTypeFilter.Add("*");
                    inputFile = await openPicker.PickSingleFileAsync();

                    var savePicker = new Windows.Storage.Pickers.FileSavePicker();
                    savePicker.DefaultFileExtension = ".h";
                    savePicker.FileTypeChoices.Add("头文件", new List<string>() { ".h" });
                    savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.ComputerFolder;
                    savePicker.SuggestedFileName = inputFile.Name.Remove(inputFile.Name.IndexOf('.')) + ".h";
                    outputFile = await savePicker.PickSaveFileAsync();

                    if ((inputFile != null) && (outputFile != null))
                    {
                        BGWorker.RunWorkerAsync();
                    }
                }
                catch (Exception ex)
                {
                    await new MessageDialog(ex.Message).ShowAsync();
                }
            }
        }
    }
}
