using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using SpaceClaim.Api.V16;
using SpaceClaim.Api.V16.Display;
using SpaceClaim.Api.V16.Extensibility;
using SpaceClaim.Api.V16.Geometry;
using Point = SpaceClaim.Api.V16.Geometry.Point;
using Direction = SpaceClaim.Api.V16.Geometry.Direction;
using Vector = SpaceClaim.Api.V16.Geometry.Vector;
using ScreenPoint = System.Drawing.Point;

namespace NK.McnpAddIn.commands
{
    class McnpPlotCapsule : CommandCapsule
    {
        public const string CommandName = "Mcnp.GetPlotCommands";

        public McnpPlotCapsule()
            : base(CommandName, "MCNP Plot commands", null, "Generate commands for MCNP plotter")
        {
        }

        protected override void OnUpdate(Command command)
        {
            Window window = Window.ActiveWindow;
            command.IsEnabled = window != null;
            command.IsChecked = window != null && window.ActiveTool is McnpPlotTool;
        }

        protected override void OnExecute(Command command, ExecutionContext context, Rectangle buttonRect)
        {
            Window window = Window.ActiveWindow;
            window.SetTool(new McnpPlotTool());
        }
    }

    class McnpPlotTool : Tool
    {
        Point pCur, pOrig;
        Vector basX, basY, basZ;
        double ext;
        byte tState; // tool state. 
        double cc = 100.0; // Meter to cantimeter conversion factor.

        private void _report(string msg, bool sr = false){
            string cm = string.Format("{0}:{1}", msg, tState);
            if (sr)
                SpaceClaim.Api.V16.Application.ReportStatus(cm, StatusMessageType.Information, null);
            StatusText = cm;
        }

        public McnpPlotTool()
            : base(InteractionMode.Section)
        {
        }

        void Reset(string msg)
        {
            tState = 0; // 0 -- undefined, 1 -- orig defined, 2 -- bas defined, 3 -- ext defined (i.e. all defined)
            // _report("Reset from " + msg);
        }

        protected override void OnInitialize()
        {
            Reset("OnInitialize");
            SelectionTypes = new Type[0]; // do not preselect anything
        }



        protected override void OnEnable(bool enable)
        {
            // _report("OnEnable");
            base.OnEnable(enable);
        }

        #region Click-Click Notifications

        protected override bool OnClickStart(ScreenPoint cursorPos, Line cursorRay)
        {
            // _report("OnClickStart");
            if (getPointUnderCursor(cursorRay, out pCur, out basZ))
            {
                pOrig = pCur;
                tState = 1;
                return true;
            }
            return false;
        }


        protected override void OnClickMove(ScreenPoint cursorPos, Line cursorRay)
        {
            if (getPointUnderCursor(cursorRay, out pCur, out basZ))
            {
                if (tState == 0)
                {
                    // nothing is defined. Mouse is moved to set origin.
                }
                else if (tState == 1)
                {
                    // pOrig is defined. Mouse is moved to find position of basX
                    basX = pCur - pOrig;
                    basY = Vector.Cross(basZ, basX);
                    basY *= (basX.Magnitude / basY.Magnitude);
                    ext = basX.Magnitude;
                    // _report(PlotCommands[1]);
                }
                else if (tState == 2)
                {
                    // pOrig and bas are defined. Mouse is moved to set extension.
                    ext = (pCur - pOrig).Magnitude;
                    // _report(PlotCommands[2]);
                }
                UpdateRendering();
                // _compare_camera_and_cursor(cursorPos, cursorRay);
            }
        }

        protected override bool OnClickEnd(ScreenPoint cursorPos, Line cursorRay)
        {
            // _report("OnClickEnd");
            if (getPointUnderCursor(cursorRay, out pCur, out basZ))
            {
                if (tState == 0)
                {
                    // Mouse click sets or.
                    pOrig = pCur;
                    tState = 1;
                    UpdateRendering();
                    return true;
                }
                else if (tState == 1)
                {
                    // Mouse click sets basX and basY.
                    tState = 2;
                    UpdateRendering();
                    return true;
                }
                else if (tState == 2)
                {
                    // Mouse click sets ext.
                    UpdateRendering();
                    // _report(PlotCommands[0] + PlotCommands[1] + PlotCommands[2], true);
                    //_compare_camera_and_cursor(cursorPos, cursorRay);

                    // Prepare view for exporting and put plot commands to clipboard:
                    double w = Window.ActiveWindow.Size.Width;
                    double h = Window.ActiveWindow.Size.Height;
                    double e = ext * Math.Max(w, h) / Math.Min(w, h) * 1.1;
                    Window.ActiveWindow.SetProjection(Frame.Create(pOrig, basX.Direction, basY.Direction), e);

                    // put to clipboard
                    System.Windows.Forms.Clipboard.SetText(formatCommands());

                    
                    Reset("OnClickEnd");
                    return false;
                }
            }
            return false;
        }

