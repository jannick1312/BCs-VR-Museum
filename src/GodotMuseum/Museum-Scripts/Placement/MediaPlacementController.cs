using System.Collections.Generic;
using System.Threading.Tasks;
using BCSVRMuseum.Museum_Scripts.Placement.Media2D;
using BCSVRMuseum.Museum_Scripts.Placement.Object3D;
using Godot;
using Logger;
using Models;

namespace BCSVRMuseum.Museum_Scripts.Placement;

public partial class MediaPlacementController : Node
{
	private static readonly EventLogger Log = new(nameof(MediaPlacementController));
	private Media2DPlacementStrategy _media2DPlacement;
	private Object3DPlacementStrategy _object3DPlacement;
	private Node3D _playerCamera;

	[Export] public float CellPadding;
	[Export] public NodePath Media2DInstancePath;
	[Export] public NodePath Media2DPlacesPath;
	[Export] public NodePath Object3DInstancePath;
	[Export] public NodePath Object3DPlacesPath;
	[Export] public float VideoResetDistance;

	public override void _Ready()
	{
		var media2DInstance = GetNode<Node3D>(Media2DInstancePath);
		var media2DPlaces = GetNode(Media2DPlacesPath);
		var object3DInstance = GetNode<Node3D>(Object3DInstancePath);
		var object3DPlaces = GetNode(Object3DPlacesPath);

		_playerCamera = (Node3D)GetTree().Root.FindChild("XRCamera3D", true, false);
		_media2DPlacement = new Media2DPlacementStrategy(this, media2DInstance, media2DPlaces, CellPadding);
		_object3DPlacement = new Object3DPlacementStrategy(this, object3DInstance, object3DPlaces);
	}

	public (int Media2D, int Objects3D) GetCapacity()
	{
		return (_media2DPlacement.GetCapacity(), _object3DPlacement.GetCapacity());
	}

	public async Task Place(IReadOnlyList<DisplayMediaItem> items)
	{
		var media2DItems = new List<DisplayMediaItem>();
		var object3DItems = new List<DisplayMediaItem>();
		foreach (var item in items)
			if (item.MediaType is MediaType.Image or MediaType.Video)
				media2DItems.Add(item);
			else if (item.MediaType == MediaType.Object3D)
				object3DItems.Add(item);

		await _media2DPlacement.Place(media2DItems);
		if (media2DItems.Count == 0)
			Log.Info("No 2D media selected for placement. Items=0.");

		await _object3DPlacement.Place(object3DItems);
		if (object3DItems.Count == 0)
			Log.Info("No 3D media selected for placement. Items=0.");
	}

	public override void _Process(double delta)
	{
		_media2DPlacement.UpdateVideos(_playerCamera, VideoResetDistance);
	}
}
