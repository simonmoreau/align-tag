using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.ApplicationServices;

namespace Align.Test
{
    [TestFixture]
    public class AlignTest
    {
        private Document document;
        [SetUp]
        public void RunBeforeTest(UIApplication uiApplication)
        {
            string versionName = uiApplication.Application.VersionName.Replace("Autodesk Revit ", "");

            string path = $"G:\\My Drive\\05 - Travail\\Revit Dev\\AlignTag\\Test Models\\AlignTestModel_{versionName}.rvt";
            UIDocument uIDocument= uiApplication.OpenAndActivateDocument(path);
            document = uIDocument.Document;
            Console.WriteLine($"Run 'SetUp' in {GetType().Name}");
            Console.WriteLine($"Open the AlignTestModel_{versionName} model.");
        }

        [TearDown]
        public void RunAfterTest()
        {
            Console.WriteLine($"Run 'TearDown' in {GetType().Name}");
        }

        [Test]
        public void AlignTagLeft()
        {
            int[] ids = new int[] { 201324, 201325, 201326, 201327 };
            AlignTestFunction(ids, AlignTag.AlignType.Left);
        }

        [Test]
        public void AlignTagRight()
        {
            int[] ids = new int[] { 201379, 201380, 201381, 201382 };
            AlignTestFunction(ids, AlignTag.AlignType.Right);
        }

        [Test]
        public void AlignTagCenter()
        {
            int[] ids = new int[] { 201439, 201440, 201441, 201442 };
            AlignTestFunction(ids, AlignTag.AlignType.Center);
        }

        [Test]
        public void AlignTagUp()
        {
            int[] ids = new int[] { 201483, 201484, 201485, 201486 };
            AlignTestFunction(ids, AlignTag.AlignType.Up);
        }

        [Test]
        public void AlignTagDown()
        {
            int[] ids = new int[] { 201556, 201557, 201558, 201559 };
            AlignTestFunction(ids, AlignTag.AlignType.Down);
        }

        [Test]
        public void AlignTagMiddle()
        {
            int[] ids = new int[] { 201600, 201601, 201602, 201603 };
            AlignTestFunction(ids, AlignTag.AlignType.Middle);
        }

        [Test]
        public void DistributeTagHorizontaly()
        {
            int[] ids = new int[] { 201956, 201957, 201958, 201959 };
            AlignTestFunction(ids, AlignTag.AlignType.Horizontally);
        }

        [Test]
        public void DistributeTagVerticaly()
        {
            int[] ids = new int[] { 202057, 202058, 202059, 202060 };
            AlignTestFunction(ids, AlignTag.AlignType.Vertically);
        }

        [Test]
        public void AlignWallLeft()
        {
            int[] ids = new int[] { 202240, 202272, 202307, 202356 };
            AlignTestFunction(ids, AlignTag.AlignType.Left);
        }

        [Test]
        public void AlignWallLeftWithPinned()
        {
            int[] ids = new int[] { 205004, 205005, 205006, 205007 };
            AlignTestFunction(ids, AlignTag.AlignType.Left);
        }

        [Test]
        public void AlignWallRight()
        {
            int[] ids = new int[] { 202393, 202394, 202395, 202396 };
            AlignTestFunction(ids, AlignTag.AlignType.Right);
        }

        [Test]
        public void AlignWallCenter()
        {
            int[] ids = new int[] { 202412, 202413, 202414, 202415 };
            AlignTestFunction(ids, AlignTag.AlignType.Center);
        }

        [Test]
        public void AlignWallUp()
        {
            int[] ids = new int[] { 202423, 202424, 202425, 202426 };
            AlignTestFunction(ids, AlignTag.AlignType.Up);
        }

        [Test]
        public void AlignWallDown()
        {
            int[] ids = new int[] { 202450, 202451, 202452, 202453 };
            AlignTestFunction(ids, AlignTag.AlignType.Down);
        }

        [Test]
        public void AlignWallMiddle()
        {
            int[] ids = new int[] { 202459, 202460, 202461, 202462 };
            AlignTestFunction(ids, AlignTag.AlignType.Middle);
        }

        [Test]
        public void DistributeWallHorizontaly()
        {
            int[] ids = new int[] { 202532, 202533, 202534, 202535 };
            AlignTestFunction(ids, AlignTag.AlignType.Horizontally);
        }

        [Test]
        public void DistributeWallVerticaly()
        {
            int[] ids = new int[] { 202470, 202471, 202472, 202473 };
            AlignTestFunction(ids, AlignTag.AlignType.Vertically);
        }

        [Test]
        public void AlignRoomTagTop()
        {
            int[] ids = new int[] { 205282, 205285 };
            AlignTestFunction(ids, AlignTag.AlignType.Up);
        }

        [Test]
        public void AlignRoomTagBottom()
        {
            int[] ids = new int[] { 205288, 205291, 205294 };
            AlignTestFunction(ids, AlignTag.AlignType.Down);
        }

        [Test]
        public void AlignSpaceTagTop()
        {
            int[] ids = new int[] { 205654, 205655 };
            AlignTestFunction(ids, AlignTag.AlignType.Up);
        }

        [Test]
        public void AlignSpaceTagBottom()
        {
            int[] ids = new int[] { 205656, 205657, 205658 };
            AlignTestFunction(ids, AlignTag.AlignType.Down);
        }

        [Test]
        public void AlignTextNoteRight()
        {
            int[] ids = new int[] { 205777, 205811, 205819 };
            AlignTestFunction(ids, AlignTag.AlignType.Right);
        }

        [Test]
        public void AlignTextNoteVerticaly()
        {
            int[] ids = new int[] { 205832, 205838, 205839 };
            AlignTestFunction(ids, AlignTag.AlignType.Vertically);
        }

        private void AlignTestFunction(int[] ids, AlignTag.AlignType alignType)
        {
            AlignTag.Align align = new AlignTag.Align();

            ICollection<ElementId> selectedIds = ids.Select(id => new ElementId(id)).ToList();

            using (TransactionGroup txg = new TransactionGroup(document))
            {
                align.AlignTag(alignType,txg,selectedIds, document);
            }
        }
    }
}
