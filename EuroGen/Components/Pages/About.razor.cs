namespace EuroGen.Components.Pages;

public partial class About
{
    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            Localizer.LanguageChanged += () =>
            {
                StateHasChanged();
            };
        }
    }
}
