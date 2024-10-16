using InternalsViewer.Internals.Engine.Address;
using InternalsViewer.UI.App.Controls.Allocation;
using InternalsViewer.UI.App.Helpers;
using InternalsViewer.UI.App.Models;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;

namespace InternalsViewer.UI.App.Controls.Page;

public sealed partial class MarkerDataTable
{
    public event EventHandler<PageAddressEventArgs>? PageClicked;

    public ObservableCollection<Marker>? Markers
    {
        get { return ((ObservableCollection<Marker>)GetValue(MarkersProperty)).Where(m => m.IsVisible).ToObservableCollection(); }
        set { SetValue(MarkersProperty, value); }
    }

    public static readonly DependencyProperty MarkersProperty = DependencyProperty
        .Register(nameof(Markers),
            typeof(ObservableCollection<Marker>),
            typeof(MarkerDataTable),
            null);

    public Marker? SelectedMarker
    {
        get => (Marker?)GetValue(SelectedMarkerProperty);
        set => SetValue(SelectedMarkerProperty, value);
    }

    public static readonly DependencyProperty SelectedMarkerProperty
        = DependencyProperty.Register(nameof(SelectedMarker),
            typeof(Marker),
            typeof(MarkerDataTable),
            null);

    public MarkerDataTable()
    {
        InitializeComponent();
    }

    private void PageLink_Click(object sender, RoutedEventArgs e)
    {
        var value = ((HyperlinkButton)sender).Content.ToString();

        if (value != null)
        {
            var rowIdentifier = RowIdentifier.Parse(value);

            var eventArgs = new PageAddressEventArgs(rowIdentifier.PageAddress.FileId, rowIdentifier.PageAddress.PageId)
            {
                Slot = rowIdentifier.SlotId
            };

            PageClicked?.Invoke(this, eventArgs);
        }
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        var value = (sender as CopyButton)?.Tag.ToString() ?? string.Empty;

        var package = new DataPackage();

        package.SetText(value);

        Clipboard.SetContent(package);
    }
}
