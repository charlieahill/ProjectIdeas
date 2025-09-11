using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ProjectIdeas.Models;
using ProjectIdeas.Services;
using WinForms = System.Windows.Forms; // Only here, if you use FolderBrowserDialog
using System.Windows.Interop;

namespace ProjectIdeas
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly DataService _dataService = new();
        private ObservableCollection<ProjectIdea> _ideas;
        private ObservableCollection<ProjectIdea> _filteredIdeas = new();
        private ProjectIdea? _selectedIdea;
        private string _searchText = string.Empty;
        private bool _activeOnly = false;

        public MainWindow()
        {
            InitializeComponent();
            _ideas = _dataService.LoadData();
            _filteredIdeas = new ObservableCollection<ProjectIdea>(_ideas);
            IdeasListView.ItemsSource = _filteredIdeas;
            Title = $"Project Ideas Manager v{VersionManager.GetVersionString()}";
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (_filteredIdeas.Count > 0)
            {
                IdeasListView.SelectedIndex = 0;
            }
        }

        private void ActiveOnlyCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var cb = FindName("ActiveOnlyCheckBox") as System.Windows.Controls.CheckBox;
            _activeOnly = cb != null && cb.IsChecked == true;
            ApplyFilters();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var tb = FindName("SearchBox") as System.Windows.Controls.TextBox;
            var clearBtn = FindName("ClearSearchButton") as System.Windows.Controls.Button;
            _searchText = tb?.Text ?? string.Empty;
            if (clearBtn != null)
                clearBtn.Visibility = string.IsNullOrEmpty(_searchText) ? Visibility.Collapsed : Visibility.Visible;
            ApplyFilters();
        }

        private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
        {
            var tb = FindName("SearchBox") as System.Windows.Controls.TextBox;
            if (tb != null)
                tb.Text = string.Empty;
        }

        private void ApplyFilters()
        {
            var filtered = _ideas.Where(idea =>
                (!_activeOnly || (!IsStatusDoneOrCancelled(idea.Status))) &&
                (string.IsNullOrWhiteSpace(_searchText) || MatchesSearch(idea, _searchText))
            ).ToList();
            _filteredIdeas.Clear();
            foreach (var idea in filtered)
                _filteredIdeas.Add(idea);
            // Keep selection if possible
            if (_filteredIdeas.Count > 0 && _selectedIdea != null && _filteredIdeas.Contains(_selectedIdea))
                IdeasListView.SelectedItem = _selectedIdea;
            else if (_filteredIdeas.Count > 0)
                IdeasListView.SelectedIndex = 0;
        }

        private bool IsStatusDoneOrCancelled(string? status)
        {
            if (string.IsNullOrWhiteSpace(status)) return false;
            var s = status.Trim().ToLowerInvariant();
            return s == "done" || s == "cancelled";
        }

        private bool MatchesSearch(ProjectIdea idea, string search)
        {
            search = search.ToLowerInvariant();
            if ((idea.Title ?? string.Empty).ToLowerInvariant().Contains(search)) return true;
            if ((idea.Description ?? string.Empty).ToLowerInvariant().Contains(search)) return true;
            if (idea.Bugs != null && idea.Bugs.Any(b => (b.Description ?? string.Empty).ToLowerInvariant().Contains(search))) return true;
            if (idea.Features != null && idea.Features.Any(f => (f.Description ?? string.Empty).ToLowerInvariant().Contains(search))) return true;
            return false;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _dataService.SaveData(_ideas);
        }

        private void AddNewIdea_Click(object sender, RoutedEventArgs e)
        {
            var newIdea = new ProjectIdea
            {
                Title = "New Project Idea",
                Description = "Enter description here...",
                Status = "Open"
            };
            _ideas.Add(newIdea);
            ApplyFilters();
            IdeasListView.SelectedItem = newIdea;
        }

        private void IdeasListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedIdea = IdeasListView.SelectedItem as ProjectIdea;
            if (_selectedIdea != null)
            {
                RightPanel.DataContext = _selectedIdea;
                FilesListView.ItemsSource = _selectedIdea.FileLinks;
                BugsListView.ItemsSource = _selectedIdea.Bugs;
                FeaturesListView.ItemsSource = _selectedIdea.Features;
                VoteHistoryListView.ItemsSource = _selectedIdea.VoteHistory.OrderByDescending(v => v.Date).ToList();
            }
        }

        private void UpVote_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is ProjectIdea idea)
            {
                idea.Votes++;
                idea.VoteHistory.Add(new VoteRecord { Date = DateTime.Now, Direction = "Up" });
                IdeasListView.Items.Refresh();
                if (_selectedIdea == idea)
                    VoteHistoryListView.ItemsSource = idea.VoteHistory.OrderByDescending(v => v.Date).ToList();
            }
        }

        private void DownVote_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is ProjectIdea idea)
            {
                idea.Votes--;
                idea.VoteHistory.Add(new VoteRecord { Date = DateTime.Now, Direction = "Down" });
                IdeasListView.Items.Refresh();
                if (_selectedIdea == idea)
                    VoteHistoryListView.ItemsSource = idea.VoteHistory.OrderByDescending(v => v.Date).ToList();
            }
        }

        private void AddFileLink_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedIdea == null) return;

            var dialog = new Microsoft.Win32.OpenFileDialog();
            if (dialog.ShowDialog() == true)
            {
                _selectedIdea.FileLinks.Add(dialog.FileName);
                FilesListView.Items.Refresh();
            }
        }

        private void AddBug_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedIdea == null) return;

            var newBug = new WorkItem { Description = "New bug" };
            _selectedIdea.Bugs.Add(newBug);
            BugsListView.Items.Refresh();
            BugsListView.SelectedItem = newBug;
            BugsListView.UpdateLayout();
            var container = BugsListView.ItemContainerGenerator.ContainerFromItem(newBug) as System.Windows.Controls.ListViewItem;
            if (container != null)
            {
                container.Focus();
                var textBox = FindVisualChild<System.Windows.Controls.TextBox>(container);
                textBox?.Focus();
                textBox?.SelectAll();
            }
        }

        private void AddFeature_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedIdea == null) return;

            var newFeature = new WorkItem { Description = "New feature" };
            _selectedIdea.Features.Add(newFeature);
            FeaturesListView.Items.Refresh();
            FeaturesListView.SelectedItem = newFeature;
            FeaturesListView.UpdateLayout();
            var container = FeaturesListView.ItemContainerGenerator.ContainerFromItem(newFeature) as System.Windows.Controls.ListViewItem;
            if (container != null)
            {
                container.Focus();
                var textBox = FindVisualChild<System.Windows.Controls.TextBox>(container);
                textBox?.Focus();
                textBox?.SelectAll();
            }
        }

        private void DescriptionTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            DescriptionTextBox.SelectAll();
        }

        private void AddFolderLink_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedIdea == null) return;

            var dialog = new WinForms.FolderBrowserDialog();
            var result = dialog.ShowDialog();
            if (result == WinForms.DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
            {
                _selectedIdea.FileLinks.Add(dialog.SelectedPath);
                FilesListView.Items.Refresh();
            }
        }

        private void FilesListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (FilesListView.SelectedItem is string path && !string.IsNullOrWhiteSpace(path))
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = path,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Could not open: {ex.Message}");
                }
            }
        }

        private void WorkItemDone_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.CheckBox cb && cb.DataContext is WorkItem item && _selectedIdea != null)
            {
                // Move to bottom by removing and adding
                if (_selectedIdea.Bugs.Contains(item))
                {
                    _selectedIdea.Bugs.Remove(item);
                    _selectedIdea.Bugs.Add(item);
                    BugsListView.Items.Refresh();
                }
                else if (_selectedIdea.Features.Contains(item))
                {
                    _selectedIdea.Features.Remove(item);
                    _selectedIdea.Features.Add(item);
                    FeaturesListView.Items.Refresh();
                }
            }
        }

        private void RemoveFileOrFolder_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedIdea == null) return;
            if (sender is System.Windows.Controls.Button btn && btn.DataContext is string path)
            {
                _selectedIdea.FileLinks.Remove(path);
                FilesListView.Items.Refresh();
            }
        }

        private void MoveProjectIdeaUp_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.DataContext is ProjectIdea idea)
            {
                var list = _filteredIdeas;
                int idx = list.IndexOf(idea);
                if (idx > 0)
                {
                    list.Move(idx, idx - 1);
                    IdeasListView.Items.Refresh();
                    IdeasListView.SelectedItem = idea;
                }
            }
        }

        private void MoveProjectIdeaDown_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.DataContext is ProjectIdea idea)
            {
                var list = _filteredIdeas;
                int idx = list.IndexOf(idea);
                if (idx < list.Count - 1 && idx >= 0)
                {
                    list.Move(idx, idx + 1);
                    IdeasListView.Items.Refresh();
                    IdeasListView.SelectedItem = idea;
                }
            }
        }

        private void SendToTop_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as System.Windows.Controls.Button)?.DataContext is ProjectIdea idea)
            {
                var list = _filteredIdeas;
                int oldIndex = list.IndexOf(idea);
                if (oldIndex > 0)
                {
                    list.Move(oldIndex, 0);
                    IdeasListView.Items.Refresh();
                    IdeasListView.SelectedItem = idea;
                }
            }
        }

        private void FilesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FilesListView.SelectedItem is string path && File.Exists(path) && IsImageFile(path))
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(path);
                    bitmap.EndInit();
                    FileImagePreview.Source = bitmap;
                    FileImagePreview.Visibility = Visibility.Visible;
                }
                catch
                {
                    FileImagePreview.Source = null;
                    FileImagePreview.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                FileImagePreview.Source = null;
                FileImagePreview.Visibility = Visibility.Collapsed;
            }
        }

        private void StatusComboBoxOverlay_Click(object sender, RoutedEventArgs e)
        {
            StatusComboBox.IsDropDownOpen = true;
        }

        private static bool IsImageFile(string path)
        {
            string[] imageExtensions = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tiff", ".ico" };
            string ext = Path.GetExtension(path)?.ToLowerInvariant() ?? string.Empty;
            return imageExtensions.Contains(ext);
        }

        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T tChild)
                    return tChild;
                var result = FindVisualChild<T>(child);
                if (result != null)
                    return result;
            }
            return null;
        }

        private void MoveWorkItemUp_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedIdea == null) return;
            if (sender is System.Windows.Controls.Button btn && btn.DataContext is WorkItem item)
            {
                if (_selectedIdea.Bugs.Contains(item))
                {
                    var list = _selectedIdea.Bugs;
                    int idx = list.IndexOf(item);
                    if (idx > 0)
                    {
                        list.Move(idx, idx - 1);
                        BugsListView.Items.Refresh();
                    }
                }
                else if (_selectedIdea.Features.Contains(item))
                {
                    var list = _selectedIdea.Features;
                    int idx = list.IndexOf(item);
                    if (idx > 0)
                    {
                        list.Move(idx, idx - 1);
                        FeaturesListView.Items.Refresh();
                    }
                }
            }
        }

        private void MoveWorkItemDown_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedIdea == null) return;
            if (sender is System.Windows.Controls.Button btn && btn.DataContext is WorkItem item)
            {
                if (_selectedIdea.Bugs.Contains(item))
                {
                    var list = _selectedIdea.Bugs;
                    int idx = list.IndexOf(item);
                    if (idx < list.Count - 1 && idx >= 0)
                    {
                        list.Move(idx, idx + 1);
                        BugsListView.Items.Refresh();
                    }
                }
                else if (_selectedIdea.Features.Contains(item))
                {
                    var list = _selectedIdea.Features;
                    int idx = list.IndexOf(item);
                    if (idx < list.Count - 1 && idx >= 0)
                    {
                        list.Move(idx, idx + 1);
                        FeaturesListView.Items.Refresh();
                    }
                }
            }
        }

        // Custom Title Bar Button Handlers
        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeRestore_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
                WindowState = WindowState.Normal;
            else
                WindowState = WindowState.Maximized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Only handle left button actions
            if (e.ChangedButton != MouseButton.Left)
                return;

            // If the click started on an interactive control (like a Button), ignore it
            if (e.OriginalSource is DependencyObject src && IsInInteractiveControl(src))
                return;

            if (e.ClickCount == 2)
            {
                // Double-click toggles maximize/restore
                if (WindowState == WindowState.Maximized)
                    WindowState = WindowState.Normal;
                else
                    WindowState = WindowState.Maximized;
                e.Handled = true;
            }
            else if (e.ClickCount == 1)
            {
                // Single-click and drag to move
                try
                {
                    DragMove();
                }
                catch
                {
                    // Ignored - DragMove can throw if called in invalid state
                }
            }
        }

        private static bool IsInInteractiveControl(DependencyObject source)
        {
            // Check if the source is a visual element that can receive input
            if (source is System.Windows.Controls.Control control && control.IsEnabled)
                return true;

            // Check if the source is a suitable feedback element (like a Button)
            if (source is System.Windows.Controls.Button)
                return true;

            // Continue traversing the visual tree
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(source); i++)
            {
                var child = VisualTreeHelper.GetChild(source, i);
                if (IsInInteractiveControl(child))
                    return true;
            }

            return false;
        }
    }

    public class VoteDirectionToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string dir)
            {
                if (dir.Equals("Up", StringComparison.OrdinalIgnoreCase))
                    return "▲";
                if (dir.Equals("Down", StringComparison.OrdinalIgnoreCase))
                    return "▼";
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}