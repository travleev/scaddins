using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpaceClaim.Api.V16;
using SpaceClaim.Api.V16.Geometry;
using ClipBoard = System.Windows.Forms.Clipboard;
using System.Globalization; // for CultureInfo


namespace NK.McnpAddIn.commands
{
    class GetPlotCommands
    {
        static double cc = 100.0; // m to cm conversion factor
        public static Command Create()
        {
            Command c = Command.Create("Mcnp.GetPlotCommands2");
            c.Text = "Plot";
            c.Hint = "When in section or sketch mode, put MCNP plot commands to clipboard.\nWhen in 3D mode, parse clipboard as MCNP plot commands and add plane.";
            c.Executing += c_Executing;
            return c;

        }

        private static double nullify(double x)
        {
            if (x*x < 1e-20)
            {return 0.0;}
            else
            {return x;}
        }

        private static void c_Executing(object sender, CommandExecutingEventArgs e)
        {
            Window aw = Window.ActiveWindow;

            // section plane. It is not null only if sketch or section mode is on.
            Plane sP = aw.ActiveContext.SectionPlane;
            if (sP != null)
            {
                int sw = aw.Size.Width;
                int sh = aw.Size.Height;
                // get screen center, right-mid and upper mid
                System.Drawing.Point sm = new System.Drawing.Point((int)Math.Round(sw*0.5), (int)Math.Round(sh*0.5));
                System.Drawing.Point sr = new System.Drawing.Point(sw, sm.Y);
                System.Drawing.Point su = new System.Drawing.Point(sm.X, 1);
                // screen center projection onto section plane in model coordinates
                Point mm;
                if (sP.TryIntersectLine(aw.ActiveContext.GetCursorRay(sm), out mm))
                {
                    // get projections of right mid and upper mid and use these projections
                    // to define bas vectors.
                    Point mr, mu;
                    sP.TryIntersectLine(aw.ActiveContext.GetCursorRay(sr), out mr);
                    sP.TryIntersectLine(aw.ActiveContext.GetCursorRay(su), out mu);
                    Vector bx = mr - mm;
                    Vector bz = Vector.Cross(bx, mu - mm); // normal to section plane looking to us
                    Vector by = Vector.Cross(bz, bx);
                    double ext = bx.Magnitude;
                    // rotate view to look perpendicular to section plane
                    double fe = ext * Math.Min(sw, sh) / Math.Max(sw, sh) * 2;
                    aw.SetProjection(Frame.Create(mm, bx.Direction, by.Direction), fe);

                    // generate string for clipboard
                    string f3 = " {0:G5} {1:G5} {2:G5} ";
                    string f1 = " {0:G5} ";
                    string pc = "";

                    // or command
                    pc = "or";
                    pc += string.Format(f3, nullify(mm.X * cc), nullify(mm.Y * cc), nullify(mm.Z * cc));
                    // bas command
                    bx = bx.Direction.UnitVector;
                    by = by.Direction.UnitVector;
                    pc += "bas";
                    pc += string.Format(f3, nullify(bx.X), nullify(bx.Y), nullify(bx.Z));
                    pc += string.Format(f3, nullify(by.X), nullify(by.Y), nullify(by.Z));
                    // ext command
                    pc += "ext";
                    pc += string.Format(f1, ext * cc);

                    // tune formatting
                    pc = pc.Replace(",", ".");
                    pc = pc.Replace("E+000", " ");
                    pc = pc.Replace("E-000", " ");
                    pc = pc.Replace("E+00", "E+");
                    pc = pc.Replace("E-00", "E-");
                    pc = pc.Replace("E+0", "E+");
                    pc = pc.Replace("E-0", "E-");

                    // put to clipboard
                    ClipBoard.SetText(pc);
                }
                else 
                {
                    SpaceClaim.Api.V16.Application.ReportStatus("Section plane exists but cannot intersect with cursor ray", StatusMessageType.Information, null);
                }
            }
            else
            {
                // try to read plot commands from clipboard
                Frame fr;
                double ext;
                if (TryParseClipboard(out fr, out ext))
                {
                    // If clipboard parsed, add a new plane to the model
                    DatumPlane.Create(aw.Document.MainPart, "MCNP plot plane", Plane.Create(fr));
                }
                else
                {
                    SpaceClaim.Api.V16.Application.ReportStatus("Clipboard does not contain MCNP plot commands.", StatusMessageType.Warning, null);
                }
            }
        }

