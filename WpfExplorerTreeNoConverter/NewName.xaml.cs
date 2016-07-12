using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace FTPClient
{
    /// <summary>
    /// Interaction logic for NewName.xaml
    /// </summary>
    public partial class NewName : Window
    {
        MainWindow mainWindow = new MainWindow();
        // indicates if user wants to rename or create a directory, 0 == create, 1 == rename
        private int mode = 0;
        private string currentPath;
        public NewName(int modeN, string path)
        {
            mode = modeN;
            currentPath = path;
            InitializeComponent();
        }

        private void btnNewNameOK_Click(object sender, RoutedEventArgs e)
        {
            switch (mode)
            {
                case 0:
                    try
                    {
                        MainWindow.client.CreateDirectoryAtServer(tbNewName.Text);
                    }
                    catch (Exception)
                    {
                        MainWindow.client.Disconnect();
                    }

                    break;

                case 1:
                    try
                    {
                        MainWindow.client.RenameDirectoryAtServer(tbNewName.Text, currentPath);
                    }
                    catch (Exception)
                    {
                        MainWindow.client.Disconnect();
                    }

                    break;

                default:
                    break;
            }

            this.Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void tbNewName_TextChanged(object sender, TextChangedEventArgs e)
        {
            btnNewNameOK.IsEnabled = true;
        }
    }
}
