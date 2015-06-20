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

            ////Retrive all tag in active view
            //List<IndependentTag> tags =
            //    new FilteredElementCollector(doc, activeView.Id).
            //    OfClass(typeof(IndependentTag)).WhereElementIsNotElementType().ToElements()
            //    .ToList().Cast<IndependentTag>().ToList();

            IEnumerable<IndependentTag> tags = from elem in new FilteredElementCollector(doc, activeView.Id).OfClass(typeof(IndependentTag)).WhereElementIsNotElementType()
                                               let type = elem as IndependentTag
                                               where type.HasLeader == true
                                               select type;

            tx.Start("Arrange Tags");

            //Create a list of location points for tag headers
            List<XYZ> headersPoints = CreateTagPositionPoints(activeView, tags, tx);

            //Create a list of TagLeader to sort them
            List<TagLeader> tagLeaders = new List<TagLeader>();

            foreach (IndependentTag tag in tags)
            {
                if (tag.HasLeader == true)
                {
                    TagLeader tagLeader = new TagLeader(tag, doc);
                    tagLeaders.Add(tagLeader);

                    //Find nearest point
                    XYZ nearestPoint = FindNearestPoint(headersPoints, tagLeader.LeaderStart);
                    tagLeader.Tag.TagHeadPosition = nearestPoint;

                    //remove this point from the list
                    headersPoints.Remove(nearestPoint);
                }
            }

            SortTag(tagLeaders);
            CreateElbow(tagLeaders, activeView);


            tx.Commit();

        }

        private void CreateElbow(List<TagLeader> tagLeaders, View activeView)
        {
            BoundingBoxXYZ bbox = activeView.CropBox;
            Transform viewTransform = bbox.Transform;

            foreach (TagLeader tagLeader in tagLeaders)
            {
                XYZ A = viewTransform.Inverse.OfPoint(tagLeader.Tag.TagHeadPosition);
                XYZ B = viewTransform.Inverse.OfPoint(tagLeader.LeaderStart);
                XYZ AB = B - A;
                double mult = AB.X * AB.Y;
                mult = mult / Math.Abs(mult);
                XYZ delta = new XYZ(AB.X - AB.Y * Math.Tan(mult * Math.PI / 4), 0, 0);
                XYZ elbowPosition = A + delta;
                elbowPosition = viewTransform.OfPoint(elbowPosition);

                tagLeader.Tag.LeaderElbow = elbowPosition;
            }
        }

        private void SortTag(List<TagLeader> tagLeaders)
        {
            foreach (TagLeader tagLeader in tagLeaders)
            {
                foreach (TagLeader secondTagLeader in tagLeaders)
                {
                    if (secondTagLeader != tagLeader)
                    {
                        //Check if leader cross
                        if (tagLeader.Leader.Intersect(secondTagLeader.Leader) == SetComparisonResult.Overlap)
                        {
                            //Invert two tags
                            XYZ leaderEnd = tagLeader.Tag.TagHeadPosition;
                            tagLeader.Tag.TagHeadPosition = secondTagLeader.Tag.TagHeadPosition;
                            secondTagLeader.Tag.TagHeadPosition = leaderEnd;
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



        private List<XYZ> CreateTagPositionPoints(View activeView, IEnumerable<IndependentTag> tags, Transaction tx)
        {
            if (!activeView.CropBoxActive)
            {
                throw new ErrorMessageException("Please set a crop box to the view");
            }

            ////Remove all leader
            foreach (IndependentTag tag in tags)
            {
                tag.LeaderEndCondition = LeaderEndCondition.Free;
                tag.LeaderEnd = tag.TagHeadPosition;
                //tag.HasLeader = false;
            }

            tx.Commit();
            tx.Start("Follow up");

            BoundingBoxXYZ bbox = activeView.CropBox;
            XYZ origin = activeView.Origin;
            Transform viewTransform = bbox.Transform;



            //Get largest tag dimension
            double tagHeight = 0;
            double tagWidth = 0;
            foreach (IndependentTag tag in tags)
            {

                BoundingBoxXYZ tagBox = tag.get_BoundingBox(activeView);
                BoundingBoxXYZ transformedTagBox = new BoundingBoxXYZ();
                transformedTagBox.Min = viewTransform.Inverse.OfPoint(tagBox.Min);
                transformedTagBox.Max = viewTransform.Inverse.OfPoint(tagBox.Max);

                //get largest width
                if (Math.Abs(transformedTagBox.Max.X - transformedTagBox.Min.X) > tagWidth)
                {
                    tagWidth = Math.Abs(transformedTagBox.Max.X - transformedTagBox.Min.X);
                }

                //get largest height
                if (Math.Abs(transformedTagBox.Max.Y - transformedTagBox.Min.Y) > tagHeight)
                {
                    tagHeight = Math.Abs(transformedTagBox.Max.Y - transformedTagBox.Min.Y);
                }

            }

            //replace all leader
            foreach (IndependentTag tag in tags)
            {
                //tag.HasLeader = true;
                tag.LeaderEndCondition = LeaderEndCondition.Attached;
            }


            List<XYZ> points = new List<XYZ>();
            double step = tagHeight * 1.5;
            //double step = (bbox.Max.Y - bbox.Min.Y) / 20;
            int max = (int)Math.Round((bbox.Max.Y - bbox.Min.Y) / (2 * step));

            //create sides points
            for (int i = 0; i < max; i++)
            {
                //Add left point
                points.Add(viewTransform.OfPoint(new XYZ(bbox.Min.X, step * i, 0)));

                //Add right point
                points.Add(viewTransform.OfPoint(new XYZ(bbox.Max.X, step * i, 0)));
            }

            return points;

        }
    }

    class TagLeader
    {
        private Document _doc;
        private View _currentView;

        public TagLeader(IndependentTag tag, Document doc)
        {
            _doc = doc;
            _currentView = _doc.GetElement(tag.OwnerViewId) as View;
            _tag = tag;
            GetTaggedElement();
            GetLeaderStart();

        }

        private Element _taggedElement;
        public Element TaggedElement
        {
            get { return _taggedElement; }
        }

        private IndependentTag _tag;
        public IndependentTag Tag
        {
            get { return _tag; }
        }

        private Line _leader;
        public Line Leader
        {
            get
            {
                _leader = Line.CreateBound(_leaderStart, _tag.TagHeadPosition);
                return _leader;
            }
        }

        private XYZ _elbow;
        public XYZ Elbow
        {
            get
            {
                Transform viewTransform = _currentView.CropBox.Transform;
                XYZ A = viewTransform.Inverse.OfPoint(_tag.TagHeadPosition);
                XYZ B = viewTransform.Inverse.OfPoint(_leaderStart);
                XYZ AB = B - A;
                double mult = AB.X * AB.Y;
                mult = mult / Math.Abs(mult);
                XYZ delta = new XYZ(AB.X - AB.Y * Math.Tan(mult * Math.PI / 4), 0, 0);
                _elbow = viewTransform.OfPoint(A + delta);
                return _elbow;
            }
        }

        private XYZ _leaderStart;
        public XYZ LeaderStart
        {
            get { return _leaderStart; }
        }

        private void GetTaggedElement()
        {
            if (_tag.TaggedElementId.HostElementId == ElementId.InvalidElementId)
            {
                RevitLinkInstance linkInstance = _doc.GetElement(_tag.TaggedElementId.LinkInstanceId) as RevitLinkInstance;
                Document linkedDocument = linkInstance.GetLinkDocument();

                _taggedElement = linkedDocument.GetElement(_tag.TaggedElementId.LinkedElementId);
            }
            else
            {
                _taggedElement = _doc.GetElement(_tag.TaggedElementId.HostElementId);
            }
        }

        private void GetLeaderStart()
        {
            BoundingBoxXYZ bbox = _taggedElement.get_BoundingBox(_currentView);
            BoundingBoxXYZ viewBox = _currentView.CropBox;

            //Retrive leader end
            XYZ leaderStart = (bbox.Max + bbox.Min) / 2;

            //Get leader end in view reference
            _leaderStart = viewBox.Transform.Inverse.OfPoint(leaderStart);
            _leaderStart = new XYZ(_leaderStart.X, _leaderStart.Y, 0);

            //Get leader end in global reference
            _leaderStart = viewBox.Transform.OfPoint(_leaderStart);
        }
    }
}
