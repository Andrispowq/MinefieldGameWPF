using Microsoft.Win32;
using MinefieldGame.Model;
using MinefieldGame.Model.Game;
using MinefieldGame.Model.Math;
using MinefieldGame.ViewModel;
using MinefieldGameView;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics.Eventing.Reader;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;

namespace MinefieldGame.View
{
    public partial class App : Application
    {
        private InputHandler inputHandler;
        private GameTimer timer;

        private MinefieldGameViewModel viewModel = null!;
        private MainWindow mainWindow = null!;

        private Point2D gameBounds;

        private Canvas canvas = null!;

        private Button newGameButton = null!;
        private Button loadGameButton = null!;
        private Button exitGameButton = null!;
        private Button saveGameButton = null!;
        private Button menuGameButton = null!;

        private ImageBrush submarineImage = null!;
        private ImageBrush mineImage = null!;

        public App()
        {
            inputHandler = new InputHandler();
            timer = new GameTimer();

            gameBounds = new Point2D(1280, 720);

            Startup += AppStartup;
        }

        private void AppStartup(object? sender, StartupEventArgs e)
        {
            submarineImage = new ImageBrush();
            try
            {
                submarineImage.ImageSource = new BitmapImage(new Uri(System.IO.Path.Combine(Environment.CurrentDirectory, "res", "submarine.jpg")));
            }
            catch
            {
                Console.Error.WriteLine("ERROR: you need to have the res/submarine.png file next to the executable!");
            }

            mineImage = new ImageBrush();

            try
            {
                mineImage.ImageSource = new BitmapImage(new Uri(System.IO.Path.Combine(Environment.CurrentDirectory, "res", "mine.jpg")));
            }
            catch
            {
                Console.Error.WriteLine("ERROR: you need to have the res/mine.png file next to the executable!");
            }

            viewModel = new MinefieldGameViewModel(inputHandler, timer, gameBounds);
            viewModel.ViewStateUpdated += ViewStateUpdated;
            viewModel.GamePrepared += OnGamePrepared;
            viewModel.GameEnded += OnGameEnded;
            viewModel.MineAdded += OnMineAdded;

            mainWindow = new MainWindow()
            {
                Width = gameBounds.X,
                MaxWidth = gameBounds.X,
                MinWidth = gameBounds.X,
                Height = gameBounds.Y,
                MaxHeight = gameBounds.Y,
                MinHeight = gameBounds.Y,
                DataContext = viewModel
            };

            mainWindow.DataContext = viewModel;
            mainWindow.KeyDown += OnKeyDown;
            mainWindow.KeyUp += OnKeyUp;

            canvas = mainWindow.mainCanvas;
            newGameButton = CreateButton("New game", (1280 - 280) / 2, 150, NewButtonAction);
            loadGameButton = CreateButton("Load game", (1280 - 280) / 2, 300, LoadGameAction);
            exitGameButton = CreateButton("Exit game", (1280 - 280) / 2, 450, ExitButtonAction);
            saveGameButton = CreateButton("Save game", (1280 - 280) / 2, 150, SaveGameAction);
            menuGameButton = CreateButton("Main menu", (1280 - 280) / 2, 450, MenuButtonAction);
            ViewStateUpdated(null, viewModel.ViewState);

            RenderOptions.SetBitmapScalingMode(mainWindow, BitmapScalingMode.HighQuality);
            RenderOptions.SetEdgeMode(mainWindow, EdgeMode.Aliased);
            RenderOptions.ProcessRenderMode = RenderMode.Default;
            mainWindow.UseLayoutRounding = true;

            mainWindow.Show();
        }

        private void OnGamePrepared(object? sender, EventArgs a)
        {
            SubmarineViewModel? sub = viewModel.Submarine;
            if (sub != null)
            {
                Rectangle r = CreateDrawable(submarineImage, sub.Position, sub.Size);
                canvas.Children.Add(r);
                RectangleDisplay rd = new RectangleDisplay(sub.Position, sub.Size, r);
                sub.Displayable = rd;
            }

            /*ObservableCollection<MineViewModel> mineList = viewModel.Mines;
            foreach (MineViewModel mine in mineList)
            {
                Rectangle r = CreateDrawable(mineImage, mine.Position, mine.Size);
                canvas.Children.Add(r);
                RectangleDisplay rd = new RectangleDisplay(mine.Position, mine.Size, r);
                mine.Displayable = rd;
            }*/
        }

        private void OnGameEnded(object? sender, EventArgs a)
        {
            MessageBox.Show("Game over", "The game has ended for you!");
            MenuButtonAction(sender, a);
            viewModel.QuitGameCommand.Execute(this);
        }

        private void OnMineAdded(object? sender, MineViewModel mine)
        {
            Rectangle r = CreateDrawable(mineImage, mine.Position, mine.Size);
            canvas.Children.Add(r);
            RectangleDisplay rd = new RectangleDisplay(mine.Position, mine.Size, r);
            mine.Displayable = rd;
        }

