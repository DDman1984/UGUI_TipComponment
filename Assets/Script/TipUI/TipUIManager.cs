#region 用法說明
/*----
CommonTipUI 用法說明：
※各種tip ui的ResourceKey目前是從9000開始，SimpleTipItem是一個通用的tip ui樣式(黑底&白色文字)，其餘為個各自定義的tip ui

※如果要用到通用樣式(黑底白字)的tip可以這樣call：
	if (null != CommonTipUI.Instance.InitialNormalTipUI(想要顯示的文字內容))
		CommonTipUI.Instance.ShowTip(eventData);

-----------------------------------------------------------------------------------------

※如果要顯示自定義樣式的tip：
	1.建立tip prefab並且加入ResourceKey裡面
	2.如果tip內容是動態的，可以寫一隻繼承TipContentScript的code，覆寫SetTipContent方法來管理控制tip資訊的顯示，並掛在該tip prefab上面。
	  然後將要顯示的資料用object[]裝起來，用法如下：
	
	固定內容=>
	if (null != CommonTipUI.Instance.InitialCustomTipUI(該tip的ResourceKey))
		CommonTipUI.Instance.ShowTip(eventData);

	動態內容=>
	if (null !=  CommonTipUI.Instance.InitialCustomTipUI<剛剛寫的那隻繼承TipContentScript的code>(該tip的ResourceKey, 包裝好的object[]))
		CommonTipUI.Instance.ShowTip(eventData);

-----------------------------------------------------------------------------------------

※tip的開&關
	顯示tip=> CommonTipUI.Instance.ShowTip(eventData,[option]延遲顯示的時間);
	關閉tip=> CommonTipUI.Instance.HideTip();
 ----*/
#endregion

using System.Collections;
using System.Collections.Generic;

using UnityEngine.EventSystems;

using Gamesofa;
using Gamesofa.Tank;

//using DG.Tweening;

namespace UnityEngine.UI
{
	public class TipUIManager : MonoBehaviour //, IUpdate
	{
		public enum TipType
		{
			Txt,
			Img,
			Custom,
			Unknow,
		}

		public enum AnchorPivot
		{
			TopLeft,
			TopCenter,
			TopRight,

			MiddleLeft,
			MiddleCenter,
			MiddleRight,

			BottomLeft,
			BottonCenter,
			BottomRight,

			Default,    //左下定位點
		}

		private static Vector2[] mV2Arr = new Vector2[]
		{
			new Vector2(0f, 1f),
			new Vector2(0.5f, 1f),
			new Vector2(1f, 1f),

			new Vector2(0, 0.5f),
			new Vector2(0.5f, 0.5f),
			new Vector2(1, 0.5f),

			new Vector2(0f, 0f),
			new Vector2(0.5f, 0f),
			new Vector2(1f, 0f)
		};

		private static Dictionary<AnchorPivot, Vector2> mRectAnchorDict = new Dictionary<AnchorPivot, Vector2>(10)
		{
			{ AnchorPivot.TopLeft, mV2Arr[0] },
			{ AnchorPivot.TopCenter, mV2Arr[1] },
			{ AnchorPivot.TopRight, mV2Arr[2] },
			{ AnchorPivot.MiddleLeft, mV2Arr[3] },
			{ AnchorPivot.MiddleCenter, mV2Arr[4] },
			{ AnchorPivot.MiddleRight, mV2Arr[5] },
			{ AnchorPivot.BottomLeft, mV2Arr[6] },
			{ AnchorPivot.BottonCenter, mV2Arr[7] },
			{ AnchorPivot.BottomRight, mV2Arr[8] },

			{ AnchorPivot.Default, mV2Arr[6] },
		};

		private static float mTextUIHeight = 24f;

		public static TipUIManager Instance = null;

		/// <summary>Root</summary>
		private RectTransform mRoot = null;

		/// <summary>Tip ui rect</summary>
		private RectTransform mRect = null;

		private CanvasGroup mCanvasGroup = null;

		private TipType mNowTipType;

		private bool mIsTipShowing = false;

		/// <summary>在同一個ui元件停佇幾秒後顯示</summary>
		private float mWaitShowTime = 1f;

		private Vector2 mNowPosition = Vector2.zero;

		private AnchorPivot mNowAnchorPivot;

