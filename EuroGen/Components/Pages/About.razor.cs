namespace EuroGen.Components.Pages;

public partial class About : IAsyncDisposable
{
    Action? languageChangedHandler;

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            languageChangedHandler = () => _ = InvokeAsync(StateHasChanged);
            Localizer.LanguageChanged += languageChangedHandler;
        }
    }

    public ValueTask DisposeAsync()
    {
        if (languageChangedHandler is not null)
        {
            Localizer.LanguageChanged -= languageChangedHandler;
            languageChangedHandler = null;
        }

		GC.SuppressFinalize(this);
		return ValueTask.CompletedTask;
    }
}
