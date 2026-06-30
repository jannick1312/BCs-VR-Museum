using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BCSVRMuseum.Museum_Scripts.Placement.Media2D;
using BCSVRMuseum.Museum_Scripts.Placement.Object3D;
using Godot;
using Logger;
using Models;

namespace BCSVRMuseum.Museum_Scripts.Placement;

public partial class MediaPlacementController : Node
{
	[Export] public NodePath Media2DInstancePath;
	[Export] public NodePath Media2DPlacesPath;
	[Export] public NodePath Object3DInstancePath;
	[Export] public NodePath Object3DPlacesPath;
	[Export] public float CellPadding;
	[Export] public float VideoResetDistance = 2.0f;

	private Media2DPlacementStrategy _media2DPlacement;
	private Object3DPlacementStrategy _object3DPlacement;
	private Node3D _playerCamera;
	private static readonly EventLogger Log = new(nameof(MediaPlacementController));

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

	public async Task Place(IReadOnlyList<DisplayMediaItem> items)
	{
		var media2DItems = items.Where(item => item.MediaType is MediaType.Image or MediaType.Video).ToList();
		var object3DItems = items.Where(item => item.MediaType == MediaType.Object3D).ToList();

		if (media2DItems.Count > 0)
			await _media2DPlacement.Place(media2DItems);
		else
			Log.Info("Search result contains no images or videos to display.");

		if (object3DItems.Count > 0)
			await _object3DPlacement.Place(object3DItems);
		else
			Log.Info("Search result contains no 3D objects to display.");
	}

	public override void _Process(double delta)
	{
		_media2DPlacement.UpdateVideos(_playerCamera, VideoResetDistance);
	}
}