        private void ViewStateUpdated(object? sender, ViewState show)
        {
            switch (show)
            {
                case ViewState.MainMenu:
                case ViewState.Over:
                    exitGameButton.Visibility = Visibility.Visible;
                    newGameButton.Visibility = Visibility.Visible;
                    loadGameButton.Visibility = Visibility.Visible;
                    saveGameButton.Visibility = Visibility.Collapsed;
                    menuGameButton.Visibility = Visibility.Collapsed;
                    break;
                case ViewState.Play:
                    exitGameButton.Visibility = Visibility.Collapsed;
                    newGameButton.Visibility = Visibility.Collapsed;
                    loadGameButton.Visibility = Visibility.Collapsed;
                    saveGameButton.Visibility = Visibility.Collapsed;
                    menuGameButton.Visibility = Visibility.Collapsed;
                    break;
                case ViewState.Paused:
                    exitGameButton.Visibility = Visibility.Collapsed;
                    newGameButton.Visibility = Visibility.Collapsed;
                    loadGameButton.Visibility = Visibility.Visible;
                    saveGameButton.Visibility = Visibility.Visible;
                    menuGameButton.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void NewButtonAction(object? sender, EventArgs e)
        {
            inputHandler.Clear();

            viewModel.NewGameCommand.Execute(this);
            viewModel.ContinueGameCommand.Execute(this);
        }

        private void ExitButtonAction(object? sender, EventArgs e)
        {
            inputHandler.Clear();

            viewModel.ExitGameCommand.Execute(this);
            mainWindow.Close();
        }

        private void MenuButtonAction(object? sender, EventArgs e)
        {
            inputHandler.Clear();

            SubmarineViewModel? sub = viewModel.Submarine;
            if (sub != null && sub.Displayable != null && sub.Displayable is RectangleDisplay rd)
            {
                canvas.Children.Remove(rd.Rectangle);
            }

            ObservableCollection<MineViewModel> mineList = viewModel.Mines;
            if (mineList != null)
            {
                foreach (var m in mineList)
                {
                    if (m.Displayable != null && m.Displayable is RectangleDisplay rd_)
                    {
                        canvas.Children.Remove(rd_.Rectangle);
                    }
                }
            }

            viewModel.QuitGameCommand.Execute(this);
        }

        private void LoadGameAction(object? sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.InitialDirectory = Environment.CurrentDirectory;
            ofd.Filter = "Minefield Game Save (*.mgs)|*.mgs";
            ofd.Multiselect = false;
            ofd.RestoreDirectory = true;

            if (ofd.ShowDialog() == true)
            {
                try
                {
                    MenuButtonAction(sender, e);

                    viewModel.LoadGameCommand.Execute(ofd.FileName);
                    viewModel.ContinueGameCommand.Execute(this);
                }
                catch
                {
                    MessageBox.Show("File reading is unsuccessful!\n", "Error");
                }
            }
        }

        private void SaveGameAction(object? sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.InitialDirectory = Environment.CurrentDirectory;
            ofd.Filter = "Minefield Game Save (*.mgs)|*.mgs";
            ofd.Multiselect = false;
            ofd.RestoreDirectory = true;
            ofd.CheckFileExists = false;

            if (ofd.ShowDialog() == true)
            {
                try
                {
                    inputHandler.Clear();

                    viewModel.SaveGameCommand.Execute(ofd.FileName);
                }
                catch
                {
                    MessageBox.Show("Game saving is unsuccessful!\n", "Error");
                }
            }
        }

        private Rectangle CreateDrawable(ImageBrush brush, Point2D position, Point2D size)
        {
            Rectangle r = new Rectangle()
            {
                Width = size.X,
                Height = size.Y,
                Fill = brush
            };

            r.CacheMode = new BitmapCache();
            r.IsManipulationEnabled = true;

            Canvas.SetLeft(r, position.X);
            Canvas.SetTop(r, position.Y);

            return r;
        }

        private Button CreateButton(string text, double x, double y, RoutedEventHandler onClick)
        {
            Button newButton = new Button
            {
                Content = text,
                Width = 280,
                Height = 100,
                Margin = new Thickness(10)
            };
            newButton.Click += onClick;

            Canvas.SetLeft(newButton, x);
            Canvas.SetTop(newButton, y);
            canvas.Children.Add(newButton);

            return newButton;
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (viewModel.ViewState == ViewState.Play)
                {
                    viewModel.StopGameCommand.Execute(this);
                }
                else if(viewModel.ViewState == ViewState.Paused)
                {
                    viewModel?.ContinueGameCommand.Execute(this);
                }
            }

            switch (e.Key)
            {
                case Key.W: inputHandler.KeyPressed('W'); break;
                case Key.S: inputHandler.KeyPressed('S'); break;
                case Key.A: inputHandler.KeyPressed('A'); break;
                case Key.D: inputHandler.KeyPressed('D'); break;
            }
        }

        private void OnKeyUp(object? sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.W: inputHandler.KeyReleased('W'); break;
                case Key.S: inputHandler.KeyReleased('S'); break;
                case Key.A: inputHandler.KeyReleased('A'); break;
                case Key.D: inputHandler.KeyReleased('D'); break;
            }
        }
    }
}
