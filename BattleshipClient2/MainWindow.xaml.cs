using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BattleshipClient
{
    public partial class MainWindow : Window
    {
        private const int BOARD_SIZE = 10;
        private const int CELL_SIZE = 40;

        private Button[,] playerBoard = new Button[BOARD_SIZE, BOARD_SIZE];
        private Button[,] enemyBoard = new Button[BOARD_SIZE, BOARD_SIZE];
        private List<Ship> ships = new List<Ship>();
        private Ship selectedShip = null;
        private TcpClient client;
        private NetworkStream stream;
        private bool isMyTurn = false;
        private bool gameStarted = false;
        private Point dragStart;

        public MainWindow()
        {
            InitializeComponent();
            CreateBoards();
            CreateShips();
            ConnectToServer();

            PlayerBoard.AllowDrop = true;
            PlayerBoard.DragEnter += PlayerBoard_DragEnter;
            PlayerBoard.Drop += PlayerBoard_Drop;
        }

        private void CreateBoards()
        {
       
            for (int i = 0; i < BOARD_SIZE; i++)
            {
                for (int j = 0; j < BOARD_SIZE; j++)
                {
                    var button = new Button
                    {
                        Width = CELL_SIZE,
                        Height = CELL_SIZE,
                        Background = new SolidColorBrush(Color.FromRgb(74, 179, 255)),
                        Tag = new Point(i, j),
                        BorderBrush = new SolidColorBrush(Color.FromRgb(51, 51, 51)),
                        BorderThickness = new Thickness(1),
                        Margin = new Thickness(1)
                    };

                    playerBoard[i, j] = button;
                    Canvas.SetLeft(button, j * CELL_SIZE);
                    Canvas.SetTop(button, i * CELL_SIZE);
                    PlayerBoard.Children.Add(button);
                }
            }

    
            for (int i = 0; i < BOARD_SIZE; i++)
            {
                for (int j = 0; j < BOARD_SIZE; j++)
                {
                    var button = new Button
                    {
                        Width = CELL_SIZE,
                        Height = CELL_SIZE,
                        Background = new SolidColorBrush(Color.FromRgb(44, 62, 80)),
                        Tag = new Point(i, j),
                        BorderBrush = new SolidColorBrush(Color.FromRgb(51, 51, 51)),
                        BorderThickness = new Thickness(1),
                        Margin = new Thickness(1)
                    };

                    button.Click += EnemyBoard_Click;
                    enemyBoard[i, j] = button;
                    Canvas.SetLeft(button, j * CELL_SIZE);
                    Canvas.SetTop(button, i * CELL_SIZE);
                    EnemyBoard.Children.Add(button);
                }
            }
        }

        private void CreateShips()
        {
            ships.Add(new Ship("Aircraft Carrier", 5));
            ships.Add(new Ship("Battleship", 4));
            ships.Add(new Ship("Submarine", 3));
            ships.Add(new Ship("Cruiser", 3));
            ships.Add(new Ship("Destroyer", 2));

            foreach (var ship in ships)
            {
                var rect = new Rectangle
                {
                    Width = ship.Length * CELL_SIZE,
                    Height = CELL_SIZE,
                    Fill = new SolidColorBrush(Color.FromRgb(85, 239, 196)),
                    Stroke = Brushes.White,
                    StrokeThickness = 2,
                    RadiusX = 5,
                    RadiusY = 5,
                    DataContext = ship
                };

                rect.MouseDown += Ship_MouseDown;
                rect.MouseMove += Ship_MouseMove;
                rect.MouseUp += Ship_MouseUp;

                ship.Rectangle = rect;
                ShipsPanel.Children.Add(rect);

              
                if (ShipsPanel.Children.Count > 1)
                {
                    var lastShip = ShipsPanel.Children[ShipsPanel.Children.Count - 2] as Rectangle;
                    Canvas.SetLeft(rect, Canvas.GetLeft(lastShip) + lastShip.Width + 10);
                }
                else
                {
                    Canvas.SetLeft(rect, 10);
                }
                Canvas.SetTop(rect, 10);
            }
        }

        private void Ship_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (gameStarted) return;

            var rectangle = sender as Rectangle;
            selectedShip = rectangle.DataContext as Ship;
            if (selectedShip != null && !selectedShip.IsPlaced)
            {
                dragStart = e.GetPosition(rectangle);
                Panel.SetZIndex(rectangle, 1);
                rectangle.CaptureMouse();
            }
        }

        private void Ship_MouseMove(object sender, MouseEventArgs e)
        {
            if (selectedShip == null || selectedShip.IsPlaced) return;

            var rectangle = sender as Rectangle;
            Point currentPos = e.GetPosition(PlayerBoard);

          
            double left = currentPos.X - dragStart.X;
            double top = currentPos.Y - dragStart.Y;

          
            left = Math.Max(0, Math.Min(left, PlayerBoard.ActualWidth - rectangle.Width));
            top = Math.Max(0, Math.Min(top, PlayerBoard.ActualHeight - rectangle.Height));

   
            Canvas.SetLeft(rectangle, left);
            Canvas.SetTop(rectangle, top);

       
            ShowPlacementPreview(left, top);
        }

        private void ShowPlacementPreview(double left, double top)
        {
      
            int x = (int)(left / CELL_SIZE);
            int y = (int)(top / CELL_SIZE);

          
            ResetBoardColors();

       
            if (CanPlaceShip(selectedShip, x, y))
            {
                for (int i = 0; i < selectedShip.Length; i++)
                {
                    if (x + i < BOARD_SIZE)
                    {
                        playerBoard[y, x + i].Background = new SolidColorBrush(Color.FromRgb(46, 213, 115));
                    }
                }
            }
            else
            {
                for (int i = 0; i < selectedShip.Length; i++)
                {
                    if (x + i < BOARD_SIZE)
                    {
                        playerBoard[y, x + i].Background = new SolidColorBrush(Color.FromRgb(255, 71, 87));
                    }
                }
            }
        }

        private void ResetBoardColors()
        {
            for (int i = 0; i < BOARD_SIZE; i++)
            {
                for (int j = 0; j < BOARD_SIZE; j++)
                {
                    if (!IsOccupied(i, j))
                    {
                        playerBoard[i, j].Background = new SolidColorBrush(Color.FromRgb(74, 179, 255));
                    }
                }
            }
        }

        private bool IsOccupied(int row, int col)
        {
            return playerBoard[row, col].Background.ToString() ==
                new SolidColorBrush(Color.FromRgb(85, 239, 196)).ToString();
        }

        private void Ship_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (selectedShip == null || selectedShip.IsPlaced) return;

            var rectangle = sender as Rectangle;
            Point dropPosition = e.GetPosition(PlayerBoard);


            int x = (int)(dropPosition.X / CELL_SIZE);
            int y = (int)(dropPosition.Y / CELL_SIZE);

            if (CanPlaceShip(selectedShip, x, y))
            {
                PlaceShip(selectedShip, x, y);

           
                Canvas.SetLeft(rectangle, x * CELL_SIZE);
                Canvas.SetTop(rectangle, y * CELL_SIZE);
                Panel.SetZIndex(rectangle, 0);

                if (ships.All(s => s.IsPlaced))
                {
                    SendBoardToServer();
                    GameStatus.Text = "All ships placed. Waiting for opponent...";
                }
            }
            else
            {

                ResetShipPosition(rectangle);
            }

            ResetBoardColors();
            rectangle.ReleaseMouseCapture();
            selectedShip = null;
        }

        private void ResetShipPosition(Rectangle shipRect)
        {
            var ship = shipRect.DataContext as Ship;
            if (ship != null)
            {
                int index = ships.IndexOf(ship);
                if (index > 0)
                {
                    var prevShip = ships[index - 1].Rectangle;
                    Canvas.SetLeft(shipRect, Canvas.GetLeft(prevShip) + prevShip.Width + 10);
                }
                else
                {
                    Canvas.SetLeft(shipRect, 10);
                }
                Canvas.SetTop(shipRect, 10);
                Panel.SetZIndex(shipRect, 0);
            }
        }

        private void PlayerBoard_DragEnter(object sender, DragEventArgs e)
        {
            if (selectedShip != null && !selectedShip.IsPlaced)
            {
                e.Effects = DragDropEffects.Move;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void PlayerBoard_Drop(object sender, DragEventArgs e)
        {
            if (selectedShip == null || selectedShip.IsPlaced) return;

            Point dropPosition = e.GetPosition(PlayerBoard);
            int x = (int)(dropPosition.X / CELL_SIZE);
            int y = (int)(dropPosition.Y / CELL_SIZE);

            if (CanPlaceShip(selectedShip, x, y))
            {
                PlaceShip(selectedShip, x, y);
                if (ships.All(s => s.IsPlaced))
                {
                    SendBoardToServer();
                    GameStatus.Text = "All ships placed. Waiting for opponent...";
                }
            }
        }

        private bool CanPlaceShip(Ship ship, int x, int y)
        {
            if (x < 0 || x + ship.Length > BOARD_SIZE || y < 0 || y >= BOARD_SIZE)
                return false;


            for (int i = 0; i < ship.Length; i++)
            {
                if (IsOccupied(y, x + i))
                    return false;
            }

            return true;
        }

        private void PlaceShip(Ship ship, int x, int y)
        {
            ship.IsPlaced = true;
            ship.Position = new Point(x, y);

            for (int i = 0; i < ship.Length; i++)
            {
                playerBoard[y, x + i].Background = new SolidColorBrush(Color.FromRgb(85, 239, 196));
            }
        }

        private void ConnectToServer()
        {
            try
            {
                client = new TcpClient("localhost", 5000);
                stream = client.GetStream();
                Task.Run(ReceiveMessages);
                GameStatus.Text = "Connected to server. Place your ships!";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to connect to server: {ex.Message}");
                GameStatus.Text = "Failed to connect to server!";
            }
        }

        private async Task ReceiveMessages()
        {
            var buffer = new byte[1024];
            while (true)
            {
                try
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    await Dispatcher.InvokeAsync(() => HandleMessage(message));
                }
                catch
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        MessageBox.Show("Lost connection to server");
                        GameStatus.Text = "Lost connection to server!";
                    });
                    break;
                }
            }
        }

        private void HandleMessage(string message)
        {
            if (message == "READY")
            {
                gameStarted = true;
                isMyTurn = true;
                GameStatus.Text = "Game started! It's your turn!";
            }
            else if (message.StartsWith("SHOT"))
            {
                var parts = message.Split(',');
                int x = int.Parse(parts[1]);
                int y = int.Parse(parts[2]);

                if (IsHit(x, y))
                {
                    playerBoard[x, y].Background = new SolidColorBrush(Color.FromRgb(255, 118, 117));
                    SendMessage($"HIT,{x},{y}");
                }
                else
                {
                    playerBoard[x, y].Background = new SolidColorBrush(Color.FromRgb(223, 230, 233));
                    SendMessage($"MISS,{x},{y}");
                }
                isMyTurn = true;
                GameStatus.Text = "It's your turn!";
            }
            else if (message.StartsWith("HIT"))
            {
                var parts = message.Split(',');
                enemyBoard[int.Parse(parts[1]), int.Parse(parts[2])].Background =
                    new SolidColorBrush(Color.FromRgb(255, 118, 117));
                GameStatus.Text = "Hit! Waiting for opponent...";
            }
            else if (message.StartsWith("MISS"))
            {
                var parts = message.Split(',');
                enemyBoard[int.Parse(parts[1]), int.Parse(parts[2])].Background =
                    new SolidColorBrush(Color.FromRgb(223, 230, 233));
                GameStatus.Text = "Miss! Waiting for opponent...";
            }
        }

        private void EnemyBoard_Click(object sender, RoutedEventArgs e)
        {
            if (!gameStarted || !isMyTurn) return;

            var button = (Button)sender;
            var pos = (Point)button.Tag;

            if (button.Background != new SolidColorBrush(Color.FromRgb(44, 62, 80)))
                return;

            SendMessage($"SHOT,{(int)pos.X},{(int)pos.Y}");
            isMyTurn = false;
            GameStatus.Text = "Shot fired! Waiting for opponent...";
        }

        private void SendMessage(string message)
        {
            try
            {
                var buffer = Encoding.UTF8.GetBytes(message);
                stream.Write(buffer, 0, buffer.Length);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to send message: {ex.Message}");
                GameStatus.Text = "Failed to send message to server!";
            }
        }

        private void SendBoardToServer()
        {
            var boardState = new StringBuilder("PLACE,");
            foreach (var ship in ships)
            {
                boardState.Append($"{ship.Position.X},{ship.Position.Y},{ship.Length},");
            }
            SendMessage(boardState.ToString());
        }

        private bool IsHit(int x, int y)
        {
            return playerBoard[x, y].Background ==
                new SolidColorBrush(Color.FromRgb(85, 239, 196));
        }
    }

    public class Ship
    {
        public string Name { get; set; }
        public int Length { get; set; }
        public Rectangle Rectangle { get; set; }
        public bool IsPlaced { get; set; }
        public Point Position { get; set; }

        public Ship(string name, int length)
        {
            Name = name;
            Length = length;
            IsPlaced = false;
        }
    }
}