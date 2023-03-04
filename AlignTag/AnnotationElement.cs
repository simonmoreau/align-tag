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

        public AnnotationElement(PreparationElement preparationElement)
        {
            Element e = preparationElement.Element;
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

            if (preparationElement.DisplacementVector != null)
            {
                globalMax = elementBBox.Max - preparationElement.DisplacementVector;
                globalMin = elementBBox.Min - preparationElement.DisplacementVector;
            }

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

        public override string ToString()
        {
            string[] Valueparams = new string[] {Parent.Id.ToString(),
                UpLeft.X.ToString(), UpLeft.Y.ToString(), UpLeft.Z.ToString(),
                UpRight.X.ToString(), UpRight.Y.ToString(), UpRight.Z.ToString(),
                DownLeft.X.ToString(), DownLeft.Y.ToString(), DownLeft.Z.ToString(),
                DownRight.X.ToString(), DownRight.Y.ToString(), DownRight.Z.ToString(),
                Center.X.ToString(), Center.Y.ToString(), Center.Z.ToString() };

            return String.Join(",", Valueparams);
        }

        private double ProjectedDistance(Plane plane, XYZ pointA, XYZ pointB)
        {
            //To be tested
            XYZ UVA = ProjectionOnPlane(pointA, plane);
            XYZ UVB = ProjectionOnPlane(pointB, plane);

            return UVA.DistanceTo(UVB);
        }

        private XYZ ProjectionOnPlane(XYZ q, Plane plane)
        {
            //The projection of a point q = (x, y, z) onto a plane given by a point p = (a, b, c) and a normal n = (d, e, f) is
            //    q_proj = q - dot(q - p, n) * n

            XYZ p = plane.Origin;
            XYZ n = plane.Normal.Normalize();
            XYZ q_proj = q - n.Multiply(n.DotProduct(q - p));

            return q_proj;
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
            XYZ LeaderEnd = this.Center;
            Element e = this.Parent;
            //Find the leader end, if any
            if (e.GetType() == typeof(IndependentTag))
            {
                IndependentTag tag = e as IndependentTag;
                if (tag.HasLeader)
                {
                    if (tag.LeaderEndCondition == LeaderEndCondition.Free)
                    {
#if Version2022 || Version2023 || Version2024
                        Reference referencedElement = tag.GetTaggedReferences().FirstOrDefault();
                        if (referencedElement != null) LeaderEnd = tag.GetLeaderEnd(referencedElement);
#elif Version2019 || Version2020 || Version2021
                        LeaderEnd = tag.LeaderEnd;
#endif
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
            if (!Parent.Pinned)
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

                if (!displacementVector.IsAlmostEqualTo(new XYZ(0, 0, 0)))
                {
                    Transform tr = Transform.CreateTranslation(_ownerView.CropBox.Transform.OfVector(displacementVector));

                    if (Parent.GetType() == typeof(IndependentTag))
                    {
                        IndependentTag tag = Parent as IndependentTag;
                        CustomLeader customLeader = new CustomLeader();
                        if (tag.HasLeader && tag.LeaderEndCondition == LeaderEndCondition.Free)
                        {
#if Version2022 || Version2023 || Version2024
                            Reference referencedElement = tag.GetTaggedReferences().FirstOrDefault();
                            if (referencedElement != null)
                            {
                                XYZ leaderEnd = tag.GetLeaderEnd(referencedElement);
                                customLeader = new CustomLeader(leaderEnd, new XYZ(0, 0, 0));
                            }
                            else
                            {
                                customLeader = new CustomLeader(new XYZ(0, 0, 0), new XYZ(0, 0, 0));
                            }

#elif Version2019 || Version2020 || Version2021
                            customLeader = new CustomLeader(tag.LeaderEnd, new XYZ(0, 0, 0));
#endif

                        }

                        tag.TagHeadPosition = tr.OfPoint(tag.TagHeadPosition);

                        if (tag.HasLeader && tag.LeaderEndCondition == LeaderEndCondition.Free)
                        {
#if Version2022 || Version2023 || Version2024
                            Reference referencedElement = tag.GetTaggedReferences().FirstOrDefault();
                            if (referencedElement != null)
                            {
                                tag.SetLeaderEnd(referencedElement, customLeader.End);
                            }

#elif Version2019 || Version2020 || Version2021
                            tag.LeaderEnd = customLeader.End;
#endif

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
                        SpatialElementTag spatialElementTag = Parent as SpatialElementTag;

                        CustomLeader leader = null;
                        if (spatialElementTag.HasLeader)
                        {
                            leader = new CustomLeader(spatialElementTag.LeaderEnd, new XYZ(0, 0, 0));
                        }

                        Parent.Location.Move(_ownerView.CropBox.Transform.OfVector(displacementVector));

                        if (!spatialElementTag.IsOrphaned && !spatialElementTag.HasLeader)
                        {
                            RoomTag roomTag = spatialElementTag as RoomTag;
                            if (roomTag !=  null && !roomTag.Room.IsPointInRoom(roomTag.TagHeadPosition))
                            {
                                spatialElementTag.HasLeader = true;
                            }

                            SpaceTag spaceTag = spatialElementTag as SpaceTag;
                            if (spaceTag != null && !spaceTag.Space.IsPointInSpace(spaceTag.TagHeadPosition))
                            {
                                spatialElementTag.HasLeader = true;
                            }

                            AreaTag areaTag = spatialElementTag as AreaTag;
                            if (areaTag != null)
                            {
                                areaTag.HasLeader = true;
                            }
                        }

                        if (leader != null)
                        {
                            spatialElementTag.LeaderEnd = leader.End;
                        }


                    }
                    else
                    {
                        Parent.Location.Move(_ownerView.CropBox.Transform.OfVector(displacementVector));
                    }
                }

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

    class PreparationElement
    {
        public PreparationElement(Element element, XYZ displacementVector)
        {
            Element = element;
            DisplacementVector = displacementVector;
        }
        public Element Element { get; set; }
        public XYZ DisplacementVector { get; set; }
    }

    public enum AlignType { Left, Right, Up, Down, Center, Middle, Vertically, Horizontally, UntangleVertically, UntangleHorizontally };

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
