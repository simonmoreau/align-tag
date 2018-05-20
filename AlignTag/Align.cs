#region Namespaces
using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Mechanical;
#endregion

namespace AlignTag
{
    class Align
    {
        public Result AlignElements(ExternalCommandData commandData, ref string message, AlignType alignType)
        {
            UIDocument UIdoc = commandData.Application.ActiveUIDocument;
            Document doc = UIdoc.Document;

            using (TransactionGroup txg = new TransactionGroup(doc))
            {
                try
                {
                    // Add Your Code Here
                    AlignTag(UIdoc, alignType, txg);
                    
                    // Return Success
                    return Result.Succeeded;
                }

                catch (Autodesk.Revit.Exceptions.OperationCanceledException exceptionCanceled)
                {
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

        private List<AnnotationElement> RetriveAnnotationElementsFromSelection(UIDocument UIDoc, Transaction tx)
        {
            // Get the element selection of current document.
            Selection selection = UIDoc.Selection;
            ICollection<ElementId> ids = selection.GetElementIds();

            List<Element> elements = new List<Element>();

            List<AnnotationElement> annotationElements = new List<AnnotationElement>();

            tx.Start("Prepare tags");

            //Remove all leader to find the correct tag height and width
            foreach (ElementId id in ids)
            {
                Element e = UIDoc.Document.GetElement(id);

                if (e.GetType() == typeof(IndependentTag))
                {
                    IndependentTag tag = e as IndependentTag;
                    tag.LeaderEndCondition = LeaderEndCondition.Free;
                    if (tag.HasLeader)
                    {
                        tag.LeaderEnd = tag.TagHeadPosition;
                        tag.LeaderElbow = tag.TagHeadPosition;
                    }

                    elements.Add(e);
                }
                else if (e.GetType() == typeof(TextNote))
                {
                    TextNote note = e as TextNote;
                    note.RemoveLeaders();
                    elements.Add(e);
                }
                else if (e.GetType().IsSubclassOf(typeof(SpatialElementTag)))
                {
                    SpatialElementTag tag = e as SpatialElementTag;

                    if (tag.HasLeader)
                    {
                        tag.LeaderEnd = tag.TagHeadPosition;
                        tag.LeaderElbow = tag.TagHeadPosition;
                    }
                    elements.Add(e);
                }
                else
                {
                    elements.Add(e);
                }
            }


            FailureHandlingOptions options = tx.GetFailureHandlingOptions();

            options.SetFailuresPreprocessor(new CommitPreprocessor());
            // Now, showing of any eventual mini-warnings will be postponed until the following transaction.
            tx.Commit(options);

            foreach (Element e in elements)
            {
                annotationElements.Add(new AnnotationElement(e));
            }

            return annotationElements;
        }

        private void AlignTag(UIDocument UIDoc,AlignType alignType, TransactionGroup txg)
        {
            

            // Get the handle of current document.
            Document doc = UIDoc.Document;
            ICollection<ElementId> selectedIds = UIDoc.Selection.GetElementIds();



            // 1. First Proposed Change
            //    First check if there is something that's been seleted, and if so - operate on that
            //    However, if the Uidoc.Slection is empty, prompt for selection. 
            //    This allows you to stay on the 'Add-ins' Tab and keep using the 'Align' tab without going back and forth to 'Modify'
            //    TO-DO: Should we disselect after we are done? (delete the boolean stuff if you don't think it's worth disselecting)

            bool empty = false;

            using (Transaction tx = new Transaction(doc))
            {
                txg.Start("Align Tag");

                if (selectedIds.Count == 0)
                {
                    empty = true;

                    IList<Reference> selectedReferences = UIDoc.Selection.PickObjects(ObjectType.Element, "Pick elements to be aligned");
                    selectedIds = Tools.RevitReferencesToElementIds(doc, selectedReferences);
                    UIDoc.Selection.SetElementIds(selectedIds);
                }


                List<AnnotationElement> annotationElements = RetriveAnnotationElementsFromSelection(UIDoc, tx);

                txg.RollBack();
                txg.Start("Align Tag");

                tx.Start("Align Tags");


                if (annotationElements.Count > 1)
                {
                    View currentView = doc.ActiveView;
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
                        case AlignType.Verticaly:
                            sortedAnnotationElements = annotationElements.OrderBy(x => x.UpRight.Y).ToList();
                            upperAnnotation = sortedAnnotationElements.LastOrDefault();
                            lowerAnnotation = sortedAnnotationElements.FirstOrDefault();
                            double spacing = (upperAnnotation.Center.Y - lowerAnnotation.Center.Y) / (annotationElements.Count - 1);
                            int i = 0;
                            foreach (AnnotationElement annotationElement in sortedAnnotationElements)
                            {
                                XYZ resultingPoint = new XYZ(annotationElement.Center.X, lowerAnnotation.Center.Y + i * spacing, 0);
                                annotationElement.MoveTo(resultingPoint, AlignType.Verticaly);
                                i++;
                            }
                            break;
                        case AlignType.Horizontaly:
                            sortedAnnotationElements = annotationElements.OrderBy(x => x.UpRight.X).ToList();
                            rightAnnotation = sortedAnnotationElements.LastOrDefault();
                            leftAnnotation = sortedAnnotationElements.FirstOrDefault();
                            spacing = (rightAnnotation.Center.X - leftAnnotation.Center.X) / (annotationElements.Count - 1);
                            i = 0;
                            foreach (AnnotationElement annotationElement in sortedAnnotationElements)
                            {
                                XYZ resultingPoint = new XYZ(leftAnnotation.Center.X + i * spacing, annotationElement.Center.Y, 0);
                                annotationElement.MoveTo(resultingPoint, AlignType.Horizontaly);
                                i++;
                            }
                            break;
                        case AlignType.Untangle:
                            sortedAnnotationElements = annotationElements.OrderBy(y => y.GetLeaderEnd().Y).ToList();
                            upperAnnotation = sortedAnnotationElements.FirstOrDefault();
                            spacing = 0;
                            foreach (AnnotationElement annotationElement in sortedAnnotationElements)
                            {
                                XYZ resultingPoint = new XYZ(annotationElement.UpLeft.X, upperAnnotation.UpLeft.Y + spacing, 0);
                                annotationElement.MoveTo(resultingPoint, AlignType.Untangle);
                                spacing = spacing + (annotationElement.UpLeft.Y - annotationElement.DownLeft.Y);
                            }
                            break;
                        default:
                            break;
                    }
                }


                tx.Commit();

                txg.Assimilate();
            }

            // Disselect if the selection was empty to begin with
            if (empty) selectedIds = new List<ElementId> { ElementId.InvalidElementId };

            UIDoc.Selection.SetElementIds(selectedIds);
        }

    }
}
