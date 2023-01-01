﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace OnlineRadio.Core
{
    public interface IButtonPlugin : IPlugin
    {
        IEnumerable<UserControl> Buttons { get; }
    }
}
