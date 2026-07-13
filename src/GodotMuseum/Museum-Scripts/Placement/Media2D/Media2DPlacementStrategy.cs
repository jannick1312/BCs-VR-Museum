using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BCSVRMuseum.Museum_Scripts.Placement.Helpers.Common;
using BCSVRMuseum.Museum_Scripts.Placement.Helpers.Media2D;
using BCSVRMuseum.Museum_Scripts.Placement.Helpers.Media2D.Image;
using BCSVRMuseum.Museum_Scripts.Placement.Helpers.Media2D.Video;
using Godot;
using Logger;
using Models;

namespace BCSVRMuseum.Museum_Scripts.Placement.Media2D;

public sealed class Media2DPlacementStrategy : PlacementStrategyBase
{
	private const string GeneratedMediaGroup = "Generated2DMedia";
	private const int DefaultMaxItemsPerPlace = 4;
	private static readonly EventLogger Log = new(nameof(Media2DPlacementStrategy));
	private readonly float _cellPadding;
	private readonly RandomNumberGenerator _rng = new();
	private readonly List<VideoPlaybackController> _videos = [];

	public Media2DPlacementStrategy(Node owner, Node3D displayRoot, Node placesRoot, float cellPadding) : base(owner, displayRoot, placesRoot, GeneratedMediaGroup)
	{
		_cellPadding = cellPadding;
		_rng.Randomize();
	}

	public int GetCapacity()
	{
		return PlaceCollector.Collect(PlacesRoot, DefaultMaxItemsPerPlace).Sum(group => group.MaxItems);
	}

	public async Task Place(IReadOnlyList<DisplayMediaItem> mediaItems)
	{
		_videos.Clear();
		ClearGenerated();

		var placeGroups = PlaceCollector.Collect(PlacesRoot, DefaultMaxItemsPerPlace);
		var nextMediaIndex = 0;
		var placedMediaCount = 0;

		foreach (var group in placeGroups)
		{
			if (nextMediaIndex >= mediaItems.Count)
				break;

			var randomCount = _rng.RandiRange(1, Mathf.Min(group.MaxItems, mediaItems.Count - nextMediaIndex));
			var placeSize = PlacementBounds.MeshAreaSize(group.Place);
			var slots = WallImageLayout.CreateCenteredHorizontalSlots(randomCount, placeSize.X, placeSize.Y);

			for (var i = 0; i < randomCount; i++)
			{
				await WaitForFrame();

				var item = mediaItems[nextMediaIndex];
				nextMediaIndex++;

				if (await CreateDisplayInstance(item, group.Place, slots[i]))
					placedMediaCount++;
			}
		}

		if (placedMediaCount < mediaItems.Count)
			Log.Warning($"Placed {placedMediaCount} of {mediaItems.Count} images/videos.");
		else
			Log.Info($"Placed all {placedMediaCount} images/videos.");
	}

	public void UpdateVideos(Node3D camera, float activeDistance)
	{
		foreach (var video in _videos)
			video.UpdateForDistance(camera, activeDistance);
	}

	private async Task<bool> CreateDisplayInstance(DisplayMediaItem mediaItem, Node3D place, Rect2 slot)
	{
		var instance = Media2DDisplayInstance.Create(DisplayTemplate, DisplayRoot, GeneratedGroup, _cellPadding, place, slot);
		instance.StoreRetrievableMetadata(mediaItem.Vector, mediaItem.Name, mediaItem.Path);

		switch (mediaItem.MediaType)
		{
			case MediaType.Image:
				Image2DMediaRenderer.Render(instance, mediaItem.Bytes, mediaItem.Path);
				break;

			case MediaType.Video:
				_videos.Add(await Video2DMediaRenderer.Render(instance, mediaItem.Bytes, mediaItem.Path, mediaItem.Name));
				break;

			default:
				Log.Warning($"Skipping unsupported 2D media '{mediaItem.Name}' with type {mediaItem.MediaType}.");
				return false;
		}

		Log.Info($"Placed {mediaItem.MediaType} '{mediaItem.Name}'.");
		return true;
	}
}
