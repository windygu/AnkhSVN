﻿using System;
using System.Collections.Generic;
using System.Text;
using SharpSvn;
using WizardFramework;

namespace Ankh.UI.MergeWizard
{
    public class MergeUtils
    {
        public static readonly WizardMessage INVALID_FROM_REVISION = new WizardMessage(Resources.InvalidFromRevision,
            WizardMessage.ERROR);
        public static readonly WizardMessage INVALID_TO_REVISION = new WizardMessage(Resources.InvalidToRevision,
            WizardMessage.ERROR);
        public static readonly WizardMessage INVALID_FROM_URL = new WizardMessage(Resources.InvalidFromUrl,
            WizardMessage.ERROR);
        public static readonly WizardMessage INVALID_TO_URL = new WizardMessage(Resources.InvalidToUrl,
            WizardMessage.ERROR);

        IAnkhServiceProvider _context;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context">The context.</param>
        public MergeUtils(IAnkhServiceProvider context)
        {
            Context = context;
        }

        /// <summary>
        /// Returns a list of strings for the suggested merge sources.
        /// </summary>
        public List<string> GetSuggestedMergeSources(SvnItem target, MergeWizard.MergeType mergeType)
        {
            List<string> sources = new List<string>();

            if (mergeType != MergeWizard.MergeType.Reintegrate)
            {
                using (SvnClient client = GetClient())
                {
                    SvnMergeSourcesCollection mergeSources;
                    SvnGetSuggestedMergeSourcesArgs args = new SvnGetSuggestedMergeSourcesArgs();
                    SvnItem parent = target.Parent;

                    args.ThrowOnError = false;

                    if (client.GetSuggestedMergeSources(SvnTarget.FromUri(target.Status.Uri), args, out mergeSources))
                    {
                        foreach (SvnMergeSource source in mergeSources)
                        {
                            sources.Add(source.Uri.ToString());
                        }
                    }
                }

                sources.Sort();
            }

            return sources;
        }

        /// <summary>
        /// Returns an instance of <code>SharpSvn.SvnClient</code> from the pool.
        /// </summary>
        public SvnClient GetClient()
        {
            ISvnClientPool pool = (Context != null) ? Context.GetService<ISvnClientPool>() : null;

            if (pool != null)
                return pool.GetClient();
            else
                return new SvnClient();
        }

        /// <summary>
        /// Gets or sets the context.
        /// </summary>
        /// <value>The context.</value>
        public IAnkhServiceProvider Context
        {
            get { return _context; }
            set { _context = value; }
        }
    }
}
