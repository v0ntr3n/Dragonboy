using System;
using UnityEngine;

namespace Mod.Graphics
{
	internal class UIImage : UnityEngine.UI.Image
	{
		internal static Mesh mesh;
		static Texture2D texture;
		static UIImage image;

		[Obsolete]
		protected override void OnPopulateMesh(Mesh m)
		{
			base.OnPopulateMesh(m);
			mesh = m;
		}

		[Obsolete]
		internal static void PopulateMesh()
		{
			if (image == null || mesh == null)
				return;
			image.OnPopulateMesh(mesh);
		}

		internal static void OnStart()
		{
			GameObject mainCamera = GameObject.Find("Main Camera");
			if (mainCamera == null || image != null)
				return;
			Canvas canvas = mainCamera.GetComponent<Canvas>();
			if (canvas == null)
				canvas = mainCamera.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			GameObject uiImageCooldown = new GameObject("Cooldown Effect");
			image = uiImageCooldown.AddComponent<UIImage>();
			texture = new Texture2D(1, 1);
			texture.SetPixel(0, 0, Color.black);
			texture.Apply();
			image.sprite = UnityEngine.Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f));
			image.type = Type.Filled;
			image.fillMethod = FillMethod.Radial360;
			image.fillClockwise = true;
			uiImageCooldown.transform.SetParent(mainCamera.transform);
			uiImageCooldown.transform.position = new Vector3(-500, -500);
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			if (sprite != null)
			{
				Destroy(sprite);
				sprite = null;
			}
			if (texture != null)
			{
				Destroy(texture);
				texture = null;
			}
			if (image == this)
			{
				image = null;
			}
		}
	}
}
