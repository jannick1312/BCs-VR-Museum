using Core;
using Godot;
using System.Collections.Generic;

namespace BCSVRMuseum.Museum_Scripts;

public partial class SearchController : Node
{
    [Export] public NodePath InputBridgePath;
    [Export] public NodePath PictureOutputSetterPath;
    [Export] public NodePath VisibilityControllerPath;

    [Export] public int SearchLimit;

    private InputBridge _inputScreen;
    private PictureOutputSetter _outputScreen;
    private VisibilityController _visibility;

    private LineEdit _inputLineEdit;
    private LineEdit _activeLineEdit;

    private SearchUseCaseFactory _searchUseCaseFactory;

    private bool _isSearching;

    public override async void _Ready()
    {
        _inputScreen = GetNode<InputBridge>(InputBridgePath);
        _outputScreen = GetNode<PictureOutputSetter>(PictureOutputSetterPath);
        _visibility = GetNode<VisibilityController>(VisibilityControllerPath);
        _searchUseCaseFactory = GetTree().Root.FindChild("SearchUseCaseFactory", true, false) as SearchUseCaseFactory;

        _inputLineEdit = await this.WaitFor(() => _inputScreen.InputLineEdit, "input line edit");

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

        var imageItems = new List<dynamic>();
        var videoItems = new List<dynamic>();
        var objectItems = new List<dynamic>();

        foreach (var item in result.Items)
        {
            switch (item.MediaType)
            {
                case MediaType.Image:
                    imageItems.Add(item);
                    break;

                case MediaType.Video:
                    videoItems.Add(item);
                    break;

                case MediaType.Object3D:
                    objectItems.Add(item);
                    break;
            }
        }
        
        if (imageItems.Count > 0)
        {
            var imageBytes = new List<byte[]>();

            foreach (var item in imageItems)
            {
                imageBytes.Add(item.Bytes);
            }
            _visibility.ShowOutput();
            _outputScreen.SetOutputImagesFromBytes(imageBytes);
        }

        if (videoItems.Count > 0)
            GD.PrintErr("Video display is not implemented yet.");

        if (objectItems.Count > 0)
            GD.PrintErr(" 3D object loading is not implemented yet.");
    }
}