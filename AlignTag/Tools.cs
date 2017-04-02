using System.Globalization;
using System.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;
using System.IO;

namespace AlignTag
{
    class Tools
    {
        //Define cultureInfo
        public static ResourceManager LangResMan;    // declare Resource manager to access to specific cultureinfo
        public static CultureInfo Cult;            // declare culture info

        public static void GetLocalisationValues()
        {
            //Create the culture for english
            Tools.Cult = CultureInfo.CreateSpecificCulture("en");
            Tools.LangResMan = new System.Resources.ResourceManager("BIM42.Revit2015.Resources.en", System.Reflection.Assembly.GetExecutingAssembly());
        }

        public static double? GetValueFromString(string text)
        {
            //Check the string value
            string heightValueString;
            double lenght;


            if (text.Contains(" mm"))
            {
                heightValueString = text.Replace(" mm", "");
            }
            else if (text.Contains("mm"))
            {
                heightValueString = text.Replace("mm", "");
            }
            else
            {
                heightValueString = text;
            }

            if (double.TryParse(heightValueString, out lenght))
            {
                return lenght;
            }
            else
            {
                return null;
            }

        }

        public static void ExtractRessource(string resourceName, string path)
        {
            using (Stream input = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            using (Stream output = File.Create(path))
            {

                // Insert null checking here for production
                byte[] buffer = new byte[8192];

                int bytesRead;
                while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    output.Write(buffer, 0, bytesRead);
                }

            }
        }

        internal static ICollection<ElementId> RevitReferencesToElementIds(Document doc, IList<Reference> selectedReferences)
        {
            return selectedReferences.Select(x => doc.GetElement(x).Id).ToList();
        }

        public static void CreateDebuggingSphere(Document doc, XYZ point, string value, Color color)
        {

            using (Transaction tx = new Transaction(doc))
            {

                tx.Start("Create Direct Shapes Spheres");

                Solid solid = CreateSphereAt(point, 0.5);

                CreateDirectShape(doc, solid, color, value + "{" + point.X + "," + point.Y + "," + point.Z + "}");

                tx.Commit();
            }
        }

        public static void DrawInView(Document doc, View view, XYZ point)
        {

            // Create a geometry plane
            
            XYZ origin = view.CropBox.Transform.Inverse.OfPoint(point);
            XYZ normal = view.ViewDirection;

            Plane geomPlane = Plane.CreateByNormalAndOrigin(normal, origin);

            //Create a circle
            Arc geomCircle = Arc.Create(origin, 10, 0, 2.0 * Math.PI, geomPlane.XVec, geomPlane.YVec);


            // Create a sketch plane in current document

            SketchPlane.Create(doc, geomPlane);

            // Create a DetailLine element using the 
            // newly created geometry line and sketch plane

            DetailLine line = doc.Create.NewDetailCurve(view, geomCircle) as DetailLine;


        }

        public void DrawInViewCoordinates(Document doc)
        {
            View view = doc.ActiveView;

            //0 of the view
            XYZ origin = new XYZ(0, 0, 0);
            //In the model reference
            XYZ viewOriginInodel = view.CropBox.Transform.OfPoint(origin);
            Solid originSolid = CreateSphereAt(viewOriginInodel, 0.1);
            CreateDirectShape(doc, originSolid, new Color(0, 255, 0), "View Origin");


            origin = new XYZ(1, 0, 0);
            //In the model reference
            viewOriginInodel = view.CropBox.Transform.OfPoint(origin);
            originSolid = CreateSphereAt(viewOriginInodel, 0.1);
            CreateDirectShape(doc, originSolid, new Color(0, 255, 0), "View X");

            origin = new XYZ(0, 1, 0);
            //In the model reference
            viewOriginInodel = view.CropBox.Transform.OfPoint(origin);
            originSolid = CreateSphereAt(viewOriginInodel, 0.1);
            CreateDirectShape(doc, originSolid, new Color(0, 255, 0), "View Y");

            origin = new XYZ(0, 0, 1);
            //In the model reference
            viewOriginInodel = view.CropBox.Transform.OfPoint(origin);
            originSolid = CreateSphereAt(viewOriginInodel, 0.1);
            CreateDirectShape(doc, originSolid, new Color(0, 255, 0), "View Z");

        }



