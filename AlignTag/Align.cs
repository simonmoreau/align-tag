#region Namespaces
using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
#endregion

namespace AlignTag
{
    class Align
    {
        public Result AlignElements(ExternalCommandData commandData, ref string message, string side)
        {
            UIDocument UIdoc = commandData.Application.ActiveUIDocument;
            Document doc = UIdoc.Document;

            using (Transaction tx = new Transaction(doc))
            {
                try
                {
                    // Add Your Code Here
                    AlignTag(UIdoc, tx, side);
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
                    return Result.Cancelled;
                }
                catch (ErrorMessageException errorEx)
                {
                    // checked exception need to show in error messagebox
                    message = errorEx.Message;
                    if (tx.HasStarted())
                    {
                        tx.RollBack();
                    }
                    return Result.Failed;
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
                    return Result.Failed;
                }
            }
        }

        private List<IndependentTag> RetriveTagsFromSelection(UIDocument UIDoc)
        {
            // Get the element selection of current document.
            Selection selection = UIDoc.Selection;
            ICollection<ElementId> ids = selection.GetElementIds();

            List<IndependentTag> tags = new List<IndependentTag>();


            foreach (ElementId id in ids)
            {
                Element e = UIDoc.Document.GetElement(id);

                IndependentTag tag = e as IndependentTag;

                if (tag != null)
                {
                    tags.Add(tag);
                }
            }

            return tags;
        }

        private void AlignTag(UIDocument UIDoc, Transaction tx, string side)
        {
            // Get the handle of current document.
            Document doc = UIDoc.Document;

            List<IndependentTag> tags = RetriveTagsFromSelection(UIDoc);

            if (tags.Count > 1)
            {
                View currentView = doc.ActiveView;
                XYZ displacementDirection = currentView.UpDirection;

                if (side == "Left" || side == "Right")
                {
                    displacementDirection = currentView.RightDirection;
                }
                else if (side == "Top" || side == "Bottom")
                {
                    displacementDirection = currentView.UpDirection;
                }

                //Get the max in displacementDirection
                double maxDisplacementDistance = tags[0].TagHeadPosition.DotProduct(displacementDirection);
                XYZ maxDisplacementVector = displacementDirection.Multiply(maxDisplacementDistance);
                XYZ headPoint = new XYZ();

                foreach (IndependentTag tag in tags)
                {
                    headPoint = tag.TagHeadPosition;
                    
                    
                    //BoundingBoxXYZ tagBBox = tag.get_BoundingBox(currentView);

                    if (side == "Left" || side == "Bottom")
                    {
                        if (headPoint.DotProduct(displacementDirection) < maxDisplacementDistance)
                        {
                            maxDisplacementVector = displacementDirection.Multiply(headPoint.DotProduct(displacementDirection));
                            maxDisplacementDistance = headPoint.DotProduct(displacementDirection);
                        }
                    }
                    else if (side == "Top" || side == "Right")
                    {
                        if (headPoint.DotProduct(displacementDirection) > maxDisplacementDistance)
                        {
                            maxDisplacementVector = displacementDirection.Multiply(headPoint.DotProduct(displacementDirection));
                            maxDisplacementDistance = headPoint.DotProduct(displacementDirection);
                        }
                    }

                }
                tx.Start("Align Tag");

                //Move each tag
                foreach (IndependentTag intag in tags)
                {
                    headPoint = intag.TagHeadPosition;

                    Transform tr = Transform.CreateTranslation(maxDisplacementVector - displacementDirection.Multiply(headPoint.DotProduct(displacementDirection)));

                    //Get the helbow point
                    if (intag.HasLeader)
                    {
                        //Create an elbow
                        XYZ elbow = null;
                        //Check if the tag has an elbow
                        try
                        {
                            elbow = intag.LeaderElbow;
                        }
                        catch // (Autodesk.Revit.Exceptions.InvalidOperationException)
                        {
                            // elbow = (intag.TagHeadPosition + leaderEnd) / 2;
                        }

                        intag.TagHeadPosition = tr.OfPoint(headPoint);

                        if (elbow != null)
                        {
                            intag.LeaderElbow = elbow;
                        }
                    }
                    else
                    {
                        intag.TagHeadPosition = tr.OfPoint(headPoint);
                    }

                }

                tx.Commit();
            }
        }

        public Result DistributeElements(ExternalCommandData commandData, ref string message, string side)
        {
            UIDocument UIdoc = commandData.Application.ActiveUIDocument;
            Document doc = UIdoc.Document;

            using (Transaction tx = new Transaction(doc))
            {
                try
                {
                    // Add Your Code Here
                    DistributeTag(UIdoc, tx, side);
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
                    return Result.Cancelled;
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

        private void DistributeTag(UIDocument UIDoc, Transaction tx, string side)
        {
            // Get the handle of current document.
            Document doc = UIDoc.Document;

            List<IndependentTag> tags = RetriveTagsFromSelection(UIDoc);

            if (tags.Count > 2)
            {
                View currentView = doc.ActiveView;
                XYZ upDir = currentView.UpDirection;

                if (side == "Horizontally")
                {
                    upDir = currentView.RightDirection;
                }
                else if (side == "Vertically")
                {
                    upDir = currentView.UpDirection;
                }

                List<DistributedTag> distributedTags = new List<DistributedTag>();

                foreach (IndependentTag tag in tags)
                {
                    distributedTags.Add(new DistributedTag(tag,upDir));
                    //double projectionLenght = upDir.DotProduct(tag.TagHeadPosition);
                }

                distributedTags = distributedTags.OrderBy(x => x.ProjectionLenght).ToList();

                double totalLenght = distributedTags.Last().ProjectionLenght - distributedTags.First().ProjectionLenght;
                double spacing = totalLenght / (distributedTags.Count() - 1);

                XYZ startingXYZ = distributedTags.First().ProjectionXYZ;

                tx.Start("Distribute Tags");

                int i = 0;

                foreach (DistributedTag distribuedTag in distributedTags)
                {
                    Transform tr = Transform.CreateTranslation(startingXYZ + upDir.Multiply(i*spacing) - distribuedTag.ProjectionXYZ);
                    distribuedTag.Tag.TagHeadPosition = tr.OfPoint(distribuedTag.Tag.TagHeadPosition);
                    i++;
                }

                tx.Commit();

            }
        }
    }

    class DistributedTag
    {
        private IndependentTag _tag;
        public IndependentTag Tag
        {
            get { return _tag; }
            set { _tag = value; }
        }

        private double _projectionLenght;
        public double ProjectionLenght
        {
            get { return _projectionLenght; }
        }

        private XYZ _projectionXYZ;
        public XYZ ProjectionXYZ
        {
            get { return _projectionXYZ; }
        }

        public DistributedTag(IndependentTag tag, XYZ upDir)
        {
            _tag = tag;
            _projectionLenght = upDir.DotProduct(tag.TagHeadPosition);
            _projectionXYZ = upDir.Multiply(_projectionLenght);
        }
    }
}
