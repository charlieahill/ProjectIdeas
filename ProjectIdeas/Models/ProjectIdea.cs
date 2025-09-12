using System;
using System.Collections.ObjectModel;
using System.ComponentModel; // Add this

namespace ProjectIdeas.Models
{
    public class ProjectIdea
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Votes { get; set; }
        public string ColorCode { get; set; } = "#FF4081"; // Default accent color
        public ObservableCollection<string> FileLinks { get; set; } = new();
        public ObservableCollection<WorkItem> Bugs { get; set; } = new();
        public ObservableCollection<WorkItem> Features { get; set; } = new();
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime LastModified { get; set; } = DateTime.Now;
        public string Status { get; set; } = "open"; // Add status property
        public ObservableCollection<VoteRecord> VoteHistory { get; set; } = new();
        public ObservableCollection<VersionRecord> VersionHistory { get; set; } = new();
    }

    public class VoteRecord
    {
        public DateTime Date { get; set; }
        public string Direction { get; set; } = string.Empty; // "Up" or "Down"
    }

    public class VersionRecord : INotifyPropertyChanged
    {
        private string _versionNumber = string.Empty;
        public string VersionNumber
        {
            get => _versionNumber;
            set { _versionNumber = value; OnPropertyChanged(nameof(VersionNumber)); }
        }

        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        private string _folderLink = string.Empty;
        public string FolderLink
        {
            get => _folderLink;
            set { _folderLink = value; OnPropertyChanged(nameof(FolderLink)); }
        }

        private DateTime _releaseDate = DateTime.Now;
        public DateTime ReleaseDate
        {
            get => _releaseDate;
            set { _releaseDate = value; OnPropertyChanged(nameof(ReleaseDate)); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class WorkItem : INotifyPropertyChanged
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        private string _description = string.Empty;
        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(nameof(Description)); }
        }

        private bool _isCompleted;
        public bool IsCompleted
        {
            get => _isCompleted;
            set { _isCompleted = value; OnPropertyChanged(nameof(IsCompleted)); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}