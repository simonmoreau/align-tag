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

            return align.AlignElements(commandData, ref message, AlignType.Left);
        }
    }

    [Transaction(TransactionMode.Manual)]
    class AlignRight : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Align align = new Align();

            return align.AlignElements(commandData, ref message, AlignType.Right);
        }
    }

    [Transaction(TransactionMode.Manual)]
    class AlignTop : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Align align = new Align();

            return align.AlignElements(commandData, ref message, AlignType.Up);
        }
    }

    [Transaction(TransactionMode.Manual)]
    class AlignBottom : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Align align = new Align();

            return align.AlignElements(commandData, ref message, AlignType.Down);
        }
    }

    [Transaction(TransactionMode.Manual)]
    class AlignCenter : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Align align = new Align();

            return align.AlignElements(commandData, ref message, AlignType.Center);
        }
    }

    [Transaction(TransactionMode.Manual)]
    class AlignMiddle : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Align align = new Align();

            return align.AlignElements(commandData, ref message, AlignType.Middle);
        }
    }

    [Transaction(TransactionMode.Manual)]
    class DistributeHorizontally : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Align align = new Align();

            return align.AlignElements(commandData, ref message, AlignType.Horizontaly);
        }
    }

    [Transaction(TransactionMode.Manual)]
    class DistributeVertically : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Align align = new Align();

            return align.AlignElements(commandData, ref message, AlignType.Verticaly);
        }
    }

    [Transaction(TransactionMode.Manual)]
    class Untangle : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Align align = new Align();

            return align.AlignElements(commandData, ref message, AlignType.Untangle);
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
