using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace EuroGen.Components.Pages;

public partial class Dialog
{
    [CascadingParameter]
    private IMudDialogInstance? MudDialog { get; set; }

    [Parameter]
    public string ContentText { get; set; } = "";

    [Parameter]
    public string OkButtonText { get; set; } = "Ok";

    [Parameter]
    public string CancelButtonText { get; set; } = "Cancel";

    [Parameter]
    public MudBlazor.Color Color { get; set; }

    private void Submit() => MudDialog?.Close(DialogResult.Ok(true));

    private void Cancel() => MudDialog?.Close(DialogResult.Cancel());
}
