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
            View activeView = doc.ActiveView;

            //Retrive all tag in active view
            List<IndependentTag> tags =
                new FilteredElementCollector(doc, activeView.Id).
                OfClass(typeof(IndependentTag)).WhereElementIsNotElementType().ToElements()
                .ToList().Cast<IndependentTag>().ToList();


            //Create a list of location points for tag headers
            List<XYZ> headersPoints = CreateTagPositionPoints(activeView, tags);

            //Create a list of TagLeader to sort them
            List<TagLeader> tagLeaders = new List<TagLeader>();

            tx.Start("Arrange Tags");

            foreach (IndependentTag tag in tags)
            {
                
                if (tag.HasLeader)
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

            foreach (TagLeader tagLeader in tagLeaders)
            {
                foreach (TagLeader secondTagLeader in tagLeaders)
                {
                    if (secondTagLeader != tagLeader)
                    {
                        //Check if leader cross
                        if (tagLeader.Leader.Intersect(secondTagLeader.Leader) == SetComparisonResult.Overlap)
                        {
                            //Invert two tag
                            XYZ leaderEnd = tagLeader.Tag.TagHeadPosition;
                            tagLeader.Tag.TagHeadPosition = secondTagLeader.Tag.TagHeadPosition;
                            secondTagLeader.Tag.TagHeadPosition = leaderEnd;
                        }
                    }
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

        private List<XYZ> CreateTagPositionPoints(View activeView, List<IndependentTag> tags)
        {
            if (!activeView.CropBoxActive)
            {
                throw new ErrorMessageException("Please set a crop box to the view");
            }

            //Get largest tag dimension
            double tagHeight = 0;
            double tagWidth = 0;
            foreach (IndependentTag tag in tags)
            {
                BoundingBoxXYZ tagBox = tag.get_BoundingBox(activeView);
                //get largest width
                if (Math.Abs(tagBox.Max.X - tagBox.Min.X)>tagWidth )
                {
                    tagWidth = Math.Abs(tagBox.Max.X - tagBox.Min.X);
                }

                //get largest height
                if (Math.Abs(tagBox.Max.Y - tagBox.Min.Y) > tagHeight)
                {
                    tagHeight = Math.Abs(tagBox.Max.Y - tagBox.Min.Y);
                }
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
