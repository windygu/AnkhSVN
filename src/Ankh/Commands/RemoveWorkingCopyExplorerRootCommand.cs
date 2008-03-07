using System;
using System.Text;
using System.Collections;
using AnkhSvn.Ids;

namespace Ankh.Commands
{
    /// <summary>
    /// Command to remove current root from the Working Copy Explorer.
    /// </summary>
    [VSNetCommand(AnkhCommand.RemoveWorkingCopyExplorerRoot,
		"RemoveWorkingCopyExplorerRoot",
         Text = "&Remove Root",
         Tooltip = "Remove this root from the Working Copy Explorer.",
         Bitmap = ResourceBitmaps.RemoveFolder ),
         VSNetControl( "WorkingCopyExplorer", Position = 1 )]
    public class RemoveWorkingCopyExplorerRootCommand : CommandBase
    {
        #region Implementation of ICommand

        public override EnvDTE.vsCommandStatus QueryStatus( IContext context )
        {
            return context.WorkingCopyExplorer.IsRootSelected ? Enabled : EnvDTE.vsCommandStatus.vsCommandStatusInvisible;
        }

        public override void OnExecute(CommandEventArgs e)
        {
            IContext context = e.Context;

            context.WorkingCopyExplorer.RemoveSelectedRoot();
        }

        #endregion
    }
}