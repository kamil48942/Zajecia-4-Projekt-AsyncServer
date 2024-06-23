using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

class AsyncServer
{
    private const int Port = 5000;
    private static readonly byte[] key = Encoding.UTF8.GetBytes("ThisIsASecretKey"); 
    private static readonly byte[] iv = Encoding.UTF8.GetBytes("ThisIsAnInitVect"); 

    public static async Task StartServerAsync()
    {
        TcpListener listener = new TcpListener(IPAddress.Any, Port);
        listener.Start();
        Console.WriteLine("Serwer uruchomiony, oczekiwanie na połączenia...");
        while (true)
        {
            TcpClient client = await listener.AcceptTcpClientAsync();
            Console.WriteLine("Połączono z klientem.");
            _ = HandleClientAsync(client); 
        }
    }

    private static async Task HandleClientAsync(TcpClient client)
    {
        using (client)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];
            int bytesRead;
            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) != 0)
            {
                string encryptedMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine($"Otrzymano zaszyfrowane: {encryptedMessage}");
                string message = Decrypt(encryptedMessage);
                Console.WriteLine($"Otrzymano: {message}");
                string responseMessage = $"Serwer otrzymał: {message}";
                string encryptedResponse = Encrypt(responseMessage);
                Console.WriteLine($"Wysyłanie zaszyfrowane: {encryptedResponse}");
                byte[] response = Encoding.UTF8.GetBytes(encryptedResponse);
                await stream.WriteAsync(response, 0, response.Length);
            }
        }
        Console.WriteLine("Klient rozłączony.");
    }

    private static string Encrypt(string plainText)
    {
        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = key;
            aesAlg.IV = iv;

            ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

            using (MemoryStream msEncrypt = new MemoryStream())
            {
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                {
                    swEncrypt.Write(plainText);
                }
                return Convert.ToBase64String(msEncrypt.ToArray());
            }
        }
    }

    private static string Decrypt(string cipherText)
    {
        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = key;
            aesAlg.IV = iv;

            ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

            using (MemoryStream msDecrypt = new MemoryStream(Convert.FromBase64String(cipherText)))
            using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
            using (StreamReader srDecrypt = new StreamReader(csDecrypt))
            {
                return srDecrypt.ReadToEnd();
            }
        }
    }

    static async Task Main(string[] args)
    {
        await StartServerAsync();
    }
}
