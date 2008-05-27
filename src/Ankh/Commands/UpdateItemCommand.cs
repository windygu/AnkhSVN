// $Id$
using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using SharpSvn;

using Ankh.UI;
using Ankh.Ids;
using Ankh.VS;
using Ankh.Selection;
using System.Collections.ObjectModel;
using System.Windows.Forms.Design;
using Ankh.Scc;

namespace Ankh.Commands
{
    /// <summary>
    /// A command that updates an item.
    /// </summary>
    [Command(AnkhCommand.UpdateItem)]
    public class UpdateItem : CommandBase
    {
        public override void OnUpdate(CommandUpdateEventArgs e)
        {
            foreach (SvnItem item in e.Selection.GetSelectedSvnItems(true))
            {
                if (item.IsVersioned)
                    return;
            }
            e.Enabled = false;
        }

        public override void OnExecute(CommandEventArgs e)
        {
            IContext context = e.GetService<IContext>();
            IAnkhDialogOwner dialogOwner = e.GetService<IAnkhDialogOwner>();

            PathSelectorResult result = null;
            PathSelectorInfo info = new PathSelectorInfo("Select Items to Update",
                e.Selection.GetSelectedSvnItems(true));

            info.CheckedFilter += delegate(SvnItem item) { return item.IsVersioned; };
            info.VisibleFilter += delegate(SvnItem item) { return item.IsVersioned; };
            info.EnableRecursive = true;
            info.RevisionStart = SvnRevision.Head;

            if (!CommandBase.Shift)
            {
                result = context.UIShell.ShowPathSelector(info);
            }
            else
            {
                result = info.DefaultResult;
            }

            if (!result.Succeeded)
                return;

            SaveAllDirtyDocuments(e.Selection, e.Context);

            SvnUpdateResult ur = null;

            e.GetService<IProgressRunner>().Run("Updating", 
                delegate(object sender, ProgressWorkerArgs ee)
                {
                    List<string> files = new List<string>();

                    
                    foreach(SvnItem item in result.Selection)
                    {
                        if(item.IsVersioned)
                            files.Add(item.FullPath);
                    }
                    SvnUpdateArgs ua = new SvnUpdateArgs();
                    ua.Depth = result.Depth;
                    ua.Revision = result.RevisionStart;

                    ee.Client.Update(files, ua, out ur);
                });
        }
    }
}