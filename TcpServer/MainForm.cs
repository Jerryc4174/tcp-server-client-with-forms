using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using TcpServer.Properties;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ProgressBar;

namespace TcpServer
{
    public partial class MainForm : Form
    {
        private static readonly int RX_BUFFER_SIZE = 4 * 1024;
        private static readonly int SELECT_TIMEOUT = 100000; // in microseconds

        private bool connected = false;
        private bool keepRunning = true;

        private Dictionary<IPEndPoint, Socket> clientDictionary = new Dictionary<IPEndPoint, Socket>();
        private ConcurrentQueue<string> txQueue = new ConcurrentQueue<string>();

        private static readonly object stopLock = new object();

        private static readonly Log logger = LogManager.GetLogger("MainForm");
        
        public MainForm()
        {
            InitializeComponent();

            UpdateProductInfo();

            UpdateFormByConnectionState();


        }

        private void LogReceived(string clientEp, string s)
        {
            logger.LogInfo($"Received from {clientEp}:{s}");
           // receivedTextBox.AppendText($"<{DateTime.Now.ToString("yy-MM-dd HH:mm:ss.fff")}> <{clientEp}> <{s}>\r\n");
            receivedTextBox.Invoke((MethodInvoker)delegate
            {
                receivedTextBox.AppendText($"<{DateTime.Now.ToString("yy-MM-dd HH:mm:ss.fff")}> <{clientEp}> <{s}>\r\n");
            });
        }

        private void LogSent(string clientEp, string s)
        {
            logger.LogInfo($"Sent to {clientEp}:{s}");
            sentTextBox.Invoke((MethodInvoker)delegate
            {
                sentTextBox.AppendText($"<{DateTime.Now.ToString("yy-MM-dd HH:mm:ss.fff")}> <{clientEp}> <{s}>\r\n");
            });
            
        }

        private void LogDebug(string s)
        {
            logger.LogInfo($"Debug:{s}");
            //logTextBox.AppendText($"<{DateTime.Now.ToString("yy-MM-dd HH:mm:ss.fff")}> <{s}>\r\n");
            logTextBox.Invoke((MethodInvoker)delegate
            {
                logTextBox.AppendText($"<{DateTime.Now.ToString("yy-MM-dd HH:mm:ss.fff")}> <{s}>\r\n");
            });
        }

        public void UpdateProductInfo()
        {
            this.Text = $"{ProductInfo.PRODUCT_NAME} {ProductInfo.GetVersionString()}";
        }

        private void UpdateFormByConnectionState()
        {
            if (connected)
            {
                startButton.Text = Resources.Stop;
                startButton.BackColor = Color.Lime;
            }
            else
            {
                startButton.Text = Resources.Start;
                startButton.BackColor = Color.LightSkyBlue;
            }
        }

        private void ClientConnected(IPEndPoint clientEp, Socket s)
        {
            clientDictionary.Add(clientEp, s);
            //clientComboBox.Items.Add(clientEp);
            clientComboBox.Invoke((MethodInvoker)delegate { 
                clientComboBox.Items.Add(clientEp);
                if (clientComboBox.SelectedIndex < 0 && clientComboBox.Items.Count > 0)
                {
                    clientComboBox.SelectedIndex = 0;
                }
            });
            /*
            if (clientComboBox.SelectedIndex < 0 && clientComboBox.Items.Count > 0)
            {
                clientComboBox.SelectedIndex = 0;
            }*/
        }

        private void ClientDisconnected(IPEndPoint clientEp)
        {
            clientDictionary.Remove(clientEp);
            clientComboBox.Invoke((MethodInvoker)delegate
            {
                clientComboBox.Items.Remove(clientEp);
                if (clientComboBox.SelectedIndex < 0 && clientComboBox.Items.Count > 0)
                {
                    clientComboBox.SelectedIndex = 0;
                }
            });
        }
        
