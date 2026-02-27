using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dhcp;
using System.Threading;
using System.Runtime.Remoting.Lifetime;
using System.Drawing.Text;


namespace DHCP_Query
{
    public partial class Form1 : Form
    {
        List<ListViewItem> globalItems = new List<ListViewItem>();
        int timeout = 500; //ping timeout

        private ListViewColumnSorter lvwColumnSorter;
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            lvwColumnSorter = new ListViewColumnSorter();
            listView1.ListViewItemSorter = lvwColumnSorter;

            var servers = DhcpServer.Servers;

            comboBox1.Items.Clear();

            foreach (DhcpServer server in servers)
            {
                comboBox1.Items.Add(server.Name);
            }
            if (comboBox1.Items.Count > 0)
            {
                comboBox1.SelectedIndex = 0;
            }

            foreach (var server in DhcpServer.Servers)
            {
                Console.WriteLine($"DHCP SERVER: {server.Name}");
                Console.WriteLine($"Connecting to: {server.Name}");
                DhcpServer dhcpServer = DhcpServer.Connect(server.Name);
                foreach (var scope in server.Scopes)
                {
                    Console.WriteLine($"SCOPE: {scope.Name} | {scope.Comment} | {scope.Address}");
                    foreach (var client in scope.Clients)
                    {
                        Console.WriteLine($"CLIENT: {client.Name} | {client.IpAddress} | {client.HardwareAddress} | {client.LeaseExpires} | {client.AddressState}");
                    }
                }
                break;
            }

            for (int i = 0; i < listView1.Columns.Count; i++)
            {
                listView1.Columns[i].Width = -1;
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            listView1.Items.Clear();
            globalItems.Clear();

            string server = comboBox1.Items[comboBox1.SelectedIndex].ToString();
            Console.WriteLine($"DHCP SERVER: {server}");
            Console.WriteLine($"CONNECTING TO: {server}");
            DhcpServer dhcpServer = DhcpServer.Connect(server);
            var scopes = dhcpServer.Scopes;
            foreach (var scope in scopes)
            {
                Console.WriteLine($"  SCOPE: {scope.Name} | {scope.Comment} | {scope.Address}");
                foreach (var client in scope.Clients)
                {
                    Console.WriteLine($"    CLIENT: {client.Name} | {client.IpAddress} | {client.HardwareAddress} | {client.LeaseExpires} | {client.AddressState}");
                    ListViewItem lvi = new ListViewItem();
                    lvi.Text = client.IpAddress.ToString();
                    lvi.SubItems.Add(client.Name);
                    lvi.SubItems.Add(client.HardwareAddress);
                    lvi.SubItems.Add(client.LeaseExpires.ToString());
                    lvi.SubItems.Add(client.LeaseExpired.ToString());
                    lvi.SubItems.Add(client.AddressState.ToString());
                    lvi.SubItems.Add("TBD");
                    lvi.SubItems.Add("------------");

                    Console.WriteLine($"Comment: {client.Comment}");
                    Console.WriteLine($"Name: {client.Name}");
                    Console.WriteLine($"Type: {client.Type}");

                    globalItems.Add(lvi);
                    listView1.Items.Add(lvi);
                    label4.Text = listView1.Items.Count.ToString();
                }
            }

            for (int i = 0; i < listView1.Columns.Count; i++)
            {
                listView1.Columns[i].Width = -1;
            }

            textBox1_TextChanged(sender, null);
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (textBox1.Text.Length > 0)
            {
                try
                {
                    Regex regex = new Regex(textBox1.Text, RegexOptions.IgnoreCase | RegexOptions.Compiled);
                    listView1.Items.Clear();

                    foreach (ListViewItem item in globalItems)
                    {
                        foreach (ListViewItem.ListViewSubItem subitem in item.SubItems)
                        {
                            bool matched = regex.IsMatch(subitem.Text);
                            if (matched)
                            {
                                Console.WriteLine($"[{textBox1.Text}] matches [{subitem.Text}]");
                                listView1.Items.Add(item);
                                break;
                            }
                        }
                    }

                    foreach (ListViewItem item in listView1.Items)
                    {
                        listView1.Items[item.Index].UseItemStyleForSubItems = false;
                        for (int i = 0; i < item.SubItems.Count; i++)
                        {
                            if (regex.IsMatch(item.SubItems[i].Text))
                            {
                                listView1.Items[item.Index].SubItems[i].ForeColor = Color.Red;
                            } else
                            {
                                listView1.Items[item.Index].SubItems[i].ForeColor = this.ForeColor;
                            }
                        }
                    }

                    label4.Text = listView1.Items.Count.ToString();
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine($"EXCEPTION: {ex.Message}");
                }
            } else
            {
                listView1.Items.Clear();
                foreach (ListViewItem item in globalItems)
                {
                    listView1.Items.Add(item);
                    label4.Text = listView1.Items.Count.ToString();
                }
                foreach (ListViewItem item in listView1.Items)
                {
                    for (int i = 0; i < item.SubItems.Count; i++)
                    {
                        listView1.Items[item.Index].SubItems[i].ForeColor = this.ForeColor;
                    }
                }
            }
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0)
            {
                return;
            }

