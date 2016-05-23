using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Mechanical;

namespace AlignTag
{
    class AnnotationElement
    {
        public XYZ Center { get; set; }

        public XYZ UpLeft { get; set; }

        public XYZ UpRight { get; set; }

        public XYZ DownLeft { get; set; }

        public XYZ DownRight { get; set; }

        public Element Parent { get; set; }

        private Document _doc;
        private View _ownerView;

        public AnnotationElement(Element e)
        {
            Parent = e;
            _doc = e.Document;
            _ownerView = _doc.GetElement(e.OwnerViewId) as View;

            BoundingBoxXYZ BBox = e.get_BoundingBox(_ownerView);
            Transform trbb = BBox.Transform;

            XYZ max = _ownerView.CropBox.Transform.Inverse.OfPoint(BBox.Max);
            XYZ min = _ownerView.CropBox.Transform.Inverse.OfPoint(BBox.Min);
            if (max.X> min.X)
            {
                UpLeft = new XYZ(min.X, max.Y, max.Z);
                UpRight = new XYZ(max.X, max.Y, max.Z);
                DownLeft = new XYZ(min.X, min.Y, max.Z);
                DownRight = new XYZ(max.X, min.Y, max.Z);
            }
            else
            {
                UpLeft = new XYZ(max.X, max.Y, max.Z);
                UpRight = new XYZ(min.X, max.Y, max.Z);
                DownLeft = new XYZ(max.X, min.Y, max.Z);
                DownRight = new XYZ(min.X, min.Y, max.Z);
            }

            Center = (UpRight + DownLeft) / 2;
        }

        public AnnotationElement(Element e, XYZ offset)
        {
            Parent = e;
            _doc = e.Document;
            _ownerView = _doc.GetElement(e.OwnerViewId) as View;

            BoundingBoxXYZ BBox = e.get_BoundingBox(_ownerView);
            XYZ max = _ownerView.CropBox.Transform.Inverse.OfPoint(BBox.Max);
            XYZ min = _ownerView.CropBox.Transform.Inverse.OfPoint(BBox.Min);

            if (max.X > min.X)
            {
                UpLeft = new XYZ(min.X, max.Y, max.Z) + offset;
                UpRight = new XYZ(max.X, max.Y, max.Z) + offset;
                DownLeft = new XYZ(min.X, min.Y, max.Z) + offset;
                DownRight = new XYZ(max.X, min.Y, max.Z) + offset;
            }
            else
            {
                UpLeft = new XYZ(max.X, max.Y, max.Z) + offset;
                UpRight = new XYZ(min.X, max.Y, max.Z) + offset;
                DownLeft = new XYZ(max.X, min.Y, max.Z) + offset;
                DownRight = new XYZ(min.X, min.Y, max.Z) + offset;
            }

            Center = (UpRight + DownLeft) / 2;
        }

        public void MoveTo(XYZ point, AlignType alignType)
        {
            XYZ displacementVector = new XYZ();

            switch (alignType)
            {
                case AlignType.Left:
                    displacementVector = point - UpLeft;
                    break;
                case AlignType.Right:
                    displacementVector = point - UpRight;
                    break;
                case AlignType.Up:
                    displacementVector = point - UpRight;
                    break;
                case AlignType.Down:
                    displacementVector = point - DownRight;
                    break;
                case AlignType.Center:
                    displacementVector = point - Center;
                    break;
                case AlignType.Middle:
                    displacementVector = point - Center;
                    break;
                case AlignType.Verticaly:
                    displacementVector = point - Center;
                    break;
                case AlignType.Horizontaly:
                    displacementVector = point - Center;
                    break;
                default:
                    break;
            }

            Transform tr = Transform.CreateTranslation(_ownerView.CropBox.Transform.OfVector(displacementVector));

            if (Parent.GetType() == typeof(IndependentTag))
            {
                IndependentTag tag = Parent as IndependentTag;
                CustomLeader leader = new CustomLeader();
                if (tag.HasLeader && tag.LeaderEndCondition == LeaderEndCondition.Free)
                {
                    
                    leader = new CustomLeader(tag.LeaderEnd,new XYZ(0,0,0));
                }

                tag.TagHeadPosition = tr.OfPoint(tag.TagHeadPosition);

                if (tag.HasLeader && tag.LeaderEndCondition == LeaderEndCondition.Free)
                {
                    tag.LeaderEnd = leader.End;
                }

            }
            else if (Parent.GetType() == typeof(TextNote))
            {
                List<CustomLeader> leaders = new List<CustomLeader>();
                TextNote note = Parent as TextNote;
                if (note.LeaderCount != 0)
                {
                    foreach (Leader leader in note.GetLeaders())
                    {
                        leaders.Add(new CustomLeader(leader));
                    }
                }

                note.Coord = tr.OfPoint(note.Coord);

                if (leaders.Count != 0)
                {
                    int i = 0;
                    foreach (Leader leader in note.GetLeaders())
                    {
                        leader.End = leaders[i].End;
                        leader.Elbow = leaders[i].Elbow;
                    }
                }
            }
            else if (Parent.GetType() == typeof(RoomTag))
            {
                RoomTag tag = Parent as RoomTag;
                tag.Location.Move(displacementVector);
            }
            else if (Parent.GetType() == typeof(SpaceTag))
            {
                SpaceTag tag = Parent as SpaceTag;
                tag.Location.Move(displacementVector);
            }
        }
    }

    class CustomLeader
    {
        public XYZ End { get; set; }
        public XYZ Elbow { get; set; }

        public CustomLeader(Leader leader)
        {
            End = leader.End;
            Elbow = leader.Elbow;
        }

        public CustomLeader()
        {
            End = new XYZ(0, 0, 0);
            Elbow = new XYZ(0, 0, 0);
        }

        public CustomLeader(XYZ end, XYZ elbow)
        {
            End = end;
            Elbow = elbow;
        }
    }

    enum AlignType { Left, Right, Up, Down,Center, Middle, Verticaly, Horizontaly };


}
