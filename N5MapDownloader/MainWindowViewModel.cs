using N5MapDownloader.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using Microsoft.Win32;
using System.Web;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Runtime.InteropServices;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.IO.Compression;
using ICSharpCode.SharpZipLib.BZip2;
using System.Threading.Tasks;

namespace N5MapDownloader
{
    public class MainWindowViewModel : PropertyChanger
    {
        private string _gameFolder;
        private string _selectedMapType;
        private string _searchRequest;
        private ObservableCollection<MapViewModel> _openedMaps;
        private ObservableCollection<MapViewModel> _maps;
        private ObservableCollection<MapViewModel> _downloadingMaps;

        private bool _isSelectedAll;
        private bool _canDownload = true;
        private int _selectedIndex = 0;

        private const string URL_FASTDL = "http://cds.n5srv.ru.net/fastdl/zs/maps/{0}.bsp.bz2";
        private const string URL_MAPLIST = "http://cds.n5srv.ru.net/n5/util/maplist.php?downloadlist={0}";

        private RelayCommand _openFolderCommand;
        private RelayCommand _downloadCommand;       
        public MainWindowViewModel() 
        {
            var res = Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Valve\\Steam", "InstallPath", null);
            if (res != null) 
            {
                res = res.ToString() + "\\steamapps\\common\\GarrysMod";
                if (Directory.Exists(res.ToString()))
                    GameFolder = res.ToString();

            }

            MapTypes = new() {"Только использующиеся", "Все карты"};
            SelectedMapType = MapTypes.First();
        }

        public RelayCommand OpenFolderCommand => _openFolderCommand ??= new(p =>
        {
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                GameFolder = dialog.FileName;
            }
        });

        public RelayCommand DownloadCommand => _downloadCommand ??= new(p =>
        {
            DownloadingMaps = new(Maps.Where(m => m.IsSelected));

            if (DownloadingMaps == null || DownloadingMaps.Count() == 0)
                return;

            string destination = $"{GameFolder}\\garrysmod\\download\\maps";
            if(!Directory.Exists(destination))
            {
                MessageBox.Show("Указанный путь не найден. Укажите верный путь до Garrys Mod");
                return;
            }

            CanDownload = false;
            SelectedIndex = 1;
            
            //WebClient client = new();
            

            foreach(var map in DownloadingMaps)
            {
                string url = String.Format(URL_FASTDL, map.Name);
                string dest = $"{destination}\\{map.Name}.bsp.bz2";

                //client.DownloadFile(url, dest);
                var task = Task.Factory.StartNew(() =>
                { 
                    using (var client = new HttpClient())
                    {
                        using (var s = client.GetStreamAsync(url))
                        {
                            using (var fs = new FileStream(dest, FileMode.OpenOrCreate))
                            {
                                map.Status = "Загрузка";
                                s.Result.CopyTo(fs);
                                fs.Seek(0, SeekOrigin.Begin);
                                s.Wait();
                                map.Status = "Распаковка";
                                FileStream destFs = new($"{destination}\\{map.Name}.bsp",FileMode.OpenOrCreate);
                                BZip2.Decompress(fs,destFs,false);                                
                                map.Status = "Готово";
                            } 
                            File.Delete(dest);                     
                        }
                    }   
                    if(map == DownloadingMaps.Last())
                        CanDownload = true;
                });                        
            }

            Task.WaitAll();            
        });

        public bool IsSelectedAll
        {
            get => _isSelectedAll;
            set
            {
                SetProperty(ref _isSelectedAll, value);
                foreach (var map in Maps)
                {
                    map.IsSelected = value;
                }
            }
        }

        public ObservableCollection<MapViewModel> Maps
        {
            get => _maps;
            set => SetProperty(ref _maps, value);
        }

        public ObservableCollection<MapViewModel> DownloadingMaps
        {
            get => _downloadingMaps;
            set => SetProperty(ref _downloadingMaps, value);
        }

        public ObservableCollection<MapViewModel> OpenedMaps
        {
            get => _openedMaps;
            set => SetProperty(ref _openedMaps, value);
        }

        public string SelectedMapType
        {
            get => _selectedMapType;
            set
            {
                SetProperty(ref _selectedMapType, value);
                LoadMapList();
                IsSelectedAll = false;
            }
        }

        public string SearchRequest
        {
            get
            {
                FindMaps();
                return _searchRequest;
            }
            set => SetProperty(ref _searchRequest, value);
        }

        public bool CanDownload
        {
            get => _canDownload;
            set => SetProperty(ref _canDownload, value);
        }

        public int SelectedIndex
        {
            get => _selectedIndex;
            set => SetProperty(ref _selectedIndex, value);
        }

        public List<string> MapTypes { get; set; }

        public string GameFolder
        {
            get => _gameFolder;
            set
            {
                SetProperty(ref _gameFolder, value);
                LoadMapList();
            }
        }

        private void LoadMapList()
        {
            WebClient client = new();
            string mapType = SelectedMapType == "Все карты" ? "all" : "allowed";
            string html = "";
            try
            {
                html = client.DownloadString(String.Format((URL_MAPLIST), new object[] { mapType })); //error too many request
            }catch(Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message} \n Перезапустите приложение");
                return;
            }
            var str = HttpUtility.UrlEncode(html);
            List<string> maps = str.Split("%0a", StringSplitOptions.RemoveEmptyEntries).ToList();//.Where(s => s != "%ef%bb%bf").ToList();
            OpenedMaps = new();

            foreach (var map in maps)
            {
                string[] mapData = map.Split("+", StringSplitOptions.RemoveEmptyEntries).ToArray();
                OpenedMaps.Add(new MapViewModel(mapData[0], Int32.Parse(mapData[1])));
            }
            Maps = OpenedMaps;
        }

        private void FindMaps()
        {
            if (_searchRequest == null)
                return;

            var maps = OpenedMaps.Where(map => map.Name.Contains(_searchRequest));
            if (maps != null && maps.Count() > 0)
                Maps = new(maps);
            else
                Maps = null;
        }
    }
}
