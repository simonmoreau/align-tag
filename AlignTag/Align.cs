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
        public Result AlignElements(ExternalCommandData commandData, ref string message, AlignType alignType)
        {
            UIDocument UIdoc = commandData.Application.ActiveUIDocument;
            Document doc = UIdoc.Document;

            using (TransactionGroup txg = new TransactionGroup(doc))
            {
                try
                {
                    // Add Your Code Here
                    AlignTag(UIdoc, txg, alignType);
                    // Return Success
                    return Result.Succeeded;
                }

                catch (Autodesk.Revit.Exceptions.OperationCanceledException exceptionCanceled)
                {
                    message = exceptionCanceled.Message;
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

        private List<AnnotationElement> RetriveAnnotationElementsFromSelection(UIDocument UIDoc, TransactionGroup txg)
        {
            // Get the element selection of current document.
            Selection selection = UIDoc.Selection;
            ICollection<ElementId> ids = selection.GetElementIds();

            List<Element> elements = new List<Element>();

            using (Transaction tx = new Transaction(UIDoc.Document))
            {
                tx.Start("Prepare tags");

                //Remove all leader to find the correct tag height and width
                foreach (ElementId id in ids)
                {
                    Element e = UIDoc.Document.GetElement(id);

                    if (e.GetType() == typeof(IndependentTag))
                    {
                        IndependentTag tag = e as IndependentTag;
                        tag.LeaderEndCondition = LeaderEndCondition.Free;
                        tag.LeaderEnd = tag.TagHeadPosition;
                        elements.Add(e);
                    }
                    else if (e.GetType() == typeof(TextNote))
                    {
                        TextNote note = e as TextNote;
                        note.RemoveLeaders();
                        elements.Add(e);
                    }
                }

                tx.Commit();

                List<AnnotationElement> annotationElements = new List<AnnotationElement>();

                foreach (Element e in elements)
                {
                    annotationElements.Add(new AnnotationElement(e));
                }

                txg.RollBack();
                txg.Start();

                return annotationElements;

            }
        }

        private void AlignTag(UIDocument UIDoc, TransactionGroup txg, AlignType alignType)
        {

            txg.Start();

            // Get the handle of current document.
            Document doc = UIDoc.Document;

            List<AnnotationElement> annotationElements = RetriveAnnotationElementsFromSelection(UIDoc, txg);

            using (Transaction tx = new Transaction(doc))
            {
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
                                XYZ resultingPoint = new XYZ(farthestAnnotation.UpLeft.X, annotationElement.UpLeft.Y, annotationElement.UpLeft.Z);
                                annotationElement.MoveTo(resultingPoint, AlignType.Left);
                            }
                            break;
                        case AlignType.Right:
                            farthestAnnotation =
                                annotationElements.OrderByDescending(x => x.UpRight.X).FirstOrDefault();
                            foreach (AnnotationElement annotationElement in annotationElements)
                            {
                                XYZ resultingPoint = new XYZ(farthestAnnotation.UpRight.X, annotationElement.UpRight.Y, annotationElement.UpRight.Z);
                                annotationElement.MoveTo(resultingPoint, AlignType.Right);
                            }
                            break;
                        case AlignType.Up:
                            farthestAnnotation =
                                annotationElements.OrderByDescending(x => x.UpRight.Y).FirstOrDefault();
                            foreach (AnnotationElement annotationElement in annotationElements)
                            {
                                XYZ resultingPoint = new XYZ(annotationElement.UpRight.X, farthestAnnotation.UpRight.Y, annotationElement.UpRight.Z);
                                annotationElement.MoveTo(resultingPoint, AlignType.Up);
                            }
                            break;
                        case AlignType.Down:
                            farthestAnnotation =
                                annotationElements.OrderBy(x => x.UpRight.Y).FirstOrDefault();
                            foreach (AnnotationElement annotationElement in annotationElements)
                            {
                                XYZ resultingPoint = new XYZ(annotationElement.UpRight.X, farthestAnnotation.UpRight.Y, annotationElement.UpRight.Z);
                                annotationElement.MoveTo(resultingPoint, AlignType.Down);
                            }
                            break;
                        case AlignType.Verticaly:
                            List<AnnotationElement> sortedAnnotationElements = annotationElements.OrderBy(x => x.UpRight.Y).ToList();
                            AnnotationElement upperAnnotation = sortedAnnotationElements.LastOrDefault();
                            AnnotationElement lowerAnnotation = sortedAnnotationElements.FirstOrDefault();
                            double spacing = (upperAnnotation.Center.Y - lowerAnnotation.Center.Y) / (annotationElements.Count-1);
                            int i = 0;
                            foreach (AnnotationElement annotationElement in sortedAnnotationElements)
                            {
                                XYZ resultingPoint = new XYZ(annotationElement.Center.X, lowerAnnotation.Center.Y + i*spacing, annotationElement.Center.Z);
                                annotationElement.MoveTo(resultingPoint, AlignType.Verticaly);
                                i++;
                            }
                            break;
                        case AlignType.Horizontaly:
                            sortedAnnotationElements = annotationElements.OrderBy(x => x.UpRight.X).ToList();
                            AnnotationElement rightAnnotation = sortedAnnotationElements.LastOrDefault();
                            AnnotationElement leftAnnotation = sortedAnnotationElements.FirstOrDefault();
                            spacing = (rightAnnotation.Center.X - leftAnnotation.Center.X) / (annotationElements.Count-1);
                            i = 0;
                            foreach (AnnotationElement annotationElement in sortedAnnotationElements)
                            {
                                XYZ resultingPoint = new XYZ(leftAnnotation.Center.X + i * spacing,annotationElement.Center.Y, annotationElement.Center.Z);
                                annotationElement.MoveTo(resultingPoint, AlignType.Horizontaly);
                                i++;
                            }
                            break;
                        default:
                            break;
                    }
                }

                tx.Commit();
            }


            txg.Commit();
        }
    }
}
