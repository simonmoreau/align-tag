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


            if (_doc.GetElement(e.OwnerViewId) != null)
            {
                _ownerView = _doc.GetElement(e.OwnerViewId) as View;
            }
            else
            {
                _ownerView = _doc.ActiveView;
            }

            //Create the view plan
            Plane viewPlane = Plane.CreateByNormalAndOrigin(_ownerView.ViewDirection, _ownerView.Origin);

            BoundingBoxXYZ elementBBox = e.get_BoundingBox(_ownerView);

            XYZ globalMax = elementBBox.Max;
            XYZ globalMin = elementBBox.Min;
            double distanceProjected = ProjectedDistance(viewPlane, globalMax, globalMin);

            XYZ alternateMax = new XYZ(globalMax.X, globalMin.Y, globalMax.Z);
            XYZ alternateMin = new XYZ(globalMin.X, globalMax.Y, globalMin.Z);
            double alternateDistanceProjected = ProjectedDistance(viewPlane, alternateMax, alternateMin);

            if (alternateDistanceProjected > distanceProjected)
            {
                globalMax = alternateMax;
                globalMin = alternateMin;
            }

            Transform ownerViewTransform = _ownerView.CropBox.Transform;
            XYZ max = ownerViewTransform.Inverse.OfPoint(globalMax); //Max in the coordinate space of the view
            XYZ min = ownerViewTransform.Inverse.OfPoint(globalMin); //Min in the coordinate space of the view

            UpLeft = new XYZ(GetMin(min.X, max.X), GetMax(max.Y, min.Y), 0);
            UpRight = new XYZ(GetMax(min.X, max.X), GetMax(max.Y, min.Y), 0);
            DownLeft = new XYZ(GetMin(min.X, max.X), GetMin(max.Y, min.Y), 0);
            DownRight = new XYZ(GetMax(min.X, max.X), GetMin(max.Y, min.Y), 0);

            Center = (UpRight + DownLeft) / 2;

        }

        private double ProjectedDistance(Plane plane, XYZ pointA, XYZ pointB)
        {
            UV UVA = new UV();
            UV UVB = new UV();

            plane.Project(pointA, out UVA, out double d);
            plane.Project(pointB, out UVB, out d);

            return UVA.DistanceTo(UVB);
        }

        private double GetMax(double value1, double value2)
        {
            if (value1 >= value2)
            {
                return value1;
            }
            else
            {
                return value2;
            }
        }

        private double GetMin(double value1, double value2)
        {
            if (value1 >= value2)
            {
                return value2;
            }
            else
            {
                return value1;
            }
        }

        public XYZ GetLeaderEnd()
        {
            XYZ LeaderEnd = new XYZ();
            Element e = this.Parent;
            //Find the leader end, if any
            if (e.GetType() == typeof(IndependentTag))
            {
                IndependentTag tag = e as IndependentTag;
                if (tag.HasLeader)
                {
                    if (tag.LeaderEndCondition == LeaderEndCondition.Free)
                    {
                        LeaderEnd = tag.LeaderEnd;
                    }
                    else
                    {
                        Element taggedElement = TagLeader.GetTaggedElement(_doc, tag);
                        LeaderEnd = TagLeader.GetLeaderEnd(taggedElement, _ownerView);
                    }
                }
            }
            else if (e.GetType() == typeof(TextNote))
            {
                TextNote note = e as TextNote;
                if (note.LeaderCount != 0)
                {
                    LeaderEnd = note.GetLeaders().FirstOrDefault().End;
                }

            }
            else if (e.GetType().IsSubclassOf(typeof(SpatialElementTag)))
            {
                SpatialElementTag tag = e as SpatialElementTag;

                if (tag.HasLeader)
                {
                    LeaderEnd = tag.LeaderEnd;
                }
            }
            else
            {
                LeaderEnd = Center;
            }

            return LeaderEnd;
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
                case AlignType.Vertically:
                    displacementVector = point - Center;
                    break;
                case AlignType.Horizontally:
                    displacementVector = point - Center;
                    break;
                case AlignType.UntangleVertically:
                    displacementVector = point - UpLeft;
                    break;
                case AlignType.UntangleHorizontally:
                    displacementVector = point - UpLeft;
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

                    leader = new CustomLeader(tag.LeaderEnd, new XYZ(0, 0, 0));
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
                        i++;
                    }
                }
            }
            else if (Parent.GetType().IsSubclassOf(typeof(SpatialElementTag)))
            {
                SpatialElementTag tag = Parent as SpatialElementTag;

                CustomLeader leader = new CustomLeader();
                if (tag.HasLeader)
                {
                    leader = new CustomLeader(tag.LeaderEnd, new XYZ(0, 0, 0));
                }

                tag.TagHeadPosition = tr.OfPoint(tag.TagHeadPosition);

                if (tag.HasLeader)
                {
                    tag.LeaderEnd = leader.End;
                }
            }
            else
            {
                Parent.Location.Move(_ownerView.CropBox.Transform.OfVector(displacementVector));
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

    enum AlignType { Left, Right, Up, Down, Center, Middle, Verticaly, Horizontaly, Untangle };

    class OffsetedElement
    {
        public OffsetedElement(Element e, XYZ offset)
        {
            Element = e;
            Offset = offset;
        }

        public Element Element { get; set; }
        public XYZ Offset { get; set; }
    }
}