		private AnchorPivot mPrefferedAnchor = AnchorPivot.Default;

		private PointerEventData mNowPointData = null;

		private GameObject mNowTipContentObj = null;

		private Coroutine mRoutin = null;

		//private Tweener mTweener = null;

		//private Camera mRootCamera = null;

		private Dictionary<TipType, GameObject> mTipDict = new Dictionary<TipType, GameObject>();

		private float mTipLimitWidth = 400f;

		void Awake()
		{
			Instance = this;
		}

		void OnDestroy()
		{
			if (null != mTipDict && mTipDict.Count > 0)
				mTipDict.Clear();
		}

		#region public static function
		public static void Initialize()
		{
			if (null == Instance)
			{
				//Find root canvas
				GameObject rootCVObj = null;
				Canvas[] cvArr = GameObject.FindObjectsOfType<Canvas>();

				if (cvArr.Length == 0)
					rootCVObj = new GameObject("Canvas", typeof(Canvas));
				else
				{
					foreach (var cv in cvArr)
					{
						if (cv.GetComponentsInParent<Canvas>(true).Length == 1)
						{
							rootCVObj = cv.gameObject;
							break;
						}
					}
				}

				if (null != rootCVObj)
				{
					GameObject obj = new GameObject("TipManager", typeof(RectTransform), typeof(TipUIManager));
					DontDestroyOnLoad(obj);
					obj.transform.SetParent(rootCVObj.transform);
					RectTransform rt = obj.GetComponent<RectTransform>();
					rt.anchorMin = Vector2.zero;
					rt.anchorMax = Vector2.one;
					rt.pivot = new Vector2(0.5f, 0.5f);

					rt.offsetMin = Vector2.zero;
					rt.offsetMax = Vector2.zero;

					Instance.mRoot = rt;
				}
			}
		}

		/// <summary>Image Tip Content</summary>
		/// <param name="root"></param>
		/// <param name="assetImageId"></param>
		public static void Show(string assetImageId, AnchorPivot preferredAnchor = AnchorPivot.Default)
		{
			if (null == Instance)
				Initialize();

			Instance.StartCoroutine(Instance.WaitForUIInitial(TipType.Img, preferredAnchor, new object[1] { assetImageId }));
		}

		/// <summary>Text Tip Content</summary>
		/// <param name="root"></param>
		/// <param name="topContent"></param>
		/// <param name="bottomContent"></param>
		public static void Show(string topContent, string bottomContent = "", AnchorPivot preferredAnchor = AnchorPivot.Default, TextAnchor topCTAnchor = TextAnchor.UpperLeft, TextAnchor bottomCTAnchor = TextAnchor.UpperLeft)
		{
			if (null == Instance)
				Initialize();

			Instance.StartCoroutine(Instance.WaitForUIInitial(TipType.Txt, preferredAnchor, new object[4] { topContent, bottomContent, topCTAnchor, bottomCTAnchor }));
		}

		IEnumerator WaitForUIInitial(TipType type, AnchorPivot ac, params object[] value)
		{
			while (null == mRoot)
				yield return null;

			if (type == TipType.Unknow)
				yield break;

			mNowTipContentObj = null;
			yield return CreateTip(type, ac);

			while(null == mNowTipContentObj)
				yield return null;

			switch (type)
			{
				case TipType.Txt:
					ShowTip((string)value[0], (string)value[1], (TextAnchor)value[2], (TextAnchor)value[3], 0f);
					break;
				case TipType.Img:
					ShowTip((string)value[0], 0f);
					break;
			}
		}

		#endregion

		#region Image tip
		/// <summary>圖像tip</summary>
		/// <param name="imgAssetId"></param>
		/// <param name="wait"></param>
		/// <param name="preferredAnchor"></param>
		private void ShowTip(string imgAssetId, float wait = 0.0f)
		{
			if (null != mNowTipContentObj)
			{
				ImageTipScript img = mNowTipContentObj.GetComponent<ImageTipScript>();

				mIsTipShowing = true;
				mNowPosition = Input.mousePosition;
				mWaitShowTime = wait;

				img.SetImg(imgAssetId, WaitForAssetLoading);
			}
		}

		private void WaitForAssetLoading()
		{
			ClearCoroutine();
			mRoutin = StartCoroutine(DisplayUI());
		}
		#endregion

