using System;

namespace UnityEngine.UI
{
	public class ImageTipScript : MonoBehaviour
	{
		private Image mImage = null;

		public Image Img {
			set { mImage = value; }
			get { return mImage; }
		}

		public void SetImg(string spriteId, Action onComplete)
		{
			mImage.sprite = Resources.Load(spriteId) as Sprite;
		}
	}
}