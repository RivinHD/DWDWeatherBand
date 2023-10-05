using CSDeskBand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace DWDWeatherBand
{
    [ComVisible(true)]
    [Guid("3A5C2853-FFAC-47E0-A48D-4463F8A61623")]
    [CSDeskBandRegistration(Name = "DWDWeatherBand", ShowDeskBand = true)]
    public class Deskband : CSDeskBandWpf
    {
        private UIElement _taskbarMonitor;
        public Deskband()
        {
            try
            {
                _taskbarMonitor = new TaskbarMonitor(this);

            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show(
                    e.ToString(),
                    "Unhandled exception",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }
#if DEBUG
        public Deskband(bool _)
        {
        }
#endif
        protected override UIElement UIElement => _taskbarMonitor;

        protected override void DeskbandOnClosed()
        {
            base.DeskbandOnClosed();
            _taskbarMonitor = null;
        }
    }
}
