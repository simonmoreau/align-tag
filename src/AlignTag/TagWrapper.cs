using System;
using System.Linq;
using Autodesk.Revit.DB;

namespace AlignTag
{
    class TagWrapper
    {
        private Document _doc;
        private View _currentView;
        private Element _taggedElement;
        private IndependentTag _tag;

        public TagWrapper(IndependentTag tag, Document doc)
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
#if REVIT2019 || REVIT2020 || REVIT2021
            LinkElementId linkElementId = tag.TaggedElementId;
#elif REVIT2022 || REVIT2023 || REVIT2024 || REVIT2025 || REVIT2026 || REVIT2027
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
#if REVIT2022 || REVIT2023 || REVIT2024 || REVIT2025 || REVIT2026 || REVIT2027
            Reference referencedElement = _tag.GetTaggedReferences().FirstOrDefault();
            _tag.HasLeader = true;
            _tag.SetLeaderElbow(referencedElement, _currentView.CropBox.Transform.OfPoint(_elbowPosition));

#elif REVIT2019 || REVIT2020 || REVIT2021
             _tag.LeaderElbow = _currentView.CropBox.Transform.OfPoint(_elbowPosition);
#endif

        }
    }
}