        private static bool TryParseClipboard(out Frame fr, out double ext)
        {
            if (ClipBoard.ContainsText())
            {
                string cmd = ClipBoard.GetText().ToLower();
                while (cmd.Contains("  ")) cmd = cmd.Replace("  ", " "); // remove repeated spaces
                List<string> tokens = cmd.Split(null).ToList(); // list elements can be removed.
                bool edef = false; // flags that ext, bas and or are defined from clipboard.
                bool bdef = false;
                bool odef = false;
                Point mm;
                Vector bx, by;
                double e1 = 0;
                while (tokens.Count > 0)
                {
                    SpaceClaim.Api.V16.Application.ReportStatus(string.Format("Parsing token {0} from {1}", tokens[0], tokens.Count), StatusMessageType.Information, null);
                    if (tokens[0].Contains("or"))
                    {
                        double x = float.Parse(tokens[1], CultureInfo.InvariantCulture.NumberFormat);
                        double y = float.Parse(tokens[2], CultureInfo.InvariantCulture.NumberFormat);
                        double z = float.Parse(tokens[3], CultureInfo.InvariantCulture.NumberFormat);
                        tokens.RemoveAt(0);
                        tokens.RemoveAt(0);
                        tokens.RemoveAt(0);
                        tokens.RemoveAt(0);
                        odef = true;
                        mm = Point.Create(x / cc, y / cc, z / cc);
                        SpaceClaim.Api.V16.Application.ReportStatus(string.Format("Parsed or: {0}, {1}, {2}", x, y, z), StatusMessageType.Information, null);
                    }
                    else if (tokens[0].Contains("bas"))
                    {
                        tokens.RemoveAt(0);
                        var vals = new List<Double> { };
                        for (int i = 1; i <= 6; i++)
                        {
                            vals.Add(float.Parse(tokens[0], CultureInfo.InvariantCulture.NumberFormat));
                            SpaceClaim.Api.V16.Application.ReportStatus(string.Format("Parsed bas: {0}, {1}", tokens[0], vals.Last()), StatusMessageType.Information, null);
                            tokens.RemoveAt(0);
                        }
                        bx = Vector.Create(vals[0], vals[1], vals[2]);
                        by = Vector.Create(vals[3], vals[4], vals[5]);
                        bdef = true;
                    }
                    else if (tokens[0].Contains("ext"))
                    {
                        // use only the first entry here
                        e1 = float.Parse(tokens[1], CultureInfo.InvariantCulture.NumberFormat);
                        tokens.RemoveAt(0);
                        tokens.RemoveAt(0);
                        edef = true;
                        SpaceClaim.Api.V16.Application.ReportStatus(string.Format("Parsed ext: {0}", e1), StatusMessageType.Information, null);
                    }
                    else
                        tokens.RemoveAt(0);

                }
                if (odef && bdef && edef)
                {
                    fr = Frame.Create(mm, bx.Direction, by.Direction);
                    ext = e1 / cc;
                    SpaceClaim.Api.V16.Application.ReportStatus("ClipBoard parsed.", StatusMessageType.Information, null);

                    return true;
                }
                else
                {
                    fr = Frame.Create(mm, Vector.Create(0, 0, 1).Direction);
                    ext = 0.0;
                    return false;
                }
                
            }

            throw new NotImplementedException();
        }

    }
}
