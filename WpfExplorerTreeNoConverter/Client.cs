using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using ClassLibrary1;
using System.Windows.Controls;
using System.IO;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System.Collections.Generic;

namespace FTPClient
{
    public class Client
    {
        IPEndPoint ipEndPoint;
        TcpClient tcpClient = null;
        NetworkStream ns = null;
        static string root = string.Empty;
        static string downloadDirectory = @"C:\download\";
        string directory = "directory";
        
        public Client(IPAddress ipAddress, int port)
        {
            ipEndPoint = new IPEndPoint(ipAddress, port);
            tcpClient = new TcpClient();
        }

        internal void CreateDirectoryAtServer(string name)
        {
            TreeViewItem treeViewItem = FindSelectedServerTreeViewItemOnServer();

            // if an element was selected on the server
            if (treeViewItem != null)
            {
                string path = Convert.ToString(treeViewItem.ToolTip);
                int index = 0;

                for (int i = path.Length - 1; i > 0; i--)
                {
                    // find last // to delete //name
                    if (path[i] == 92)
                    {
                        index = i;
                        break;
                    }
                }

                // if a directory was selected but not opened delete the name of the selected folder
                path = path.Remove(index, path.Length - index);
                SendCreateFileRequest(path, name);
            }
            // else just create the directory in the root directory
            else
            {
                SendCreateDirectoryRequest(name);
            }
        }

        internal void DeleteElement(string path)
        {
            TreeViewItem treeViewItem = FindSelectedServerTreeViewItemOnServer();

            if (treeViewItem != null)
            {
                // if treeVieItem == node
                if (treeViewItem.Name.Equals(directory))
                {
                    SendDeleteDirectoryRequest(Convert.ToString(treeViewItem.ToolTip));
                }
                // treeViewItem is a childe == file
                else
                {
                    SendDeleteFileRequest(path + "\\" + Convert.ToString(treeViewItem.ToolTip));
                }

            }
            else
            {
                MessageBox.Show("No element on server selected.");
            }
        }

        /// <summary>
        /// Disconnect client
        /// </summary>
        internal void Disconnect()
        {
            ClearTreeview();
            ns.Flush();
            ns.Close();
            ns.Dispose();
            tcpClient.Close();
        }

        internal void DownloadElement(string path)
        {
            TreeViewItem treeViewItem = FindSelectedServerTreeViewItemOnServer();

            if (treeViewItem != null)
            {
                if (treeViewItem.Name.Equals(directory))
                {
                    SendDownloadDirectoryRequest(Convert.ToString(treeViewItem.ToolTip));
                }
                else
                {
                    SendDownloadFileRequest(path + "\\" + Convert.ToString(treeViewItem.ToolTip));
                }
            }
            else
            {
                MessageBox.Show("No element on server selected");
            }
        }

        internal void RenameDirectoryAtServer(string newName, string currentPath)
        {
            TreeViewItem treeViewItem = FindSelectedServerTreeViewItemOnServer();

            if (treeViewItem != null)
            {
                if (treeViewItem.Name.Equals(directory))
                {
                    string path = Convert.ToString(treeViewItem.ToolTip);
                    int index = 0;

                    for (int i = path.Length - 1; i > 0; i--)
                    {
                        // find last // to delete //name
                        if (path[i] == 92)
                        {
                            index = i;
                            break;
                        }
                    }

                    // if a directory was selected but not opened delete the name of the selected folder
                    path = path.Remove(index, path.Length - index);
                    newName = path + "\\" + newName;
                    SendRenameDirectoryRequest(Convert.ToString(treeViewItem.ToolTip), newName);
                }
                else
                {
                    string path = currentPath + "\\" + Convert.ToString(treeViewItem.ToolTip);
                    newName = currentPath + "\\" + newName;
                    SendRenameFileRequest(path, newName);
                }

            }
            else
            {
                MessageBox.Show("No directory or file selected");
            }
        }

        /// <summary>
        /// Send request for subdirectories of root
        /// </summary>
        /// <param name="directory">Requested directory</param>
        internal void SendChildNodeRequest(string directory)
        {
            NetzwerkDings netzDings = new NetzwerkDings();
            netzDings.RequestedDirectoryPath = directory;

            NetworkTalk.SendPackage(netzDings, tcpClient.GetStream());
            ReceiveStructure();
        }

