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
                        transGroup.Start("Transaction Group");
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
                tag.LeaderEnd = tag.TagHeadPosition;
            }

            tx.Commit();
            tx.Start("Arrange Tags");

            //Create a list of TagLeader
            List<TagLeader> tagLeaders = new List<TagLeader>();
            foreach (IndependentTag tag in tags)
            {
                tagLeaders.Add(new TagLeader(tag, doc));
            }

            //Create a list of location points for tag headers
            List<XYZ> tagHeadPoints = CreateTagPositionPoints(activeView,tagLeaders);

            //place TagLeader
            foreach (TagLeader tag in tagLeaders)
            {
                //Find nearest point
                XYZ nearestPoint = FindNearestPoint(tagHeadPoints, tag.TagHeadPosition);
                tag.TagHeadPosition = nearestPoint;

                //remove this point from the list
                tagHeadPoints.Remove(nearestPoint);
            }

            //replace all leaders before commiting
            foreach (TagLeader tag in tagLeaders)
            {
                tag.UpdateTagPosition();
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

        private List<XYZ> CreateTagPositionPoints(View activeView, List<TagLeader> tagLeaders)
        {
            BoundingBoxXYZ bbox = activeView.CropBox;

            //Get largest tag dimension
            double tagHeight = tagLeaders.Max(x => x.TagHeight);
            double tagWidth = tagLeaders.Max(x => x.TagWidth);

            List<XYZ> points = new List<XYZ>();
            double step = tagHeight * 1.5;
            //double step = (bbox.Max.Y - bbox.Min.Y) / 20;
            int max = (int)Math.Round((bbox.Max.Y - bbox.Min.Y) / (2 * step));

            //create sides points
            for (int i = 0; i < max; i++)
            {
                //Add left point
                points.Add(new XYZ(bbox.Min.X, step * i, 0));

                //Add right point
                points.Add(new XYZ(bbox.Max.X, step * i, 0));
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

            _taggedElement = GetTaggedElement();
            _tagHeadPosition = _currentView.CropBox.Transform.Inverse.OfPoint(tag.TagHeadPosition);
            _tagHeadPosition = new XYZ(_tagHeadPosition.X, _tagHeadPosition.Y, 0);
            _leaderEnd = GetLeaderEnd();
            GetTagDimension();
        }

        private XYZ _tagHeadPosition;
        public XYZ TagHeadPosition
        {
            get { return _tagHeadPosition; }
            set 
            {
                _tagHeadPosition = value; 
                UpdateElbowPosition();
            }
        }

        private XYZ _elbowPosition;
        public XYZ ElbowPosition
        {
            get { return _elbowPosition; }
        }

        private void UpdateElbowPosition()
        {
                //Update elbow position
                XYZ AB = _leaderEnd - _tagHeadPosition;
                double mult = AB.X * AB.Y;
                mult = mult / Math.Abs(mult);
                XYZ delta = new XYZ(AB.X - AB.Y * Math.Tan(mult * Math.PI / 4), 0, 0);
                _elbowPosition = _tagHeadPosition + delta;
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

        private Element GetTaggedElement()
        {
            Element taggedElement;
            if (_tag.TaggedElementId.HostElementId == ElementId.InvalidElementId)
            {
                RevitLinkInstance linkInstance = _doc.GetElement(_tag.TaggedElementId.LinkInstanceId) as RevitLinkInstance;
                Document linkedDocument = linkInstance.GetLinkDocument();

                taggedElement = linkedDocument.GetElement(_tag.TaggedElementId.LinkedElementId);
            }
            else
            {
                taggedElement = _doc.GetElement(_tag.TaggedElementId.HostElementId);
            }

            return taggedElement;
        }

        private void GetTagDimension()
        {
            BoundingBoxXYZ bbox = _tag.get_BoundingBox(_currentView);
            BoundingBoxXYZ viewBox = _currentView.CropBox;

            _tagHeight = viewBox.Transform.Inverse.OfPoint(bbox.Max).Y - viewBox.Transform.Inverse.OfPoint(bbox.Min).Y;
            _tagWidth = viewBox.Transform.Inverse.OfPoint(bbox.Max).X - viewBox.Transform.Inverse.OfPoint(bbox.Min).X;
        }

        private XYZ GetLeaderEnd()
        {
            BoundingBoxXYZ bbox = _taggedElement.get_BoundingBox(_currentView);
            BoundingBoxXYZ viewBox = _currentView.CropBox;

            //Retrive leader end
            XYZ leaderStart = (bbox.Max + bbox.Min) / 2;

            //Get leader end in view reference
            leaderStart = viewBox.Transform.Inverse.OfPoint(leaderStart);
            leaderStart = new XYZ(leaderStart.X, leaderStart.Y, 0);

            return leaderStart;
        }

        public void UpdateTagPosition()
        {
            _tag.LeaderEndCondition = LeaderEndCondition.Attached;
            _tag.TagHeadPosition = _currentView.CropBox.Transform.OfPoint(_tagHeadPosition);
            _tag.LeaderElbow = _currentView.CropBox.Transform.OfPoint(_elbowPosition);
        }
    }
}
