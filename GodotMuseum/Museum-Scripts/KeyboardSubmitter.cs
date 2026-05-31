using Application;
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

        var useCase = CreateSearchUseCase();
        var result = await useCase.ExecuteAsync(text, SearchLimit);

        _isSearching = false;

        if (!result.Success)
        {
            GD.PrintErr(result.ErrorMessage);
            return;
        }

        switch (result.MediaType)
        {
            case MediaType.Image:
                _outputScreen.SetOutputImageFromBytes(result.Bytes);
                _visibility.ShowOutput();
                break;

            case MediaType.Video:
                GD.PrintErr("Video display is not implemented yet.");
                break;

            case MediaType.Object3D:
                GD.PrintErr("3D object loading is not implemented yet.");
                break;

            default:
                GD.PrintErr("Unknown media type: " + result.FileName);
                break;
        }
    }

    private SearchAndLoadMedia CreateSearchUseCase()
    {
        ISearchService searchService = new VitrivrSearchService(_serverUrlStore.Settings);

        IMediaLoader mediaLoader = new MediaResolver();

        return new SearchAndLoadMedia(searchService, mediaLoader);
    }
}