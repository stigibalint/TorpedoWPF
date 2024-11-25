using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class BattleshipServer
{
    private static TcpListener listener;
    private static TcpClient[] clients = new TcpClient[2];
    private static NetworkStream[] streams = new NetworkStream[2];
    private static string[] boards = new string[2];

    static void Main(string[] args)
    {
        Console.WriteLine("Szerver indítása...");
        listener = new TcpListener(IPAddress.Any, 5000);
        listener.Start();
        Console.WriteLine("Várakozás a játékosokra...");

        for (int i = 0; i < 2; i++)
        {
            clients[i] = listener.AcceptTcpClient();
            streams[i] = clients[i].GetStream();
            Console.WriteLine($"Játékos {i + 1} csatlakozott.");
            int playerIndex = i;
            new Thread(() => HandleClient(playerIndex)).Start();
        }

        Console.WriteLine("Mindkét játékos csatlakozott. Kezdődhet a játék!");
    }

    private static void HandleClient(int playerIndex)
    {
        try
        {
            var stream = streams[playerIndex];
            var buffer = new byte[1024];
            while (true)
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine($"Játékos {playerIndex + 1} üzenete: {message}");

                if (message.StartsWith("PLACE"))
                {
                    boards[playerIndex] = message.Substring(6);
                    streams[1 - playerIndex].Write(Encoding.UTF8.GetBytes("READY"), 0, 5);
                }
                else if (message.StartsWith("SHOT"))
                {
                    streams[1 - playerIndex].Write(Encoding.UTF8.GetBytes(message), 0, message.Length);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Hiba történt a játékos {playerIndex + 1} kezelésénél: {ex.Message}");
        }
        finally
        {
            clients[playerIndex]?.Close();
        }
    }
}