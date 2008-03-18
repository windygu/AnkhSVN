﻿//***************************************************************************
//
//    Copyright (c) Microsoft Corporation. All rights reserved.
//    This code is licensed under the Visual Studio SDK license terms.
//    THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
//    ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
//    IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
//    PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//***************************************************************************

using System;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace Ankh.UI.PendingChanges
{
    /// <summary>
    /// This class is used to implement CodeEditorUserControl
    /// </summary>
    /// <seealso cref="UserControl"/>
    public partial class LogMessageEditor : UserControl
    {
        private CodeEditorNativeWindow codeEditorNativeWindow;

        #region Methods

        /// <summary>
        /// Creation and initialization of <see cref="CodeEditorNativeWindow"/> class.
        /// </summary>
        /// <param name="serviceProvider">The IOleServiceProvider interface is a generic access mechanism to locate a globally unique identifier (GUID) identified service.</param>
        [CLSCompliant(false)]
        public void Init(IOleServiceProvider serviceProvider)
        {
            codeEditorNativeWindow = new CodeEditorNativeWindow();
            codeEditorNativeWindow.Init(serviceProvider, this);
            codeEditorNativeWindow.Area = this.ClientRectangle;
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    if (codeEditorNativeWindow != null)
                    {
                        codeEditorNativeWindow.Dispose();
                    }
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }


        /// <summary>
        /// Determines whether the specified key is a
        /// regular input key or a special key that requires preprocessing.        
        /// </summary>
        /// <param name="keyData">Specifies key codes and modifiers</param>
        /// <returns>Always returns true</returns>
        protected override bool IsInputKey(Keys keyData)
        {
            // Since we process each pressed keystroke, the return value is always true.
            return true;
        }

        /// <summary>
        /// Overrides OnGotFocus method to handle OnGotFocus event
        /// </summary>
        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            if (codeEditorNativeWindow != null)
            {
                codeEditorNativeWindow.Focus();
            }
        }

        /// <summary>
        /// Overrides OnSizeChanged method to handle OnSizeChanged event
        /// </summary>
        protected override void OnSizeChanged(EventArgs e)
        {
            if (codeEditorNativeWindow != null)
            {
                codeEditorNativeWindow.Area = this.ClientRectangle;
            }
        }

        #endregion
    }

    /// <summary>
    /// This class inherits from NativeWindow class, that provides a low-level encapsulation of a window handle and a window procedure
    /// </summary>
    /// <seealso cref="NativeWindow"/>
    internal class CodeEditorNativeWindow : NativeWindow, System.Windows.Forms.IMessageFilter, IOleCommandTarget, IDisposable
    {
        #region Fields

        /// <summary>
        /// Editor window handle
        /// </summary>
        private IntPtr editorHwnd;

        /// <summary>
        /// Command window handle
        /// </summary>
        private IntPtr commandHwnd;

        /// <summary>
        /// The IOleCommandTarget interface enables objects and their containers to dispatch commands to each other.
        /// For example, an object's toolbars may contain buttons for commands such as Print, Print Preview, Save, New, and Zoom. 
        /// </summary>
        private IOleCommandTarget commandTarget;

        /// <summary>
        /// Service provider
        /// </summary>
        private IOleServiceProvider serviceProvider;

        /// <summary>
        /// priority command target cookie
        /// </summary>
        private uint priorityCommandTargetCookie;

        /// <summary>
        /// Reference to VsCodeWindow object
        /// </summary>
        private IVsCodeWindow codeWindow;

        #endregion

        #region Properties

        /// <summary>
        /// Determines editor's window placement
        /// </summary>
        public Rectangle Area
        {
            set
            {
                if (editorHwnd != IntPtr.Zero)
                {
                    NativeMethods.SetWindowPos(editorHwnd, IntPtr.Zero, value.X, value.Y,
                        value.Width, value.Height, 0x04);
                }
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Used to create Code Window
        /// </summary>
        /// <param name="parentHandle">Parent window handle</param>
        /// <param name="codeWindow">Represents a multiple-document interface (MDI) child that contains one or more code views.</param>
        private void CreateCodeWindow(IntPtr parentHandle, out IVsCodeWindow codeWindow)
        {
            ILocalRegistry localRegistry = QueryService<ILocalRegistry>(typeof(SLocalRegistry));

            // create code window
            Guid guidVsCodeWindow = typeof(VsCodeWindowClass).GUID;
            codeWindow = CreateObject(localRegistry, guidVsCodeWindow, typeof(IVsCodeWindow).GUID) as IVsCodeWindow;

            // initialize code window
            INITVIEW[] initView = new INITVIEW[1];
            initView[0].fSelectionMargin = 0;
            initView[0].IndentStyle = vsIndentStyle.vsIndentStyleSmart;
            initView[0].fWidgetMargin = 0;
            initView[0].fVirtualSpace = 0;
            initView[0].fDragDropMove = 1;
            initView[0].fVisibleWhitespace = 0;

            IVsCodeWindowEx codeWindowEx = codeWindow as IVsCodeWindowEx;
            int hr = codeWindowEx.Initialize((uint)_codewindowbehaviorflags.CWB_DISABLEDROPDOWNBAR |
                (uint)_codewindowbehaviorflags.CWB_DISABLESPLITTER,
                0, null, null,
                (int)TextViewInitFlags.VIF_SET_WIDGET_MARGIN |
                (int)TextViewInitFlags.VIF_SET_SELECTION_MARGIN |
                (int)TextViewInitFlags2.VIF_ACTIVEINMODALSTATE |
                (int)TextViewInitFlags2.VIF_SUPPRESSBORDER |
                (int)TextViewInitFlags2.VIF_SUPPRESS_STATUS_BAR_UPDATE |
                (int)TextViewInitFlags2.VIF_SUPPRESSTRACKCHANGES,
                initView);

            if (hr != VSConstants.S_OK)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            // set buffer
            Guid guidVsTextBuffer = typeof(VsTextBufferClass).GUID;
            IVsTextBuffer textBuffer = CreateObject(localRegistry, guidVsTextBuffer, typeof(IVsTextBuffer).GUID) as IVsTextBuffer;
            Guid CLSID_LogMessageService = typeof(LogMessageLanguageService).GUID;

            hr = textBuffer.SetLanguageServiceID(ref CLSID_LogMessageService);
            if (hr != VSConstants.S_OK)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            hr = codeWindow.SetBuffer(textBuffer as IVsTextLines);
            if (hr != VSConstants.S_OK)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            // create pane window
            IVsWindowPane windowPane = codeWindow as IVsWindowPane;

            hr = windowPane.SetSite(serviceProvider);
            if (hr != VSConstants.S_OK)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            hr = windowPane.CreatePaneWindow(parentHandle, 10, 10, 50, 50, out editorHwnd);
            if (hr != VSConstants.S_OK)
            {
                Marshal.ThrowExceptionForHR(hr);
            }
        }

        /// <summary>
        /// Creates an object
        /// </summary>
        /// <param name="localRegistry">Establishes a locally-registered COM object relative to the local Visual Studio registry hive</param>
        /// <param name="clsid">GUID if object to be created</param>
        /// <param name="iid">GUID assotiated with specified System.Type</param>
        /// <returns>An object</returns>
        private object CreateObject(ILocalRegistry localRegistry, Guid clsid, Guid iid)
        {
            object objectInstance;
            IntPtr unknown = IntPtr.Zero;

            int hr = localRegistry.CreateInstance(clsid, null, ref iid, (uint)CLSCTX.CLSCTX_INPROC_SERVER, out unknown);

            if (hr != VSConstants.S_OK)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            try
            {
                objectInstance = Marshal.GetObjectForIUnknown(unknown);
            }
            finally
            {
                if (unknown != IntPtr.Zero)
                {
                    Marshal.Release(unknown);
                }
            }

            // Try to site object instance
            IObjectWithSite objectWithSite = objectInstance as IObjectWithSite;
            if (objectWithSite != null)
                objectWithSite.SetSite(serviceProvider);

            return objectInstance;
        }

        /// <summary>
        /// Sets focus to Editor's Window
        /// </summary>
        public void Focus()
        {
            NativeMethods.SetFocus(editorHwnd);
        }

        /// <summary>
        /// Window initialization
        /// </summary>
        /// <param name="serviceProvider">IOleServiceProvider</param>
        /// <param name="parent">Control, that can be used to create other controls</param>
        public void Init(IOleServiceProvider serviceProvider, UserControl parent)
        {
            // Store service provider
            this.serviceProvider = serviceProvider;

            //Create window            
            CreateCodeWindow(parent.Handle, out codeWindow);
            commandTarget = codeWindow as IOleCommandTarget;

            IVsTextView textView;
            int hr = codeWindow.GetPrimaryView(out textView);

            if (hr != VSConstants.S_OK)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            commandHwnd = textView.GetWindowHandle();

            //Assign a handle to this window
            AssignHandle(commandHwnd);
            NativeMethods.ShowWindow(editorHwnd, 1);

            //Register priority command target
            IVsRegisterPriorityCommandTarget register = QueryService<IVsRegisterPriorityCommandTarget>(typeof(SVsRegisterPriorityCommandTarget));
            hr = register.RegisterPriorityCommandTarget(0, (IOleCommandTarget)this, out priorityCommandTargetCookie);

            if (hr != VSConstants.S_OK)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            //Add message filter
            Application.AddMessageFilter((System.Windows.Forms.IMessageFilter)this);
        }

        /// <summary>
        ///  This method is used to get service of specified type
        /// </summary>
        /// <typeparam name="InterfaceType">A name of the interface which the caller wishes to receive for the service</typeparam>
        /// <param name="serviceType">A name of the requested service</param>
        /// <returns></returns>
        private InterfaceType QueryService<InterfaceType>(Type serviceType)
            where InterfaceType : class
        {
            Guid serviceGuid = serviceType.GUID;
            Guid interfaceGuid = typeof(InterfaceType).GUID;

            IntPtr unknown = IntPtr.Zero;

            InterfaceType service = null;

            int hr = serviceProvider.QueryService(ref serviceGuid, ref interfaceGuid, out unknown);

            if (hr != VSConstants.S_OK)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            try
            {
                service = (InterfaceType)Marshal.GetObjectForIUnknown(unknown);
            }
            finally
            {
                if (unknown != IntPtr.Zero)
                {
                    Marshal.Release(unknown);
                }
            }

            return service;
        }

        /// <summary>
        /// Releases the handle, previously assigned in Init method
        /// </summary>
        public override void DestroyHandle()
        {
            ReleaseHandle();
        }

        #endregion

        #region IDisposable Members
        /// <summary>
        /// Releases all resources, other than memory, used by the CodeEditorUserControl
        /// </summary>
        public void Dispose()
        {
            //Remove message filter
            Application.RemoveMessageFilter((System.Windows.Forms.IMessageFilter)this);

            if (serviceProvider != null)
            {
                // Remove this object from the list of the priority command targets.
                if (priorityCommandTargetCookie != 0)
                {
                    IVsRegisterPriorityCommandTarget register = QueryService<IVsRegisterPriorityCommandTarget>(typeof(SVsRegisterPriorityCommandTarget));
                    if (null != register)
                    {
                        int hr = register.UnregisterPriorityCommandTarget(priorityCommandTargetCookie);
                        if (hr != VSConstants.S_OK)
                        {
                            Marshal.ThrowExceptionForHR(hr);
                        }
                    }
                    priorityCommandTargetCookie = 0;
                }
                serviceProvider = null;
            }

            if (codeWindow != null)
            {
                codeWindow.Close();
                codeWindow = null;
            }
        }

        #endregion

        #region IMessageFilter Members

        /// <summary>
        /// Filters out a message before it is dispatched
        /// </summary>
        /// <param name="m">The message to be dispatched. You cannot modify this message.</param>
        /// <returns>True to filter the message and stop it from being dispatched; false to allow the message to continue to the next filter or control.</returns>
        public bool PreFilterMessage(ref Message m)
        {
            //IVsFilterKeys2 performs advanced keyboard message translation
            IVsFilterKeys2 filterKeys2 = QueryService<IVsFilterKeys2>(typeof(SVsFilterKeys));

            MSG[] messages = new MSG[1];
            messages[0].hwnd = m.HWnd;
            messages[0].lParam = m.LParam;
            messages[0].wParam = m.WParam;
            messages[0].message = (uint)m.Msg;

            Guid cmdGuid;
            uint cmdCode;
            int cmdTranslated;
            int keyComboStarts;

            int hr = filterKeys2.TranslateAcceleratorEx(messages,
                (uint)__VSTRANSACCELEXFLAGS.VSTAEXF_UseTextEditorKBScope //Translates keys using TextEditor key bindings. Equivalent to passing CMDUIGUID_TextEditor, CMDSETID_StandardCommandSet97, and guidKeyDupe for scopes and the VSTAEXF_IgnoreActiveKBScopes flag. 
                | (uint)__VSTRANSACCELEXFLAGS.VSTAEXF_AllowModalState,  //By default this function cannot be called when the shell is in a modal state, since command routing is inherently dangerous. However if you must access this in a modal state, specify this flag, but keep in mind that many commands will cause unpredictable behavior if fired. 
                0,
                null,
                out cmdGuid,
                out cmdCode,
                out cmdTranslated,
                out keyComboStarts);

            if (hr != VSConstants.S_OK)
            {
                return false;
            }

            return cmdTranslated != 0;
        }

        #endregion

        #region IOleCommandTarget Members
        //This implementation is a simple delegation to the implementation inside the text view 

        /// <summary>
        /// Executes a specified command or displays help for a command.
        /// </summary>
        /// <param name="pguidCmdGroup">Pointer to command group</param>
        /// <param name="nCmdID">Identifier of command to execute</param>
        /// <param name="nCmdexecopt">Options for executing the command</param>
        /// <param name="pvaIn">Pointer to input arguments</param>
        /// <param name="pvaOut">Pointer to command output</param>
        /// <returns></returns>
        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            return commandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
        }


        /// <summary>
        /// Queries the object for the status of one or more commands generated by user interface events.
        /// </summary>
        /// <param name="pguidCmdGroup">Pointer to command group</param>
        /// <param name="cCmds">Number of commands in prgCmds array</param>
        /// <param name="prgCmds">Array of commands</param>
        /// <param name="pCmdText">Pointer to name or status of command</param>
        /// <returns></returns>
        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            int hr = commandTarget.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);


            return hr;
        }

        #endregion


        static class NativeMethods
        {
            [DllImport("user32.dll", ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
            internal static extern IntPtr SetFocus(IntPtr hWnd);

            [DllImport("user32.dll", ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
            [return: MarshalAs(UnmanagedType.U1)]
            internal static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, int flags);

            [DllImport("user32.dll", ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
            [return: MarshalAs(UnmanagedType.U1)]
            internal static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        }
    }
}
