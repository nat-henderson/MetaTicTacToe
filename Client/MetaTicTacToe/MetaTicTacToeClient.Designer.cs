using System.Drawing;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System;
using System.ComponentModel;
namespace MetaTicTacToe
{
    partial class MetaTicTacToeClient
    {
        private System.Windows.Forms.Button[] buttons = new System.Windows.Forms.Button[81];
        Thread netThread;
        enum State { START, PARSING, MYTURN, TRANSMITTED, OTHERTURN, FINISHED };
        volatile State state;
        volatile Socket connection;
        private delegate void processTakenList(string s);
        private delegate void processOwnedList(string s);
        private delegate void processMoveList(string s);

        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void _processTakenList(string s)
        {
            string[] integers = s.Split(' ');
            for (int i = 0; i < 9; i++)
            {
                int val;
                int[] retVal = new int[9];
                int tl = ((i / 3) * 27) + ((i % 3) * 3);
                retVal[0] = tl;
                retVal[1] = tl + 1;
                retVal[2] = tl + 2;
                retVal[3] = tl + 9;
                retVal[4] = tl + 10;
                retVal[5] = tl + 11;
                retVal[6] = tl + 18;
                retVal[7] = tl + 19;
                retVal[8] = tl + 20;
                if (Int32.TryParse(integers[i], out val))
                {
                    if (val == 1)
                    {
                        foreach (int bInd in retVal)
                        {
                            buttons[bInd].BackColor = Color.Red;
                        }
                    }
                    else if (val == -1)
                    {
                        foreach (int bInd in retVal)
                        {
                            buttons[bInd].BackColor = Color.Blue;
                        }
                    }
                }
            }
        }

        private void _processOwnedList(string s)
        {
            string[] integers = s.Split(' ');
            for (int i = 0; i < 81; i++)
            {
                int val;
                if (Int32.TryParse(integers[i], out val))
                {
                    if (val == 1)
                    {
                        buttons[i].Text = "X";
                    }
                    else if (val == -1)
                    {
                        buttons[i].Text = "O";
                    }
                }
            }
        }

        private void _processMoveList(string s)
        {
            string[] integers = s.Split(' ');
            foreach (string num in integers)
            { 
                int val;
                if (Int32.TryParse(num, out val))
                {
                    buttons[val].BackColor = Color.Green;
                }
            }
        }

        private void InitializeComponent()
        {
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(510, 510);
            this.Name = "Form1";
            this.Text = "Form1";
            for (int i = 0; i < 81; i++)
            {
                this.buttons[i] = new Button();
                this.buttons[i].Location = new System.Drawing.Point((i % 9) * 55 + 10, (i / 9) * 55 + 10);
                this.buttons[i].Name = "button"+i;
                this.buttons[i].Size = new System.Drawing.Size(35, 35);
                this.buttons[i].TabIndex = i;
                this.buttons[i].MouseClick += buttonClick;
                this.buttons[i].BackColor = Color.Gray;
                this.Controls.Add(buttons[i]);
            }
            connection = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            string hostname = Microsoft.VisualBasic.Interaction.InputBox("Enter the hostname to connect to.", "Hostname Prompt", "kvothe.case.edu");
            if (hostname.Equals(""))
            {
                Application.Exit();
            }
            try
            {
                connection.Connect(hostname, 30004);
            }
            catch (SocketException e)
            {
                MessageBox.Show("Networking error:  " + e.ErrorCode.ToString());
                Application.Exit();
            }
            netThread = new Thread(new ThreadStart(dealWithNetworkStuff));
            netThread.Start();
        }

        private void buttonClick(object sender, System.EventArgs e)
        {
            Button s = (Button)sender;
            lock(this)
                System.Console.Write(s.TabIndex);
            processClick(s.TabIndex, connection);
        }

        private void processClick(int toSend, Socket conn)
        {
            int sent = 0;
            System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
            while (sent == 0)
            {
                try
                {
                    Console.WriteLine("Trying to send!");
                    sent = conn.Send(encoding.GetBytes(toSend.ToString() + "\n"), SocketFlags.None);
                    state = State.TRANSMITTED;
                    Console.WriteLine("Sent! " + sent);
                }
                catch (SocketException e)
                {
                    if (e.SocketErrorCode == SocketError.WouldBlock || e.SocketErrorCode == SocketError.IOPending || e.SocketErrorCode == SocketError.NoBufferSpaceAvailable)
                    {
                        Console.WriteLine("Error!");
                        Thread.Sleep(1);
                    }
                    else
                    {
                        throw e;
                    }
                }
            }
            for (int i = 0; i < 81; i++)
            {
                buttons[i].BackColor = Color.Gray;
            }
        }

