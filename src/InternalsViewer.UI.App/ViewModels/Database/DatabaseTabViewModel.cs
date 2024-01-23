﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using InternalsViewer.Internals.Engine.Address;
using InternalsViewer.Internals.Engine.Database;
using InternalsViewer.UI.App.Messages;
using InternalsViewer.UI.App.Models;
using InternalsViewer.UI.App.ViewModels.Allocation;
using InternalsViewer.UI.App.ViewModels.Tabs;
using DatabaseFile = InternalsViewer.UI.App.Models.DatabaseFile;

namespace InternalsViewer.UI.App.ViewModels.Database;

public class DatabaseTabViewModelFactory
{
    public DatabaseTabViewModel Create(DatabaseSource database)
        => new(database);
}

public partial class DatabaseTabViewModel(DatabaseSource database): TabViewModel
{
    [ObservableProperty]
    private DatabaseSource database = database;

    [ObservableProperty]
    private DatabaseFile[] databaseFiles = Array.Empty<DatabaseFile>();

    [ObservableProperty]
    private ObservableCollection<AllocationLayer> allocationLayers = new();

    [ObservableProperty]
    private AllocationLayer? selectedLayer;

    [ObservableProperty]
    private int extentCount;

    [ObservableProperty]
    private Allocation.AllocationOverViewModel allocationOver = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(GridAllocationLayers))]
    private string filter = string.Empty;

    [ObservableProperty]
    private bool isDetailVisible = true;

    [ObservableProperty]
    private bool isTooltipEnabled;

    [ObservableProperty]
    private short fileId = 1;

    [ObservableProperty]
    private double allocationMapHeight = 200;

    [RelayCommand]
    private void OpenPage(PageAddress pageAddress)
    {
        WeakReferenceMessenger.Default.Send(new OpenPageMessage(new OpenPageRequest(database, pageAddress)));
    }

    public List<AllocationLayer> GridAllocationLayers
        => allocationLayers.Where(w => string.IsNullOrEmpty(Filter) || w.Name.ToLower().Contains(filter.ToLower())).ToList();

    public void Load(string name)
    {
        Name = name;

        DatabaseFiles = database.Files
                                .Select(f => new DatabaseFile(this) { FileId = f.FileId, Size = f.Size })
                                .ToArray();
        IsLoading = true;

        var layers = AllocationLayerBuilder.GenerateLayers(database, true);

        ExtentCount = database.GetFileSize(1) / 8;
        AllocationLayers = new ObservableCollection<AllocationLayer>(layers);

        IsLoading = false;
    }
}