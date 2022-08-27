#region Namespaces
using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Mechanical;
using System.Diagnostics;
#endregion

namespace AlignTag
{
    public class Align
    {
        private UIDocument UIdoc;
        public Result AlignElements(ExternalCommandData commandData, ref string message, AlignType alignType)
        {
            // Get the handle of current document.
            UIdoc = commandData.Application.ActiveUIDocument;
            Document document = UIdoc.Document;

            using (TransactionGroup txg = new TransactionGroup(document))
            {
                try
                {
                    ICollection<ElementId> selectedIds = UIdoc.Selection.GetElementIds();

                    bool empty = false;

                    if (selectedIds.Count == 0)
                    {
                        empty = true;

                        IList<Reference> selectedReferences = UIdoc.Selection.PickObjects(ObjectType.Element, "Pick elements to be aligned");
                        selectedIds = Tools.RevitReferencesToElementIds(document, selectedReferences);
                        UIdoc.Selection.SetElementIds(selectedIds);
                    }

                    AlignTag(alignType, txg, selectedIds, document);

                    // Disselect if the selection was empty to begin with
                    if (empty) selectedIds = new List<ElementId> { ElementId.InvalidElementId };

                    UIdoc.Selection.SetElementIds(selectedIds);

                    // Return Success
                    return Result.Succeeded;
                }

                catch (Autodesk.Revit.Exceptions.OperationCanceledException exceptionCanceled)
                {
                    Console.WriteLine(exceptionCanceled.Message);
                    //message = exceptionCanceled.Message;
                    if (txg.HasStarted())
                    {
                        txg.RollBack();
                    }
                    return Result.Cancelled;
                }
                catch (ErrorMessageException errorEx)
                {
                    // checked exception need to show in error messagebox
                    message = errorEx.Message;
                    if (txg.HasStarted())
                    {
                        txg.RollBack();
                    }
                    return Result.Failed;
                }
                catch (Exception ex)
                {
                    // unchecked exception cause command failed
                    message = ex.Message;
                    //Trace.WriteLine(ex.ToString());
                    if (txg.HasStarted())
                    {
                        txg.RollBack();
                    }
                    return Result.Failed;
                }
            }
        }


        public void AlignTag(AlignType alignType, TransactionGroup txg, ICollection<ElementId> selectedIds, Document document)
        {
            
            // 1. First Proposed Change
            //    First check if there is something that's been seleted, and if so - operate on that
            //    However, if the Uidoc.Slection is empty, prompt for selection. 
            //    This allows you to stay on the 'Add-ins' Tab and keep using the 'Align' tab without going back and forth to 'Modify'
            //    TO-DO: Should we disselect after we are done? (delete the boolean stuff if you don't think it's worth disselecting)

            using (Transaction tx = new Transaction(document))
            {
                txg.Start(AlignTypeToText(alignType));

                tx.Start("Prepare tags");
                Debug.WriteLine(DateTime.Now.ToString() + " - Start Prepare tags");

                List<AnnotationElement> annotationElements = RetriveAnnotationElementsFromSelection(document, tx, selectedIds);

                txg.RollBack();
                // txg.Assimilate();
                Debug.WriteLine(DateTime.Now.ToString() + " - Rollback Prepare tags");

                txg.Start(AlignTypeToText(alignType));

                tx.Start(AlignTypeToText(alignType));
                Debug.WriteLine(DateTime.Now.ToString() + " - Start align tags");

                if (annotationElements.Count > 1)
                {
                    AlignAnnotationElements(annotationElements, alignType, document);
                }

                Debug.WriteLine(DateTime.Now.ToString() + " - Commit align tags");

                tx.Commit();

                txg.Assimilate();
            }
        }

