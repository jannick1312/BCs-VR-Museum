using Core;
using Godot;
using Infrastructure.Media;
using Infrastructure.Vitrivr;

namespace BCSVRMuseum.Museum_Scripts;

public partial class KeyboardSubmitter : Node
{
    [Export] public NodePath InputScreenBridgePath;
    [Export] public NodePath OutputScreenBridgePath;
    [Export] public NodePath VisibilityControllerPath;

    [Export] public int SearchLimit = 1;

    private InputScreenBridge _inputScreen;
    private OutputScreenBridge _outputScreen;
    private VisibilityController _visibility;

    private LineEdit _inputLineEdit;
    private LineEdit _activeLineEdit;

    private ServerUrlStore _serverUrlStore;

    private bool _isSearching;

    public override async void _Ready()
    {
        for (var i = 0; i < 12; i++)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        _inputScreen = GetNodeOrNull<InputScreenBridge>(InputScreenBridgePath);
        _outputScreen = GetNodeOrNull<OutputScreenBridge>(OutputScreenBridgePath);
        _visibility = GetNodeOrNull<VisibilityController>(VisibilityControllerPath);

        _serverUrlStore = GetTree().Root.FindChild("ServerUrlStore", true, false) as ServerUrlStore;

        _inputLineEdit = _inputScreen.InputLineEdit;

        _inputLineEdit.FocusEntered += () => SetActiveInput(_inputLineEdit);
        _inputLineEdit.GuiInput += inputEvent => OnInputGuiInput(inputEvent, _inputLineEdit);
    }

    private void SetActiveInput(LineEdit lineEdit)
    {
        _activeLineEdit = lineEdit;
        _visibility.ShowKeyboard();
    }

    private void OnInputGuiInput(InputEvent inputEvent, LineEdit lineEdit)
    {
        if (inputEvent is not InputEventMouseButton mouseButton || !mouseButton.Pressed)
            return;

        _activeLineEdit = lineEdit;
        _visibility.ShowKeyboard();
    }

    public async void SubmitText()
    {
        if (_activeLineEdit == null || _isSearching)
            return;

        var text = _activeLineEdit.Text;

        if (string.IsNullOrWhiteSpace(text))
        {
            _activeLineEdit.ReleaseFocus();
            _visibility.HideKeyboard();
            return;
        }

        _activeLineEdit.Clear();
        _activeLineEdit.ReleaseFocus();
        _visibility.HideKeyboard();

        _isSearching = true;

        var query = new SearchQuery(text, SearchLimit);

        var searchService = new VitrivrSearchService(_serverUrlStore.Settings);
        var result = await searchService.SearchAsync(query);

        _isSearching = false;

        if (!result.Success)
        {
            GD.PrintErr(result.ErrorMessage);
            return;
        }

        var item = result.FirstOrDefault();

        if (item == null)
        {
            GD.PrintErr("Search returned no result item.");
            return;
        }

        switch (item.MediaType)
        {
            case MediaType.Image:
                ShowImage(item);
                break;

            case MediaType.Video:
                GD.PrintErr("Video results are recognized, but video display is not implemented yet.");
                break;

            case MediaType.Object3D:
                GD.PrintErr("3D object results are recognized, but 3D loading is not implemented yet.");
                break;

            case MediaType.Unknown:
                GD.PrintErr("Not a known Media Type.");
                break;
            
            default:
                GD.PrintErr("Unknown media type: " + item.FileName);
                break;
        }
    }

    private void ShowImage(SearchResultItem item)
    {
        if (MediaResolver.IsLocal(item))
        {
            GD.Print("LOCAL MEDIA FOUND -> loading from local path");
            _outputScreen.SetOutputImageFromLocalPath(item.LocalPath);
        }
        else
        {
            GD.Print("LOCAL MEDIA NOT FOUND -> loading from remote URL");
            _outputScreen.SetOutputImageFromUrl(item.RemoteUrl);
        }

        _visibility.ShowOutput();
    }
}