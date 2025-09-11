using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using ProjectIdeas.Models;

namespace ProjectIdeas.Services
{
    public class DataService
    {
        private readonly string _dataFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ProjectIdeas",
            "ideas.json"
        );

        public ObservableCollection<ProjectIdea> LoadData()
        {
            try
            {
                if (File.Exists(_dataFile))
                {
                    var json = File.ReadAllText(_dataFile);
                    return JsonSerializer.Deserialize<ObservableCollection<ProjectIdea>>(json) 
                           ?? new ObservableCollection<ProjectIdea>();
                }
            }
            catch (Exception ex)
            {
                // In production, you'd want to log this error
                System.Diagnostics.Debug.WriteLine($"Error loading data: {ex}");
            }
            return new ObservableCollection<ProjectIdea>();
        }

        public void SaveData(ObservableCollection<ProjectIdea> ideas)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_dataFile)!);
                var json = JsonSerializer.Serialize(ideas, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                File.WriteAllText(_dataFile, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving data: {ex}");
            }
        }
    }
}