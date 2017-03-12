using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronWebSocketClient; 

namespace RemoteManagementLibrary
{
    public class RemoteManagementWebSocketClient
    {
        public WebSocketClient wsc;
        public WebSocketClient.WEBSOCKET_RESULT_CODES resultCodes;
        public WebSocketClient.WEBSOCKET_PACKET_TYPES packetTypes;
        public byte[] ReceiveData;
        public uint port;
        public string host;
        public int debug;
        public delegate void DataReceiveHandler(SimplSharpString data);
        public DataReceiveHandler dataReceiveEvent { set; get; }
        private CTimer keepAliveTimer;
        private bool keepAliveMessageReceived;
        private uint keepAliveCounter;
        private bool keepAliveReconnect;
        private long keepAliveTimerRepeatTime;
        
        public RemoteManagementWebSocketClient()
        {            
            keepAliveMessageReceived = false;
            keepAliveCounter = 0;
            keepAliveReconnect = false;
            keepAliveTimerRepeatTime = 5000;
            keepAliveTimer = new CTimer(keepAliveCheck, null, 1000, keepAliveTimerRepeatTime);
            keepAliveTimer.Stop();   
        }

        public void intitialize()
        {
            wsc = new WebSocketClient();
            wsc.KeepAlive = true;
            wsc.SSL = true;
            wsc.VerifyServerCertificate = false;
            wsc.SendCallBack = SendCallback;
            wsc.ReceiveCallBack = ReceiveCallback;
            wsc.DisconnectCallBack = DisconnectCallBack;            
        }
        
        public int DisconnectCallBack(WebSocketClient.WEBSOCKET_RESULT_CODES error, object obj)
        {
            try
            {
                if (debug > 0)
                {
                    CrestronConsole.Print("\n Websocket DisconnectCallBack error: " + error.ToString());
                }                
            }
            catch (Exception e)
            {
                if (debug > 0)
                {
                    CrestronConsole.Print("\n Websocket DisconnectCallBack exception: " + e.Message);
                }
                return -1;
            }
            return 0;
        }
        
        public void Connect()
        {
            try
            {
                wsc.Port = port;
                wsc.URL = host;
                resultCodes = wsc.Connect();
                if (debug > 0)
                {
                    CrestronConsole.Print("\n Websocket resultCodes: " + resultCodes.ToString());
                }    

                if (resultCodes == (int)WebSocketClient.WEBSOCKET_RESULT_CODES.WEBSOCKET_CLIENT_SUCCESS)
                {
                    CrestronConsole.PrintLine("\n Websocket Connect WEBSOCKET_CLIENT_SUCCESS: " + host + ":" + port.ToString());
                    keepAliveMessageReceived = false;
                    keepAliveCounter = 0;
                    keepAliveReconnect = false;
                    keepAliveTimerRepeatTime = 5000;
                    keepAliveTimer.Reset(1000, keepAliveTimerRepeatTime);
                    if (debug > 0)
                    {
                        CrestronConsole.PrintLine("\n Websocket connected.");
                    }
                    resultCodes = wsc.ReceiveAsync();

                    if (debug > 0)
                    {
                        CrestronConsole.Print("\n Websocket ReceiveAsync resultCodes: " + resultCodes.ToString());
                    }
                }
                else if (resultCodes.ToString().Contains("WEBSOCKET_CLIENT_ALREADY_CONNECTED"))
                {
                    if (debug > 0)
                    {
                        CrestronConsole.Print("\n Websocket re-initialize...");
                    }
                    wsc.Disconnect();
                }
                else
                {
                    if (debug > 0)
                    {
                        CrestronConsole.Print("\n Websocket could not connect to server.  Connect return code: " + resultCodes.ToString());
                    }
                    Disconnect();
                }
            }
            catch (Exception e)
            {
                if (debug > 0)
                {
                    CrestronConsole.Print("\n Websocket Connect exception: " + e.Message);
                }
                Disconnect();                
            }
        }

