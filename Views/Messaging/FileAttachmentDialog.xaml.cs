using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using Microsoft.UI.Xaml;
using Microsoft.Win32;

namespace LIMS.Views.Messaging
{
    public partial class FileAttachmentDialog : Window, INotifyPropertyChanged
    {
        public ObservableCollection<FileAttachmentItem> Files { get; }

        public bool HasFiles => Files.Count > 0;

        public event PropertyChangedEventHandler PropertyChanged;

        public FileAttachmentDialog()
        {
            InitializeComponent();
            Files = new ObservableCollection<FileAttachmentItem>();
            FileListBox.ItemsSource = Files;
            DataContext = this;

            Files.CollectionChanged += (s, e) => OnPropertyChanged(nameof(HasFiles));

            // Enable drag and drop
            AllowDrop = true;
            Drop += FileAttachmentDialog_Drop;
            DragEnter += FileAttachmentDialog_DragEnter;
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "All files (*.*)|*.*|Images (*.jpg;*.jpeg;*.png;*.gif)|*.jpg;*.jpeg;*.png;*.gif|Documents (*.pdf;*.doc;*.docx;*.txt)|*.pdf;*.doc;*.docx;*.txt"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (var fileName in openFileDialog.FileNames)
                {
                    AddFile(fileName);
                }
            }
        }

        private void FileAttachmentDialog_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void FileAttachmentDialog_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (var file in files)
                {
                    if (File.Exists(file))
                    {
                        AddFile(file);
                    }
                }
            }
        }

        private void AddFile(string filePath)
        {
            var fileInfo = new FileInfo(filePath);

            // Check file size (limit to 10MB for example)
            if (fileInfo.Length > 10 * 1024 * 1024)
            {
                MessageBox.Show($"File '{fileInfo.Name}' is too large. Maximum size is 10MB.",
                    "File Too Large", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Check if file already added
            if (Files.Any(f => f.Path == filePath))
            {
                return;
            }

            var fileItem = new FileAttachmentItem
            {
                Name = fileInfo.Name,
                Path = filePath,
                Size = fileInfo.Length,
                SizeFormatted = FormatFileSize(fileInfo.Length)
            };

            Files.Add(fileItem);
        }

        private void RemoveFile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is FileAttachmentItem file)
            {
                Files.Remove(file);
            }
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class FileAttachmentItem : INotifyPropertyChanged
    {
        private bool _isUploading;
        private double _uploadProgress;

        public string Name { get; set; }
        public string Path { get; set; }
        public long Size { get; set; }
        public string SizeFormatted { get; set; }

        public bool IsUploading
        {
            get => _isUploading;
            set
            {
                _isUploading = value;
                OnPropertyChanged();
            }
        }

        public double UploadProgress
        {
            get => _uploadProgress;
            set
            {
                _uploadProgress = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}