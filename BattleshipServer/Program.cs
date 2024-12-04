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
                    if (message == "RETRY_REQUEST")
                    {
                        retryCount++;
                        if (retryCount == 2)
                        {
                      
                            Broadcast("GAME_RETRY");
                            retryCount = 0;

                            
                            playerShipPositions.Clear();
                            player1Turn = false;
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

                Console.WriteLine($"Shot received from {shooterName} at position {shotPosition}");
                Console.WriteLine($"Current turn: {(player1Turn ? "Player1" : "Player2")}");

              
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
                    Console.WriteLine($"Hit on {targetPlayerName} at {shotPosition}");
                }
                else
                {
                    Console.WriteLine($"Miss on {targetPlayerName} at {shotPosition}");
                    player1Turn = !player1Turn;
                }

                ForwardMessage(shooter, message);
                SendTurnMessages();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing shot: {ex.Message}");
                SendErrorMessage(shooter, "UNEXPECTED_ERROR");
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
