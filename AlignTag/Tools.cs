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
    public class PlintePreprocessor : IFailuresPreprocessor
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
                if (failID == BuiltInFailures.OverlapFailures.WallsOverlap)
                {
                    failuresAccessor.DeleteWarning(failure);
                }
            }

            return FailureProcessingResult.Continue;
        }
    }

}
