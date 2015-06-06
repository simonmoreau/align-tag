using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;

namespace AlignTag
{
    [Transaction(TransactionMode.Manual)]
    class Arrange : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument UIdoc = commandData.Application.ActiveUIDocument;
            Document doc = UIdoc.Document;

            using (Transaction tx = new Transaction(doc))
            {
                try
                {
                    // Add Your Code Here
                    ArrangeTag(UIdoc, tx);
                    // Return Success
                    return Result.Succeeded;
                }

                catch (Autodesk.Revit.Exceptions.OperationCanceledException exceptionCanceled)
                {
                    message = exceptionCanceled.Message;
                    if (tx.HasStarted())
                    {
                        tx.RollBack();
                    }
                    return Autodesk.Revit.UI.Result.Cancelled;
                }
                catch (ErrorMessageException errorEx)
                {
                    // checked exception need to show in error messagebox
                    message = errorEx.Message;
                    if (tx.HasStarted())
                    {
                        tx.RollBack();
                    }
                    return Autodesk.Revit.UI.Result.Failed;
                }
                catch (Exception ex)
                {
                    // unchecked exception cause command failed
                    message = ex.Message;
                    //Trace.WriteLine(ex.ToString());
                    if (tx.HasStarted())
                    {
                        tx.RollBack();
                    }
                    return Autodesk.Revit.UI.Result.Failed;
                }
            }
        }

        private void ArrangeTag(UIDocument uidoc, Transaction tx)
        {
            Document doc = uidoc.Document;
            //Get view center
            View activeView = doc.ActiveView;
            XYZ viewCenter = activeView.Origin;

            //Retrive all tag in active view
            List<IndependentTag> tags =
                new FilteredElementCollector(doc, activeView.Id).
                OfClass(typeof(IndependentTag)).WhereElementIsNotElementType().ToElements()
                .ToList().Cast<IndependentTag>().ToList();

            //Create a list of location points for tag headers
            List<XYZ> headersPoints = CreateTagPositionPoints(activeView);

            tx.Start("Arrange Tags");

            foreach (IndependentTag tag in tags)
            {
                if (tag.HasLeader)
                {

                    Element taggedElement;
                    if (tag.TaggedElementId.HostElementId == ElementId.InvalidElementId)
                    {
                        RevitLinkInstance linkInstance = doc.GetElement(tag.TaggedElementId.LinkInstanceId) as RevitLinkInstance;
                        Document linkedDocument = linkInstance.GetLinkDocument();

                        taggedElement = linkedDocument.GetElement(tag.TaggedElementId.LinkedElementId);
                    }
                    else
                    {
                        taggedElement = doc.GetElement(tag.TaggedElementId.HostElementId);
                    }

                    

                    BoundingBoxXYZ bbox = taggedElement.get_BoundingBox(activeView);

                    //XYZ leaderEnd = tag.LeaderEnd;
                    XYZ leaderEnd = (bbox.Max + bbox.Min) / 2;

                    //Find nearest point
                    XYZ nearestPoint = FindNearestPoint(headersPoints, leaderEnd);
                    tag.TagHeadPosition = nearestPoint;

                    //remove this point from the list
                    headersPoints.Remove(nearestPoint);
                    //int rank = headersPoints.IndexOf(nearestPoint);

                    //tag.TagHeadPosition = viewCenter + 20 * (leaderEnd - viewCenter);
                }


            }

            tx.Commit();

        }

        private XYZ FindNearestPoint(List<XYZ> points, XYZ basePoint)
        {
            XYZ nearestPoint = points.FirstOrDefault();
            double nearestDistance = basePoint.DistanceTo(nearestPoint);
            double currentDistance = basePoint.DistanceTo(nearestPoint);

            foreach (XYZ point in points)
            {
                currentDistance = basePoint.DistanceTo(point);
                if (currentDistance < nearestDistance)
                {
                    nearestPoint = point;
                    nearestDistance = basePoint.DistanceTo(point);
                }
            }


            return nearestPoint;
        }

        private List<XYZ> CreateTagPositionPoints(View activeView)
        {
            if (!activeView.CropBoxActive)
            {
                throw new ErrorMessageException("Please set a crop box to the view");
            }

            BoundingBoxXYZ bbox = activeView.CropBox;
            XYZ origin = activeView.Origin;
            Transform viewTransform = bbox.Transform;
            
            List<XYZ> points = new List<XYZ>();
            double step = (bbox.Max.Y - bbox.Min.Y) / 10;

            //create sides points
            for (int i = 0; i < 10; i++)
            {
                //Add left point
                points.Add(viewTransform.OfPoint(new XYZ(bbox.Min.X, step * i, 0)));

                //Add right point
                points.Add(viewTransform.OfPoint(new XYZ(bbox.Max.X, step * i, 0)));
            }

            return points;

        }

    }
}