		#region text content tip
		/// <summary>文字tip</summary>
		/// <param name="title"></param>
		/// <param name="contentStr"></param>
		/// <param name="wait"></param>
		/// <param name="preferredAnchor"></param>
		/// <param name="topCTAnchor"></param>
		/// <param name="contentAnchor"></param>
		private void ShowTip(string topContent, string bottomContent = "", TextAnchor topCTAnchor = TextAnchor.UpperLeft, TextAnchor bottomCTAnchor = TextAnchor.UpperLeft, float wait = 0.0f)
		{
			if (null != mNowTipContentObj)
			{
				Text[] txtArr = mNowTipContentObj.GetComponentsInChildren<Text>(true);

				if (txtArr.Length == 0)
				{
					Debug.LogError("tip type incorrect!");
					return;
				}

				//foreach (Text t in txtArr)
				//	Debug.Log(t.name);

				RectTransform rt_Top = txtArr[0].GetComponent<RectTransform>();
				RectTransform rt_Bottom = txtArr[1].GetComponent<RectTransform>();

				LayoutElement ly_Top = txtArr[0].gameObject.GetComponent<LayoutElement>();
				LayoutElement ly_Bottom = txtArr[1].gameObject.GetComponent<LayoutElement>();

				//Logic:base on top text element, to adjust the text tip ui size
				ContentSizeFitter size_Fitter_top = txtArr[0].gameObject.GetComponent<ContentSizeFitter>();
				ContentSizeFitter size_Fitter_Bottom = txtArr[1].gameObject.GetComponent<ContentSizeFitter>();

				#region reset ui before display
				size_Fitter_top.enabled = false;
				size_Fitter_Bottom.enabled = false;

				ly_Bottom.preferredWidth = -1;

				txtArr[0].text = string.Empty;
				txtArr[1].text = string.Empty;

				txtArr[0].gameObject.SetActive(!string.IsNullOrEmpty(topContent));
				txtArr[1].gameObject.SetActive(!string.IsNullOrEmpty(bottomContent));

				Canvas.ForceUpdateCanvases();
				#endregion

				#region adjust ui display
				if (!string.IsNullOrEmpty(topContent))
				{
					txtArr[0].text = topContent;
					txtArr[0].alignment = topCTAnchor;

					rt_Top.sizeDelta = new Vector2(rt_Top.sizeDelta.x, mTextUIHeight);
				}

				if (!string.IsNullOrEmpty(bottomContent))
				{
					bool isShowTitle = !string.IsNullOrEmpty(topContent);
					txtArr[1].text = bottomContent;
					txtArr[1].alignment = bottomCTAnchor;

					rt_Bottom.sizeDelta = new Vector2(rt_Bottom.sizeDelta.x, mTextUIHeight);

					if (isShowTitle)
					{
						if (null != ly_Top && rt_Top.sizeDelta.x > mTipLimitWidth)
						{
							ly_Top.preferredWidth = mTipLimitWidth;
							ly_Bottom.preferredWidth = mTipLimitWidth;
						}
						else
							ly_Bottom.preferredWidth = rt_Top.sizeDelta.x;
					}
					else
					{
						if (rt_Bottom.sizeDelta.x > mTipLimitWidth)
							ly_Bottom.preferredWidth = mTipLimitWidth;
					}
				}

				size_Fitter_top.enabled = true;
				size_Fitter_Bottom.enabled = true;

				#endregion

				mIsTipShowing = true;
				mNowPosition = Vector2.zero/*Input.mousePosition*/;
				mWaitShowTime = wait;

				ClearCoroutine();

				mRoutin = StartCoroutine(DisplayUI());
			}
		}
		#endregion

		#region tip object create
		IEnumerator CreateTip(TipType tipType, AnchorPivot ac = AnchorPivot.Default)
		{
			if (!mTipDict.ContainsKey(tipType))
			{
				switch (tipType)
				{
					case TipType.Img:
						ImageTipObj();
						break;
					case TipType.Txt:
						TextTipObj();
						break;
				}
			}

			while (!mTipDict.ContainsKey(tipType))
				yield return null;

			mNowTipType = tipType;

			mPrefferedAnchor = ac;

			//tip GameObject
			mNowTipContentObj = mTipDict[mNowTipType];
			mNowTipContentObj.SetActive(true);

			//tip RectTransform
			mRect = mNowTipContentObj.GetComponent<RectTransform>();
			mRect.pivot = mRectAnchorDict[mPrefferedAnchor];

			//tip CanvasGroup
			mCanvasGroup = mNowTipContentObj.GetComponent<CanvasGroup>();
		}

