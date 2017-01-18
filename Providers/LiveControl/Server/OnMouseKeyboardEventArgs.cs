using Model.LiveControl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Providers.LiveControl.Server
{
    public class OnMouseKeyboardEventArgs
    {
        public MouseKeyboardState MoseKeySnapshot { get; set; }
    }
}
