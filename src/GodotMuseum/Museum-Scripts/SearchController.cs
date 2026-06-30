using Godot;
using Logger;

namespace BCSVRMuseum.Museum_Scripts;

public partial class SearchController : Node
{
    [Export] public NodePath InputBridgePath;
    [Export] public NodePath MediaPlacementControllerPath;
    [Export] public NodePath VisibilityControllerPath;
    [Export] public int SearchLimit;

    private InputBridge _inputScreen;
    private Placement.MediaPlacementController _mediaPlacement;
    private VisibilityController _visibility;
    private LineEdit _inputLineEdit;
    private LineEdit _activeLineEdit;
    private SearchUseCaseFactory _searchUseCaseFactory;
    private readonly EventLogger _logger = new(nameof(SearchController));
    private bool _isSearching;

    public override async void _Ready()
    {
        _inputScreen = GetNode<InputBridge>(InputBridgePath);
        _mediaPlacement = GetNode<Placement.MediaPlacementController>(MediaPlacementControllerPath);
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

        await _mediaPlacement.Place(result.Items);
        _logger.Info("Search results shown.");
    }
}