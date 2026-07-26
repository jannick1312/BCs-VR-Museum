using System.Collections.Generic;
using BCSVRMuseum.Museum_Scripts.Decision;
using BCSVRMuseum.Museum_Scripts.Placement.Helpers.Common;
using BCSVRMuseum.Museum_Scripts.Placement.Helpers.Media2D;
using Godot;

namespace BCSVRMuseum.Museum_Scripts.Placement.Media2D;

public sealed class Media2DDisplayInstance
{
	private readonly float _cellPadding;
	private readonly FrameMaker _frameMaker;
	private readonly Node3D _place;
	private readonly Rect2 _slot;

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

	public static Media2DDisplayInstance Create(Node3D template, Node3D displayRoot, string groupName, float cellPadding, Node3D place, Rect2 slot)
	{
		var item = (Node3D)template.Duplicate();
		item.AddToGroup(groupName);
		displayRoot.AddChild(item);
		NodeTreeActivator.SetActive(item, true);
		item.GetNode<Label3D>("Play").Visible = false;

		return new Media2DDisplayInstance(item, item.GetNode<MeshInstance3D>("Picture"), place, slot, cellPadding);
	}

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

	public void StoreRetrievableMetadata(IReadOnlyList<double> vector, string mediaName, string mediaPath)
	{
		RetrievableMetadata.Store(Item, vector, mediaName, mediaPath);
		ConfigureActionPopups(vector, mediaPath);
	}

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
