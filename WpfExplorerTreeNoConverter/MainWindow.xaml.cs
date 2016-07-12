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
using System.IO;
using System.Net;
using System.Windows.Forms;

namespace FTPClient
{
    public partial class MainWindow : Window
    {
        private readonly object _dummyNode = null;
        internal static IPAddress ip;
        internal static int port;
        public static Client client = null;
        public static string currentPath = string.Empty;
        string directory = "directory";

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(MainWindow_Loaded);
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (string drive in Directory.GetLogicalDrives())
            {
                TreeViewItem item = new TreeViewItem();
                item.Header = drive;
                item.Tag = drive;
                item.Items.Add(_dummyNode);
                item.Expanded += folder_Expanded;

                // Apply the attached property so that the triggers know that this is root item.
                TreeViewItemProps.SetIsRootLevel(item, true);

                foldersTree.Items.Add(item);
            }
        }

        /// <summary>
        /// Client Treeview
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void folder_Expanded(object sender, RoutedEventArgs e)
        {
            TreeViewItem item = (TreeViewItem)sender;
            if (item.Items.Count == 1 && item.Items[0] == _dummyNode)
            {
                item.Items.Clear();
                try
                {
                    foreach (string dir in Directory.GetDirectories(item.Tag as string))
                    {
                        TreeViewItem subitem = new TreeViewItem();
                        subitem.Header = new DirectoryInfo(dir).Name;
                        subitem.Tag = dir;
                        subitem.Items.Add(_dummyNode);
                        subitem.Expanded += folder_Expanded;
                        item.Items.Add(subitem);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();

            btnCreateDirectoryAtServer.IsEnabled = true;
            btnConnect.IsEnabled = false;
            btnDeleteFromServer.IsEnabled = true;
            btnDisconnect.IsEnabled = true;
            btnDownload.IsEnabled = true;
            btnRenameOnServer.IsEnabled = true;
            btnUploadDirectory.IsEnabled = true;
            btnUploadFile.IsEnabled = true;
        }

        private void btnDownload_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                client.DownloadElement(currentPath);
            }
            catch (Exception)
            {
                System.Windows.MessageBox.Show("No connection to server");
                DisconnectClient();
            }            
        }

        /// <summary>
        /// Doubleclick event on Treeview to get child directory
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tvServer_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            TreeViewItem treeViewItem = GetSelectedItem((FrameworkElement)e.OriginalSource, tvServer);

            if (treeViewItem != null)
            {
                // if the selected item is a Folder
                if (treeViewItem.Name.Equals(directory))
                {
                    currentPath = (Convert.ToString(treeViewItem.ToolTip));
                    client.SendChildNodeRequest(Convert.ToString(treeViewItem.ToolTip));
                }
            }
        }

        /// <summary>
        /// Method to get selected Treeview Item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="objTreeViewControl"></param>
        /// <returns></returns>
        private TreeViewItem GetSelectedItem(UIElement sender, UIElement objTreeViewControl)
        {
            Point point = sender.TranslatePoint(new Point(0, 0), objTreeViewControl);
            var isHitTestAvailable = objTreeViewControl.InputHitTest(point) as DependencyObject;
            while (isHitTestAvailable != null && !(isHitTestAvailable is TreeViewItem))
            {
                isHitTestAvailable = VisualTreeHelper.GetParent(isHitTestAvailable);
            }
            return isHitTestAvailable as TreeViewItem;
        }

        /// <summary>
        /// Starts connection
        /// </summary>
        /// <param name="ipS">IP from Login Window</param>
        /// <param name="portS">Port from Login Window</param>
        internal void SetServerData(IPAddress ipS, int portS)
        {
            ip = ipS;
            port = portS;
            client = new Client(ip, port);
            client.Start();
        }

        private void btnDisconnect_Click(object sender, RoutedEventArgs e)
        {
            DisconnectClient();
        }

        /// <summary>
        /// End program
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnEndFTPClient_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                client.Disconnect();
            }
            catch (Exception)
            {
                // is thorwn when client not connected, doesnt matter
            }

            Environment.Exit(0);
        }

        private void btnDeleteFromServer_Click(object sender, RoutedEventArgs e)
        {
               try
            {
                 client.DeleteElement(currentPath);
            }
            catch (Exception)
            {
                System.Windows.MessageBox.Show("No connection to server");
                DisconnectClient();
            }            
        }

        private void btnCreateDirectoryAtServer_Click(object sender, RoutedEventArgs e)
        {
            NewName newName = new NewName(0, currentPath);
            newName.Show();
        }

        private void btnRenameOnServer_Click(object sender, RoutedEventArgs e)
        {
            NewName newName = new NewName(1, currentPath);
            newName.Show();
        }

        /// <summary>
        /// On closing main windows disconnect from server before closing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FTP_Client_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                client.Disconnect();
            }
            catch (Exception)
            {
                // is thorwn when client not connected, doesnt matter
            }

            Environment.Exit(0);
        }

        private void btnUploadFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();

            dialog.ShowDialog();
            dialog.InitialDirectory = "c:\\";
            string path = dialog.FileName;

            if (path != string.Empty)
            {
                try
                {
                    client.SendUploadFileRequest(path, currentPath);
                }
                catch (Exception)
                {
                    System.Windows.MessageBox.Show("No connection to server");
                    DisconnectClient();
                }  
            }
        }

        private void btnUploadDirectory_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog folderDialog = new FolderBrowserDialog();
            folderDialog.SelectedPath = "C:\\";

            DialogResult result = folderDialog.ShowDialog();

            // if user selects a folder
            if (result.ToString() == "OK")
            {
                try
                {
                    client.SendUploadDirectoryRequest(folderDialog.SelectedPath, currentPath + "\\");
                }
                catch (Exception)
                {
                    System.Windows.MessageBox.Show("No connection to server");
                    DisconnectClient();
                }  
            }
        }

        private void DisconnectClient()
        {
            try
            {
                client.Disconnect();
            }
            catch (Exception)
            {
                //only if not connected
            }

            btnCreateDirectoryAtServer.IsEnabled = false;
            btnConnect.IsEnabled = true;
            btnDeleteFromServer.IsEnabled = false;
            btnDisconnect.IsEnabled = false;
            btnDownload.IsEnabled = false;
            btnRenameOnServer.IsEnabled = false;
            btnUploadDirectory.IsEnabled = false;
            btnUploadFile.IsEnabled = false;
        }
    }
}