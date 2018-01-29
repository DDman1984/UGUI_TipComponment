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
	public class CommonTipUI : MonoBehaviour //, IUpdate
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

			Default,	//左下定位點
			UnKnow,
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

		public static CommonTipUI Instance = null;

		public static bool IsTipShowing = false;

		/// <summary>Root</summary>
		private RectTransform m_Root = null;

		/// <summary>Tip ui rect</summary>
		private RectTransform m_Rect = null;

		private CanvasGroup m_CanvasGroup = null;

		private static TipType mNowTipType;

		/// <summary>在同一個ui元件停佇幾秒後顯示</summary>
		private static  float mWaitShowTime = 1f;

		private static Vector2 mNowPosition = Vector2.zero;

		private AnchorPivot mNowAnchorPivot;

		private static AnchorPivot mPrefferedAnchor = AnchorPivot.UnKnow;

		private PointerEventData mNowPointData = null;

		private GameObject mNowTipContentObj = null;

		private Coroutine mRoutin = null;

		//private Tweener mTweener = null;

		//private Camera mRootCamera = null;

		private static Dictionary<TipType, GameObject> mTipDict = new Dictionary<TipType, GameObject>();

		private LayoutElement mSimpleTipTitleElement = null;
		private LayoutElement mSimpleTipContentElement = null;
		private ContentSizeFitter mCt = null;

		private float mTipLimitWidth = 400f;

		private RectTransform rt_1, rt_2;
		private Text[] txtArr;

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
				GameObject obj = new GameObject("TipManager", typeof(CommonTipUI));
				DontDestroyOnLoad(obj);
			}
		}
		/// <summary>Image Tip Content</summary>
		/// <param name="root"></param>
		/// <param name="assetImageId"></param>
		public static void Show(RectTransform root, string assetImageId, AnchorPivot preferredAnchor = AnchorPivot.UnKnow)
		{
			if (null == Instance)
				Initialize();

			mNowTipType = TipType.Img;

			if (!mTipDict.ContainsKey(mNowTipType))
			{
				GameObject tipObj = Resources.Load(string.Format("{0}_prefab", mNowTipType.ToString().ToLower())) as GameObject;
				mTipDict.Add(mNowTipType, UIHelpers.InstantiateChild(root, tipObj));
			}

			mPrefferedAnchor = preferredAnchor;

			Instance.ShowTip(assetImageId, 0f, preferredAnchor);
		}

		/// <summary>Text Tip Content</summary>
		/// <param name="root"></param>
		/// <param name="topContent"></param>
		/// <param name="bottomContent"></param>
		public static void Show(RectTransform root, string topContent = "", string bottomContent = "", AnchorPivot preferredAnchor = AnchorPivot.UnKnow)
		{
			if (null == Instance)
				Initialize();

			mNowTipType = TipType.Txt;

			if (!mTipDict.ContainsKey(mNowTipType))
			{
				GameObject tipObj = Resources.Load(string.Format("{0}_prefab", mNowTipType.ToString().ToLower())) as GameObject;
				mTipDict.Add(mNowTipType, UIHelpers.InstantiateChild(root, tipObj));
			}

			mPrefferedAnchor = preferredAnchor;
		}

		#endregion


		#region public function
		public GameObject InitialCustomTipUI(TipType tipType, AnchorPivot preferredAnchor = AnchorPivot.UnKnow)
		{
			GameObject tipObj = CreateTip(tipType);

			if (null != tipObj && preferredAnchor != AnchorPivot.UnKnow)
				mPrefferedAnchor = preferredAnchor;

			return tipObj;
		}

		public GameObject InitialCustomTipUI<T>(TipType tipType, object[] content, AnchorPivot preferredAnchor = AnchorPivot.UnKnow) where T : TipContentScript
		{
			GameObject tipObj = CreateTip(tipType);

			if (null != tipObj)
			{
				if (preferredAnchor != AnchorPivot.UnKnow)
					mPrefferedAnchor = preferredAnchor;

				T t = tipObj.GetComponent<T>();

				if (t != null)
					t.SetTipContent(content);
			}

			return tipObj;
		}

		/// <summary>圖像tip</summary>
		/// <param name="imgAssetId"></param>
		/// <param name="wait"></param>
		/// <param name="preferredAnchor"></param>
		private void ShowTip(string imgAssetId, float wait = 0.0f, AnchorPivot preferredAnchor = AnchorPivot.UnKnow)
		{
			GameObject tipObj = mTipDict[TipType.Img];

			if (null != tipObj)
			{
				ImageTipScript img = tipObj.GetComponent<ImageTipScript>();

				if (null == img)
					img = tipObj.AddComponent<ImageTipScript>();

				//mRootCamera = null;

				IsTipShowing = true;
				mNowPosition = Input.mousePosition;
				mWaitShowTime = wait;

				img.SetImg(imgAssetId, WaitForAssetLoading);
			}
		}

		private void WaitForAssetLoading()
		{
			if (null != mRoutin)
			{
				StopCoroutine(mRoutin);
				mRoutin = null;
			}

			mRoutin = StartCoroutine(DisplayUI());
		}

		/// <summary>文字tip</summary>
		/// <param name="title"></param>
		/// <param name="contentStr"></param>
		/// <param name="wait"></param>
		/// <param name="preferredAnchor"></param>
		/// <param name="titleAnchor"></param>
		/// <param name="contentAnchor"></param>
		public void ShowTip(string title, string contentStr = "", float wait = 0.0f, AnchorPivot preferredAnchor = AnchorPivot.UnKnow, TextAnchor titleAnchor = TextAnchor.UpperLeft, TextAnchor contentAnchor = TextAnchor.UpperLeft)
		{
			mNowTipType = TipType.Txt;

			GameObject tipObj = CreateTip(TipType.Txt);
			if (null != tipObj)
			{
				if (preferredAnchor != AnchorPivot.UnKnow)
					mPrefferedAnchor = preferredAnchor;

				txtArr = tipObj.GetComponentsInChildren<Text>(true);

				RectTransform rt = tipObj.GetComponent<RectTransform>();
				rt_1 = null;
				rt_2 = null;

				if (null != txtArr[0])
				{
					rt_1 = txtArr[0].GetComponent<RectTransform>();

					if (null == mSimpleTipTitleElement)
						mSimpleTipTitleElement = txtArr[0].gameObject.GetComponent<LayoutElement>();

					if (null == mCt)
						mCt = txtArr[0].gameObject.GetComponent<ContentSizeFitter>();
				}

				if (null != txtArr[1])
				{
					rt_2 = txtArr[1].GetComponent<RectTransform>();

					if (null == mSimpleTipContentElement)
						mSimpleTipContentElement = txtArr[1].gameObject.GetComponent<LayoutElement>();
				}
				mCt.enabled = false;
				mSimpleTipTitleElement.preferredWidth = -1;
				mSimpleTipContentElement.preferredWidth = -1;

				bool showTitle = (false == string.IsNullOrEmpty(title));
				bool showContent = (false == string.IsNullOrEmpty(contentStr));

				txtArr[0].text = string.Empty;
				txtArr[1].text = string.Empty;

				Canvas.ForceUpdateCanvases();

				if (showTitle)
				{
					txtArr[0].text = title;
					txtArr[0].alignment = titleAnchor;
				}

				if (showContent)
				{
					txtArr[1].text = contentStr;
					txtArr[1].alignment = contentAnchor;
				}

				txtArr[0].gameObject.SetActive(showTitle);
				txtArr[1].gameObject.SetActive(showContent);


				IsTipShowing = true;
				mNowPosition = Input.mousePosition;
				mWaitShowTime = wait;

				if (null != mRoutin)
				{
					StopCoroutine(mRoutin);
					mRoutin = null;
				}

				mRoutin = StartCoroutine(DisplayUI());
			}
		}

		private void ResetUIElement()
		{
			bool showTitle = (txtArr[0].text != string.Empty);
			bool showContent = (txtArr[1].text != string.Empty);

			rt_1.sizeDelta = new Vector2(rt_1.sizeDelta.x, (!showTitle) ? 0f : 24f);
			rt_2.sizeDelta = new Vector2(rt_2.sizeDelta.x, (!showContent) ? 0f : 24f);

			//依據狀況設定layoutElement的參數
			if (null != rt_2 && null != mSimpleTipContentElement)
			{
				if (!showContent)
					rt_2.sizeDelta = Vector2.zero;
				else
				{
					if (showTitle)
					{
						if (null != mSimpleTipTitleElement && rt_1.sizeDelta.x > mTipLimitWidth)
						{
							mCt.enabled = true;
							mSimpleTipTitleElement.preferredWidth = mTipLimitWidth;
							mSimpleTipContentElement.preferredWidth = mTipLimitWidth;
						}
						else
							mSimpleTipContentElement.preferredWidth = rt_1.sizeDelta.x;
					}
					else
					{
						if (rt_2.sizeDelta.x > mTipLimitWidth)
							mSimpleTipContentElement.preferredWidth = mTipLimitWidth;
					}
				}
			}
		}

		IEnumerator DisplayUI()
		{
			yield return new WaitForSeconds(mWaitShowTime);

			m_Rect.gameObject.GetComponent<ContentSizeFitter>().enabled = false;
			m_Rect.gameObject.GetComponent<ContentSizeFitter>().enabled = true;

			if (mNowTipType == TipType.Txt)
				ResetUIElement();

			//等3個frame確保m_Rect的尺寸已變更
			yield return null;
			yield return null;
			yield return null;

			SetUIPosition();
			StartTweenAnimation();
		}

		public void HideTip()
		{
			IsTipShowing = false;

			if (null != m_CanvasGroup)
				m_CanvasGroup.alpha = 0f;

			//reset pivot
			if (null != m_Rect)
				m_Rect.pivot = mRectAnchorDict[AnchorPivot.Default];

			if (null != mNowTipContentObj)
				mNowTipContentObj.SetActive(false);

			//if (null != mTweener)
			//	mTweener.Kill();

			if (null != mSimpleTipContentElement)
				mSimpleTipContentElement.preferredWidth = -1;

			if (null == mRoutin)
				return;

			StopCoroutine(mRoutin);
		}

		#endregion

		private GameObject CreateTip(TipType tipType, AnchorPivot ac = AnchorPivot.Default)
		{
			if (tipType == TipType.Unknow)
				return null;

			if (!mTipDict.ContainsKey(tipType))
			{
				GameObject tipObj = Resources.Load(string.Format("{0}_prefab", tipType.ToString().ToLower())) as GameObject;
				mTipDict.Add(tipType, UIHelpers.InstantiateChild(m_Root, tipObj));
			}

			mNowTipContentObj = mTipDict[tipType];
			mNowTipContentObj.SetActive(true);

			m_Rect = mNowTipContentObj.GetComponent<RectTransform>();
			m_Rect.pivot = mRectAnchorDict[ac];
			m_CanvasGroup = mNowTipContentObj.GetComponent<CanvasGroup>();

			return mNowTipContentObj;
		}

		private void SetUIPosition()
		{
			if (Vector2.Distance(mNowPosition, Input.mousePosition) > 10f)
				mNowPosition = Input.mousePosition;

			Vector2 v;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(m_Root, mNowPosition, null, out v);
			m_Rect.anchoredPosition = v;

			//如果有強制設定定位點，就直接套用
			if (mPrefferedAnchor != AnchorPivot.UnKnow)
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
			if (null == m_CanvasGroup)
			{
				Debug.Log("No Find CanvasGroup");
				return;
			}

			m_CanvasGroup.alpha = 0.0f;
			m_CanvasGroup.alpha = 1.0f;

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
			m_Rect.GetWorldCorners(mRectPointArr);

			Vector3[] mParentRectPointArr = new Vector3[4];
			m_Root.GetWorldCorners(mParentRectPointArr);

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
			RectTransformUtility.ScreenPointToLocalPointInRectangle(m_Root, mNowPosition, null, out v);
			m_Rect.anchoredPosition = v;
		}

		/// <summary>確認是否超出螢幕範圍</summary>
		/// <param name="anchorPivot">ui pivot</param>
		private bool CheckUIOutZone(out AnchorPivot anchorPivot)
		{
			Vector3[] mRectPointArr = new Vector3[4];
			m_Rect.GetWorldCorners(mRectPointArr);

			Vector3[] mParentRectPointArr = new Vector3[4];
			m_Root.GetWorldCorners(mParentRectPointArr);

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
			m_Rect.pivot = mRectAnchorDict[mNowAnchorPivot];
		}
	}
}