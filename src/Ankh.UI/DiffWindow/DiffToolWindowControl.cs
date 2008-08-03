﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using Ankh.Scc.UI;
using Ankh.UI.Services;
using Microsoft.VisualStudio.Shell.Interop;
using System.Collections.ObjectModel;
using System.IO;
using Ankh.Diff.DiffUtils;
using Ankh.Ids;

namespace Ankh.UI.DiffWindow
{
    public partial class DiffToolWindowControl : AnkhToolWindowControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DiffControl"/> class.
        /// </summary>
        public DiffToolWindowControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Called when the frame is created
        /// </summary>
        /// <param name="e"></param>
        protected override void OnFrameCreated(EventArgs e)
        {
            base.OnFrameCreated(e);

            ToolWindowSite.CommandContext = AnkhId.DiffMergeContextGuid;
            ToolWindowSite.KeyboardContext = AnkhId.DiffMergeContextGuid;
        }

        int _nFrame;
        protected override void OnFrameClose(EventArgs e)
        {
            base.OnFrameClose(e);

            OnClose();
        }

        protected override void OnFrameShow(FrameEventArgs e)
        {
            base.OnFrameShow(e);

            switch(e.Show)
            {
                case __FRAMESHOW.FRAMESHOW_Hidden:
                case __FRAMESHOW.FRAMESHOW_DestroyMultInst:
                case __FRAMESHOW.FRAMESHOW_WinClosed:
                    OnClose();
                    break;
            }
        }

        void OnClose()
        {
            Clear();

            if (_nFrame >= 0)
            {
                Context.GetService<IAnkhDiffHandler>().ReleaseDiff(_nFrame);
                _nFrame = -1;
            }
        }

        private void Clear()
        {
            //throw new NotImplementedException();
        }

        private void GetFileLines(string strA, string strB, out Collection<string> A, out Collection<string> B)
        {
            A = Functions.GetFileTextLines(strA);
            B = Functions.GetFileTextLines(strB);
        }

        public void Reset(int n, AnkhDiffArgs args)
        {
            _nFrame = n;
            Clear();

            Collection<string> A, B;
            GetFileLines(args.BaseFile, args.MineFile, out A, out B);
            TextDiff Diff = new TextDiff(HashType.HashCode, false, false);
            EditScript Script = Diff.Execute(A, B);

            string strCaptionA = args.BaseTitle ?? Path.GetFileName(args.BaseFile);
            string strCaptionB = args.MineTitle ?? Path.GetFileName(args.MineFile);
            //Ankh.Diff.FileName fnA = new Ankh.Diff.FileName(mine);
            //Ankh.Diff.FileName fnB = new Ankh.Diff.FileName(theirs);
            diffControl1.SetData(A, B, Script, strCaptionA, strCaptionB);

            ToolWindowSite.Title = Path.GetFileName(args.MineFile) + " - Diff";
        }
    }
}
