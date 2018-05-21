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


            //Add Align Left Button
            PushButtonData alignLeftButton = new PushButtonData("alignLeftButton", "Align Left", DllPath, "AlignTag.AlignLeft");
            alignLeftButton.ToolTip = "Align Tags or Elements Left";
            alignLeftButton.LargeImage = RetriveImage("AlignTag.Resources.AlignLeftLarge.png");
            alignLeftButton.Image = RetriveImage("AlignTag.Resources.AlignLeftSmall.png");
            alignLeftButton.SetContextualHelp(help);

            //Add Align Right Button
            PushButtonData alignRightButton = new PushButtonData("alignRightButton", "Align Right", DllPath, "AlignTag.AlignRight");
            alignRightButton.ToolTip = "Align Tags or Elements Right";
            alignRightButton.LargeImage = RetriveImage("AlignTag.Resources.AlignRightLarge.png");
            alignRightButton.Image = RetriveImage("AlignTag.Resources.AlignRightSmall.png");
            alignRightButton.SetContextualHelp(help);

            //Add Align TOp Button
            PushButtonData alignTopButton = new PushButtonData("alignTopButton", "Align Top", DllPath, "AlignTag.AlignTop");
            alignTopButton.ToolTip = "Align Tags or Elements Top";
            alignTopButton.LargeImage = RetriveImage("AlignTag.Resources.AlignTopLarge.png");
            alignTopButton.Image = RetriveImage("AlignTag.Resources.AlignTopSmall.png");
            alignTopButton.SetContextualHelp(help);

            //Add Align bottom Button
            PushButtonData alignBottomButton = new PushButtonData("alignBottomButton", "Align Bottom", DllPath, "AlignTag.AlignBottom");
            alignBottomButton.ToolTip = "Align Tags or Elements Bottom";
            alignBottomButton.LargeImage = RetriveImage("AlignTag.Resources.AlignBottomLarge.png");
            alignBottomButton.Image = RetriveImage("AlignTag.Resources.AlignBottomSmall.png");
            alignBottomButton.SetContextualHelp(help);

            //Add Align Center Button
            PushButtonData alignCenterButton = new PushButtonData("alignCenterButton", "Align Center", DllPath, "AlignTag.AlignCenter");
            alignCenterButton.ToolTip = "Align Tags or Elements Center";
            alignCenterButton.LargeImage = RetriveImage("AlignTag.Resources.AlignCenterLarge.png");
            alignCenterButton.Image = RetriveImage("AlignTag.Resources.AlignCenterSmall.png");
            alignCenterButton.SetContextualHelp(help);

            //Add Align Middle Button
            PushButtonData alignMiddleButton = new PushButtonData("alignMiddleButton", "Align Middle", DllPath, "AlignTag.AlignMiddle");
            alignMiddleButton.ToolTip = "Align Tags or Elements Middle";
            alignMiddleButton.LargeImage = RetriveImage("AlignTag.Resources.AlignMiddleLarge.png");
            alignMiddleButton.Image = RetriveImage("AlignTag.Resources.AlignMiddleSmall.png");
            alignMiddleButton.SetContextualHelp(help);

            //Add Distribute horizontally Button
            PushButtonData distributeHorizontallyButton = new PushButtonData("distributeHorizontallyButton", "Distribute\nHorizontally", DllPath, "AlignTag.DistributeHorizontally");
            distributeHorizontallyButton.ToolTip = "Distribute Tags or Elements Horizontally";
            distributeHorizontallyButton.LargeImage = RetriveImage("AlignTag.Resources.DistributeHorizontallyLarge.png");
            distributeHorizontallyButton.Image = RetriveImage("AlignTag.Resources.DistributeHorizontallySmall.png");
            distributeHorizontallyButton.SetContextualHelp(help);

            //Add Distribute vertically Button
            PushButtonData distributeVerticallyButton = new PushButtonData("distributeVerticallyButton", "Distribute\nVertically", DllPath, "AlignTag.DistributeVertically");
            distributeVerticallyButton.ToolTip = "Distribute Tags or Elements Vertically";
            distributeVerticallyButton.LargeImage = RetriveImage("AlignTag.Resources.DistributeVerticallyLarge.png");
            distributeVerticallyButton.Image = RetriveImage("AlignTag.Resources.DistributeVerticallySmall.png");
            distributeVerticallyButton.SetContextualHelp(help);

            //Add Arrange Button
            PushButtonData arrangeButton = new PushButtonData("ArrangeButton", "Arrange\nTags", DllPath, "AlignTag.Arrange");
            arrangeButton.ToolTip = "Arrange Tags around the view";
            arrangeButton.LargeImage = RetriveImage("AlignTag.Resources.ArrangeLarge.png");
            arrangeButton.Image = RetriveImage("AlignTag.Resources.ArrangeSmall.png");
            arrangeButton.SetContextualHelp(help);

            //Add Arrange Button
            PushButtonData untangleButton = new PushButtonData("UntangleButton", "Untangle", DllPath, "AlignTag.Untangle");
            untangleButton.ToolTip = "Untangle Tags or Elements ";
            untangleButton.LargeImage = RetriveImage("AlignTag.Resources.UntangleLarge.png");
            untangleButton.Image = RetriveImage("AlignTag.Resources.UntangleSmall.png");
            untangleButton.SetContextualHelp(help);

            bim42Panel.AddStackedItems(alignLeftButton, alignCenterButton, alignRightButton);
            bim42Panel.AddStackedItems(alignTopButton, alignMiddleButton, alignBottomButton);
            bim42Panel.AddStackedItems(distributeHorizontallyButton, distributeVerticallyButton, arrangeButton);
            bim42Panel.AddItem(untangleButton);

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

            FileInfo dllFileInfo = new FileInfo(Assembly.GetExecutingAssembly().Location);

            string helpFilePath = Path.Combine(dllFileInfo.Directory.Parent.FullName, "help.htm");

            FileInfo helpFileInfo = new FileInfo(helpFilePath);
            if (helpFileInfo.Exists)
            {
                return new ContextualHelp(ContextualHelpType.Url, helpFilePath);
            }
            else
            {
                string dirPath = dllFileInfo.Directory.FullName;
                //Get the english documentation
                string HelpName = helpFile;

                string HelpPath = Path.Combine(dirPath, HelpName);

                //if the help file does not exist, extract it in the HelpDirectory
                //Extract the english documentation

                Tools.ExtractRessource("AlignTag.Resources.AlignHelp.chm", HelpPath);

                return new ContextualHelp(ContextualHelpType.ChmFile, HelpPath);
            }
        }
    }
}
