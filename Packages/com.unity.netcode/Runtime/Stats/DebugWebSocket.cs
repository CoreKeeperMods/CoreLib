#if UNITY_EDITOR || NETCODE_DEBUG
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.NetCode
{
    class DebugWebSocket : IDisposable
    {
        private Socket m_serverSocket;
        private Socket m_connectionSocket;
        private bool m_connectionComplete;
        private byte[] m_frameHeader;

        public DebugWebSocket(ushort port)
        {
            try
            {
                m_serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                m_serverSocket.Blocking = false;
                m_serverSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
                m_serverSocket.Bind(new IPEndPoint(IPAddress.Any, port));
                m_serverSocket.Listen(10);
                m_frameHeader = new byte[16];

            }
            catch (SocketException e)
            {
                UnityEngine.Debug.LogError("Failed to create websocket: " + e);
                m_serverSocket = null;
            }
        }

        public bool HasConnection => m_connectionComplete && m_connectionSocket != null;

        public void Dispose()
        {
            if (m_connectionSocket != null)
                m_connectionSocket.Dispose();
            if (m_serverSocket != null)
                m_serverSocket.Dispose();
        }

        public bool AcceptNewConnection()
        {
            if (m_serverSocket == null)
                return false;
            if (!m_connectionComplete && m_connectionSocket != null)
            {
                try
                {
                    // Listen for the http header, parse it and reply with another http header
                    var headerBuffer = new byte[4096];
                    var headerSize = m_connectionSocket.Receive(headerBuffer);
                    if (headerSize > 0)
                    {
                        var header = Encoding.UTF8.GetString(headerBuffer, 0, headerSize);
                        var headerFields = header.Split(new[] {"\r\n"}, StringSplitOptions.None);
                        if (headerFields.Length < 3 || headerFields[headerFields.Length - 2] != "" ||
                            headerFields[headerFields.Length - 1] != "")
                        {
                            UnityEngine.Debug.LogError("Invalid header for websocket: " + header);
                            FailConnection();
                            return false;
                        }

                        var get = headerFields[0].Split(null);
                        if (get.Length != 3 || get[0] != "GET" || get[2] != "HTTP/1.1")
                        {
                            UnityEngine.Debug.LogError("Invalid GET header for websocket: " + headerFields[0]);
                            FailConnection();
                            return false;
                        }

                        var headerLookup = new Dictionary<string, string>();
                        for (var field = 1; field < headerFields.Length - 2; ++field)
                        {
                            var keyval = headerFields[field].Split(new[] {':'}, StringSplitOptions.None);
                            if (keyval.Length < 2)
                            {
                                UnityEngine.Debug.LogError("Invalid header line for websocket: " + headerFields[field]);
                                FailConnection();
                                return false;
                            }

                            for (int i = 2; i < keyval.Length; ++i)
                                keyval[1] += ":" + keyval[i];
                            headerLookup.Add(keyval[0].Trim().ToLowerInvariant(), keyval[1].Trim());
                        }

                        // Parse the header and reply
                        string wsKey, wsConnection, wsUpgrade, wsVer;
                        if (!headerLookup.TryGetValue("sec-websocket-key", out wsKey) ||
                            !headerLookup.TryGetValue("connection", out wsConnection) ||
                            !headerLookup.TryGetValue("upgrade", out wsUpgrade) ||
                            !headerLookup.TryGetValue("sec-websocket-version", out wsVer) ||
                            !headerLookup.ContainsKey("host"))
                        {
                            UnityEngine.Debug.LogError("Header does not contain all required fields: " + header);
                            FailConnection();
                            return false;
                        }

                        if (wsVer != "13")
                        {
                            UnityEngine.Debug.LogError("Invalid websocket version: " + wsVer);
                            FailConnection();
                            return false;
                        }

                        if (wsUpgrade.ToLowerInvariant() != "websocket")
                        {
                            UnityEngine.Debug.LogError("Invalid websocket upgrade request: " + wsUpgrade);
                            FailConnection();
                            return false;
                        }

                        bool upgrade = false;
                        var upgradeTokens = wsConnection.Split(new[] {','}, StringSplitOptions.None);
                        foreach (var token in upgradeTokens)
                            upgrade |= token.Trim().ToLowerInvariant() == "upgrade";
                        if (!upgrade)
                        {
                            UnityEngine.Debug.LogError("Invalid websocket connection header: " + wsConnection);
                            FailConnection();
                            return false;
                        }

                        wsKey += "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
                        SHA1 sha = new SHA1CryptoServiceProvider();
                        var resultKey = Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(wsKey)));
                        header =
                            "HTTP/1.1 101 Switching Protocols\r\nUpgrade: websocket\r\nConnection: Upgrade\r\nSec-WebSocket-Accept: " +
                            resultKey + "\r\n\r\n";
                        m_connectionSocket.Send(Encoding.UTF8.GetBytes(header));
                        m_connectionComplete = true;
                        return true;
                    }
                }
                catch (SocketException e)
                {
                    if (e.SocketErrorCode != SocketError.WouldBlock)
                    {
                        m_connectionSocket.Dispose();
                        m_connectionSocket = null;
                    }
                }
            }

            if (!m_serverSocket.Poll(0, SelectMode.SelectRead))
                return false;
            try
            {
                var sock = m_serverSocket.Accept();
                if (m_connectionSocket != null)
                {
                    // FIXME: send close frame etc
                    m_connectionSocket.Dispose();
                }

                m_connectionSocket = sock;
                m_connectionSocket.Blocking = false;
                m_connectionComplete = false;
            }
            catch (SocketException)
            {
            }

            return false;
        }

        void FailConnection()
        {
            m_connectionSocket.Send(
                Encoding.UTF8.GetBytes("HTTP/1.1 400 Bad Request\r\nContent-Length: 0\r\nConnection: Closed\r\n\r\n"));
            m_connectionSocket.Dispose();
            m_connectionSocket = null;
        }

        public void SendText(NativeArray<byte> msg)
        {
            if (m_connectionSocket == null)
                return;
            // Fin bit + text message
            m_frameHeader[0] = 0x81;
            int headerLen = 2;
            if (msg.Length < 126)
                m_frameHeader[1] = (byte) msg.Length;
            else if (msg.Length <= ushort.MaxValue)
            {
                m_frameHeader[1] = 126;
                m_frameHeader[2] = (byte) (msg.Length >> 8);
                m_frameHeader[3] = (byte) (msg.Length);
                headerLen = 4;
            }
            else
            {
                m_frameHeader[1] = 127;
                m_frameHeader[2] = 0;
                m_frameHeader[3] = 0;
                m_frameHeader[4] = 0;
                m_frameHeader[5] = 0;
                m_frameHeader[6] = (byte) (msg.Length >> 24);
                m_frameHeader[7] = (byte) (msg.Length >> 16);
                m_frameHeader[8] = (byte) (msg.Length >> 8);
                m_frameHeader[9] = (byte) (msg.Length);
                headerLen = 10;
            }

            try
            {
                m_connectionSocket.Send(m_frameHeader, headerLen, SocketFlags.None);
                unsafe
                {
                    m_connectionSocket.Send(new ReadOnlySpan<byte>((byte*)msg.GetUnsafePtr(), msg.Length), SocketFlags.None);
                }
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode != SocketError.WouldBlock)
                {
                    m_connectionSocket.Dispose();
                    m_connectionSocket = null;
                }
            }
        }

        public void SendBinary(NativeArray<byte> msg)
        {
            if (m_connectionSocket == null)
                return;
            // Fin bit + binary message
            m_frameHeader[0] = 0x82;
            int headerLen = 2;
            if (msg.Length < 126)
                m_frameHeader[1] = (byte) msg.Length;
            else if (msg.Length <= ushort.MaxValue)
            {
                m_frameHeader[1] = 126;
                m_frameHeader[2] = (byte) (msg.Length >> 8);
                m_frameHeader[3] = (byte) (msg.Length);
                headerLen = 4;
            }
            else
            {
                m_frameHeader[1] = 127;
                m_frameHeader[2] = 0;
                m_frameHeader[3] = 0;
                m_frameHeader[4] = 0;
                m_frameHeader[5] = 0;
                m_frameHeader[6] = (byte) (msg.Length >> 24);
                m_frameHeader[7] = (byte) (msg.Length >> 16);
                m_frameHeader[8] = (byte) (msg.Length >> 8);
                m_frameHeader[9] = (byte) (msg.Length);
                headerLen = 10;
            }

            try
            {
                m_connectionSocket.Send(m_frameHeader, headerLen, SocketFlags.None);
                unsafe
                {
                    m_connectionSocket.Send(new ReadOnlySpan<byte>((byte*)msg.GetUnsafePtr(), msg.Length), SocketFlags.None);
                }
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode != SocketError.WouldBlock)
                {
                    m_connectionSocket.Dispose();
                    m_connectionSocket = null;
                }
            }
        }
    }
}
#endif