        private string[] getDataFromConnection(Socket conn)
        {
            byte [] buffer = new byte[4096];
            int received = 0;
            while (received == 0)
            {
                try
                {
                    received += conn.Receive(buffer, received, buffer.Length - received, SocketFlags.None);
                }
                catch (SocketException e)
                {
                    if (e.SocketErrorCode == SocketError.WouldBlock || e.SocketErrorCode == SocketError.IOPending || e.SocketErrorCode == SocketError.NoBufferSpaceAvailable)
                    {
                        Thread.Sleep(1);
                    }
                    else throw e;
                }
            }
            byte [] trim = new byte[received];
            for (int i = 0; i < received; i++)
            {
                trim[i] = buffer[i];
            }
            return Encoding.UTF8.GetString(trim).Split('\n');
        }

        private void dealWithNetworkStuff()
        {
            state = State.START;
            string[] transferString;
            while (state != State.FINISHED)
            {
                transferString = getDataFromConnection(connection);
                System.Console.WriteLine(transferString.Length);
                foreach (string s in transferString)
                {
                    lock (this)
                    {
                        if (state == State.FINISHED)
                        {
                            continue;
                        }
                        else if (s.Equals("STATE"))
                        {
                            state = State.PARSING;
                        }
                        else if (state == State.PARSING && s.Split(' ').Length == 9)
                        {
                            processTakenList p = _processTakenList;
                            this.Invoke(p, s);
                        }
                        else if (state == State.PARSING && s.Split(' ').Length == 81)
                        {
                            processOwnedList p = _processOwnedList;
                            this.Invoke(p, s);
                        }
                        else if (state == State.PARSING && s.Equals("TURN"))
                        {
                            state = State.MYTURN;
                        }
                        else if (state == State.MYTURN)
                        {
                            processMoveList p = _processMoveList;
                            this.Invoke(p, s);
                        }
                        else if (state == State.TRANSMITTED && s.Equals("ACPT"))
                        {
                            state = State.OTHERTURN;
                        }
                        else if (state == State.TRANSMITTED && s.Equals("STATE"))
                        {
                            state = State.PARSING;
                        }
                        else if (s.Equals("WINNER"))
                        {
                            state = State.FINISHED;
                            MessageBox.Show("You win!");
                        }
                        else if (s.Equals("LOSER"))
                        {
                            state = State.FINISHED;
                            MessageBox.Show("You lose!");
                        }
                    }
                    Console.WriteLine(s);
                    Console.WriteLine(state.ToString());
                }
            }
        }

        protected override void OnPaint(System.Windows.Forms.PaintEventArgs e)
        {
            base.OnPaint(e);
            Pen pen = new Pen(Color.Red, 5);
            e.Graphics.DrawLine(pen, 0, 165, 510, 165);
            e.Graphics.DrawLine(pen, 0, 330, 510, 330);
            e.Graphics.DrawLine(pen, 165, 0, 165, 510);
            e.Graphics.DrawLine(pen, 330, 0, 330, 510);
            pen.Dispose();
            pen = new Pen(Color.Green, 3);
            for (int i = 0; i < 3; i++)
            {
                e.Graphics.DrawLine(pen, 5 + i * 165, 55, 150 + i * 165, 55);
                e.Graphics.DrawLine(pen, 5 + i * 165, 110, 150 + i * 165, 110);
                e.Graphics.DrawLine(pen, 5 + i * 165, 220, 150 + i * 165, 220);
                e.Graphics.DrawLine(pen, 5 + i * 165, 275, 150 + i * 165, 275);
                e.Graphics.DrawLine(pen, 5 + i * 165, 385, 150 + i * 165, 385);
                e.Graphics.DrawLine(pen, 5 + i * 165, 440, 150 + i * 165, 440);

                e.Graphics.DrawLine(pen, 55, 5 + i * 165, 55, 150 + i * 165);
                e.Graphics.DrawLine(pen, 110, 5 + i * 165, 110, 150 + i * 165);
                e.Graphics.DrawLine(pen, 220, 5 + i * 165, 220, 150 + i * 165);
                e.Graphics.DrawLine(pen, 275, 5 + i * 165, 275, 150 + i * 165);
                e.Graphics.DrawLine(pen, 385, 5 + i * 165, 385, 150 + i * 165);
                e.Graphics.DrawLine(pen, 440, 5 + i * 165, 440, 150 + i * 165);
            }
        }

    }
}

