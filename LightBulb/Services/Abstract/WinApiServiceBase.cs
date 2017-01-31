﻿#define IgnoreWinAPIErrors

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace LightBulb.Services.Abstract
{
    public abstract class WinApiServiceBase : IDisposable
    {
        #region WinAPI
        protected delegate void WinEventDelegate(
            IntPtr hWinEventHook, uint eventType, IntPtr hWnd,
            int idObject, int idChild, uint dwEventThread,
            uint dwmsEventTime);

        [DllImport("user32.dll", EntryPoint = "SetWinEventHook", SetLastError = true)]
        private static extern IntPtr SetWinEventHookInternal(
            uint eventMin, uint eventMax,
            IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc,
            uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll", EntryPoint = "UnhookWinEvent", SetLastError = true)]
        private static extern bool UnhookWinEventInternal(IntPtr hWinEventHook);
        #endregion

        private readonly Dictionary<IntPtr, WinEventDelegate> _hookHandlerDic;

        protected WinApiServiceBase()
        {
            _hookHandlerDic = new Dictionary<IntPtr, WinEventDelegate>();
        }

        protected Win32Exception GetLastError()
        {
            int errCode = Marshal.GetLastWin32Error();
            if (errCode == 0) return null;
            return new Win32Exception(errCode);
        }

        protected void CheckLogWin32Error()
        {
#if !IgnoreWinAPIErrors
            var ex = GetLastError();
            if (ex != null) Debug.WriteLine($"Win32 error: {ex.Message} ({ex.NativeErrorCode})", GetType().Name);
#endif
        }

        protected IntPtr RegisterWinEvent(
            uint eventId, WinEventDelegate handler,
            uint processId = 0, uint threadId = 0, uint flags = 0)
        {
            var handle = SetWinEventHookInternal(eventId, eventId, IntPtr.Zero, handler, processId, threadId, flags);
            if (handle == IntPtr.Zero)
            {
                CheckLogWin32Error();
                Debug.WriteLine($"Could not register WinEventHook for {eventId}", GetType().Name);
            }
            else
            {
                _hookHandlerDic.Add(handle, handler);
                //Debug.WriteLine($"Registered WinEventHook for {eventId}", GetType().Name);
            }
            return handle;
        }

        protected void UnregisterWinEvent(IntPtr handle)
        {
            if (!UnhookWinEventInternal(handle))
            {
                CheckLogWin32Error();
            }
            else
            {
                //Debug.WriteLine("Unregistered some WinEventHook", GetType().Name);
            }
            _hookHandlerDic.Remove(handle);
        }

        public virtual void Dispose()
        {
            foreach (var hook in _hookHandlerDic)
                UnregisterWinEvent(hook.Key);
        }
    }
}
