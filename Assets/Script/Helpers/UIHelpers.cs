using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System;

namespace Gamesofa.Tank
{
	public static class UIHelpers
	{
		static Camera _mainCamera = null;
		static Camera mainCamera
		{
			get
			{
				if (null == _mainCamera || null == _mainCamera.gameObject)
					_mainCamera = Camera.main;

				return _mainCamera;
			}
		}

		/// <summary>將Prefab產生到目標GameObject上</summary>
		public static GameObject InstantiateChild(this GameObject parent, GameObject prefab)
		{
			GameObject child = GameObject.Instantiate<GameObject>(prefab);
			
			child.layer = parent.layer;
			child.transform.SetParent(parent.transform, false);

			return child;
		}

		/// <summary>將Prefab產生到目標GameObject上</summary>
		public static GameObject InstantiateChild(this Transform parent, GameObject childPrefab)
		{
			//GameObject child = GameObject.Instantiate<GameObject>(prefab);

			childPrefab.layer = parent.gameObject.layer;
			childPrefab.transform.SetParent(parent, false);

			return childPrefab;
		}

		/// <summary>將Prefab產生到目標GameObject上，並且回傳T型態物件</summary>
		public static T InstantiateChild<T>(this Transform parent, T prefab)
			where T : MonoBehaviour
		{
			T child = GameObject.Instantiate<T>(prefab);

			child.gameObject.layer = parent.gameObject.layer;
			child.transform.SetParent(parent, false);

			return child;
		}

		/// <summary>將Prefab產生到目標GameObject上，並且回傳T型態物件</summary>
		public static T InstantiateChild<T>(this Transform parent, GameObject prefab)
			where T : Component
		{
			var child = GameObject.Instantiate<GameObject>(prefab);

			child.gameObject.layer = parent.gameObject.layer;
			child.transform.SetParent(parent, false);

			T component = child.GetComponent<T>();
			if (null == component)
				Debug.LogError("InstantiateChild error, component is not exist");

			return component;
		}

		/// <summary>
		/// 把World Point轉換成Screen Point，並且將UI物件放到parent的local位置
		/// </summary>
		/// <param name="targetPos">World Point</param>
		/// <param name="rectTrans">UI物件</param>
		/// <param name="parent">UI物件的parent</param>
		/// <param name="camera">UI Camera</param>
		public static Vector3 SetWorldToScreenPos(this Vector3 targetPos, RectTransform rectTrans, RectTransform parent, Camera camera)
		{
			Vector3 screenPos = mainCamera.WorldToScreenPoint(targetPos);
			var localPos = Vector2.zero;
			if (rectTrans.parent != null &&
				RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPos, camera, out localPos))
				rectTrans.anchoredPosition = localPos;

			return localPos;
		}

		/// <summary>
		/// 將World Point轉換成UI的Local Point
		/// </summary>
		/// <param name="worldPos">World Point</param>
		/// <param name="parent">UI物件的parent</param>
		/// <param name="camera">UI Camera</param>
		/// <returns>UI物件的parent下的Local Point</returns>
		public static Vector2 WorldToLocalPos(this Vector3 worldPos, RectTransform parent, Camera camera)
		{
			Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);
			var localPos = Vector2.zero;

			RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPos, camera, out localPos);

			return localPos;
		}

		/// <summary>
		/// 將輸入的數字變換成數字陣列 (不定長)
		/// </summary>
		/// <param name="value">輸入的數字</param>
		/// <param name="digitNum">要返回至少幾位數；不夠位數時，左邊會補0</param>
		/// <returns></returns>
		public static byte[] DigitToArray(int value, int digitNum = 0)
		{
			List<byte> digitList = new List<byte>();
			if (0 == value)
			{
				digitList.Add(0);
			}
			else
			{
				for (; 0 != value; value /= 10)
					digitList.Add((byte)(value % 10));
			}

			// 不夠位數要補0
			for (int i = digitList.Count; i < digitNum; ++i)
				digitList.Add(0);

			byte[] digitArray = digitList.ToArray();
			System.Array.Reverse(digitArray);

			return digitArray;
		}

		/// <summary> 將輸入的數字變換成數字陣列 (定長, 過短補零過長省略) </summary>
		/// <param name="value">輸入的數字</param>
		/// <param name="arrayLength">要返回幾位數</param>
		/// <returns></returns>
		public static byte[] DigitToFixedArray(int value, int arrayLength)
		{
			return DigitToFixedArray(value, (byte)arrayLength);
		}

		/// <summary> 將輸入的數字變換成數字陣列 (定長, 過短補零過長省略) </summary>
		/// <param name="value">輸入的數字</param>
		/// <param name="arrayLength">要返回幾位數</param>
		/// <returns></returns>
		public static byte[] DigitToFixedArray(int value, byte arrayLength)
		{
			byte index = (byte)(arrayLength - 1);
			byte[] digitArray = new byte[arrayLength];

			if (0 != value)
			{
				for (; index < arrayLength && value != 0; value /= 10)
					digitArray[index--] =(byte)(value % 10);
			}

			for (; index < arrayLength; index--)
				digitArray[index] = 0;

			return digitArray;
		}

		
		private static void SetIcon(Image targetImg, Sprite img, bool isPreserveAspect, bool isSetNativeSzie)
		{
			targetImg.enabled = true;

			if (null != targetImg.sprite && targetImg.sprite.name == img.name)
				return;
			else
			{
				if (null == img)
				{
					targetImg.enabled = false;
					return;
				}

				targetImg.sprite = img;

				targetImg.preserveAspect = isPreserveAspect;

				if (isSetNativeSzie)
					targetImg.SetNativeSize();
			}
		}
	}
}