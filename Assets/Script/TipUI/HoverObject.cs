/*
在ui掛 上HoverObject
用法1.填入要顯示的多語系，呼叫TipUI顯示文字內容
用法2.SetTipContent(TipContentText, hoverAction)
*/

using System;
using UnityEngine.EventSystems;

namespace UnityEngine.UI
{
	public class HoverObject : UIBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
	{
		public enum TipStyle
		{
			Txt,
			Image_Asset_Bundle,
			Image_Atlas,
			Custom,
		}

		public TipStyle ThisTipStyle;

		#region 文字tip
		public string Title;
		public string Content;

		private bool mTitleLocalize = true;
		private bool mContentLocalize = true;
		#endregion

		#region 圖像tip(AssetBundle)
		public string AssetId;
		#endregion

		#region 圖像tip(Atlas)
		public string AltasName;
		public string SpriteId;
		#endregion

		private Action mHoverAction;

		public void SetTipContent(TipContentText t, Action hoverAction = null)
		{
			Title = t.Title;
			Content = t.Content;

			if (null != hoverAction)
				mHoverAction = hoverAction;
		}

		#region override funciton
		public virtual void OnPointerEnter(PointerEventData eventData)
		{
			Hover(eventData);
		}

		public virtual void OnPointerExit(PointerEventData eventData)
		{
			Exit();
		}

		public virtual void OnPointerDown(PointerEventData eventData)
		{
			Exit();
		}

		public virtual void OnPointerUp(PointerEventData eventData)
		{
			Exit();
		}
		#endregion

		private void Hover(PointerEventData eventData)
		{
			if (!IsActive())
				return;

			if (null != mHoverAction)
				mHoverAction();

			TipUIManager.Show("Test Title", "Test Content....................", true);

			//switch (ThisTipStyle)
			//{
			//	case TipStyle.Txt:
			//		if (string.IsNullOrEmpty(Title) && string.IsNullOrEmpty(Content))
			//			return;

			//		string t, c;

			//		t = Title;
			//		c = Content;

			//		CommonTipUI.Instance.ShowTip(t, c);

			//		break;
			//	case TipStyle.Image_Asset_Bundle:
			//		if (string.IsNullOrEmpty(AssetId))
			//			return;

			//		CommonTipUI.Instance.ShowTip(AssetId, 0);
			//		break;
			//	case TipStyle.Image_Atlas:
			//		break;
			//	case TipStyle.Custom:
			//		break;
			//	default:
			//		break;
			//}
		}

		private void Exit()
		{
			if (!IsActive())
				return;

			TipUIManager.Instance.HideTip();
		}
	}

	public class TipContentText
	{
		public string Title = string.Empty;
		public string Content = string.Empty;
	}

	public class TipContentImage
	{
	}
}