            listView1.Enabled = false;
            for (int i = 0; i < listView1.SelectedItems.Count; i++)
            {
                Thread.Sleep(100);

                string mac = listView1.SelectedItems[i].SubItems[2].Text;
                string uri = $"https://www.macvendorlookup.com/api/v2/{mac}";

                HttpClient client = new HttpClient();
                HttpResponseMessage responseMessage = await client.GetAsync(uri, HttpCompletionOption.ResponseContentRead);
                if (responseMessage.IsSuccessStatusCode)
                {
                    string company = "";
                    string addr1 = "";
                    string addr2 = "";
                    string addr3 = "";
                    string country = "";
                    string type = "";

                    string bodyText = await responseMessage.Content.ReadAsStringAsync();

                    string pattern = "\"company\":\"(.*?)\",";
                    Match match = Regex.Match(bodyText, pattern);
                    if (match.Success && match.Groups.Count > 1)
                    {
                        company = match.Groups[1].Value;
                    }
                    pattern = "\"addressL1\":\"(.*?)\",";
                    match = Regex.Match(bodyText, pattern);
                    if (match.Success && match.Groups.Count > 1)
                    {
                        addr1 = match.Groups[1].Value;
                    }
                    pattern = "\"addressL2\":\"(.*?)\",";
                    match = Regex.Match(bodyText, pattern);
                    if (match.Success && match.Groups.Count > 1)
                    {
                        addr2 = match.Groups[1].Value;
                    }
                    pattern = "\"addressL3\":\"(.*?)\",";
                    match = Regex.Match(bodyText, pattern);
                    if (match.Success && match.Groups.Count > 1)
                    {
                        addr3 = match.Groups[1].Value;
                    }
                    pattern = "\"country\":\"(.*?)\",";
                    match = Regex.Match(bodyText, pattern);
                    if (match.Success && match.Groups.Count > 1)
                    {
                        country = match.Groups[1].Value;
                    }
                    pattern = "\"addressL1\":\"(.*?)\",";
                    match = Regex.Match(bodyText, pattern);
                    if (match.Success && match.Groups.Count > 1)
                    {
                        type = match.Groups[1].Value;
                    }
                    //MessageBox.Show($"{company}\r\n\r\n{addr1}\r\n{addr2}\r\n{addr3}\r\n{country}\r\n{type}", "OUI", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    listView1.SelectedItems[i].SubItems[7].Text = $"{company} | {addr1} {addr2} {addr3} {country} | {type}";
                    listView1.Columns[7].Width = -1;
                    this.Refresh();
                }
            }
            listView1.Enabled = true;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0)
            {
                return;
            }

