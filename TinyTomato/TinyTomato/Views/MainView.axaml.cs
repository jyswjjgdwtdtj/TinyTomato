using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using DialogHostAvalonia;
using TinyTomato.Services;
using TinyTomato.ViewModels;

namespace TinyTomato.Views;

public partial class MainView : UserControl
{
    MainViewModel VM;
    public MainView()
    {
        InitializeComponent();
        DataContext=VM=new MainViewModel();
    }

    private async void OpenImages_Click(object? sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Open Images",
            Filters = new System.Collections.Generic.List<FileDialogFilter>
            {
                new FileDialogFilter { Name = "Image Files", Extensions = { "png", "jpg", "jpeg", "bmp", "gif" } }
            },
            AllowMultiple = true
        };

        var window = this.VisualRoot as Window;
        if (window == null) return;

        var result = await dlg.ShowAsync(window);
        if (result != null && result.Length > 0)
        {
            if (DataContext is MainViewModel vm)
            {
                foreach (var file in result)
                {
                    ConsoleTarget.Text="正在载入："+file;
                    ConsoleTarget.InvalidateVisual();
                    ImageCache.Instance.AddBitmap(file);
                }

                ConsoleTarget.Text = "";
            }
        }
    }

    private void Image_Tapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is MainViewModel vm&&sender is Image img&&img.DataContext is CacheItem ci)
        {
            ImageCache.Instance.CurrentIndex = ImageCache.Instance.GetIndexByBitmap(ci);
        }
    }

    private async void Save_Clicked(object? sender, RoutedEventArgs e)
    {
        if(ImageCache.Instance.CurrentItem ==null)
            return;
        if (VM.DirectlySave)
        {
            if (ImageCache.Instance.CurrentItem != null)
            {
                var originPath = ImageCache.Instance.CurrentItem.Path;
                ImageCache.Instance.CurrentItem.Image.Save(VM.DefualtSaveFolderPath+new FileInfo(originPath).Name);
            }
        }
        else
        {
            var dlg = new SaveFileDialog
            {
                Title = "Save Image",
                Filters = new System.Collections.Generic.List<FileDialogFilter>
                {
                    new FileDialogFilter { Name = "Image Files", Extensions = { "png", "jpg", "jpeg", "bmp", "gif" } }
                },InitialFileName = new FileInfo(ImageCache.Instance.CurrentItem.Path).Name,
            
            };

            var window = this.VisualRoot as Window;
            if (window == null) return;

            var result = await dlg.ShowAsync(window);
            if (result != null && result.Length > 0)
            {
                ImageCache.Instance.CurrentItem?.Image.Save(result);
            }
        }
        if(VM.SaveAndClean) 
            ImageCache.Instance.ClearCurrent();
        
    }

    private void Global_KeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.S when e.KeyModifiers == KeyModifiers.Control:
            {
                //save
                Save_Clicked(sender, e);
            }
                break;
        }
    }

    private async void DefaultPath_Tapped(object? sender, RoutedEventArgs e)
    {
        var dlg = new OpenFolderDialog()
        {
            Title = "Default Path"
        };
        dlg.Directory = VM.DefualtSaveFolderPath;
        var window = this.VisualRoot as Window;
        if (window == null) return;

        var result = await dlg.ShowAsync(window);
        if (result != null && result.Length > 0)
        {
            VM.DefualtSaveFolderPath = result.EndsWith("\\")?result:result+"\\";
        }
    }

    private async void SaveAll_Clicked(object? sender, RoutedEventArgs e)
    {
        if(ImageCache.Instance.Count==0)
            return;
        Parallel.ForEach(ImageCache.Instance.CacheList, (ci) =>
        {
            ci.Image.Save(VM.DefualtSaveFolderPath+new FileInfo(ci.Path).Name);
        });
        if (VM.SaveAndClean)
            ImageCache.Instance.ClearAll();
    }

    private void DelectSource_Clicked(object? sender, RoutedEventArgs e)
    {
        if (ImageCache.Instance.CurrentItem != null)
        {
            File.Delete(ImageCache.Instance.CurrentItem.Path);
            ImageCache.Instance.ClearCurrent();
        }
    }
}
