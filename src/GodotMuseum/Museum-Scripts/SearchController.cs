using Godot;
using Logger;
using Models;
using System.Collections.Generic;

namespace BCSVRMuseum.Museum_Scripts;

public partial class SearchController : Node
{
    [Export] public NodePath InputBridgePath;
    [Export] public NodePath Media2DOutputSetterPath;
    [Export] public NodePath ObjectOutputSetterPath;
    [Export] public NodePath VisibilityControllerPath;

    [Export] public int SearchLimit;

    private InputBridge _inputScreen;
    private Media2DOutputSetter _outputScreen;
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
        _outputScreen = GetNode<Media2DOutputSetter>(Media2DOutputSetterPath);
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
        if (inputEvent is not InputEventMouseButton { Pressed: true })
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
        _logger.Info("Search submitted.");

        var application = _searchUseCaseFactory.GetMuseumApplication();
        var result = await application.SearchAsync(text, SearchLimit);

        _isSearching = false;

        if (!result.Success)
        {
            _logger.Warning("Search failed, output will not be shown.");
            return;
        }

        var media2DBytes = new List<byte[]>();
        var media2DPaths = new List<string>();
        var media2DNames = new List<string>();
        var media2DIsVideo = new List<bool>();
        var objectBytes = new List<byte[]>();
        var objectPaths = new List<string>();
        var objectNames = new List<string>();

        foreach (var item in result.Items)
        {
            switch (item.MediaType)
            {
                case MediaType.Image:
                    media2DBytes.Add(item.Bytes);
                    media2DPaths.Add(item.Path);
                    media2DNames.Add(item.Name);
                    media2DIsVideo.Add(false);
                    break;

                case MediaType.Video:
                    media2DBytes.Add(item.Bytes);
                    media2DPaths.Add(item.Path);
                    media2DNames.Add(item.Name);
                    media2DIsVideo.Add(true);
                    break;

                case MediaType.Object3D:
                    objectBytes.Add(item.Bytes);
                    objectPaths.Add(item.Path);
                    objectNames.Add(item.Name);
                    break;
            }
        }
        
        if (media2DBytes.Count > 0)
            await _outputScreen.SetOutput2DMedia(media2DBytes, media2DPaths, media2DNames, media2DIsVideo);
        else
            _logger.Info("Search result contains no images or videos to display.");
        
        if (objectBytes.Count > 0)
            await _objectOutput.SetOutputObjects(objectBytes, objectPaths, objectNames);
        else
            _logger.Info("Search result contains no 3D objects to display.");
        
        _logger.Info("Search output shown.");
        _visibility.ShowOutput();
    }
}