		private GameObject ImageTipObj()
		{
			GameObject obj = new GameObject("ImageTip", typeof(RectTransform), typeof(Image), /*typeof(Canvas),*/ typeof(CanvasGroup), typeof(ContentSizeFitter), typeof(ImageTipScript));

			RectTransform rt = obj.GetComponent<RectTransform>();

			Image img = obj.GetComponent<Image>();
			img.type = Image.Type.Sliced;
			img.sprite = Resources.Load<Sprite>("Sprite/frame_background");
			img.raycastTarget = false;

			GameObject imgct = new GameObject("imgContent", typeof(RectTransform), typeof(Image));
			UIHelpers.InstantiateChild(rt, imgct);

			obj.GetComponent<ImageTipScript>().Img = imgct.GetComponent<Image>();

			mTipDict.Add(TipType.Img, UIHelpers.InstantiateChild(mRoot, obj));
			return obj;
		}

		private GameObject TextTipObj()
		{
			GameObject obj = new GameObject("TextTip", typeof(RectTransform), typeof(Image), /*typeof(Canvas),*/ typeof(CanvasGroup), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));

			RectTransform rt = obj.GetComponent<RectTransform>();

			Image img = obj.GetComponent<Image>();
			img.raycastTarget = false;
			img.type = Image.Type.Sliced;
			img.sprite = Resources.Load<Sprite>("Sprite/frame_background");

			VerticalLayoutGroup vg = obj.GetComponent<VerticalLayoutGroup>();
			vg.padding.left = 16;
			vg.padding.right = 16;
			vg.padding.top = 10;
			vg.padding.bottom = 16;
			vg.childAlignment = TextAnchor.MiddleCenter;
			vg.childControlWidth = true;
			vg.childControlHeight = false;
			vg.childForceExpandWidth = true;
			vg.childForceExpandHeight = false;

			ContentSizeFitter sizeFt = obj.GetComponent<ContentSizeFitter>();
			sizeFt.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
			sizeFt.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
			sizeFt.enabled = false;

			obj.GetComponent<CanvasGroup>().alpha = 0f;

			//text content
			GameObject topTxtObj = new GameObject("topTxt", typeof(RectTransform), typeof(Text), typeof(LayoutElement), typeof(ContentSizeFitter));
			GameObject bottomTxtObj = new GameObject("bottomTxt", typeof(RectTransform), typeof(Text), typeof(LayoutElement), typeof(ContentSizeFitter));
			UIHelpers.InstantiateChild(rt, topTxtObj);
			UIHelpers.InstantiateChild(rt, bottomTxtObj);

			Text tText = topTxtObj.GetComponent<Text>();
			tText.fontSize = 18;
			tText.font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;
			tText.alignment = TextAnchor.UpperLeft;
			tText.horizontalOverflow = HorizontalWrapMode.Overflow;
			tText.raycastTarget = false;

			Text bText = bottomTxtObj.GetComponent<Text>();
			bText.fontSize = 15;
			bText.font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;
			bText.alignment = TextAnchor.MiddleLeft;
			bText.raycastTarget = false;

			ContentSizeFitter topCZ = topTxtObj.GetComponent<ContentSizeFitter>();
			topCZ.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
			topCZ.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
			topCZ.enabled = false;

			ContentSizeFitter bottomCZ = bottomTxtObj.GetComponent<ContentSizeFitter>();
			bottomCZ.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
			bottomCZ.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
			bottomCZ.enabled = false;

			mTipDict.Add(TipType.Txt, UIHelpers.InstantiateChild(mRoot, obj));
			return obj;
		}

		#endregion

		private void ClearCoroutine()
		{
			if (null != mRoutin)
			{
				StopCoroutine(mRoutin);
				mRoutin = null;
			}
		}

