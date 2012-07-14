﻿using System;
using System.Windows.Forms;

namespace Ankh.UI
{
    public interface IWinFormsThemingService
    {
        void ThemeControl(Control control);

        // 
        void VSThemeWindow(Control control);

        bool TryGetIcon(string path, out IntPtr hIcon);
    }
}
