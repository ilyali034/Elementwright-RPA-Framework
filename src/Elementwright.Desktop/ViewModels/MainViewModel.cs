using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Elementwright.Desktop.Models;
using Elementwright.Desktop.Services;

namespace Elementwright.Desktop.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IApplicationDiscovery _applicationDiscovery;

    [ObservableProperty]
    private ObservableCollection<ApplicationInfo> applications = [];

    [ObservableProperty]
    private ApplicationInfo? selectedApplication;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private string statusMessage = "Ready";

    [ObservableProperty]
    private int discoveredCount;

    public IEnumerable<ApplicationInfo> FilteredApplications =>
        string.IsNullOrWhiteSpace(SearchText)
            ? Applications
            : Applications.Where(x =>
                x.WindowTitle.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase) ||
                x.ProcessName.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase));

    public MainViewModel(IApplicationDiscovery applicationDiscovery)
    {
        _applicationDiscovery = applicationDiscovery;
    }

    partial void OnSearchTextChanged(string value) =>
        OnPropertyChanged(nameof(FilteredApplications));

    [RelayCommand]
    private async Task RefreshApplicationsAsync()
    {
        if (IsLoading)
            return;

        try
        {
            IsLoading = true;
            StatusMessage = "Scanning desktop applications...";

            var result = await _applicationDiscovery.GetApplicationsAsync();

            Applications = new ObservableCollection<ApplicationInfo>(result);
            DiscoveredCount = result.Count;
            OnPropertyChanged(nameof(FilteredApplications));

            StatusMessage = $"Discovery complete · {DiscoveredCount} windows found";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Discovery failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ConnectToApplication()
    {
        if (SelectedApplication is null)
            return;

        StatusMessage = $"Selected target: {SelectedApplication.WindowTitle} (PID {SelectedApplication.ProcessId})";
    }
}