        private void ListenHandler(object obj)
        {
            ArrayList rlist = null;
           

            try
            {
                var server = (Socket)obj;

                rlist = new ArrayList
                {
                    server
                };

                var checkRead = new ArrayList();
                var checkWrite = new ArrayList();

                checkRead.AddRange(rlist);

                var buffer = new byte[RX_BUFFER_SIZE];
                var pktBuffer = new byte[64];

                while (keepRunning && rlist.Count > 0)
                {
                    //LogInfo("Select ...");
                    Socket.Select(checkRead, checkWrite, null, SELECT_TIMEOUT);

                    foreach (var socket in checkRead)
                    {
                        if (socket == server)
                        {
                            Socket client = server.Accept();
                            rlist.Add(client);
                            
                            IPEndPoint ep = (IPEndPoint)client.RemoteEndPoint;
                            ClientConnected(ep, client);
                            LogDebug($"Connection[{ep.ToString()}] established.");
                        }
                        else
                        {
                            IPEndPoint ep = (IPEndPoint)((Socket)socket).RemoteEndPoint;

                            try
                            {
                                int count = ((Socket)socket).Receive(buffer);

                                if (count > 0)
                                {
                                    Array.Copy(buffer, pktBuffer, count);

                                    string receivedStr = Encoding.UTF8.GetString(buffer, 0, count);
                                    LogReceived(ep.ToString(), $"{receivedStr.TrimEnd('\r', '\n')}");

                                    UpdatePopUpDialog(pktBuffer, count);
                                    
                                }
                                else
                                {
                                    ClientDisconnected(ep);
                                    LogDebug($"Connection[{ep.ToString()}] closed.");
                                    ((Socket)socket).Close();
                                    rlist.Remove(socket);
                                }
                            }
                            catch (Exception e)
                            {
                                ClientDisconnected(ep);
                                LogDebug($"Connection[{ep.ToString()}] broken.");
                                ((Socket)socket).Close();
                                rlist.Remove(socket);
                            }
                        }
                    }

                    foreach (var socket in checkWrite)
                    {
                        IPEndPoint ep = (IPEndPoint)((Socket)socket).RemoteEndPoint;

                        try
                        {
                            while (txQueue.TryDequeue(out string lineToSend))
                            {
                                ((Socket)socket).Send(Encoding.UTF8.GetBytes($"{lineToSend}\r\n"));
                                LogSent(ep.ToString(), $"{lineToSend}");
                            }
                        }
                        catch (Exception e)
                        {
                            ClientDisconnected(ep);
                            LogDebug($"Connection[{ep.ToString()}] broken.");
                            ((Socket)socket).Close();
                            rlist.Remove(socket);
                        }
                    }

                    checkRead.Clear();
                    checkWrite.Clear();

                    checkRead.AddRange(rlist);

                    if (txQueue.Count > 0)
                    {
                         
                        //var clientEp = (IPEndPoint)(clientComboBox.SelectedItem);
                        clientComboBox.Invoke((MethodInvoker)delegate{
                            var clientEp = (IPEndPoint)(clientComboBox.SelectedItem);
                            if (clientEp != null)
                            {
                                clientDictionary.TryGetValue(clientEp, out Socket socket);
                                if (socket != null)
                                {
                                    checkWrite.Add(socket);
                                }
                            }
                        });
                        
                    }
                } // while (keepRunning && rlist.Count > 0)
            }
            catch (Exception ex)
            {
                LogDebug(ex.ToString());
            }
            finally
            {
                if (rlist != null)
                {
                    foreach (var socket in rlist)
                    {
                        try
                        {
                            ((Socket)socket).Close();
                        }
                        catch (Exception ex)
                        {
                            LogDebug(ex.ToString());
                        }
                    }

                    rlist.Clear();
                }

                clientDictionary.Clear();
                //clientComboBox.Items.Clear();
                clientComboBox.Invoke((MethodInvoker)delegate { 
                    clientComboBox.Items.Clear();
                });

                LogDebug("Server stopped running.");

                lock (stopLock)
                {
                    keepRunning = false;
                    connected = false;

                    Monitor.PulseAll(stopLock);
                }
            }
        }

        private Socket OpenConnection()
        {
            Socket server = null;

            try
            {
                var portStr = portTextBox.Text.Trim();
                if (string.IsNullOrEmpty(portStr))
                {
                    LogDebug("Port is empty.");
                    return null;
                }

                var port = int.Parse(portStr);

                server = new Socket(AddressFamily.InterNetwork,
                             SocketType.Stream,
                             ProtocolType.Tcp);
                server.Bind(new IPEndPoint(IPAddress.Any, port));
                server.Listen(2);

                connected = true;

                return server;
            }
            catch (Exception ex)
            {
                if (server != null)
                {
                    server.Close();
                }

                throw ex;
            }
        }

        private void CloseConnection()
        {
            lock (stopLock)
            {
                keepRunning = false;
                startButton.Invoke((MethodInvoker) delegate{
                    startButton.Text = "Start";
                });
                connected = false;
                        
               // while (connected) Monitor.Wait(stopLock);
            }
        }
        
