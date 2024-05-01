using DunGen.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace DunGen.Editor.Validation
{
	public sealed class DungeonValidator
	{
		public static readonly DungeonValidator Instance = new DungeonValidator();

		#region Nested Types

		public enum MessageType
		{
			Warning,
			Error,
		}

		public sealed class Message
		{
			public MessageType Type { get; private set; }
			public string Text { get; private set; }
			public UnityEngine.Object Context { get; private set; }
			public int Count { get; set; }


			public Message(MessageType messageType, string text, UnityEngine.Object context = null)
			{
				Type = messageType;
				Text = text;
				Context = context;
				Count = 1;
			}
		}

		#endregion

		private readonly List<IValidationRule> rules = new List<IValidationRule>();
		private readonly List<Message> messages = new List<Message>();


		public DungeonValidator()
		{
			var ruleTypes = Assembly.GetExecutingAssembly().GetTypes()
							.Where(t => typeof(IValidationRule).IsAssignableFrom(t) && !t.IsAbstract);

			foreach (var type in ruleTypes)
				rules.Add((IValidationRule)Activator.CreateInstance(type));
		}

		public bool Validate(DungeonFlow dungeonFlow)
		{
			messages.Clear();

			foreach (var rule in rules)
				rule.Validate(dungeonFlow, this);

			PrintMessages(dungeonFlow);
			return !messages.Any(m => m.Type == MessageType.Error);
		}

		#region AddMessage Helpers

		public void AddWarning(string format, params object[] args)
		{
			AddMessage(MessageType.Warning, string.Format(format, args));
		}

		public void AddWarning(string format, UnityEngine.Object context, params object[] args)
		{
			AddMessage(MessageType.Warning, string.Format(format, args), context);
		}

		public void AddWarning(string text, UnityEngine.Object context = null)
		{
			AddMessage(MessageType.Warning, text, context);
		}

		public void AddError(string format, params object[] args)
		{
			AddMessage(MessageType.Error, string.Format(format, args));
		}

		public void AddError(string format, UnityEngine.Object context = null, params object[] args)
		{
			AddMessage(MessageType.Error, string.Format(format, args), context);
		}

		public void AddError(string text, UnityEngine.Object context = null)
		{
			AddMessage(MessageType.Error, text, context);
		}

		#endregion

		private void AddMessage(MessageType type, string text, UnityEngine.Object context = null)
		{
			foreach (var message in messages)
			{
				if (message.Type == type &&
					message.Text == text &&
					message.Context == context)
				{
					message.Count++;
					return;
				}
			}

			messages.Add(new Message(type, text, context));
		}

		private void PrintMessages(UnityEngine.Object defaultContext)
		{
			var orderedMessages = messages.OrderByDescending(m => m.Type);

			if (orderedMessages.Any())
			{
				foreach (var message in orderedMessages)
				{
					string text = (message.Count < 2) ? message.Text : message.Text + " (x" + message.Count + ")";
					var context = (message.Context == null) ? defaultContext : message.Context;

					switch (message.Type)
					{
						case MessageType.Warning:
							Debug.LogWarning(text, context);
							break;

						case MessageType.Error:
							Debug.LogError(text, context);
							break;

						default:
							throw new Exception(typeof(MessageType).Name + "." + message.Type + " is not implemented");
					}
				}
			}
			else
			{
				Debug.Log("No issues were found in the dungeon flow");
			}
		}
	}
}