        internal void SendUploadDirectoryRequest(string directoryPath, string serverPath)
        {
            NetzwerkDings netzDings = new NetzwerkDings();

            // if user didnt select a path on the server
            if (serverPath.Equals(string.Empty))
            {
                serverPath = root;
            }

            DirectoryInfo directory = new DirectoryInfo(directoryPath);
            netzDings.DownloadFiles = new List<FileInfo>();
            netzDings.Filepaths = new List<string>();

            try
            {
                netzDings.DownloadDirectory = Directory.GetDirectories(directoryPath, "*", SearchOption.AllDirectories);
            }
            catch (Exception)
            {
            }

            FileInfo[] file = directory.GetFiles();

            // add for each file the path (from the first directory)
            foreach (var item in file)
            {
                netzDings.DownloadFiles.Add(item);
                netzDings.Filepaths.Add(RemoveRootPath(directoryPath, directoryPath) + "\\");
            }

            // add for each file the path (all subdirectories) 
            for (int i = 0; i < netzDings.DownloadDirectory.Length; i++)
            {
                directory = new DirectoryInfo(netzDings.DownloadDirectory[i]);
                file = directory.GetFiles();

                foreach (var item in file)
                {
                    netzDings.DownloadFiles.Add(item);
                    netzDings.Filepaths.Add(RemoveRootPath(netzDings.DownloadDirectory[i], directoryPath) + "\\");
                }
            }

            // remove path from directory till only name is left
            for (int i = 0; i < netzDings.DownloadDirectory.Length; i++)
            {
                netzDings.DownloadDirectory[i] = RemoveRootPath(netzDings.DownloadDirectory[i], directoryPath);
            }

            // if no subdirectories are selected use only requested path
            if (netzDings.DownloadDirectory.Length == 0)
            {
                netzDings.DownloadDirectory = new string[1];
                netzDings.DownloadDirectory[0] = RemoveRootPath(directoryPath, directoryPath);
            }

            netzDings.RequestedDirectoryPath = serverPath;
            netzDings.DirectoryUpload = true;
            NetworkTalk.SendPackage(netzDings, tcpClient.GetStream());

            netzDings = null;

            do
            {
                if (ns.DataAvailable)
                {
                    netzDings = NetworkTalk.RecievePackage(ns);
                }
            }
            while (netzDings == null);

            if (netzDings.Error)
            {
                MessageBox.Show("At least one file or directory couldn't be uploaded");
            }
            else
            {
                MessageBox.Show("Directories and files uploded");
            }
        }

        /// <summary>
        /// Upload a file
        /// </summary>
        /// <param name="path">Path of the file</param>
        /// <param name="currentDirectory">Current selected directory on the server</param>
        internal void SendUploadFileRequest(string path, string currentDirectory)
        {
            NetzwerkDings netzDings = new NetzwerkDings();
            FileInfo fileInfo = new FileInfo(path);
            netzDings.DownloadFiles = new List<FileInfo>();
            netzDings.RequestedDirectoryPath = path;
            netzDings.FileUpload = true;
            netzDings.DownloadFiles.Add(fileInfo);
            netzDings.RequestedDirectoryPath = currentDirectory;

            NetworkTalk.SendPackage(netzDings, tcpClient.GetStream());

            netzDings = null;

            do
            {
                if (ns.DataAvailable)
                {
                    netzDings = NetworkTalk.RecievePackage(ns);
                }
            }
            while (netzDings == null);

            // set if there was an error on the server uploading the file
            if (netzDings.Error)
            {
                MessageBox.Show("An error occured");
            }
            else
            {
                MessageBox.Show("File uploded");
            }
        }

        internal void Start()
        {
            ns = EstablishConnection();

            ReceiveStructure();
        }

        /// <summary>
        /// Clear treeview for refresh
        /// </summary>
        private void ClearTreeview()
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (window.GetType() == typeof(MainWindow))
                {
                    while ((window as MainWindow).tvServer.Items.Count > 0)
                    {
                        (window as MainWindow).tvServer.Items.RemoveAt(0);
                    }
                }

