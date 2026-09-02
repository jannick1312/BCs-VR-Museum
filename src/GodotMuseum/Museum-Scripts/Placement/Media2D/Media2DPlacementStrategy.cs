using System.Collections.Generic;
using System.Threading.Tasks;
using BCSVRMuseum.Museum_Scripts.Placement.Helpers.Common;
using BCSVRMuseum.Museum_Scripts.Placement.Helpers.Media2D;
using BCSVRMuseum.Museum_Scripts.Placement.Helpers.Media2D.Image;
using BCSVRMuseum.Museum_Scripts.Placement.Helpers.Media2D.Video;
using Godot;
using Logger;
using Models;

namespace BCSVRMuseum.Museum_Scripts.Placement.Media2D;

/// <summary>
/// Places images and videos on wall areas.
/// </summary>
public sealed class Media2DPlacementStrategy : PlacementStrategyBase
{
	private const string GeneratedMediaGroup = "Generated2DMedia";
	private const int DefaultMaxItemsPerPlace = 2;

	private static readonly EventLogger Log = new(nameof(Media2DPlacementStrategy));

	private readonly float _cellPadding;
	private readonly RandomNumberGenerator _rng = new();
	private readonly List<VideoPlaybackController> _videos = [];

	/// <summary>
	/// Sets up image and video placement for the wall areas.
	/// </summary>
	/// <param name="owner">The node that runs placement tasks.</param>
	/// <param name="displayRoot">The template root used to create displays.</param>
	/// <param name="placesRoot">The root containing wall placement areas.</param>
	/// <param name="cellPadding">The spacing kept around media.</param>
	public Media2DPlacementStrategy(Node owner, Node3D displayRoot, Node placesRoot, float cellPadding) : base(owner, displayRoot, placesRoot, GeneratedMediaGroup)
	{
		_cellPadding = cellPadding;
		_rng.Randomize();
	}

	/// <summary>
	/// Gets the total number of images and videos that fit on all wall areas.
	/// </summary>
	/// <returns>The total number of image and video places.</returns>
	public int GetCapacity()
	{
		var capacity = 0;
		foreach (var group in PlaceCollector.Collect(PlacesRoot, DefaultMaxItemsPerPlace))
			capacity += group.MaxItems;
		return capacity;
	}

	/// <summary>
	/// Spreads images and videos across the wall areas.
	/// </summary>
	/// <param name="mediaItems">The images and videos to place.</param>
	/// <returns>A task that completes when image and video placement finishes.</returns>
	public async Task Place(IReadOnlyList<DisplayMediaItem> mediaItems)
	{
		_videos.Clear();
		ClearGenerated();

		var placeGroups = PlaceCollector.Collect(PlacesRoot, DefaultMaxItemsPerPlace);
		var nextMediaIndex = 0;
		var placementTasks = new List<Task<bool>>();

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

				placementTasks.Add(CreateDisplayInstance(item, group.Place, slots[i]));
			}
		}

		var placementResults = await Task.WhenAll(placementTasks);
		var placedMediaCount = 0;
		foreach (var placed in placementResults)
			if (placed)
				placedMediaCount++;

		if (placedMediaCount < mediaItems.Count)
			Log.Warning($"Placed {placedMediaCount} of {mediaItems.Count} images/videos.");
		else
			Log.Info($"Placed all {placedMediaCount} images/videos.");
	}

	/// <summary>
	/// Updates each placed video's playback for viewer distance.
	/// </summary>
	/// <param name="camera">The viewer camera.</param>
	/// <param name="activeDistance">The maximum distance for active playback.</param>
	public void UpdateVideos(Node3D camera, float activeDistance)
	{
		foreach (var video in _videos)
			video.UpdateForDistance(camera, activeDistance);
	}

	/// <summary>
	/// Creates one display and renders its image or video content.
	/// </summary>
	/// <param name="mediaItem">The media item to display.</param>
	/// <param name="place">The wall placement area.</param>
	/// <param name="slot">The slot assigned to the display.</param>
	/// <returns>A task containing <see langword="true"/> if the media was placed and <see langword="false"/> otherwise.</returns>
	private async Task<bool> CreateDisplayInstance(DisplayMediaItem mediaItem, Node3D place, Rect2 slot)
	{
		var instance = Media2DDisplayInstance.Create(DisplayTemplate, DisplayRoot, GeneratedGroup, _cellPadding, place, slot);
		instance.StoreRetrievableMetadata(mediaItem.Vector, mediaItem.Name, mediaItem.Path, mediaItem.Metadata);

		switch (mediaItem.MediaType)
		{
			case MediaType.Image:
				await Image2DMediaRenderer.Render(instance, mediaItem.Path);
				break;

			case MediaType.Video:
				_videos.Add(await Video2DMediaRenderer.Render(instance, mediaItem.Path, mediaItem.StartTimeSeconds));
				break;

			default:
				Log.Warning($"Skipping unsupported 2D media '{mediaItem.Name}' with type {mediaItem.MediaType}.");
				return false;
		}

		Log.Info($"Placed {mediaItem.MediaType} '{mediaItem.Name}'.");
		return true;
	}
}
