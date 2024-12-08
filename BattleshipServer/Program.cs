using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace BattleshipServer
{
    class Program
    {
        static TcpListener server;
        static TcpClient player1;
        static TcpClient player2;
        static NetworkStream stream1;
        static NetworkStream stream2;

        static string player1Positions = "";
        static string player2Positions = "";
        static bool player1Turn = false;
        static int retryCount = 0;
        static Dictionary<string, HashSet<string>> playerShipPositions = new Dictionary<string, HashSet<string>>();
        static void Main(string[] args)
        {
            Console.WriteLine("Battleship Server starting...");
            server = new TcpListener(IPAddress.Any, 5000);
            server.Start();

            Console.WriteLine("Waiting for players...");
            player1 = server.AcceptTcpClient();
            Console.WriteLine("Player 1 connected!");
            stream1 = player1.GetStream();

            player2 = server.AcceptTcpClient();
            Console.WriteLine("Player 2 connected!");
            stream2 = player2.GetStream();

            Thread player1Thread = new Thread(() => HandlePlayer(player1, stream1, "Player1"));
            Thread player2Thread = new Thread(() => HandlePlayer(player2, stream2, "Player2"));
            player1Thread.Start();
            player2Thread.Start();
        }

        static bool gameStarted = false;

        static void HandlePlayer(TcpClient player, NetworkStream stream, string playerName)
        {
            byte[] buffer = new byte[1024];
            Console.WriteLine($"Starting HandlePlayer for {playerName}");

            while (true)
            {
                try
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine($"[{playerName}] Received: {message}");

                    if (message.StartsWith("POSITIONS:"))
                    {
                        string positions = message.Substring(10);
                        playerShipPositions[playerName] = new HashSet<string>(positions.Split(';'));

                        Console.WriteLine($"[{playerName}] Ship Positions: {positions}");
                        Console.WriteLine($"Total Players with Positions: {playerShipPositions.Count}");

                        if (playerShipPositions.Count == 2 && !gameStarted)
                        {
                            Console.WriteLine("BOTH PLAYERS HAVE POSITIONS. STARTING GAME!");
                            Broadcast("ALL_READY");
                            player1Turn = true;
                            SendTurnMessages();
                            gameStarted = true;
                        }
                    }
                    else if (message.StartsWith("SHOT:"))
                    {
                        HandleShot(player, message);
                    }
                    else if (message.StartsWith("SHOT_RESULT:"))
                    {
                        ForwardMessage(player, message);
                    }
                    else if (message.StartsWith("GAME_OVER_"))
                    {
                        ForwardMessage(player, message);
                    }
                    else if (message == "RETRY_REQUEST")
                    {
                        int playerIndex = (playerName == "Player1") ? 0 : 1;
                        playerRetryReady[playerIndex] = true;

                        if (playerRetryReady[0] && playerRetryReady[1])
                        {
                            Broadcast("GAME_RETRY");

                            // Játékállapot alaphelyzetbe
                            playerShipPositions.Clear();
                            player1Turn = false;
                            gameStarted = false;
                            playerRetryReady[0] = false;
                            playerRetryReady[1] = false;
                            shotHistory.Clear(); // Lövéstörténet törlése
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{playerName} disconnected: {ex.Message}");
                    break;
                }
            }
        }

        static HashSet<string> shotHistory = new HashSet<string>(); // Új statikus mező a kezdő részhez
        static bool[] playerRetryReady = new bool[2]; // Új statikus mező a kezdő részhez

        static void HandleShot(TcpClient shooter, string message)
        {
            try
            {
                string[] parts = message.Substring(5).Split(',');

                if (parts.Length < 2)
                {
                    SendErrorMessage(shooter, "INVALID_SHOT_FORMAT");
                    return;
                }

                if (!int.TryParse(parts[0], out int row) ||
                    !int.TryParse(parts[1], out int col))
                {
                    SendErrorMessage(shooter, "INVALID_COORDINATES");
                    return;
                }

                if (row < 0 || row >= 10 || col < 0 || col >= 10)
                {
                    SendErrorMessage(shooter, "OUT_OF_BOARD");
                    return;
                }

                string shooterName = (shooter == player1) ? "Player1" : "Player2";
                string targetPlayerName = (shooter == player1) ? "Player2" : "Player1";
                string shotPosition = $"{row},{col}";

                // Ismételt lövés ellenőrzése
                if (shotHistory.Contains(shotPosition))
                {
                    SendErrorMessage(shooter, "ALREADY_SHOT");
                    return;
                }
                shotHistory.Add(shotPosition);

                if ((shooterName == "Player1" && !player1Turn) ||
                    (shooterName == "Player2" && player1Turn))
                {
                    SendErrorMessage(shooter, "INVALID_TURN");
                    return;
                }

                bool isHit = playerShipPositions[targetPlayerName].Contains(shotPosition);
                if (isHit)
                {
                    playerShipPositions[targetPlayerName].Remove(shotPosition);
                }
                else
                {
                    player1Turn = !player1Turn;
                }

                // Ellenőrizzük, hogy veszített-e valaki
                if (!playerShipPositions[targetPlayerName].Any())
                {
                    // Ha a TARGET játékos elvesztette az összes hajóját
                    string winnerName = (targetPlayerName == "Player1") ? "Player2" : "Player1";

                    // Külön üzenet a nyertesnek és a vesztesnek
                    if (winnerName == "Player1")
                    {
                        SendGameOverMessage(player1, "GAME_OVER_WIN");
                        SendGameOverMessage(player2, "GAME_OVER_LOSE");
                    }
                    else
                    {
                        SendGameOverMessage(player1, "GAME_OVER_LOSE");
                        SendGameOverMessage(player2, "GAME_OVER_WIN");
                    }

                    gameStarted = false;
                    shotHistory.Clear();
                    playerShipPositions.Clear();
                }
                else
                {
                    ForwardMessage(shooter, message);
                    SendTurnMessages();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing shot: {ex.Message}");
                SendErrorMessage(shooter, "UNEXPECTED_ERROR");
            }
        }
        static void SendGameOverMessage(TcpClient player, string message)
        {
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                NetworkStream stream = player == player1 ? stream1 : stream2;
                stream.Write(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hiba a játék vége üzenet küldésekor: {ex.Message}");
            }
        }
        static void SendErrorMessage(TcpClient shooter, string errorCode)
        {
            try
            {
                byte[] errorMessage = Encoding.UTF8.GetBytes($"ERROR:{errorCode}");
                if (shooter == player1)
                    stream1.Write(errorMessage, 0, errorMessage.Length);
                else
                    stream2.Write(errorMessage, 0, errorMessage.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending error message: {ex.Message}");
            }
        }
        static void SendTurnMessages()
        {
            if (player1Turn)
            {
                stream1.Write(Encoding.UTF8.GetBytes("YOUR_TURN"), 0, 9);
                stream2.Write(Encoding.UTF8.GetBytes("OPPONENT_TURN"), 0, 13);
            }
            else
            {
                stream1.Write(Encoding.UTF8.GetBytes("OPPONENT_TURN"), 0, 13);
                stream2.Write(Encoding.UTF8.GetBytes("YOUR_TURN"), 0, 9);
            }
        }

        static void Broadcast(string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            stream1.Write(data, 0, data.Length);
            stream2.Write(data, 0, data.Length);
        }

        static void ForwardMessage(TcpClient sender, string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            if (sender == player1)
                stream2.Write(data, 0, data.Length);
            else
                stream1.Write(data, 0, data.Length);
        }
    }
}