        private void StartButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (connected)
                {
                    CloseConnection();

                    UpdateFormByConnectionState();
                }
                else
                {
                    Socket server = OpenConnection();

                    if (server != null)
                    {
                        keepRunning = true;

                        Thread thread = new Thread(ListenHandler);
                        thread.Start(server);

                        LogDebug($"Server listening on {server.LocalEndPoint.ToString()} ...");
                    }
                }
            }
            catch (Exception ex)
            {
                LogDebug(ex.Message);
            }
            finally
            {
                UpdateFormByConnectionState();
            }
        }

        private void SendButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (clientComboBox.SelectedItem != null)
                {
                    string lineToSend = $"{sendTextBox.Text}";
                    txQueue.Enqueue(lineToSend);
                }
                else
                {
                    LogDebug("Client not selected.");
                }
            }
            catch (Exception ex)
            {
                LogDebug(ex.Message);
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            CloseConnection();

            Settings.Default.ListeningPort = portTextBox.Text;
            Settings.Default.TextToSend = sendTextBox.Text;
            
            if (WindowState == FormWindowState.Normal)
            {
                Settings.Default.WindowLocation = Location;
                Settings.Default.WindowSize = Size;
            }
            else
            {
                Settings.Default.WindowLocation = RestoreBounds.Location;
                Settings.Default.WindowSize = RestoreBounds.Size;
            }

            Settings.Default.Save();
        }

        private void SendTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SendButton_Click(sender, e);
            }
        }

        private void ClearSentButton_Click(object sender, EventArgs e)
        {
            sentTextBox.Clear();
        }

        private void ClearReceivedButton_Click(object sender, EventArgs e)
        {
            receivedTextBox.Clear();
        }

        private void ClearLogButton_Click(object sender, EventArgs e)
        {
            logTextBox.Clear();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            if (Settings.Default.ListeningPort != null)
            {
                portTextBox.Text = Settings.Default.ListeningPort;
            }

            if (Settings.Default.TextToSend != null)
            {
                sendTextBox.Text = Settings.Default.TextToSend;
            }
            
            if (Settings.Default.WindowLocation != null)
            {
                Location = Settings.Default.WindowLocation;
            }

            if (Settings.Default.WindowSize != null)
            {
                Size = Settings.Default.WindowSize;
            }
        }

        private void UpdatePopUpDialog(byte[] pktBuffer, int count)
        {
            var popupStat = new popupStatus();
            // byte 0 bits 8-15 of status bits
            // byte 1 bits 0-7  of status bits
            popupStat.SetCB16(Convert.ToBoolean(pktBuffer[0] & 0x80));
            popupStat.SetCB15(Convert.ToBoolean(pktBuffer[0] & 0x40));
            popupStat.SetCB14(Convert.ToBoolean(pktBuffer[0] & 0x20));
            popupStat.SetCB13(Convert.ToBoolean(pktBuffer[0] & 0x10));
            popupStat.SetCB12(Convert.ToBoolean(pktBuffer[0] & 0x08));
            popupStat.SetCB11(Convert.ToBoolean(pktBuffer[0] & 0x04));
            popupStat.SetCB10(Convert.ToBoolean(pktBuffer[0] & 0x02));
            popupStat.SetCB9(Convert.ToBoolean(pktBuffer[0] & 0x01));
            popupStat.SetCB8(Convert.ToBoolean(pktBuffer[1] & 0x80));
            popupStat.SetCB7(Convert.ToBoolean(pktBuffer[1] & 0x40));
            popupStat.SetCB6(Convert.ToBoolean(pktBuffer[1] & 0x20));
            popupStat.SetCB5(Convert.ToBoolean(pktBuffer[1] & 0x10));
            popupStat.SetCB4(Convert.ToBoolean(pktBuffer[1] & 0x08));
            popupStat.SetCB3(Convert.ToBoolean(pktBuffer[1] & 0x04));
            popupStat.SetCB2(Convert.ToBoolean(pktBuffer[1] & 0x02));
            popupStat.SetCB1(Convert.ToBoolean(pktBuffer[1] & 0x01));
            int outFreq = (pktBuffer[2] * 256 + pktBuffer[3]);
            float avgRMSCur = (pktBuffer[4] * 256 + pktBuffer[5]) / 10.0f;
            float avgRMSVolt = (pktBuffer[6] * 256 + pktBuffer[7]) / 10.0f;
            float VS1 = (pktBuffer[8] * 256 + pktBuffer[9]) / 10.0f;
            float VS2 = (pktBuffer[10] * 256 + pktBuffer[11]) / 10.0f;
            float gndFltCurr = (pktBuffer[12] * 256 + pktBuffer[13]) / 10.0f;
            float temperature = (pktBuffer[14] * 256 + pktBuffer[15]) / 10.0f;
            int swvMaj = pktBuffer[16];
            int swvMin = pktBuffer[17];
            int badCmd = pktBuffer[18];
            popupStat.SetDtb1Text(outFreq.ToString());
            popupStat.SetDtb2Text(avgRMSCur.ToString());
            popupStat.SetDtb3Text(avgRMSVolt.ToString());
            popupStat.SetDtb4Text(VS1.ToString());
            popupStat.SetDtb5Text(VS2.ToString());
            popupStat.SetDtb6Text(gndFltCurr.ToString());
            popupStat.SetDtb7Text(temperature.ToString());
            popupStat.SetDtb8Text(swvMaj.ToString());
            popupStat.SetDtb9Text(swvMin.ToString());
            popupStat.SetDtb10Text(badCmd.ToString());


            popupStat.ShowDialog();
        }
    }
}
