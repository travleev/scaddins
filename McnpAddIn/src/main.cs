using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SpaceClaim.Api.V16;
using SpaceClaim.Api.V16.Extensibility;
using SpaceClaim.Api.V16.Geometry;
using System.Diagnostics;

namespace NK.McnpAddIn
{
    // class describes whole add-in.
    public class McnpNKAddIn: AddIn, IExtensibility, IRibbonExtensibility, ICommandExtensibility
    {
        /*readonly CommandCapsule[] capsules = new[] {
            new commands.McnpPlotCapsule()
        };*/
        #region IExtensibility Members

        public bool Connect()
        {
            // perform any initialization for your add-in here
            return true;
        }

        public void Disconnect()
        {
            // perform any cleanup for ypur add-in here
        }
        #endregion


        #region IRibbonExtensibility Members

        public string GetCustomUI()
        {
            return Resources.Resource1.Ribbon;
        }
        #endregion

        #region ICommandExtensibility Members
        public void Initialize()
        {
            Command getVols = NK.McnpAddIn.commands.GetVolumesCommand.Create("Mcnp.GetVolumes");
            Command getPcom = NK.McnpAddIn.commands.GetPlotCommands.Create();
            /*foreach (CommandCapsule capsule in capsules)
                capsule.Initialize();
            */
        }



        void _report_info(string sss)
        {
            SpaceClaim.Api.V16.Application.ReportStatus(sss, StatusMessageType.Information, null);
        }
        
        #endregion
    }
}
