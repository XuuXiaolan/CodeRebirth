using UnityEngine;
using System.Collections.Generic;

namespace DunGen.Demo
{
	public class ScreenText : MonoBehaviour
	{
		#region Helper Class

		private sealed class ScreenTextData
		{
			public string Text;
			public float Timer;
		}

		#endregion

		public GUIStyle Style = new GUIStyle();
		public float MessageFadeTime = 5;

		private readonly List<ScreenTextData> messages = new List<ScreenTextData>();


		public void AddMessage(string message)
		{
			messages.Add(new ScreenTextData() { Text = message, Timer = MessageFadeTime });
		}

		private void Update()
		{
			for (int i = messages.Count - 1; i >= 0; i--)
			{
				var message = messages[i];

				if (message.Timer > 0)
				{
					message.Timer -= Time.deltaTime;

					if (message.Timer <= 0)
						messages.RemoveAt(i);
				}
			}
		}

		private void OnGUI()
		{
			Vector2 bottomRight = GUIUtility.ScreenToGUIPoint(new Vector2(Screen.width, Screen.height));
			float bufferSize = 5;

			bottomRight -= new Vector2(bufferSize, bufferSize);

			for (int i = messages.Count - 1; i >= 0; i--)
			{
				var msg = messages[i];

				GUIContent content = new GUIContent(msg.Text);
				Vector2 stringSize = GUI.skin.label.CalcSize(content);

				GUI.Label(new Rect(bottomRight.x - stringSize.x, bottomRight.y - stringSize.y, stringSize.x, stringSize.y), content, Style);
				bottomRight -= new Vector2(0, stringSize.y + bufferSize);
			}
		}


		#region Static Methods

		public static void Log(object obj)
		{
			ScreenText.Log(obj.ToString());
		}

		public static void Log(string format, params object[] args)
		{
			var component = Component.FindObjectOfType<ScreenText>();

			if (component == null)
				return;

			component.AddMessage(string.Format(format, args));
		}

		#endregion
	}
}