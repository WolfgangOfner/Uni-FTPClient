using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Net;
using System.Net.Sockets;
using System.Linq;

namespace FTPClient
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Enable port textbox after Ip is inserted
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tbIp_TextChanged(object sender, TextChangedEventArgs e)
        {
            tbPort.IsEnabled = true;
        }

        /// <summary>
        /// Enable user textbox after port is inserted
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tbPort_TextChanged(object sender, TextChangedEventArgs e)
        {
            btnConnect.IsEnabled = true;
        }

        /// <summary>
        /// close this window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Establish connection to the FTP server
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            IPAddress ip = null;
            int port = 0;
            bool error = false;
           
            // if " " in ip input
            if (tbIp.Text.Contains(" "))
            {
                tbIp.Text = tbIp.Text.Replace(" ", "");
            }

            try
            {
                ip = IPAddress.Parse(tbIp.Text);
            }
            catch (Exception)
            {
                tbIp.Text = "Wrong Ip";
                error = true;
            }

            try
            {
                port = Convert.ToInt32(tbPort.Text);
            }
            catch (Exception)
            {
                tbPort.Text = "Wrong Port";
                error = true;
            }

            if (!error)
            {
                MainWindow window = new MainWindow();
                window.SetServerData(ip, port);
                this.Close();
            }
        }
    }
}
