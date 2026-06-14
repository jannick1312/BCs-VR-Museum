using Core;
using Godot;
using Infrastructure.Logging;
using System.Collections.Generic;

namespace BCSVRMuseum.Museum_Scripts;

public partial class SearchController : Node
{
    [Export] public NodePath InputBridgePath;
    [Export] public NodePath PictureOutputSetterPath;
    [Export] public NodePath ObjectOutputSetterPath;
    [Export] public NodePath VisibilityControllerPath;

    [Export] public int SearchLimit;

    private InputBridge _inputScreen;
    private PictureOutputSetter _outputScreen;
    private ObjectOutputSetter _objectOutput;
    private VisibilityController _visibility;

    private LineEdit _inputLineEdit;
    private LineEdit _activeLineEdit;

    private SearchUseCaseFactory _searchUseCaseFactory;
    private readonly EventLogger _logger = new(nameof(SearchController));

    private bool _isSearching;

    public override async void _Ready()
    {
        _inputScreen = GetNode<InputBridge>(InputBridgePath);
        _outputScreen = GetNode<PictureOutputSetter>(PictureOutputSetterPath);
        _objectOutput = GetNode<ObjectOutputSetter>(ObjectOutputSetterPath);
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
        if (_isSearching)
        {
            _logger.Warning("Search submit ignored because another search is already running.");
            return;
        }

        var text = _activeLineEdit.Text;

        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.Warning("Search submit ignored because query text is empty.");
            _activeLineEdit.ReleaseFocus();
            _visibility.HideKeyboard();
            return;
        }

        _activeLineEdit.Clear();
        _activeLineEdit.ReleaseFocus();
        _visibility.HideKeyboard();

        _isSearching = true;
        _logger.Info($"Search submitted.");

        var useCase = _searchUseCaseFactory.GetSearchAndLoadMedia();
        var result = await useCase.ExecuteAsync(text, SearchLimit);

        _isSearching = false;

        if (!result.Success)
        {
            _logger.Warning("Search failed, output will not be shown.");
            return;
        }

        var imageBytes = new List<byte[]>();
        var imageNames = new List<string>();
        var videoBytes = new List<byte[]>();
        var videoNames = new List<string>();
        var objectBytes = new List<byte[]>();
        var objectNames = new List<string>();

        foreach (var item in result.Items)
        {
            switch (item.MediaType)
            {
                case MediaType.Image:
                    imageBytes.Add(item.Bytes);
                    imageNames.Add(item.Name);
                    break;

                case MediaType.Video:
                    videoBytes.Add(item.Bytes);
                    videoNames.Add(item.Name);
                    break;

                case MediaType.Object3D:
                    objectBytes.Add(item.Bytes);
                    objectNames.Add(item.Name);
                    break;
            }
        }
        
        if (imageBytes.Count > 0)
            await _outputScreen.SetOutputImages(imageBytes, imageNames);
        else
            _logger.Info("Search result contains no images to display.");

        if (videoBytes.Count > 0)
            _logger.Warning($"Video display is not implemented yet.");
            

        if (objectBytes.Count > 0)
            await _objectOutput.SetOutputObjects(objectBytes, objectNames);
        else
            _logger.Info("Search result contains no 3D objects to display.");
        _logger.Info($"Search output shown.");
        _visibility.ShowOutput();
    }
}