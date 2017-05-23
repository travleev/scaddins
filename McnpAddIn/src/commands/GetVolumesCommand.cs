using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading.Tasks;
using SpaceClaim.Api.V16;


namespace NK.McnpAddIn.commands
{
    public static class GetVolumesCommand
    {
        public static Command Create(string name)
        {
            Command c = Command.Create(name);
            c.Text = "Get Volumes";
            c.Hint = "Comute volumes of all model components and save them to a text file.";
            c.Executing += c_Executing;
            return c;

        }

        static void c_Executing(object sender, CommandExecutingEventArgs e)
        {

                // Find the document and model in the active widnow:
                Document doc = Window.ActiveWindow.Document;
                Part rootPart = doc.MainPart;

                // File, where volumes will be written.
                System.IO.StreamWriter fff = new System.IO.StreamWriter(doc.Path + ".volumes");

                /* this loop is over all unique parts, i.e. Master parts.
                fff.WriteLine("document.Parts");
                foreach (Part p in doc.Parts)
                {
                    fff.WriteLine("{0}", p.DisplayName);
                }

                fff.WriteLine("document.IParts");
                foreach (IPart ipart in doc.MainPart.GetDescendants<IPart>())
                {
                    fff.WriteLine("{0} {1} in {2}", ipart.Master.DisplayName, ipart.Root, ipart.GetAncestor<IPart>().Master.DisplayName);
                }
                 */

                // fff.WriteLine("document.DesignBodies");
                foreach (IPart ipart in getAllParts(doc.MainPart))
                {
                    foreach(IDesignBody ibody in ipart.Master.Bodies)
                    {
                        string all_names = ibody.Master.Name;
                        IPart p = ipart;
                        while (p.Parent != null)
                        {
                            all_names = p.Master.DisplayName + " / " + all_names;
                            p = p.GetAncestor<IPart>();
                        }
                        fff.WriteLine("{0}: {1}", all_names, ibody.Shape.Volume);
                    }
                    fff.WriteLine("");
                }


                /* check that rootPart is the model's root:
                Debug.Assert(rootPart.Parent == null);
                Debug.Assert(rootPart.Root == rootPart);

                // loop over all parts of the model:
                foreach (IPart ipart in getAllParts(rootPart))
                {
                    // output component names
                    fff.WriteLine("{0}:", ipart.Master.DisplayName);

                    // get names of all parents
                    string all__names = ipart.Master.Name;
                    string all_dnames = ipart.Master.DisplayName;
                    IInstance cpart = ipart.Parent;
                    // while (cpart != null)
                    // {
                        // all__names = cpart.Master.Name + '.' + all__names;
                        // all_dnames = cpart.Master.DisplayName + '.' + all_dnames;
                        // cpart = cpart.Parent;
                    // }

                    double cvol = 0.0, v = 0.0; // volume of all bodies in the current component
                    foreach (IDesignBody ib in ipart.Master.Bodies)
                    {
                        v = ib.Shape.Volume;
                        fff.WriteLine("{0} {1}: {2}", all__names, ib.Master.Name, v);
                        fff.WriteLine("{0} {1}: {2}", all_dnames, ib.Master.Name, v);
                    }
                } */
                MessageBox.Show(SpaceClaim.Api.V16.Application.MainWindow, "Output to " + doc.Path + ".volumes", "Volumes", MessageBoxButtons.OK);

                fff.Close();


        }

        private static IEnumerable<IPart> getAllParts(Part prt)
        {
            yield return prt;
            foreach (IPart p in prt.GetDescendants<IPart>())
            {
                yield return p;
            }
        }
        private static IEnumerable<IPart> getAllComponents(Part prt)
        {
            // yield return prt;
            foreach (IPart p in prt.GetDescendants<IComponent>())
            {
                yield return p;
            }
        }

    }
}
