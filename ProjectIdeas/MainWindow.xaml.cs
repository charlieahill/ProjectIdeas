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
using System.Windows.Controls.Primitives;

namespace ProjectIdeas
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly DataService _data_service = new();
        private ObservableCollection<ProjectIdea> _ideas;
        private ObservableCollection<ProjectIdea> _filteredIdeas = new();
        private ProjectIdea? _selectedIdea;
        private string _searchText = string.Empty;
        private bool _activeOnly = false;
        private Rect _restoreBounds;
        private bool _isCustomMaximized = false;

        public MainWindow()
        {
            InitializeComponent();
            MinHeight = 800;
            Height = 900;
            _ideas = _data_service.LoadData();
            _filteredIdeas = new ObservableCollection<ProjectIdea>(_ideas);
            IdeasListView.ItemsSource = _filteredIdeas;
            Title = $"Project Ideas Manager v{VersionManager.GetVersionString()}";
            Loaded += MainWindow_Loaded;
            StateChanged += MainWindow_StateChanged;
            System.Windows.Input.InputManager.Current.PreProcessInput += InputManager_PreProcessInput;
        }

        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            // If the system sets WindowState to Maximized (e.g. via keyboard), adjust to work area
            if (WindowState == WindowState.Maximized && !_isCustomMaximized)
            {
                var workArea = SystemParameters.WorkArea;
                Left = workArea.Left;
                Top = workArea.Top;
                Width = workArea.Width;
                Height = workArea.Height;
                _isCustomMaximized = true;
                UpdateMaximizeIcon(true);
            }
            else if (WindowState == WindowState.Normal && !_isCustomMaximized)
            {
                UpdateMaximizeIcon(false);
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (_filteredIdeas.Count > 0)
            {
                IdeasListView.SelectedIndex = 0;
            }
            var newDate = FindName("NewVersionDate") as DatePicker;
            if (newDate != null)
                newDate.SelectedDate = DateTime.Now;
            // Theme selection now handled in settings dialog
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
            _data_service.SaveData(_ideas);
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
                var versionsLv = FindName("VersionHistoryListView") as System.Windows.Controls.ListView;
                if (versionsLv != null)
                    versionsLv.ItemsSource = _selectedIdea.VersionHistory;
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
                {
                    VoteHistoryListView.ItemsSource = idea.VoteHistory.OrderByDescending(v => v.Date).ToList();
                }
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
                {
                    VoteHistoryListView.ItemsSource = idea.VoteHistory.OrderByDescending(v => v.Date).ToList();
                }
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

        private bool _isShiftPressed = false;

        // Attached property for shift state
        public static readonly System.Windows.DependencyProperty IsShiftPressedProperty = System.Windows.DependencyProperty.RegisterAttached(
            "IsShiftPressed", typeof(bool), typeof(MainWindow), new System.Windows.PropertyMetadata(false));

        public static void SetIsShiftPressed(System.Windows.DependencyObject element, bool value)
        {
            element.SetValue(IsShiftPressedProperty, value);
        }
        public static bool GetIsShiftPressed(System.Windows.DependencyObject element)
        {
            return (bool)element.GetValue(IsShiftPressedProperty);
        }

        private void UpdateMoveButtonVisuals()
        {
            foreach (var item in IdeasListView.Items)
            {
                var container = IdeasListView.ItemContainerGenerator.ContainerFromItem(item) as System.Windows.Controls.ListViewItem;
                if (container != null)
                {
                    var upBtn = FindVisualChildByTag<System.Windows.Controls.Button>(container, "MoveUp");
                    var downBtn = FindVisualChildByTag<System.Windows.Controls.Button>(container, "MoveDown");
                    if (upBtn != null)
                        SetIsShiftPressed(upBtn, _isShiftPressed);
                    if (downBtn != null)
                        SetIsShiftPressed(downBtn, _isShiftPressed);
                }
            }
        }

        private T? FindVisualChildByTag<T>(DependencyObject parent, string tag) where T : FrameworkElement
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T tChild && tChild.Tag is string t && t == tag)
                    return tChild;
                var result = FindVisualChildByTag<T>(child, tag);
                if (result != null)
                    return result;
            }
            return null;
        }

        private void MoveProjectIdeaUp_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.DataContext is ProjectIdea idea)
            {
                var list = _filteredIdeas;
                int idx = list.IndexOf(idea);
                if (_isShiftPressed)
                {
                    // Move to top
                    if (idx > 0)
                    {
                        list.Move(idx, 0);
                        IdeasListView.Items.Refresh();
                        IdeasListView.SelectedItem = idea;
                    }
                }
                else
                {
                    // Normal move up one
                    if (idx > 0)
                    {
                        list.Move(idx, idx - 1);
                        IdeasListView.Items.Refresh();
                        IdeasListView.SelectedItem = idea;
                    }
                }
            }
        }

        private void MoveProjectIdeaDown_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.DataContext is ProjectIdea idea)
            {
                var list = _filteredIdeas;
                int idx = list.IndexOf(idea);
                if (_isShiftPressed)
                {
                    // Move to bottom
                    if (idx < list.Count - 1 && idx >= 0)
                    {
                        list.Move(idx, list.Count - 1);
                        IdeasListView.Items.Refresh();
                        IdeasListView.SelectedItem = idea;
                    }
                }
                else
                {
                    // Normal move down one
                    if (idx < list.Count - 1 && idx >= 0)
                    {
                        list.Move(idx, idx + 1);
                        IdeasListView.Items.Refresh();
                        IdeasListView.SelectedItem = idea;
                    }
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
            if (_isCustomMaximized)
            {
                // Restore
                Left = _restoreBounds.Left;
                Top = _restoreBounds.Top;
                Width = _restoreBounds.Width;
                Height = _restoreBounds.Height;
                _isCustomMaximized = false;
                WindowState = WindowState.Normal;
                UpdateMaximizeIcon(false);
            }
            else if (WindowState == WindowState.Maximized)
            {
                // System maximized - restore to normal
                WindowState = WindowState.Normal;
                UpdateMaximizeIcon(false);
            }
            else
            {
                // Custom maximize to work area (respect taskbar)
                _restoreBounds = new Rect(Left, Top, Width, Height);
                var workArea = SystemParameters.WorkArea;
                Left = workArea.Left;
                Top = workArea.Top;
                Width = workArea.Width;
                Height = workArea.Height;
                _isCustomMaximized = true;
                WindowState = WindowState.Normal; // keep state Normal but sized to work area
                UpdateMaximizeIcon(true);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // Improved: Use PreviewMouseLeftButtonDown for more reliable title bar drag/maximize/restore
        private void TitleBar_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Only handle if not clicking on a Button or its child
            if (e.OriginalSource is DependencyObject src && IsInButtonOrChild(src))
                return;

            // Double-click toggles maximize/restore
            if (e.ClickCount == 2)
            {
                MaximizeRestore_Click(this, new RoutedEventArgs());
                e.Handled = true;
                return;
            }

            // Single-click and drag to move (with maximize/restore logic)
            if (e.ClickCount == 1)
            {
                if (WindowState == WindowState.Maximized || _isCustomMaximized)
                {
                    var mouseScreen = PointToScreen(e.GetPosition(this));
                    double restoreWidth, restoreHeight;
                    if (_isCustomMaximized)
                    {
                        restoreWidth = _restoreBounds.Width;
                        restoreHeight = _restoreBounds.Height;
                        _isCustomMaximized = false;
                        WindowState = WindowState.Normal;
                        UpdateMaximizeIcon(false);
                    }
                    else
                    {
                        restoreWidth = RestoreBounds.Width;
                        restoreHeight = RestoreBounds.Height;
                        WindowState = WindowState.Normal;
                        UpdateMaximizeIcon(false);
                    }
                    var mousePos = e.GetPosition(this);
                    double xRatio = mousePos.X / ActualWidth;
                    double yRatio = mousePos.Y / ActualHeight;
                    Width = restoreWidth;
                    Height = restoreHeight;
                    Left = mouseScreen.X - Width * xRatio;
                    Top = mouseScreen.Y - Height * yRatio;
                    UpdateLayout();
                    try { DragMove(); } catch { }
                    e.Handled = true;
                }
                else
                {
                    try { DragMove(); } catch { }
                    e.Handled = true;
                }
            }
        }

        // Helper: check if the source is a Button or inside a Button
        private bool IsInButtonOrChild(DependencyObject src)
        {
            while (src != null)
            {
                if (src is System.Windows.Controls.Button)
                    return true;
                src = System.Windows.Media.VisualTreeHelper.GetParent(src);
            }
            return false;
        }

        private void AddVersion_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedIdea == null) return;
            var txtVer = FindName("NewVersionNumber") as System.Windows.Controls.TextBox;
            var txtName = FindName("NewVersionName") as System.Windows.Controls.TextBox;
            var txtFolder = FindName("NewVersionFolder") as System.Windows.Controls.TextBox;
            var dp = FindName("NewVersionDate") as System.Windows.Controls.DatePicker;
            var versionsLv = FindName("VersionHistoryListView") as System.Windows.Controls.ListView;

            var v = new VersionRecord
            {
                VersionNumber = txtVer?.Text ?? string.Empty,
                Name = txtName?.Text ?? string.Empty,
                FolderLink = txtFolder?.Text ?? string.Empty,
                ReleaseDate = dp?.SelectedDate ?? DateTime.Now
            };
            _selectedIdea.VersionHistory.Add(v);
            versionsLv?.Items.Refresh();
        }

        private void RemoveVersion_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedIdea == null) return;
            if (sender is System.Windows.Controls.Button wpfBtn && wpfBtn.CommandParameter is VersionRecord vr && _selectedIdea.VersionHistory.Contains(vr))
            {
                _selectedIdea.VersionHistory.Remove(vr);
                var versionsLv = FindName("VersionHistoryListView") as System.Windows.Controls.ListView;
                versionsLv?.Items.Refresh();
                return;
            }
            if (sender is System.Windows.Controls.Button wpfBtn2 && wpfBtn2.DataContext is VersionRecord vr2 && _selectedIdea.VersionHistory.Contains(vr2))
            {
                _selectedIdea.VersionHistory.Remove(vr2);
                var versionsLv = FindName("VersionHistoryListView") as System.Windows.Controls.ListView;
                versionsLv?.Items.Refresh();
            }
        }

        private void BrowseVersionFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new WinForms.FolderBrowserDialog();
            var result = dialog.ShowDialog();
            if (result == WinForms.DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
            {
                var txtFolder = FindName("NewVersionFolder") as System.Windows.Controls.TextBox;
                if (txtFolder != null)
                    txtFolder.Text = dialog.SelectedPath;
            }
        }

        private void ResizeLeftThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double newWidth = Width - e.HorizontalChange;
            double minWidth = MinWidth > 0 ? MinWidth : 400;
            if (newWidth > minWidth)
            {
                Left += e.HorizontalChange;
                Width = newWidth;
            }
        }

        private void ResizeRightThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double newWidth = Width + e.HorizontalChange;
            double minWidth = MinWidth > 0 ? MinWidth : 400;
            if (newWidth > minWidth)
            {
                Width = newWidth;
            }
        }

        private void ResizeBottomThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double newHeight = Height + e.VerticalChange;
            double minHeight = MinHeight > 0 ? MinHeight : 300;
            if (newHeight > minHeight)
            {
                Height = newHeight;
            }
        }

        private void ResizeTopThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double newHeight = Height - e.VerticalChange;
            double minHeight = MinHeight > 0 ? MinHeight : 300;
            if (newHeight > minHeight)
            {
                Top += e.VerticalChange;
                Height = newHeight;
            }
        }

        private void ResizeTopLeftThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            ResizeTopThumb_DragDelta(sender, e);
            ResizeLeftThumb_DragDelta(sender, e);
        }

        private void ResizeTopRightThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            ResizeTopThumb_DragDelta(sender, e);
            ResizeRightThumb_DragDelta(sender, e);
        }

        private void ResizeBottomLeftThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            ResizeLeftThumb_DragDelta(sender, e);
            ResizeBottomThumb_DragDelta(sender, e);
        }

        private void ResizeBottomRightThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            ResizeRightThumb_DragDelta(sender, e);
            ResizeBottomThumb_DragDelta(sender, e);
        }

        private void UpdateMaximizeIcon(bool isMaximized)
        {
            var btn = FindName("MaximizeButton") as System.Windows.Controls.Button;
            if (btn == null) return;
            // Use common symbols: restore (🗗) for maximized, square (☐) for normal
            btn.Content = isMaximized ? "🗗" : "☐";
            btn.ToolTip = isMaximized ? "Restore" : "Maximize";
        }

        // Open folder link in Windows Explorer when double-clicking a VersionHistory item
        private void VersionHistoryListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is System.Windows.Controls.ListView lv && lv.SelectedItem is ProjectIdeas.Models.VersionRecord vr)
            {
                var folder = vr.FolderLink;
                if (!string.IsNullOrWhiteSpace(folder) && Directory.Exists(folder))
                {
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = folder,
                            UseShellExecute = true
                        });
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show($"Could not open folder: {ex.Message}");
                    }
                }
            }
        }

        // Sort ideas by votes descending
        private void SortByVotingButton_Click(object sender, RoutedEventArgs e)
        {
            var sorted = _filteredIdeas.OrderByDescending(i => i.Votes).ToList();
            _filteredIdeas.Clear();
            foreach (var idea in sorted)
                _filteredIdeas.Add(idea);
            if (_filteredIdeas.Count > 0)
                IdeasListView.SelectedIndex = 0;
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            int currentTheme = ProjectIdeas.Properties.Settings.Default.ThemeSelection;
            var dlg = new SettingsDialog(currentTheme) { Owner = this };
            if (dlg.ShowDialog() == true)
            {
                int idx = dlg.SelectedThemeIndex;
                var app = System.Windows.Application.Current as App;
                if (app != null)
                {
                    ThemeOption option = ThemeOption.System;
                    try { option = (ThemeOption)idx; } catch { option = ThemeOption.System; }
                    app.ApplyTheme(option);
                    app.SaveThemeSetting(option);
                }
            }
        }

        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // No longer used; theme selection is now in settings dialog
        }

        private void InputManager_PreProcessInput(object sender, System.Windows.Input.PreProcessInputEventArgs e)
        {
            var inputEventArgs = e.StagingItem.Input;
            if (inputEventArgs is System.Windows.Input.KeyEventArgs keyArgs)
            {
                if ((keyArgs.Key == System.Windows.Input.Key.LeftShift || keyArgs.Key == System.Windows.Input.Key.RightShift))
                {
                    bool shiftDown = (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Shift) == System.Windows.Input.ModifierKeys.Shift;
                    if (shiftDown != _isShiftPressed)
                    {
                        _isShiftPressed = shiftDown;
                        UpdateMoveButtonVisuals();
                    }
                }
            }
        }

        private void DataFolderButton_Click(object sender, RoutedEventArgs e)
        {
            string dataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ProjectIdeas");
            if (!Directory.Exists(dataFolder))
                Directory.CreateDirectory(dataFolder);
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = dataFolder,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Could not open data folder: {ex.Message}");
            }
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