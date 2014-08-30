#region Namespaces
using System;
using System.Collections.Generic;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Reflection;
using System.IO;
#endregion

namespace AlignTag
{
    class App : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication a)
        {

            //Create the panel for BIM42 Tools
            RibbonPanel AlignPanel = a.CreateRibbonPanel("Align");

            //Create icons in this panel
            Icons.CreateIcons(AlignPanel);

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication a)
        {
            return Result.Succeeded;
        }
    }

    class Icons
    {
        public static void CreateIcons(RibbonPanel bim42Panel)
        {
            //Retrive dll path
            string DllPath = Assembly.GetExecutingAssembly().Location;

            //Create contextual help
            ContextualHelp help = CreateContextualHelp("AlignHelp.chm");


            //Create align left button
            //AlignTag.ExecuteAlign.AlignBottom;
            //AlignTag.ExecuteAlign.AlignLeft;
            //AlignTag.ExecuteAlign.AlignRight;
            //AlignTag.ExecuteAlign.AlignTop;

            //Add Align Left Button
            PushButtonData alignLeftButton = new PushButtonData("alignLeftButton", "Align Left", DllPath, "AlignTag.AlignLeft");
            alignLeftButton.ToolTip = "Align Tags Left";
            alignLeftButton.LargeImage = RetriveImage("AlignTag.Resources.AlignLeftLarge.png");
            alignLeftButton.Image = RetriveImage("AlignTag.Resources.AlignLeftSmall.png");
            alignLeftButton.SetContextualHelp(help);

            //Add Align Right Button
            PushButtonData alignRightButton = new PushButtonData("alignRightButton", "Align Right", DllPath, "AlignTag.AlignRight");
            alignRightButton.ToolTip = "Align Tags Right";
            alignRightButton.LargeImage = RetriveImage("AlignTag.Resources.AlignRightLarge.png");
            alignRightButton.Image = RetriveImage("AlignTag.Resources.AlignRightSmall.png");
            alignRightButton.SetContextualHelp(help);

            //Add Align TOp Button
            PushButtonData alignTopButton = new PushButtonData("alignTopButton", "Align Top", DllPath, "AlignTag.AlignTop");
            alignTopButton.ToolTip = "Align Tags Top";
            alignTopButton.LargeImage = RetriveImage("AlignTag.Resources.AlignTopLarge.png");
            alignTopButton.Image = RetriveImage("AlignTag.Resources.AlignTopSmall.png");
            alignTopButton.SetContextualHelp(help);

            //Add Align bottom Button
            PushButtonData alignBottomButton = new PushButtonData("alignBottomButton", "Align Bottom", DllPath, "AlignTag.AlignBottom");
            alignBottomButton.ToolTip = "Align Tags Bottom";
            alignBottomButton.LargeImage = RetriveImage("AlignTag.Resources.AlignBottomLarge.png");
            alignBottomButton.Image = RetriveImage("AlignTag.Resources.AlignBottomSmall.png");
            alignBottomButton.SetContextualHelp(help);

            //Add Distribute horizontally Button
            PushButtonData distributeHorizontallyButton = new PushButtonData("distributeHorizontallyButton", "Distribute\nHorizontally", DllPath, "AlignTag.DistributeHorizontally");
            distributeHorizontallyButton.ToolTip = "Distribute Tags Horizontally";
            distributeHorizontallyButton.LargeImage = RetriveImage("AlignTag.Resources.DistributeHorizontallyLarge.png");
            distributeHorizontallyButton.Image = RetriveImage("AlignTag.Resources.DistributeHorizontallySmall.png");
            distributeHorizontallyButton.SetContextualHelp(help);

            //Add Distribute vertically Button
            PushButtonData distributeVerticallyButton = new PushButtonData("distributeVerticallyButton", "Distribute\nVertically", DllPath, "AlignTag.DistributeVertically");
            distributeVerticallyButton.ToolTip = "Distribute Tags Vertically";
            distributeVerticallyButton.LargeImage = RetriveImage("AlignTag.Resources.DistributeVerticallyLarge.png");
            distributeVerticallyButton.Image = RetriveImage("AlignTag.Resources.DistributeVerticallySmall.png");
            distributeVerticallyButton.SetContextualHelp(help);

            //Group align buttons
            SplitButtonData sbAlignData = new SplitButtonData("alignSplitButton", "Align Tags");
            SplitButton sbAlign = bim42Panel.AddItem(sbAlignData) as SplitButton;
            sbAlign.AddPushButton(alignLeftButton);
            sbAlign.AddPushButton(alignRightButton);
            sbAlign.AddPushButton(alignTopButton);
            sbAlign.AddPushButton(alignBottomButton);
            sbAlign.AddPushButton(distributeHorizontallyButton);
            sbAlign.AddPushButton(distributeVerticallyButton);

        }

        private static ImageSource RetriveImage(string imagePath)
        {
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(imagePath);

            switch (imagePath.Substring(imagePath.Length - 3))
            {
                case "jpg":
                    var jpgDecoder = new System.Windows.Media.Imaging.JpegBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                    return jpgDecoder.Frames[0];
                case "bmp":
                    var bmpDecoder = new System.Windows.Media.Imaging.BmpBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                    return bmpDecoder.Frames[0];
                case "png":
                    var pngDecoder = new System.Windows.Media.Imaging.PngBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                    return pngDecoder.Frames[0];
                case "ico":
                    var icoDecoder = new System.Windows.Media.Imaging.IconBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                    return icoDecoder.Frames[0];
                default:
                    return null;
            }
        }

        private static ContextualHelp CreateContextualHelp(string helpFile)
        {
            string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            //Get the english documentation
            string HelpName = helpFile;

            string HelpPath = Path.Combine(dir, HelpName);

            //if the help file does not exist, extract it in the HelpDirectory
                //Extract the english documentation
            
            Tools.ExtractRessource("AlignTag.Resources.AlignHelp.chm", HelpPath);

            return new ContextualHelp(ContextualHelpType.ChmFile, HelpPath);
        }
    }
}
