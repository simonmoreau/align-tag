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
            using (TransactionGroup transGroup = new TransactionGroup(doc))
            {

                using (Transaction tx = new Transaction(doc))
                {
                    try
                    {
                        transGroup.Start("Arrange Tags");
                        // Add Your Code Here
                        ArrangeTag(UIdoc, tx);
                        transGroup.Assimilate();
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

        }

        private void ArrangeTag(UIDocument uidoc, Transaction tx)
        {
            Document doc = uidoc.Document;
            View activeView = doc.ActiveView;

            //Check the current view
            if (!activeView.CropBoxActive)
            {
                throw new ErrorMessageException("Please set a crop box to the view");
            }

            IEnumerable<IndependentTag> tags = from elem in new FilteredElementCollector(doc, activeView.Id).OfClass(typeof(IndependentTag)).WhereElementIsNotElementType()
                                               let type = elem as IndependentTag
                                               where type.HasLeader == true
                                               select type;

            tx.Start("Prepare Tags");

            //Remove all leader to find the correct tag height and width
            List<IndependentTag> independentTags = tags.ToList();
            foreach (IndependentTag tag in independentTags)
            {
                tag.LeaderEndCondition = LeaderEndCondition.Free;

#if REVIT2022 || REVIT2023 || REVIT2024 || REVIT2025 || REVIT2026 || REVIT2027
                Reference referencedElement = tag.GetTaggedReferences().FirstOrDefault();
                
                tag.SetLeaderElbow(referencedElement, tag.TagHeadPosition);
                tag.HasLeader = false;
#elif REVIT2019 || REVIT2020 || REVIT2021
                tag.LeaderEnd = tag.TagHeadPosition;
#endif

            }



            tx.Commit();
            tx.Start("Arrange Tags");

            //Create two lists of TagLeader
            List<TagWrapper> leftTagLeaders = new List<TagWrapper>();
            List<TagWrapper> rightTagLeaders = new List<TagWrapper>();

            foreach (IndependentTag tag in independentTags)
            {
                TagWrapper currentTag = new TagWrapper(tag, doc);
                if (currentTag.Side == ViewSides.Left)
                {
                    leftTagLeaders.Add(currentTag);
                }
                else
                {
                    rightTagLeaders.Add(currentTag);
                }
            }

            //Sort tag by Y position
            leftTagLeaders = leftTagLeaders.OrderBy(x => x.LeaderEnd.X).ToList();
            leftTagLeaders = leftTagLeaders.OrderBy(x => x.LeaderEnd.Y).ToList();

            //Create a list of potential location points for tag headers
            List<XYZ> leftTagHeadPoints = CreateTagPositionPoints(activeView, leftTagLeaders, ViewSides.Left);
            List<XYZ> rightTagHeadPoints = CreateTagPositionPoints(activeView, rightTagLeaders, ViewSides.Right);

            //place and sort
            PlaceAndSort(leftTagHeadPoints, leftTagLeaders);

            //Sort tag by Y position
            rightTagLeaders = rightTagLeaders.OrderByDescending(x => x.LeaderEnd.X).ToList();
            rightTagLeaders = rightTagLeaders.OrderBy(x => x.LeaderEnd.Y).ToList();

            //place and sort
            PlaceAndSort(rightTagHeadPoints, rightTagLeaders);

            tx.Commit();

        }

        private void PlaceAndSort(List<XYZ> positionPoints,List<TagWrapper> tags)
        {
            //place TagLeader
            foreach (TagWrapper tag in tags)
            {
                XYZ nearestPoint = FindNearestPoint(positionPoints, tag.TagCenter);
                tag.TagCenter = nearestPoint;

                //remove this point from the list
                positionPoints.Remove(nearestPoint);
            }

            //unCross leaders (2 times)
            UnCross(tags);
            UnCross(tags);

            //update their position
            foreach (TagWrapper tag in tags)
            {
                tag.UpdateTagPosition();
            }
        }

        private void UnCross(List<TagWrapper> tags)
        {
            foreach (TagWrapper tag in tags)
            {
                foreach (TagWrapper otherTag in tags)
                {
                    if (tag != otherTag)
                    {
                        if (AreTagLeadersIntersect(tag, otherTag))
                        {
                            XYZ newPosition = tag.TagCenter;
                            tag.TagCenter = otherTag.TagCenter;
                            otherTag.TagCenter = newPosition;
                        }
                    }
                }
            }
        }

        private bool AreTagLeadersIntersect(TagWrapper tag, TagWrapper otherTag)
        {
#if REVIT2019 || REVIT2020 || REVIT2021 || REVIT2022 || REVIT2023 || REVIT2024 || REVIT2025
            return tag.BaseLine.Intersect(otherTag.BaseLine) == SetComparisonResult.Overlap
                                        || tag.BaseLine.Intersect(otherTag.EndLine) == SetComparisonResult.Overlap
                                        || tag.EndLine.Intersect(otherTag.BaseLine) == SetComparisonResult.Overlap
                                        || tag.EndLine.Intersect(otherTag.EndLine) == SetComparisonResult.Overlap;
#elif REVIT2026 || REVIT2027 
            return tag.BaseLine.Intersect(otherTag.BaseLine, CurveIntersectResultOption.Simple ).Result == SetComparisonResult.Overlap
                                        || tag.BaseLine.Intersect(otherTag.EndLine, CurveIntersectResultOption.Simple ).Result == SetComparisonResult.Overlap
                                        || tag.EndLine.Intersect(otherTag.BaseLine, CurveIntersectResultOption.Simple ).Result == SetComparisonResult.Overlap
                                        || tag.EndLine.Intersect(otherTag.EndLine, CurveIntersectResultOption.Simple ).Result == SetComparisonResult.Overlap;
#endif

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

        private List<XYZ> CreateTagPositionPoints(View activeView, List<TagWrapper> tagLeaders, ViewSides side)
        {
            List<XYZ> points = new List<XYZ>();

            if (tagLeaders.Count == 0) return points;

            BoundingBoxXYZ bbox = activeView.CropBox;


            // Get the middle of the vertical line
            double middleHeight = (bbox.Min.Y + bbox.Max.Y) / 2;

            // Get the sum of all height of all tags
            double height = tagLeaders.Sum(t => t.TagHeight * 1.2);

            // Get the bottom Y position
            double bottomHeight = middleHeight - height / 2;

            XYZ basePoint = new XYZ(bbox.Max.X + 1, bbox.Min.Y, 0);

            if (side == ViewSides.Left)
            {
                //Add left point
                basePoint = new XYZ(bbox.Min.X - 1, bbox.Min.Y, 0);
            }

            // The tag are already sorted by altitude, we loop 
            // and start with the hightest one

            double altitude = bottomHeight;

            foreach (TagWrapper tagLeader in tagLeaders)
            {
                points.Add(basePoint + new XYZ(0, altitude, 0));

                altitude = altitude + tagLeader.TagHeight * 1.2;
            }

            return points;
        }
    }

    enum ViewSides { Left, Right };
}