                break;
            }
        }

        private NetworkStream EstablishConnection()
        {
            // Connect the client to the endpoint.
            // Try to fetch the network stream
            try
            {
                tcpClient.Connect(ipEndPoint);
                ns = tcpClient.GetStream();
            }
            catch
            {
            }

            return ns;
        }

        /// <summary>
        /// Get data of selected treeView Element on server
        /// </summary>
        /// <returns></returns>
        private TreeViewItem FindSelectedServerTreeViewItemOnServer()
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (window.GetType() == typeof(MainWindow))
                {
                    TreeViewItem treeViewItem = (TreeViewItem)(window as MainWindow).tvServer.SelectedItem;

                    if (treeViewItem != null)
                    {
                        return treeViewItem;
                    }
                }
            }

            TreeViewItem errorTreeViewItem = null;
            return errorTreeViewItem;
        }

        private void ReceiveDownloadedFiles()
        {
            NetzwerkDings netzDings = null;
            bool success = false;
            do
            {
                if (ns.DataAvailable)
                {
                    netzDings = NetworkTalk.RecievePackage(ns);
                }
            }
            while (netzDings == null);

            if (netzDings.DownloadDirectory == null)
            {
                if (!Directory.Exists(downloadDirectory))
                {
                    try
                    {
                        Directory.CreateDirectory(downloadDirectory);
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            else
            {
                // Create downloaded folders if they dont exist already
                for (int i = 0; i < netzDings.DownloadDirectory.Length; i++)
                {
                    netzDings.DownloadDirectory[i] = downloadDirectory + netzDings.DownloadDirectory[i];

                    if (!Directory.Exists(netzDings.DownloadDirectory[i]))
                    {
                        try
                        {
                            Directory.CreateDirectory(netzDings.DownloadDirectory[i]);
                            success = true;
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }

            if (netzDings.FileDownload)
            {
                string path = Convert.ToString(netzDings.DownloadFiles[0]);
                int index = 0;

                for (int i = path.Length - 1; i > 0; i--)
                {
                    // find last // to delete //name
                    if (path[i] == 92)
                    {
                        index = i;
                        break;
                    }
                }

                // if a directory was selected but not opened delete the name of the selected folder
                path = path.Remove(0, index + 1);
                path = downloadDirectory + path;

                try
                {
                    netzDings.DownloadFiles[0].CopyTo(path);
                    success = true;
                }
                catch (Exception)
                {
                    MessageBox.Show("Cant download file.");
                }

            }
            else
            {
                for (int i = 0; i < netzDings.DownloadFiles.Count; i++)
                {
                    var path = downloadDirectory + netzDings.Filepaths[i] + netzDings.DownloadFiles[i];

                    try
                    {
                        netzDings.DownloadFiles[i].CopyTo(path);
                        success = true;
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("Cant download file.");
                    }
                }
            }

            // if files were copied show message, if only error dont show message
            if (success)
            {
                MessageBox.Show("Files downloaded to C:\\download");
            }
        }

        /// <summary>
        /// recieves data for directory structure
        /// </summary>
        private void ReceiveStructure()
        {
            NetzwerkDings netzDings = null;

            do
            {
                try
                {
                    if (ns.DataAvailable)
                    {
                        netzDings = NetworkTalk.RecievePackage(ns);

                    }
                }
                catch (Exception)
                {
                }

            }
            while (netzDings == null);

            // set root directory, to know actual directory if no folders are selected
            if (root == string.Empty)
            {
                root = netzDings.RequestedDirectoryPath;
            }

            // delete all elements from tree view
            ClearTreeview();
            string iconPath = Directory.GetCurrentDirectory() + "\\Images\\";
            
            // fill tree view with elements
            foreach (Window window in Application.Current.Windows)
            {
                if (window.GetType() == typeof(MainWindow))
                {
                    // set root element to go back to the root directory
                    TreeViewItem rootElement = new TreeViewItem();
                    rootElement = SetServerTreeViewIcon(directory, "...", iconPath + "folder.png");
                    rootElement.Name = directory;
                    rootElement.ToolTip = root;

                    (window as MainWindow).tvServer.Items.Add(rootElement);

                    // add directories as nodes
                    for (int i = 0; i < netzDings.DirectoryArray.Length; i++)
                    {
                        // creates a new Node
                        TreeViewItem node = new TreeViewItem();
                        node = SetServerTreeViewIcon(directory, Convert.ToString(netzDings.DirectoryArray[i]), iconPath + "folder.png");
                        node.Name = directory;
                        node.ToolTip = netzDings.Paths[i];

                        (window as MainWindow).tvServer.Items.Add(node);
                    }

                    // add files as childs
                    for (int i = 0; i < netzDings.FileArray.Length; i++)
                    {
                        TreeViewItem node = new TreeViewItem();
                        node = SetServerTreeViewIcon(directory, Convert.ToString(netzDings.FileArray[i]), iconPath + "file.png");
                        node.Name = "file";
                        node.ToolTip = netzDings.FileArray[i];
                        (window as MainWindow).tvServer.Items.Add(node);
                    }

                    break;
                }
            }
        }

        /// <summary>
        /// remove path so only name is left
        /// </summary>
        /// <param name="path1"></param>
        /// <param name="path2"></param>
        /// <returns></returns>
        private static string RemoveRootPath(string path1, string path2)
        {
            int index = 0;

            for (int i = path2.Length - 1; i > 0; i--)
            {
                // find last // to delete //name
                if (path2[i] == 92)
                {
                    index = i;
                    break;
                }
            }

            path2 = path2.Remove(index, path2.Length - index);
            path2 += "\\";
            path1 = path1.Replace(path2, string.Empty);

            return path1;
        }

        /// <summary>
        /// Send request to create a new directory
        /// </summary>
        /// <param name="name">Name and path of the directory</param>
        private void SendCreateDirectoryRequest(string name)
        {
            NetzwerkDings netzDings = new NetzwerkDings();
            netzDings.RequestedDirectoryPath = root;
            netzDings.NewName = name;
            netzDings.CreateDirectory = true;

            NetworkTalk.SendPackage(netzDings, tcpClient.GetStream());
            ReceiveStructure();
            MessageBox.Show("Directory created");
        }

        private void SendCreateFileRequest(string path, string name)
        {
            NetzwerkDings netzDings = new NetzwerkDings();
            netzDings.RequestedDirectoryPath = path;
            netzDings.NewName = name;
            netzDings.CreateDirectory = true;

            NetworkTalk.SendPackage(netzDings, tcpClient.GetStream());
            ReceiveStructure();
            MessageBox.Show("Directory Created");
        }

        private void SendDeleteDirectoryRequest(string path)
        {
            NetzwerkDings netzDings = new NetzwerkDings();
            netzDings.RequestedDirectoryPath = path;
            netzDings.DirectoryDelete = true;

            NetworkTalk.SendPackage(netzDings, tcpClient.GetStream());
            ReceiveStructure();

            if (netzDings.Error)
            {
                MessageBox.Show("An error occured while deleting.");
            }
            else
            {
                MessageBox.Show("Directories deleted.");
            }            
        }

        private void SendDeleteFileRequest(string path)
        {
            NetzwerkDings netzDings = new NetzwerkDings();
            netzDings.RequestedDirectoryPath = path;
            netzDings.FileDelete = true;

            NetworkTalk.SendPackage(netzDings, tcpClient.GetStream());
            ReceiveStructure();

            if (netzDings.Error)
            {
                MessageBox.Show("An error occured while deleting.");
            }
            else
            {
                MessageBox.Show("File deleted.");
            }
        }

        private void SendDownloadDirectoryRequest(string path)
        {
            NetzwerkDings netzDings = new NetzwerkDings();
            netzDings.RequestedDirectoryPath = path;
            netzDings.DirectoryDownload = true;

            NetworkTalk.SendPackage(netzDings, tcpClient.GetStream());
            ReceiveDownloadedFiles();
        }

        private void SendDownloadFileRequest(string path)
        {
            NetzwerkDings netzDings = new NetzwerkDings();
            netzDings.RequestedDirectoryPath = path;
            netzDings.FileDownload = true;

            NetworkTalk.SendPackage(netzDings, tcpClient.GetStream());
            ReceiveDownloadedFiles();
        }

        /// <summary>
        /// Send request to rename a directory
        /// </summary>
        /// <param name="path">name of old directory</param>
        /// <param name="name">name of new directory</param>
        private void SendRenameDirectoryRequest(string path, string name)
        {
            NetzwerkDings netzDings = new NetzwerkDings();
            netzDings.RequestedDirectoryPath = path;
            netzDings.NewName = name;
            netzDings.DirectoryRename = true;

            NetworkTalk.SendPackage(netzDings, tcpClient.GetStream());
            ReceiveStructure();
            MessageBox.Show("Directory renamed");
        }

        private void SendRenameFileRequest(string path, string name)
        {
            NetzwerkDings netzDings = new NetzwerkDings();
            netzDings.RequestedDirectoryPath = path;
            netzDings.NewName = name;
            netzDings.FileRename = true;

            NetworkTalk.SendPackage(netzDings, tcpClient.GetStream());
            ReceiveStructure();
            MessageBox.Show("File renamed");
        }

        /// <summary>
        /// Set icon for the node or child
        /// </summary>
        /// <param name="header"></param>
        /// <param name="text">text which is displayed in the treeview</param>
        /// <param name="imagePath"></param>
        /// <returns></returns>
        private TreeViewItem SetServerTreeViewIcon(string header, string text, string imagePath)
        {
            TreeViewItem item = new TreeViewItem();
            item.Uid = header;
            item.IsExpanded = false;

            // create stack panel
            StackPanel stack = new StackPanel();
            stack.Orientation = Orientation.Horizontal;

            // create Image
            Image image = new Image();
            image.Source = new BitmapImage
                (new Uri(imagePath));
            image.Width = 16;
            image.Height = 16;
            // Label
            Label lbl = new Label();
            lbl.Content = text;

            // Add into stack
            stack.Children.Add(image);
            stack.Children.Add(lbl);

            // assign stack to header
            item.Header = stack;
            return item;
        }
    }
}