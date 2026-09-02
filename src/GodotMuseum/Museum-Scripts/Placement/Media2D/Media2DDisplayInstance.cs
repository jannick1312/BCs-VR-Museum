using System.Collections.Generic;
using BCSVRMuseum.Museum_Scripts.Decision;
using BCSVRMuseum.Museum_Scripts.Placement.Helpers.Common;
using BCSVRMuseum.Museum_Scripts.Placement.Helpers.Media2D;
using Godot;
using Models;

namespace BCSVRMuseum.Museum_Scripts.Placement.Media2D;

/// <summary>
/// Creates and manages one image or video display.
/// </summary>
public sealed class Media2DDisplayInstance
{
	private readonly float _cellPadding;
	private readonly FrameMaker _frameMaker;
	private readonly Node3D _place;
	private readonly Rect2 _slot;

	/// <summary>
	/// Sets up a created display and its place on the wall.
	/// </summary>
	/// <param name="item">The root node of the created display.</param>
	/// <param name="displaySurface">The mesh receiving the media texture.</param>
	/// <param name="place">The wall placement area.</param>
	/// <param name="slot">The slot assigned to the display.</param>
	/// <param name="cellPadding">The spacing kept around the media.</param>
	private Media2DDisplayInstance(Node3D item, MeshInstance3D displaySurface, Node3D place, Rect2 slot, float cellPadding)
	{
		Item = item;
		DisplaySurface = displaySurface;
		_place = place;
		_slot = slot;
		_cellPadding = cellPadding;
		_frameMaker = item.GetNode<FrameMaker>("FrameMaker");
	}

	public Node3D Item { get; }

	private MeshInstance3D DisplaySurface { get; }

	/// <summary>
	/// Creates and shows a display from the template.
	/// </summary>
	/// <param name="template">The display template to duplicate.</param>
	/// <param name="displayRoot">The node that receives the created display.</param>
	/// <param name="groupName">The group used for created displays.</param>
	/// <param name="cellPadding">The spacing kept inside the slot.</param>
	/// <param name="place">The wall placement area.</param>
	/// <param name="slot">The slot assigned to the display.</param>
	/// <returns>The created display.</returns>
	public static Media2DDisplayInstance Create(Node3D template, Node3D displayRoot, string groupName, float cellPadding, Node3D place, Rect2 slot)
	{
		var item = (Node3D)template.Duplicate();
		item.AddToGroup(groupName);
		displayRoot.AddChild(item);
		NodeTreeActivator.SetActive(item, true);
		item.GetNode<Label3D>("Play").Visible = false;

		return new Media2DDisplayInstance(item, item.GetNode<MeshInstance3D>("Picture"), place, slot, cellPadding);
	}

	/// <summary>
	/// Shows a media texture and fits the display to the shape.
	/// </summary>
	/// <param name="texture">The texture to display.</param>
	public void ShowTexture(Texture2D texture)
	{
		DisplaySurface.MaterialOverride = new StandardMaterial3D
		{
			CullMode = BaseMaterial3D.CullModeEnum.Disabled,
			AlbedoTexture = texture,
			TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmapsAnisotropic
		};

		ResizeToAspect((float)texture.GetWidth() / texture.GetHeight());
	}

	/// <summary>
	/// Adds search data and sets up the display's action popups.
	/// </summary>
	/// <param name="vector">The feature vector for the media.</param>
	/// <param name="mediaName">The display name of the media.</param>
	/// <param name="mediaPath">The media file path.</param>
	/// <param name="metadata">The metadata for the media.</param>
	public void StoreRetrievableMetadata(IReadOnlyList<double> vector, string mediaName, string mediaPath, MediaMetadata metadata)
	{
		RetrievableMetadata.Store(Item, vector, mediaName, mediaPath, metadata);
		ConfigureActionPopups(vector, mediaPath);
	}

	/// <summary>
	/// Fits and centers the display while keeping the image shape.
	/// </summary>
	/// <param name="aspect">The image width divided by its height.</param>
	private void ResizeToAspect(float aspect)
	{
		var maxWidth = Mathf.Max(0.1f, _slot.Size.X - _cellPadding);
		var maxHeight = Mathf.Max(0.1f, _slot.Size.Y - _cellPadding);
		var width = maxWidth;
		var height = width / aspect;

		if (height > maxHeight)
		{
			height = maxHeight;
			width = height * aspect;
		}

		var x = _slot.Position.X + _slot.Size.X / 2.0f;
		var y = _slot.Position.Y + _slot.Size.Y / 2.0f;

		Item.GlobalTransform = new Transform3D(_place.GlobalTransform.Basis.Orthonormalized(), _place.ToGlobal(new Vector3(x, y, 0)));
		DisplaySurface.Scale = new Vector3(width, height, 1.0f);
		_frameMaker.UpdateFrame(DisplaySurface, width, height);
	}

	/// <summary>
	/// Sets search data on every action popup in the display.
	/// </summary>
	/// <param name="vector">The feature vector for the media.</param>
	/// <param name="mediaPath">The media file path.</param>
	private void ConfigureActionPopups(IReadOnlyList<double> vector, string mediaPath)
	{
		foreach (var child in Item.FindChildren("*", "", true, false))
		{
			if (child is not DisplayActionPopup popup)
				continue;
			NodeTreeActivator.SetActive(popup.GetParent<Node3D>(), false);
			popup.SetVector(vector);
			popup.SetSourcePath(mediaPath);
		}
	}
}



// All calculations in this file were implemented with the assistance of Codex.
