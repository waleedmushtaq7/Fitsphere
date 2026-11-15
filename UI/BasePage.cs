namespace FitSphere.UI;

/// <summary>
/// Shared page shell: background, loading overlay, error handling and a standard header.
/// Pages override <see cref="Refresh"/> to (re)load data every time they appear, so data always reflects the database.
/// </summary>
public abstract class BasePage : ContentPage
{
    readonly Grid _root = new();
    readonly ContentView _host = new();
    readonly ActivityIndicator _spinner;

    protected BasePage(bool isTab = false)
    {
        BackgroundColor = Ds.Bg;
        Shell.SetNavBarIsVisible(this, false);
        NavigationPage.SetHasNavigationBar(this, false);
        Shell.SetTabBarIsVisible(this, isTab);

        _spinner = new ActivityIndicator
        {
            Color = Ds.Primary, IsRunning = false, IsVisible = false,
            HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center
        };
        _root.Children.Add(_host);
        _root.Children.Add(_spinner);
        Content = _root;
    }

    protected void SetBody(View view) => _host.Content = view;

    protected virtual Task Refresh() => Task.CompletedTask;

    protected async Task Busy(Func<Task> work)
    {
        _spinner.IsVisible = true;
        _spinner.IsRunning = true;
        try { await work(); }
        catch (Exception ex) { await DisplayAlert("Something went wrong", ex.Message, "OK"); }
        finally { _spinner.IsRunning = false; _spinner.IsVisible = false; }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await Busy(Refresh);
    }

    /// <summary>Page title row. Sub-pages get a back button; tab pages just show a large title.</summary>
    protected View Head(string title, bool back = true, View trailing = null)
    {
        var g = new Grid { ColumnSpacing = 12 };
        g.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        g.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        g.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        if (back) g.Add(Ui.IconButton("ic_back", () => Navigation.PopAsync()), 0, 0);
        var t = back ? Ui.Txt(title, 22, true) : Ui.Title(title);
        t.VerticalOptions = LayoutOptions.Center;
        g.Add(t, 1, 0);
        if (trailing != null) { trailing.VerticalOptions = LayoutOptions.Center; g.Add(trailing, 2, 0); }
        return g;
    }

    protected static VerticalStackLayout Column(double spacing = 20) =>
        new() { Spacing = spacing, Padding = new Thickness(Ds.PageMargin, 16, Ds.PageMargin, 32) };
}