        private static void CreateDirectShape(Document doc, Solid solid, Color color, string paramValue)
        {
            OverrideGraphicSettings ogs = new OverrideGraphicSettings();
            ogs.SetProjectionFillColor(color); //new Color(0,255,0)
            ogs.SetProjectionFillPatternId(new ElementId(4));
            ogs.SetProjectionFillPatternVisible(true);

            // create direct shape and assign the sphere shape
            DirectShape dsmax = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));

            dsmax.ApplicationId = "ApplicationID";
            dsmax.ApplicationDataId = "ApplicationDataId";

            dsmax.SetShape(new GeometryObject[] { solid });
            doc.ActiveView.SetElementOverrides(dsmax.Id, ogs);

            Parameter parameter = dsmax.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
            parameter.Set(paramValue);
        }

        /// <summary>
        /// Create and return a solid sphere with
        /// a given radius and centre point.
        /// </summary>
        private static Solid CreateSphereAt(XYZ centre, double radius)
        {
            // Use the standard global coordinate system
            // as a frame, translated to the sphere centre.

            Frame frame = new Frame(centre, XYZ.BasisX, XYZ.BasisY, XYZ.BasisZ);

            // Create a vertical half-circle loop;
            // this must be in the frame location.

            Arc arc = Arc.Create(centre - radius * XYZ.BasisZ, centre + radius * XYZ.BasisZ, centre + radius * XYZ.BasisX);

            Line line = Line.CreateBound(arc.GetEndPoint(1), arc.GetEndPoint(0));

            CurveLoop halfCircle = new CurveLoop(); halfCircle.Append(arc); halfCircle.Append(line);

            List<CurveLoop> loops = new List<CurveLoop>(1);

            loops.Add(halfCircle);

            return GeometryCreationUtilities.CreateRevolvedGeometry(frame, loops, 0, 2 * Math.PI);
        }

    }

    /// <summary>
    /// Retrive the error message for displaying it in the Revit interface
    /// </summary>
    public class ErrorMessageException : ApplicationException
    {
        /// <summary>
        /// constructor entirely using baseclass'
        /// </summary>
        public ErrorMessageException()
            : base()
        {
        }

        /// <summary>
        /// constructor entirely using baseclass'
        /// </summary>
        /// <param name="message">error message</param>
        public ErrorMessageException(String message)
            : base(message)
        {
        }
    }


    /// <summary>
    /// Manage Warning in the Revit interface
    /// </summary>
    public class CommitPreprocessor : IFailuresPreprocessor
    {
        public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
        {
            IList<FailureMessageAccessor> failList = new List<FailureMessageAccessor>();
            // Inside event handler, get all warnings
            failList = failuresAccessor.GetFailureMessages();
            foreach (FailureMessageAccessor failure in failList)
            {
                // check FailureDefinitionIds against ones that you want to dismiss,
                FailureDefinitionId failID = failure.GetFailureDefinitionId();
                // prevent Revit from showing Unenclosed room warnings
                if (failID == BuiltInFailures.RoomFailures.RoomTagNotInRoom ||
                    failID == BuiltInFailures.RoomFailures.RoomTagNotInRoomToArea ||
                    failID == BuiltInFailures.RoomFailures.RoomTagNotInRoomToRoom ||
                    failID == BuiltInFailures.RoomFailures.RoomTagNotInRoomToSpace)
                {
                    failuresAccessor.DeleteWarning(failure);
                }
            }

            return FailureProcessingResult.Continue;
        }
    }

}