        private List<AnnotationElement> RetriveAnnotationElementsFromSelection(Document document, Transaction tx, ICollection<ElementId> ids)
        {
            List<PreparationElement> preparationElements = new List<PreparationElement>();

            List<AnnotationElement> annotationElements = new List<AnnotationElement>();


            //Remove all leader to find the correct tag height and width
            foreach (ElementId id in ids)
            {
                Element e = document.GetElement(id);

                if (e.GetType() == typeof(IndependentTag))
                {
                    IndependentTag tag = e as IndependentTag;

                    //XYZ displacementVector = null;
                    //if (tag.LeaderEndCondition == LeaderEndCondition.Free)
                    //{
                    //    displacementVector = tag.LeaderEnd - tag.TagHeadPosition;
                    //}

                    if (tag.HasLeader)
                    {
                        tag.HasLeader = false;
                    }

                    preparationElements.Add(new PreparationElement(e, null));


                }
                else if (e.GetType() == typeof(TextNote))
                {
                    TextNote note = e as TextNote;
                    note.RemoveLeaders();
                    preparationElements.Add(new PreparationElement(e, null));
                }
                else if (e.GetType().IsSubclassOf(typeof(SpatialElementTag)))
                {
                    SpatialElementTag tag = e as SpatialElementTag;

                    XYZ displacementVector = null;

                    if (tag.HasLeader)
                    {
                        displacementVector = tag.LeaderEnd - tag.TagHeadPosition;
                        tag.HasLeader = false;
                    }
                    
                    preparationElements.Add(new PreparationElement(e, displacementVector));
                }
                else
                {
                    preparationElements.Add(new PreparationElement(e, null));
                }
            }

            FailureHandlingOptions options = tx.GetFailureHandlingOptions();

            options.SetFailuresPreprocessor(new TemporaryCommitPreprocessor());
            // Now, showing of any eventual mini-warnings will be postponed until the following transaction.
            tx.Commit(options);

            foreach (PreparationElement e in preparationElements)
            {
                annotationElements.Add(new AnnotationElement(e));
            }

            return annotationElements;
        }

