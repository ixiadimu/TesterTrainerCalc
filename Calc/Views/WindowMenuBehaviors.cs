using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Interactivity;

namespace Calc.Views
{

	public enum WindowStyleFlag
	{
		WS_SYSMENU = 0x80000,
		WS_MINIMIZEBOX = 0x20000,
		WS_MAXIMIZEBOX = 0x10000,
	}

	class WindowMenuBehaviors : Behavior<Window>
	{
		const int GWL_STYLE = -16;

		[DllImport("user32")]
		private static extern uint GetWindowLong(IntPtr hWnd, int index);

		[DllImport("user32")]
		private static extern uint SetWindowLong(IntPtr hWnd, int index, WindowStyleFlag dwLong);


		public static readonly DependencyProperty MinimizeBoxProperty =
			DependencyProperty.RegisterAttached("MinimizeBox", typeof(bool), typeof(WindowMenuBehaviors), new PropertyMetadata(true, new PropertyChangedCallback(SourceInitialized)));

		public static readonly DependencyProperty MaximizeBoxProperty =
			DependencyProperty.RegisterAttached("MaximizeBox", typeof(bool), typeof(WindowMenuBehaviors), new PropertyMetadata(true, new PropertyChangedCallback(SourceInitialized)));

		public static readonly DependencyProperty ControlBoxProperty =
			DependencyProperty.RegisterAttached("ControlBox", typeof(bool), typeof(WindowMenuBehaviors), new PropertyMetadata(true, new PropertyChangedCallback(SourceInitialized)));

		/// <summary>
		/// 最小化ボタン
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		[AttachedPropertyBrowsableForType(typeof(WindowMenuBehaviors))]
		public static bool GetMinimizeBox(DependencyObject obj)
		{
			return (bool)obj.GetValue(MinimizeBoxProperty);
		}
		[AttachedPropertyBrowsableForType(typeof(WindowMenuBehaviors))]
		public static void SetMinimizeBox(DependencyObject obj, bool value)
		{
			obj.SetValue(MinimizeBoxProperty, value);
		}

		/// <summary>
		/// 最大化ボタン
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		[AttachedPropertyBrowsableForType(typeof(WindowMenuBehaviors))]
		public static bool GetMaximizeBox(DependencyObject obj)
		{
			return (bool)obj.GetValue(MaximizeBoxProperty);
		}
		[AttachedPropertyBrowsableForType(typeof(WindowMenuBehaviors))]
		public static void SetMaximizeBox(DependencyObject obj, bool value)
		{
			obj.SetValue(MaximizeBoxProperty, value);
		}

		/// <summary>
		/// システムメニュー
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		[AttachedPropertyBrowsableForType(typeof(WindowMenuBehaviors))]
		public static bool GetControlBox(DependencyObject obj)
		{
			return (bool)obj.GetValue(ControlBoxProperty);
		}
		[AttachedPropertyBrowsableForType(typeof(WindowMenuBehaviors))]
		public static void SetControlBox(DependencyObject obj, bool value)
		{
			obj.SetValue(ControlBoxProperty, value);
		}

		private static void SourceInitialized(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			var window = sender as Window;
			if (window == null) return;

			Livet.DispatcherHelper.UIDispatcher.Invoke(new Action(() =>
			{
				IntPtr handle = new WindowInteropHelper(window).EnsureHandle();
				var original = (WindowStyleFlag)GetWindowLong(handle, GWL_STYLE);
				var current = GetWindowStyle(window, original, e);
				if (original != current) {
					SetWindowLong(handle, GWL_STYLE, current);
				}
			}));
		}

		private static WindowStyleFlag GetWindowStyle(DependencyObject obj, WindowStyleFlag windowStyle, DependencyPropertyChangedEventArgs ex)
		{
			var style = windowStyle;

			switch (ex.Property.Name) {
			case "MinimizeBox":
				if ((bool)obj.GetValue(MinimizeBoxProperty)) {
					style |= WindowStyleFlag.WS_MINIMIZEBOX;
				} else {
					style ^= WindowStyleFlag.WS_MINIMIZEBOX;
				}
				break;
			case "MaximizeBox":
				if ((bool)obj.GetValue(MaximizeBoxProperty)) {
					style |= WindowStyleFlag.WS_MAXIMIZEBOX;
				} else {
					style ^= WindowStyleFlag.WS_MAXIMIZEBOX;
				}
				break;
			case "ControlBox":
				if ((bool)obj.GetValue(ControlBoxProperty)) {
					style |= WindowStyleFlag.WS_SYSMENU;
				} else {
					style ^= WindowStyleFlag.WS_SYSMENU;
				}
				break;
			}
			return style;
		}
	}
}



