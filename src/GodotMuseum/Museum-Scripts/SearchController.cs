using Core;
using Godot;
using System.Linq;

namespace BCSVRMuseum.Museum_Scripts;

public partial class SearchController : Node
{
    [Export] public NodePath InputBridgePath;
    [Export] public NodePath PictureOutputSetterPath;
    [Export] public NodePath VisibilityControllerPath;

    [Export] public int SearchLimit = 4;

    private InputBridge _inputScreen;
    private PictureOutputSetter _outputScreen;
    private VisibilityController _visibility;

    private LineEdit _inputLineEdit;
    private LineEdit _activeLineEdit;

    private SearchUseCaseFactory _searchUseCaseFactory;

    private bool _isSearching;

    public override async void _Ready()
    {
        for (var i = 0; i < 12; i++)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        _inputScreen = GetNodeOrNull<InputBridge>(InputBridgePath);
        _outputScreen = GetNodeOrNull<PictureOutputSetter>(PictureOutputSetterPath);
        _visibility = GetNodeOrNull<VisibilityController>(VisibilityControllerPath);

        _searchUseCaseFactory = GetTree().Root.FindChild( "SearchUseCaseFactory", true, false) as SearchUseCaseFactory;

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

        var useCase = _searchUseCaseFactory.GetSearchAndLoadMedia();
        var result = await useCase.ExecuteAsync(text, SearchLimit);

        _isSearching = false;

        if (!result.Success)
        {
            GD.PrintErr(result.ErrorMessage);
            return;
        }

        var imageItems = result.Items.Where(item => item.MediaType == MediaType.Image).ToList();
        var videoItems = result.Items.Where(item => item.MediaType == MediaType.Video).ToList();
        var objectItems = result.Items.Where(item => item.MediaType == MediaType.Object3D).ToList();

        if (imageItems.Count > 0)
        {
            var imageBytes = imageItems.Select(item => item.Bytes).ToList();
            _visibility.ShowOutput();
            _outputScreen.SetOutputImagesFromBytes(imageBytes);
        }

        if (videoItems.Count > 0)
            GD.PrintErr("Video display is not implemented yet.");

        if (objectItems.Count > 0)
            GD.PrintErr(" 3D object loading is not implemented yet.");
    }
}