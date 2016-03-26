using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

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
        private XYZ _origin;

        public AnnotationElement(Element e)
        {
            Parent = e;
            _doc = e.Document;
            _ownerView = _doc.GetElement(e.OwnerViewId) as View;

            BoundingBoxXYZ BBox = e.get_BoundingBox(_ownerView);
            XYZ max = _ownerView.CropBox.Transform.Inverse.OfPoint(BBox.Max);
            XYZ min = _ownerView.CropBox.Transform.Inverse.OfPoint(BBox.Min);
            UpLeft = new XYZ(max.X, max.Y, max.Z);
            UpRight = new XYZ(min.X, max.Y, max.Z);
            DownLeft = new XYZ(max.X, min.Y, max.Z);
            DownRight = new XYZ(min.X, min.Y, max.Z);

            Center = (UpRight + DownLeft) / 2;

            if (e.GetType() == typeof(IndependentTag))
            {
                IndependentTag tag = e as IndependentTag;
                _origin = _ownerView.CropBox.Transform.Inverse.OfPoint(tag.TagHeadPosition);
            }
            else if (e.GetType() == typeof(TextNote))
            {
                TextNote note = e as TextNote;
                _origin = _ownerView.CropBox.Transform.Inverse.OfPoint(note.Coord);
            }
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
                tag.TagHeadPosition = tr.OfPoint(tag.TagHeadPosition);
            }
            else if (Parent.GetType() == typeof(TextNote))
            {
                TextNote note = Parent as TextNote;
                note.Coord = tr.OfPoint(note.Coord);
            }
        }
    }

    enum AlignType { Left, Right, Up, Down, Verticaly, Horizontaly };


}