        protected override void OnClickCancel()
        {
            Reset("OnClickCancel");
        }
        #endregion

        private bool getPointUnderCursor(Line cursorRay, out Point p, out Vector v)
        {
            Plane sP = Window.ActiveContext.SectionPlane;
            if (sP != null)
            {
                sP.TryIntersectLine(cursorRay, out p);
                v = sP.Frame.DirZ.UnitVector;
                return true;
            }
            return false;
        }

        string formatCommands()
        {
            string f3 = " {0:G5} {1:G5} {2:G5} ";
            string f1 = " {0:G5} ";
            string[] PlotCommands = { "", "", "" };
            string pc = "";


            // prepare PlotCommands strings
            if (tState >= 1)
            {
                // origin is set
                PlotCommands[0] = "or";
                PlotCommands[0] += string.Format(f3, pOrig.X * cc, pOrig.Y * cc, pOrig.Z * cc);
                pc += PlotCommands[0];
            }
            if (tState >= 2)
            {
                // bas and ext are set
                PlotCommands[1] = "bas";
                PlotCommands[1] += string.Format(f3, basX.X * cc, basX.Y * cc, basX.Z * cc);
                PlotCommands[1] += string.Format(f3, basY.X * cc, basY.Y * cc, basY.Z * cc);
                pc += PlotCommands[1];

                PlotCommands[2] = "ext";
                PlotCommands[2] += string.Format(f1, ext * cc);
                pc += PlotCommands[2];
            }

            // tune formatting
            pc = pc.Replace(",", ".");
            pc = pc.Replace("E+000", " ");
            pc = pc.Replace("E-000", " ");
            pc = pc.Replace("E+00", "E+");
            pc = pc.Replace("E-00", "E-");
            pc = pc.Replace("E+0", "E+");
            pc = pc.Replace("E-0", "E-");
            return pc;

        }


        void UpdateRendering()
        {
            // list of primitives to be rendered:
            var primitives = new List<Primitive> { };

            // line for basX, basY:
            if (tState >= 1)
            {
                Vector v1, v2;
                v1 = basX.Direction.UnitVector * ext;
                v2 = basY.Direction.UnitVector * ext;
                primitives.Add(CurvePrimitive.Create(CurveSegment.Create(pOrig, pOrig + v1)));
                primitives.Add(CurvePrimitive.Create(CurveSegment.Create(pOrig, pOrig + v2)));
            }
            if (tState >= 2)
            {
                Point p1, p2, p3, p4;
                Vector v1, v2;
                v1 = basX.Direction.UnitVector * ext;
                v2 = basY.Direction.UnitVector * ext;
                p1 = pOrig - v1 - v2;
                p2 = pOrig - v1 + v2;
                p3 = pOrig + v1 + v2;
                p4 = pOrig + v1 - v2;
                primitives.Add(CurvePrimitive.Create(CurveSegment.Create(p1, p2)));
                primitives.Add(CurvePrimitive.Create(CurveSegment.Create(p2, p3)));
                primitives.Add(CurvePrimitive.Create(CurveSegment.Create(p3, p4)));
                primitives.Add(CurvePrimitive.Create(CurveSegment.Create(p4, p1)));
            }

            /*{
                // show frame of the sectionPlane:
                Point p0 = Window.ActiveContext.SectionPlane.Frame.Origin;
                Vector vx = Window.ActiveContext.SectionPlane.Frame.DirX.UnitVector * ext * 1.2;
                Vector vy = Window.ActiveContext.SectionPlane.Frame.DirY.UnitVector * ext * 0.9;
                primitives.Add(CurvePrimitive.Create(CurveSegment.Create(p0, p0 + vx)));
                primitives.Add(CurvePrimitive.Create(CurveSegment.Create(p0, p0 + vy)));
            }
            */

            {
                // show screen
                ScreenPoint sp1 = new ScreenPoint(10, 10);
                ScreenPoint sp2 = new ScreenPoint(Window.ActiveWindow.Size.Width - 10, Window.ActiveWindow.Size.Height - 10);
                Point p1;
                Single r = 1.0f;
                Line l;
                Vector v1;
                l = Window.ActiveContext.GetCursorRay(sp1);
                if (getPointUnderCursor(l, out p1, out v1))
                    primitives.Add(PointPrimitive.Create(p1, r));
                l = Window.ActiveContext.GetCursorRay(sp2);
                if (getPointUnderCursor(l, out p1, out v1))
                    primitives.Add(PointPrimitive.Create(p1, r));

            }

            // string msg = PlotCommands[0] + PlotCommands[1] + PlotCommands[2];
            // primitives.Add(TextPrimitive.Create(msg, LocationPoint.RightSide, pCur, 0, 0, new TextPadding(3)));

            var style = new GraphicStyle
            {
                LineColor = Color.Blue,
                FillColor = Color.Black,
                TextColor = Color.DarkGray,
                LineWidth = 2
            };
            Rendering = Graphic.Create(style, primitives);
        }


    }
}
