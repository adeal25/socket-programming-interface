﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;

namespace Server
{
    class Program
    {
        private static Socket listener;
        private static bool isListening;
        private static bool isRunning = true;
        private static bool isSending = false;
        private static Socket clientHandler;
        private static bool receivedNAK = false; // Flag to track NAK reception
        private static bool receivedACK = false; // Flag to track NAK reception
        private static int retryCount = 0; // Flag to track NAK reception
        private static EventWaitHandle sendHandle = new AutoResetEvent(true);
        private static bool startOfMessage = false;
        private static List<byte> finalMsgBuff = new List<byte>();
        private static List<byte> finalMessage = new List<byte>();

        static void Main(string[] args)
        {
            StartServer();
        }
        
        static void StartServer()
        {
            Console.Write("Masukkan IP Address listen: ");
            string ipAddressInput = Console.ReadLine();
            IPAddress ipAddr;
            if (!IPAddress.TryParse(ipAddressInput, out ipAddr))
            {
                LogWithTime("INFO","IP Address tidak valid");
                return;
            }

            Console.Write("Masukkan port listen: ");
            string portInput = Console.ReadLine();
            int port;

            if (!int.TryParse(portInput, out port) || port <= 0 || port > 65535)
            {
                LogWithTime("INFO","Port tidak valid.");
                return;
            }

            IPEndPoint localEndPoint = new IPEndPoint(ipAddr, port);

            try
            {
                // Initialize the listener socket
                listener = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                listener.Bind(localEndPoint);
                listener.Listen(10);

                LogWithTime("INFO","Menunggu koneksi dari klien...");
                isListening = true;

                while (isListening)
                {
                    clientHandler = listener.Accept();
                    LogWithTime("INFO","Klien terhubung!");
                    try
                    {
                    Thread thInputUser = new Thread(ServerSendMessage);
                    Thread thMainSocket = new Thread(ServerReceiveMessage);

                    thInputUser.Start();
                    thMainSocket.Start();

                    thInputUser.Join();
                    thMainSocket.Join();

                    }
                    catch (Exception e)
                    {
                        e.ToString();
                        LogWithTime("ERROR",$"Unexpected exception: {0}");
                    }   
                }
            }
            catch (Exception e)
            {
                LogWithTime("ERROR", $"Unexpected exception: {e.Message}");
                Thread.Sleep(2000);
            }
            finally
            {
                listener?.Close();
            }
        }

