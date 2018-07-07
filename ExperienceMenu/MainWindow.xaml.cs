using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
//using System.Windows.Shapes;
using Start9.Api;
using Start9.Api.Controls;
using Start9.Api.DiskItems;
using Start9.Api.Tools;

namespace ExperienceMenu
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static string _pinnedPath = Environment.ExpandEnvironmentVariables(@"%appdata%\Start9\TempData\Menu2600_PinnedApps.txt");
        static string _placesPath = Environment.ExpandEnvironmentVariables(@"%appdata%\Start9\TempData\Menu2600_Places.txt");

        public Thickness ListPadding
        {
            get => (Thickness)GetValue(ListPaddingProperty);
            set => SetValue(ListPaddingProperty, value);
        }

        public static readonly DependencyProperty ListPaddingProperty = DependencyProperty.Register("ListPadding", typeof(Thickness), typeof(MainWindow), new PropertyMetadata(new Thickness(0)));

        public ObservableCollection<DiskItem> PinnedItems
        {
            get
            {
                string[] pathStrings = File.ReadAllLines(_pinnedPath);
                ObservableCollection<DiskItem> items = new ObservableCollection<DiskItem>();
                foreach(string s in pathStrings)
                {
                    items.Add(new DiskItem(s));
                }
                return items;
            }
            set
            {
                List<string> pathStrings = new List<string>();
                foreach (DiskItem d in value)
                {
                    pathStrings.Add(d.ItemPath);
                }
                File.WriteAllLines(_pinnedPath, pathStrings);
            }
        }

        public ObservableCollection<DiskItem> Places
        {
            get
            {
                string[] pathStrings = File.ReadAllLines(_placesPath);
                ObservableCollection<DiskItem> items = new ObservableCollection<DiskItem>();
                foreach (string s in pathStrings)
                {
                    items.Add(new DiskItem(s));
                }
                return items;
            }
            set
            {
                List<string> pathStrings = new List<string>();
                foreach (DiskItem d in value)
                {
                    pathStrings.Add(d.ItemPath);
                }
                File.WriteAllLines(_placesPath, pathStrings);
            }
        }

        [DllImport("dwmapi.dll")]
        static extern void DwmEnableBlurBehindWindow(IntPtr hwnd, ref DWM_BLURBEHIND blurBehind);

        [StructLayout(LayoutKind.Sequential)]
        struct DWM_BLURBEHIND
        {
            public DWM_BB dwFlags;
            public bool fEnable;
            public IntPtr hRgnBlur;
            public bool fTransitionOnMaximized;

            public DWM_BLURBEHIND(bool enabled)
            {
                fEnable = enabled ? true : false;
                hRgnBlur = IntPtr.Zero;
                fTransitionOnMaximized = false;
                dwFlags = DWM_BB.Enable;
            }

            public System.Drawing.Region Region
            {
                get { return System.Drawing.Region.FromHrgn(hRgnBlur); }
            }

            public bool TransitionOnMaximized
            {
                get { return fTransitionOnMaximized != false; }
                set
                {
                    fTransitionOnMaximized = value ? true : false;
                    dwFlags |= DWM_BB.TransitionMaximized;
                }
            }

            public void SetRegion(System.Drawing.Graphics graphics, System.Drawing.Region region)
            {
                hRgnBlur = region.GetHrgn(graphics);
                dwFlags |= DWM_BB.BlurRegion;
            }
        }

        [Flags]
        enum DWM_BB
        {
            Enable = 1,
            BlurRegion = 2,
            TransitionMaximized = 4
        }

        TimeSpan MarginInAnimationDuration = TimeSpan.FromMilliseconds(625);

        TimeSpan MarginOutAnimationDuration = TimeSpan.FromMilliseconds(625);

        TimeSpan OpacityAnimationDuration = TimeSpan.FromMilliseconds(375);

        QuinticEase AnimationEase = new QuinticEase()
        {
            EasingMode = EasingMode.EaseOut
        };

        public readonly string SearchText = "Type here to search";

        public MainWindow()
        {
            InitializeComponent();
            Application.Current.MainWindow = this;
            NameTextBlock.Text = Environment.UserName;
            SearchBox.Text = SearchText;
            Visibility = Visibility.Visible;
            Left = 0;
            Topmost = true;

            Deactivated += (sender, e) => Hide();

            /*foreach (string s in Directory.EnumerateDirectories(Environment.ExpandEnvironmentVariables(@"%userprofile%")))
            {
                var item = new IconListViewItem()
                {
                    Content = Path.GetFileName(s),
                    Tag = s,
                    Icon = new Canvas()
                    {
                        Width = 32,
                        Height = 32,
                        Background = new ImageBrush(/*MiscTools.GetIconFromFilePath(s, 32, 32, (uint)0x100)*)
                    }
                };
                item.MouseLeftButtonUp += delegate { Process.Start(s); };
                if (!(item.Content.ToString().StartsWith(".")))
                {
                    PlacesListView.Items.Add(item);
                }
            }

            /*foreach (string s in /*((ExperienceMenuConfiguration) ExperienceMenuAddIn.Instance.Configuration).PinnedItems* new string[] { })
            {
                string itemPath = s;
                /*if (Path.GetExtension(s).Contains("lnk"))
                {
                    itemPath = ShortcutTools.GetTargetPath(s);
                }*

                var item = new IconListViewItem()
                {
                    Content = Path.GetFileNameWithoutExtension(itemPath),
                    Tag = s,
                    Icon = new Canvas()
                    {
                        Width = 32,
                        Height = 32,
                        Background = new ImageBrush(/*MiscTools.GetIconFromFilePath(itemPath, 32, 32, (uint)0x100)*)
                    }
                };
                item.MouseLeftButtonUp += delegate
                {
                    try
                    {
                        Process.Start(s);
                    }
                    catch (System.ComponentModel.Win32Exception ex)
                    {
                        Debug.WriteLine(ex);
                    }
                };
                if (!(item.Content.ToString().StartsWith(".")))
                {
                    PinnedListView.Items.Add(item);
                }
            }*/
            Loaded += MainWindow_Loaded;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var hwnd = new WindowInteropHelper(this).Handle;
            var extendedStyle = WinApi.GetWindowLong(hwnd, WinApi.GwlExstyle);
            WinApi.SetWindowLong(hwnd, WinApi.GwlExstyle, extendedStyle.ToInt32() | WinApi.WsExToolwindow);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Show();
            Instance = this;
        }

        public static MainWindow Instance { get; private set; }
        public new void Show()
        {
            base.Show();
            Top = (SystemParameters.WorkArea.Height + Margin.Bottom) - Height;

            DoubleAnimation opacityAnim = new DoubleAnimation()
            {
                To = 1,
                Duration = OpacityAnimationDuration,
                EasingFunction = AnimationEase
            };
            opacityAnim.Completed += delegate
            {
                var hwnd = new WindowInteropHelper(this).EnsureHandle();
                DWM_BLURBEHIND blur = new DWM_BLURBEHIND()
                {
                    dwFlags = DWM_BB.Enable,
                    fEnable = true,
                    hRgnBlur = IntPtr.Zero,
                    fTransitionOnMaximized = true
                };
                DwmEnableBlurBehindWindow(hwnd, ref blur);
            };

            ThicknessAnimation marginAnim = new ThicknessAnimation()
            {
                From = new Thickness(0, Height, Width, 0),
                To = new Thickness(0),
                Duration = MarginInAnimationDuration,
                EasingFunction = AnimationEase
            };


            BeginAnimation(OpacityProperty, opacityAnim);
            BeginAnimation(MarginProperty, marginAnim);
        }

        public new void Hide()
        {
            var hwnd = new WindowInteropHelper(this).EnsureHandle();
            DWM_BLURBEHIND blur = new DWM_BLURBEHIND()
            {
                dwFlags = DWM_BB.Enable,
                fEnable = false,
                hRgnBlur = IntPtr.Zero,
                fTransitionOnMaximized = true
            };
            DwmEnableBlurBehindWindow(hwnd, ref blur);
            DoubleAnimation opacityAnim = new DoubleAnimation()
            {
                To = 0,
                Duration = OpacityAnimationDuration,
                EasingFunction = AnimationEase
            };

            ThicknessAnimation marginAnim = new ThicknessAnimation()
            {
                To = new Thickness(0, Height, Width, 0),
                Duration = MarginOutAnimationDuration,
                EasingFunction = AnimationEase
            };

            opacityAnim.Completed += delegate
            {
                base.Hide();
            };


            BeginAnimation(OpacityProperty, opacityAnim);
            BeginAnimation(MarginProperty, marginAnim);
        }

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SearchBox.Text == SearchText)
            {
                SearchBox.Text = string.Empty;
            }
            SearchBox.Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF));
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            SearchBox.Foreground = new SolidColorBrush(Color.FromArgb(0xAA, 0xFF, 0xFF, 0xFF));
            if (string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                SearchBox.Text = SearchText;
            }
        }

        private void AllProgramsButton_MouseEnter(object sender, MouseEventArgs e)
        {

        }

        private void AllProgramsButton_Click(object sender, RoutedEventArgs e)
        {
            /*AllProgramsContextMenu.Items.Clear();
            foreach (MenuItem m in GetRootMenuItems())
            {
                AllProgramsContextMenu.Items.Add(m);
            }*/
            AllProgramsContextMenu.ItemsSource = GetRootMenuItems();
            AllProgramsContextMenu.HorizontalOffset = SystemScaling.RealPixelsToWpfUnits(AllProgramsButton.PointToScreen(new System.Windows.Point(0, 0)).X);
            AllProgramsContextMenu.VerticalOffset =
                SystemScaling.RealPixelsToWpfUnits(AllProgramsButton.PointToScreen(new System.Windows.Point(0, 0)).Y)
                - SystemScaling.CursorPosition.Y
            + (AllProgramsButton.ActualHeight / 2);
            AllProgramsContextMenu.IsOpen = true;
        }

        public List<MenuItem> GetRootMenuItems()
        {
            List<MenuItem> items = new List<MenuItem>();
            string appData = Environment.ExpandEnvironmentVariables(@"%appdata%\Microsoft\Windows\Start Menu\Programs");
            string programData = Environment.ExpandEnvironmentVariables(@"%programdata%\Microsoft\Windows\Start Menu\Programs");
            foreach (MenuItem m in GetMenuItemsForFolder(appData, true, false))
            {
                items.Add(m);
            }
            foreach (MenuItem m in GetMenuItemsForFolder(programData, true, false))
            {
                items.Add(m);
            }
            foreach (MenuItem m in GetMenuItemsForFolder(appData, false, true))
            {
                items.Add(m);
            }
            foreach (MenuItem m in GetMenuItemsForFolder(programData, false, true))
            {
                items.Add(m);
            }
            return items;
        }
        public List<MenuItem> GetMenuItemsForFolder(string path)
        {
            return GetMenuItemsForFolder(path, true, true);
        }

        public List<MenuItem> GetMenuItemsForFolder(string path, bool files, bool folders)
        {
            List<MenuItem> items = new List<MenuItem>();
            string expandedPath = Environment.ExpandEnvironmentVariables(path);
            if (files)
            {
                foreach (string s in Directory.EnumerateFiles(expandedPath))
                {
                    MenuItem item = new MenuItem()
                    {
                        Header = Path.GetFileNameWithoutExtension(s),
                        Tag = s
                    };

                    /// Fix and restore this ASAP
                    /// Why does it continue execution when the VS Debugger is attached, but crash otherwise?
                    /*try
                    {
                        WinApi.ShFileInfo shInfo = new WinApi.ShFileInfo();
                        WinApi.SHGetFileInfo(item.Tag.ToString(), 0, ref shInfo, (uint)Marshal.SizeOf(shInfo), 0x000000001 | 0x100);
                        System.Drawing.Icon entryIcon = System.Drawing.Icon.FromHandle(shInfo.hIcon);
                        ImageSource entryIconImageSource = Imaging.CreateBitmapSourceFromHIcon(
                        entryIcon.Handle,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromWidthAndHeight(Convert.ToInt32(DpiManager.ConvertPixelsToWpfUnits(16)), Convert.ToInt32(Convert.ToInt32(DpiManager.ConvertPixelsToWpfUnits(16))))
                        );

                        item.Icon = new Canvas()
                        {
                            Width = 16,
                            Height = 16,
                            Background = new ImageBrush(entryIconImageSource)
                        };
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex);
                    }*/

                    item.Click += delegate { Process.Start(s); };
                    items.Add(item);
                }
            }

            if (folders)
            {
                foreach (string s in Directory.EnumerateDirectories(expandedPath))
                {
                    MenuItem item = new MenuItem()
                    {
                        Header = Path.GetFileNameWithoutExtension(s),
                        Tag = s
                    };

                    WinApi.ShFileInfo shInfo = new WinApi.ShFileInfo();
                    WinApi.SHGetFileInfo(item.Tag.ToString(), 0, ref shInfo, (uint)Marshal.SizeOf(shInfo), 0x000000001 | 0x100);
                    System.Drawing.Icon entryIcon = System.Drawing.Icon.FromHandle(shInfo.hIcon);
                    ImageSource entryIconImageSource = Imaging.CreateBitmapSourceFromHIcon(
                    entryIcon.Handle,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromWidthAndHeight(Convert.ToInt32(SystemScaling.RealPixelsToWpfUnits(16)), Convert.ToInt32(SystemScaling.RealPixelsToWpfUnits(16)))
                    );

                    item.Icon = new Canvas()
                    {
                        Width = 16,
                        Height = 16,
                        Background = new ImageBrush(entryIconImageSource)
                    };

                    item.MouseDoubleClick += delegate { Process.Start(s); };
                    foreach (MenuItem m in GetMenuItemsForFolder(item.Tag.ToString()))
                    {
                        item.Items.Add(m);
                    }

                    items.Add(item);
                }
            }
            return items;
        }

        private void Button_Click(Object sender, RoutedEventArgs e)
        {

        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Process.Start(Environment.ExpandEnvironmentVariables(((sender as ListView).SelectedItem as DiskItem).ItemPath));
        }
    }
}
