using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using Avalonia.Media.Imaging;
using ReactiveUI;
using TinyTomato.Services;
using System.Threading.Tasks;
using Avalonia.Data.Converters;
using System.Globalization;

namespace TinyTomato.ViewModels;

public class MainViewModel : ViewModelBase
{
    public string DefualtSaveFolderPath { get; set=>this.RaiseAndSetIfChanged(ref field,value); }
    public bool DirectlySave { get;set=>this.RaiseAndSetIfChanged(ref field,value); }=false;
    public bool SaveAndClean { get;set=>this.RaiseAndSetIfChanged(ref field,value); }=false;
    public ImageCache ImageCache { get;}= ImageCache.Instance;

    public ReactiveCommand<Unit, Unit> OpenImagesCommand { get; }
    public ReactiveCommand<Unit, Unit> NextCommand { get; }
    public ReactiveCommand<Unit, Unit> PrevCommand { get; }

    public ReactiveCommand<Unit, Unit> EncryptCurrentCommand { get; }
    public ReactiveCommand<Unit, Unit> EncryptAllCommand { get; }
    public ReactiveCommand<Unit, Unit> DecryptCurrentCommand { get; }
    public ReactiveCommand<Unit, Unit> DecryptAllCommand { get; }
    public ReactiveCommand<Unit, Unit> ClearCurrentCommand { get; }
    public ReactiveCommand<Unit, Unit> ClearAllCommand { get; }
    public ReactiveCommand<Unit, Unit> BackCurrentCommand { get; }
    public ReactiveCommand<Unit, Unit> BackAllCommand { get; }

    public MainViewModel()
    {
        OpenImagesCommand = ReactiveCommand.Create(OpenImages);
        NextCommand = ReactiveCommand.Create(ImageCache.Instance.Next);
        PrevCommand = ReactiveCommand.Create(ImageCache.Instance.Previous);
        ClearAllCommand = ReactiveCommand.Create(ImageCache.Instance.ClearAll);
        ClearCurrentCommand = ReactiveCommand.Create(ImageCache.Instance.ClearCurrent);

        EncryptCurrentCommand = ReactiveCommand.Create(ImageCache.Instance.EncryptCurrent);
        EncryptAllCommand = ReactiveCommand.Create(ImageCache.Instance.EncryptAll);
        DecryptCurrentCommand = ReactiveCommand.Create(ImageCache.Instance.DecryptCurrent);
        DecryptAllCommand = ReactiveCommand.Create(ImageCache.Instance.DecryptAll);
        BackCurrentCommand = ReactiveCommand.Create(ImageCache.Instance.BackCurrent);
        BackAllCommand = ReactiveCommand.Create(ImageCache.Instance.BackAll);
        var pic=Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        DefualtSaveFolderPath = pic.EndsWith("\\")?pic:pic+"\\";
    }

    private void OpenImages()
    {
        // placeholder; actual dialog is in view code-behind
    }


    
}

public class IndexToBitmapConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int index)
        {
            var item = ImageCache.Instance.GetBitmapByIndex(index);
            return item;
        }
        return null;
    }
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}