        private void AlignAnnotationElements(List<AnnotationElement> annotationElements, AlignType alignType, Document document)
        {
            View currentView = document.ActiveView;
            XYZ displacementDirection = currentView.UpDirection;

            switch (alignType)
            {
                case AlignType.Left:
                    AnnotationElement farthestAnnotation =
                        annotationElements.OrderBy(x => x.UpLeft.X).FirstOrDefault();
                    foreach (AnnotationElement annotationElement in annotationElements)
                    {
                        XYZ resultingPoint = new XYZ(farthestAnnotation.UpLeft.X, annotationElement.UpLeft.Y, 0);
                        annotationElement.MoveTo(resultingPoint, AlignType.Left);
                    }
                    break;
                case AlignType.Right:
                    farthestAnnotation =
                        annotationElements.OrderByDescending(x => x.UpRight.X).FirstOrDefault();
                    foreach (AnnotationElement annotationElement in annotationElements)
                    {
                        XYZ resultingPoint = new XYZ(farthestAnnotation.UpRight.X, annotationElement.UpRight.Y, 0);
                        annotationElement.MoveTo(resultingPoint, AlignType.Right);
                    }
                    break;
                case AlignType.Up:
                    farthestAnnotation =
                        annotationElements.OrderByDescending(x => x.UpRight.Y).FirstOrDefault();
                    foreach (AnnotationElement annotationElement in annotationElements)
                    {
                        XYZ resultingPoint = new XYZ(annotationElement.UpRight.X, farthestAnnotation.UpRight.Y, 0);
                        annotationElement.MoveTo(resultingPoint, AlignType.Up);
                    }
                    break;
                case AlignType.Down:
                    farthestAnnotation =
                        annotationElements.OrderBy(x => x.DownRight.Y).FirstOrDefault();
                    foreach (AnnotationElement annotationElement in annotationElements)
                    {
                        XYZ resultingPoint = new XYZ(annotationElement.DownRight.X, farthestAnnotation.DownRight.Y, 0);
                        annotationElement.MoveTo(resultingPoint, AlignType.Down);
                    }
                    break;
                case AlignType.Center: //On the same vertical axe
                    List<AnnotationElement> sortedAnnotationElements = annotationElements.OrderBy(x => x.UpRight.X).ToList();
                    AnnotationElement rightAnnotation = sortedAnnotationElements.LastOrDefault();
                    AnnotationElement leftAnnotation = sortedAnnotationElements.FirstOrDefault();
                    double XCoord = (rightAnnotation.Center.X + leftAnnotation.Center.X) / 2;
                    foreach (AnnotationElement annotationElement in sortedAnnotationElements)
                    {
                        XYZ resultingPoint = new XYZ(XCoord, annotationElement.Center.Y, 0);
                        annotationElement.MoveTo(resultingPoint, AlignType.Center);
                    }
                    break;
                case AlignType.Middle: //On the same horizontal axe
                    sortedAnnotationElements = annotationElements.OrderBy(x => x.UpRight.Y).ToList();
                    AnnotationElement upperAnnotation = sortedAnnotationElements.LastOrDefault();
                    AnnotationElement lowerAnnotation = sortedAnnotationElements.FirstOrDefault();
                    double YCoord = (upperAnnotation.Center.Y + lowerAnnotation.Center.Y) / 2;
                    foreach (AnnotationElement annotationElement in sortedAnnotationElements)
                    {
                        XYZ resultingPoint = new XYZ(annotationElement.Center.X, YCoord, 0);
                        annotationElement.MoveTo(resultingPoint, AlignType.Middle);
                    }
                    break;
                case AlignType.Vertically:
                    sortedAnnotationElements = annotationElements.OrderBy(x => x.UpRight.Y).ToList();
                    upperAnnotation = sortedAnnotationElements.LastOrDefault();
                    lowerAnnotation = sortedAnnotationElements.FirstOrDefault();
                    double spacing = (upperAnnotation.Center.Y - lowerAnnotation.Center.Y) / (annotationElements.Count - 1);
                    int i = 0;
                    foreach (AnnotationElement annotationElement in sortedAnnotationElements)
                    {
                        XYZ resultingPoint = new XYZ(annotationElement.Center.X, lowerAnnotation.Center.Y + i * spacing, 0);
                        annotationElement.MoveTo(resultingPoint, AlignType.Vertically);
                        i++;
                    }
                    break;
                case AlignType.Horizontally:
                    sortedAnnotationElements = annotationElements.OrderBy(x => x.UpRight.X).ToList();
                    rightAnnotation = sortedAnnotationElements.LastOrDefault();
                    leftAnnotation = sortedAnnotationElements.FirstOrDefault();
                    spacing = (rightAnnotation.Center.X - leftAnnotation.Center.X) / (annotationElements.Count - 1);
                    i = 0;
                    foreach (AnnotationElement annotationElement in sortedAnnotationElements)
                    {
                        XYZ resultingPoint = new XYZ(leftAnnotation.Center.X + i * spacing, annotationElement.Center.Y, 0);
                        annotationElement.MoveTo(resultingPoint, AlignType.Horizontally);
                        i++;
                    }
                    break;
                case AlignType.UntangleVertically:
                    sortedAnnotationElements = annotationElements.OrderBy(y => y.GetLeaderEnd().Y).ToList();
                    upperAnnotation = sortedAnnotationElements.FirstOrDefault();
                    spacing = 0;
                    foreach (AnnotationElement annotationElement in sortedAnnotationElements)
                    {
                        XYZ resultingPoint = new XYZ(annotationElement.UpLeft.X, upperAnnotation.UpLeft.Y + spacing, 0);
                        annotationElement.MoveTo(resultingPoint, AlignType.UntangleVertically);
                        spacing = spacing + (annotationElement.UpLeft.Y - annotationElement.DownLeft.Y);
                    }
                    break;
                case AlignType.UntangleHorizontally:
                    sortedAnnotationElements = annotationElements.OrderBy(x => x.GetLeaderEnd().X).ToList();
                    leftAnnotation = sortedAnnotationElements.FirstOrDefault();
                    spacing = 0;
                    foreach (AnnotationElement annotationElement in sortedAnnotationElements)
                    {
                        XYZ resultingPoint = new XYZ(leftAnnotation.UpLeft.X + spacing, annotationElement.UpLeft.Y, 0);
                        annotationElement.MoveTo(resultingPoint, AlignType.UntangleHorizontally);
                        spacing = spacing + (annotationElement.UpRight.X - annotationElement.UpLeft.X);
                    }
                    break;
                default:
                    break;
            }
        }

        private string AlignTypeToText(AlignType alignType)
        {
            string text = "";

            switch (alignType)
            {
                case AlignType.Left:
                    text = "Align Left";
                    break;
                case AlignType.Right:
                    text = "Align Right";
                    break;
                case AlignType.Up:
                    text = "Align Top";
                    break;
                case AlignType.Down:
                    text = "Align Bottom";
                    break;
                case AlignType.Center:
                    text = "Align Center";
                    break;
                case AlignType.Middle:
                    text = "Align Middle";
                    break;
                case AlignType.Vertically:
                    text = "Distribute Vertically";
                    break;
                case AlignType.Horizontally:
                    text = "Distribute Horizontally";
                    break;
                case AlignType.UntangleVertically:
                    text = "Untangle Vertically";
                    break;
                case AlignType.UntangleHorizontally:
                    text = "Untangle Horizontally";
                    break;
                default:
                    break;
            }

            return text;
        }

    }
}
