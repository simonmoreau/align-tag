using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.DB.Architecture;
using System.Globalization;
using System.Resources;

namespace AlignTag
{
    [Transaction(TransactionMode.Manual)]
    class AlignLeft : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Align align = new Align();

            return align.AlignElements(commandData, ref message, "Left");
        }
    }

    [Transaction(TransactionMode.Manual)]
    class AlignRight : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Align align = new Align();

            return align.AlignElements(commandData, ref message, "Right");
        }
    }

    [Transaction(TransactionMode.Manual)]
    class AlignTop : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Align align = new Align();

            return align.AlignElements(commandData, ref message, "Top");
        }
    }

    [Transaction(TransactionMode.Manual)]
    class AlignBottom : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Align align = new Align();

            return align.AlignElements(commandData, ref message, "Bottom");
        }
    }

    [Transaction(TransactionMode.Manual)]
    class DistributeHorizontally : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Align align = new Align();

            return align.DistributeElements(commandData, ref message, "Horizontally");
        }
    }

    [Transaction(TransactionMode.Manual)]
    class DistributeVertically : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Align align = new Align();

            return align.DistributeElements(commandData, ref message, "Vertically");
        }
    }

    //[Transaction(TransactionMode.Manual)]
    //class Arrange : IExternalCommand
    //{
    //    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    //    {
    //        Arrange arrange = new Arrange();

    //        return arrange.ArrangeElements(commandData, ref message);
    //    }
    //}
}
