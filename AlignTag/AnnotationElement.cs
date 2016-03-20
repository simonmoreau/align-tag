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

        public XYZ LowLeft { get; set; }

        public XYZ LowRight { get; set; }

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
            UpLeft = new XYZ(min.X, max.Y, 0);
            UpRight = new XYZ(max.X, max.Y, 0);
            LowLeft = new XYZ(min.X, min.Y, 0);
            LowRight = new XYZ(max.X, min.Y, 0);

            Center = (UpRight + LowLeft) / 2;

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

        public void MoveTo(XYZ point)
        {
            
        }
    }


}
