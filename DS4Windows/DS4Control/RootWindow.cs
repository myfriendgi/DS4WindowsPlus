using DS4WinWPF.DS4Control.DTOXml;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Effects;
using System.Windows.Media.Media3D;
using System.Windows.Media;
using System.Windows.Shell;
using System.Windows;


    class RootWindow : Window
    {
        double radius = 5;
        Border windowBorder, titleBar;
        Grid contentGrid, titlebarIconGrid;
        Button close, minimize, maxRestore;
        public RootWindow()
        {
            Height = 800;
            Width = 1200;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Title = "Rent Manager";
            WindowChrome.SetWindowChrome(this, new WindowChrome()
            {
                ResizeBorderThickness = new Thickness(0, 0, 5, 5),
                CaptionHeight = 0
            });
            addTitleIcons();
            titleBar = new Border()
            {
                CornerRadius = new CornerRadius(radius, radius, 0, 0),
                Background = Brushes.LightGray,
                Height = 32,
                Effect = new DropShadowEffect() { BlurRadius = 5, Opacity = 0.5, Direction = -90 },
                Child = titlebarIconGrid
            };
            contentGrid = new Grid()
            {
                RowDefinitions = {
                new RowDefinition() { Height = GridLength.Auto },
                new RowDefinition()
            },
                Children = { titleBar }
            };
            windowBorder = new Border()
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(radius),
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.LightBlue,
                Child = contentGrid
            };
            AddVisualChild(windowBorder);
            titleBar.MouseLeftButtonDown += handleResize;
            titleBar.MouseMove += move;
        }
        void move(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }
        void handleResize(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2) resize();
        }
        void addTitleIcons()
        {
            close = new Button()
            {
                Width = 24,
                Height = 24,
                ToolTip = "Close",
                Margin = new Thickness(0, 0, 5, 0),
                //Icon = Icons.CloseCircle,
                // Command = Application.Current.Shutdown
            };
            maxRestore = new Button()
            {
                Width = 18,
                Height = 18,
                ToolTip = "Maximize",
                Margin = new Thickness(0, 0, 5, 0),
                //Icon = Icons.Maximize,
                // Command = resize
            };
            minimize = new Button()
            {
                Width = 18,
                Height = 18,
                ToolTip = "Minimize",
                Margin = new Thickness(0, 0, 5, 0),
               // Icon = Icons.Minimize,
                // Command = () => WindowState = WindowState.Minimized
            };
            Grid.SetColumn(close, 3);
            Grid.SetColumn(maxRestore, 2);
            Grid.SetColumn(minimize, 1);
            titlebarIconGrid = new Grid()
            {
                ColumnDefinitions = {
                new ColumnDefinition(),
                new ColumnDefinition(){ Width = GridLength.Auto },
                new ColumnDefinition(){ Width = GridLength.Auto },
                new ColumnDefinition(){ Width = GridLength.Auto }
            },
                Children = { close, maxRestore, minimize }
            };
        }
        void resize()
        {
            if (WindowState == WindowState.Maximized)
            {
                ResizeMode = ResizeMode.CanResizeWithGrip;
                WindowState = WindowState.Normal;
                // maxRestore.Icon = Icons.Maximize;
                maxRestore.ToolTip = "Maximize";
            }
            else
            {
                ResizeMode = ResizeMode.NoResize;
                WindowState = WindowState.Maximized;
                // maxRestore.Icon = Icons.Restore;
                maxRestore.ToolTip = "Restore";
            }
        }
        protected override void OnContentChanged(object oldContent, object newContent)
        {
            var content = newContent as FrameworkElement;
            Grid.SetRow(content, 1);
            contentGrid.Children.Add(content);
        }
        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            windowBorder.Width = arrangeBounds.Width;
            windowBorder.Height = arrangeBounds.Height;
            windowBorder.Measure(arrangeBounds);
            windowBorder.Arrange(new Rect(windowBorder.DesiredSize));
            return windowBorder.DesiredSize;
        }
        protected override Visual GetVisualChild(int index) => windowBorder;
        protected override int VisualChildrenCount => 1;
    }