            listView1.Enabled = false;
            for (int i = 0; i < listView1.SelectedItems.Count; i++)
            {
                Ping ping = new Ping();
                PingReply reply = ping.Send(listView1.SelectedItems[i].SubItems[0].Text, timeout);
                Console.WriteLine($"PING REPLY: {reply.Status} | {reply.RoundtripTime} ms | TIMEOUT: {timeout}");
                if (reply.Status == IPStatus.Success)
                {
                    listView1.SelectedItems[i].SubItems[6].Text = $"{reply.RoundtripTime.ToString()} ms";
                }
                else if (reply.Status == IPStatus.TimedOut || reply.Status == IPStatus.TimeExceeded)
                {
                    listView1.SelectedItems[i].SubItems[6].Text = "timeout";
                } else if (reply.Status == IPStatus.DestinationHostUnreachable || reply.Status == IPStatus.DestinationNetworkUnreachable || reply.Status == IPStatus.DestinationPortUnreachable || reply.Status == IPStatus.DestinationProtocolUnreachable || reply.Status == IPStatus.DestinationUnreachable) 
                {
                    listView1.SelectedItems[i].SubItems[6].Text = "unreachable";
                } else
                {
                    listView1.SelectedItems[i].SubItems[6].Text = "failed";
                }
                
                listView1.Columns[6].Width = -1;
                this.Refresh();
            }
            listView1.Enabled = true;
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                button1.Enabled = true;
                button2.Enabled = true;
                button3.Enabled = true;
                button4.Enabled = true;
                button5.Enabled = true;
                button6.Enabled = true;
                button7.Enabled = true;
                button8.Enabled = true;
                button9.Enabled = true;
                button10.Enabled = true;
                button11.Enabled = true;
                button12.Enabled = true;
            } else
            {
                button1.Enabled = false;
                button2.Enabled = false;
                button3.Enabled = false;
                button4.Enabled = false;
                button5.Enabled = false;
                button6.Enabled = false;
                button7.Enabled = false;
                button8.Enabled = false;
                button9.Enabled = false;
                button10.Enabled = false;
                button11.Enabled = false;
                button12.Enabled = false;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            listView1.Enabled = false;
            for (int i = 0; i < listView1.SelectedItems.Count; i++)
            {
                System.Diagnostics.Process.Start("mstsc.exe", $"/v:{listView1.SelectedItems[i].Text} /admin");
            }
            listView1.Enabled = true;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            listView1.Enabled = false;
            for (int i = 0; i < listView1.SelectedItems.Count; i++)
            {
                System.Diagnostics.Process.Start("C:\\Program Files\\TightVNC\\tvnviewer.exe", $"-host={listView1.SelectedItems[i].Text}");
            }
            listView1.Enabled = true;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            listView1.Enabled = false;
            for (int i = 0; i < listView1.SelectedItems.Count; i++)
            {
                System.Diagnostics.Process.Start("C:\\Program Files\\PuTTY\\putty.exe", listView1.SelectedItems[i].Text);
            }
            listView1.Enabled = true;
        }

        private void button6_Click(object sender, EventArgs e)
        {
            listView1.Enabled = false;
            for (int i = 0; i < listView1.SelectedItems.Count; i++)
            {
                System.Diagnostics.Process.Start("msinfo32.exe", $"/computer {listView1.SelectedItems[i].Text}");
            }
            listView1.Enabled = true;
        }

        private void button7_Click(object sender, EventArgs e)
        {
            listView1.Enabled = false;
            for (int i = 0; i < listView1.SelectedItems.Count; i++)
            {
                System.Diagnostics.Process.Start("eventvwr.msc", $"/computer:{listView1.SelectedItems[i].Text}");
            }
            listView1.Enabled = true;
        }

        private void button8_Click(object sender, EventArgs e)
        {
            listView1.Enabled = false;
            for (int i = 0; i < listView1.SelectedItems.Count; i++)
            {
                System.Diagnostics.Process.Start("compmgmt.msc", $"/computer:{listView1.SelectedItems[i].Text}");
            }
            listView1.Enabled = true;
        }

        private void button9_Click(object sender, EventArgs e)
        {
            listView1.Enabled = false;
            for (int i = 0; i < listView1.SelectedItems.Count; i++)
            {
                ProcessStartInfo info = new ProcessStartInfo(@"C:\Windows\Sysnative\cmd.exe", $"/c query session /server:{listView1.SelectedItems[i].Text}");
                info.UseShellExecute = false;
                info.RedirectStandardOutput = true;

                Process proc = new Process();
                proc.StartInfo = info;
                proc.Start();
                string[] output = (proc.StandardOutput.ReadToEnd()).Split('\n');
                proc.WaitForExit();

                foreach (string line in output)
                {
                    if (line.Contains("console"))
                    {
                        string pattern = @"^\s*console\s+\S+\s+(\d+)\s+Active.*$";
                        MatchCollection matches = Regex.Matches(line, pattern);

                        if (matches.Count > 0)
                        {
                            if (matches[0].Groups.Count > 0)
                            {
                                int sessionID = int.Parse(matches[0].Groups[1].Value);
                                System.Diagnostics.Process.Start("mstsc", $"/v:{listView1.SelectedItems[i].Text} /shadow:{sessionID} /noConsentPrompt /prompt");
                            }
                        }
                        break;
                    }
                }
                
            }
            listView1.Enabled = true;
        }

        private void testToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string copyText = string.Empty;
            for (int i = 0; i < listView1.SelectedItems.Count; i++)
            {
                for (int j = 0; j < listView1.SelectedItems[i].SubItems.Count; j++)
                {
                    copyText += listView1.SelectedItems[i].SubItems[j].Text + '\t';
                }
                copyText += Environment.NewLine;
            }
            Clipboard.SetText(copyText);
        }

        private void listView1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.C)
            {
                testToolStripMenuItem_Click((object)sender, e);
            }

            if (e.Control && e.KeyCode == Keys.A)
            {
                for (int i = 0; i < listView1.Items.Count; i++)
                {
                    listView1.Items[i].Selected = true;
                }
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            listView1.Enabled = false;
            for (int i = 0; i < listView1.SelectedItems.Count; i++)
            {
                ProcessStartInfo info = new ProcessStartInfo("powershell.exe", "Invoke-Command -ComputerName " + listView1.SelectedItems[i].Text + " -ScriptBlock { echo 'HOSTNAME' ; hostname ; echo '=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-' ; echo 'NETSTAT' ; netstat -ab -o ; echo '=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-' ; echo 'IPCONFIG' ; ipconfig /all ; echo '=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-' ; echo 'STATS' ; netstat -e -r -s ; echo '=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-' ; echo 'ARP CACHE' ; arp -a ; echo '=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-=-+-' ; echo 'SHARES' ; net share }");
                info.UseShellExecute = false;
                info.RedirectStandardOutput = true;

                Process proc = new Process();
                proc.StartInfo = info;
                proc.Start();
                string output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();

                netstatOutput netstatForm = new netstatOutput(listView1.SelectedItems[i].Text, output);
                netstatForm.Show();
            }
            listView1.Enabled = true;
        }

        private void button11_Click(object sender, EventArgs e)
        {
            listView1.Enabled = false;
            for (int i = 0; i < listView1.SelectedItems.Count; i++)
            {
                ProcessStartInfo info = new ProcessStartInfo(@"tracert.exe", listView1.SelectedItems[i].Text);
                info.UseShellExecute = false;
                info.RedirectStandardOutput = true;

                Process proc = new Process();
                proc.StartInfo = info;
                proc.Start();
                string output = (proc.StandardOutput.ReadToEnd());
                proc.WaitForExit();
                MessageBox.Show(output, $"TRACERT: {listView1.SelectedItems[i].Text}", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            listView1.Enabled = true;
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {

        }

        private void listView1_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (e.Column == lvwColumnSorter.SortColumn)
            {
                lvwColumnSorter.Order = (lvwColumnSorter.Order == SortOrder.Ascending) ? SortOrder.Descending : SortOrder.Ascending;
            } else
            {
                lvwColumnSorter.SortColumn = e.Column;
                lvwColumnSorter.Order = SortOrder.Ascending;
            }

            listView1.Sort();
        }

        private void button12_Click(object sender, EventArgs e)
        {  
            msgForm msgFrm = new msgForm();
            DialogResult result = msgFrm.ShowDialog();

            if (result != DialogResult.OK)
            {
                return;
            }

            string msg = msgFrm.ReturnString;

            listView1.Enabled = false;
            for (int i = 0; i < listView1.SelectedItems.Count; i++)
            {
                Console.Write($"Sending message: [{msg}] to all users at destination: {listView1.SelectedItems[i].Text}");
                ProcessStartInfo info = new ProcessStartInfo(@"powershell.exe", "Invoke-Command -ComputerName " + listView1.SelectedItems[i].Text + " -ScriptBlock { msg * " + msg + " }");
                info.UseShellExecute = false;
                info.RedirectStandardOutput = true;

                Process proc = new Process();
                proc.StartInfo = info;
                proc.Start();
                string output = (proc.StandardOutput.ReadToEnd());
                proc.WaitForExit();
            }
            listView1.Enabled = true;
        }
    }
}
