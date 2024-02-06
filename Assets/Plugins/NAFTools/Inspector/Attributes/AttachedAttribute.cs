/**
 * This file is part of the NAF-Extension, an editor extension for Unity3d.
 *
 * @link   NAF-URL
 * @author Nevin Foster
 * @since  14.06.23
 */
namespace NAF.Inspector
{
	using System;
	using UnityEngine;

	public enum MessageType
	{
		None = 0,
		Info = 1,
		Warning = 2,
		Error = 3
	}

	[AttributeUsage(System.AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
	[System.Diagnostics.Conditional("UNITY_EDITOR")]
	public class AttachedAttribute : InlineLabelAttribute, IArrayPropertyAttribute
	{
		/// <summary>
		/// If true, the component will be searched for in the children of the GameObject.
		/// </summary>
		public bool Children { get; set; } = false;

		/// <summary>
		/// If false, the component search will omit the GameObject this attribute is attached to.
		/// </summary>
		public bool Self { get; set; } = true;

		public AttachedAttribute() : this(MessageType.Error) {}

		/// <summary>
		/// </summary>
		/// <param name="type">Sets the <see cref="Icon"/> to match the message type. </param>
		public AttachedAttribute(MessageType type)
		{
			this.Tooltip ??= "Component was not found attached to " + (
				Self ? ("this GameObject" + (Children ? " nor any of its children." : ".")) :
					"any of this GameObject's children."
			);

			if (this.Icon == null)
			{
				switch (type)
				{
					case MessageType.None:
						this.Icon = null;
						break;
					case MessageType.Info:
						this.Icon = EditorIcons.Info;
						break;
					case MessageType.Warning:
						this.Icon = EditorIcons.Warning;
						break;
					case MessageType.Error:
						this.Icon = EditorIcons.Error;
						break;
				}
			}

			if (this.Alignment == LabelAlignment.BetweenLeft || this.Alignment == LabelAlignment.BetweenRight)
			{
				Debug.LogWarning("Only Left and Right alignments are supported for the AttachedAttribute.");
				this.Alignment = LabelAlignment.Left;
			}
		}
	}
}