        public void Disconnect()
        {
            resultCodes = wsc.Disconnect();
            if (debug > 0)
            {
                CrestronConsole.Print("\n Websocket Disconnect resultCodes: \r\n" + resultCodes.ToString());
            }            
        }

        public int SendCallback(WebSocketClient.WEBSOCKET_RESULT_CODES error)
        {
            try
            {
                if (debug > 0)
                {
                    CrestronConsole.Print("\n Websocket SendCallback.");
                }
                
                if (debug > 0)
                {
                    CrestronConsole.Print("\n Websocket ReceiveAsync resultCodes: \r\n" + resultCodes.ToString());
                }
            }
            catch (Exception e)
            {
                if (debug > 0)
                {
                    CrestronConsole.Print("\n Websocket SendCallback exception: " + e.Message);
                }
                return -1;
            }
            
            return 0;
        }
        
        public int ReceiveCallback(byte[] data, uint datalen, WebSocketClient.WEBSOCKET_PACKET_TYPES opcode, WebSocketClient.WEBSOCKET_RESULT_CODES error)
        {
            try
            {                
                string receiveBuffer = Encoding.UTF8.GetString(data, 0, data.Length);
                if (debug > 0)
                {
                    CrestronConsole.Print("\n Websocket ReceiveCallback receiveBuffer: " + receiveBuffer);
                }

                if (receiveBuffer.Contains("webSocketClientKeepAlive"))
                {
                    keepAliveMessageReceived = true;
                    sendDataAsync("webSocketClientKeepAlive");
                }

                else if (receiveBuffer.Contains("webSocketClientDisconnect"))
                {
                    //Prepare keepAlive check routine to disconnect
                    //We can't just call disconnect here because ReceiveCallback must return a value before the socket can disconnect.
                    keepAliveMessageReceived = false;
                    keepAliveCounter = 2;
                }
                
                else 
                {
                    dataReceiveEvent(receiveBuffer);                    
                }
                
                resultCodes = wsc.ReceiveAsync();
                if (debug > 0)
                {
                    CrestronConsole.Print("\n Websocket ReceiveAsync resultCodes: " + resultCodes.ToString());
                }
            }
            catch (Exception e)
            {
                if (debug > 0)
                {
                    CrestronConsole.Print("\n Websocket ReceiveCallback exception: " + e.Message);
                }
                return -1;
            }                        
            return 0;
        }

        public void sendDataAsync(string dataToSend)
        {
            try
            {
                byte[] SendData = System.Text.Encoding.ASCII.GetBytes(dataToSend);
                wsc.SendAsync(SendData, (uint)SendData.Length, WebSocketClient.WEBSOCKET_PACKET_TYPES.LWS_WS_OPCODE_07__TEXT_FRAME, WebSocketClient.WEBSOCKET_PACKET_SEGMENT_CONTROL.WEBSOCKET_CLIENT_PACKET_END);
            }
            catch (Exception e)
            {
                CrestronConsole.Print("AsyncSendAndReceive exception: \r\n" + e.Message);
            }
        }

        private void keepAliveCheck(object obj)
        {                         
            if (keepAliveReconnect)
            {
                if (debug > 0)
                {
                    CrestronConsole.Print("\n Websocket keepAliveCheck reconnect");
                }
                Connect();
            }
            if (keepAliveMessageReceived)
            {
                if (debug > 0)
                {
                    CrestronConsole.Print("\n Websocket keepAliveCheck ok");
                }
                keepAliveMessageReceived = false;
                keepAliveCounter = 0;
            }
            else
            {
                
                keepAliveCounter++;
                if (keepAliveCounter > 2)
                {
                    if (debug > 0)
                    {
                        CrestronConsole.Print("\n Websocket keepAliveCheck failed");
                    }
                    //Slow down timer
                    keepAliveTimerRepeatTime = keepAliveTimerRepeatTime + 1000;
                    keepAliveTimer.Reset(1000, keepAliveTimerRepeatTime);
                    keepAliveReconnect = true;
                    Disconnect();
                }
            }
        } 
    }
}