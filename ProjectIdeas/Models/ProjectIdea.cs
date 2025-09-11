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
    }

    public class VoteRecord
    {
        public DateTime Date { get; set; }
        public string Direction { get; set; } = string.Empty; // "Up" or "Down"
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