using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;


// Based on http://www.codeproject.com/info/cpol10.aspx
namespace MonitorSwitcherGUI
{
	/// <summary>
	/// A class that manages a global low level keyboard hook
	/// </summary>
	class GlobalKeyboardHook {
		#region Constant, Structure and Delegate Definitions
		/// <summary>
		/// defines the callback type for the hook
		/// </summary>
		public delegate int keyboardHookProc(int code, int wParam, ref keyboardHookStruct lParam);

		public struct keyboardHookStruct {
			public int vkCode;
			public int scanCode;
			public int flags;
			public int time;
			public int dwExtraInfo;
		}

		const int WH_KEYBOARD_LL = 13;
		const int WM_KEYDOWN = 0x100;
		const int WM_KEYUP = 0x101;
		const int WM_SYSKEYDOWN = 0x104;
		const int WM_SYSKEYUP = 0x105;
		#endregion

		#region Instance Variables
		/// <summary>
		/// The collections of keys to watch for
		/// </summary>
		public List<Hotkey> HookedKeys = new List<Hotkey>();
		/// <summary>
		/// Handle to the hook, need this to unhook and call the next hook
		/// </summary>
		IntPtr hhook = IntPtr.Zero;
        keyboardHookProc hookDelegate;
		#endregion

		#region Events
		/// <summary>
		/// Occurs when one of the hooked keys is pressed
		/// </summary>
		//public event KeyEventHandler KeyDown;
		/// <summary>
		/// Occurs when one of the hooked keys is released
		/// </summary>
		public event KeyEventHandler KeyUp;
		#endregion

		#region Constructors and Destructors
		/// <summary>
		/// Initializes a new instance of the <see cref="GlobalKeyboardHook"/> class and installs the keyboard hook.
		/// </summary>
		public GlobalKeyboardHook() {
            hookDelegate = hookProc;
			hook();
		}

		/// <summary>
		/// Releases unmanaged resources and performs other cleanup operations before the
		/// <see cref="GlobalKeyboardHook"/> is reclaimed by garbage collection and uninstalls the keyboard hook.
		/// </summary>
		~GlobalKeyboardHook() {
			unhook();
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Installs the global hook
		/// </summary>
		public void hook() {
			IntPtr hInstance = LoadLibrary("User32");
            hhook = SetWindowsHookEx(WH_KEYBOARD_LL, hookDelegate, hInstance, 0);
		}

		/// <summary>
		/// Uninstalls the global hook
		/// </summary>
		public void unhook() {
			UnhookWindowsHookEx(hhook);
		}

        /// <summary> 
        /// Checks whether Alt, Shift, Control or CapsLock 
        /// is pressed at the same time as the hooked key. 
        /// Modifies the keyCode to include the pressed keys. 
        /// </summary> 
        private Keys AddModifiers(Keys key) 
        { 
            //CapsLock 
            if ((GetKeyState(VK_CAPITAL) & 0x0001) != 0) key = key | Keys.CapsLock; 
            
            //Shift 
            if ((GetKeyState(VK_SHIFT) & 0x8000) != 0) key = key | Keys.Shift; 
            
            //Ctrl 
            if ((GetKeyState(VK_CONTROL) & 0x8000) != 0) key = key | Keys.Control; 
            
            //Alt 
            if ((GetKeyState(VK_MENU) & 0x8000) != 0) key = key | Keys.Alt; 
            
            return key; 
        }

		/// <summary>
		/// The callback for the keyboard hook
		/// </summary>
		/// <param name="code">The hook code, if it isn't >= 0, the function shouldn't do anyting</param>
		/// <param name="wParam">The event type</param>
		/// <param name="lParam">The keyhook event information</param>
		/// <returns></returns>
		public int hookProc(int code, int wParam, ref keyboardHookStruct lParam) {
			if (code >= 0) {
				Keys key = (Keys)lParam.vkCode;
                foreach (Hotkey hotkey in HookedKeys) {
                    if (hotkey.Key == key)
                    {
                        //Get modifiers 
                        key = AddModifiers(key);
                        
                        KeyEventArgs kea = new KeyEventArgs(key);
                        //if ((wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN) && (KeyDown != null))
                        if ((wParam == WM_KEYUP || wParam == WM_SYSKEYUP) && (KeyUp != null))
                        {
                            if ((hotkey.Alt == kea.Alt) && (hotkey.Shift == kea.Shift) && (hotkey.Ctrl == kea.Control))
                            {
                                //KeyDown(this, kea);
                                KeyUp(hotkey, kea);
                            }                                                    
                        }

                        if (kea.Handled)
                            return 1;
                    }
				}
			}
			return CallNextHookEx(hhook, code, wParam, ref lParam);
		}
		#endregion

		#region DLL imports
		/// <summary>
		/// Sets the windows hook, do the desired event, one of hInstance or threadId must be non-null
		/// </summary>
		/// <param name="idHook">The id of the event you want to hook</param>
		/// <param name="callback">The callback.</param>
		/// <param name="hInstance">The handle you want to attach the event to, can be null</param>
		/// <param name="threadId">The thread you want to attach the event to, can be null</param>
		/// <returns>a handle to the desired hook</returns>
		[DllImport("user32.dll")]
		static extern IntPtr SetWindowsHookEx(int idHook, keyboardHookProc callback, IntPtr hInstance, uint threadId);

		/// <summary>
		/// Unhooks the windows hook.
		/// </summary>
		/// <param name="hInstance">The hook handle that was returned from SetWindowsHookEx</param>
		/// <returns>True if successful, false otherwise</returns>
		[DllImport("user32.dll")]
		static extern bool UnhookWindowsHookEx(IntPtr hInstance);

		/// <summary>
		/// Calls the next hook.
		/// </summary>
		/// <param name="idHook">The hook id</param>
		/// <param name="nCode">The hook code</param>
		/// <param name="wParam">The wparam.</param>
		/// <param name="lParam">The lparam.</param>
		/// <returns></returns>
		[DllImport("user32.dll")]
		static extern int CallNextHookEx(IntPtr idHook, int nCode, int wParam, ref keyboardHookStruct lParam);

		/// <summary>
		/// Loads the library.
		/// </summary>
		/// <param name="lpFileName">Name of the library</param>
		/// <returns>A handle to the library</returns>
		[DllImport("kernel32.dll")]
		static extern IntPtr LoadLibrary(string lpFileName);

        /// <summary> 
        /// Gets the state of modifier keys for a given keycode.
        /// </summary> 
        /// <param name="keyCode">The keyCode</param> 
        /// <returns></returns> 
        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true, CallingConvention = CallingConvention.Winapi)] 
        public static extern short GetKeyState(int keyCode); 
        //Modifier key vkCode constants
        private const int VK_SHIFT = 0x10; 
        private const int VK_CONTROL = 0x11; 
        private const int VK_MENU = 0x12; 
        private const int VK_CAPITAL = 0x14;

		#endregion
	}
}
