﻿using System;
using System.Collections.Generic;
using System.Text;
using Ankh.ContextServices;
using System.Windows.Forms;
using System.Runtime.Remoting.Contexts;
using Ankh.VS;
using Microsoft.VisualStudio.Shell.Interop;
using System.ComponentModel.Design;
using System.ComponentModel;
using Ankh.Ids;
using System.Windows.Forms.Design;
using Microsoft.VisualStudio.OLE.Interop;

namespace Ankh.UI
{
    [CLSCompliant(false)]
    public interface IAnkhVSContainerForm
    {
        //IVsToolWindowToolbarHost ToolBarHost { get; }
    }

    /// <summary>
    /// .Net form which when shown modal let's the VS command routing continue
    /// </summary>
    /// <remarks>If the IAnkhDialogOwner service is not available this form behaves like any other form</remarks>
    public class VSContainerForm : System.Windows.Forms.Form, IAnkhVSContainerForm
    {
        IAnkhServiceProvider _context;
        IAnkhDialogOwner _dlgOwner;
        AnkhToolBar _toolbarId;
        
        [Browsable(false)]
        public IAnkhServiceProvider Context
        {
            get { return _context; }
            set 
            {
                if (_context != value)
                {
                    _context = value;
                    _dlgOwner = null;
                    OnContextChanged(EventArgs.Empty);
                }
            }
        }

        protected virtual void OnContextChanged(EventArgs e)
        {
        }

        /// <summary>
        /// Gets the dialog owner service
        /// </summary>
        /// <value>The dialog owner.</value>
        [Browsable(false), CLSCompliant(false)]
        protected IAnkhDialogOwner DialogOwner
        {
            get 
            { 
                if(_dlgOwner == null && _context != null)
                    _dlgOwner = _context.GetService<IAnkhDialogOwner>();

                return _dlgOwner; 
            }
        }

        /// <summary>
        /// Obsolete: Use ShowDialog(Context)
        /// </summary>
        [Obsolete("Always use ShowDialog(Context) even when the context is already set")]
        public new DialogResult ShowDialog()
        {
            if (Context != null)
                return ShowDialog(Context);
            else
                return ShowDialog(new AnkhServiceContainer());
        }

        [Obsolete("Always use ShowDialog(Context) even when the context is already set")]
        public new DialogResult ShowDialog(IWin32Window owner)
        {
            if (Context != null)
                return ShowDialog(Context, owner);
            else
                return ShowDialog(new AnkhServiceContainer(), owner);
        }

        /// <summary>
        /// Shows the form as a modal dialog box with the VS owner window
        /// </summary>
        /// <param name="context">The context.</param>
        public DialogResult ShowDialog(IAnkhServiceProvider context)
        {
            if(context == null && _context == null)
                throw new ArgumentNullException("context");
           
            return ShowDialog(context, null);
        }       

        /// <summary>
        /// Show the form as a modal dialog with the specified owner window
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="owner">The owner.</param>
        public DialogResult ShowDialog(IAnkhServiceProvider context, IWin32Window owner)
        {
            bool setContext = false;

            if(context == null)
            {
                if(Context == null)
                    throw new ArgumentNullException("context");
            }
            else if(Context == null)
                setContext = true;
            else if(context != Context)
                throw new ArgumentOutOfRangeException("context", "context must match context or be null");
            
            if(setContext)
                Context = context;

            IUIService uiService = null;

            if(Context != null)
                uiService = Context.GetService<IUIService>();

            try
            {
                if(owner == null && DialogOwner != null)
                    owner = DialogOwner.DialogOwner;

                OnBeforeShowDialog(EventArgs.Empty);

                DialogResult rslt;

                if (DialogOwner != null)
                {
                    using (DialogOwner.InstallFormRouting(this, EventArgs.Empty))
                    {
                        if (uiService != null)
                            rslt = uiService.ShowDialog(this);
                        else
                            rslt = base.ShowDialog(owner);
                    }
                }
                else
                {
                    if (uiService != null)
                        rslt = uiService.ShowDialog(this);
                    else
                        rslt = base.ShowDialog(owner);
                }

                OnAfterShowDialog(EventArgs.Empty);

                return rslt;
            }
            finally
            {
                if (setContext)
                    Context = null;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            if (DialogOwner != null)
                DialogOwner.OnContainerCreated(this);
        }

        protected virtual void OnBeforeShowDialog(EventArgs e)
        {
        }

        protected virtual void OnAfterShowDialog(EventArgs e)
        {

        }

        public AnkhToolBar ToolBar
        {
            get { return _toolbarId; }
            set { _toolbarId = value; }
        }

        [CLSCompliant(false)]
        protected void AddCommandTarget(IOleCommandTarget commandTarget)
        {
            if (commandTarget == null)
                throw new ArgumentNullException("commandTarget");

            if(DialogOwner == null)
                throw new InvalidOperationException("DialogOwner not available");

            DialogOwner.AddCommandTarget(this, commandTarget);
        }
    }
}
