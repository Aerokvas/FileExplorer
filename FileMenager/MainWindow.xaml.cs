using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;

namespace FileMenager
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ObservableCollection<FileItem> files;
        private List<FileItem> copiedFiles;
        private Stack<string> directoryHistory;
        private string currentDirectory;

        public MainWindow()
        {
            InitializeComponent();
            files = new ObservableCollection<FileItem>();
            copiedFiles = new List<FileItem>();
            directoryHistory = new Stack<string>();
            fileListView.ItemsSource = files;
            currentDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            LoadFiles(currentDirectory);
        }

        private void FileListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var selectedFile = fileListView.SelectedItem as FileItem;

            if (selectedFile != null)
            {
                if (selectedFile.Type == "Folder")
                    LoadFiles(selectedFile.FullPath);
                else
                {
                    try
                    {
                        System.Diagnostics.Process.Start(selectedFile.FullPath);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error opening file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }


        private void FileListView_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Pressed)
            {
                var listViewItem = GetListViewItemUnderMouse(fileListView, e.GetPosition(fileListView));

                if (listViewItem != null)
                    listViewItem.IsSelected = true;
            }
        }

        private ListViewItem GetListViewItemUnderMouse(ListView listView, Point mousePoint)
        {
            HitTestResult hitTestResult = VisualTreeHelper.HitTest(listView, mousePoint);
            DependencyObject obj = hitTestResult.VisualHit;

            while (obj != null && obj != listView)
            {
                if (obj is ListViewItem)
                    return (ListViewItem)obj;
                obj = VisualTreeHelper.GetParent(obj);
            }

            return null;
        }

        private void LoadFiles(string path)
        {
            try
            {
                files.Clear();
                currentPathTextBlock.Text = $"{path}";

                foreach (var directory in Directory.GetDirectories(path))
                {
                    files.Add(new FileItem { Name = Path.GetFileName(directory), Type = "Folder", Directory = path });
                }

                foreach (var file in Directory.GetFiles(path))
                {
                    files.Add(new FileItem { Name = Path.GetFileName(file), Type = "File", Directory = path });
                }
                currentDirectory = path;
                directoryHistory.Push(path);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GoBack_Click(object sender, RoutedEventArgs e)
        {
            if (directoryHistory.Count > 1)
            {
                directoryHistory.Pop();
                string previousDirectory = directoryHistory.Peek();
                LoadFiles(previousDirectory);
            }
        }

        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.SelectedPath = currentDirectory;
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                currentDirectory = dialog.SelectedPath;
                LoadFiles(currentDirectory);
            }
        }

        private void CreateFile_Click(object sender, RoutedEventArgs e)
        {
            var createFileDialog = new CreateFileDialog();

            if (createFileDialog.ShowDialog() == true)
            {
                string newFileName = createFileDialog.FileName;
                string newFilePath = Path.Combine(currentDirectory, newFileName);

                try
                {
                    File.Create(newFilePath).Close();
                    LoadFiles(currentDirectory);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка создания файла: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


        private void CreateFolder_Click(object sender, RoutedEventArgs e)
        {
            var createFolderDialog = new CreateFolderDialog();

            if (createFolderDialog.ShowDialog() == true)
            {
                string newFolderName = createFolderDialog.FolderName;
                string newFolderPath = Path.Combine(currentDirectory, newFolderName);

                try
                {
                    Directory.CreateDirectory(newFolderPath);
                    LoadFiles(currentDirectory);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка создания папки: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


        private void Rename_Click(object sender, RoutedEventArgs e)
        {
            var selectedFile = fileListView.SelectedItem as FileItem;

            if (selectedFile != null)
            {
                var renameDialog = new RenameDialog(selectedFile.Name);

                if (renameDialog.ShowDialog() == true)
                {
                    string newPath = Path.Combine(selectedFile.Directory, renameDialog.NewName);
                    try
                    {
                        File.Move(selectedFile.FullPath, newPath);
                        LoadFiles(selectedFile.Directory);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка переименования: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            var selectedFile = fileListView.SelectedItem as FileItem;

            if (selectedFile != null)
            {
                if (MessageBox.Show($"Вы уверены, что хотите удалить {selectedFile.Name}?", "Удаление", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    string fullPath = selectedFile.FullPath;
                    try
                    {
                        if (selectedFile.Type == "Folder")
                        {
                            Directory.Delete(fullPath, true);
                        }
                        else
                        {
                            File.Delete(fullPath);
                        }
                        LoadFiles(selectedFile.Directory);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка удаления: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            var selectedFile = fileListView.SelectedItem as FileItem;
            if (selectedFile != null)
            {
                copiedFiles.Clear();
                copiedFiles.Add(selectedFile);
            }
        }

        private void Paste_Click(object sender, RoutedEventArgs e)
        {
            if (copiedFiles.Count > 0)
            {
                string destinationPath = Path.Combine(currentDirectory, "Копия_" + copiedFiles[0].Name);

                try
                {
                    if (copiedFiles[0].Type == "Folder")
                    {
                        CopyDirectory(copiedFiles[0].FullPath, destinationPath);
                    }
                    else
                    {
                        File.Copy(copiedFiles[0].FullPath, destinationPath);
                    }
                    LoadFiles(currentDirectory);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка копирования: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CopyDirectory(string sourcePath, string destinationPath)
        {
            if (!Directory.Exists(destinationPath))
            {
                Directory.CreateDirectory(destinationPath);
            }

            foreach (string file in Directory.GetFiles(sourcePath))
            {
                string destFile = Path.Combine(destinationPath, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            foreach (string folder in Directory.GetDirectories(sourcePath))
            {
                string destFolder = Path.Combine(destinationPath, Path.GetFileName(folder));
                CopyDirectory(folder, destFolder);
            }
        }
    }
}