		IEnumerator DisplayUI()
		{
			yield return new WaitForSeconds(mWaitShowTime);

			mRect.gameObject.GetComponent<ContentSizeFitter>().enabled = false;
			mRect.gameObject.GetComponent<ContentSizeFitter>().enabled = true;

			//等3個frame確保mRect的尺寸已變更
			yield return null;
			yield return null;
			yield return null;

			SetUIPosition();
			StartTweenAnimation();
		}

		public void HideTip()
		{
			mIsTipShowing = false;

			if (null != mCanvasGroup)
				mCanvasGroup.alpha = 0f;

			//reset pivot
			if (null != mRect)
				mRect.pivot = mRectAnchorDict[AnchorPivot.Default];

			if (null != mNowTipContentObj)
				mNowTipContentObj.SetActive(false);

			//if (null != mTweener)
			//	mTweener.Kill();

			if (null == mRoutin)
				return;

			StopCoroutine(mRoutin);
		}

		private void SetUIPosition()
		{
			if (Vector2.Distance(mNowPosition, Input.mousePosition) > 10f)
				mNowPosition = Input.mousePosition;

			Vector2 v;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(mRoot, mNowPosition, null, out v);
			mRect.anchoredPosition = v;

			//如果有強制設定定位點，就直接套用
			if (mPrefferedAnchor != AnchorPivot.Default)
			{
				mNowAnchorPivot = mPrefferedAnchor;
				ResetUIPivot();

				FixOutPosition();

				return;
			}

			if (CheckUIOutZone(out mNowAnchorPivot))
				ResetUIPivot();
		}

		/// <summary>ui淡入顯示</summary>
		private void StartTweenAnimation()
		{
			if (null == mCanvasGroup)
			{
				Debug.Log("No Find CanvasGroup");
				return;
			}

			mCanvasGroup.alpha = 0.0f;
			mCanvasGroup.alpha = 1.0f;

			//mTweener = m_CanvasGroup.DOFade(1f, 0.5f);
			//mTweener.SetAutoKill(false);
			//mTweener.OnComplete(OnTweenComplete);
			////mTweener.SetDelay(0.5f);
			//mTweener.Play();
		}

		private void OnTweenComplete()
		{
			//位置修正，再觀察是否適當
			if (Vector2.Distance(mNowPosition, Input.mousePosition) > 10f)
				mNowPosition = Input.mousePosition;
		}

		private void FixOutPosition()
		{
			Vector3[] mRectPointArr = new Vector3[4];
			mRect.GetWorldCorners(mRectPointArr);

			Vector3[] mParentRectPointArr = new Vector3[4];
			mRoot.GetWorldCorners(mParentRectPointArr);

			Vector3 mRPoint = mRectPointArr[2];
			Vector3 mPPoint = mParentRectPointArr[2];

			float xFix = 0f;
			float yFix = 0f;
			float plusValue = 1.25f;

			if (mRPoint.x >= mPPoint.x)
				xFix = mRPoint.x - mPPoint.x;

			if (mRPoint.y >= mPPoint.y)
				yFix = mRPoint.y - mPPoint.y;

			mNowPosition += new Vector2(-xFix * plusValue, -yFix * plusValue);

			Vector2 v;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(mRoot, mNowPosition, null, out v);
			mRect.anchoredPosition = v;
		}

		/// <summary>確認是否超出螢幕範圍</summary>
		/// <param name="anchorPivot">ui pivot</param>
		private bool CheckUIOutZone(out AnchorPivot anchorPivot)
		{
			Vector3[] mRectPointArr = new Vector3[4];
			mRect.GetWorldCorners(mRectPointArr);

			Vector3[] mParentRectPointArr = new Vector3[4];
			mRoot.GetWorldCorners(mParentRectPointArr);

			Vector3 mRPoint = mRectPointArr[2];
			Vector3 mPPoint = mParentRectPointArr[2];

			if (mRPoint.x >= mPPoint.x)
			{
				anchorPivot = (mRPoint.y >= mPPoint.y) ? AnchorPivot.TopRight : AnchorPivot.BottomRight;
				return true;
			}
			else
			{
				anchorPivot = (mRPoint.y >= mPPoint.y) ? AnchorPivot.TopLeft : AnchorPivot.BottomLeft;
				return true;
			}
		}

		private void ResetUIPivot()
		{
			mRect.pivot = mRectAnchorDict[mNowAnchorPivot];
		}
	}
}