        private static void ServerReceiveMessage()
        {
            if (clientHandler == null) return;
            try
            {
                while (isRunning)
                {                    
                    byte[] receiveBuffer = new byte[2048];
                    int byteReceived = clientHandler.Receive(receiveBuffer);

                    
                    
                    for (int i = 0; i<byteReceived; i++)
                    {
                        if (isSending == true)
                        {                        
                            HandleAckNak(receiveBuffer[i]);
                        }
                        else
                        {
                            if (receiveBuffer[i] == 0x01)
                            {
                                HandleAckNak(receiveBuffer[i]);
                                

                                // while (true)
                                // {
                                //     byte[] finalMsgBuff = new byte[1024];
                                //     int msgByteReceived = clientHandler.Receive(finalMsgBuff);
                                //     // Console.WriteLine(Encoding.ASCII.GetString(finalMsgBuff));
                                    
                                //     for (int j = 0; j < msgByteReceived; j++)
                                //     {
                                //         finalMsgBuff.Add(finalMsgBuff[j]);                                        
                                //     }
                                //     // LogWithTime("INFO", "finalMsgBuff saat ini: " + BitConverter.ToString(finalMsgBuff.ToArray()));
            
                                //     int stxIdk = finalMsgBuff.IndexOf(0x02);
                                //     int etbIdk = finalMsgBuff.LastIndexOf(0x23);
                                //     int etxIdk = finalMsgBuff.LastIndexOf(0x03);
                                //     // Check if STX is found and either ETB or ETX is found

                                //     if (stxIdk != -1 && (etbIdk != -1 || etxIdk != -1))
                                //     {
                                //         int endIdk = etbIdk != -1 ? etbIdk : etxIdk;

                                //         if (endIdk > stxIdk)
                                //         {
                                //             while (etxIdk > stxIdk && etxIdk > endIdk)
                                //             {
                                //                 endIdk = etxIdk;
                                //             }
                                //         }
                                //         int chunkLength = endIdk - stxIdk - 1;

                                //         byte[] chunkBytes = finalMsgBuff.GetRange(stxIdk + 1, chunkLength - 3).ToArray();
                                //         finalMessage.AddRange(chunkBytes);
                                //         finalMessage.Add(0x0A);
                                //         // Console.WriteLine(chunkBytes);

                                //         byte cs1Byte = finalMsgBuff[endIdk - 2];
                                //         byte cs2Byte = finalMsgBuff[endIdk - 1];
                                //         string cs1Val =  cs1Byte.ToString("X2")[1].ToString();
                                //         string cs2Val =  cs2Byte.ToString("X2")[1].ToString();

                                //         // Validate Payload data by calculating checksum while receiving data transmited
                                //         if (ValidateChecksum(chunkBytes, cs1Val, cs2Val))
                                //         {
                                //             LogWithTime("DEBUG","Checksum Cocok, Server kirim: <ACK>");
                                //             clientHandler.Send(new byte[] {0x06});
                                //             string chunkMessage = Encoding.ASCII.GetString(chunkBytes);
                                            
                                //             if (etbIdk != -1)
                                //             {
                                //                 LogWithTime("INFO",$"<STX>{chunkMessage}<ETB>");
                                //             }
                                //             else if (etxIdk != -1)
                                //             {
                                //                 LogWithTime("INFO",$"<STX>{chunkMessage}<ETX>");
                                //             }
                                        
                                //         }
                                //         else
                                //         {
                                //             LogWithTime("DEBUG","Checksum tidak valid, Server kirim: <NAK>");
                                //             clientHandler.Send(new byte[] {0x15});
                                //             // break;
                                //         }
            
                                //         finalMsgBuff.RemoveRange(0, endIdk + 1);
                                //     }
            
                                //     if (finalMsgBuff.IndexOf(0x04) != -1)
                                //     {   
                                //         HandleEotReceived(finalMsgBuff, finalMessage);
                                //         break;
                                //     }
                                // }
                            }
                            else if (receiveBuffer[i] == 0x02)
                            {
                                startOfMessage = true;
                            }
                            else if (receiveBuffer[i] == 0x03 || receiveBuffer[i] == 0x23)
                            {
                                startOfMessage = false;
                            
                                if (finalMsgBuff.Count >= 2)
                                {
                                    byte cs1Byte = finalMsgBuff[^2]; // Second last byte
                                    byte cs2Byte = finalMsgBuff[^1]; // Last byte
                                    string cs1Val = cs1Byte.ToString("X2")[1].ToString();
                                    string cs2Val = cs2Byte.ToString("X2")[1].ToString();
                            
                                    byte[] chunkBytes = finalMsgBuff.ToArray();
                                    finalMsgBuff.RemoveRange(finalMsgBuff.Count - 2, 2); // Remove checksum bytes
                            
                                    if (ValidateChecksum(chunkBytes, cs1Val, cs2Val))
                                    {
                                        LogWithTime("DEBUG", "Checksum Cocok, Server kirim: <ACK>");
                                        clientHandler.Send(new byte[] { 0x06 });
                            
                                        finalMessage.AddRange(finalMsgBuff); // Add message content without checksum
                                        finalMessage.Add(0x0A); // Add newline for separation
                            
                                        string messageContent = Encoding.ASCII.GetString(finalMsgBuff.ToArray());
                            
                                        if (receiveBuffer[i] == 0x23) // ETB
                                        {
                                            LogWithTime("INFO", $"<STX>{messageContent}<ETB>");
                                        }
                                        else if (receiveBuffer[i] == 0x03) // ETX
                                        {
                                            LogWithTime("INFO", $"<STX>{messageContent}<ETX>");
                                        }
                                    }
                                    else
                                    {
                                        LogWithTime("DEBUG", "Checksum tidak valid, Server kirim: <NAK>");
                                        clientHandler.Send(new byte[] { 0x15 });
                                    }
                                }
                                else
                                {
                                    LogWithTime("ERROR", "Message buffer too short to contain checksum.");
                                }
                            
                                finalMsgBuff.Clear();
                            
                                if (receiveBuffer[i] == 0x03)
                                {
                                    break;
                                }
                            }
                            else if (receiveBuffer[i] == 0x04)
                            {
                                HandleEotReceived(finalMsgBuff, finalMessage);
                                break;
                            }
                            else if (startOfMessage)
                            {
                                finalMsgBuff.Add(receiveBuffer[i]);
                            }
                            
                            
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LogWithTime("ERROR", $"Exception: {e.Message}");
            }
            finally
            {
                clientHandler?.Shutdown(SocketShutdown.Both);
                clientHandler?.Close();
            }
            
        }

        private static void HandleAckNak(byte receivedByte)
        {
            if (receivedByte == 0x06)
            {
                LogWithTime("INFO","Server terima: <ACK>");
                sendHandle.Set();
            }
            else if (receivedByte == 0x15)
            {
                LogWithTime("INFO","Server terima: <NAK>");
                sendHandle.Set();
            }
        }

        private static void HandleEotReceived(List<byte> finalMsgBuff, List<byte> finalMessage)
        {   
            LogWithTime("INFO", "Akhir transmisi pesan: <EOT>");
            LogWithTime("INFO", "Server terima semua pesan:");

            string fullMessage = Encoding.ASCII.GetString(finalMessage.ToArray());
            ParseData(fullMessage);

            finalMsgBuff.Clear();
            finalMessage.Clear();
            
        }
    


        private static void ParseData(string input)
        {
            string[] lines = input.Split("\n");

            foreach (string line in lines)
            {
            
                string[] parts = line.Split('|');

                // parsing data berdasarkan jenis pesan
                if (parts[0] == "PAT")
                {
                    Console.WriteLine("Pasien");
                    Console.WriteLine("Nomor RM: " + parts[1]);
                    Console.WriteLine("Nama Lengkap: " + parts[2] + " " + parts[3]);
                    Console.WriteLine("Tanggal Lahir: " + parts[4]);
                    Console.WriteLine("Nomor Ruang: " + parts[5]);
                    Console.WriteLine("Tanggal Registrasi: " + parts[6]);
                }
                else if (parts[0] == "SMP")
                {
                    Console.WriteLine("Sampel");
                    Console.WriteLine("Nomor Sampel: " + parts[1]);
                    Console.WriteLine("Jenis Sampel: " + parts[2]);
                }
                else if (parts[0] == "ORD")
                {
                    Console.WriteLine("Hasil");
                    Console.WriteLine("Parameter: " + parts[1]);
                    Console.WriteLine("Nilai: " + parts[2]);
                    Console.WriteLine("Satuan: " + parts[3]);
                }
            }
        }


        private static void ServerSendMessage()
        {
            if (clientHandler == null) return;

            while (isRunning)
            {
                Console.Write("Masukkan pesan untuk dikirimkan ke klien: ");
                string serverMessage = Console.ReadLine();

                if (serverMessage.ToLower() == "exit")
                {
                    isListening = false;
                    listener.Close();
                    isRunning = false;
                    return;
                }

                // bool hasRestarted = false; // Flag untuk menandakan jika sudah mengulang dari awal


                
                KirimDariAwal:
                isSending = true;
                bool sendSuccess = false;
                byte[] soh = new byte[] { 0x01 };
                clientHandler.Send(soh);
                LogWithTime("DEBUG", "Server kirim: <SOH>");

                sendHandle.WaitOne();
                // sendHandle.Reset();

                byte[] finalMsgBuff = Encoding.ASCII.GetBytes(serverMessage);
                int bufferSize = 255;

                
                for (int i = 0; i < finalMsgBuff.Length; i += bufferSize)
                {
                    
                    retryCount = 0;
                    sendSuccess = false;
                    
                    bool isLastChunk = i + bufferSize >= finalMsgBuff.Length;
                    int chunkSize = isLastChunk ? finalMsgBuff.Length - i : bufferSize;
                    byte[] chunkBuffer = new byte[chunkSize];
                    Array.Copy(finalMsgBuff, i, chunkBuffer, 0, chunkSize);

                    string chunkMessage = Encoding.ASCII.GetString(chunkBuffer);

                    
                    while (retryCount < 5 && !sendSuccess)
                    {
                        KirimUlangPotongan:
                        string checksumValues = Checksum(chunkBuffer);
                        byte cs1 = Convert.ToByte(checksumValues[0].ToString(), 16);
                        byte cs2 = Convert.ToByte(checksumValues[1].ToString(), 16);

                        byte[] messageToSend = Encoding.ASCII.GetBytes($"\x02{chunkMessage}\x0D");

                        messageToSend = AppendBytes(messageToSend, new byte[] { cs1, cs2 });
                        messageToSend = AppendBytes(messageToSend, new byte[] { isLastChunk ? (byte)0x03 : (byte)0x23 });

                        LogWithTime("DEBUG", $"Server kirim pesan: <STX> {chunkMessage} <CR>{checksumValues}{(isLastChunk ? "<ETX>" : "<ETB>")}");
                        clientHandler.Send(messageToSend);

                        sendHandle.Reset();
                        sendHandle.WaitOne(1000);
                        if (receivedNAK == true && retryCount<5)
                        {
                            LogWithTime("DEBUG", $"Server kirim ulang: Percobaan ke {retryCount+1}: {chunkMessage}");
                            retryCount++;
                            receivedNAK = false;
                            goto KirimUlangPotongan;
                        }
                        else if (receivedACK == true)
                        {
                            receivedACK = false;
                            sendSuccess = true;
                        }
                        else if (retryCount >=5)
                        {
                            LogWithTime("DEBUG", $"Gagal mengirim pesan setelah {retryCount} percobaan. Mengirim EOT dan berhenti.");
                            byte[] eot = new byte[] { 0x04 };
                            clientHandler.Send(eot);
                            LogWithTime("DEBUG", $"Server kirim: <EOT>");
                            isSending = false;
                            break;
                            
                        }
                        else
                        {
                            LogWithTime("DEBUG", "Tidak ada respons dari server. Mengulang dari awal...");
                            goto KirimDariAwal; // Start from SOH again
                        }
                    }
                
                }
                if(sendSuccess)
                {
                    clientHandler.Send(new byte[] { 0x04 });
                    LogWithTime("DEBUG", $"Server kirim: <EOT>");
                    isSending = false;
                    break;
                } 
            }
        }

        private static void LogWithTime(string logLevel, string message)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:ff");
            Console.WriteLine($"[{timestamp}] {logLevel}: {message}");
        }

        

        private static byte[] AppendBytes(byte[] original, byte[] toAppend)
        {
            byte[] result = new byte[original.Length + toAppend.Length];
            Array.Copy(original, result, original.Length);
            Array.Copy(toAppend, 0, result, original.Length, toAppend.Length);
            return result;
        }

        private static string Checksum(byte[] message)
        {
            int checksum = 0;

            foreach (byte b in message)
            {
                checksum += b;
            }

            checksum = checksum % 256;

            return checksum.ToString("X2");
        }

        private static bool ValidateChecksum(byte[] chunkBytes, string cs1Val, string cs2Val)
        {
            // Now that the Checksum method returns a single string instead of an array, you need to modify this method accordingly.
            string calculatedChecksum = Checksum(chunkBytes);
            
            // Log the received and calculated checksums for debugging.
            LogWithTime("INFO", $"Checksum received: {cs1Val}{cs2Val}, Calculated: {calculatedChecksum}");

            // Compare the entire checksum strings instead of splitting them.
            return calculatedChecksum == $"{cs1Val}{cs2Val}";
        }
    }
}