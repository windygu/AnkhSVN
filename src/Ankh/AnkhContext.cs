// $Id$
using System;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using Ankh.UI;
using Ankh.VS;

using SharpSvn;
using IServiceProvider = System.IServiceProvider;

namespace Ankh
{
    /// <summary>
    /// General context object for the Ankh addin. Contains pointers to objects
    /// required by commands.
    /// </summary>
    public class OldAnkhContext : AnkhService, IContext, IAnkhServiceProvider
    {
        OutputPaneWriter _outputPane;
        IAnkhConfigurationService _config;

        /// <summary>
        /// Fired when the addin is unloading.
        /// </summary>
        public event EventHandler Unloading;

        public OldAnkhContext(IAnkhPackage package)
            : this(package, null)
        {
        }

        public OldAnkhContext(IAnkhPackage package, IUIShell uiShell)
            : base(package)
        {
            if (uiShell != null)
                _uiShell = uiShell;

            this._config = package.GetService<IAnkhConfigurationService>();

            this.LoadConfig();

            this._outputPane = new OutputPaneWriter(this, "AnkhSVN");
        }

        EnvDTE._DTE _dte;
        /// <summary>
        /// The top level automation object.
        /// </summary>
        EnvDTE._DTE DTE
        {
            [System.Diagnostics.DebuggerStepThrough]
            get { return _dte ?? (_dte = GetService<EnvDTE._DTE>(typeof(Microsoft.VisualStudio.Shell.Interop.SDTE))); }
        }

        IUIShell _uiShell;
        /// <summary>
        /// The UI shell service
        /// </summary>
        public IUIShell UIShell
        {
            [System.Diagnostics.DebuggerStepThrough]
            get { return _uiShell ?? (_uiShell = GetService<IUIShell>()); }
        }

        /// <summary>
        /// The output pane.
        /// </summary>
        public OutputPaneWriter OutputPane
        {
            [System.Diagnostics.DebuggerStepThrough]
            get { return this._outputPane; }
        }

        ISvnClientPool _clientPool;
        /// <summary>
        /// Gets the client pool service
        /// </summary>
        public ISvnClientPool ClientPool
        {
            [System.Diagnostics.DebuggerStepThrough]
            get { return this._clientPool ?? (this._clientPool = GetService<ISvnClientPool>()); }
        }

        /// <summary>
        /// The configloader.
        /// </summary>
        public IAnkhConfigurationService Configuration
        {
            [System.Diagnostics.DebuggerStepThrough]
            get { return this._config; }
        }

        bool _operationRunning;
        /// <summary>
        /// Should be called before starting any lengthy operation
        /// </summary>
        public IDisposable StartOperation(string description)
        {
            //TODO: maybe refactor this?
            _operationRunning = true;
            try
            {
                this.DTE.StatusBar.Text = description + "...";
                this.DTE.StatusBar.Animate(true, EnvDTE.vsStatusAnimation.vsStatusAnimationSync);
            }
            catch (Exception)
            {
                // Swallow, not critical
            }

            return new OperationCompleter(this, this.OutputPane.StartActionText(description));
        }

        class OperationCompleter : IDisposable
        {
            OldAnkhContext _context;
            IDisposable _disp2;

            public OperationCompleter(OldAnkhContext context, IDisposable disp2)
            {
                _context = context;
                _disp2 = disp2;
            }

            public void Dispose()
            {
                _context.EndOperation();
                _context = null;
                _disp2.Dispose();
                _disp2 = null;
            }
        }

        /// <summary>
        ///  called at the end of any lengthy operation
        /// </summary>
        public void EndOperation()
        {
            if (_operationRunning)
            {
                try
                {
                    this.DTE.StatusBar.Text = "Ready";
                    this.DTE.StatusBar.Animate(false, EnvDTE.vsStatusAnimation.vsStatusAnimationSync);
                }
                catch (Exception)
                {
                    // swallow, not critical
                }
                _operationRunning = false;
            }
        }

        /// <summary>
        /// Miscellaneous cleanup stuff goes here.
        /// </summary>
        public void Shutdown()
        {
            if (this.Unloading != null)
                this.Unloading(this, EventArgs.Empty);
        }

        /// <summary>
        /// try to load the configuration file
        /// </summary>
        private void LoadConfig()
        {
            this._config.LoadConfig();


            this.Configuration.ConfigFileChanged += new EventHandler(ConfigLoader_ConfigFileChanged);
        }

        /// <summary>
        /// Seems someone has updated the config file on disk.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ConfigLoader_ConfigFileChanged(object sender, EventArgs e)
        {
            try
            {
                this.Configuration.LoadConfig();
                this.OutputPane.WriteLine("Configuration reloaded.");
            }
            catch (Ankh.Configuration.ConfigException ex)
            {
                this.OutputPane.WriteLine("Configuration file has errors: " + ex.Message);
            }
            catch (Exception ex)
            {
                IAnkhErrorHandler handler = GetService<IAnkhErrorHandler>();

                if (handler != null)
                    handler.OnError(ex);
                else
                    throw;
            }
        }

        #region Win32Window class
        private class Win32Window : IWin32Window
        {
            public Win32Window(IntPtr handle)
            {
                this.handle = handle;
            }
            #region IWin32Window Members

            public System.IntPtr Handle
            {
                get
                {
                    return this.handle;
                }
            }

            #endregion
            private IntPtr handle;

        }
        #endregion
    }
}
