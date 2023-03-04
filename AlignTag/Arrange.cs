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
            foreach (IndependentTag tag in tags)
            {
                tag.LeaderEndCondition = LeaderEndCondition.Free;
                
#if Version2022 || Version2023 || Version2024
                Reference referencedElement = tag.GetTaggedReferences().FirstOrDefault();
                tag.SetLeaderElbow(referencedElement, tag.TagHeadPosition);
#elif Version2019 || Version2020 || Version2021
                tag.LeaderEnd = tag.TagHeadPosition;
#endif

            }



            tx.Commit();
            tx.Start("Arrange Tags");

            //Create two lists of TagLeader
            List<TagLeader> leftTagLeaders = new List<TagLeader>();
            List<TagLeader> rightTagLeaders = new List<TagLeader>();

            foreach (IndependentTag tag in tags)
            {
                TagLeader currentTag = new TagLeader(tag, doc);
                if (currentTag.Side == ViewSides.Left)
                {
                    leftTagLeaders.Add(currentTag);
                }
                else
                {
                    rightTagLeaders.Add(currentTag);
                }
            }

            //Create a list of potential location points for tag headers
            List<XYZ> leftTagHeadPoints = CreateTagPositionPoints(activeView, leftTagLeaders, ViewSides.Left);
            List<XYZ> rightTagHeadPoints = CreateTagPositionPoints(activeView, rightTagLeaders, ViewSides.Right);

            //Sort tag by Y position
            leftTagLeaders = leftTagLeaders.OrderBy(x => x.LeaderEnd.X).ToList();
            leftTagLeaders = leftTagLeaders.OrderBy(x => x.LeaderEnd.Y).ToList();

            //place and sort
            PlaceAndSort(leftTagHeadPoints, leftTagLeaders);


            //Sort tag by Y position
            rightTagLeaders = rightTagLeaders.OrderByDescending(x => x.LeaderEnd.X).ToList();
            rightTagLeaders = rightTagLeaders.OrderBy(x => x.LeaderEnd.Y).ToList();

            //place and sort
            PlaceAndSort(rightTagHeadPoints, rightTagLeaders);

            tx.Commit();

        }

        private void PlaceAndSort(List<XYZ> positionPoints,List<TagLeader> tags)
        {
            //place TagLeader
            foreach (TagLeader tag in tags)
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
            foreach (TagLeader tag in tags)
            {
                tag.UpdateTagPosition();
            }
        }

        private void UnCross(List<TagLeader> tags)
        {
            foreach (TagLeader tag in tags)
            {
                foreach (TagLeader otherTag in tags)
                {
                    if (tag != otherTag)
                    {
                        if (tag.BaseLine.Intersect(otherTag.BaseLine) == SetComparisonResult.Overlap
                            || tag.BaseLine.Intersect(otherTag.EndLine) == SetComparisonResult.Overlap
                            || tag.EndLine.Intersect(otherTag.BaseLine) == SetComparisonResult.Overlap
                            || tag.EndLine.Intersect(otherTag.EndLine) == SetComparisonResult.Overlap)
                        {
                            XYZ newPosition = tag.TagCenter;
                            tag.TagCenter = otherTag.TagCenter;
                            otherTag.TagCenter = newPosition;
                        }
                    }
                }
            }
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

        private List<XYZ> CreateTagPositionPoints(View activeView, List<TagLeader> tagLeaders, ViewSides side)
        {
            List<XYZ> points = new List<XYZ>();

            BoundingBoxXYZ bbox = activeView.CropBox;

            if (tagLeaders.Count != 0)
            {
                //Get largest tag dimension
                double tagHeight = tagLeaders.Max(x => x.TagHeight);
                double tagWidth = tagLeaders.Max(x => x.TagWidth);

                double step = tagHeight*1.2;
                //double step = (bbox.Max.Y - bbox.Min.Y) / 20;
                int max = (int)Math.Round((bbox.Max.Y - bbox.Min.Y) / step);
                XYZ baseRight = new XYZ(bbox.Max.X, bbox.Min.Y, 0);
                XYZ baseLeft = new XYZ(bbox.Min.X, bbox.Min.Y, 0);

                //create sides points
                for (int i = max*2; i > 0; i--)
                {
                    if (side == ViewSides.Left)
                    {
                        //Add left point
                        points.Add(baseLeft + new XYZ(0, step * i, 0));
                    }
                    else
                    {
                        //Add right point
                        points.Add(baseRight + new XYZ(0, step * i, 0));
                    }
                }
            }


            return points;
        }
    }

    class TagLeader
    {
        private Document _doc;
        private View _currentView;
        private Element _taggedElement;
        private IndependentTag _tag;

        public TagLeader(IndependentTag tag, Document doc)
        {
            _doc = doc;
            _currentView = _doc.GetElement(tag.OwnerViewId) as View;
            _tag = tag;

            _taggedElement = GetTaggedElement(_doc,_tag);
            _tagHeadPosition = _currentView.CropBox.Transform.Inverse.OfPoint(tag.TagHeadPosition);
            _tagHeadPosition = new XYZ(_tagHeadPosition.X, _tagHeadPosition.Y, 0);
            _leaderEnd = GetLeaderEnd(_taggedElement,_currentView);

            //View center
            XYZ viewCenter = (_currentView.CropBox.Max + _currentView.CropBox.Min) / 2;
            if (viewCenter.X > _leaderEnd.X)
            {
                _side = ViewSides.Left;
            }
            else
            {
                _side = ViewSides.Right;
            }

            GetTagDimension();
        }

        private XYZ _tagHeadPosition;
        private XYZ _headOffset;

        private XYZ _tagCenter;
        public XYZ TagCenter
        {
            get { return _tagCenter; }
            set
            {
                _tagCenter = value;
                UpdateLeaderPosition();
            }
        }

        private Line _endLine;
        public Line EndLine
        {
            get { return _endLine; }
        }

        private Line _baseLine;
        public Line BaseLine
        {
            get { return _baseLine; }
        }

        private ViewSides _side;
        public ViewSides Side
        {
            get { return _side; }
        }

        private XYZ _elbowPosition;
        public XYZ ElbowPosition
        {
            get { return _elbowPosition; }
        }

        private void UpdateLeaderPosition()
        {
            //Update elbow position
            XYZ AB = _leaderEnd - _tagCenter;
            double mult = AB.X * AB.Y;
            mult = mult / Math.Abs(mult);
            XYZ delta = new XYZ(AB.X - AB.Y * Math.Tan(mult * Math.PI / 4), 0, 0);
            _elbowPosition = _tagCenter + delta;

            //Update lines
            if (_leaderEnd.DistanceTo(_elbowPosition)> _doc.Application.ShortCurveTolerance)
            {
                _endLine = Line.CreateBound(_leaderEnd, _elbowPosition);
            }
            else
            {
                _endLine = Line.CreateBound(new XYZ(0, 0, 0), new XYZ(0, 0, 1));
            }
            if (_elbowPosition.DistanceTo(_tagCenter) > _doc.Application.ShortCurveTolerance)
            {
                _baseLine = Line.CreateBound(_elbowPosition, _tagCenter);
            }
            else
            {
                _baseLine = Line.CreateBound(new XYZ(0, 0, 0), new XYZ(0, 0, 1));
            }
        }

        private XYZ _leaderEnd;
        public XYZ LeaderEnd
        {
            get { return _leaderEnd; }
        }

        private double _tagHeight;
        public double TagHeight
        {
            get { return _tagHeight; }
        }

        private double _tagWidth;
        public double TagWidth
        {
            get { return _tagWidth; }
        }

        private void GetTagDimension()
        {
            BoundingBoxXYZ bbox = _tag.get_BoundingBox(_currentView);
            BoundingBoxXYZ viewBox = _currentView.CropBox;

            _tagHeight = viewBox.Transform.Inverse.OfPoint(bbox.Max).Y - viewBox.Transform.Inverse.OfPoint(bbox.Min).Y;
            _tagWidth = viewBox.Transform.Inverse.OfPoint(bbox.Max).X - viewBox.Transform.Inverse.OfPoint(bbox.Min).X;
            _tagCenter = (viewBox.Transform.Inverse.OfPoint(bbox.Max) + viewBox.Transform.Inverse.OfPoint(bbox.Min)) / 2;
            _tagCenter = new XYZ(_tagCenter.X, _tagCenter.Y, 0);
            _headOffset = _tagHeadPosition - _tagCenter;
        }

        public static Element GetTaggedElement(Document doc, IndependentTag tag)
        {
#if Version2019 || Version2020 || Version2021
            LinkElementId linkElementId = tag.TaggedElementId;
#elif Version2022 || Version2023 || Version2024
            LinkElementId linkElementId = tag.GetTaggedElementIds().FirstOrDefault();
#endif
            Element taggedElement;
            if (linkElementId.HostElementId == ElementId.InvalidElementId)
            {
                RevitLinkInstance linkInstance = doc.GetElement(linkElementId.LinkInstanceId) as RevitLinkInstance;
                Document linkedDocument = linkInstance.GetLinkDocument();

                taggedElement = linkedDocument.GetElement(linkElementId.LinkedElementId);
            }
            else
            {
                taggedElement = doc.GetElement(linkElementId.HostElementId);
            }

            return taggedElement;
        }

        public static XYZ GetLeaderEnd(Element taggedElement, View currentView)
        {
            BoundingBoxXYZ bbox = taggedElement.get_BoundingBox(currentView);
            BoundingBoxXYZ viewBox = currentView.CropBox;

            //Retrive leader end
            XYZ leaderEnd = new XYZ();
            if (bbox != null)
            {
                leaderEnd = (bbox.Max + bbox.Min) / 2;
            }
            else
            {
                leaderEnd = (viewBox.Max + viewBox.Min) / 2 + new XYZ(0.001, 0, 0);
            }

            //Get leader end in view reference
            leaderEnd = viewBox.Transform.Inverse.OfPoint(leaderEnd);
            leaderEnd = new XYZ(Math.Round(leaderEnd.X,4), Math.Round(leaderEnd.Y,4) ,0);

            return leaderEnd;
        }

        public void UpdateTagPosition()
        {
            _tag.LeaderEndCondition = LeaderEndCondition.Attached;

            XYZ offsetFromView = new XYZ();
            if (_side == ViewSides.Left)
            {
                offsetFromView = new XYZ(-Math.Abs(_tagWidth)*0.5-0.1, 0, 0);
            }
            else
            {
                offsetFromView = new XYZ(Math.Abs(_tagWidth )* 0.5+0.1, 0, 0);
            }


            _tag.TagHeadPosition = _currentView.CropBox.Transform.OfPoint(_headOffset + _tagCenter + offsetFromView);
#if Version2022 || Version2023 || Version2024
            Reference referencedElement = _tag.GetTaggedReferences().FirstOrDefault();
            _tag.SetLeaderElbow(referencedElement, _currentView.CropBox.Transform.OfPoint(_elbowPosition));

#elif Version2019 || Version2020 || Version2021
             _tag.LeaderElbow = _currentView.CropBox.Transform.OfPoint(_elbowPosition);
#endif

        }
    }

    enum ViewSides { Left, Right };
}
