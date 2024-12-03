using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BattleshipClient
{
    public partial class MainWindow : Window
    {
        private Rectangle currentShip = null;
        private Point startPoint;
        private int[,] playerGrid = new int[10, 10];

        private TcpClient client;
        private NetworkStream stream;
        private Thread listenerThread;
        private bool shipsPlaced = false;
        private bool isMyTurn = false;

        private List<Rectangle> shipRectangles;
        private List<int> shipSizes = new List<int> { 5, 4, 3, 3, 2 };
        private int remainingEnemyShips;
        private int remainingPlayerShips;

        public MainWindow()
        {
            InitializeComponent();
            InitializeBoard(PlayerBoard);
            InitializeBoard(EnemyBoard);
            EnemyBoard.MouseLeftButtonDown += EnemyBoard_MouseLeftButtonDown;
            InitializeShips();
            ConnectToServer("127.0.0.1", 5000);

            remainingEnemyShips = shipSizes.Sum();
            remainingPlayerShips = shipSizes.Sum();
        }

        private void InitializeShips()
        {
            shipRectangles = new List<Rectangle>
            {
                Carrier, Battleship, Cruiser, Submarine, Destroyer
            };
        }

        private void InitializeBoard(UniformGrid board)
        {
            for (int i = 0; i < 100; i++)
            {
                Rectangle cell = new Rectangle
                {
                    Stroke = Brushes.Black,
                    Fill = Brushes.Transparent
                };
                board.Children.Add(cell);
            }
        }

        private void Ship_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && sender is Rectangle ship)
            {
                currentShip = ship;
                startPoint = e.GetPosition(this);
                DragDrop.DoDragDrop(ship, ship.Tag, DragDropEffects.Move);
            }
        }

        private void PlayerBoard_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.StringFormat) && sender is UniformGrid board)
            {
                int shipSize = int.Parse((string)e.Data.GetData(DataFormats.StringFormat));
                Point dropPosition = e.GetPosition(board);

                int col = (int)(dropPosition.X / (board.ActualWidth / 10));
                int row = (int)(dropPosition.Y / (board.ActualHeight / 10));

                if (CanPlaceShip(row, col, shipSize))
                {
                    PlaceShip(row, col, shipSize);
                    currentShip.Visibility = Visibility.Hidden;
                }
                else
                {
                    MessageBox.Show("A hajó nem helyezhető el itt!");
                }
            }
        }

        private bool CanPlaceShip(int row, int col, int size)
        {
            if (col + size > 10) return false;

            for (int i = 0; i < size; i++)
            {
                if (playerGrid[row, col + i] == 1) return false;
            }

            return true;
        }

        private void PlaceShip(int row, int col, int size)
        {
            for (int i = 0; i < size; i++)
            {
                playerGrid[row, col + i] = 1;
                Rectangle cell = (Rectangle)PlayerBoard.Children[row * 10 + col + i];
                cell.Fill = Brushes.DarkGray;
            }

            if (AllShipsPlaced())
            {
                shipsPlaced = true;
                NotifyServerReady();
            }
        }

        private bool AllShipsPlaced()
        {
            int placedCells = 0;
            foreach (int cell in playerGrid)
            {
                if (cell == 1) placedCells++;
            }

            int totalShipCells = shipSizes.Sum();
            return placedCells == totalShipCells;
        }

        private void NotifyServerReady()
        {
            try
            {
                string message = "PLAYER_READY";
                byte[] data = Encoding.UTF8.GetBytes(message);
                stream.Write(data, 0, data.Length);

                
                if (AllShipsPlaced())
                {
                    SendShipPositions();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba az állapot küldésekor: {ex.Message}");
            }
        }

        private void SendShipPositions()
        {
            StringBuilder positions = new StringBuilder();
            for (int row = 0; row < 10; row++)
            {
                for (int col = 0; col < 10; col++)
                {
                    if (playerGrid[row, col] == 1)
                        positions.Append($"{row},{col};");
                }
            }

            try
            {
                string message = $"POSITIONS:{positions.ToString().TrimEnd(';')}";
                byte[] data = Encoding.UTF8.GetBytes(message);
                stream.Write(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba az üzenet küldésekor: {ex.Message}");
            }
        }

        private void ConnectToServer(string ip, int port)
        {
            try
            {
                client = new TcpClient();
                client.Connect(ip, port);
                stream = client.GetStream();

                listenerThread = new Thread(ListenToServer);
                listenerThread.IsBackground = true;
                listenerThread.Start();

                MessageBox.Show("Csatlakozás a szerverhez sikeres!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Nem sikerült csatlakozni a szerverhez: {ex.Message}");
            }
        }

        private void ListenToServer()
        {
            try
            {
                byte[] buffer = new byte[1024];
                while (true)
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Dispatcher.Invoke(() => HandleServerMessage(message));
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => MessageBox.Show($"Szerverkapcsolat megszakadt: {ex.Message}"));
            }
        }
        private bool readyForRetry = false;

        private void RetryButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string message = "RETRY_REQUEST";
                byte[] data = Encoding.UTF8.GetBytes(message);
                stream.Write(data, 0, data.Length);
                RetryButton.IsEnabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error sending retry request: {ex.Message}");
            }
        }private void ResetGameState()
{
    Dispatcher.Invoke(() => {

        for (int i = 0; i < PlayerBoard.Children.Count; i++)
        {
            Rectangle cell = (Rectangle)PlayerBoard.Children[i];
            cell.Fill = Brushes.Transparent;
        }

        for (int i = 0; i < EnemyBoard.Children.Count; i++)
        {
            Rectangle cell = (Rectangle)EnemyBoard.Children[i];
            cell.Fill = Brushes.Transparent;
        }

  
        foreach (var ship in shipRectangles)
        {
            ship.Visibility = Visibility.Visible;
        }

        playerGrid = new int[10, 10];
        remainingPlayerShips = shipSizes.Sum();
        remainingEnemyShips = shipSizes.Sum();
        isMyTurn = false;
        shipsPlaced = false;

        TurnIndicator.Text = "Place Your Ships";
        TurnIndicator.Foreground = Brushes.Black;
        EnemyBoard.IsEnabled = false;
        RetryButton.Visibility = Visibility.Visible;
    });
}

        private void HandleServerMessage(string message)
        {
            Console.WriteLine($"Received server message: {message}");

            if (message == "ALL_READY")
            {
                if (!AllShipsPlaced())
                {
                    EnemyBoard.IsEnabled = false;
                }
                else
                {
                    SendShipPositions();
                }
            }
            else if (message == "WAITING_FOR_OPPONENT")
            {
                EnemyBoard.IsEnabled = false;
            }
            else if (message == "YOUR_TURN")
            {
                isMyTurn = true;
                Dispatcher.Invoke(() =>
                {
                    TurnIndicator.Text = "Your Turn";
                    TurnIndicator.Foreground = Brushes.Green;
                    EnemyBoard.IsEnabled = true;
                });
            }
            else if (message == "OPPONENT_TURN")
            {
                isMyTurn = false;
                Dispatcher.Invoke(() =>
                {
                    TurnIndicator.Text = "Opponent's Turn";
                    TurnIndicator.Foreground = Brushes.Red;
                    EnemyBoard.IsEnabled = false;
                });
            }
            else if (message.StartsWith("SHOT:"))
            {
                HandleIncomingShot(message);
            }
            else if (message.StartsWith("SHOT_RESULT:"))
            {
                HandleShotResult(message);
            }
            else if (message == "GAME_OVER_WIN")
            {
                Dispatcher.Invoke(() =>
                {
                    TurnIndicator.Text = "You Win!";
                    TurnIndicator.Foreground = Brushes.Green;
                    EnemyBoard.IsEnabled = false;
                });
                ResetGame();
            }
            else if (message == "GAME_OVER_LOSE")
            {
                Dispatcher.Invoke(() =>
                {
                    TurnIndicator.Text = "You Lose!";
                    TurnIndicator.Foreground = Brushes.Red;
                    EnemyBoard.IsEnabled = false;
                });
                ResetGame();
            }
        }

        private void EnemyBoard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!isMyTurn) return;

            UniformGrid board = (UniformGrid)sender;
            Point clickPosition = e.GetPosition(board);

            int col = (int)(clickPosition.X / (board.ActualWidth / 10));
            int row = (int)(clickPosition.Y / (board.ActualHeight / 10));

            SendShot(row, col);
        }

        private void SendShot(int row, int col)
        {
            try
            {
                string message = $"SHOT:{row},{col}";
                byte[] data = Encoding.UTF8.GetBytes(message);
                stream.Write(data, 0, data.Length);
                isMyTurn = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba a lövés küldésekor: {ex.Message}");
            }
        }

        private void HandleIncomingShot(string message)
        {
            string[] parts = message.Substring(5).Split(',');
            int row = int.Parse(parts[0]);
            int col = int.Parse(parts[1]);

            bool isHit = CheckShotOnPlayerBoard(row, col);
            SendShotResult(row, col, isHit);
        }

        private bool CheckShotOnPlayerBoard(int row, int col)
        {
            if (playerGrid[row, col] == 1)
            {
                Rectangle cell = (Rectangle)PlayerBoard.Children[row * 10 + col];
                cell.Fill = Brushes.Red;
                playerGrid[row, col] = 2;
                remainingPlayerShips--;
                return true;
            }
            return false;
        }

        private void SendShotResult(int row, int col, bool hit)
        {
            try
            {
                string message = $"SHOT_RESULT:{row},{col},{hit}";
                byte[] data = Encoding.UTF8.GetBytes(message);
                stream.Write(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba az eredmény küldésekor: {ex.Message}");
            }
        }

        private void HandleShotResult(string message)
        {
            string[] parts = message.Substring(12).Split(',');
            int row = int.Parse(parts[0]);
            int col = int.Parse(parts[1]);
            bool hit = bool.Parse(parts[2]);

            Rectangle cell = (Rectangle)EnemyBoard.Children[row * 10 + col];
            cell.Fill = hit ? Brushes.Red : Brushes.White;

            if (hit)
            {
                remainingEnemyShips--;
                if (remainingEnemyShips == 0)
                {
                    SendGameOverMessage(true);
                }
            }
        }

        private void SendGameOverMessage(bool won)
        {
            try
            {
                string message = won ? "GAME_OVER_WIN" : "GAME_OVER_LOSE";
                byte[] data = Encoding.UTF8.GetBytes(message);
                stream.Write(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba a játék vége üzenet küldésekor: {ex.Message}");
            }
        }

        private void ResetGame()
        {
            playerGrid = new int[10, 10];
            remainingPlayerShips = shipSizes.Sum();
            remainingEnemyShips = shipSizes.Sum();
            isMyTurn = false;

            InitializeBoard(PlayerBoard);
            InitializeBoard(EnemyBoard);

            shipsPlaced = false;
        }
    }
}